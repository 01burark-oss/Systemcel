#!/usr/bin/env bash
set -euo pipefail

deploy_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
backup_dir="${BACKUP_DIR:-/opt/systemcel/backups}"
state_dir="${OFFSITE_BACKUP_STATE_DIR:-/var/lib/systemcel-backup}"
state_file="${OFFSITE_BACKUP_STATE_FILE:-${state_dir}/offsite-last-success.json}"
lock_file="${OFFSITE_BACKUP_LOCK_FILE:-/run/systemcel-backup/offsite.lock}"
remote="${RCLONE_CRYPT_REMOTE:-}"
attempts="${OFFSITE_BACKUP_ATTEMPTS:-4}"
transfer_only=false

if [[ "${1:-}" == "--transfer-only" ]]; then
  transfer_only=true
elif [[ "$#" -ne 0 ]]; then
  echo "Kullanım: $0 [--transfer-only]" >&2
  exit 2
fi

for command_name in cmp flock jq rclone sha256sum; do
  command -v "${command_name}" >/dev/null || {
    echo "Eksik bağımlılık: ${command_name}" >&2
    exit 1
  }
done

if [[ -z "${remote}" || "${remote}" != *:* ]]; then
  echo "RCLONE_CRYPT_REMOTE, rclone crypt hedefi olarak ayarlanmalıdır." >&2
  exit 1
fi
if [[ ! "${attempts}" =~ ^[1-9][0-9]*$ ]]; then
  echo "OFFSITE_BACKUP_ATTEMPTS pozitif bir tam sayı olmalıdır." >&2
  exit 1
fi

remote="${remote%/}"
remote_name="${remote%%:*}"
if ! rclone config show "${remote_name}" 2>/dev/null | grep -Eq '^[[:space:]]*type[[:space:]]*=[[:space:]]*crypt[[:space:]]*$'; then
  echo "Yedek hedefi doğrulanmış bir rclone crypt remote değil." >&2
  exit 1
fi

umask 077
mkdir -p "$(dirname "${lock_file}")" "${state_dir}"
exec 9>"${lock_file}"
if ! flock -n 9; then
  echo "Başka bir uzak yedek işi çalışıyor." >&2
  exit 75
fi

if [[ "${transfer_only}" == false ]]; then
  BACKUP_DIR="${backup_dir}" OFFSITE_BACKUP_STATE_FILE="${state_file}" \
    "${deploy_dir}/scripts/backup.sh" --quiesce
fi

shopt -s nullglob
manifests=("${backup_dir}"/systemcel-[0-9]*T[0-9]*Z.sha256)
if [[ "${#manifests[@]}" -eq 0 ]]; then
  echo "Aktarılacak doğrulanmış yerel yedek bulunamadı." >&2
  exit 1
fi
IFS=$'\n' sorted_manifests=($(printf '%s\n' "${manifests[@]}" | sort))
unset IFS
manifest="${sorted_manifests[${#sorted_manifests[@]}-1]}"
manifest_name="$(basename "${manifest}")"

if [[ ! "${manifest_name}" =~ ^systemcel-([0-9]{8}T[0-9]{6}Z)\.sha256$ ]]; then
  echo "Geçersiz yedek manifesti." >&2
  exit 1
fi
timestamp="${BASH_REMATCH[1]}"
package_id="systemcel-${timestamp}"
db_name="systemcel-db-${timestamp}.dump"
appdata_name="systemcel-appdata-${timestamp}.tar.gz"
for local_name in "${db_name}" "${appdata_name}" "${manifest_name}"; do
  if [[ ! -f "${backup_dir}/${local_name}" ]]; then
    echo "Yedek paketi eksik: ${local_name}" >&2
    exit 1
  fi
done
(cd "${backup_dir}" && sha256sum --check --strict "${manifest_name}" >/dev/null)

retry() {
  local try=1 delay=2
  until "$@"; do
    if (( try >= attempts )); then
      return 1
    fi
    echo "Uzak yedek işlemi başarısız; yeniden denenecek (${try}/${attempts})." >&2
    sleep "${delay}"
    try=$((try + 1))
    delay=$((delay * 2))
    (( delay > 30 )) && delay=30
  done
}

final_remote="${remote}/packages/${package_id}"
include_args=(--include "/${db_name}" --include "/${appdata_name}" --include "/${manifest_name}" --exclude '*')
retry rclone copy "${backup_dir}" "${final_remote}" "${include_args[@]}" --immutable --retries 1 --low-level-retries 1 --log-level ERROR
retry rclone check "${backup_dir}" "${final_remote}" "${include_args[@]}" --one-way --download --checkers 2 --log-level ERROR

manifest_sha256="$(sha256sum "${manifest}" | awk '{print $1}')"
completed_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
marker="$(mktemp "${state_dir}/completed.XXXXXX")"
state_tmp="$(mktemp "${state_dir}/state.XXXXXX")"
cleanup() { rm -f -- "${marker}" "${state_tmp}"; }
trap cleanup EXIT
if rclone cat "${final_remote}/COMPLETED.json" --retries 1 --low-level-retries 1 --log-level ERROR >"${marker}" 2>/dev/null; then
  jq -e --arg package_id "${package_id}" --arg manifest_sha256 "${manifest_sha256}" \
    '.package_id == $package_id and .manifest_sha256 == $manifest_sha256' "${marker}" >/dev/null || {
      echo "Uzak tamamlanma işareti yerel paketle uyuşmuyor." >&2
      exit 1
    }
else
  jq -n --arg package_id "${package_id}" --arg completed_at "${completed_at}" --arg manifest_sha256 "${manifest_sha256}" \
    '{schema_version:1, package_id:$package_id, completed_at_utc:$completed_at, manifest_sha256:$manifest_sha256}' >"${marker}"
  # Object stores do not have atomic directory renames. Consumers therefore only
  # accept a package whose COMPLETED.json marker was uploaded after final checks.
  retry rclone copyto "${marker}" "${final_remote}/COMPLETED.json" --immutable --retries 1 --low-level-retries 1 --log-level ERROR
fi
verify_marker() {
  cmp -s "${marker}" <(rclone cat "${final_remote}/COMPLETED.json" --retries 1 --low-level-retries 1 --log-level ERROR)
}
retry verify_marker

cp -- "${marker}" "${state_tmp}"
chmod 600 "${state_tmp}"
mv -f -- "${state_tmp}" "${state_file}"
echo "Uzak yedek tamamlandı ve doğrulandı: ${package_id}"
