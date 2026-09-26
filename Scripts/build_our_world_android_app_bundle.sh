#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if command -v pwsh >/dev/null 2>&1; then
  exec pwsh -NoProfile -File "$SCRIPT_DIR/build_our_world_android_app_bundle.ps1" "$@"
fi
exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build_our_world_android_app_bundle.ps1" "$@"
