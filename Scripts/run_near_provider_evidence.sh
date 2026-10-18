#!/usr/bin/env bash
set -euo pipefail

: "${OASIS_REPO_ROOT_WSL:?OASIS_REPO_ROOT_WSL is required}"
: "${OASIS_NEAR_CACHE_ROOT:?OASIS_NEAR_CACHE_ROOT is required}"

node_version="22.22.2"
node_root="${OASIS_NEAR_CACHE_ROOT}/node/${node_version}"

mkdir -p "${OASIS_NEAR_CACHE_ROOT}"
if [[ ! -x "${node_root}/bin/node" ]]; then
  echo 'Retained pinned Linux Node runtime missing; provision once explicitly, no automatic download.' >&2
  exit 1
fi
[[ "$("${node_root}/bin/node" --version)" == "v${node_version}" ]] || { echo 'Retained Linux Node version differs.' >&2; exit 1; }

export PATH="${node_root}/bin:${PATH}"
build_root="/var/tmp/oasis-near-evidence"
mkdir -p "$build_root"
[[ "$(realpath "$build_root")" == /var/tmp/oasis-near-evidence ]] || exit 1
exec 9>"$build_root/environment.lock"
flock -n 9 || { echo 'The reusable NEAR environment is already in use.' >&2; exit 1; }
mkdir -p "${build_root}/tmp"
export TMPDIR="${build_root}/tmp"

cp -a "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/package.json" \
  "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/package-lock.json" \
  "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/run-evidence.mjs" \
  "${build_root}/"
mkdir -p "${build_root}/contract/src"
cp "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/contract/src/contract.ts" "${build_root}/contract/src/contract.ts"

cd "${build_root}"
if [[ ! -e node_modules ]]; then
  test -d "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/node_modules" || { echo 'Retained NEAR compiler/SDK packages missing; no installation attempted.' >&2; exit 1; }
  ln -s "${OASIS_REPO_ROOT_WSL}/Scripts/TestHosts/near-local/node_modules" node_modules
fi
npm ls --all >/dev/null
npm run build:contract
node run-evidence.mjs
