#!/usr/bin/env bash
# Launch ODuke3D-RT: Raze (ODuke3D-RT) + OASIS. Raze's startup picker selects Duke Nukem 3D (RT).
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RAZE_SRC="${RAZE_SRC:-$HOME/Source/ODuke3D-RT}"
EXE="$RAZE_SRC/build/raze"
[[ -x "$EXE" ]] || bash "$HERE/BUILD_ODUKE3DRT.sh"
cd "$RAZE_SRC/build"
exec "$EXE" "$@"
