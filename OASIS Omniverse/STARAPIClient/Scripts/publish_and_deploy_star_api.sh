#!/usr/bin/env bash
# Build and deploy STARAPIClient native library + star_api.h on Linux/macOS.
# Unix equivalent of publish_and_deploy_star_api.bat (which runs the Windows-only publish_and_deploy_star_api.ps1).
# Implementation delegates to build-and-deploy-star-api-unix.sh (same flags as the unix deploy path).

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

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNIX_DEPLOY="$SCRIPT_DIR/build-and-deploy-star-api-unix.sh"
[[ -f "$UNIX_DEPLOY" ]] || UNIX_DEPLOY="$SCRIPT_DIR/build-and-deploy-star-api-linux.sh"

if [[ ! -f "$UNIX_DEPLOY" ]]; then
  echo "ERROR: Deploy script not found (build-and-deploy-star-api-unix.sh or build-and-deploy-star-api-linux.sh)." >&2
  exit 1
fi

exec bash "$UNIX_DEPLOY" "$@"
