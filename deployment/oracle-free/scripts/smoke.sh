#!/usr/bin/env bash
set -euo pipefail

base_url="${1:-http://127.0.0.1:8080}"

curl --fail --silent --show-error "${base_url}/api/health/live" | jq -e '.durum == "canli"' >/dev/null
curl --fail --silent --show-error "${base_url}/api/health/ready" | jq -e '.durum == "hazir" and .veritabani == "PostgreSql"' >/dev/null

headers="$(mktemp)"
trap 'rm -f "${headers}"' EXIT
curl --fail --silent --show-error --dump-header "${headers}" --output /dev/null "${base_url}/"

grep -qi '^x-content-type-options: nosniff' "${headers}"
grep -qi '^x-frame-options:' "${headers}"
grep -qi '^referrer-policy:' "${headers}"

echo "Smoke testi geçti: ${base_url}"
