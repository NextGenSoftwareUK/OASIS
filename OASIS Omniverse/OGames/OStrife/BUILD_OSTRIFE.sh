#!/usr/bin/env bash
# OStrife — UZDoom + OASIS STAR API
# Usage: ./BUILD_OSTRIFE.sh [ batch ]
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
UZDOOM_SRC="${UZDOOM_SRC:-$HOME/Source/ODOOM}"
OGENGINECLIENT="$HERE/../../OGEngineClient"

[ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ] && bash "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true

if [ ! -d "$UZDOOM_SRC/src" ]; then
    echo "[OStrife] UZDoom source not found at: $UZDOOM_SRC"; exit 1
fi

cp -f "$HERE/ostrife_ogengine_integration.h"   "$UZDOOM_SRC/src/"
cp -f "$HERE/ostrife_ogengine_integration.cpp" "$UZDOOM_SRC/src/"
cp -f "$OGENGINECLIENT/ogengine.h"             "$UZDOOM_SRC/src/"

if [ -f "$UZDOOM_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$UZDOOM_SRC/build-linux"
    cmake -S "$UZDOOM_SRC" -B "$UZDOOM_SRC/build-linux" -DCMAKE_BUILD_TYPE=Release
    cmake --build "$UZDOOM_SRC/build-linux" -- -j"$(nproc 2>/dev/null || echo 4)"
fi

echo "[OStrife] Done. Run with: uzdoom -iwad strife1.wad"
[ "${1:-}" != "batch" ] && read -r -p "Press Enter to continue..."
