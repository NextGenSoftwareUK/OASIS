#!/usr/bin/env bash
# Launch OHeretic: the ODOOM build with the Heretic IWAD (heretic.wad).
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IWAD="${1:-heretic.wad}"
BUILD_DIR="$HERE/../ODOOM/build"
EXE="$BUILD_DIR/ODOOM"
[[ -x "$EXE" ]] || bash "$HERE/BUILD_OHERETIC.sh"
cd "$BUILD_DIR"
exec "$EXE" -iwad "$IWAD"
