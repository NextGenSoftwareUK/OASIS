#!/usr/bin/env bash
# Build OGEngineClient and all 22 OGEngine game integrations with no prompts.
# Linux/macOS equivalent of BUILD EVERYTHING.bat.

set -e

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

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

if [[ -f "$ROOT/run_oasis_header.sh" ]]; then
  bash "$ROOT/run_oasis_header.sh" BUILD
fi

echo "[1/22] Building and deploying OGEngineClient..."
bash "$ROOT/BUILD_AND_DEPLOY_STAR_CLIENT.sh"
echo ""

echo "[2/22] Building ODOOM (batch, no prompts)..."
bash "$ROOT/OGames/ODOOM/BUILD_ODOOM.sh" batch nosprites
echo ""

echo "[3/22] Building OQuake (batch, no prompts)..."
bash "$ROOT/OGames/OQuake/BUILD_OQUAKE.sh" batch
echo ""

echo "[4/22] Building OQuake2 (batch, no prompts)..."
bash "$ROOT/OGames/OQuake2/BUILD_OQUAKE2.sh" batch
echo ""

echo "[5/22] Building OQuake2-RTX (batch, no prompts)..."
bash "$ROOT/OGames/OQuake2-RTX/BUILD_OQUAKE2RTX.sh" batch
echo ""

echo "[6/22] Building OQuake3 - Quake3e (batch, no prompts)..."
bash "$ROOT/OGames/OQuake3/BUILD_OQUAKE3.sh" batch
echo ""

echo "[7/22] Building ODOOM3-BFG (batch, no prompts)..."
bash "$ROOT/OGames/ODOOM3-BFG/BUILD_ODOOM3BFG.sh" batch
echo ""

echo "[8/22] Building ODOOM3 - dhewm3 (batch, no prompts)..."
bash "$ROOT/OGames/ODOOM3/BUILD_ODOOM3.sh" batch
echo ""

echo "[9/22] Building ODuke3D - EDuke32 (batch, no prompts)..."
bash "$ROOT/OGames/ODuke3D/BUILD_ODUKE3D.sh" batch
echo ""

echo "[10/22] Building ODuke3D-RT - Duke-RT (batch, no prompts)..."
bash "$ROOT/OGames/ODuke3D-RT/BUILD_ODUKE3DRT.sh" batch
echo ""

echo "[11/22] Building OWolf3D - ECWolf (batch, no prompts)..."
bash "$ROOT/OGames/OWolf3D/BUILD_OWOLF3D.sh" batch
echo ""

echo "[12/22] Building OHeretic - UZDoom (batch, no prompts)..."
bash "$ROOT/OGames/OHeretic/BUILD_OHERETIC.sh" batch
echo ""

echo "[13/22] Building OHexen - UZDoom (batch, no prompts)..."
bash "$ROOT/OGames/OHexen/BUILD_OHEXEN.sh" batch
echo ""

echo "[14/22] Building OShadowWarrior - Raze (batch, no prompts)..."
bash "$ROOT/OGames/OShadowWarrior/BUILD_OSHADOWWARRIOR.sh" batch
echo ""

echo "[15/22] Building OShadowWarriorRT - Duke-RT (batch, no prompts)..."
bash "$ROOT/OGames/OShadowWarriorRT/BUILD_OSHADOWWARRIORRT.sh" batch
echo ""

echo "[16/22] Building OBlood - Raze (batch, no prompts)..."
bash "$ROOT/OGames/OBlood/BUILD_OBLOOD.sh" batch
echo ""

echo "[17/22] Building OExhumed - Raze (batch, no prompts)..."
bash "$ROOT/OGames/OExhumed/BUILD_OEXHUMED.sh" batch
echo ""

echo "[18/22] Building OStrife - UZDoom (batch, no prompts)..."
bash "$ROOT/OGames/OStrife/BUILD_OSTRIFE.sh" batch
echo ""

echo "[19/22] Building ODoom64 - Doom64 EX+ (batch, no prompts)..."
bash "$ROOT/OGames/ODoom64/BUILD_ODOOM64.sh" batch
echo ""

echo "[20/22] Building OHexenII - uhexen2 (batch, no prompts)..."
bash "$ROOT/OGames/OHexenII/BUILD_OHEXEN2.sh" batch
echo ""

echo "[21/22] Building ORtCW - iortcw (batch, no prompts)..."
bash "$ROOT/OGames/ORtCW/BUILD_ORTCW.sh" batch
echo ""

echo "[22/22] Building OMorrowind - OpenMW (batch, no prompts)..."
bash "$ROOT/OGames/OMorrowind/BUILD_OMORROWIND.sh" batch
echo ""

if [[ -f "$ROOT/show_oasis_header.ps1" ]] && command -v pwsh &>/dev/null; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Success \
    -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" \
    -Message2 "Run RUN_ODOOM.sh, RUN_OQUAKE.sh, etc. to launch."
fi

echo "BUILD EVERYTHING completed successfully."
