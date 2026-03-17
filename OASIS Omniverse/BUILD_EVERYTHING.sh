#!/usr/bin/env bash
# Build STARAPIClient, ODOOM and OQuake with no prompts and no launch.
# Linux/macOS equivalent of BUILD EVERYTHING.bat.
# Use RUN_ODOOM.sh or RUN_OQUAKE.sh to launch after a successful build.

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

if [[ -f "$ROOT/run_oasis_header.sh" ]]; then
  bash "$ROOT/run_oasis_header.sh" BUILD
fi

echo "[1/3] Building and deploying STARAPIClient..."
bash "$ROOT/BUILD_AND_DEPLOY_STAR_CLIENT.sh"
echo ""

echo "[2/3] Building ODOOM (batch, no prompts)..."
bash "$ROOT/ODOOM/BUILD_ODOOM.sh" batch nosprites
echo ""

echo "[3/3] Building OQuake (batch, no prompts)..."
bash "$ROOT/OQuake/BUILD_OQUAKE.sh" batch
echo ""

if [[ -f "$ROOT/show_oasis_header.ps1" ]] && command -v pwsh &>/dev/null; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Success -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" -Message2 "Run RUN_ODOOM.sh or RUN_OQUAKE.sh to launch."
fi

echo "BUILD EVERYTHING completed successfully."
