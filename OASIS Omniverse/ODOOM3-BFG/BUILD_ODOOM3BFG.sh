#!/usr/bin/env bash
# BUILD_ODOOM3BFG.sh — Build ODOOM3-BFG on Linux/macOS
#
# Usage:
#   ./BUILD_ODOOM3BFG.sh          — interactive build
#   ./BUILD_ODOOM3BFG.sh batch    — non-interactive
#
# Prerequisites:
#   - GCC/Clang + CMake 3.15+
#   - C:\Source\ODOOM3-BFG (or $RBDOOM_SRC) checked out
#   - STARAPIClient built (star_api.so in STARAPIClient/)

set -euo pipefail

BATCH="${1:-}"
BUILD_TYPE="Release"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OMNIVERSE_ROOT="$SCRIPT_DIR"
RBDOOM_SRC="${RBDOOM_SRC:-/opt/ODOOM3-BFG}"
OGLIB_SRC="$OMNIVERSE_ROOT/../OGLib"
STAR_SRC="$OMNIVERSE_ROOT/../STARAPIClient"
DEST="$RBDOOM_SRC/neo/d3xp"
BUILD_DIR="$RBDOOM_SRC/build-linux"

echo ""
echo "======================================================="
echo " ODOOM3-BFG - OASIS STAR Integration Build (Linux/Mac)"
echo "======================================================="
echo ""

# -------------------------------------------------------
# 1. Copy integration files
# -------------------------------------------------------
echo "[1/4] Copying integration source..."
cp -v "$OMNIVERSE_ROOT/d3doom_star_integration.h"   "$DEST/"
cp -v "$OMNIVERSE_ROOT/d3doom_star_integration.cpp" "$DEST/"

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
for f in star_api.h star_sync.h; do
    [ -f "$STAR_SRC/$f" ] && cp -v "$STAR_SRC/$f" "$DEST/"
done
for f in star_api.so libstar_api.so star_api.a; do
    [ -f "$STAR_SRC/$f" ] && cp -v "$STAR_SRC/$f" "$DEST/"
done

# -------------------------------------------------------
# 4. CMake configure + build
# -------------------------------------------------------
echo ""
echo "[4/4] Building RBDOOM-3-BFG ($BUILD_TYPE)..."

if [ ! -d "$BUILD_DIR" ]; then
    cmake -S "$RBDOOM_SRC/neo" -B "$BUILD_DIR" \
          -DCMAKE_BUILD_TYPE=$BUILD_TYPE \
          -DOASIS_STAR_SYNC_IN_CLIENT=1
fi

cmake --build "$BUILD_DIR" --config $BUILD_TYPE --target d3game -- -j"$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)"

# Deploy star_api library and oasisstar.json next to the output
EXE_DIR="$BUILD_DIR"
for f in star_api.so libstar_api.so; do
    [ -f "$DEST/$f" ] && cp -v "$DEST/$f" "$EXE_DIR/"
done
[ ! -f "$EXE_DIR/oasisstar.json" ] && cp -v "$OMNIVERSE_ROOT/oasisstar.json" "$EXE_DIR/"

echo ""
echo "[ODOOM3-BFG] Build complete."
