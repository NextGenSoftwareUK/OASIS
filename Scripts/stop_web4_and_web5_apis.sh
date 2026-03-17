#!/usr/bin/env bash
# Launcher for stop_web4_and_web5_apis.ps1 on Linux/macOS. Requires PowerShell Core (pwsh).
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec pwsh -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/stop_web4_and_web5_apis.ps1" "$@"
