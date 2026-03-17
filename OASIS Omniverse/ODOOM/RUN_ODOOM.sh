#!/usr/bin/env bash
# Build ODOOM if needed, then launch. Put WAD (e.g. doom2.wad) in ODOOM/build/ or ~/.local/share/games/odoom.
# Equivalent of "RUN ODOOM.bat" on Windows.

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/.." && pwd)"
# Banner (centre-aligned, colours, slogan - same as RUN_OQUAKE)
if [[ -f "$OMNIVERSE/run_oasis_header.sh" ]]; then
  bash "$OMNIVERSE/run_oasis_header.sh" RUN_ODOOM || true
fi

UZDOOM_SRC="${UZDOOM_SRC:-$HOME/Source/UZDoom}"
ODOOM_EXE=""

if [[ -x "$HERE/build/ODOOM" ]]; then
  ODOOM_EXE="$HERE/build/ODOOM"
elif [[ -f "$UZDOOM_SRC/build/uzdoom" && -x "$UZDOOM_SRC/build/uzdoom" ]]; then
  ODOOM_EXE="$UZDOOM_SRC/build/uzdoom"
fi

if [[ -z "$ODOOM_EXE" ]]; then
  echo "ODOOM not built. Building..."
  exec "$HERE/BUILD_ODOOM.sh" run
fi

ODOOM_DIR="$(dirname "$ODOOM_EXE")"
# Ensure libstar_api.so (or .dylib) is in the same dir as the executable; binary is linked against libstar_api
if [[ ! -f "$ODOOM_DIR/libstar_api.so" && ! -f "$ODOOM_DIR/libstar_api.dylib" ]]; then
  if [[ -f "$ODOOM_DIR/star_api.so" ]]; then
    ln -sf star_api.so "$ODOOM_DIR/libstar_api.so" 2>/dev/null || cp -f "$ODOOM_DIR/star_api.so" "$ODOOM_DIR/libstar_api.so"
  elif [[ -f "$ODOOM_DIR/star_api.dylib" ]]; then
    ln -sf star_api.dylib "$ODOOM_DIR/libstar_api.dylib" 2>/dev/null || cp -f "$ODOOM_DIR/star_api.dylib" "$ODOOM_DIR/libstar_api.dylib"
  elif [[ -f "$HERE/libstar_api.so" ]]; then
    cp -f "$HERE/libstar_api.so" "$ODOOM_DIR/"
  elif [[ -f "$HERE/star_api.so" ]]; then
    cp -f "$HERE/star_api.so" "$ODOOM_DIR/libstar_api.so"
  elif [[ -f "$HERE/libstar_api.dylib" ]]; then
    cp -f "$HERE/libstar_api.dylib" "$ODOOM_DIR/"
  elif [[ -f "$HERE/star_api.dylib" ]]; then
    cp -f "$HERE/star_api.dylib" "$ODOOM_DIR/libstar_api.dylib"
  else
    echo "ERROR: libstar_api.so not found in $ODOOM_DIR or $HERE. Run ./BUILD_ODOOM.sh first to build and copy the STAR API library." >&2
    exit 1
  fi
fi
# So the loader finds libstar_api next to the executable (Linux/macOS don't search . by default)
export LD_LIBRARY_PATH="$ODOOM_DIR${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
[[ "$(uname -s)" == "Darwin" ]] && export DYLD_LIBRARY_PATH="$ODOOM_DIR${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"

# Ensure ODOOM can find IWADs (doom.wad, doom2.wad, heretic.wad, etc.)
# Create ~/.local/share/games/odoom (and parents) so user can copy wads there; add both locations to odoom.ini
ODOOM_CONFIG="$HOME/.config/odoom.ini"
ODOOM_DATA="$HOME/.local/share/games/odoom"
mkdir -p "$(dirname "$ODOOM_CONFIG")" "$ODOOM_DATA"
if [[ ! -f "$ODOOM_CONFIG" ]]; then
  printf '[IWADSearch.Directories]\nPath=%s\nPath=%s\n' "$ODOOM_DIR" "$ODOOM_DATA" > "$ODOOM_CONFIG"
else
  # Ensure build dir and data dir are in IWAD search (idempotent)
  for want in "$ODOOM_DIR" "$ODOOM_DATA"; do
    if ! grep -qF "Path=$want" "$ODOOM_CONFIG" 2>/dev/null; then
      if ! grep -q '\[IWADSearch.Directories\]' "$ODOOM_CONFIG" 2>/dev/null; then
        echo "" >> "$ODOOM_CONFIG"
        echo "[IWADSearch.Directories]" >> "$ODOOM_CONFIG"
      fi
      echo "Path=$want" >> "$ODOOM_CONFIG"
    fi
  done
fi

# Pre-launch check: ensure at least one IWAD is in search paths (commented out so we can still launch and add IWAD later)
# IWAD_NAMES="doom.wad doom2.wad heretic.wad hexen.wad strife1.wad freedoom1.wad freedoom2.wad"
# HAS_IWAD=0
# for wad in $IWAD_NAMES; do
#   if [[ -f "$ODOOM_DIR/$wad" || -f "$ODOOM_DATA/$wad" ]]; then
#     HAS_IWAD=1
#     break
#   fi
# done
# if [[ $HAS_IWAD -eq 0 ]]; then
#   echo ""
#   echo "  ERROR: No game IWAD found. ODOOM needs one of:"
#   echo "    doom.wad, doom2.wad, heretic.wad, hexen.wad, strife1.wad, freedoom1.wad, freedoom2.wad"
#   echo ""
#   echo "  Put one of these files in either:"
#   echo "    1. $ODOOM_DIR"
#   echo "    2. $ODOOM_DATA"
#   echo ""
#   echo "  Or add your own IWAD folder in: $ODOOM_CONFIG"
#   echo "    Under [IWADSearch.Directories] add: Path=/path/to/your/wads"
#   echo ""
#   exit 1
# fi

echo "Launching ODOOM..."
cd "$ODOOM_DIR"
exec ./"$(basename "$ODOOM_EXE")"
