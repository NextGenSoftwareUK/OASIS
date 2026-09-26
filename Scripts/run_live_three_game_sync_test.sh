#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec pwsh -NoProfile -File "$SCRIPT_DIR/run_live_three_game_sync_test.ps1" "$@"
