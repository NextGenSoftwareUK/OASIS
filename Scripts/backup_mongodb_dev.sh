#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
if command -v pwsh >/dev/null 2>&1; then
    powershell_command="pwsh"
elif command -v powershell.exe >/dev/null 2>&1; then
    powershell_command="powershell.exe"
else
    echo "PowerShell 7 (pwsh) is required." >&2
    exit 1
fi

"${powershell_command}" -NoProfile -File "${script_dir}/backup_mongodb_dev.ps1" "$@"
