#!/usr/bin/env bash
set -euo pipefail

repo_dir="/opt/systemcel/repo"
state_file="/var/lib/systemcel-deploy/last-success.json"
max_bundle_bytes=$((256 * 1024 * 1024))

case "${SSH_ORIGINAL_COMMAND:-}" in
  status)
    if [[ -f "${state_file}" ]]; then
      jq -er '.deployedSha | select(type == "string" and test("^[0-9a-f]{40}$"))' "${state_file}"
    else
      git -C "${repo_dir}" rev-parse HEAD
    fi
    ;;
  deploy\ *)
    candidate_sha="${SSH_ORIGINAL_COMMAND#deploy }"
    if [[ ! "${candidate_sha}" =~ ^[0-9a-f]{40}$ ]]; then
      echo "Geçersiz dağıtım komutu." >&2
      exit 64
    fi

    umask 077
    bundle_path="$(mktemp "/tmp/systemcel-${candidate_sha}.XXXXXX.bundle")"
    trap 'rm -f -- "${bundle_path}"' EXIT
    dd if=/dev/stdin of="${bundle_path}" bs=1M count=257 status=none || true
    bundle_size="$(stat -c '%s' "${bundle_path}")"
    if (( bundle_size == 0 || bundle_size > max_bundle_bytes )); then
      echo "Dağıtım paketi boyutu geçersiz." >&2
      exit 65
    fi

    /opt/systemcel/bin/systemcel-deploy-bundle "${candidate_sha}" "${bundle_path}"
    ;;
  *)
    echo "Bu SSH anahtarı yalnız Systemcel dağıtımı için kullanılabilir." >&2
    exit 64
    ;;
esac
