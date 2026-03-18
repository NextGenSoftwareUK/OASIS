#!/usr/bin/env bash
# Launcher for start_web4_and_web5_apis.ps1 on Linux/macOS. Requires PowerShell Core (pwsh).
set -e


SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/include/pause_on_exit.inc.sh"
pwsh -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/start_web4_and_web5_apis.ps1" "$@"
exit $?
