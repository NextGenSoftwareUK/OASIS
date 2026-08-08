#!/usr/bin/env bash
# BUILD_OHALFLIFE.sh — Build OHalfLife (Xash3D FWGS + HLSDK game DLL)
#
# Prerequisites:
#   - GCC/Clang, CMake 3.16+, SDL2 dev libraries
#   - Xash3D FWGS cloned at $XASH3D_DIR (default: ~/Source/xash3d-fwgs)
#   - HLSDK-portable cloned at $HLSDK_DIR (default: ~/Source/hlsdk-portable)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"

XASH3D_DIR="${XASH3D_DIR:-$HOME/Source/xash3d-fwgs}"
HLSDK_DIR="${HLSDK_DIR:-$HOME/Source/hlsdk-portable}"
OGENGINE_LIB="${OGENGINE_LIB:-$SCRIPT_DIR/../../OGEditorSDK/build/libOGEngineClient.so}"

echo ""
echo "======================================================="
echo " OHalfLife - OASIS STAR API Integration Build"
echo "======================================================="
echo ""

# --- Copy integration files into HLSDK ---
echo "[OHalfLife] Copying integration files into HLSDK..."
cp -f "$SCRIPT_DIR/ohalflife_ogengine_integration.h"   "$HLSDK_DIR/dlls/"
cp -f "$SCRIPT_DIR/ohalflife_ogengine_integration.cpp" "$HLSDK_DIR/dlls/"
cp -f "$SCRIPT_DIR/../../OGEditorSDK/ogengine.h"       "$HLSDK_DIR/dlls/"
cp -f "$SCRIPT_DIR/../../OGEditorSDK/ogengine_sync.h"  "$HLSDK_DIR/dlls/"
cp -f "$SCRIPT_DIR/oasisstar.json"                      "$HLSDK_DIR/dlls/"

# --- Build HLSDK game DLL ---
echo "[OHalfLife] Building HLSDK game DLL..."
cmake -S "$HLSDK_DIR" -B "$HLSDK_DIR/build_hl" \
    -DCMAKE_BUILD_TYPE=RelWithDebInfo \
    -DOGENGINE_INTEGRATION=ON
cmake --build "$HLSDK_DIR/build_hl" -j"$(nproc)"

# --- Build Xash3D FWGS ---
echo "[OHalfLife] Building Xash3D FWGS engine..."
cmake -S "$XASH3D_DIR" -B "$XASH3D_DIR/build" \
    -DCMAKE_BUILD_TYPE=RelWithDebInfo \
    -DXASH_DEDICATED=OFF
cmake --build "$XASH3D_DIR/build" -j"$(nproc)"

# --- Assemble build output ---
echo "[OHalfLife] Assembling build output..."
mkdir -p "$BUILD_DIR/valve/dlls"

cp -f "$XASH3D_DIR/build/xash"                    "$BUILD_DIR/OHalfLife"
cp -f "$HLSDK_DIR/build_hl/hl.so"                 "$BUILD_DIR/valve/dlls/"
[ -f "$OGENGINE_LIB" ] && cp -f "$OGENGINE_LIB"  "$BUILD_DIR/"
cp -f "$SCRIPT_DIR/oasisstar.json"                 "$BUILD_DIR/"

chmod +x "$BUILD_DIR/OHalfLife"

echo ""
echo "[OHalfLife] Build complete."
echo "  Engine:   $BUILD_DIR/OHalfLife"
echo "  Game DLL: $BUILD_DIR/valve/dlls/hl.so"
echo ""
echo "Next steps:"
echo "  1. Copy your Half-Life 'valve' folder content into $BUILD_DIR/valve/"
echo "  2. Run: $BUILD_DIR/OHalfLife -game valve"
echo "  3. The OASIS STAR API integration auto-initialises on map load."
