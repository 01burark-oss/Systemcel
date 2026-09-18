#!/usr/bin/env bash
set -euo pipefail

test_dir="$(mktemp -d)"
trap 'rm -rf -- "${test_dir}"' EXIT
source_repo="${test_dir}/source"
bundle_path="${test_dir}/candidate.bundle"
mkdir -p "${source_repo}" "${test_dir}/bin"

cat >"${test_dir}/bin/flock" <<'FLOCK'
#!/usr/bin/env bash
exit 0
FLOCK
chmod +x "${test_dir}/bin/flock"
export PATH="${test_dir}/bin:${PATH}"

git -C "${source_repo}" init --quiet --initial-branch=main
git -C "${source_repo}" config user.name 'Systemcel Test'
git -C "${source_repo}" config user.email 'systemcel-test@example.invalid'
mkdir -p "${source_repo}/deployment/oracle-free/scripts"
printf '.env\n' >"${source_repo}/deployment/oracle-free/.gitignore"

write_scripts() {
  local version="$1"
  printf '%s\n' "${version}" >"${source_repo}/version.txt"
  cat >"${source_repo}/deployment/oracle-free/scripts/backup.sh" <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
printf 'backup\n' >>"${DEPLOY_TEST_LOG:?}"
[[ "${DEPLOY_TEST_BACKUP_FAIL:-0}" != 1 ]]
SCRIPT
  cat >"${source_repo}/deployment/oracle-free/scripts/deploy.sh" <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
printf 'deploy\n' >>"${DEPLOY_TEST_LOG:?}"
SCRIPT
  cat >"${source_repo}/deployment/oracle-free/scripts/smoke.sh" <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
printf 'smoke\n' >>"${DEPLOY_TEST_LOG:?}"
SCRIPT
  chmod +x "${source_repo}/deployment/oracle-free/scripts/"*.sh
}

write_scripts v1
git -C "${source_repo}" add .
git -C "${source_repo}" commit --quiet -m 'base'
base_sha="$(git -C "${source_repo}" rev-parse HEAD)"

write_scripts v2
git -C "${source_repo}" add .
git -C "${source_repo}" commit --quiet -m 'candidate'
candidate_sha="$(git -C "${source_repo}" rev-parse HEAD)"
bundle_ref="refs/heads/systemcel-deploy-${candidate_sha}"
git -C "${source_repo}" update-ref "${bundle_ref}" "${candidate_sha}"
git -C "${source_repo}" bundle create "${bundle_path}" "${bundle_ref}" "^${base_sha}"
git -C "${source_repo}" update-ref -d "${bundle_ref}"

new_target() {
  local name="$1"
  local target="${test_dir}/${name}"
  git clone --quiet "${source_repo}" "${target}"
  git -C "${target}" checkout --quiet --detach "${base_sha}"
  touch "${target}/deployment/oracle-free/.env"
  printf '%s' "${target}"
}

run_deploy() {
  local target="$1"
  local log_path="$2"
  env \
    SYSTEMCEL_REPO_DIR="${target}" \
    SYSTEMCEL_DEPLOY_LOCK_FILE="${test_dir}/deploy.lock" \
    SYSTEMCEL_DEPLOY_STATE_DIR="${target}/state" \
    DEPLOY_TEST_LOG="${log_path}" \
    "$(dirname "${BASH_SOURCE[0]}")/../scripts/deploy-bundle.sh" \
      "${candidate_sha}" "${bundle_path}"
}

success_target="$(new_target success)"
success_log="${test_dir}/success.log"
run_deploy "${success_target}" "${success_log}"
test "$(git -C "${success_target}" rev-parse HEAD)" = "${candidate_sha}"
test "$(tr '\n' ',' <"${success_log}")" = 'backup,deploy,smoke,'
grep -q "\"deployedSha\":\"${candidate_sha}\"" "${success_target}/state/last-success.json"

dirty_target="$(new_target dirty)"
printf 'dirty\n' >>"${dirty_target}/version.txt"
if run_deploy "${dirty_target}" "${test_dir}/dirty.log"; then
  echo "Takipli yerel değişiklik dağıtımı durdurmadı." >&2
  exit 1
fi
test "$(git -C "${dirty_target}" rev-parse HEAD)" = "${base_sha}"
test ! -e "${test_dir}/dirty.log"

backup_target="$(new_target backup-failure)"
if env \
  SYSTEMCEL_REPO_DIR="${backup_target}" \
  SYSTEMCEL_DEPLOY_LOCK_FILE="${test_dir}/backup-failure.lock" \
  SYSTEMCEL_DEPLOY_STATE_DIR="${backup_target}/state" \
  DEPLOY_TEST_LOG="${test_dir}/backup-failure.log" \
  DEPLOY_TEST_BACKUP_FAIL=1 \
  "$(dirname "${BASH_SOURCE[0]}")/../scripts/deploy-bundle.sh" \
    "${candidate_sha}" "${bundle_path}"; then
  echo "Başarısız yedekten sonra dağıtım devam etti." >&2
  exit 1
fi
test "$(git -C "${backup_target}" rev-parse HEAD)" = "${candidate_sha}"
test "$(tr '\n' ',' <"${test_dir}/backup-failure.log")" = 'backup,'
test ! -e "${backup_target}/state/last-success.json"

if env \
  SYSTEMCEL_REPO_DIR="${success_target}" \
  SYSTEMCEL_DEPLOY_LOCK_FILE="${test_dir}/invalid.lock" \
  SYSTEMCEL_DEPLOY_STATE_DIR="${success_target}/state" \
  "$(dirname "${BASH_SOURCE[0]}")/../scripts/deploy-bundle.sh" \
    invalid "${bundle_path}"; then
  echo "Geçersiz commit SHA kabul edildi." >&2
  exit 1
fi

echo 'deploy-bundle smoke: OK'
