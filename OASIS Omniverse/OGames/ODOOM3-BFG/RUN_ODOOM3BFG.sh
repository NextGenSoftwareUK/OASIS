#!/usr/bin/env bash
# RUN_ODOOM3BFG.sh — Launch RBDOOM-3-BFG (Linux/macOS)
set -euo pipefail

RBDOOM_BUILD="${RBDOOM_BUILD:-/opt/ODOOM3-BFG/build-linux}"
EXE="$RBDOOM_BUILD/rbdoom3bfg"

if [ ! -f "$EXE" ]; then
    echo "[ERROR] Executable not found: $EXE"
    echo "Run BUILD_ODOOM3BFG.sh first, or set RBDOOM_BUILD."
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
[ ! -f "$RBDOOM_BUILD/oasisstar.json" ] && cp "$SCRIPT_DIR/oasisstar.json" "$RBDOOM_BUILD/"

echo "Starting ODOOM3-BFG..."
exec "$EXE" +set fs_game d3xp "$@"
