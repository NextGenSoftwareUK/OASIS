#!/usr/bin/env bash
# Launcher for run_starapi_client_test_suite.ps1 on Linux/macOS. Requires PowerShell Core (pwsh).
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec pwsh -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/run_starapi_client_test_suite.ps1" "$@"
