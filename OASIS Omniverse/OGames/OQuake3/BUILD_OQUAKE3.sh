#!/usr/bin/env bash
# OQuake3 — Quake3e + OASIS STAR API
# Base: Quake3e (https://github.com/ec-/Quake3e) — open-source Quake III Arena engine
# OASIS thing type range: 7000-7899 (Quake III Arena)
# Usage: ./BUILD_OQUAKE3.sh [ run | batch ]
set -e

Q3E_SRC="${Q3E_SRC:-$HOME/src/Quake3e}"
HERE="$(cd "$(dirname "$0")" && pwd)"
OGENGINECLIENT="$HERE/../../OGEngineClient"
OQUAKE3_CODE="$HERE/Code"
BUILD_STAR_CLIENT=0

pause_on_exit() {
    if [ "$1" != "batch" ]; then
        echo "Press Enter to continue..."
        read -r _
    fi
}

# Deploy OGEngineClient if available
if [ -f "$HERE/../../BUILD_AND_DEPLOY_STAR_CLIENT.sh" ]; then
    echo "[OQuake3] Checking OGEngineClient..."
    bash "$HERE/../BUILD_AND_DEPLOY_STAR_CLIENT.sh" || true
fi

# Find STAR shared library
STAR_SO=""
if [ -f "$OGENGINECLIENT/bin/Release/net9.0/linux-x64/publish/libstar_api.so" ]; then
    STAR_SO="$OGENGINECLIENT/bin/Release/net9.0/linux-x64/publish/libstar_api.so"
elif [ "$(uname)" = "Darwin" ] && [ -f "$OGENGINECLIENT/bin/Release/net9.0/osx-x64/publish/libstar_api.dylib" ]; then
    STAR_SO="$OGENGINECLIENT/bin/Release/net9.0/osx-x64/publish/libstar_api.dylib"
elif [ -f "$HERE/libstar_api.so" ]; then
    STAR_SO="$HERE/libstar_api.so"
elif [ -f "$HERE/libstar_api.dylib" ]; then
    STAR_SO="$HERE/libstar_api.dylib"
fi

if [ -z "$STAR_SO" ]; then
    echo "[OQuake3] libstar_api.so/.dylib not found. Build OGEngineClient first."
    pause_on_exit "$1"
    exit 1
fi

# Check Quake3e source
if [ ! -f "$Q3E_SRC/code/game/g_main.c" ]; then
    echo "[OQuake3] Quake3e source not found at: $Q3E_SRC"
    echo "Set Q3E_SRC environment variable or edit this script."
    echo "Download from: https://github.com/ec-/Quake3e"
    pause_on_exit "$1"
    exit 1
fi

# Copy shared headers
mkdir -p "$OQUAKE3_CODE"
if [ -f "$OGENGINECLIENT/ogengine.h" ]; then
    cp -f "$OGENGINECLIENT/ogengine.h" "$OQUAKE3_CODE/"
fi
if [ -f "$OGENGINECLIENT/ogengine_sync.h" ]; then
    cp -f "$OGENGINECLIENT/ogengine_sync.h" "$OQUAKE3_CODE/"
fi

# Copy integration files into Quake3e source
echo "[OQuake3] Copying integration files into Quake3e source..."
mkdir -p "$Q3E_SRC/code/game"
cp -f "$OQUAKE3_CODE/oquake3_ogengine_integration.c" "$Q3E_SRC/code/game/"
cp -f "$OQUAKE3_CODE/oquake3_ogengine_integration.h" "$Q3E_SRC/code/game/"
cp -f "$OGENGINECLIENT/ogengine.h" "$Q3E_SRC/code/game/"
[ -f "$OQUAKE3_CODE/ogengine_sync.h" ] && cp -f "$OQUAKE3_CODE/ogengine_sync.h" "$Q3E_SRC/code/game/"
cp -f "$STAR_SO" "$Q3E_SRC/"
echo "  Copied to: $Q3E_SRC/code/game/"

# Build
echo "[OQuake3] Building Quake3e..."
Q3E_ENGINE_BIN=""
if [ -f "$Q3E_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$Q3E_SRC/build"
    (cd "$Q3E_SRC/build" && cmake .. -DCMAKE_BUILD_TYPE=Release && cmake --build . -- -j"$(nproc 2>/dev/null || echo 4)")
    if [ -f "$Q3E_SRC/build/quake3e" ]; then
        Q3E_ENGINE_BIN="$Q3E_SRC/build/quake3e"
    fi
elif [ -f "$Q3E_SRC/Makefile" ]; then
    (cd "$Q3E_SRC" && make -j"$(nproc 2>/dev/null || echo 4)")
    # Quake3e usually produces quake3e.x86_64 or quake3e
    for candidate in "$Q3E_SRC/quake3e.x86_64" "$Q3E_SRC/quake3e.aarch64" "$Q3E_SRC/quake3e"; do
        [ -f "$candidate" ] && Q3E_ENGINE_BIN="$candidate" && break
    done
else
    echo "[OQuake3] No CMakeLists.txt or Makefile found in $Q3E_SRC"
fi

# Copy output
if [ -n "$Q3E_ENGINE_BIN" ] && [ -f "$Q3E_ENGINE_BIN" ]; then
    mkdir -p "$HERE/build"
    cp -f "$Q3E_ENGINE_BIN" "$HERE/build/OQUAKE3"
    cp -f "$STAR_SO" "$HERE/build/"
    echo "[OQuake3] Output: $HERE/build/OQUAKE3"
fi

echo ""
echo "---"
if [ -n "$Q3E_ENGINE_BIN" ]; then
    echo "OQuake3 ready."
    echo "Game data: baseq3/pak0.pk3 in exe folder or via -fs_basepath."
else
    echo "Engine not yet built. Set Q3E_SRC and run again."
    echo "See Docs/INTEGRATION_INSTRUCTIONS.md for setup steps."
fi
echo "OASIS thing type range: 7000-7899 (Quake III Arena). Portal: 5900."
echo "Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or OGENGINE_KEY / STAR_AVATAR_ID"
echo "---"

if [ "$1" = "run" ] && [ -n "$Q3E_ENGINE_BIN" ] && [ -f "$Q3E_ENGINE_BIN" ]; then
    echo "Launching OQuake3..."
    "$HERE/build/OQUAKE3" &
fi

pause_on_exit "$1"
