#!/usr/bin/env bash
# Build STARAPIClient, ODOOM, OQuake, ODOOM3-BFG, ODOOM3, ODuke3D, and ODuke3D-RT with no prompts.
# Linux/macOS equivalent of BUILD EVERYTHING.bat.
# Use RUN_ODOOM.sh, RUN_OQUAKE.sh, RUN_ODOOM3BFG.sh, RUN_ODOOM3.sh, RUN_ODUKE3D.sh,
# or RUN_ODUKE3DRT.sh to launch after a successful build.

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

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

if [[ -f "$ROOT/run_oasis_header.sh" ]]; then
  bash "$ROOT/run_oasis_header.sh" BUILD
fi

echo "[1/7] Building and deploying STARAPIClient..."
bash "$ROOT/BUILD_AND_DEPLOY_STAR_CLIENT.sh"
echo ""

echo "[2/7] Building ODOOM (batch, no prompts)..."
bash "$ROOT/ODOOM/BUILD_ODOOM.sh" batch nosprites
echo ""

echo "[3/7] Building OQuake (batch, no prompts)..."
bash "$ROOT/OQuake/BUILD_OQUAKE.sh" batch
echo ""

echo "[4/7] Building ODOOM3-BFG (batch, no prompts)..."
bash "$ROOT/ODOOM3-BFG/BUILD_ODOOM3BFG.sh" batch
echo ""

echo "[5/7] Building ODOOM3 - dhewm3 (batch, no prompts)..."
bash "$ROOT/ODOOM3/BUILD_ODOOM3.sh" batch
echo ""

echo "[6/7] Building ODuke3D - EDuke32 (batch, no prompts)..."
bash "$ROOT/ODuke3D/BUILD_ODUKE3D.sh" batch
echo ""

echo "[7/7] Building ODuke3D-RT - Duke-RT (batch, no prompts)..."
bash "$ROOT/ODuke3D-RT/BUILD_ODUKE3DRT.sh" batch
echo ""

if [[ -f "$ROOT/show_oasis_header.ps1" ]] && command -v pwsh &>/dev/null; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Success \
    -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" \
    -Message2 "Run RUN_ODOOM.sh, RUN_OQUAKE.sh, RUN_ODOOM3BFG.sh, RUN_ODOOM3.sh, RUN_ODUKE3D.sh, or RUN_ODUKE3DRT.sh to launch."
fi

echo "BUILD EVERYTHING completed successfully."
