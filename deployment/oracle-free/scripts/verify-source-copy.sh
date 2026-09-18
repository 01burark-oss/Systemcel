#!/usr/bin/env bash
set -euo pipefail
cd /opt/systemcel/repo/deployment/oracle-free
config="$(docker compose config --format json)"
db_user="$(jq -r '.services.db.environment.POSTGRES_USER' <<<"$config")"
audit_db="systemcel_source_audit_20260902"
audit_dir=/opt/systemcel/backups/source-audit-20260902
umask 077
mkdir -p "$audit_dir"
docker run --rm --env-file /opt/systemcel/systemcel-source-audit.env postgres:18-alpine pg_dump --format=custom > "$audit_dir/source.dump"
docker compose exec -T db createdb -U "$db_user" "$audit_db"
docker compose exec -T db pg_restore -U "$db_user" -d "$audit_db" --no-owner --no-acl --exit-on-error < /opt/systemcel/backups/final/systemcel-db-20260902T100305Z.dump
docker compose exec -T db psql -X -U "$db_user" -d "$audit_db" < /opt/systemcel/fingerprint.sql > "$audit_dir/copied-source.txt"
docker run --rm --env-file /opt/systemcel/systemcel-source-audit.env -v /opt/systemcel/fingerprint.sql:/audit.sql:ro postgres:18-alpine psql -X -f /audit.sql > "$audit_dir/current-source.txt"
if diff -u "$audit_dir/copied-source.txt" "$audit_dir/current-source.txt"; then
  echo 'SOURCE_MATCH: all source tables match the final migration dump.'
else
  echo 'SOURCE_DIFF: do not delete DigitalOcean.' >&2
  exit 1
fi
