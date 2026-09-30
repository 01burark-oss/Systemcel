#!/usr/bin/env bash
set -euo pipefail
set +x
umask 077
cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
if [[ -e .env || -e .env.secrets ]]; then
    echo "Configuration already exists; refusing to overwrite." >&2
    exit 1
fi
[[ -t 0 ]] || { echo "Use an interactive SSH terminal." >&2; exit 1; }
read -r -p "Tester public IPv4 address: " customer_ip
if [[ ! "$customer_ip" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]]; then
    echo "Invalid IPv4 address." >&2; exit 1
fi
IFS=. read -r a b c d <<< "$customer_ip"
for octet in "$a" "$b" "$c" "$d"; do
    ((10#$octet <= 255)) || { echo "Invalid IPv4 address." >&2; exit 1; }
done
read -r -s -p "PayTR Merchant Key: " merchant_key
printf '\n'
read -r -s -p "PayTR Merchant Salt: " merchant_salt
printf '\n'
if [[ -z "$merchant_key" || -z "$merchant_salt" || ${#merchant_key} -gt 256 || ${#merchant_salt} -gt 256 ||
      "$merchant_key$merchant_salt" == *$'\r'* ]]; then
    echo "Invalid PayTR values." >&2; exit 1
fi
password="$(openssl rand -hex 32)"
printf 'PAYTR_TEST_HOST=paytr-test.systemcel.app\nPAYTR_MERCHANT_ID=753709\nPRODUCTION_EDGE_NETWORK=systemcel_edge\nTEST_CUSTOMER_IP=%s\nPOSTGRES_PASSWORD=%s\n' "$customer_ip" "$password" > .env
printf 'SYSTEMCEL_PAYTR_MERCHANT_KEY=%s\nSYSTEMCEL_PAYTR_MERCHANT_SALT=%s\n' "$merchant_key" "$merchant_salt" > .env.secrets
unset merchant_key merchant_salt password
chmod 600 .env .env.secrets
echo "Test configuration created. No credentials were printed."
