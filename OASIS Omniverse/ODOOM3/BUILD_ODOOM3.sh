#!/usr/bin/env bash
# BUILD_ODOOM3.sh — Build ODOOM3 (dhewm3) on Linux/macOS
#
# Usage:
#   ./BUILD_ODOOM3.sh          — interactive build
#   ./BUILD_ODOOM3.sh batch    — non-interactive
#
# Prerequisites:
#   - GCC/Clang + CMake 3.15+ + SDL2
#   - $DHEWM3_SRC or /opt/ODOOM3 checked out from dhewm3
#   - OGEngineClient built (star_api.so in OGEngineClient/)

set -euo pipefail

BATCH="${1:-}"
BUILD_TYPE="Release"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OMNIVERSE_ROOT="$SCRIPT_DIR"
DHEWM3_SRC="${DHEWM3_SRC:-/opt/ODOOM3}"
OGLIB_SRC="$OMNIVERSE_ROOT/../OGLib"
STAR_SRC="$OMNIVERSE_ROOT/../OGEngineClient"
DEST="$DHEWM3_SRC/neo/game"
BUILD_DIR="$DHEWM3_SRC/build-linux"

echo ""
echo "======================================================="
echo " ODOOM3 - OASIS STAR Integration Build (dhewm3 Linux/Mac)"
echo "======================================================="
echo ""

# -------------------------------------------------------
# 1. Copy integration files
# -------------------------------------------------------
echo "[1/4] Copying integration source..."
cp -v "$OMNIVERSE_ROOT/d3doom3_ogengine_integration.h"   "$DEST/"
cp -v "$OMNIVERSE_ROOT/d3doom3_ogengine_integration.cpp" "$DEST/"

# -------------------------------------------------------
# 2. Copy OGLib headers
# -------------------------------------------------------
echo ""
echo "[2/4] Copying OGLib headers..."
mkdir -p "$DEST/OGLib"
for f in oglib.h oglib_str.h oglib_json.h oglib_crossgame.h \
          oglib_monster.h oglib_session.h oglib_config.h oglib_beamin.h; do
    [ -f "$OGLIB_SRC/$f" ] && cp -v "$OGLIB_SRC/$f" "$DEST/OGLib/"
done

# -------------------------------------------------------
# 3. Copy STAR API files
# -------------------------------------------------------
echo ""
echo "[3/4] Copying STAR API files..."
for f in ogengine.h ogengine_sync.h; do
    [ -f "$STAR_SRC/$f" ] && cp -v "$STAR_SRC/$f" "$DEST/"
done
for f in star_api.so libstar_api.so star_api.a; do
    [ -f "$STAR_SRC/$f" ] && cp -v "$STAR_SRC/$f" "$DEST/"
done

# -------------------------------------------------------
# 4. CMake configure + build
# -------------------------------------------------------
echo ""
echo "[4/4] Building dhewm3 base.dll ($BUILD_TYPE)..."

if [ ! -d "$BUILD_DIR" ]; then
    cmake -S "$DHEWM3_SRC/neo" -B "$BUILD_DIR" \
          -DCMAKE_BUILD_TYPE=$BUILD_TYPE \
          -DOASIS_STAR_SYNC_IN_CLIENT=1
fi

cmake --build "$BUILD_DIR" --config $BUILD_TYPE --target base -- -j"$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)"

# Deploy star_api library and oasisstar.json
EXE_DIR="$BUILD_DIR"
for f in star_api.so libstar_api.so; do
    [ -f "$DEST/$f" ] && cp -v "$DEST/$f" "$EXE_DIR/"
done
[ ! -f "$EXE_DIR/oasisstar.json" ] && cp -v "$OMNIVERSE_ROOT/oasisstar.json" "$EXE_DIR/"

echo ""
echo "[ODOOM3] Build complete."
