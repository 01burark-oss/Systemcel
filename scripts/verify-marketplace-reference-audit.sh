#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
container_id="$(docker ps --filter ancestor=postgres:18-alpine --format '{{.ID}}' | head -n 1)"
if [[ -z "$container_id" ]]; then
  echo "PostgreSQL test container is unavailable." >&2
  exit 1
fi

result="$({
  cat "$repo_root/scripts/marketplace-reference-audit.sql"
  cat "$repo_root/scripts/marketplace-reference-audit-fixtures.sql"
  cat "$repo_root/scripts/marketplace-reference-audit.sql"
} | docker exec -i "$container_id" psql \
  -U postgres -d systemcel_test_ci -X -q -t -A -F '|' -v ON_ERROR_STOP=1
)"

expected=(
  shipment_line_order_item_mismatch
  receipt_label_order_mismatch
  payment_distribution_master_order_mismatch
  paid_settlement_missing_transfer_reference
  successful_payment_missing_provider_transaction_reference
)

for issue in "${expected[@]}"; do
  matches="$(printf '%s\n' "$result" | awk -F '|' -v issue="$issue" '$1 == issue { print $2 }')"
  if [[ "$matches" != $'0\n1' ]]; then
    echo "Reference audit expected clean/fixture counts 0/1 for $issue, got: ${matches:-missing}." >&2
    exit 1
  fi
done

row_count="$(printf '%s\n' "$result" | awk -F '|' 'NF == 2 { count++ } END { print count+0 }')"
if [[ "$row_count" != "$((${#expected[@]} * 2))" ]]; then
  echo "Reference audit returned $row_count rows; expected $((${#expected[@]} * 2))." >&2
  exit 1
fi

echo "Marketplace reference audit: ${#expected[@]} clean and positive fixture checks passed."
