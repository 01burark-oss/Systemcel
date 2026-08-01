# Systemcel staging monitoring baseline

This baseline is intentionally provider-light. Configure it on the DigitalOcean staging app before inviting beta users; production thresholds are opened only after the company/payment gate.

## Health signals

- Liveness: `GET /api/health/live`; process is running. Do not page on one failed probe.
- Readiness: `GET /api/health/ready`; process can connect to PostgreSQL. Use this for App Platform traffic readiness.
- Deployment: App Platform build/deploy/health-check status and runtime restart count.
- Database: Managed PostgreSQL CPU, memory, disk, connection utilization, replication/failover events and backup availability.
- Product: checkout failures, webhook rejection/replay, subscription lifecycle job failures, reminder delivery failures, import rejection rate and sustained `429` rate limiting.

## Initial staging alerts

| Signal | Warning | Critical | First response |
| --- | --- | --- | --- |
| Readiness | 2 failures in 5 minutes | 5 consecutive failures | Check runtime and PostgreSQL logs; stop rollout |
| HTTP 5xx | >2% for 10 minutes | >5% for 5 minutes | Correlate by trace ID; rollback code if schema-compatible |
| App restarts | 2 in 15 minutes | 3 in 10 minutes | Inspect OOM/exit/health events |
| PostgreSQL connections | >70% for 15 minutes | >85% for 5 minutes | Find leaked/long queries; scale only after diagnosis |
| PostgreSQL disk | >70% | >85% | Review growth/backups; expand before write risk |
| Backup age | >26 hours | >36 hours | Open provider incident; take logical backup if safe |
| Checkout failure | 3 synthetic failures | >10% real attempts | Disable checkout flag; preserve event IDs |
| Webhook processing | Any signature mismatch in synthetic test | sustained valid-event failures | Preserve provider event IDs; do not replay blindly |
| Rate limiting | >1% API responses for 15 minutes | >5% for 5 minutes | Separate abuse from bad client retry logic |

Thresholds are a starting point and must be tuned from staging traffic. A single user error, rejected malicious file or invalid signature is not an availability incident.

## Log contract

Every actionable server log should include UTC timestamp, level, event name, trace ID and non-secret tenant/business identifier where appropriate. Payment logs may include internal payment/event IDs and state transitions, but never raw provider payloads, card data or auth headers.

Never log:

- connection strings, passwords, AES keys, Clerk tokens or cookies;
- raw GİB credentials;
- uploaded document contents or full customer records;
- payment-provider signatures or complete webhook bodies;
- plaintext e-mail/phone when a stable internal identifier is sufficient.

## Triage order

1. Confirm scope with liveness/readiness and App Platform deployment health.
2. Correlate sanitized logs by trace ID and deployment ID.
3. Classify as code, configuration, database, provider or abusive traffic.
4. Follow `release.md`: code rollback does not restore database data; database recovery restores to a new cluster.
5. Record the incident timeline and follow-up owner without copying secrets or customer content.
