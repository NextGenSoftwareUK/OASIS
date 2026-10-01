#!/usr/bin/env bash
# Launch OBlood: Raze (OShadowWarrior) + OASIS. Raze's startup picker selects Blood.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RAZE_SRC="${RAZE_SRC:-$HOME/Source/OShadowWarrior}"
EXE="$RAZE_SRC/build/raze"
[[ -x "$EXE" ]] || bash "$HERE/BUILD_OBLOOD.sh"
cd "$RAZE_SRC/build"
exec "$EXE" "$@"
