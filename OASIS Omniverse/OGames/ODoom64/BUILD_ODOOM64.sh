#!/usr/bin/env bash
# ODoom64 - DOOM 64 EX+ + OASIS, the ODOOM/OQuake way via OGLib/oglib_game.h.
# Installs the integration into src/engine and builds with -DOASIS_STAR_API=ON.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/../.." && pwd)"
DOOM64_SRC="${DOOM64_SRC:-$HOME/Source/ODOOM64}"
OGENGINECLIENT="$OMNIVERSE/OGEngineClient"
OGLIB="$OMNIVERSE/OGLib"
DST="$DOOM64_SRC/src/engine"

if [[ ! -f "$DST/d_main.c" ]]; then
  echo "[ODoom64] Doom64 EX+ source not found at $DOOM64_SRC (set DOOM64_SRC)"
  exit 1
fi

echo "[ODoom64] Installing OASIS integration into $DST ..."
mkdir -p "$DST/oasis"
cp -f "$HERE/odoom64_ogengine_integration.c" "$HERE/odoom64_ogengine_integration.h" "$DST/"
for f in ogengine.h ogengine_sync.h ogengine_sync.c; do cp -f "$OGENGINECLIENT/$f" "$DST/oasis/"; done
for f in oglib_game.h oglib_config.h oglib_edge.h oglib_json.h oglib_str.h; do cp -f "$OGLIB/$f" "$DST/oasis/"; done

cmake -S "$DOOM64_SRC" -B "$DOOM64_SRC/build" -DCMAKE_BUILD_TYPE=Release -DOASIS_STAR_API=ON "-DOGENGINE_DIR=$OGENGINECLIENT"
cmake --build "$DOOM64_SRC/build" --parallel
echo "[ODoom64] Done. In the game console: star beamin <user> <pass>"
