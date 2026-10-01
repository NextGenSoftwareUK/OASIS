#!/usr/bin/env bash
# Launch OHexen: the ODOOM build with the Hexen IWAD (hexen.wad).
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IWAD="${1:-hexen.wad}"
BUILD_DIR="$HERE/../ODOOM/build"
EXE="$BUILD_DIR/ODOOM"
[[ -x "$EXE" ]] || bash "$HERE/BUILD_OHEXEN.sh"
cd "$BUILD_DIR"
exec "$EXE" -iwad "$IWAD"
