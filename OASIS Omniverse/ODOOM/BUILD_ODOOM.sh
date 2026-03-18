#!/usr/bin/env bash
# ODOOM - UZDoom + OASIS STAR API. Credit: UZDoom (GPL-3.0). See CREDITS_AND_LICENSE.md.
# Cross-platform (Linux, macOS) build; equivalent of "BUILD ODOOM.bat" on Windows.
# Usage: ./BUILD_ODOOM.sh [ run | batch ] [ nosprites ]
#   (none)  = prompt clean/incremental, then copy, patch, configure, build, package
#   run     = incremental build then launch (no prompt; build log written to build_logs/)
#   batch   = incremental build, no prompts, no launch (for BUILD_EVERYTHING.sh)
#   nosprites = skip sprite/icon regeneration (faster; use if sprites already in UZDoom tree)

set -e


# OASIS: pause before exit when run from GUI (CI: OASIS_SCRIPT_NO_PAUSE=1)
if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" != "1" ]]; then
  _OASIS_TD="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  while [[ "$_OASIS_TD" != "/" ]]; do
    if [[ -f "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh" ]]; then
      # shellcheck disable=SC1091
      source "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh"
      break
    fi
    _OASIS_TD="$(dirname "$_OASIS_TD")"
  done
fi

# When first arg is "run", wrap in build log then re-exec with __logrun (match Windows behaviour).
if [[ "${1:-}" == "run" ]]; then
  ODOOM_LOG_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/build_logs"
  mkdir -p "$ODOOM_LOG_DIR"
  ODOOM_LOG_TS=$(date +%Y%m%d_%H%M%S 2>/dev/null || echo "log")
  ODOOM_BUILD_LOG="$ODOOM_LOG_DIR/odoom_build_${ODOOM_LOG_TS}.log"
  echo "[ODOOM][INFO] Writing build log to: $ODOOM_BUILD_LOG"
  if "${BASH_SOURCE[0]:-$0}" __logrun "$@" 2>&1 | tee "$ODOOM_BUILD_LOG"; then
    echo "[ODOOM][INFO] Build log saved: $ODOOM_BUILD_LOG"
    exit 0
  else
    echo "[ODOOM][INFO] Build log saved: $ODOOM_BUILD_LOG"
    exit 1
  fi
fi

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/.." && pwd)"
STARAPICLIENT="$OMNIVERSE/STARAPIClient"
ODOOM_INTEGRATION="$HERE"
DOOM_FOLDER="$ODOOM_INTEGRATION"

# Default source path (Linux / macOS)
UZDOOM_SRC="${UZDOOM_SRC:-$HOME/Source/UZDoom}"
DO_FULL_CLEAN=0
DO_SPRITE_REGEN=1
RUN_AFTER_BUILD=0
BATCH_MODE=0
OQ_MONSTER_PAD="${OQ_MONSTER_PAD:-0}"
OQ_ITEM_PAD="${OQ_ITEM_PAD:-0}"
# Default: use star_sync from star_api (C#). Set OASIS_STAR_SYNC_IN_CLIENT=0 to compile star_sync.c (C) instead.
OASIS_STAR_SYNC_IN_CLIENT="${OASIS_STAR_SYNC_IN_CLIENT:-1}"
BUILD_STAR_CLIENT=0
# OASIS sprite source: UDB Build or Assets, or ODOOM build/Editor copy. Override with OASIS_SPRITES_SRC.
ULTIMATE_DOOM_BUILDER_BUILD="${ULTIMATE_DOOM_BUILDER_BUILD:-$HOME/Source/UltimateDoomBuilder/Build}"
ULTIMATE_DOOM_BUILDER_ASSETS="${ULTIMATE_DOOM_BUILDER_ASSETS:-$HOME/Source/UltimateDoomBuilder/Assets/Common/UDBScript/Scripts/OASIS/Sprites}"
if [[ -n "${OASIS_SPRITES_SRC:-}" ]]; then
  :
elif [[ -f "$ODOOM_INTEGRATION/build/Editor/UDBScript/Scripts/OASIS/Sprites/5005.png" ]]; then
  OASIS_SPRITES_SRC="$ODOOM_INTEGRATION/build/Editor/UDBScript/Scripts/OASIS/Sprites"
elif [[ -f "$ULTIMATE_DOOM_BUILDER_ASSETS/5005.png" ]]; then
  OASIS_SPRITES_SRC="$ULTIMATE_DOOM_BUILDER_ASSETS"
elif [[ -f "$ULTIMATE_DOOM_BUILDER_BUILD/UDBScript/Scripts/OASIS/Sprites/5005.png" ]]; then
  OASIS_SPRITES_SRC="$ULTIMATE_DOOM_BUILDER_BUILD/UDBScript/Scripts/OASIS/Sprites"
else
  OASIS_SPRITES_SRC=""
fi
# Quake PAKs for OQ monster MDL sprite generation (Linux Steam default). Override QUAKE_PAK0 / QUAKE_PAK1.
QUAKE_STEAM="${QUAKE_STEAM:-$HOME/.steam/steam/steamapps/common/Quake/id1}"
[[ "$(uname -s)" == "Darwin" ]] && QUAKE_STEAM="${QUAKE_STEAM:-$HOME/Library/Application Support/Steam/steamapps/common/Quake/id1}"
QUAKE_PAK0="${QUAKE_PAK0:-$QUAKE_STEAM/PAK0.PAK}"
QUAKE_PAK1="${QUAKE_PAK1:-$QUAKE_STEAM/PAK1.PAK}"
[[ -f "$QUAKE_STEAM/pak0.pak" && ! -f "$QUAKE_PAK0" ]] && QUAKE_PAK0="$QUAKE_STEAM/pak0.pak"
[[ -f "$QUAKE_STEAM/pak1.pak" && ! -f "$QUAKE_PAK1" ]] && QUAKE_PAK1="$QUAKE_STEAM/pak1.pak"

for arg in "$@"; do
  [[ "$arg" == "run" ]] && RUN_AFTER_BUILD=1
  [[ "$arg" == "batch" ]] && BATCH_MODE=1
  [[ "$arg" == "__logrun" ]] && shift && break
  [[ "$arg" == "nosprites" ]] && DO_SPRITE_REGEN=0
done
# Re-parse args for __logrun case (run was shifted out)
for arg in "$@"; do
  [[ "$arg" == "run" ]] && RUN_AFTER_BUILD=1
  [[ "$arg" == "nosprites" ]] && DO_SPRITE_REGEN=0
done

# When not "run" and not "batch", prompt for full clean vs incremental (match Windows).
if [[ $RUN_AFTER_BUILD -eq 0 && $BATCH_MODE -eq 0 ]]; then
  echo ""
  read -p "  Full clean/rebuild [c] or incremental build [i]? [i]: " BUILD_CHOICE
  BUILD_CHOICE="${BUILD_CHOICE:-i}"
  if [[ "${BUILD_CHOICE,,}" == "c" ]]; then
    DO_FULL_CLEAN=1
    BUILD_STAR_CLIENT=1
  fi
fi
if [[ "${1:-}" == "nosprites" || "${2:-}" == "nosprites" ]]; then
  DO_SPRITE_REGEN=0
fi
# When not run and not batch, prompt for sprite regen (match Windows). If nosprites, still auto-enable if required sprites missing.
if [[ $RUN_AFTER_BUILD -eq 0 && $BATCH_MODE -eq 0 && $DO_SPRITE_REGEN -eq 1 ]]; then
  echo ""
  read -p "  Regenerate sprites/icons this build [Y/N]? [Y]: " SPRITE_CHOICE
  SPRITE_CHOICE="${SPRITE_CHOICE:-Y}"
  [[ "${SPRITE_CHOICE,,}" == "n" || "${SPRITE_CHOICE,,}" == "no" ]] && DO_SPRITE_REGEN=0
fi
if [[ $DO_SPRITE_REGEN -eq 0 ]]; then
  [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKGA0.png" ]] && DO_SPRITE_REGEN=1
  [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQW1A0.png" ]] && DO_SPRITE_REGEN=1
  [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQM1A1.png" ]] && DO_SPRITE_REGEN=1
  [[ $DO_SPRITE_REGEN -eq 1 ]] && echo "[ODOOM][NOTE] Required OQ runtime sprites missing in UZDoom; enabling sprite/icon regeneration automatically."
fi

# Version display
VERSION_DISPLAY="1.0 (Build 1)"
[[ -f "$ODOOM_INTEGRATION/version_display.txt" ]] && VERSION_DISPLAY=$(cat "$ODOOM_INTEGRATION/version_display.txt" | tr -d '\r')
if [[ -f "$ODOOM_INTEGRATION/generate_odoom_version.ps1" ]]; then
  if command -v pwsh &>/dev/null; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/generate_odoom_version.ps1" -Root "$ODOOM_INTEGRATION" || true
  fi
  [[ -f "$ODOOM_INTEGRATION/version_display.txt" ]] && VERSION_DISPLAY=$(cat "$ODOOM_INTEGRATION/version_display.txt" | tr -d '\r')
fi

# Banner (centre-aligned, colours, slogan - same as OQuake)
if [[ -f "$OMNIVERSE/run_oasis_header.sh" ]]; then
  bash "$OMNIVERSE/run_oasis_header.sh" ODOOM "$VERSION_DISPLAY" || true
fi

# --- Prerequisites ---
if ! command -v pkg-config &>/dev/null; then
  echo "ERROR: pkg-config is required for UZDoom CMake. Install with:"
  echo "  sudo apt install -y pkg-config        # Debian/Ubuntu"
  echo "  sudo dnf install -y pkgconfig         # Fedora"
  echo "  brew install pkg-config               # macOS"
  exit 1
fi
if ! pkg-config --exists sdl2 2>/dev/null; then
  echo "ERROR: SDL2 not found. UZDoom needs SDL2, ALSA (Linux), glib-2.0, gtk+-3.0, libvpx. Install with:"
  echo "  sudo apt install -y libsdl2-dev libasound2-dev libglib2.0-dev libgtk-3-dev libvpx-dev   # Debian/Ubuntu"
  echo "  sudo dnf install -y SDL2-devel alsa-lib-devel glib2-devel gtk3-devel libvpx-devel         # Fedora"
  echo "  brew install pkg-config sdl2 glib gtk+3 libvpx   # macOS (ALSA not needed)"
  exit 1
fi
if [[ ! -f "$UZDOOM_SRC/src/d_main.cpp" ]]; then
  echo "ERROR: UZDoom source not found: $UZDOOM_SRC"
  echo "  Set UZDOOM_SRC or clone UZDoom to \$HOME/Source/UZDoom"
  exit 1
fi

# Always check STARAPIClient (build if source changed, then deploy). Use BUILD_STAR_CLIENT=1 to force full rebuild.
echo "[ODOOM] Checking STARAPIClient - build if changed, deploy..."
if [[ -f "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh" ]]; then
  if [[ "$BUILD_STAR_CLIENT" -eq 1 ]]; then
    bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh" -ForceBuild
  else
    bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh"
  fi
else
  if [[ "$BUILD_STAR_CLIENT" -eq 1 ]]; then
    bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh" -ForceBuild 2>/dev/null || bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh"
  else
    bash "$OMNIVERSE/STARAPIClient/Scripts/build-and-deploy-star-api-linux.sh"
  fi
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
[[ -f "$ODOOM_INTEGRATION/KEYCONF.txt" ]] && cp -f "$ODOOM_INTEGRATION/KEYCONF.txt" "$UZDOOM_SRC/wadsrc/static/KEYCONF"
mkdir -p "$UZDOOM_SRC/wadsrc/static/textures"
# Prepare OASFACE.png from face_anorak.png (34x30). Use PowerShell on Windows; on Linux pwsh fails (System.Drawing/Win32), so fall back to Python+Pillow.
if [[ -f "$ODOOM_INTEGRATION/face_anorak.png" ]]; then
  echo "[ODOOM][STEP] Preparing anorak HUD face from face_anorak.png (target 34x30)..."
  FACE_OK=0
  if command -v pwsh &>/dev/null && [[ -f "$ODOOM_INTEGRATION/prepare_odoom_face_texture.ps1" ]]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/prepare_odoom_face_texture.ps1" -SourcePath "$ODOOM_INTEGRATION/face_anorak.png" -DestPath "$ODOOM_INTEGRATION/textures/OASFACE.png" -Width 34 -Height 30 -OffsetX 1 -OffsetY 1 2>/dev/null && FACE_OK=1
  fi
  if [[ $FACE_OK -eq 0 ]] && [[ -f "$ODOOM_INTEGRATION/prepare_odoom_face_texture.py" ]]; then
    PYTHON3_EXE="${PYTHON3_EXE:-$(command -v python3 || command -v python)}"
    if [[ -n "$PYTHON3_EXE" ]]; then
      "$PYTHON3_EXE" "$ODOOM_INTEGRATION/prepare_odoom_face_texture.py" --source "$ODOOM_INTEGRATION/face_anorak.png" --dest "$ODOOM_INTEGRATION/textures/OASFACE.png" --width 34 --height 30 --offset-x 1 --offset-y 1 && FACE_OK=1
    fi
  fi
  if [[ $FACE_OK -eq 0 ]]; then
    echo "[ODOOM][WARN] face_anorak.png processing failed (pwsh/System.Drawing not available on Linux; install Pillow and use Python: pip install Pillow). Using existing textures/OASFACE.png if present."
  fi
fi
[[ -d "$ODOOM_INTEGRATION/textures" ]] && [[ -f "$ODOOM_INTEGRATION/textures/OASFACE.png" ]] && cp -f "$ODOOM_INTEGRATION/textures/OASFACE.png" "$UZDOOM_SRC/wadsrc/static/textures/"
mkdir -p "$UZDOOM_SRC/wadsrc/static/sprites" "$UZDOOM_SRC/wadsrc/static/graphics"

# Sprite/icon regeneration (parity with Windows: pwsh for key/item sprites, Python for OQ monster MDL, then HUD icons).
if [[ $DO_SPRITE_REGEN -eq 1 ]]; then
  if [[ -z "$OASIS_SPRITES_SRC" || ! -d "$OASIS_SPRITES_SRC" ]]; then
    echo "[ODOOM][NOTE] OASIS_SPRITES_SRC not set or missing; set it to sprite source (e.g. UltimateDoomBuilder Sprites) or use nosprites."
    DO_SPRITE_REGEN=0
  fi
fi
PYTHON3_EXE="${PYTHON3_EXE:-$(command -v python3 || command -v python)}"
if [[ $DO_SPRITE_REGEN -eq 1 ]] && [[ -n "$OASIS_SPRITES_SRC" ]] && command -v pwsh &>/dev/null; then
  echo "[ODOOM][INFO] OASIS sprite source: $OASIS_SPRITES_SRC"
  # Gold key
  if [[ -f "$OASIS_SPRITES_SRC/5005.png" ]] && [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKGA0.png" ]]; then
    echo "[ODOOM][STEP] Regenerating OQ gold key sprites..."
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/prepare_odoom_key_sprite.ps1" -SourcePath "$OASIS_SPRITES_SRC/5005.png" -DestPath "$UZDOOM_SRC/wadsrc/static/sprites/OQKGA0.png" -MaxWidth 18 -MaxHeight 24
    echo "[ODOOM][DONE] OQ gold key sprite generation complete."
  fi
  [[ -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKGA0.png" ]] && [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKGB0.png" ]] && cp -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKGA0.png" "$UZDOOM_SRC/wadsrc/static/sprites/OQKGB0.png"
  # Silver key
  if [[ -f "$OASIS_SPRITES_SRC/5013.png" ]] && [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKSA0.png" ]]; then
    echo "[ODOOM][STEP] Regenerating OQ silver key sprites..."
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/prepare_odoom_key_sprite.ps1" -SourcePath "$OASIS_SPRITES_SRC/5013.png" -DestPath "$UZDOOM_SRC/wadsrc/static/sprites/OQKSA0.png" -MaxWidth 18 -MaxHeight 24
    echo "[ODOOM][DONE] OQ silver key sprite generation complete."
  fi
  [[ -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKSA0.png" ]] && [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKSB0.png" ]] && cp -f "$UZDOOM_SRC/wadsrc/static/sprites/OQKSA0.png" "$UZDOOM_SRC/wadsrc/static/sprites/OQKSB0.png"
  # Non-key OQUAKE sprites (OQW, OQA, OQH)
  for base in "5201:OQW1A0" "5202:OQW2A0" "5203:OQW3A0" "5204:OQW4A0" "5205:OQW5A0" "5206:OQW6A0" "5207:OQW7A0" "5208:OQA1A0" "5209:OQA2A0" "5210:OQA3A0" "5211:OQA4A0" "5212:OQH1A0" "5213:OQH2A0" "5214:OQH3A0" "5215:OQH4A0" "5216:OQH5A0"; do
    src_num="${base%%:*}"
    dest_name="${base##*:}"
    if [[ -f "$OASIS_SPRITES_SRC/${src_num}.png" ]] && [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/${dest_name}.png" ]]; then
      pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/prepare_odoom_key_sprite.ps1" -SourcePath "$OASIS_SPRITES_SRC/${src_num}.png" -DestPath "$UZDOOM_SRC/wadsrc/static/sprites/${dest_name}.png" -MaxWidth 24 -MaxHeight 24 -PadBottom "$OQ_ITEM_PAD" || true
    fi
  done
fi
# OQ monster sprites from Quake MDL (Python) - only if QUAKE_PAK0/PAK1 exist (match Windows)
MDLGEN_FAILED=0
run_mdl() {
  local prefix="$1" mdl="$2" pak="${3:-$QUAKE_PAK0}"
  if [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/${prefix}A1.png" ]]; then
    if ! "$PYTHON3_EXE" "$ODOOM_INTEGRATION/generate_oquake_mdl_sprites.py" --mdl-pak "$pak" --mdl-entry "$mdl" --palette-pak "$QUAKE_PAK0" --palette-entry "gfx/palette.lmp" --out-dir "$UZDOOM_SRC/wadsrc/static/sprites" --sprite-prefix "$prefix" --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest; then
      MDLGEN_FAILED=1
      return 1
    fi
  fi
  return 0
}
if [[ $DO_SPRITE_REGEN -eq 1 ]] && [[ -n "$PYTHON3_EXE" ]] && [[ -f "$ODOOM_INTEGRATION/generate_oquake_mdl_sprites.py" ]]; then
  if [[ -f "$QUAKE_PAK0" ]] && [[ -f "$QUAKE_PAK1" ]]; then
    [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQM1A1.png" ]] && echo "[ODOOM][STEP] Generating OQ monster sprites: dog OQM1..." && run_mdl OQM1 "progs/dog.mdl"
    [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/OQM2A1.png" ]] && echo "[ODOOM][STEP] Generating OQ monster sprites: zombie OQM2..." && run_mdl OQM2 "progs/zombie.mdl"
    run_mdl OQM3 "progs/demon.mdl" || true
    run_mdl OQM4 "progs/shambler.mdl" || true
    run_mdl OQM5 "progs/soldier.mdl" || true
    run_mdl OQM6 "progs/fish.mdl" "$QUAKE_PAK1" || true
    run_mdl OQM7 "progs/ogre.mdl" || true
    run_mdl OQM8 "progs/enforcer.mdl" "$QUAKE_PAK1" || true
    run_mdl OQM9 "progs/tarbaby.mdl" "$QUAKE_PAK1" || true
    run_mdl OQMA "progs/hknight.mdl" "$QUAKE_PAK1" || true
    if [[ $MDLGEN_FAILED -eq 1 ]]; then
      echo "[ODOOM][ERROR] Failed to generate one or more Doom-profile OQ monster sprite sets."
      exit 1
    fi
  else
    echo "[ODOOM][NOTE] QUAKE_PAK0 or QUAKE_PAK1 not found; skipping OQ monster MDL sprite generation. Set QUAKE_PAK0/QUAKE_PAK1 (e.g. Steam Quake id1)."
  fi
fi
# OQ HUD key icons
if [[ $DO_SPRITE_REGEN -eq 1 ]] && command -v pwsh &>/dev/null && [[ ! -f "$UZDOOM_SRC/wadsrc/static/graphics/OQKGI0.png" ]] && [[ -f "$ODOOM_INTEGRATION/generate_odoom_hud_key_icons.ps1" ]]; then
  echo "[ODOOM][STEP] Generating OQ HUD key icons..."
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/generate_odoom_hud_key_icons.ps1" -OutputDir "$UZDOOM_SRC/wadsrc/static/graphics"
  echo "[ODOOM][DONE] OQ HUD key icons generated."
fi
if [[ $DO_SPRITE_REGEN -eq 0 ]]; then
  echo "[ODOOM][NOTE] Sprite/icon regeneration disabled - nosprites mode."
fi
# Verify required OQ runtime sprites (match Windows: exit 1 if any required missing)
echo "[ODOOM][STEP] Verifying required OQ runtime sprites..."
REQ_OQ_MISSING=0
for s in OQKGA0.png OQKSA0.png OQW1A0.png OQH1A0.png OQM1A1.png OQM2A1.png OQMAA1.png; do
  if [[ ! -f "$UZDOOM_SRC/wadsrc/static/sprites/$s" ]]; then
    echo "[ODOOM][ERROR] Missing required sprite: $s"
    REQ_OQ_MISSING=1
  fi
done
OQW_COUNT=0
OQH_COUNT=0
OQM_COUNT=0
[[ -d "$UZDOOM_SRC/wadsrc/static/sprites" ]] && OQW_COUNT=$(find "$UZDOOM_SRC/wadsrc/static/sprites" -maxdepth 1 -name "OQW*A0.png" 2>/dev/null | wc -l)
[[ -d "$UZDOOM_SRC/wadsrc/static/sprites" ]] && OQH_COUNT=$(find "$UZDOOM_SRC/wadsrc/static/sprites" -maxdepth 1 -name "OQH*A0.png" 2>/dev/null | wc -l)
[[ -d "$UZDOOM_SRC/wadsrc/static/sprites" ]] && OQM_COUNT=$(find "$UZDOOM_SRC/wadsrc/static/sprites" -maxdepth 1 -name "OQM*.png" 2>/dev/null | wc -l)
echo "[ODOOM][INFO] Sprite counts: OQW=$OQW_COUNT OQH=$OQH_COUNT OQM=$OQM_COUNT"
if [[ $REQ_OQ_MISSING -eq 1 ]]; then
  echo "[ODOOM][ERROR] Required OQ sprites are missing. Re-run with sprite regeneration enabled or copy sprites from a built tree."
  exit 1
fi
echo "[ODOOM][DONE] OQ runtime sprite verification passed."

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

# On Linux/macOS we pass STAR_API_DIR (header) and STAR_API_LIB_DIR (folder containing libstar_api.so / libstar_api.dylib)
STAR_API_DIR="$STARAPICLIENT"
STAR_API_LIB_DIR="$DOOM_FOLDER"
PYTHON3_EXE="${PYTHON3_EXE:-$(command -v python3 || command -v python)}"

# Copy STAR API lib into build/ and build/src/ so the linker finds it (link runs from build/src/; -lstar_api needs libstar_api.so).
STAR_LIB_SRC=""
for lib in libstar_api.so star_api.so libstar_api.dylib star_api.dylib; do
  if [[ -f "$STAR_API_LIB_DIR/$lib" ]]; then
    STAR_LIB_SRC="$STAR_API_LIB_DIR/$lib"
    break
  fi
done
if [[ -z "$STAR_LIB_SRC" || ! -f "$STAR_LIB_SRC" ]]; then
  echo "ERROR: No STAR API library in $STAR_API_LIB_DIR. Run BUILD_ODOOM.sh from ODOOM folder (it deploys STAR API first)."
  exit 1
fi
STAR_LIB_NAME="libstar_api.so"
[[ "$STAR_LIB_SRC" == *.dylib ]] && STAR_LIB_NAME="libstar_api.dylib"
mkdir -p "$UZDOOM_SRC/build/src"
for destdir in "$UZDOOM_SRC/build" "$UZDOOM_SRC/build/src"; do
  cp -f "$STAR_LIB_SRC" "$destdir/$STAR_LIB_NAME"
done
# OASIS_STAR_SYNC_IN_CLIENT: 1 = use star_sync from star_api (C#); 0 = compile star_sync.c (C). See star_sync.h / STAR_INTEGRATION_AUDIT.md.
if [[ "${OASIS_STAR_SYNC_IN_CLIENT:-1}" == "0" ]]; then
  CMAKE_STAR_SYNC="-DOASIS_STAR_SYNC_IN_CLIENT=OFF"
  echo "[ODOOM][INFO] Compiling star_sync.c - C implementation"
else
  CMAKE_STAR_SYNC="-DOASIS_STAR_SYNC_IN_CLIENT=ON"
  echo "[ODOOM][INFO] Using star_sync from star_api - default"
fi
# Add both ODOOM folder and build/src to link path so -lstar_api resolves
CMAKE_LINK_FLAGS="-L\"$UZDOOM_SRC/build/src\" -L\"$STAR_API_LIB_DIR\""
cmake .. \
  -G "Unix Makefiles" \
  -DCMAKE_BUILD_TYPE=Release \
  -DOASIS_STAR_API=ON \
  -DSTAR_API_DIR:PATH="$STAR_API_DIR" \
  -DSTAR_API_LIB_DIR:PATH="$STAR_API_LIB_DIR" \
  "$CMAKE_STAR_SYNC" \
  -DCMAKE_EXE_LINKER_FLAGS:STRING="$CMAKE_LINK_FLAGS" \
  -DPython3_EXECUTABLE:FILEPATH="$PYTHON3_EXE"

echo ""
echo "[ODOOM][STEP] Building..."
NPROC=$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 2)
# Ensure linker finds libstar_api.so at link time (in case CMake does not pass our -L flags)
export LIBRARY_PATH="$UZDOOM_SRC/build/src:$STAR_API_LIB_DIR${LIBRARY_PATH:+:$LIBRARY_PATH}"
cmake --build . -j${NPROC}

echo ""
echo "[ODOOM][STEP] Packaging output..."
echo "[ODOOM][STEP] Packaging OASIS beamed-in face (OASFACE)..."
if command -v python3 &>/dev/null && [[ -f "$ODOOM_INTEGRATION/create_odoom_face_pk3.py" ]]; then
  python3 "$ODOOM_INTEGRATION/create_odoom_face_pk3.py" || echo "[ODOOM][WARN] OASFACE pk3 generation failed - beamed-in face may be missing."
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
# Deploy STAR API shared lib next to executable; binary is linked against libstar_api.so / libstar_api.dylib
for so in libstar_api.so star_api.so libstar_api.dylib star_api.dylib; do
  if [[ -f "$ODOOM_INTEGRATION/$so" ]]; then
    cp -f "$ODOOM_INTEGRATION/$so" "$ODOOM_INTEGRATION/build/"
    break
  fi
done
# Ensure libstar_api.so (or .dylib) exists so the loader finds it (in case deploy produced only star_api.so)
if [[ ! -f "$ODOOM_INTEGRATION/build/libstar_api.so" && -f "$ODOOM_INTEGRATION/build/star_api.so" ]]; then
  cp -f "$ODOOM_INTEGRATION/build/star_api.so" "$ODOOM_INTEGRATION/build/libstar_api.so"
fi
if [[ ! -f "$ODOOM_INTEGRATION/build/libstar_api.dylib" && -f "$ODOOM_INTEGRATION/build/star_api.dylib" ]]; then
  cp -f "$ODOOM_INTEGRATION/build/star_api.dylib" "$ODOOM_INTEGRATION/build/libstar_api.dylib"
fi
[[ -f "$ODOOM_INTEGRATION/odoom_face.pk3" ]] && cp -f "$ODOOM_INTEGRATION/odoom_face.pk3" "$ODOOM_INTEGRATION/build/"

# Create XDG IWAD directory so user can copy doom2.wad etc. without creating folders by hand
mkdir -p "$HOME/.local/share/games/odoom"

[[ -f "$ODOOM_INTEGRATION/RUN_ODOOM.sh" ]] && chmod +x "$ODOOM_INTEGRATION/RUN_ODOOM.sh"
echo ""
echo ""
echo "---"
echo "[ODOOM][DONE] ODOOM ready: ${ODOOM_INTEGRATION}/build/ODOOM"
echo "[ODOOM][INFO] Put doom2.wad or other IWAD in build folder. odoom_face.pk3 is included for beamed-in status bar face."
echo "[ODOOM][INFO] IWAD can also go in: ${HOME}/.local/share/games/odoom - created automatically."
printf '[ODOOM][INFO] To launch: ./RUN_ODOOM.sh  or from ODOOM folder: cd "%s" && ./RUN_ODOOM.sh\n' "$ODOOM_INTEGRATION"
echo "---"

if [[ $RUN_AFTER_BUILD -eq 1 ]]; then
  if [[ -x "$ODOOM_INTEGRATION/build/ODOOM" ]]; then
    echo "Launching ODOOM..."
    cd "$ODOOM_INTEGRATION/build" && exec ./ODOOM
  else
    exec "$ODOOM_BIN"
  fi
fi
