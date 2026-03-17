#!/usr/bin/env bash
# Build STARAPIClient and deploy native lib (libstar_api.so / libstar_api.dylib) + star_api.h to game folders.
# Linux/macOS equivalent of BUILD_AND_DEPLOY_STAR_CLIENT.bat.
# Usage: ./BUILD_AND_DEPLOY_STAR_CLIENT.sh [ -ForceBuild ] [ -Runtime linux-x64|osx-x64|osx-arm64 ]
# Build is skipped if library is up to date unless -ForceBuild is passed.

set -e

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
