#!/usr/bin/env bash
set -euo pipefail

if [[ "$#" -ne 3 ]]; then
  echo "Kullanım: $0 <systemcel-db.dump> <systemcel-appdata.tar.gz> <systemcel.sha256>" >&2
  exit 1
fi

deploy_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
db_dump="$(realpath "$1")"
appdata_archive="$(realpath "$2")"
checksum_manifest="$(realpath "$3")"
backup_dir="$(dirname "${checksum_manifest}")"
db_dump_name="$(basename "${db_dump}")"
appdata_archive_name="$(basename "${appdata_archive}")"

cd "${deploy_dir}"

if [[ ! -f .env ]]; then
  echo "Eksik: ${deploy_dir}/.env" >&2
  exit 1
fi

command -v jq >/dev/null || {
  echo "Eksik bağımlılık: jq" >&2
  exit 1
}

if [[ "$(dirname "${db_dump}")" != "${backup_dir}" || "$(dirname "${appdata_archive}")" != "${backup_dir}" ]]; then
  echo "Dump, uygulama arşivi ve checksum manifesti aynı klasörde olmalıdır." >&2
  exit 1
fi

if [[ ! "${db_dump_name}" =~ ^systemcel-db-([0-9]{8}T[0-9]{6}Z)\.dump$ ]]; then
  echo "Geçersiz veritabanı yedeği adı: ${db_dump_name}" >&2
  exit 1
fi
backup_timestamp="${BASH_REMATCH[1]}"
expected_archive_name="systemcel-appdata-${backup_timestamp}.tar.gz"
expected_manifest_name="systemcel-${backup_timestamp}.sha256"

if [[ "${appdata_archive_name}" != "${expected_archive_name}" || "$(basename "${checksum_manifest}")" != "${expected_manifest_name}" ]]; then
  echo "Yedek dosyalarının zaman damgaları birbiriyle eşleşmiyor." >&2
  exit 1
fi

if ! awk -v db="${db_dump_name}" -v appdata="${appdata_archive_name}" '
  NF != 2 || length($1) != 64 || $1 !~ /^[0-9a-fA-F]+$/ { exit 1 }
  $2 == db && !seen_db { seen_db = 1; count++; next }
  $2 == appdata && !seen_appdata { seen_appdata = 1; count++; next }
  { exit 1 }
  END { if (count != 2 || !seen_db || !seen_appdata) exit 1 }
' "${checksum_manifest}"; then
  echo "Checksum manifesti beklenen iki yedek dosyasını içermiyor." >&2
  exit 1
fi

(
  cd "${backup_dir}"
  sha256sum --check --strict "$(basename "${checksum_manifest}")"
)

docker run --rm --interactive postgres:18-alpine \
  pg_restore --list < "${db_dump}" >/dev/null
if tar -tzf "${appdata_archive}" | grep -Eq '(^/|(^|/)\.\.(/|$))'; then
  echo "Uygulama verisi arşivinde güvenli olmayan dosya yolu bulundu." >&2
  exit 1
fi
tar -tzf "${appdata_archive}" >/dev/null

compose_config="$(docker compose config --format json)"
postgres_user="$(jq -er '.services.db.environment.POSTGRES_USER' <<<"${compose_config}")"
postgres_db="$(jq -er '.services.db.environment.POSTGRES_DB' <<<"${compose_config}")"

echo "Bu işlem mevcut Oracle aday veritabanını ve app_data içeriğini değiştirir."
read -r -p "Devam etmek için RESTORE yazın: " confirmation
[[ "${confirmation}" == "RESTORE" ]] || exit 1

restore_failed=true
restore_status() {
  if [[ "${restore_failed}" == true ]]; then
    docker compose stop app caddy >/dev/null 2>&1 || true
    echo "Geri yükleme tamamlanamadı. Güvenlik için app ve caddy kapalı bırakıldı." >&2
  fi
}
trap restore_status EXIT

docker compose stop app caddy
docker compose up -d db

for attempt in {1..30}; do
  if docker compose exec -T db pg_isready --username "${postgres_user}" --dbname "${postgres_db}" >/dev/null 2>&1; then
    break
  fi
  if [[ "${attempt}" -eq 30 ]]; then
    docker compose logs --tail=120 db >&2
    echo "PostgreSQL geri yükleme için hazır olmadı." >&2
    exit 1
  fi
  sleep 2
done

docker compose exec -T db dropdb --if-exists --force --username "${postgres_user}" "${postgres_db}"
docker compose exec -T db createdb --username "${postgres_user}" --owner "${postgres_user}" "${postgres_db}"
docker compose exec -T db pg_restore \
  --username "${postgres_user}" \
  --dbname "${postgres_db}" \
  --exit-on-error --no-owner --no-acl \
  < "${db_dump}"

docker compose exec -T db psql \
  --username "${postgres_user}" \
  --dbname "${postgres_db}" \
  --tuples-only --no-align \
  --command 'SELECT 1' | grep -qx '1'

docker compose create app >/dev/null
docker run --rm \
  --volume systemcel_app_data:/data \
  --volume "$(dirname "${appdata_archive}"):/restore:ro" \
  alpine:3.22 \
  sh -c "find /data -mindepth 1 -maxdepth 1 -exec rm -rf -- {} + && tar -C /data -xzf '/restore/$(basename "${appdata_archive}")'"

docker compose up -d
for attempt in {1..30}; do
  if curl --fail --silent --show-error http://127.0.0.1:8080/api/health/ready >/dev/null; then
    break
  fi
  if [[ "${attempt}" -eq 30 ]]; then
    docker compose logs --tail=120 app >&2
    echo "Geri yükleme sonrası readiness kontrolü zaman aşımına uğradı." >&2
    exit 1
  fi
  sleep 3
done
"${deploy_dir}/scripts/smoke.sh" http://127.0.0.1:8080

restore_failed=false
echo "Geri yükleme tamamlandı ve smoke testi geçti."
