#!/usr/bin/env bash
# ORtCW — iortcw (Q3-engine) + OASIS STAR API
# Same engine lineage as OQuake3 (ioquake3 → iortcw).
# Usage: ./BUILD_ORTCW.sh [ batch ]
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
IORTCW_SRC="${IORTCW_SRC:-$HOME/Source/ORtCW}"
OGENGINECLIENT="$HERE/../../OGEngineClient"

[ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ] && bash "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true

if [ ! -d "$IORTCW_SRC/SP_src" ]; then
    echo "[ORtCW] iortcw source not found at: $IORTCW_SRC"
    echo "Clone from https://github.com/iortcw/iortcw or set IORTCW_SRC."
    exit 1
fi

echo "[ORtCW] Copying integration files..."
cp -f "$HERE/ortcw_ogengine_integration.h"   "$IORTCW_SRC/SP_src/game/"
cp -f "$HERE/ortcw_ogengine_integration.c"   "$IORTCW_SRC/SP_src/game/"
cp -f "$OGENGINECLIENT/ogengine.h"           "$IORTCW_SRC/SP_src/game/"
[ -f "$OGENGINECLIENT/ogengine_sync.h" ] && cp -f "$OGENGINECLIENT/ogengine_sync.h" "$IORTCW_SRC/SP_src/game/"

if [ -f "$IORTCW_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$IORTCW_SRC/build-linux"
    cmake -S "$IORTCW_SRC" -B "$IORTCW_SRC/build-linux" -DCMAKE_BUILD_TYPE=Release
    cmake --build "$IORTCW_SRC/build-linux" -- -j"$(nproc 2>/dev/null || echo 4)"
elif [ -f "$IORTCW_SRC/Makefile" ] && command -v make >/dev/null 2>&1; then
    make -C "$IORTCW_SRC" -j"$(nproc 2>/dev/null || echo 4)"
else
    echo "[ORtCW] Build manually — see $IORTCW_SRC/README.md"
fi

echo "[ORtCW] Done. Blazkowicz reports for duty."
[ "${1:-}" != "batch" ] && read -r -p "Press Enter to continue..."
