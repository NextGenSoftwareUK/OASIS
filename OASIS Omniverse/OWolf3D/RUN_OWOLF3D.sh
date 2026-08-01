#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ECWOLF_SRC="${OWOLF3D_SRC:-${HOME}/Source/OWolf3D}"
WOLF3D_DATA="${WOLF3D_DATA:-${HOME}/Wolf3D}"
EXE="$ECWOLF_SRC/build-linux/ecwolf"

if [ ! -f "$EXE" ]; then
    echo "ecwolf not found. Building first..."
    "$SCRIPT_DIR/BUILD_OWOLF3D.sh"
fi

echo "Starting OWolf3D..."
echo "Gamedata: $WOLF3D_DATA"
"$EXE" --data "$WOLF3D_DATA"
