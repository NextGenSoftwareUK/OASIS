#!/usr/bin/env bash
# BUILD_ODUKE3D.sh — Build ODuke3D (EDuke32 fork) with OASIS STAR integration
#
# Usage:
#   ./BUILD_ODUKE3D.sh           — interactive build
#   ./BUILD_ODUKE3D.sh batch     — non-interactive (used by BUILD_EVERYTHING.sh)
#
# Prerequisites:
#   - gcc, make (GNU Make), and build-essential installed
#   - EDUKE32_SRC set (default: $HOME/Source/ODuke3D — EDuke32 fork)
#   - OGEngineClient built (star_api.so / ogengine.h in OGEngineClient/)

set -e

BATCH="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"

# ODuke3D is a fork of EDuke32; default path after folder setup
EDUKE32_SRC="${EDUKE32_SRC:-$HOME/Source/ODuke3D}"

if [[ ! -d "$EDUKE32_SRC" ]]; then
    echo "ERROR: ODuke3D source (EDuke32 fork) not found: $EDUKE32_SRC"
    echo "  Clone ODuke3D (fork of EDuke32) to \$HOME/Source/ODuke3D"
    echo "  or set EDUKE32_SRC to the correct path."
    exit 1
fi

STAR_SRC="$OMNIVERSE_ROOT/OGEngineClient"
OGLIB_SRC="$OMNIVERSE_ROOT/OGLib"
DEST="$EDUKE32_SRC/source/duke3d/src"

echo "[ODuke3D] Source root : $SCRIPT_DIR"
echo "[ODuke3D] EDuke32 src : $EDUKE32_SRC"
echo "[ODuke3D] Destination : $DEST"

# 1. Copy integration files
echo ""
echo "[1/3] Copying integration source files..."
cp -f "$SCRIPT_DIR/oduke3d_ogengine_integration.h" "$DEST/"
cp -f "$SCRIPT_DIR/oduke3d_ogengine_integration.c" "$DEST/"
echo "  Copied: oduke3d_ogengine_integration.h / .c"

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

# 3. Copy STAR API headers/library
echo ""
echo "[3/3] Copying STAR API files..."
for f in ogengine.h ogengine_sync.h ogengine_sync.c; do
    if [[ -f "$STAR_SRC/$f" ]]; then
        cp -f "$STAR_SRC/$f" "$DEST/"
        echo "  Copied: $f"
    fi
done

# Build via GNU Make (EDuke32 standard build system)
echo ""
echo "[BUILD] Building EDuke32 with ODuke3D integration..."
if command -v make &>/dev/null; then
    make -C "$EDUKE32_SRC/source/duke3d" EDUKE32_STANDALONE=1
else
    echo "WARNING: 'make' not found. Install GNU Make and GCC or build manually."
    echo "  cd \"$EDUKE32_SRC/source/duke3d\" && make EDUKE32_STANDALONE=1"
fi

echo ""
echo "[ODuke3D] Build complete."
if [[ "$BATCH" != "batch" ]]; then
    echo "Press Enter to exit."
    read -r
fi
