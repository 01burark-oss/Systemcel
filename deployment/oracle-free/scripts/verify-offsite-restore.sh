#!/usr/bin/env bash
set -euo pipefail

for command_name in docker jq rclone sha256sum tar; do
  command -v "${command_name}" >/dev/null || {
    echo "Eksik bağımlılık: ${command_name}" >&2
    exit 1
  }
done

remote="${SYSTEMCEL_OFFSITE_REMOTE:-}"
if [[ -z "${remote}" || "${remote}" != *:* ]]; then
  echo "SYSTEMCEL_OFFSITE_REMOTE, rclone crypt hedefi olarak ayarlanmalıdır." >&2
  exit 1
fi

if [[ "$#" -gt 1 ]]; then
  echo "Kullanım: $0 [systemcel-YYYYMMDDTHHMMSSZ]" >&2
  exit 2
fi

remote="${remote%/}"
package_id="${1:-}"
if [[ -z "${package_id}" ]]; then
  package_id="$(rclone lsf "${remote}/packages/" --dirs-only --log-level ERROR |
    sed -n 's@^\(systemcel-[0-9]\{8\}T[0-9]\{6\}Z\)/$@\1@p' | sort | tail -n 1)"
fi
if [[ ! "${package_id}" =~ ^systemcel-[0-9]{8}T[0-9]{6}Z$ ]]; then
  echo "Uzak hedefte geçerli yedek paketi bulunamadı." >&2
  exit 1
fi

timestamp="${package_id#systemcel-}"
restore_dir="$(mktemp -d /tmp/systemcel-remote-restore.XXXXXX)"
container="systemcel-restore-check-$$"
container_created=false
started_at="$(date +%s)"
cleanup() {
  if [[ "${container_created}" == true ]]; then
    docker rm -f "${container}" >/dev/null 2>&1 || true
  fi
  if [[ "${restore_dir}" == /tmp/systemcel-remote-restore.* ]]; then
    rm -rf -- "${restore_dir}"
  fi
}
trap cleanup EXIT

rclone copy "${remote}/packages/${package_id}" "${restore_dir}" --retries 1 --low-level-retries 1 --log-level ERROR
cd "${restore_dir}"
jq -e --arg package_id "${package_id}" --arg manifest_sha256 "$(sha256sum "${package_id}.sha256" | awk '{print $1}')" \
  '.package_id == $package_id and .manifest_sha256 == $manifest_sha256' COMPLETED.json >/dev/null
sha256sum --check --strict "${package_id}.sha256"
tar -tzf "systemcel-appdata-${timestamp}.tar.gz" >/dev/null

# This disposable database has no network and exposes no host port.
docker run -d --network none --name "${container}" -e POSTGRES_HOST_AUTH_METHOD=trust \
  -v "${restore_dir}:/restore:ro" postgres:18-alpine >/dev/null
container_created=true
for attempt in $(seq 1 30); do
  if docker exec "${container}" pg_isready -U postgres >/dev/null 2>&1; then break; fi
  sleep 1
done
docker exec "${container}" pg_isready -U postgres >/dev/null
docker exec "${container}" pg_restore --no-owner -U postgres -d postgres "/restore/systemcel-db-${timestamp}.dump"
tables="$(docker exec "${container}" psql -U postgres -d postgres -Atc \
  "select count(*) from information_schema.tables where table_schema='public' and table_type='BASE TABLE'")"
[[ "${tables}" =~ ^[1-9][0-9]*$ ]]
completed_at="$(date +%s)"
backup_started_at="$(date -u -d "${timestamp:0:4}-${timestamp:4:2}-${timestamp:6:2} ${timestamp:9:2}:${timestamp:11:2}:${timestamp:13:2} UTC" +%s)"
echo "REMOTE_RESTORE_OK package=${package_id} tables=${tables} rto_seconds=$((completed_at - started_at)) backup_age_seconds=$((completed_at - backup_started_at))"
