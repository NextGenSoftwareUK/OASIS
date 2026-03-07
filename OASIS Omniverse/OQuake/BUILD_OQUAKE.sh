#!/usr/bin/env bash
# OQuake - vkQuake + OASIS STAR API. Cross-platform (Linux, macOS) build; equivalent of "BUILD_OQUAKE.bat" on Windows.
# Supports: Windows (use BUILD_OQUAKE.bat), Linux, macOS (use this script).
# Usage: ./BUILD_OQUAKE.sh [ run ] [ batch ]
#   (none) = incremental: deploy STAR API, copy integration, patch vkQuake, build, package
#   run    = build then launch OQuake
#   batch  = build only, no launch, no prompt

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/.." && pwd)"
STARAPICLIENT="$OMNIVERSE/STARAPIClient"
OQUAKE_INTEGRATION="$HERE"
OQUAKE_CODE="$HERE/Code"

# Default paths: Linux Steam vs macOS Steam
if [[ "$(uname -s)" == "Darwin" ]]; then
  OQUAKE_BASEDIR_DEFAULT="$HOME/Library/Application Support/Steam/steamapps/common/Quake"
else
  OQUAKE_BASEDIR_DEFAULT="$HOME/.steam/steam/steamapps/common/Quake"
fi
QUAKE_SRC="${QUAKE_SRC:-$HOME/Source/quake-rerelease-qc}"
VKQUAKE_SRC="${VKQUAKE_SRC:-$HOME/Source/vkQuake}"
OQUAKE_BASEDIR="${OQUAKE_BASEDIR:-$OQUAKE_BASEDIR_DEFAULT}"

DO_FULL_CLEAN=0
RUN_AFTER_BUILD=0
BATCH_MODE=0
for arg in "$@"; do
  [[ "$arg" == "run" ]] && RUN_AFTER_BUILD=1
  [[ "$arg" == "batch" ]] && BATCH_MODE=1
done

# Version
if [[ -f "$HERE/Scripts/generate_oquake_version.ps1" ]] && command -v pwsh &>/dev/null; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$HERE/Scripts/generate_oquake_version.ps1" -Root "$HERE" || true
fi
VERSION_DISPLAY="1.0 (Build 1)"
[[ -f "$HERE/Version/version_display.txt" ]] && VERSION_DISPLAY=$(cat "$HERE/Version/version_display.txt" | tr -d '\r')

echo ""
echo "  O A S I S   O Q U A K E  v$VERSION_DISPLAY"
echo ""

echo "[OQuake] Checking STARAPIClient - build if changed, deploy..."
if [[ -f "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh" ]]; then
  bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh"
else
  bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh"
fi

# STAR API: Linux .so, macOS .dylib
STAR_SO=""
for so in "$OQUAKE_CODE/libstar_api.so" "$OQUAKE_INTEGRATION/libstar_api.so" "$OQUAKE_CODE/libstar_api.dylib" "$OQUAKE_INTEGRATION/libstar_api.dylib"; do
  if [[ -f "$so" ]]; then
    STAR_SO="$so"
    break
  fi
done
if [[ -z "$STAR_SO" ]]; then
  for so in "$OQUAKE_INTEGRATION/star_api.so" "$OQUAKE_CODE/star_api.so" "$OQUAKE_INTEGRATION/star_api.dylib" "$OQUAKE_CODE/star_api.dylib"; do
    if [[ -f "$so" ]]; then
      STAR_SO="$so"
      break
    fi
  done
fi
if [[ -z "$STAR_SO" ]]; then
  case "$(uname -s)" in
    Darwin) [[ "$(uname -m)" == "arm64" ]] && RID="osx-arm64" || RID="osx-x64" ;;
    *)      RID="linux-x64" ;;
  esac
  for name in libstar_api.so star_api.so libstar_api.dylib star_api.dylib; do
    if [[ -f "$STARAPICLIENT/bin/Release/net8.0/$RID/publish/$name" ]]; then
      STAR_SO="$STARAPICLIENT/bin/Release/net8.0/$RID/publish/$name"
      break
    fi
  done
fi
if [[ -z "$STAR_SO" || ! -f "$STAR_SO" ]]; then
  echo "ERROR: STAR API native library (libstar_api.so / libstar_api.dylib) missing after deploy. Check STARAPIClient build."
  exit 1
fi

if [[ ! -f "$STARAPICLIENT/star_api.h" ]]; then
  echo "ERROR: star_api.h not found: $STARAPICLIENT"
  exit 1
fi

# QuakeC tree
if [[ ! -d "$QUAKE_SRC" ]]; then
  echo "ERROR: Quake source not found: $QUAKE_SRC"
  echo "  Set QUAKE_SRC or clone quake-rerelease-qc to \$HOME/Source/quake-rerelease-qc"
  exit 1
fi

# star_sync (copy if missing so local edits are not overwritten)
if [[ ! -f "$OQUAKE_CODE/star_sync.c" ]] && [[ -f "$STARAPICLIENT/star_sync.c" ]]; then
  mkdir -p "$OQUAKE_CODE"
  cp -f "$STARAPICLIENT/star_sync.c" "$OQUAKE_CODE/"
  cp -f "$STARAPICLIENT/star_sync.h" "$OQUAKE_CODE/"
fi

echo ""
echo "[OQuake] Installing integration into QuakeC tree..."
cp -f "$OQUAKE_CODE/oquake_star_integration.c" "$QUAKE_SRC/"
cp -f "$OQUAKE_CODE/oquake_star_integration.h" "$QUAKE_SRC/"
[[ -f "$OQUAKE_CODE/oquake_version.h" ]] && cp -f "$OQUAKE_CODE/oquake_version.h" "$QUAKE_SRC/"
[[ -f "$HERE/Docs/WINDOWS_INTEGRATION.md" ]] && cp -f "$HERE/Docs/WINDOWS_INTEGRATION.md" "$QUAKE_SRC/"
[[ -f "$OQUAKE_CODE/engine_oquake_hooks.c.example" ]] && cp -f "$OQUAKE_CODE/engine_oquake_hooks.c.example" "$QUAKE_SRC/"
cp -f "$STARAPICLIENT/star_api.h" "$QUAKE_SRC/"
[[ -f "$OQUAKE_CODE/star_sync.c" ]] && cp -f "$OQUAKE_CODE/star_sync.c" "$QUAKE_SRC/"
[[ -f "$OQUAKE_CODE/star_sync.h" ]] && cp -f "$OQUAKE_CODE/star_sync.h" "$QUAKE_SRC/"
cp -f "$STAR_SO" "$QUAKE_SRC/"
echo "  $QUAKE_SRC"

# vkQuake: patch and build
QUAKE_ENGINE_EXE=""
if [[ -n "$VKQUAKE_SRC" && -d "$VKQUAKE_SRC" && -f "$VKQUAKE_SRC/Quake/pr_ext.c" ]]; then
  echo ""
  echo "[OQuake] Patching vkQuake source..."
  if command -v pwsh &>/dev/null && [[ -f "$HERE/vkquake_oquake/apply_oquake_to_vkquake.ps1" ]]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$HERE/vkquake_oquake/apply_oquake_to_vkquake.ps1" -VkQuakeSrc "$VKQUAKE_SRC" -SkipQuakeInstallPrompt || true
  else
    echo "[OQuake][WARN] pwsh not found or apply script missing; copying integration files manually."
    QUAKE_DIR="$VKQUAKE_SRC/Quake"
    [[ -f "$OQUAKE_CODE/oquake_star_integration.c" ]] && cp -f "$OQUAKE_CODE/oquake_star_integration.c" "$QUAKE_DIR/"
    [[ -f "$OQUAKE_CODE/oquake_star_integration.h" ]] && cp -f "$OQUAKE_CODE/oquake_star_integration.h" "$QUAKE_DIR/"
    [[ -f "$OQUAKE_CODE/star_sync.c" ]] && cp -f "$OQUAKE_CODE/star_sync.c" "$QUAKE_DIR/"
    [[ -f "$OQUAKE_CODE/star_sync.h" ]] && cp -f "$OQUAKE_CODE/star_sync.h" "$QUAKE_DIR/"
    cp -f "$STARAPICLIENT/star_api.h" "$QUAKE_DIR/"
  fi
  # Ensure STAR API shared lib is in vkQuake/Quake (.so on Linux, .dylib on macOS; Windows uses .dll)
  cp -f "$STAR_SO" "$VKQUAKE_SRC/Quake/"

  if [[ "$DO_FULL_CLEAN" == "1" ]]; then
    echo "[OQuake] Full clean..."
    rm -rf "$VKQUAKE_SRC/Windows/VisualStudio/Build-vkQuake" "$VKQUAKE_SRC/Windows/VisualStudio/x64" "$VKQUAKE_SRC/build"
  fi

  echo ""
  echo "[OQuake] Building engine..."
  if [[ -f "$VKQUAKE_SRC/meson.build" ]]; then
    if command -v meson &>/dev/null && command -v ninja &>/dev/null; then
      cd "$VKQUAKE_SRC"
      if [[ ! -d build ]]; then
        meson setup build --buildtype=release
      fi
      ninja -C build
      cd "$HERE"
      if [[ -f "$VKQUAKE_SRC/build/vkquake" ]]; then
        QUAKE_ENGINE_EXE="$VKQUAKE_SRC/build/vkquake"
      elif [[ -f "$VKQUAKE_SRC/build/vkquake.exe" ]]; then
        QUAKE_ENGINE_EXE="$VKQUAKE_SRC/build/vkquake.exe"
      fi
    else
      echo "[OQuake][WARN] meson/ninja not found. Install with: sudo apt install meson ninja-build (or equivalent)."
    fi
  else
    echo "[OQuake][WARN] vkQuake meson.build not found at $VKQUAKE_SRC"
  fi
fi

# Copy to OQuake/build
if [[ -n "$QUAKE_ENGINE_EXE" && -f "$QUAKE_ENGINE_EXE" ]]; then
  echo ""
  echo "[OQuake] Copying files to build folder..."
  mkdir -p "$OQUAKE_INTEGRATION/build"
  cp -f "$QUAKE_ENGINE_EXE" "$OQUAKE_INTEGRATION/build/OQUAKE"
  chmod +x "$OQUAKE_INTEGRATION/build/OQUAKE"
  cp -f "$STAR_SO" "$OQUAKE_INTEGRATION/build/"
  # Copy any other shared libs from vkQuake build (e.g. Vulkan loader)
  EXE_DIR="$(dirname "$QUAKE_ENGINE_EXE")"
  for d in "$EXE_DIR"/*.so "$EXE_DIR"/*.dylib; do
    [[ -f "$d" ]] && cp -f "$d" "$OQUAKE_INTEGRATION/build/" || true
  done
  echo "  Output: $OQUAKE_INTEGRATION/build/OQUAKE"
fi

echo ""
echo "---"
if [[ -n "$QUAKE_ENGINE_EXE" ]]; then
  echo "OQuake ready. Use ./RUN_OQUAKE.sh or BUILD_OQUAKE.sh run to launch."
  echo "Game data: id1 with pak0.pak, pak1.pak in -basedir (e.g. $OQUAKE_BASEDIR)."
else
  echo "To build engine: set VKQUAKE_SRC (e.g. \$HOME/Source/vkQuake) and ensure meson/ninja are installed."
fi
echo "Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or STAR_API_KEY / STAR_AVATAR_ID"
echo "---"

if [[ $RUN_AFTER_BUILD -eq 1 ]] && [[ -x "$OQUAKE_INTEGRATION/build/OQUAKE" ]]; then
  echo "Launching OQuake..."
  cd "$OQUAKE_INTEGRATION/build"
  exec ./OQUAKE -basedir "$OQUAKE_BASEDIR"
fi

