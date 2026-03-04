#!/usr/bin/env bash
# ODOOM - UZDoom + OASIS STAR API. Cross-platform (Linux/macOS) build; equivalent of "BUILD ODOOM.bat" on Windows.
# Usage: ./BUILD_ODOOM.sh [ run ] [ nosprites ]
#   (none)  = incremental build: copy integration, patch, configure, build, package
#   run     = build then launch ODOOM
#   nosprites = skip sprite/icon regeneration (faster; use if sprites already in UZDoom tree)

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/.." && pwd)"
STARAPICLIENT="$OMNIVERSE/STARAPIClient"
ODOOM_INTEGRATION="$HERE"
DOOM_FOLDER="$ODOOM_INTEGRATION"

# Default source path (Linux/macOS)
UZDOOM_SRC="${UZDOOM_SRC:-$HOME/Source/UZDoom}"
DO_FULL_CLEAN=0
DO_SPRITE_REGEN=0
RUN_AFTER_BUILD=0
for arg in "$@"; do
  [[ "$arg" == "run" ]] && RUN_AFTER_BUILD=1
  [[ "$arg" == "nosprites" ]] && DO_SPRITE_REGEN=0
done

# Version display
VERSION_DISPLAY="1.0 (Build 1)"
[[ -f "$ODOOM_INTEGRATION/version_display.txt" ]] && VERSION_DISPLAY=$(cat "$ODOOM_INTEGRATION/version_display.txt" | tr -d '\r')
if [[ -f "$ODOOM_INTEGRATION/generate_odoom_version.ps1" ]]; then
  if command -v pwsh &>/dev/null; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/generate_odoom_version.ps1" -Root "$ODOOM_INTEGRATION" || true
  fi
  [[ -f "$ODOOM_INTEGRATION/version_display.txt" ]] && VERSION_DISPLAY=$(cat "$ODOOM_INTEGRATION/version_display.txt" | tr -d '\r')
fi

echo ""
echo "  ============================================================"
echo "  O A S I S   O D O O M  v$VERSION_DISPLAY"
echo "  By NextGen World Ltd"
echo "  ============================================================"
echo "  Enabling full interoperable games across the OASIS Omniverse!"
echo ""

# --- Prerequisites ---
if ! command -v pkg-config &>/dev/null; then
  echo "ERROR: pkg-config is required for UZDoom CMake. Install with:"
  echo "  sudo apt install -y pkg-config   # Debian/Ubuntu"
  echo "  sudo dnf install -y pkgconfig    # Fedora"
  exit 1
fi
if ! pkg-config --exists sdl2 2>/dev/null; then
  echo "ERROR: SDL2 not found. UZDoom needs SDL2, ALSA, glib-2.0, gtk+-3.0, libvpx. Install with:"
  echo "  sudo apt install -y libsdl2-dev libasound2-dev libglib2.0-dev libgtk-3-dev libvpx-dev   # Debian/Ubuntu"
  echo "  sudo dnf install -y SDL2-devel alsa-lib-devel glib2-devel gtk3-devel libvpx-devel       # Fedora"
  exit 1
fi
if [[ ! -f "$UZDOOM_SRC/src/d_main.cpp" ]]; then
  echo "ERROR: UZDoom source not found: $UZDOOM_SRC"
  echo "  Set UZDOOM_SRC or clone UZDoom to \$HOME/Source/UZDoom"
  exit 1
fi

echo "[ODOOM] Checking STARAPIClient - build if changed, deploy..."
if [[ -f "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh" ]]; then
  bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh"
else
  bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh"
fi
# Linux: .so; macOS: .dylib
if [[ ! -f "$ODOOM_INTEGRATION/libstar_api.so" && ! -f "$ODOOM_INTEGRATION/star_api.so" && ! -f "$ODOOM_INTEGRATION/libstar_api.dylib" && ! -f "$ODOOM_INTEGRATION/star_api.dylib" ]]; then
  echo "ERROR: star_api not found in $ODOOM_INTEGRATION (expected libstar_api.so / .dylib or star_api.so / .dylib after deploy)."
  exit 1
fi
if [[ ! -f "$STARAPICLIENT/star_api.h" ]]; then
  echo "ERROR: star_api.h not found: $STARAPICLIENT"
  exit 1
fi

# star_sync
if [[ -f "$STARAPICLIENT/star_sync.c" ]]; then
  cp -f "$STARAPICLIENT/star_sync.c" "$ODOOM_INTEGRATION/"
  cp -f "$STARAPICLIENT/star_sync.h" "$ODOOM_INTEGRATION/"
fi

echo ""
echo "[ODOOM][STEP] Installing integration files..."
cp -f "$ODOOM_INTEGRATION/uzdoom_star_integration.cpp" "$UZDOOM_SRC/src/"
cp -f "$ODOOM_INTEGRATION/uzdoom_star_integration.h" "$UZDOOM_SRC/src/"
[[ -f "$ODOOM_INTEGRATION/star_sync.c" ]] && cp -f "$ODOOM_INTEGRATION/star_sync.c" "$UZDOOM_SRC/src/"
[[ -f "$ODOOM_INTEGRATION/star_sync.h" ]] && cp -f "$ODOOM_INTEGRATION/star_sync.h" "$UZDOOM_SRC/src/"
cp -f "$ODOOM_INTEGRATION/odoom_branding.h" "$UZDOOM_SRC/src/"
mkdir -p "$UZDOOM_SRC/wadsrc/static/zscript/actors/doom"
cp -f "$ODOOM_INTEGRATION/odoom_oquake_keys.zs" "$UZDOOM_SRC/wadsrc/static/zscript/actors/doom/"
cp -f "$ODOOM_INTEGRATION/odoom_oquake_items.zs" "$UZDOOM_SRC/wadsrc/static/zscript/actors/doom/"
mkdir -p "$UZDOOM_SRC/wadsrc/static/zscript/ui/statusbar"
cp -f "$ODOOM_INTEGRATION/odoom_inventory_popup.zs" "$UZDOOM_SRC/wadsrc/static/zscript/ui/statusbar/"
mkdir -p "$UZDOOM_SRC/wadsrc/static/textures"
[[ -d "$ODOOM_INTEGRATION/textures" ]] && cp -f "$ODOOM_INTEGRATION/textures/OASFACE.png" "$UZDOOM_SRC/wadsrc/static/textures/" 2>/dev/null || true
mkdir -p "$UZDOOM_SRC/wadsrc/static/sprites" "$UZDOOM_SRC/wadsrc/static/graphics"

# Optional: sprite regeneration (PowerShell/Python on Windows). On Linux we skip unless sprites already exist.
if [[ $DO_SPRITE_REGEN -eq 1 ]]; then
  echo "[ODOOM][NOTE] Sprite regeneration requested but Linux script uses nosprites by default. Pre-generate sprites on Windows or add Python/PowerShell steps here."
fi

# Verify required OQ runtime sprites exist (or warn and continue)
REQ_MISSING=0
for s in OQKGA0.png OQKSA0.png OQW1A0.png OQH1A0.png OQM1A1.png OQM2A1.png OQMAA1.png; do
  if [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/$s" ]]; then
    echo "[ODOOM][WARN] Missing sprite: $s (build may fail; generate on Windows or copy from a built tree)."
    REQ_MISSING=1
  fi
done
[[ $REQ_MISSING -eq 1 ]] && echo "[ODOOM][INFO] Continuing anyway; if link fails, generate sprites in UZDoom tree (see Windows BUILD ODOOM.bat)."

[[ -f "$ODOOM_INTEGRATION/odoom_version_generated.h" ]] && cp -f "$ODOOM_INTEGRATION/odoom_version_generated.h" "$UZDOOM_SRC/src/"

# Patch UZDoom (requires PowerShell Core on Linux)
if command -v pwsh &>/dev/null; then
  echo "[ODOOM][STEP] Patching UZDoom engine..."
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/patch_uzdoom_engine.ps1" -UZDOOM_SRC "$UZDOOM_SRC" || true
else
  echo "[ODOOM][WARN] pwsh not found; skipping patch. Install PowerShell Core (pwsh) to apply UZDoom patches, or patch manually."
fi

if [[ -f "$ODOOM_INTEGRATION/oasis_banner.png" ]]; then
  cp -f "$ODOOM_INTEGRATION/oasis_banner.png" "$UZDOOM_SRC/wadsrc/static/ui/banner-dark.png"
  cp -f "$ODOOM_INTEGRATION/oasis_banner.png" "$UZDOOM_SRC/wadsrc/static/ui/banner-light.png"
  cp -f "$ODOOM_INTEGRATION/oasis_banner.png" "$UZDOOM_SRC/wadsrc/static/graphics/bootlogo.png"
fi

echo ""
if [[ "$DO_FULL_CLEAN" == "1" ]]; then
  echo "[ODOOM][STEP] Full clean..."
  rm -rf "$UZDOOM_SRC/build"
fi

echo "[ODOOM][STEP] Configuring CMake and STAR API..."
mkdir -p "$UZDOOM_SRC/build"
cd "$UZDOOM_SRC/build"

# On Linux we pass STAR_API_DIR (header) and STAR_API_LIB_DIR (folder containing libstar_api.so)
STAR_API_DIR="$STARAPICLIENT"
STAR_API_LIB_DIR="$DOOM_FOLDER"
PYTHON3_EXE="${PYTHON3_EXE:-$(command -v python3 || command -v python)}"

cmake .. \
  -G "Unix Makefiles" \
  -DCMAKE_BUILD_TYPE=Release \
  -DOASIS_STAR_API=ON \
  -DSTAR_API_DIR:PATH="$STAR_API_DIR" \
  -DSTAR_API_LIB_DIR:PATH="$STAR_API_LIB_DIR" \
  -DPython3_EXECUTABLE:FILEPATH="$PYTHON3_EXE"

echo ""
echo "[ODOOM][STEP] Building..."
NPROC=$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 2)
cmake --build . -j${NPROC}

echo ""
echo "[ODOOM][STEP] Packaging output..."
if command -v python3 &>/dev/null && [[ -f "$ODOOM_INTEGRATION/create_odoom_face_pk3.py" ]]; then
  python3 "$ODOOM_INTEGRATION/create_odoom_face_pk3.py" || true
fi

# Find built executable (uzdoom or uzdoom.exe)
ODOOM_BIN=""
for exe in uzdoom uzdoom.exe; do
  if [[ -f "$UZDOOM_SRC/build/$exe" ]]; then
    ODOOM_BIN="$UZDOOM_SRC/build/$exe"
    break
  fi
done
if [[ -z "$ODOOM_BIN" ]]; then
  # Some CMake configs put exe in a subdir
  ODOOM_BIN="$(find "$UZDOOM_SRC/build" -maxdepth 2 -type f -executable -name "uzdoom*" 2>/dev/null | head -1)"
fi
if [[ -z "$ODOOM_BIN" || ! -f "$ODOOM_BIN" ]]; then
  echo "ERROR: uzdoom executable not found under $UZDOOM_SRC/build"
  exit 1
fi

mkdir -p "$ODOOM_INTEGRATION/build"
cp -f "$ODOOM_BIN" "$ODOOM_INTEGRATION/build/ODOOM"
chmod +x "$ODOOM_INTEGRATION/build/ODOOM"
# Deploy STAR API shared lib next to executable (.so on Linux, .dylib on macOS)
for so in libstar_api.so star_api.so libstar_api.dylib star_api.dylib; do
  if [[ -f "$ODOOM_INTEGRATION/$so" ]]; then
    cp -f "$ODOOM_INTEGRATION/$so" "$ODOOM_INTEGRATION/build/"
    break
  fi
done
[[ -f "$ODOOM_INTEGRATION/odoom_face.pk3" ]] && cp -f "$ODOOM_INTEGRATION/odoom_face.pk3" "$ODOOM_INTEGRATION/build/"

echo ""
echo "---"
echo "[ODOOM][DONE] ODOOM ready: $ODOOM_INTEGRATION/build/ODOOM"
echo "[ODOOM][INFO] Put doom2.wad in build folder. Use ./RUN_ODOOM.sh to launch."
echo "---"

if [[ $RUN_AFTER_BUILD -eq 1 ]]; then
  if [[ -x "$ODOOM_INTEGRATION/build/ODOOM" ]]; then
    echo "Launching ODOOM..."
    cd "$ODOOM_INTEGRATION/build" && exec ./ODOOM
  else
    exec "$ODOOM_BIN"
  fi
fi
