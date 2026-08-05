#!/usr/bin/env bash
# OBlood — Raze + OASIS STAR API (Blood)
# Raze covers Blood, Exhumed, and Shadow Warrior from one binary.
# Usage: ./BUILD_OBLOOD.sh [ batch ]
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
RAZE_SRC="${RAZE_SRC:-$HOME/Source/Raze}"
OGENGINECLIENT="$HERE/../../OGEngineClient"

if [ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ]; then
    bash "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true
fi

if [ ! -d "$RAZE_SRC/source/blood/src" ]; then
    echo "[OBlood] Raze source not found at: $RAZE_SRC"
    echo "Clone from https://github.com/ZDoom/Raze or set RAZE_SRC."
    exit 1
fi

echo "[OBlood] Copying integration files into Raze source..."
cp -f "$HERE/oblood_ogengine_integration.h"   "$RAZE_SRC/source/blood/src/"
cp -f "$HERE/oblood_ogengine_integration.cpp" "$RAZE_SRC/source/blood/src/"
cp -f "$OGENGINECLIENT/ogengine.h"            "$RAZE_SRC/source/blood/src/"
[ -f "$OGENGINECLIENT/ogengine_sync.h" ] && cp -f "$OGENGINECLIENT/ogengine_sync.h" "$RAZE_SRC/source/blood/src/"

echo "[OBlood] Building Raze (covers Blood, Exhumed, Shadow Warrior)..."
if [ -f "$RAZE_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$RAZE_SRC/build-linux"
    cmake -S "$RAZE_SRC" -B "$RAZE_SRC/build-linux" -DCMAKE_BUILD_TYPE=Release
    cmake --build "$RAZE_SRC/build-linux" -- -j"$(nproc 2>/dev/null || echo 4)"
else
    echo "[OBlood] No CMakeLists.txt or cmake — build Raze manually."
fi

echo ""
echo "[OBlood] Done. Raze binary runs Blood, Exhumed, and Shadow Warrior."
if [ "${1:-}" != "batch" ]; then read -r -p "Press Enter to continue..."; fi
