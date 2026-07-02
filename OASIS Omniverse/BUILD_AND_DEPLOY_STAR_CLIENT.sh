#!/usr/bin/env bash
# Build STARAPIClient and deploy native lib (star_api.so / star_api.dylib from NativeAOT) + star_api.h to game folders.
# Linux/macOS equivalent of BUILD_AND_DEPLOY_STAR_CLIENT.bat.
# Usage: ./BUILD_AND_DEPLOY_STAR_CLIENT.sh [ -ForceBuild ] [ -Runtime linux-x64|osx-x64|osx-arm64 ]
# Build is skipped if library is up to date unless -ForceBuild is passed.

set -e


# OASIS: pause before exit when run from GUI (CI: OASIS_SCRIPT_NO_PAUSE=1)
if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" != "1" ]]; then
  _OASIS_TD="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  while [[ "$_OASIS_TD" != "/" ]]; do
    if [[ -f "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh" ]]; then
      # shellcheck disable=SC1091
      source "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh"
      break
    fi
    _OASIS_TD="$(dirname "$_OASIS_TD")"
  done
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPT="$ROOT/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh"
[[ ! -f "$SCRIPT" ]] && SCRIPT="$ROOT/STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh"

if [[ ! -f "$SCRIPT" ]]; then
  echo "ERROR: Deploy script not found under STARAPIClient/Scripts (build-and-deploy-star-api-unix.sh or build-and-deploy-star-api-linux.sh)." >&2
  exit 1
fi

echo "========================================"
echo "OASIS STAR API - Build and Deploy"
echo "========================================"
echo ""

exec bash "$SCRIPT" "$@"
