#!/usr/bin/env bash
# BUILD_ODUKE3DRT.sh — Build ODuke3D-RT (Duke-RT fork) with OASIS STAR integration
#
# Usage:
#   ./BUILD_ODUKE3DRT.sh           — interactive build
#   ./BUILD_ODUKE3DRT.sh batch     — non-interactive (used by BUILD_EVERYTHING.sh)
#
# Prerequisites:
#   - gcc, cmake, ninja (or make), Vulkan SDK
#   - DUKERT_SRC set (default: $HOME/Source/ODuke3D-RT — Duke-RT fork)
#   - OGEngineClient built (star_api.so / ogengine.h in OGEngineClient/)

set -e

BATCH="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE_ROOT="$(dirname "$SCRIPT_DIR")"

DUKERT_SRC="${DUKERT_SRC:-$HOME/Source/ODuke3D-RT}"  # ODuke3D-RT is a fork of Duke-RT

if [[ ! -d "$DUKERT_SRC" ]]; then
    echo "ERROR: ODuke3D-RT source (Duke-RT fork) not found: $DUKERT_SRC"
    echo "  Clone Duke-RT to \$HOME/Source/ODuke3D-RT"
    echo "  or set DUKERT_SRC to the correct path."
    exit 1
fi

STAR_SRC="$OMNIVERSE_ROOT/OGEngineClient"
OGLIB_SRC="$OMNIVERSE_ROOT/OGLib"
DEST="$DUKERT_SRC/source/duke3d/src"
BUILD_DIR="$DUKERT_SRC/build-linux-rt"

echo "[ODuke3D-RT] Source root : $SCRIPT_DIR"
echo "[ODuke3D-RT] Duke-RT src : $DUKERT_SRC"
echo "[ODuke3D-RT] Destination : $DEST"

# 1. Copy integration files
echo ""
echo "[1/3] Copying integration source files..."
cp -f "$SCRIPT_DIR/oduke3drt_ogengine_integration.h" "$DEST/"
cp -f "$SCRIPT_DIR/oduke3drt_ogengine_integration.c" "$DEST/"
echo "  Copied: oduke3drt_ogengine_integration.h / .c"

# 2. Copy OGLib headers
echo ""
echo "[2/3] Copying OGLib headers..."
mkdir -p "$DEST/OGLib"
for f in oglib.h oglib_str.h oglib_json.h oglib_crossgame.h \
          oglib_monster.h oglib_session.h oglib_config.h oglib_beamin.h; do
    if [[ -f "$OGLIB_SRC/$f" ]]; then
        cp -f "$OGLIB_SRC/$f" "$DEST/OGLib/"
        echo "  Copied: OGLib/$f"
    fi
done

# 3. Copy STAR API
echo ""
echo "[3/3] Copying STAR API files..."
for f in ogengine.h ogengine_sync.h ogengine_sync.c; do
    if [[ -f "$STAR_SRC/$f" ]]; then
        cp -f "$STAR_SRC/$f" "$DEST/"
        echo "  Copied: $f"
    fi
done

# Build via CMake (Duke-RT uses CMake)
echo ""
echo "[BUILD] Configuring CMake..."
mkdir -p "$BUILD_DIR"
cmake -S "$DUKERT_SRC" -B "$BUILD_DIR" \
      -DCMAKE_BUILD_TYPE=Release \
      -DOASIS_STAR_SYNC_IN_CLIENT=1

echo "[BUILD] Building ODuke3D-RT..."
cmake --build "$BUILD_DIR" --config Release -j"$(nproc)"

# Deploy oasisstar.json
if [[ -f "$SCRIPT_DIR/oasisstar.json" && -d "$BUILD_DIR" ]]; then
    cp -f "$SCRIPT_DIR/oasisstar.json" "$BUILD_DIR/"
    echo "  Deployed oasisstar.json to $BUILD_DIR"
fi

echo ""
echo "[ODuke3D-RT] Build complete."
if [[ "$BATCH" != "batch" ]]; then
    echo "Press Enter to exit."
    read -r
fi
