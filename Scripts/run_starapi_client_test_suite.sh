#!/usr/bin/env bash
set -e


SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/include/pause_on_exit.inc.sh"
pwsh -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/run_starapi_client_test_suite.ps1" "$@"
exit $?
