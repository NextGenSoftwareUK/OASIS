#!/bin/bash
# Run ODOOM on macOS - launches UZDoom with OASIS STAR API mods
#
# Usage: ./run_odoom_mac.sh [extra uzdoom args]
#
# Optionally set OASIS credentials:
#   export STAR_USERNAME=yourusername
#   export STAR_PASSWORD=yourpassword
# Or use API key:
#   export STAR_API_KEY=yourkey
#   export STAR_AVATAR_ID=yourid

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
UZDOOM_APP="$SCRIPT_DIR/uzdoom.app"
UZDOOM_BIN="$UZDOOM_APP/Contents/MacOS/uzdoom"

if [ ! -f "$UZDOOM_BIN" ]; then
    echo "ERROR: uzdoom.app not found at $SCRIPT_DIR"
    echo "Build UZDoom first: cd /path/to/UZDoom/build && cmake --build ."
    exit 1
fi

# Find WAD file - prefer doom2.wad, fall back to freedoom2.wad, then freedoom1.wad
WAD=""
for candidate in "$SCRIPT_DIR/doom2.wad" "$SCRIPT_DIR/doom.wad" "$SCRIPT_DIR/freedoom2.wad" "$SCRIPT_DIR/freedoom1.wad"; do
    if [ -f "$candidate" ]; then
        WAD="$candidate"
        break
    fi
done

if [ -z "$WAD" ]; then
    echo "ERROR: No WAD file found in $SCRIPT_DIR"
    echo "Copy doom2.wad (or freedoom2.wad) to $SCRIPT_DIR/"
    exit 1
fi

WAD_NAME=$(basename "$WAD")
echo "ODOOM - OASIS Omniverse DOOM"
echo "Engine:  $UZDOOM_BIN"
echo "WAD:     $WAD_NAME"

# Load OASIS mods if present
MOD_ARGS=""
for pk3 in odoom_oasis.pk3 odoom_face.pk3; do
    if [ -f "$SCRIPT_DIR/$pk3" ]; then
        MOD_ARGS="$MOD_ARGS -file $SCRIPT_DIR/$pk3"
        echo "Mod:     $pk3"
    fi
done

echo ""

# Pass OASIS credentials via env if set
STAR_ENV_ARGS=""
[ -n "$STAR_USERNAME" ] && STAR_ENV_ARGS="$STAR_ENV_ARGS +set star_username $STAR_USERNAME"
[ -n "$STAR_PASSWORD" ] && STAR_ENV_ARGS="$STAR_ENV_ARGS +set star_password $STAR_PASSWORD"
[ -n "$STAR_API_KEY" ]  && STAR_ENV_ARGS="$STAR_ENV_ARGS +set star_api_key $STAR_API_KEY"
[ -n "$STAR_AVATAR_ID" ] && STAR_ENV_ARGS="$STAR_ENV_ARGS +set star_avatar_id $STAR_AVATAR_ID"

exec "$UZDOOM_BIN" \
    -iwad "$WAD" \
    $MOD_ARGS \
    $STAR_ENV_ARGS \
    "$@"
