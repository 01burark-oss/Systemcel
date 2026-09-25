# Systemcel Oracle monitoring baseline

This baseline applies to the Oracle VM Docker deployment. Production thresholds must be tuned from observed traffic; the current payment provider remains `Fake` until the company/provider gate.

## Collection and ownership

`deployment/oracle-free/scripts/collect-monitoring.sh` writes Prometheus text atomically to `/var/lib/systemcel-monitoring/systemcel.prom` every minute. Configure the host metric agent/node exporter to collect that directory and forward it to persistent monitoring outside the VM. The backup metric reads `/var/lib/systemcel-backup/offsite-last-success.json`; a local dump timestamp is deliberately not accepted as offsite success.

The application already records request count, duration and error instruments under meter `Systemcel.Api`. Bind that meter to the chosen OpenTelemetry/Prometheus backend for HTTP 5xx ratio alerts; do not derive a durable rate from short Docker log retention. Keep trace/request ID searchable in the log backend. Never use tenant ID, user ID, token, request body, filename or backup package ID as a metric label. The current package ID remains in the local backup state JSON and service log for recovery evidence.

An external HTTPS monitor must probe `https://systemcel.app/api/health/ready` from outside Oracle. The alert destination, secondary critical channel and owner are deployment inputs; neither should depend only on this VM. Recommended notification repeat is 30 minutes while critical and one explicit recovery notification.

## Health signals

- Liveness: `GET /api/health/live`; process is running. Do not page on one failed probe.
- Readiness: `GET /api/health/ready`; app can connect to PostgreSQL. Use this for traffic readiness.
- Deployment: Docker Compose container health, Caddy certificate status, image/deploy logs and restart count.
- Host: Oracle VM CPU, memory, disk and Docker volume usage.
- Database: PostgreSQL 18 connections, CPU, memory, disk, backup age and checksum.
- Product: checkout failures, webhook rejection/replay, subscription lifecycle failures, reminder delivery failures, import rejection rate and sustained `429` rate limiting.

## Initial alerts

| Signal | Warning | Critical | First response |
| --- | --- | --- | --- |
| Readiness | 2 failures in 5 minutes | 5 consecutive failures | Check app and PostgreSQL logs; stop rollout |
| HTTP 5xx | >2% for 10 minutes | >5% for 5 minutes | Correlate by trace ID; roll back code if schema-compatible |
| App restarts | 2 in 15 minutes | 3 in 10 minutes | Inspect OOM, exit and health events |
| PostgreSQL connections | >70% for 15 minutes | >85% for 5 minutes | Find leaked/long queries; diagnose before scaling |
| Host or PostgreSQL disk | >70% | >85% | Review growth and backups; expand before write risk |
| Backup age | >26 hours | >36 hours | Check backup timer and take a logical backup if safe |
| Host CPU (initial, not observed) | >80% for 15 minutes | >95% for 10 minutes | Correlate load, throttling and request duration |
| Host memory (initial, not observed) | >80% for 10 minutes | >90% for 5 minutes | Check OOM events and container growth |
| Checkout failure | 3 synthetic failures | >10% real attempts | Disable checkout flag; preserve event IDs |
| Webhook processing | Any synthetic signature mismatch | Sustained valid-event failures | Preserve provider event IDs; do not replay blindly |
| Rate limiting | >1% API responses for 15 minutes | >5% for 5 minutes | Separate abuse from bad client retry logic |

Metric mappings:

- `systemcel_host_cpu_usage_percent`, `systemcel_host_memory_usage_percent`, `systemcel_host_disk_usage_percent`: thresholds above.
- `increase(systemcel_container_restart_count[15m]) >= 2` warning; `increase(...[10m]) >= 3` critical. Container recreation can reset the gauge, so also retain Docker events.
- `systemcel_postgres_connections / systemcel_postgres_max_connections`: 70% warning, 85% critical.
- `systemcel_offsite_backup_state_valid != 1` or backup age over 26/36 hours. Age `-1` means unknown and is critical after initial setup grace.
- `systemcel-monitoring-alert.timer` checks local disk, verified offsite backup age, and collector freshness every five minutes. It sends SMTP warnings at >70% disk or >26 hours backup age, critical alerts at >85% or >36 hours, repeats critical alerts every 30 minutes, and sends one recovery message. This local route depends on the VM and does not replace the external HTTPS probe or persistent off-VM metrics.
- `systemcel_readiness_success`: two failures within five minutes warning; five consecutive failures critical. The external probe is authoritative for a VM outage.
- `rate(systemcel.http.server.request.error.count{error.type="server"}[10m]) / rate(systemcel.http.server.request.count[10m])`: 2% warning; use a five-minute window and 5% for critical.

## Safe dry tests

Run local collection without changing the metric file:

```bash
cd /opt/systemcel/repo/deployment/oracle-free
./scripts/collect-monitoring.sh --stdout
systemctl start systemcel-monitoring.service
journalctl -u systemcel-monitoring.service --no-pager -n 30
```

Test rules in an isolated/silenced route or staging alert policy. Inject a synthetic metric into the monitoring backend (for example disk `86`, backup age `129601`, readiness `0`) and then remove it to prove both alert and recovery delivery. Do not fill the live disk, stop the production database, age/delete the real backup state, or create restart loops. Record UTC time, rule name, receiving channel, alert receipt and recovery receipt; do not attach secrets or customer data.

The local alert evaluator can be tested without changing the production collector output or alert state: run `python3 deployment/oracle-free/tests/alert-monitoring-smoke.py`, then pass a separate synthetic metric file and state path to `scripts/alert-monitoring.py --metrics ... --state ... --dry-run`. An SMTP delivery test uses the same isolated paths without `--dry-run`.
Confirm the critical alert in the recipient's inbox or spam folder, not only in the SMTP sender's “Delivered” list. On 25 September the first synthetic critical message was classified as spam while recovery reached the inbox; the critical message was marked “Not spam” for this sender. Do not disable spam filtering account-wide.

Before enabling paging, verify these failure paths separately:

1. Stop or block only a disposable external probe target to prove the off-VM readiness alert.
2. Point a staging copy of the backup service at an invalid/non-production remote and confirm that `offsite-last-success.json` is unchanged.
3. Restore the staging target, run a transfer, and confirm backup age returns to normal and a recovery notification arrives.
4. Confirm warning/critical repeat behavior and secondary-channel routing with the named operations owner.

## Log contract

Every actionable server log should include UTC timestamp, level, event name, trace ID and a non-secret tenant/business identifier where appropriate. Payment logs may include internal payment/event IDs and state transitions, but never raw provider payloads, card data or auth headers.

Never log connection strings, passwords, AES keys, Clerk tokens or cookies, raw GİB credentials, uploaded documents, full customer records, provider signatures or complete webhook bodies.

## Triage order

1. Confirm scope with liveness/readiness, Docker Compose status and Caddy health.
2. Correlate sanitized logs by trace ID, commit SHA and container restart time.
3. Classify the issue as code, configuration, database, provider or abusive traffic.
4. Follow `release.md`: code rollback does not restore database data; restore requires a verified dump and reconciliation.
5. Record the incident timeline and follow-up owner without copying secrets or customer content.
