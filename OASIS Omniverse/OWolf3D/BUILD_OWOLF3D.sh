#!/usr/bin/env bash
# OWolf3D Build Script (Linux / macOS)
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ECWOLF_SRC="${OWOLF3D_SRC:-${HOME}/Source/OWolf3D}"
OASIS_DIR="${SCRIPT_DIR}"
OGLIB_DIR="${SCRIPT_DIR}/../OGLib"
STAR_DIR="${SCRIPT_DIR}/../OGEngineClient"

echo "============================================================="
echo " OWolf3D Build"
echo "============================================================="

if [ ! -d "$ECWOLF_SRC" ]; then
    echo "ERROR: ECWolf source not found at $ECWOLF_SRC"
    echo "Set OWOLF3D_SRC env var or clone OWolf3D to $ECWOLF_SRC"
    exit 1
fi

DST="$ECWOLF_SRC/src"

echo "[1/4] Copying integration files to $DST ..."
cp -v "$OASIS_DIR/owolf3d_ogengine_integration.h"   "$DST/"
cp -v "$OASIS_DIR/owolf3d_ogengine_integration.cpp" "$DST/"
cp -rv "$OGLIB_DIR/"   "$DST/OGLib/"
cp -v  "$STAR_DIR/ogengine.h"   "$DST/"
cp -v  "$STAR_DIR/ogengine_sync.h"  "$DST/"

echo "[2/4] Patching CMakeLists.txt ..."
CMAKEFILE="$ECWOLF_SRC/src/CMakeLists.txt"
if ! grep -q "owolf3d_ogengine_integration" "$CMAKEFILE"; then
    # Insert before the closing ) of initial_sources(...)
    sed -i 's/\tzstring\.cpp$/\tzstring.cpp\n\towolf3d_ogengine_integration.cpp/' "$CMAKEFILE"
    echo "  Added owolf3d_ogengine_integration.cpp to source list"
else
    echo "  Already present in CMakeLists.txt"
fi

echo "[3/4] Configuring CMake ..."
BUILD_DIR="$ECWOLF_SRC/build-linux"
mkdir -p "$BUILD_DIR"
cmake -S "$ECWOLF_SRC" -B "$BUILD_DIR" \
    -DCMAKE_BUILD_TYPE=Release \
    -DGPL=ON

echo "[4/4] Building ..."
cmake --build "$BUILD_DIR" --config Release -- -j"$(nproc 2>/dev/null || sysctl -n hw.logicalcpu)"

echo ""
echo "Build complete: $BUILD_DIR/ecwolf"
