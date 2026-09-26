#!/usr/bin/env bash
# ODoom64 — Doom64 EX+ + OASIS STAR API
# id Tech 1 C codebase. Requires doom64.wad.
# Usage: ./BUILD_ODOOM64.sh [ batch ]
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
DOOM64_SRC="${DOOM64_SRC:-$HOME/Source/ODoom64}"
OGENGINECLIENT="$HERE/../../OGEngineClient"

[ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ] && bash "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true

if [ ! -d "$DOOM64_SRC/src" ]; then
    echo "[ODoom64] Doom64 EX+ source not found at: $DOOM64_SRC"
    echo "Clone from https://github.com/azdo/doom64ex-plus or set DOOM64_SRC."
    exit 1
fi

echo "[ODoom64] Copying integration files..."
cp -f "$HERE/odoom64_ogengine_integration.h" "$DOOM64_SRC/src/doom64/"
cp -f "$HERE/odoom64_ogengine_integration.c" "$DOOM64_SRC/src/doom64/"
cp -f "$OGENGINECLIENT/ogengine.h"           "$DOOM64_SRC/src/doom64/"

if [ -f "$DOOM64_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$DOOM64_SRC/build-linux"
    cmake -S "$DOOM64_SRC" -B "$DOOM64_SRC/build-linux" -DCMAKE_BUILD_TYPE=Release
    cmake --build "$DOOM64_SRC/build-linux" -- -j"$(nproc 2>/dev/null || echo 4)"
fi

echo "[ODoom64] Done. Requires doom64.wad."
[ "${1:-}" != "batch" ] && read -r -p "Press Enter to continue..."
