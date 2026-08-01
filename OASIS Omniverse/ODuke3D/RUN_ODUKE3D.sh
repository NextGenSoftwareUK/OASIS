#!/usr/bin/env bash
# RUN_ODUKE3D.sh — Build (if needed) and launch ODuke3D
#
# Usage: ./RUN_ODUKE3D.sh [gamedata_dir]
#   gamedata_dir  — directory containing duke3d.grp (default: $HOME/Duke3D)
#
# Environment variables:
#   EDUKE32_SRC    — ODuke3D (EDuke32 fork) source path (default: $HOME/Source/ODuke3D)
#   STAR_USERNAME  — OASIS username
#   STAR_PASSWORD  — OASIS password
#   STAR_API_KEY   — API key (alternative)
#   STAR_AVATAR_ID — OASIS avatar ID

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

EDUKE32_SRC="${EDUKE32_SRC:-$HOME/Source/ODuke3D}"
GAMEDATA="${1:-$HOME/Duke3D}"

EXE="$EDUKE32_SRC/eduke32"

if [[ ! -f "$EXE" ]]; then
    echo "[ODuke3D] Executable not found — building first..."
    bash "$HERE/BUILD_ODUKE3D.sh" batch
fi

echo "[ODuke3D] Launching: $EXE"
echo "[ODuke3D] Game data: $GAMEDATA"

"$EXE" -j "$GAMEDATA"
