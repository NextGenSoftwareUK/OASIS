#!/usr/bin/env bash
# Launch OStrife: the ODOOM build with the Strife IWAD (strife1.wad).
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IWAD="${1:-strife1.wad}"
BUILD_DIR="$HERE/../ODOOM/build"
EXE="$BUILD_DIR/ODOOM"
[[ -x "$EXE" ]] || bash "$HERE/BUILD_OSTRIFE.sh"
cd "$BUILD_DIR"
exec "$EXE" -iwad "$IWAD"
