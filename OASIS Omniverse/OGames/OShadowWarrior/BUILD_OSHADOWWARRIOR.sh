#!/usr/bin/env bash
# OShadowWarrior — Raze + OASIS STAR API
# Usage: ./BUILD_OSHADOWWARRIOR.sh [ batch ]
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
RAZE_SRC="${RAZE_SRC:-$HOME/Source/Raze}"
OGENGINECLIENT="$HERE/../../OGEngineClient"

[ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ] && bash "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true

if [ ! -d "$RAZE_SRC/source/sw/src" ]; then
    echo "[OShadowWarrior] Raze source not found at: $RAZE_SRC"; exit 1
fi

cp -f "$HERE/osw_ogengine_integration.h"   "$RAZE_SRC/source/sw/src/"
cp -f "$HERE/osw_ogengine_integration.cpp" "$RAZE_SRC/source/sw/src/"
cp -f "$OGENGINECLIENT/ogengine.h"         "$RAZE_SRC/source/sw/src/"

if [ -f "$RAZE_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$RAZE_SRC/build-linux"
    cmake -S "$RAZE_SRC" -B "$RAZE_SRC/build-linux" -DCMAKE_BUILD_TYPE=Release
    cmake --build "$RAZE_SRC/build-linux" -- -j"$(nproc 2>/dev/null || echo 4)"
fi

echo "[OShadowWarrior] Done."
[ "${1:-}" != "batch" ] && read -r -p "Press Enter to continue..."
