#!/usr/bin/env bash
# ODOOM - Install/copy only the Editor (Ultimate Doom Builder) into build/Editor.
# Linux equivalent of INSTALL_EDITOR.bat. Use when you only want to refresh the Editor without a full build.
# Requires: Ultimate Doom Builder build tree (set ULTIMATE_DOOM_BUILDER_BUILD or default below).

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

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ODOOM_INTEGRATION="$HERE"
ULTIMATE_DOOM_BUILDER_BUILD="${ULTIMATE_DOOM_BUILDER_BUILD:-$HOME/Source/UltimateDoomBuilder/Build}"

echo ""
echo "[ODOOM] Editor-only install: copying Ultimate Doom Builder into build/Editor..."
echo ""

mkdir -p "$ODOOM_INTEGRATION/build/Editor"

# On Linux, UDB may ship Builder (or a launcher); copy the tree if present.
if [[ ! -f "$ODOOM_INTEGRATION/build/Editor/Builder" && ! -f "$ODOOM_INTEGRATION/build/Editor/Builder.exe" ]]; then
  if [[ -d "$ULTIMATE_DOOM_BUILDER_BUILD" ]]; then
    echo "[ODOOM] Copying Ultimate Doom Builder into build/Editor..."
    cp -R "$ULTIMATE_DOOM_BUILDER_BUILD"/* "$ODOOM_INTEGRATION/build/Editor/" 2>/dev/null || cp -R "$ULTIMATE_DOOM_BUILDER_BUILD"/. "$ODOOM_INTEGRATION/build/Editor/" 2>/dev/null || true
    if [[ -f "$ODOOM_INTEGRATION/build/Editor/Builder" ]]; then
      chmod +x "$ODOOM_INTEGRATION/build/Editor/Builder"
    fi
  else
    echo "[ODOOM] ERROR: Ultimate Doom Builder not found at $ULTIMATE_DOOM_BUILDER_BUILD"
    echo "        Set ULTIMATE_DOOM_BUILDER_BUILD or put Editor files in build/Editor manually."
    exit 1
  fi
else
  echo "[ODOOM] build/Editor already contains Builder (or Builder.exe)."
fi

if command -v pwsh &>/dev/null && [[ -f "$ODOOM_INTEGRATION/copy_builder_native.ps1" ]]; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$ODOOM_INTEGRATION/copy_builder_native.ps1" -EditorDir "$ODOOM_INTEGRATION/build/Editor" -UltimateDoomBuilderRoot "${ULTIMATE_DOOM_BUILDER_BUILD%/Build}" || true
fi

echo ""
echo "[ODOOM] Editor install done: $ODOOM_INTEGRATION/build/Editor"
echo ""
