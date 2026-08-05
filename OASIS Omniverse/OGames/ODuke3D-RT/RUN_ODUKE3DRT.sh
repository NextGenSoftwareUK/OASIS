#!/usr/bin/env bash
# RUN_ODUKE3DRT.sh — Build (if needed) and launch ODuke3D-RT
#
# Usage: ./RUN_ODUKE3DRT.sh [gamedata_dir]
#
# Requires: Vulkan-capable GPU and driver, Duke Nukem 3D game data (duke3d.grp)

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

DUKERT_SRC="${DUKERT_SRC:-$HOME/Source/ODuke3D-RT}"  # Fork of Duke-RT
GAMEDATA="${1:-$HOME/Duke3D}"
BUILD_DIR="$DUKERT_SRC/build-linux-rt"
EXE="$BUILD_DIR/eduke32"

if [[ ! -f "$EXE" ]]; then
    echo "[ODuke3D-RT] Executable not found — building first..."
    bash "$HERE/BUILD_ODUKE3DRT.sh" batch
fi

echo "[ODuke3D-RT] Launching: $EXE"
echo "[ODuke3D-RT] Game data: $GAMEDATA"

"$EXE" -j "$GAMEDATA"
