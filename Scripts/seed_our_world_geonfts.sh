#!/usr/bin/env bash
# Launcher for seed_our_world_geonfts.ps1 on Linux/macOS. Requires PowerShell Core (pwsh).
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/include/pause_on_exit.inc.sh"
pwsh -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/seed_our_world_geonfts.ps1" "$@"
exit $?
