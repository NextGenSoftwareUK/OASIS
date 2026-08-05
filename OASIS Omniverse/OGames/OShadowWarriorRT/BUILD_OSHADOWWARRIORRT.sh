#!/usr/bin/env bash
# OShadowWarriorRT — Duke-RT (Raze fork, Vulkan RT) + OASIS STAR API
# Usage: ./BUILD_OSHADOWWARRIORRT.sh [ batch ]
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
DUKERT_SRC="${DUKERT_SRC:-$HOME/Source/OShadowWarriorRT}"
OGENGINECLIENT="$HERE/../../OGEngineClient"

[ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ] && bash "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true

if [ ! -d "$DUKERT_SRC/source/sw/src" ]; then
    echo "[OShadowWarriorRT] Duke-RT source not found at: $DUKERT_SRC"; exit 1
fi

cp -f "$HERE/osw_rt_ogengine_integration.h"   "$DUKERT_SRC/source/sw/src/"
cp -f "$HERE/osw_rt_ogengine_integration.cpp" "$DUKERT_SRC/source/sw/src/"
cp -f "$OGENGINECLIENT/ogengine.h"            "$DUKERT_SRC/source/sw/src/"

if [ -f "$DUKERT_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$DUKERT_SRC/build-linux"
    cmake -S "$DUKERT_SRC" -B "$DUKERT_SRC/build-linux" -DCMAKE_BUILD_TYPE=Release -DOASIS_STAR_SYNC_IN_CLIENT=1
    cmake --build "$DUKERT_SRC/build-linux" -- -j"$(nproc 2>/dev/null || echo 4)"
fi

echo "[OShadowWarriorRT] Done."
[ "${1:-}" != "batch" ] && read -r -p "Press Enter to continue..."
