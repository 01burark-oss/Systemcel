#!/usr/bin/env bash
set -euo pipefail

deploy_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output_file="${MONITORING_OUTPUT_FILE:-/var/lib/systemcel-monitoring/systemcel.prom}"
backup_state="${OFFSITE_BACKUP_STATE_FILE:-/var/lib/systemcel-backup/offsite-last-success.json}"
readiness_url="${SYSTEMCEL_READINESS_URL:-http://127.0.0.1:8080/api/health/ready}"
disk_path="${SYSTEMCEL_DISK_PATH:-/opt/systemcel}"
stdout=false

if [[ "${1:-}" == "--stdout" ]]; then
  stdout=true
elif [[ "$#" -ne 0 ]]; then
  echo "Kullanım: $0 [--stdout]" >&2
  exit 2
fi

for command_name in curl docker jq awk df date sed; do
  command -v "${command_name}" >/dev/null || {
    echo "Eksik bağımlılık: ${command_name}" >&2
    exit 1
  }
done

umask 077
tmp_dir="$(mktemp -d)"
metrics_tmp="${tmp_dir}/systemcel.prom"
trap 'rm -rf -- "${tmp_dir}"' EXIT

read -r _ cpu_user cpu_nice cpu_system cpu_idle cpu_iowait cpu_irq cpu_softirq cpu_steal _ < /proc/stat
cpu_idle_before=$((cpu_idle + cpu_iowait))
cpu_total_before=$((cpu_user + cpu_nice + cpu_system + cpu_idle + cpu_iowait + cpu_irq + cpu_softirq + cpu_steal))
sleep 1
read -r _ cpu_user cpu_nice cpu_system cpu_idle cpu_iowait cpu_irq cpu_softirq cpu_steal _ < /proc/stat
cpu_idle_after=$((cpu_idle + cpu_iowait))
cpu_total_after=$((cpu_user + cpu_nice + cpu_system + cpu_idle + cpu_iowait + cpu_irq + cpu_softirq + cpu_steal))
cpu_delta=$((cpu_total_after - cpu_total_before))
idle_delta=$((cpu_idle_after - cpu_idle_before))
cpu_percent="$(awk -v total="${cpu_delta}" -v idle="${idle_delta}" 'BEGIN { if (total <= 0) print 0; else printf "%.2f", (total-idle)*100/total }')"

memory_percent="$(awk '/MemTotal:/ {total=$2} /MemAvailable:/ {available=$2} END {if (total <= 0) print 0; else printf "%.2f", (total-available)*100/total}' /proc/meminfo)"
disk_percent="$(df -P "${disk_path}" | awk 'NR==2 {gsub(/%/, "", $5); print $5}')"
now_epoch="$(date -u +%s)"
backup_age=-1
backup_ok=0
if [[ -f "${backup_state}" ]]; then
  completed_at="$(jq -er '.completed_at_utc | select(type == "string")' "${backup_state}" 2>/dev/null || true)"
  if [[ -n "${completed_at}" ]]; then
    completed_epoch="$(date -u -d "${completed_at}" +%s 2>/dev/null || true)"
    if [[ "${completed_epoch}" =~ ^[0-9]+$ && "${completed_epoch}" -le "${now_epoch}" ]]; then
      backup_age=$((now_epoch - completed_epoch))
      backup_ok=1
    fi
  fi
fi

readiness=0
if curl --fail --silent --show-error --max-time 10 "${readiness_url}" >/dev/null 2>&1; then
  readiness=1
fi

compose_json="$(cd "${deploy_dir}" && docker compose config --format json)"
postgres_user="$(jq -er '.services.db.environment.POSTGRES_USER' <<<"${compose_json}")"
postgres_db="$(jq -er '.services.db.environment.POSTGRES_DB' <<<"${compose_json}")"
db_connections=-1
db_max_connections=-1
if db_values="$(cd "${deploy_dir}" && docker compose exec -T db psql -At -U "${postgres_user}" -d "${postgres_db}" -c "SELECT count(*) FROM pg_stat_activity; SHOW max_connections;" 2>/dev/null)"; then
  db_connections="$(sed -n '1p' <<<"${db_values}")"
  db_max_connections="$(sed -n '2p' <<<"${db_values}")"
fi

{
  echo '# HELP systemcel_host_cpu_usage_percent One-second host CPU utilization sample.'
  echo '# TYPE systemcel_host_cpu_usage_percent gauge'
  echo "systemcel_host_cpu_usage_percent ${cpu_percent}"
  echo '# HELP systemcel_host_memory_usage_percent Host memory utilization.'
  echo '# TYPE systemcel_host_memory_usage_percent gauge'
  echo "systemcel_host_memory_usage_percent ${memory_percent}"
  echo '# HELP systemcel_host_disk_usage_percent Deployment filesystem utilization.'
  echo '# TYPE systemcel_host_disk_usage_percent gauge'
  echo "systemcel_host_disk_usage_percent ${disk_percent}"
  echo '# HELP systemcel_readiness_success Whether the local readiness probe succeeded.'
  echo '# TYPE systemcel_readiness_success gauge'
  echo "systemcel_readiness_success ${readiness}"
  echo '# HELP systemcel_offsite_backup_state_valid Whether the last remote backup state is valid.'
  echo '# TYPE systemcel_offsite_backup_state_valid gauge'
  echo "systemcel_offsite_backup_state_valid ${backup_ok}"
  echo '# HELP systemcel_offsite_backup_age_seconds Age of last verified completed remote backup; -1 when unavailable.'
  echo '# TYPE systemcel_offsite_backup_age_seconds gauge'
  echo "systemcel_offsite_backup_age_seconds ${backup_age}"
  echo '# HELP systemcel_postgres_connections Current PostgreSQL connections; -1 when unavailable.'
  echo '# TYPE systemcel_postgres_connections gauge'
  echo "systemcel_postgres_connections ${db_connections}"
  echo '# HELP systemcel_postgres_max_connections Configured PostgreSQL connection limit; -1 when unavailable.'
  echo '# TYPE systemcel_postgres_max_connections gauge'
  echo "systemcel_postgres_max_connections ${db_max_connections}"
  echo '# HELP systemcel_container_restart_count Docker restart count since container creation.'
  echo '# TYPE systemcel_container_restart_count gauge'
  for service in app db caddy; do
    container_id="$(cd "${deploy_dir}" && docker compose ps -q "${service}" 2>/dev/null || true)"
    restarts=0
    running=0
    if [[ -n "${container_id}" ]]; then
      restarts="$(docker inspect --format '{{.RestartCount}}' "${container_id}" 2>/dev/null || echo 0)"
      [[ "$(docker inspect --format '{{.State.Running}}' "${container_id}" 2>/dev/null || true)" == true ]] && running=1
    fi
    printf 'systemcel_container_restart_count{service="%s"} %s\n' "${service}" "${restarts}"
    printf 'systemcel_container_running{service="%s"} %s\n' "${service}" "${running}"
  done
  echo '# HELP systemcel_monitoring_collector_success Collector completed successfully.'
  echo '# TYPE systemcel_monitoring_collector_success gauge'
  echo 'systemcel_monitoring_collector_success 1'
} >"${metrics_tmp}"

if [[ "${stdout}" == true ]]; then
  cat "${metrics_tmp}"
else
  mkdir -p "$(dirname "${output_file}")"
  chmod 600 "${metrics_tmp}"
  mv -f -- "${metrics_tmp}" "${output_file}"
fi
