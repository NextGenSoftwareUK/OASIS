#!/usr/bin/env bash
# ODuke3D - install the OASIS integration (OGLib/oglib_game.h, the ODOOM/OQuake pattern)
# into EDuke32 and build with: make OASIS_STAR_API=1
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
EDUKE32_SRC="${EDUKE32_SRC:-$HOME/Source/ODuke3D}"
STAR_SRC="$OMNIVERSE_ROOT/OGEngineClient"
OGLIB_SRC="$OMNIVERSE_ROOT/OGLib"
DEST="$EDUKE32_SRC/source/duke3d/src"

if [[ ! -f "$DEST/game.cpp" ]]; then
  echo "ERROR: ODuke3D source (EDuke32 fork) not found at $EDUKE32_SRC (set EDUKE32_SRC)"
  exit 1
fi

echo "[1/2] Installing integration files into $DEST ..."
mkdir -p "$DEST/oasis"
cp -f "$SCRIPT_DIR/oduke3d_ogengine_integration.h" "$SCRIPT_DIR/oduke3d_ogengine_integration.cpp" "$DEST/"
for f in ogengine.h ogengine_sync.h ogengine_sync.c; do cp -f "$STAR_SRC/$f" "$DEST/oasis/"; done
for f in oglib_game.h oglib_config.h oglib_edge.h oglib_json.h oglib_str.h; do cp -f "$OGLIB_SRC/$f" "$DEST/oasis/"; done

echo "[2/2] Building EDuke32 with OASIS_STAR_API=1 ..."
make -C "$EDUKE32_SRC" -j"$(nproc 2>/dev/null || sysctl -n hw.logicalcpu)" OASIS_STAR_API=1 "OGENGINE_DIR=$STAR_SRC"
echo "Done: $EDUKE32_SRC/eduke32   (in the game console: star beamin <user> <pass>)"
