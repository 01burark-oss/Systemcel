#!/usr/bin/env bash
set -euo pipefail

candidate_sha="${1:-}"
bundle_path="${2:-}"
repo_dir="${SYSTEMCEL_REPO_DIR:-/opt/systemcel/repo}"
state_dir="${SYSTEMCEL_DEPLOY_STATE_DIR:-/var/lib/systemcel-deploy}"
lock_file="${SYSTEMCEL_DEPLOY_LOCK_FILE:-${state_dir}/deploy.lock}"

if [[ ! "${candidate_sha}" =~ ^[0-9a-f]{40}$ ]]; then
  echo "Geçersiz aday commit SHA." >&2
  exit 2
fi

if [[ ! -f "${bundle_path}" ]]; then
  echo "Dağıtım paketi bulunamadı." >&2
  exit 2
fi

command -v flock >/dev/null
exec 9>"${lock_file}"
if ! flock -n 9; then
  echo "Başka bir Systemcel dağıtımı devam ediyor." >&2
  exit 3
fi

if [[ ! -d "${repo_dir}/.git" || ! -f "${repo_dir}/deployment/oracle-free/.env" ]]; then
  echo "Oracle çalışma dizini veya production .env bulunamadı." >&2
  exit 4
fi

cd "${repo_dir}"
git config core.fileMode false

if [[ -n "$(git status --porcelain --untracked-files=no)" ]]; then
  echo "Oracle checkout takipli yerel değişiklik içeriyor; dağıtım durduruldu." >&2
  git status --short --untracked-files=no >&2
  exit 5
fi

bundle_ref="refs/heads/systemcel-deploy-${candidate_sha}"
bundle_head="$(git bundle list-heads "${bundle_path}" "${bundle_ref}" | awk 'NR == 1 { print $1 }')"
if [[ "${bundle_head}" != "${candidate_sha}" ]]; then
  echo "Dağıtım paketi beklenen commit'i içermiyor." >&2
  exit 6
fi

git bundle verify "${bundle_path}" >/dev/null
git fetch --no-tags "${bundle_path}" "${bundle_ref}"
fetched_sha="$(git rev-parse FETCH_HEAD)"
if [[ "${fetched_sha}" != "${candidate_sha}" ]]; then
  echo "Aktarılan commit doğrulanamadı." >&2
  exit 6
fi

previous_sha="$(git rev-parse HEAD)"
if ! git merge-base --is-ancestor "${previous_sha}" "${candidate_sha}"; then
  echo "Aday commit canlı sürümün devamı değil; otomatik geri veya yan dal dağıtımı reddedildi." >&2
  exit 7
fi

bash ./deployment/oracle-free/scripts/backup.sh --quiesce
git checkout --detach "${candidate_sha}"

if [[ "$(git rev-parse HEAD)" != "${candidate_sha}" ]]; then
  echo "Oracle checkout aday commit'e geçemedi." >&2
  exit 8
fi

bash ./deployment/oracle-free/scripts/deploy.sh
bash ./deployment/oracle-free/scripts/smoke.sh http://127.0.0.1:8080

umask 077
mkdir -p "${state_dir}"
state_tmp="$(mktemp "${state_dir}/last-success.XXXXXX")"
printf '{"deployedSha":"%s","previousSha":"%s","completedAtUtc":"%s"}\n' \
  "${candidate_sha}" \
  "${previous_sha}" \
  "$(date -u +%Y-%m-%dT%H:%M:%SZ)" >"${state_tmp}"
mv "${state_tmp}" "${state_dir}/last-success.json"

echo "Systemcel dağıtımı tamamlandı: ${candidate_sha}"
