#!/usr/bin/env bash
# OQuake2 - Yamagi Quake II + OASIS STAR API. Cross-platform (Linux, macOS) build.
# Credit: Yamagi Quake II team (GPL-2.0). See Docs/INTEGRATION_INSTRUCTIONS.md.
# Usage: ./BUILD_OQUAKE2.sh [ run ] [ batch ]
#   (none) = incremental: deploy STAR API, copy integration, build, package
#   run    = build then launch OQuake2
#   batch  = build only, no launch, no prompt

set -e

# OASIS: pause before exit when run from GUI (CI: OASIS_SCRIPT_NO_PAUSE=1)
if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" != "1" ]]; then
  _OASIS_TD="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  while [[ "$_OASIS_TD" != "/" ]]; do
    if [[ -f "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh" ]]; then
      source "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh"
      break
    fi
    _OASIS_TD="$(dirname "$_OASIS_TD")"
  done
fi

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/.." && pwd)"
OGENGINECLIENT="$OMNIVERSE/OGEngineClient"
OQUAKE2_INTEGRATION="$HERE"
OQUAKE2_CODE="$HERE/Code"

# Default paths
if [[ "$(uname -s)" == "Darwin" ]]; then
  YQUAKE2_BASEDIR_DEFAULT="$HOME/Library/Application Support/Steam/steamapps/common/Quake 2"
else
  YQUAKE2_BASEDIR_DEFAULT="$HOME/.steam/steam/steamapps/common/Quake 2"
fi
YQUAKE2_SRC="${YQUAKE2_SRC:-$HOME/Source/yquake2}"
YQUAKE2_BASEDIR="${YQUAKE2_BASEDIR:-$YQUAKE2_BASEDIR_DEFAULT}"

DO_FULL_CLEAN=0
RUN_AFTER_BUILD=0
BATCH_MODE=0
BUILD_STAR_CLIENT=0
for arg in "$@"; do
  [[ "$arg" == "run" ]] && RUN_AFTER_BUILD=1
  [[ "$arg" == "batch" ]] && BATCH_MODE=1
done

if [[ $RUN_AFTER_BUILD -eq 0 && $BATCH_MODE -eq 0 ]]; then
  echo ""
  read -p "  Full clean/rebuild [c] or incremental build [i]? [i]: " BUILD_CHOICE
  BUILD_CHOICE="${BUILD_CHOICE:-i}"
  if [[ "${BUILD_CHOICE,,}" == "c" ]]; then
    DO_FULL_CLEAN=1
    BUILD_STAR_CLIENT=1
  fi
fi

# Banner
if [[ -f "$OMNIVERSE/run_oasis_header.sh" ]]; then
  bash "$OMNIVERSE/run_oasis_header.sh" OQUAKE2 || true
fi

echo "[OQuake2] Checking OGEngineClient - build if changed, deploy..."
DEPLOY_SCRIPT="$OMNIVERSE/OGEngineClient/Scripts/build-and-deploy-star-api-unix.sh"
[[ ! -f "$DEPLOY_SCRIPT" ]] && DEPLOY_SCRIPT="$OMNIVERSE/OGEngineClient/Scripts/build-and-deploy-star-api-linux.sh"
if [[ "$BUILD_STAR_CLIENT" == "1" ]]; then
  bash "$DEPLOY_SCRIPT" -ForceBuild
else
  bash "$DEPLOY_SCRIPT"
fi

# STAR API: Linux .so, macOS .dylib
STAR_SO=""
case "$(uname -s)" in
  Darwin) [[ "$(uname -m)" == "arm64" ]] && RID="osx-arm64" || RID="osx-x64" ;;
  *)      RID="linux-x64" ;;
esac
for name in libstar_api.so star_api.so libstar_api.dylib star_api.dylib; do
  for dir in \
    "$OQUAKE2_CODE" \
    "$OQUAKE2_INTEGRATION" \
    "$OGENGINECLIENT/bin/Release/net9.0/$RID/publish" \
    "$OGENGINECLIENT/bin/Release/net10.0/$RID/publish"; do
    if [[ -f "$dir/$name" ]]; then
      STAR_SO="$dir/$name"
      break 2
    fi
  done
done
if [[ -z "$STAR_SO" || ! -f "$STAR_SO" ]]; then
  echo "ERROR: STAR API native library missing after deploy. Check OGEngineClient build."
  exit 1
fi

if [[ ! -f "$OGENGINECLIENT/ogengine.h" ]]; then
  echo "ERROR: ogengine.h not found: $OGENGINECLIENT"
  exit 1
fi

# Copy shared headers into integration Code folder
mkdir -p "$OQUAKE2_CODE"
cp -f "$OGENGINECLIENT/ogengine.h" "$OQUAKE2_CODE/"
[[ -f "$OGENGINECLIENT/ogengine_sync.h" ]] && cp -f "$OGENGINECLIENT/ogengine_sync.h" "$OQUAKE2_CODE/"

# Require Yamagi Q2 source
if [[ -z "$YQUAKE2_SRC" || ! -d "$YQUAKE2_SRC" || ! -f "$YQUAKE2_SRC/src/client/cl_main.c" ]]; then
  echo "ERROR: Yamagi Q2 source required. Set YQUAKE2_SRC (e.g. \$HOME/Source/yquake2)."
  echo "  Clone from: https://github.com/yquake2/yquake2"
  exit 1
fi

echo ""
echo "[OQuake2] Copying integration files into Yamagi Q2 source..."
mkdir -p "$YQUAKE2_SRC/src/game"
cp -f "$OQUAKE2_CODE/oquake2_ogengine_integration.c" "$YQUAKE2_SRC/src/game/"
cp -f "$OQUAKE2_CODE/oquake2_ogengine_integration.h" "$YQUAKE2_SRC/src/game/"
cp -f "$OGENGINECLIENT/ogengine.h" "$YQUAKE2_SRC/src/game/"
[[ -f "$OQUAKE2_CODE/ogengine_sync.h" ]] && cp -f "$OQUAKE2_CODE/ogengine_sync.h" "$YQUAKE2_SRC/src/game/"
cp -f "$STAR_SO" "$YQUAKE2_SRC/"
echo "  Copied to: $YQUAKE2_SRC/src/game/"

# Build
QUAKE2_ENGINE_EXE=""
if [[ "$DO_FULL_CLEAN" == "1" ]]; then
  echo "[OQuake2] Full clean..."
  rm -rf "$YQUAKE2_SRC/build"
fi

echo ""
echo "[OQuake2] Building engine..."
if [[ -f "$YQUAKE2_SRC/CMakeLists.txt" ]]; then
  if command -v cmake &>/dev/null; then
    mkdir -p "$YQUAKE2_SRC/build"
    cd "$YQUAKE2_SRC/build"
    cmake .. -DCMAKE_BUILD_TYPE=Release
    cmake --build . -- -j$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)
    cd "$HERE"
    [[ -f "$YQUAKE2_SRC/build/quake2" ]] && QUAKE2_ENGINE_EXE="$YQUAKE2_SRC/build/quake2"
    [[ -f "$YQUAKE2_SRC/build/yquake2" ]] && QUAKE2_ENGINE_EXE="$YQUAKE2_SRC/build/yquake2"
  else
    echo "[OQuake2][WARN] cmake not found. Install with: sudo apt install cmake (or equivalent)."
  fi
elif [[ -f "$YQUAKE2_SRC/Makefile" ]]; then
  cd "$YQUAKE2_SRC"
  make -j$(nproc 2>/dev/null || echo 4)
  cd "$HERE"
  [[ -f "$YQUAKE2_SRC/quake2" ]] && QUAKE2_ENGINE_EXE="$YQUAKE2_SRC/quake2"
else
  echo "[OQuake2][WARN] No CMakeLists.txt or Makefile found in $YQUAKE2_SRC"
fi

# Copy to OQuake2/build
if [[ -n "$QUAKE2_ENGINE_EXE" && -f "$QUAKE2_ENGINE_EXE" ]]; then
  echo ""
  echo "[OQuake2] Copying files to build folder..."
  mkdir -p "$OQUAKE2_INTEGRATION/build"
  cp -f "$QUAKE2_ENGINE_EXE" "$OQUAKE2_INTEGRATION/build/OQUAKE2"
  chmod +x "$OQUAKE2_INTEGRATION/build/OQUAKE2"
  cp -f "$STAR_SO" "$OQUAKE2_INTEGRATION/build/"
  # Copy oasisstar.json if not already there
  [[ ! -f "$OQUAKE2_INTEGRATION/build/oasisstar.json" ]] && \
    cp -f "$OQUAKE2_INTEGRATION/oasisstar.json" "$OQUAKE2_INTEGRATION/build/"
  echo "  Output: $OQUAKE2_INTEGRATION/build/OQUAKE2"
fi

echo ""
echo "---"
if [[ -n "$QUAKE2_ENGINE_EXE" ]]; then
  echo "OQuake2 ready. Use ./BUILD_OQUAKE2.sh run to launch."
  echo "Game data: baseq2 with pak0.pak in -datadir (e.g. $YQUAKE2_BASEDIR)."
else
  echo "To build engine: set YQUAKE2_SRC (e.g. \$HOME/Source/yquake2) and ensure cmake/make are installed."
  echo "See Docs/INTEGRATION_INSTRUCTIONS.md for setup steps."
fi
echo "OASIS thing type range: 6000-6899. Portal thing type: 5900."
echo "Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or OGENGINE_KEY / STAR_AVATAR_ID"
echo "---"

if [[ $RUN_AFTER_BUILD -eq 1 ]] && [[ -x "$OQUAKE2_INTEGRATION/build/OQUAKE2" ]]; then
  echo "Launching OQuake2..."
  cd "$OQUAKE2_INTEGRATION/build"
  exec ./OQUAKE2 +set game baseq2
fi
