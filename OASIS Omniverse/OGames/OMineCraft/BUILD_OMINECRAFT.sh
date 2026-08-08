#!/usr/bin/env bash
# BUILD_OMINECRAFT.sh — Install OMineCraft OASIS mod into Minetest
#
# No compilation required — Minetest loads Lua mods directly.
# This script copies the mod to the Minetest mods directory.
#
# Usage:
#   ./BUILD_OMINECRAFT.sh [batch]
#
# Prerequisites:
#   - Minetest 5.6+ installed (set MINETEST_DIR or use default paths)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Detect Minetest mods directory
if [ -n "$MINETEST_DIR" ]; then
    MOD_DEST="$MINETEST_DIR/mods/oasis"
elif [ -d "$HOME/.minetest/mods" ]; then
    MOD_DEST="$HOME/.minetest/mods/oasis"
elif [ -d "/usr/share/minetest/mods" ]; then
    MOD_DEST="/usr/share/minetest/mods/oasis"
else
    echo "[ERROR] Minetest mods directory not found."
    echo "        Set MINETEST_DIR to your Minetest installation."
    exit 1
fi

echo ""
echo "======================================================="
echo " OMineCraft - OASIS STAR API Mod Install (Minetest)"
echo "======================================================="
echo ""
echo "[OMineCraft] Installing mod to: $MOD_DEST"

mkdir -p "$MOD_DEST"
for f in init.lua api.lua portals.lua hud.lua mod.conf oasisstar.json; do
    cp -f "$SCRIPT_DIR/$f" "$MOD_DEST/$f"
done

echo ""
echo "[OMineCraft] Mod installed."
echo ""
echo "Next steps:"
echo "  1. Open Minetest and select your world."
echo "  2. Enable the 'oasis' mod in world settings."
echo "  3. Add to minetest.conf:"
echo "       secure.http_mods = oasis"
echo "       oasis_star_url = https://star-api.oasisplatform.world/api"
echo "  4. In-game: /oasis login <username> <password>"
