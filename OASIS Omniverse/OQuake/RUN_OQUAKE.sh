#!/usr/bin/env bash
# Run OQuake. Builds if not already built, then launches.
# Linux equivalent of "RUN OQUAKE.bat". Set OQUAKE_BASEDIR to Quake game data (id1 with pak0.pak, pak1.pak). Default: Steam Quake path.
# Optional: create ~/.config/oquake/basedir (or OQuake/.quake_basedir) with one line = path to Quake, to override auto-detect.


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

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/.." && pwd)"
# Banner (centre-aligned, colours, slogan - same as RUN_ODOOM)
if [[ -f "$OMNIVERSE/run_oasis_header.sh" ]]; then
  bash "$OMNIVERSE/run_oasis_header.sh" RUN_OQUAKE || true
fi

# Default vkQuake tree: sibling of OASIS repo when launched from this OQuake folder; else $HOME/Source/vkQuake.
if [[ -z "${VKQUAKE_SRC:-}" ]]; then
  _vk_sibling="$(cd "$HERE/../.." && pwd)/../vkQuake"
  if [[ -d "$_vk_sibling" ]]; then
    VKQUAKE_SRC="$(cd "$_vk_sibling" && pwd)"
  else
    VKQUAKE_SRC="$HOME/Source/vkQuake"
  fi
fi
VKQUAKE_BIN="${VKQUAKE_BIN:-}"
if [[ "$(uname -s)" == "Darwin" ]]; then
  OQUAKE_BASEDIR="${OQUAKE_BASEDIR:-$HOME/Library/Application Support/Steam/steamapps/common/Quake}"
else
  # Config file overrides (one line = path to Quake folder that contains id1/)
  if [[ -z "${OQUAKE_BASEDIR:-}" && -f "$HOME/.config/oquake/basedir" ]]; then
    read -r OQUAKE_BASEDIR < "$HOME/.config/oquake/basedir"
    OQUAKE_BASEDIR="${OQUAKE_BASEDIR%%#*}"
    OQUAKE_BASEDIR="$(echo "$OQUAKE_BASEDIR" | tr -d '\r' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')"
    [[ -z "${OQUAKE_BASEDIR:-}" ]] && unset OQUAKE_BASEDIR
  fi
  if [[ -z "${OQUAKE_BASEDIR:-}" && -f "$HERE/.quake_basedir" ]]; then
    read -r OQUAKE_BASEDIR < "$HERE/.quake_basedir"
    OQUAKE_BASEDIR="${OQUAKE_BASEDIR%%#*}"
    OQUAKE_BASEDIR="$(echo "$OQUAKE_BASEDIR" | tr -d '\r' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')"
    [[ -z "${OQUAKE_BASEDIR:-}" ]] && unset OQUAKE_BASEDIR
  fi
  # If OQUAKE_BASEDIR not set, try common Steam/Quake paths (Snap layout varies; some have .steam, some .local)
  if [[ -z "${OQUAKE_BASEDIR:-}" ]]; then
    QUAKE_CANDIDATES=(
      "$HOME/snap/steam/common/.steam/steam/steamapps/common/Quake"
      "$HOME/snap/steam/common/.steam/root/steam/steamapps/common/Quake"
      "$HOME/snap/steam/common/.local/share/Steam/steamapps/common/Quake"
      "$HOME/.steam/steam/steamapps/common/Quake"
      "$HOME/.local/share/Steam/steamapps/common/Quake"
    )
    for dir in "${QUAKE_CANDIDATES[@]}"; do
      if [[ -d "$dir/id1" ]]; then
        OQUAKE_BASEDIR="$dir"
        break
      fi
    done
    # If still no basedir, search for any id1 folder containing pak0.pak (any case) under Steam-like trees
    if [[ -z "${OQUAKE_BASEDIR:-}" ]]; then
      while IFS= read -r id1dir; do
        [[ -z "$id1dir" || ! -d "$id1dir" ]] && continue
        for f in "$id1dir"/*; do
          if [[ -f "$f" ]]; then
            base=$(basename "$f")
            if [[ "${base,,}" == "pak0.pak" ]]; then
              OQUAKE_BASEDIR="$(dirname "$id1dir")"
              break 2
            fi
          fi
        done
      done < <(find "$HOME/snap/steam" "$HOME/.steam" "$HOME/.local/share/Steam" -type d -name id1 2>/dev/null)
    fi
    # Last fallback (script will later prompt if id1/gfx.wad still missing)
    [[ -z "${OQUAKE_BASEDIR:-}" ]] && OQUAKE_BASEDIR="$HOME/snap/steam/common/.local/share/Steam/steamapps/common/Quake"
  fi
fi
QUAKE_ENGINE_EXE=""

# Accept either Quake base dir or direct id1 dir.
if [[ -n "${OQUAKE_BASEDIR:-}" ]]; then
  if [[ "$(basename "$OQUAKE_BASEDIR")" == "id1" ]]; then
    OQUAKE_BASEDIR="$(dirname "$OQUAKE_BASEDIR")"
  fi
fi

# Explicit binary override (useful for sanitizer/debug builds).
if [[ -n "$VKQUAKE_BIN" && -x "$VKQUAKE_BIN" ]]; then
  QUAKE_ENGINE_EXE="$VKQUAKE_BIN"
elif [[ -x "$HERE/build/OQUAKE" ]]; then
  QUAKE_ENGINE_EXE="$HERE/build/OQUAKE"
else
  for candidate in \
    "$VKQUAKE_SRC/build-asan/vkquake" \
    "$VKQUAKE_SRC/build/vkquake" \
    "$VKQUAKE_SRC/build-debug/vkquake" \
    "$VKQUAKE_SRC/build-sanitize/vkquake" \
    "$VKQUAKE_SRC/build/vkquake.exe"; do
    if [[ -x "$candidate" ]]; then
      QUAKE_ENGINE_EXE="$candidate"
      break
    fi
  done
fi

if [[ -z "$QUAKE_ENGINE_EXE" ]]; then
  echo "OQuake not built. Building..."
  exec "$HERE/BUILD_OQUAKE.sh" run
fi

# Optional: copy config to basedir so exec config.cfg finds it
if [[ -f "$HERE/build/config.cfg" && -d "$OQUAKE_BASEDIR" ]]; then
  cp -f "$HERE/build/config.cfg" "$OQUAKE_BASEDIR/config.cfg" 2>/dev/null || true
fi

# Anorak HUD face: vkQuake loads loose PNG from game data id1/gfx/ (not from the OQUAKE exe directory).
_oq_face_src=""
if [[ -f "$HERE/Images/face_anorak.png" ]]; then
  _oq_face_src="$HERE/Images/face_anorak.png"
elif [[ -f "$HERE/face_anorak.png" ]]; then
  _oq_face_src="$HERE/face_anorak.png"
elif [[ -f "$HERE/build/id1/gfx/face_anorak.png" ]]; then
  _oq_face_src="$HERE/build/id1/gfx/face_anorak.png"
fi
if [[ -n "$_oq_face_src" && -n "${OQUAKE_BASEDIR:-}" && -d "$OQUAKE_BASEDIR/id1" ]]; then
  mkdir -p "$OQUAKE_BASEDIR/id1/gfx"
  if cp -f "$_oq_face_src" "$OQUAKE_BASEDIR/id1/gfx/face_anorak.png" 2>/dev/null; then
    echo "[OQuake] Synced face_anorak.png -> $OQUAKE_BASEDIR/id1/gfx/"
  fi
fi
unset _oq_face_src

# vkQuake needs gfx.wad in basedir or id1. Steam often has only id1/pak0.pak (gfx.wad is inside the pak).
if [[ -d "$OQUAKE_BASEDIR" ]]; then
  GFX_WAD=""
  [[ -f "$OQUAKE_BASEDIR/gfx.wad" ]] && GFX_WAD="$OQUAKE_BASEDIR/gfx.wad"
  [[ -z "$GFX_WAD" && -f "$OQUAKE_BASEDIR/id1/gfx.wad" ]] && GFX_WAD="$OQUAKE_BASEDIR/id1/gfx.wad"
  # Always try to extract gfx.wad from pak0.pak when missing (extractor looks for id1/pak0.pak under basedir)
  if [[ -z "$GFX_WAD" ]] && command -v python3 &>/dev/null && [[ -f "$HERE/Scripts/extract_gfx_wad.py" ]]; then
    echo "gfx.wad not found; extracting from id1/pak0.pak if present..."
    if python3 "$HERE/Scripts/extract_gfx_wad.py" "$OQUAKE_BASEDIR"; then
      echo "Done."
    fi
  fi
  # Final check: refuse to launch if gfx.wad still missing (avoid confusing engine error)
  if [[ ! -f "$OQUAKE_BASEDIR/gfx.wad" && ! -f "$OQUAKE_BASEDIR/id1/gfx.wad" ]]; then
    echo "  gfx.wad is required but not found in:"
    echo "    $OQUAKE_BASEDIR"
    echo "    $OQUAKE_BASEDIR/id1/"
    if command -v python3 &>/dev/null && [[ -f "$HERE/Scripts/extract_gfx_wad.py" ]]; then
      echo "  Extraction was attempted (id1/pak0.pak may be missing or not contain gfx.wad)."
      echo "  Ensure id1/pak0.pak exists, or copy gfx.wad into one of the paths above."
    else
      echo "  Run: python3 \"$HERE/Scripts/extract_gfx_wad.py\" \"$OQUAKE_BASEDIR\""
      echo "  Or copy gfx.wad into one of those paths."
    fi
    echo "  To use a different Quake install:"
    echo "    OQUAKE_BASEDIR=/path/to/Quake ./RUN_OQUAKE.sh"
    echo "  Or set once:  mkdir -p ~/.config/oquake && echo '/path/to/Quake' > ~/.config/oquake/basedir"
    echo "  (Find path:   find \$HOME -type d -name id1 2>/dev/null | xargs -I{} dirname {})"
    exit 1
  fi
fi

# Ensure runtime can always find STAR native library for source-tree launches
# (e.g. vkQuake build-asan binary outside OQuake/build).
export LD_LIBRARY_PATH="$VKQUAKE_SRC/Quake:$HERE/build:$HERE/Code:${LD_LIBRARY_PATH:-}"

echo "Launching OQuake (-basedir $OQUAKE_BASEDIR)..."
# Use OQuake/build as cwd when present so oasisstar.json / config next to packaged OQUAKE are found,
# even when VKQUAKE_BIN points at e.g. vkQuake/build-asan/vkquake.
_ENGINE_DIR="$(cd "$(dirname "$QUAKE_ENGINE_EXE")" && pwd)"
LAUNCH_EXE="$_ENGINE_DIR/$(basename "$QUAKE_ENGINE_EXE")"
if [[ -d "$HERE/build" ]]; then
  cd "$HERE/build"
else
  cd "$_ENGINE_DIR"
fi
exec "$LAUNCH_EXE" -basedir "$OQUAKE_BASEDIR"
