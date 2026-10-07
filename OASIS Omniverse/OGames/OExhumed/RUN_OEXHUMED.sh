#!/usr/bin/env bash
# Launch OExhumed: Raze (OShadowWarrior) + OASIS. Raze's startup picker selects Exhumed / PowerSlave.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RAZE_SRC="${RAZE_SRC:-$HOME/Source/OShadowWarrior}"
EXE="$RAZE_SRC/build/raze"
[[ -x "$EXE" ]] || bash "$HERE/BUILD_OEXHUMED.sh"
cd "$RAZE_SRC/build"
exec "$EXE" "$@"
