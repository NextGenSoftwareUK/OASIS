#!/usr/bin/env bash
set -euo pipefail

: "${OASIS_REPO_ROOT_WSL:?OASIS_REPO_ROOT_WSL is required}"
: "${OASIS_NEAR_CACHE_ROOT:?OASIS_NEAR_CACHE_ROOT is required}"

node_version="22.22.2"
node_archive="node-v${node_version}-linux-x64.tar.xz"
node_sha256="88fd1ce767091fd8d4a99fdb2356e98c819f93f3b1f8663853a2dee9b438068a"
node_root="${OASIS_NEAR_CACHE_ROOT}/node/${node_version}"
archive_path="${OASIS_NEAR_CACHE_ROOT}/${node_archive}"

mkdir -p "${OASIS_NEAR_CACHE_ROOT}"
if [[ ! -x "${node_root}/bin/node" ]]; then
  curl -fsSL "https://nodejs.org/dist/v${node_version}/${node_archive}" -o "${archive_path}"
  echo "${node_sha256}  ${archive_path}" | sha256sum --check --status
  mkdir -p "${node_root}"
  tar -xJ --strip-components=1 -C "${node_root}" -f "${archive_path}"
fi

export PATH="${node_root}/bin:${PATH}"
build_root="$(mktemp -d /var/tmp/oasis-near-evidence.XXXXXX)"
settings_path="${OASIS_NEAR_CACHE_ROOT}/near-evidence-settings-$$.json"
mkdir -p "${build_root}/tmp"
export TMPDIR="${build_root}/tmp"
cleanup() {
  case "${build_root}" in
    /var/tmp/oasis-near-evidence.*) rm -rf -- "${build_root}" ;;
    *) echo "Refusing to remove unexpected NEAR evidence directory: ${build_root}" >&2 ;;
  esac
  rm -f -- "${settings_path}"
}
trap cleanup EXIT

cp -a "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/package.json" \
  "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/package-lock.json" \
  "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/run-evidence.mjs" \
  "${build_root}/"
cp -a "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/contract" "${build_root}/contract"

cd "${build_root}"
npm ci
npm run build:contract
OASIS_NEAR_SETTINGS_PATH_WSL="${settings_path}" node run-evidence.mjs
