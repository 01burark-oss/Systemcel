#!/usr/bin/env bash
set -euo pipefail

deploy_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${deploy_dir}"

if [[ ! -f .env ]]; then
  echo "Eksik: ${deploy_dir}/.env. Önce .env.example dosyasını kopyalayıp gerçek sırlarla doldurun." >&2
  exit 1
fi

docker compose config --quiet
docker compose build --pull app
docker compose pull db caddy
docker compose up -d --remove-orphans

for attempt in {1..30}; do
  if curl --fail --silent --show-error http://127.0.0.1:8080/api/health/ready >/dev/null; then
    docker compose ps
    echo "Systemcel readiness kontrolü geçti."
    exit 0
  fi
  sleep 3
done

docker compose ps
docker compose logs --tail=120 app
echo "Systemcel readiness kontrolü zaman aşımına uğradı." >&2
exit 1
