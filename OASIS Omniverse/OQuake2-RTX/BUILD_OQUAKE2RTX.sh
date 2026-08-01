#!/usr/bin/env bash
# OQuake2-RTX — NVIDIA Q2 RTX + OASIS STAR API
# Base: Q2 RTX (https://github.com/NVIDIA/Q2RTX) — Yamagi Q2 + Vulkan RTX renderer
# OASIS thing type range: 6000-6899 (same as OQuake2 — shared game content)
# Usage: ./BUILD_OQUAKE2RTX.sh [ run | batch ]
set -e

Q2RTX_SRC="${Q2RTX_SRC:-$HOME/src/Q2RTX}"
HERE="$(cd "$(dirname "$0")" && pwd)"
OGENGINECLIENT="$HERE/../OGEngineClient"
OQUAKE2RTX_CODE="$HERE/Code"
BUILD_STAR_CLIENT=0

pause_on_exit() {
    if [ "$1" != "batch" ]; then
        echo "Press Enter to continue..."
        read -r _
    fi
}

# Deploy OGEngineClient if available
if [ -f "$HERE/../BUILD_AND_DEPLOY_STAR_CLIENT.bat" ]; then
    echo "[OQuake2-RTX] BUILD_AND_DEPLOY_STAR_CLIENT.sh not yet implemented on this platform."
    echo "[OQuake2-RTX] Skipping OGEngineClient build — using existing libstar_api.so/.dylib if present."
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
    echo "[OQuake2-RTX] libstar_api.so/.dylib not found. Build OGEngineClient first."
    pause_on_exit "$1"
    exit 1
fi

# Check Q2 RTX source
if [ ! -f "$Q2RTX_SRC/src/client/cl_main.c" ]; then
    echo "[OQuake2-RTX] Q2 RTX source not found at: $Q2RTX_SRC"
    echo "Set Q2RTX_SRC environment variable or edit this script."
    echo "Download from: https://github.com/NVIDIA/Q2RTX"
    pause_on_exit "$1"
    exit 1
fi

# Copy shared headers
mkdir -p "$OQUAKE2RTX_CODE"
if [ -f "$OGENGINECLIENT/ogengine.h" ]; then
    cp -f "$OGENGINECLIENT/ogengine.h" "$OQUAKE2RTX_CODE/"
fi
if [ -f "$OGENGINECLIENT/ogengine_sync.h" ]; then
    cp -f "$OGENGINECLIENT/ogengine_sync.h" "$OQUAKE2RTX_CODE/"
fi

# Copy integration files into Q2 RTX source
echo "[OQuake2-RTX] Copying integration files into Q2 RTX source..."
mkdir -p "$Q2RTX_SRC/src/game"
cp -f "$OQUAKE2RTX_CODE/oquake2rtx_ogengine_integration.c" "$Q2RTX_SRC/src/game/"
cp -f "$OQUAKE2RTX_CODE/oquake2rtx_ogengine_integration.h" "$Q2RTX_SRC/src/game/"
cp -f "$OGENGINECLIENT/ogengine.h" "$Q2RTX_SRC/src/game/"
[ -f "$OQUAKE2RTX_CODE/ogengine_sync.h" ] && cp -f "$OQUAKE2RTX_CODE/ogengine_sync.h" "$Q2RTX_SRC/src/game/"
cp -f "$STAR_SO" "$Q2RTX_SRC/"
echo "  Copied to: $Q2RTX_SRC/src/game/"

# Build
echo "[OQuake2-RTX] Building Q2 RTX (requires Vulkan SDK + NVIDIA RTX GPU or RTX fallback)..."
Q2RTX_ENGINE_BIN=""
if [ -f "$Q2RTX_SRC/CMakeLists.txt" ] && command -v cmake >/dev/null 2>&1; then
    mkdir -p "$Q2RTX_SRC/build"
    (cd "$Q2RTX_SRC/build" && cmake .. -DCMAKE_BUILD_TYPE=Release && cmake --build . -- -j"$(nproc 2>/dev/null || echo 4)")
    if [ -f "$Q2RTX_SRC/build/q2rtx" ]; then
        Q2RTX_ENGINE_BIN="$Q2RTX_SRC/build/q2rtx"
    fi
elif [ -f "$Q2RTX_SRC/Makefile" ]; then
    (cd "$Q2RTX_SRC" && make -j"$(nproc 2>/dev/null || echo 4)")
    [ -f "$Q2RTX_SRC/q2rtx" ] && Q2RTX_ENGINE_BIN="$Q2RTX_SRC/q2rtx"
else
    echo "[OQuake2-RTX] No CMakeLists.txt or Makefile found in $Q2RTX_SRC"
fi

# Copy output
if [ -n "$Q2RTX_ENGINE_BIN" ] && [ -f "$Q2RTX_ENGINE_BIN" ]; then
    mkdir -p "$HERE/build"
    cp -f "$Q2RTX_ENGINE_BIN" "$HERE/build/OQUAKE2RTX"
    cp -f "$STAR_SO" "$HERE/build/"
    echo "[OQuake2-RTX] Output: $HERE/build/OQUAKE2RTX"
fi

echo ""
echo "---"
if [ -n "$Q2RTX_ENGINE_BIN" ]; then
    echo "OQuake2-RTX ready."
    echo "Note: Q2 RTX requires Vulkan + RTX GPU (or software RTX fallback)."
    echo "Game data: baseq2/pak0.pak in exe folder or via -datadir."
else
    echo "Engine not yet built. Set Q2RTX_SRC and run again."
    echo "See Docs/INTEGRATION_INSTRUCTIONS.md for setup steps."
fi
echo "OASIS thing type range: 6000-6899 (same as OQuake2). Portal: 5900."
echo "Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or OGENGINE_KEY / STAR_AVATAR_ID"
echo "---"

if [ "$1" = "run" ] && [ -n "$Q2RTX_ENGINE_BIN" ] && [ -f "$Q2RTX_ENGINE_BIN" ]; then
    echo "Launching OQuake2-RTX..."
    "$HERE/build/OQUAKE2RTX" &
fi

pause_on_exit "$1"
