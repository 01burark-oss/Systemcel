# Systemcel staging release and rollback runbook

This runbook is for the DigitalOcean App Platform staging app and its attached Managed PostgreSQL cluster. It does not authorize live payments or production rollout.

## 1. Required release inputs

- A reviewed commit SHA with a green GitHub Actions `CI` workflow.
- A separate staging App Platform app and Managed PostgreSQL cluster.
- The database attached as the app's only trusted source.
- App secrets stored as encrypted environment variables, never in the repository or build logs.
- `SYSTEMCEL_DATABASE_CONNECTION_STRING` with TLS required.
- `SYSTEMCEL_SECRET_ENCRYPTION_KEY`: stable Base64 for exactly 32 random bytes. Generate once with `openssl rand -base64 32`; do not rotate without a re-encryption plan.
- Clerk staging keys and explicit HTTPS origins.
- Payment provider `Fake` only until the company/PayTR gate is opened.

Required fail-closed variables outside Development:

| Variable | Staging rule |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `SYSTEMCEL_DATABASE_CONNECTION_STRING` | Encrypted secret; attached staging database only |
| `SYSTEMCEL_SECRET_ENCRYPTION_KEY` | Encrypted secret; stable across deploys |
| `SYSTEMCEL_CLERK_ENABLED` | `true` |
| `SYSTEMCEL_CLERK_AUTHORITY` | Staging Clerk issuer |
| `SYSTEMCEL_CLERK_PUBLISHABLE_KEY` | Staging publishable key |
| `SYSTEMCEL_CLERK_AUTHORIZED_PARTIES` | Explicit staging HTTPS origin |
| `SYSTEMCEL_ALLOWED_ORIGINS` | Explicit staging HTTPS origin; no wildcard |
| `SYSTEMCEL_PAYMENT_PROVIDER` | `Fake` before the live-payment gate |

## 2. Pre-deploy gate

1. Confirm the working tree contains no secret or local `.env` file.
2. Require green unit, browser, PostgreSQL migration/API smoke, dependency audit, secret scan, and Docker build jobs.
3. Record the commit SHA, current live deployment ID, and current database migration count in the release note.
4. Confirm the Managed PostgreSQL backup is recent. DigitalOcean takes daily backups retained for seven days and supports point-in-time restore to a new cluster.
5. For a schema-changing release, inspect the generated idempotent SQL and explicitly identify destructive statements. Take a logical `pg_dump` as extra evidence when the change can delete or rewrite data.
6. Confirm `/api/health/live` and `/api/health/ready` return 200 on the currently live deployment.

## 3. Deploy

1. Deploy the reviewed commit through App Platform.
2. Configure the component health check as HTTP `GET /api/health/ready` on port `8080`. Suggested staging values: initial delay 30 seconds, period 10 seconds, timeout 5 seconds, success threshold 1, failure threshold 5.
3. Do not route traffic based only on `/api/health/live`; it intentionally does not check PostgreSQL.
4. Watch build, deploy, runtime, and migration logs. The app applies pending EF migrations before accepting traffic; a migration or secret configuration error must fail the deployment.
5. App Platform keeps the previous instance serving until the new instance is healthy. Do not disable the readiness check to force a broken release through.

## 4. Post-deploy smoke (staging)

Run without displaying tokens or connection strings:

1. `GET /api/health/live` → `200`, `durum=canli`.
2. `GET /api/health/ready` → `200`, `durum=hazir`, `veritabani=PostgreSql`.
3. `GET /api/public/planlar` → exactly five public paid plans and non-zero monthly prices.
4. Sign in with the staging Clerk account; verify tenant switching cannot expose another business.
5. Open `/app/abonelik`: summary, payment history, monthly-only initial checkout, VAT, explicit consent, extra accountant customer credits, and period-end cancellation.
6. Upload valid and invalid profile/chat files; invalid signature/extension and oversized archives must be rejected.
7. Generate a desktop import code; wrong user, second claim, and replay after success must fail.
8. Inspect response headers for HSTS (HTTPS), `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and CSP.
9. Verify rate limiting returns `429 application/problem+json` under a controlled burst, never against real user traffic.

Record UTC time, deployment ID, tester, test account, and pass/fail result. Do not record access tokens, passwords, customer data, or full request bodies.

## 5. Code rollback

Use code rollback for an application regression when the new database schema remains backward compatible.

1. In App Platform, open **Activity** and compare the current deployment with the target previous successful deployment.
2. Validate the rollback and select one of the ten most recent successful deployments.
3. Disable automatic deploy during incident containment if repeated deploys would overwrite the rollback.
4. Execute rollback and wait for `/api/health/ready` plus the smoke checks.
5. App Platform rollback restores code, configuration, and app spec; it does not roll back database data.
6. Do not run EF `database update` to an older migration in staging or production as an emergency shortcut.

## 6. Database recovery

Use database recovery only for corrupt or accidentally deleted data, not for an ordinary code regression.

1. Freeze writes by disabling ingress or putting the app in maintenance mode. Preserve logs and the incident timestamp.
2. In DigitalOcean Managed Databases, choose **Actions → Restore from backup**.
3. Restore the latest transaction or a selected point in time. DigitalOcean creates a new cluster; it does not overwrite the existing primary.
4. Add only the staging app as a trusted source on the restored cluster.
5. Validate migration history and application smoke tests against the restored cluster before changing the app connection secret.
6. Switch `SYSTEMCEL_DATABASE_CONNECTION_STRING` to the restored cluster as an encrypted secret and redeploy.
7. Keep the old cluster read-only until reconciliation and sign-off. Destroying a cluster also destroys its retained backups; never delete it during the incident.

## 7. Roll-forward preference

EF migrations in this repository are forward-only for deployed environments. Prefer a corrective additive migration and a new deployment. A code rollback is safe only when the prior code can operate on the current schema. A database restore requires explicit incident ownership because it can discard transactions after the restore point.

## 8. Incident evidence

Capture:

- commit SHA and deployment IDs;
- UTC start/end and detection channel;
- health/readiness results and sanitized error codes;
- migration IDs before and after;
- backup/restore point and restored cluster ID when applicable;
- user impact, reconciliation result, and follow-up owner.

Never capture secret values, auth tokens, payment payloads, raw GİB credentials, or customer document contents.

## References

- [DigitalOcean App Platform deployment rollback](https://docs.digitalocean.com/products/app-platform/how-to/manage-deployments/)
- [DigitalOcean App Platform health checks](https://docs.digitalocean.com/products/app-platform/how-to/manage-health-checks/)
- [DigitalOcean Managed PostgreSQL backup restore](https://docs.digitalocean.com/products/databases/postgresql/how-to/restore-from-backups/)
