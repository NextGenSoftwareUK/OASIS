#!/usr/bin/env bash
# Launch OShadowWarriorRT: Raze (OShadowWarrior-RT) + OASIS. Raze's startup picker selects Shadow Warrior (RT).
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RAZE_SRC="${RAZE_SRC:-$HOME/Source/OShadowWarrior-RT}"
EXE="$RAZE_SRC/build/raze"
[[ -x "$EXE" ]] || bash "$HERE/BUILD_OSHADOWWARRIORRT.sh"
cd "$RAZE_SRC/build"
exec "$EXE" "$@"
