#!/usr/bin/env bash
# Run OQuake. Builds if not already built, then launches.
# Set OQUAKE_BASEDIR to Quake game data (id1 with pak0.pak, pak1.pak). Default: Steam Quake path.

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VKQUAKE_SRC="${VKQUAKE_SRC:-$HOME/Source/vkQuake}"
if [[ "$(uname -s)" == "Darwin" ]]; then
  OQUAKE_BASEDIR="${OQUAKE_BASEDIR:-$HOME/Library/Application Support/Steam/steamapps/common/Quake}"
else
  OQUAKE_BASEDIR="${OQUAKE_BASEDIR:-$HOME/.steam/steam/steamapps/common/Quake}"
fi
QUAKE_ENGINE_EXE=""

if [[ -x "$HERE/build/OQUAKE" ]]; then
  QUAKE_ENGINE_EXE="$HERE/build/OQUAKE"
elif [[ -f "$VKQUAKE_SRC/build/vkquake" && -x "$VKQUAKE_SRC/build/vkquake" ]]; then
  QUAKE_ENGINE_EXE="$VKQUAKE_SRC/build/vkquake"
elif [[ -f "$VKQUAKE_SRC/build/vkquake.exe" && -x "$VKQUAKE_SRC/build/vkquake.exe" ]]; then
  QUAKE_ENGINE_EXE="$VKQUAKE_SRC/build/vkquake.exe"
fi

if [[ -z "$QUAKE_ENGINE_EXE" ]]; then
  echo "OQuake not built. Building..."
  exec "$HERE/BUILD_OQUAKE.sh" run
fi

# Optional: copy config to basedir so exec config.cfg finds it
if [[ -f "$HERE/build/config.cfg" && -d "$OQUAKE_BASEDIR" ]]; then
  cp -f "$HERE/build/config.cfg" "$OQUAKE_BASEDIR/config.cfg" 2>/dev/null || true
fi

echo "Launching OQuake (-basedir $OQUAKE_BASEDIR)..."
cd "$(dirname "$QUAKE_ENGINE_EXE")"
exec ./"$(basename "$QUAKE_ENGINE_EXE")" -basedir "$OQUAKE_BASEDIR"
