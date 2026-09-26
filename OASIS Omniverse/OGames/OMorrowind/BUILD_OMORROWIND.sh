#!/usr/bin/env bash
# BUILD_OMORROWIND.sh — Build OMorrowind (OpenMW with OASIS STAR integration)
#
# Usage:
#   ./BUILD_OMORROWIND.sh [batch]
#
# Prerequisites:
#   - gcc/clang, CMake 3.15+, Qt 5.15+, SDL2, OpenAL, Boost
#   - OpenMW source at $HOME/Source/OMorrowind (or OMORROWIND_SRC)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OPENMW_SRC="${OMORROWIND_SRC:-$HOME/Source/OMorrowind}"
BUILD_DIR="$OPENMW_SRC/build"
BUILD_TYPE="${BUILD_TYPE:-Release}"

echo ""
echo "======================================================="
echo " OMorrowind - OASIS STAR Integration Build (OpenMW)"
echo "======================================================="
echo ""

# Copy integration files
echo "[OMorrowind] Copying OASIS integration files..."
cp -f "$SCRIPT_DIR/omorrowind_ogengine_integration.h"   "$OPENMW_SRC/apps/openmw/"
cp -f "$SCRIPT_DIR/omorrowind_ogengine_integration.cpp" "$OPENMW_SRC/apps/openmw/"
cp -f "$SCRIPT_DIR/oasisstar.json"                      "$OPENMW_SRC/"

if [ ! -f "$OPENMW_SRC/apps/openmw/ogengine.h" ]; then
    cp -f "$SCRIPT_DIR/../../OGLib/ogengine.h"      "$OPENMW_SRC/apps/openmw/"
    cp -f "$SCRIPT_DIR/../../OGLib/ogengine_sync.h" "$OPENMW_SRC/apps/openmw/"
fi

# CMake configure
mkdir -p "$BUILD_DIR"
echo "[OMorrowind] Configuring with CMake..."
cmake -S "$OPENMW_SRC" -B "$BUILD_DIR" \
    -DCMAKE_BUILD_TYPE="$BUILD_TYPE" \
    -DCMAKE_EXPORT_COMPILE_COMMANDS=ON

echo "[OMorrowind] Building..."
cmake --build "$BUILD_DIR" --parallel "$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)"

echo ""
echo "[OMorrowind] Build successful. Output: $BUILD_DIR/"
