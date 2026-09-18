#!/usr/bin/env bash
set -euo pipefail

test_dir="$(mktemp -d)"
trap 'rm -rf -- "${test_dir}"' EXIT
mkdir -p "${test_dir}/bin" "${test_dir}/backups" "${test_dir}/remote" "${test_dir}/state" "${test_dir}/run"

cat >"${test_dir}/bin/rclone" <<'RCLONE'
#!/usr/bin/env bash
set -euo pipefail
operation="$1"
shift
remote_path() {
  local value="$1"
  printf '%s/%s' "${FAKE_REMOTE_ROOT}" "${value#*:}"
}
case "${operation}" in
  config)
    echo 'type = crypt'
    ;;
  copy)
    source_dir="$1"
    destination="$(remote_path "$2")"
    mkdir -p "${destination}"
    cp -- "${source_dir}"/systemcel-* "${destination}/"
    ;;
  check)
    [[ "${FAKE_RCLONE_FAIL_CHECK:-0}" != 1 ]] || exit 9
    source_dir="$1"
    destination="$(remote_path "$2")"
    for source_file in "${source_dir}"/systemcel-*; do
      cmp -s "${source_file}" "${destination}/$(basename "${source_file}")"
    done
    ;;
  copyto)
    destination="$(remote_path "$2")"
    mkdir -p "$(dirname "${destination}")"
    cp -- "$1" "${destination}"
    ;;
  cat)
    cat "$(remote_path "$1")"
    ;;
  *)
    echo "Beklenmeyen fake rclone işlemi: ${operation}" >&2
    exit 10
    ;;
esac
RCLONE
chmod +x "${test_dir}/bin/rclone"
cat >"${test_dir}/bin/flock" <<'FLOCK'
#!/usr/bin/env bash
exit 0
FLOCK
chmod +x "${test_dir}/bin/flock"
cat >"${test_dir}/bin/jq" <<'JQ'
#!/usr/bin/env bash
set -euo pipefail
[[ "${1:-}" != -e ]] || exit 0
package_id=''
completed_at=''
manifest_sha256=''
while [[ "$#" -gt 0 ]]; do
  if [[ "$1" == --arg ]]; then
    case "$2" in
      package_id) package_id="$3" ;;
      completed_at) completed_at="$3" ;;
      manifest_sha256) manifest_sha256="$3" ;;
    esac
    shift 3
  else
    shift
  fi
done
printf '{"schema_version":1,"package_id":"%s","completed_at_utc":"%s","manifest_sha256":"%s"}\n' \
  "${package_id}" "${completed_at}" "${manifest_sha256}"
JQ
chmod +x "${test_dir}/bin/jq"

make_package() {
  local stamp="$1"
  printf 'db-%s\n' "${stamp}" >"${test_dir}/backups/systemcel-db-${stamp}.dump"
  printf 'app-%s\n' "${stamp}" >"${test_dir}/backups/systemcel-appdata-${stamp}.tar.gz"
  (
    cd "${test_dir}/backups"
    sha256sum "systemcel-db-${stamp}.dump" "systemcel-appdata-${stamp}.tar.gz" >"systemcel-${stamp}.sha256"
  )
}

make_package 20260911T010203Z
env \
  PATH="${test_dir}/bin:${PATH}" \
  BACKUP_DIR="${test_dir}/backups" \
  OFFSITE_BACKUP_STATE_DIR="${test_dir}/state" \
  OFFSITE_BACKUP_LOCK_FILE="${test_dir}/run/offsite.lock" \
  RCLONE_CRYPT_REMOTE='fake:systemcel' \
  FAKE_REMOTE_ROOT="${test_dir}/remote" \
  OFFSITE_BACKUP_ATTEMPTS=1 \
  "$(dirname "${BASH_SOURCE[0]}")/../scripts/backup-offsite.sh" --transfer-only

grep -q '"package_id":"systemcel-20260911T010203Z"' "${test_dir}/state/offsite-last-success.json"
test -f "${test_dir}/remote/systemcel/packages/systemcel-20260911T010203Z/COMPLETED.json"

# The same completed package can be retried without replacing its marker.
env \
  PATH="${test_dir}/bin:${PATH}" \
  BACKUP_DIR="${test_dir}/backups" \
  OFFSITE_BACKUP_STATE_DIR="${test_dir}/state" \
  OFFSITE_BACKUP_LOCK_FILE="${test_dir}/run/offsite.lock" \
  RCLONE_CRYPT_REMOTE='fake:systemcel' \
  FAKE_REMOTE_ROOT="${test_dir}/remote" \
  OFFSITE_BACKUP_ATTEMPTS=1 \
  "$(dirname "${BASH_SOURCE[0]}")/../scripts/backup-offsite.sh" --transfer-only
previous_state="$(sha256sum "${test_dir}/state/offsite-last-success.json")"

make_package 20260912T010203Z
if env \
  PATH="${test_dir}/bin:${PATH}" \
  BACKUP_DIR="${test_dir}/backups" \
  OFFSITE_BACKUP_STATE_DIR="${test_dir}/state" \
  OFFSITE_BACKUP_LOCK_FILE="${test_dir}/run/offsite.lock" \
  RCLONE_CRYPT_REMOTE='fake:systemcel' \
  FAKE_REMOTE_ROOT="${test_dir}/remote" \
  FAKE_RCLONE_FAIL_CHECK=1 \
  OFFSITE_BACKUP_ATTEMPTS=1 \
  "$(dirname "${BASH_SOURCE[0]}")/../scripts/backup-offsite.sh" --transfer-only; then
  echo "Checksum arızası başarı sayıldı." >&2
  exit 1
fi

test "${previous_state}" = "$(sha256sum "${test_dir}/state/offsite-last-success.json")"
test ! -e "${test_dir}/remote/systemcel/packages/systemcel-20260912T010203Z/COMPLETED.json"
echo 'backup-offsite smoke: OK'
