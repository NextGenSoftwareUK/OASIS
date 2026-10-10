#!/usr/bin/env bash
# ODuke3D-RT - Raze (ODuke3D-RT) + OASIS STAR API, the ODOOM/OQuake way via OGLib/oglib_game.h.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OMNIVERSE="$(cd "$HERE/../.." && pwd)"
RAZE_SRC="${RAZE_SRC:-$HOME/Source/ODuke3D-RT}"
OGENGINECLIENT="$OMNIVERSE/OGEngineClient"
OGLIB="$OMNIVERSE/OGLib"
INTEGRATION="$HERE/../OShadowWarrior"

if [[ ! -f "$RAZE_SRC/source/core/gamecontrol.cpp" ]]; then
  echo "[ODuke3D-RT] Raze source not found at $RAZE_SRC (set RAZE_SRC to override)"
  exit 1
fi

echo "[ODuke3D-RT] Installing OASIS integration into $RAZE_SRC/source/core ..."
mkdir -p "$RAZE_SRC/source/core/oasis"
cp -f "$INTEGRATION/raze_ogengine_integration.cpp" "$INTEGRATION/raze_ogengine_integration.h" "$RAZE_SRC/source/core/"
for f in ogengine.h ogengine_sync.h ogengine_sync.c; do cp -f "$OGENGINECLIENT/$f" "$RAZE_SRC/source/core/oasis/"; done
for f in oglib_game.h oglib_config.h oglib_edge.h oglib_json.h oglib_str.h; do cp -f "$OGLIB/$f" "$RAZE_SRC/source/core/oasis/"; done

echo "[ODuke3D-RT] Building Raze with OASIS_STAR_API=ON..."
cmake -S "$RAZE_SRC" -B "$RAZE_SRC/build" -DCMAKE_BUILD_TYPE=Release -DOASIS_STAR_API=ON "-DOGENGINE_DIR=$OGENGINECLIENT"
cmake --build "$RAZE_SRC/build" --parallel
echo "[ODuke3D-RT] Done: $RAZE_SRC/build/raze"
