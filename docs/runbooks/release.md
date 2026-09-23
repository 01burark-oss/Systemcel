# Systemcel Oracle release and recovery runbook

This runbook covers the live Oracle VM deployment. It does not authorize live payments or production rollout before the company and provider gates are complete.

## Release inputs

- A reviewed commit SHA with green CI, including migration/API smoke, dependency audit, secret scan and Docker build.
- SSH access to the Oracle VM and the checked-out repository under `deployment/oracle-free/`.
- Production secrets in the Oracle `.env`; never commit them or print them in logs.
- `SYSTEMCEL_DATABASE_CONNECTION_STRING` pointing to the private PostgreSQL 18 container.
- `SYSTEMCEL_SECRET_ENCRYPTION_KEY` stable across releases; do not rotate without a re-encryption plan.
- Exact HTTPS origins and Clerk production configuration.
- Payment provider `Fake` until the company/PayTR gate is opened.

## Pre-deploy gate

1. Confirm the working tree contains no secret or local `.env` file.
2. Record the commit SHA, current deployment state and migration count.
3. Confirm the latest Oracle logical backup exists, has a checksum, and has enough disk space for a new backup.
4. For schema changes, inspect the idempotent SQL and destructive statements. Take a logical `pg_dump` before deployment when data can be rewritten or removed.
5. Confirm `docker compose config --quiet` succeeds from `deployment/oracle-free/`.
6. Confirm current `/api/health/live` and `/api/health/ready` responses before changing the service.

## Deploy on Oracle

1. From `deployment/oracle-free/`, run `./scripts/deploy.sh` after reviewing its output.
2. The script validates Compose, builds the app image, pulls the `db` and `caddy` images, and starts the stack without exposing PostgreSQL publicly.
3. Watch `docker compose logs --tail=120 app` and confirm migrations complete before accepting traffic.
4. Confirm Caddy serves `https://systemcel.app` and `/api/health/ready` returns 200.

## Automatic production deploy

A successful `CI` run caused by a push to the default branch triggers `Deploy production`. The workflow rechecks that the CI commit is still the current default-branch head. A superseded candidate is skipped with a notice; its newer CI candidate triggers a separate deploy. For the current candidate, the workflow asks Oracle for its deployed SHA through a restricted SSH key and sends only the incremental Git bundle between those two commits.

The production SSH key is environment-scoped and forced to `/opt/systemcel/bin/systemcel-github-deploy-gateway`. It cannot open a general shell or enable port, agent, X11, or PTY forwarding. The gateway accepts only `status` and `deploy <40-character-sha>`.

Oracle verifies the bundle reference and ancestry, rejects tracked local changes, acquires a single-deploy lock, and checks out the exact candidate detached. It then runs that candidate's quiesced verified database/appdata backup; a failed backup stops the release and leaves `last-success` unchanged. Only after the backup succeeds does Oracle deploy, check locally with readiness and smoke, and get checked again from GitHub against `https://systemcel.app`.

Required `production` environment secrets:

- `ORACLE_SSH_PRIVATE_KEY`: dedicated restricted deploy key; never reuse a personal administration key.
- `ORACLE_SSH_KNOWN_HOSTS`: verified Oracle host-key entry; runtime `ssh-keyscan` is not accepted.

The workflow does not perform an automatic database downgrade or silent rollback. A failed deployment remains failed and requires the forward-fix or schema-compatible recovery procedure below.

## Post-deploy smoke

Run the public checks with the exact candidate SHA and write a sanitized result file:

```powershell
pwsh ./scripts/Test-SystemcelPublic.ps1 `
  -BaseUrl https://systemcel.app `
  -CandidateSha <40-character-sha> `
  -EnvironmentName production `
  -EvidencePath ./artifacts/public-smoke.json
```

The smoke covers liveness, readiness, security headers, public plan/config responses, CORS, and the canonical `https://systemcel.app/api/v1` authentication boundary. It does not authenticate, create an account, mutate customer data, or prove Clerk/tenant/pilot acceptance.

Create the real-world checklist from the same candidate SHA, keep all unexecuted checks `pending`, and validate it before and after adding observations:

```powershell
pwsh ./scripts/New-SystemcelReleaseEvidence.ps1 -CandidateSha <40-character-sha> -EnvironmentName production -OutputPath ./artifacts/release-evidence.json
pwsh ./scripts/Test-SystemcelReleaseEvidence.ps1 -Path ./artifacts/release-evidence.json
```

K1, K2, K6, K7 and K9 require controlled accounts, provider access, a physical device or pilot participants as stated in the generated checklist. An executed check needs an anonymous `actorLabel`, UTC time, actual result and a non-secret evidence reference. Do not record names, e-mail addresses, tokens, passwords, customer data or full request bodies.

## Controlled release bundle

The manually triggered `Release bundle` workflow accepts a full commit SHA from the default branch. It checks out that exact commit, rejects a mismatched or non-default-branch candidate, creates a source archive, manifest, SHA-256 checksum and pending evidence template, then uploads them as a 30-day artifact. The workflow never connects to Oracle and never deploys production.

Before deployment, verify both the artifact checksum and the manifest's `candidateSha`. Record the deployed SHA separately in the evidence file; artifact creation alone is not deployment proof. GitHub Actions concurrency permits only one release bundle job at a time.

## Rollback and database recovery

Prefer a forward fix. For a schema-compatible application regression, restore the previous verified image/commit through the Oracle checkout and rerun readiness and smoke checks. Never run EF `database update` to an older migration. Code rollback does not roll back database data.

For corrupt or accidentally deleted data, freeze writes by stopping app and Caddy, verify the dump checksum, and use `deployment/oracle-free/scripts/restore.sh`. Validate migration history, row checks and smoke tests before reopening traffic. Keep the pre-restore backup and incident evidence until reconciliation and sign-off. A DNS-only reversal is not safe after Oracle writes; reconcile data first.

## Evidence and safety

Capture commit SHA, container status, UTC start/end, readiness results, migration IDs, backup/restore checksum and user impact. Never capture secrets, auth tokens, payment payloads, raw GİB credentials or customer documents.
