#!/usr/bin/env bash
# OWolf3D - install the OASIS integration (OGLib/oglib_game.h, the ODOOM/OQuake pattern)
# into ECWolf and build it with -DOASIS_STAR_API=ON.
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$SCRIPT_DIR/../.." && pwd)"
ECWOLF_SRC="${OWOLF3D_SRC:-${HOME}/Source/OWolf3D}"
OGLIB_DIR="$OMNIVERSE/OGLib"
STAR_DIR="$OMNIVERSE/OGEngineClient"
BUILD_DIR="$ECWOLF_SRC/build"
DST="$ECWOLF_SRC/src"

if [[ ! -f "$DST/wl_main.cpp" ]]; then
  echo "ECWolf source not found at $ECWOLF_SRC (set OWOLF3D_SRC)"
  exit 1
fi

echo "[1/3] Installing integration files into $DST ..."
mkdir -p "$DST/oasis"
cp -f "$SCRIPT_DIR/owolf3d_ogengine_integration.h" "$SCRIPT_DIR/owolf3d_ogengine_integration.cpp" "$DST/"
for f in ogengine.h ogengine_sync.h ogengine_sync.c; do cp -f "$STAR_DIR/$f" "$DST/oasis/"; done
for f in oglib_game.h oglib_config.h oglib_edge.h oglib_json.h oglib_str.h; do cp -f "$OGLIB_DIR/$f" "$DST/oasis/"; done

echo "[2/3] Configuring CMake (OASIS_STAR_API=ON)..."
cmake -S "$ECWOLF_SRC" -B "$BUILD_DIR" -DCMAKE_BUILD_TYPE=Release -DGPL=ON -DOASIS_STAR_API=ON "-DOGENGINE_DIR=$STAR_DIR"

echo "[3/3] Building..."
cmake --build "$BUILD_DIR" --parallel
[[ -f "$BUILD_DIR/oasisstar.json" ]] || cp -f "$SCRIPT_DIR/oasisstar.json" "$BUILD_DIR/" 2>/dev/null || true
echo "Done: $BUILD_DIR/ecwolf   (beam in once with: ecwolf --star \"beamin <user> <pass>\")"
