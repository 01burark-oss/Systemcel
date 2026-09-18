#!/usr/bin/env bash
set -euo pipefail

test_dir="$(mktemp -d)"
trap 'rm -rf -- "${test_dir}"' EXIT
mkdir -p "${test_dir}/bin" "${test_dir}/state"
printf '{}\n' >"${test_dir}/state/offsite-last-success.json"

cat >"${test_dir}/bin/curl" <<'CURL'
#!/usr/bin/env bash
exit 0
CURL

cat >"${test_dir}/bin/df" <<'DF'
#!/usr/bin/env bash
printf 'Filesystem 1024-blocks Used Available Capacity Mounted-on\n'
printf '/dev/fake 100 72 28 72%% /opt/systemcel\n'
DF

cat >"${test_dir}/bin/jq" <<'JQ'
#!/usr/bin/env bash
set -euo pipefail
query="$*"
case "${query}" in
  *POSTGRES_USER*) echo systemcel_app ;;
  *POSTGRES_DB*) echo systemcel ;;
  *completed_at_utc*) date -u +%Y-%m-%dT%H:%M:%SZ ;;
  *) exit 2 ;;
esac
JQ

cat >"${test_dir}/bin/docker" <<'DOCKER'
#!/usr/bin/env bash
set -euo pipefail
if [[ "$1" == compose && "$2" == config ]]; then
  echo '{"services":{"db":{"environment":{"POSTGRES_USER":"systemcel_app","POSTGRES_DB":"systemcel"}}}}'
elif [[ "$1" == compose && "$2" == exec ]]; then
  printf '4\n100\n'
elif [[ "$1" == compose && "$2" == ps ]]; then
  echo "fake-$4"
elif [[ "$1" == inspect && "$2" == --format && "$3" == *RestartCount* ]]; then
  echo 2
elif [[ "$1" == inspect && "$2" == --format && "$3" == *State.Running* ]]; then
  echo true
else
  exit 3
fi
DOCKER
chmod +x "${test_dir}/bin/"*

output="$(env \
  PATH="${test_dir}/bin:${PATH}" \
  OFFSITE_BACKUP_STATE_FILE="${test_dir}/state/offsite-last-success.json" \
  SYSTEMCEL_DISK_PATH="${test_dir}" \
  "$(dirname "${BASH_SOURCE[0]}")/../scripts/collect-monitoring.sh" --stdout)"

grep -q '^systemcel_host_disk_usage_percent 72$' <<<"${output}"
grep -q '^systemcel_readiness_success 1$' <<<"${output}"
grep -q '^systemcel_offsite_backup_state_valid 1$' <<<"${output}"
grep -q '^systemcel_postgres_connections 4$' <<<"${output}"
grep -q '^systemcel_postgres_max_connections 100$' <<<"${output}"
test "$(grep -c '^systemcel_container_restart_count.* 2$' <<<"${output}")" -eq 3
if grep -q 'package_id=' <<<"${output}"; then
  echo 'Paket kimliği metric label olarak yayınlandı.' >&2
  exit 1
fi
echo 'collect-monitoring smoke: OK'
