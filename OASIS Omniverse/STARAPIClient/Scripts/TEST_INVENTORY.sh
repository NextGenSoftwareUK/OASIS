#!/usr/bin/env bash
# Inventory test: init, auth, get inventory, has_item, add_item, sync, send-to-avatar, send-to-clan.
# Linux/macOS equivalent of TEST_INVENTORY.bat (requires PowerShell 7+ for compile_and_test_inventory.ps1).
# Test runs with: BaseUrl http://localhost:5556, Username dellams. Avatar ID from API (see test output).
# Optional: pass through script args, e.g. -SendAvatarTarget "username" -SendClanName "ClanName"

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

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PS1="$SCRIPT_DIR/compile_and_test_inventory.ps1"

if ! command -v pwsh >/dev/null 2>&1; then
  echo "ERROR: pwsh (PowerShell 7+) is required to run compile_and_test_inventory.ps1." >&2
  echo "Install from https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-linux" >&2
  exit 1
fi
if [[ ! -f "$PS1" ]]; then
  echo "ERROR: compile_and_test_inventory.ps1 not found at $PS1" >&2
  exit 1
fi

echo ""
echo "Test will use: BaseUrl=http://localhost:8888  Username=dellams  (avatar from API - see output below)"
echo ""
read -r -p "Rebuild STAR API client first? [Y/n]: " REBUILD
REBUILD=${REBUILD:-Y}
REBUILD_LOWER=$(printf '%s' "$REBUILD" | tr '[:upper:]' '[:lower:]')

# Use -RebuildClient:0 / :1 so bash does not expand $false / $true
if [[ "$REBUILD_LOWER" == "n" ]]; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$PS1" \
    -BaseUrl 'http://localhost:5556' -Username 'dellams' -Password 'test!' -RebuildClient:0 "$@"
else
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$PS1" \
    -BaseUrl 'http://localhost:8888' -Username 'dellams' -Password 'test!' -RebuildClient:1 "$@"
fi
