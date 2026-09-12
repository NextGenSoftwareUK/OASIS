#!/usr/bin/env bash
# OHexenII — uhexen2 (Hammer of Thyrion) + OASIS STAR API
# C codebase. 4 player classes: Paladin, Crusader, Necromancer, Assassin.
# Usage: ./BUILD_OHEXEN2.sh [ batch ]
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
UHEXEN2_SRC="${UHEXEN2_SRC:-$HOME/Source/OHexenII}"
OGENGINECLIENT="$HERE/../../OGEngineClient"

[ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ] && bash "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true

if [ ! -d "$UHEXEN2_SRC/engine" ]; then
    echo "[OHexenII] uhexen2 source not found at: $UHEXEN2_SRC"
    echo "Clone from https://sourceforge.net/p/uhexen2 or set UHEXEN2_SRC."
    exit 1
fi

echo "[OHexenII] Copying integration files..."
cp -f "$HERE/ohexen2_ogengine_integration.h" "$UHEXEN2_SRC/engine/h2/"
cp -f "$HERE/ohexen2_ogengine_integration.c" "$UHEXEN2_SRC/engine/h2/"
cp -f "$OGENGINECLIENT/ogengine.h"           "$UHEXEN2_SRC/engine/h2/"

if [ -f "$UHEXEN2_SRC/engine/h2/Makefile" ] && command -v make >/dev/null 2>&1; then
    make -C "$UHEXEN2_SRC/engine/h2" -j"$(nproc 2>/dev/null || echo 4)"
else
    echo "[OHexenII] Build manually: cd $UHEXEN2_SRC/engine/h2 && make"
fi

echo "[OHexenII] Done. 4 player classes: Paladin, Crusader, Necromancer, Assassin."
[ "${1:-}" != "batch" ] && read -r -p "Press Enter to continue..."
