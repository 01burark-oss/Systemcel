#!/usr/bin/env bash
set -euo pipefail
cd /opt/systemcel/repo/deployment/oracle-free
config="$(docker compose config --format json)"
db_user="$(jq -r '.services.db.environment.POSTGRES_USER' <<<"$config")"
db_name="$(jq -r '.services.db.environment.POSTGRES_DB' <<<"$config")"
audit_dir=/opt/systemcel/backups/source-audit-20260902
umask 077
docker compose exec -T db psql -X -q -U "$db_user" -d systemcel_source_audit_20260902 < /opt/systemcel/project-source.sql > "$audit_dir/projection.sql"
docker compose exec -T db psql -X -qAt -v ON_ERROR_STOP=1 -U "$db_user" -d systemcel_source_audit_20260902 < "$audit_dir/projection.sql" | sort > "$audit_dir/source-rows.txt"
docker compose exec -T db psql -X -qAt -v ON_ERROR_STOP=1 -U "$db_user" -d "$db_name" < "$audit_dir/projection.sql" | sort > "$audit_dir/target-rows.txt"
comm -23 "$audit_dir/source-rows.txt" "$audit_dir/target-rows.txt" > "$audit_dir/unmatched-source-rows.txt"
echo "Source rows: $(wc -l < "$audit_dir/source-rows.txt")"
echo "Unmatched source rows: $(wc -l < "$audit_dir/unmatched-source-rows.txt")"
cut -d '|' -f 1 "$audit_dir/unmatched-source-rows.txt" | uniq -c
