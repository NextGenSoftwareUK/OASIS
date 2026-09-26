#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec pwsh -NoProfile -File "$SCRIPT_DIR/build_edge_unity_package.ps1" "$@"
