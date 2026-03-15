#!/usr/bin/env bash
# Run ODOOM. Builds if not already built, then launches.
# Put doom2.wad in ODOOM/build/ (or same dir as ODOOM binary).

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UZDOOM_SRC="${UZDOOM_SRC:-$HOME/Source/UZDoom}"
ODOOM_EXE=""

if [[ -x "$HERE/build/ODOOM" ]]; then
  ODOOM_EXE="$HERE/build/ODOOM"
elif [[ -f "$UZDOOM_SRC/build/uzdoom" && -x "$UZDOOM_SRC/build/uzdoom" ]]; then
  ODOOM_EXE="$UZDOOM_SRC/build/uzdoom"
fi

if [[ -z "$ODOOM_EXE" ]]; then
  echo "ODOOM not built. Building..."
  exec "$HERE/BUILD_ODOOM.sh" run
fi

echo "Launching ODOOM..."
cd "$(dirname "$ODOOM_EXE")"
exec ./"$(basename "$ODOOM_EXE")"
