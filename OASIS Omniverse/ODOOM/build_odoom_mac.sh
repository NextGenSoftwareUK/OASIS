#!/bin/bash
# build_odoom_mac.sh - Build and set up ODOOM on macOS
#
# Clones UZDoom, installs Homebrew deps, builds, and wires in OASIS mods.
# Run from the OASIS Omniverse/ODOOM/ folder.
#
# Requirements: macOS, Homebrew (https://brew.sh), Xcode CLI tools
#
# Usage: ./build_odoom_mac.sh [--source-dir /path/to/source]

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SOURCE_DIR="${SOURCE_DIR:-$HOME/Source}"
UZDOOM_SRC="$SOURCE_DIR/UZDoom"
BUILD_DIR="$UZDOOM_SRC/build"
FREEDOOM_URL="https://github.com/freedoom/freedoom/releases/download/v0.13.0/freedoom-0.13.0.zip"

echo "============================================"
echo " ODOOM macOS Build - OASIS Omniverse"
echo "============================================"
echo "Source dir: $UZDOOM_SRC"
echo "Build dir:  $BUILD_DIR"
echo ""

# Parse args
while [[ "$1" == --* ]]; do
    case "$1" in
        --source-dir) SOURCE_DIR="$2"; shift 2;;
        *) echo "Unknown arg: $1"; exit 1;;
    esac
done

# 1. Homebrew deps
echo "[1/5] Installing Homebrew dependencies..."
brew install cmake ninja sdl2 openal-soft libvpx fluid-synth molten-vk vulkan-volk \
             vulkan-headers glslang spirv-tools meson pkgconf 2>/dev/null || true
echo "     Done."

# 2. Clone UZDoom if not present
echo "[2/5] Cloning UZDoom..."
mkdir -p "$SOURCE_DIR"
if [ ! -d "$UZDOOM_SRC/.git" ]; then
    git clone --depth=1 https://github.com/UZDoom/UZDoom.git "$UZDOOM_SRC"
else
    echo "     Already cloned, pulling latest..."
    git -C "$UZDOOM_SRC" pull --ff-only || true
fi

# 3. Configure with CMake
echo "[3/5] Configuring with CMake..."
mkdir -p "$BUILD_DIR"
cd "$BUILD_DIR"
cmake \
    -DCMAKE_BUILD_TYPE=RelWithDebInfo \
    -DCMAKE_EXPORT_COMPILE_COMMANDS=ON \
    -DBUILD_SHARED_LIBS=OFF \
    -DOPENAL_INCLUDE_DIR="$(brew --prefix openal-soft)/include/AL" \
    -DOPENAL_LIBRARY="$(brew --prefix openal-soft)/lib/libopenal.dylib" \
    -DVPX_INCLUDE_DIR="$(brew --prefix libvpx)/include" \
    -DVPX_LIBRARIES="$(brew --prefix libvpx)/lib/libvpx.a" \
    -DDYN_OPENAL=OFF \
    -DHAVE_VULKAN=ON \
    -DHAVE_GLES2=OFF \
    -G Ninja \
    ..

# 4. Build
echo "[4/5] Building UZDoom (this takes ~15-20 min on first build)..."
cmake --build . -- -j"$(sysctl -n hw.ncpu)"
echo "     Build complete: $BUILD_DIR/uzdoom.app"

# 5. Set up OASIS mods and WAD
echo "[5/5] Setting up OASIS ODOOM mods..."

# Package the OASIS ZScript mods into a pk3
STAGING="/tmp/odoom_mod_$$"
mkdir -p "$STAGING"

cat > "$STAGING/ZSCRIPT" << 'ZEOF'
version "4.10"
#include "odoom_cvarinfo_stub.zs"
#include "odoom_oquake_keys.zs"
#include "odoom_oquake_items.zs"
#include "odoom_inventory_popup.zs"
ZEOF

printf "// CVARINFO is a separate lump\n" > "$STAGING/odoom_cvarinfo_stub.zs"
cp "$SCRIPT_DIR/odoom_oquake_keys.zs" "$STAGING/"
cp "$SCRIPT_DIR/odoom_oquake_items.zs" "$STAGING/"
cp "$SCRIPT_DIR/odoom_inventory_popup.zs" "$STAGING/"
cp "$SCRIPT_DIR/odoom_cvarinfo.txt" "$STAGING/CVARINFO"

cd "$STAGING" && zip -r "$BUILD_DIR/odoom_oasis.pk3" . > /dev/null
rm -rf "$STAGING"

# Copy face pk3
if [ -f "$SCRIPT_DIR/odoom_face.pk3" ]; then
    cp "$SCRIPT_DIR/odoom_face.pk3" "$BUILD_DIR/"
fi

# Download Freedoom if no WAD present
WAD_FOUND=false
for candidate in "$BUILD_DIR/doom2.wad" "$BUILD_DIR/doom.wad" "$BUILD_DIR/freedoom2.wad"; do
    [ -f "$candidate" ] && WAD_FOUND=true && break
done

if [ "$WAD_FOUND" = false ]; then
    echo "     No WAD found - downloading Freedoom (free DOOM replacement)..."
    TMPZIP="/tmp/freedoom_$$.zip"
    curl -L -o "$TMPZIP" "$FREEDOOM_URL"
    unzip -o "$TMPZIP" "freedoom-0.13.0/freedoom2.wad" -d /tmp/ > /dev/null
    cp "/tmp/freedoom-0.13.0/freedoom2.wad" "$BUILD_DIR/"
    rm -f "$TMPZIP"
    echo "     Freedoom2.wad installed."
fi

# Copy run script
cp "$SCRIPT_DIR/run_odoom_mac.sh" "$BUILD_DIR/"
chmod +x "$BUILD_DIR/run_odoom_mac.sh"

echo ""
echo "============================================"
echo " ODOOM build complete!"
echo "============================================"
echo ""
echo " Run: $BUILD_DIR/run_odoom_mac.sh"
echo "   or: open $BUILD_DIR/uzdoom.app"
echo ""
echo " To use your own DOOM WAD:"
echo "   cp /path/to/doom2.wad $BUILD_DIR/"
echo ""
echo " OASIS credentials (optional):"
echo "   export STAR_USERNAME=youruser"
echo "   export STAR_PASSWORD=yourpass"
echo "   $BUILD_DIR/run_odoom_mac.sh"
