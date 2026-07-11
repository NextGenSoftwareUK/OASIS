#!/usr/bin/env bash
# Emergency disk cleanup (Ubuntu/Linux): NuGet + XDG cache + Cursor heavy artifacts.
#
# Does NOT delete all of ~/.local/share (that would remove GNOME keyrings, app data, etc.).
#
# Defaults (aggressive): clear NuGet global-packages + ~/.nuget/packages, remove Cursor
#   globalStorage state.vscdb (+ wal/shm) and backup, remove ~/.local/share/JetBrains, plus
#   ~/.cache and NuGet http-cache/temp/plugins and Cursor UI caches.
#
# Usage:
#   ./Scripts/cleanup-emergency-space.sh
#   ./Scripts/cleanup-emergency-space.sh --dry-run
#   ./Scripts/cleanup-emergency-space.sh --keep-nuget-packages   # skip global-packages / ~/.nuget/packages
#   ./Scripts/cleanup-emergency-space.sh --keep-cursor-global-db # keep state.vscdb* (only trim backup + caches)
#   ./Scripts/cleanup-emergency-space.sh --keep-jetbrains-local  # keep ~/.local/share/JetBrains
#   ./Scripts/cleanup-emergency-space.sh --force   # skip "Cursor/Rider running" check
#
# Close Cursor before running (and Rider recommended). Automation: OASIS_SCRIPT_NO_PAUSE=1

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/include/pause_on_exit.inc.sh"

DRY_RUN=0
NUGET_PACKAGES=1
CURSOR_DB_RESET=1
JETBRAINS_LOCAL=1
FORCE=0

usage() {
  cat <<'EOF'
Emergency disk cleanup (Ubuntu/Linux): NuGet + XDG cache + Cursor + JetBrains local data.

Defaults: clear global NuGet packages, reset Cursor globalStorage DB, remove ~/.local/share/JetBrains.

Options:
  --dry-run                  Print actions only
  --keep-nuget-packages      Do not clear global-packages / ~/.nuget/packages
  --keep-cursor-global-db    Do not remove state.vscdb (+ wal/shm); still removes .backup + UI caches
  --keep-jetbrains-local     Do not remove ~/.local/share/JetBrains
  --force                    Proceed even if cursor/rider processes are running (risky)
  -h, --help                 This help
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run) DRY_RUN=1 ;;
    --keep-nuget-packages) NUGET_PACKAGES=0 ;;
    --keep-cursor-global-db) CURSOR_DB_RESET=0 ;;
    --keep-jetbrains-local) JETBRAINS_LOCAL=0 ;;
    --nuget-packages) NUGET_PACKAGES=1 ;;               # legacy: same as default
    --cursor-reset-global-db) CURSOR_DB_RESET=1 ;;       # legacy: same as default
    --jetbrains-local) JETBRAINS_LOCAL=1 ;;             # legacy: same as default
    --force) FORCE=1 ;;
    -h|--help) usage; exit 0 ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
  shift
done

log() { echo "[cleanup-emergency-space] $*"; }

run_rm_rf() {
  local path="$1"
  if [[ ! -e "$path" ]]; then
    return 0
  fi
  if [[ $DRY_RUN -eq 1 ]]; then
    log "dry-run: rm -rf $path"
  else
    rm -rf "$path"
  fi
}

empty_dir_contents() {
  local dir="$1"
  [[ -d "$dir" ]] || return 0
  if [[ $DRY_RUN -eq 1 ]]; then
    log "dry-run: empty $dir/*"
    return 0
  fi
  find "$dir" -mindepth 1 -maxdepth 1 -exec rm -rf {} +
}

cursor_global_dir="${HOME}/.config/Cursor/User/globalStorage"

if [[ $FORCE -eq 0 ]]; then
  if pgrep -u "${USER:-}" -x cursor >/dev/null 2>&1; then
    log "Cursor appears to be running (process name: cursor). Close it first, or re-run with --force (risky)."
    exit 1
  fi
  if pgrep -u "${USER:-}" -x rider >/dev/null 2>&1; then
    log "Rider appears to be running. Quit Rider first for a clean JetBrains cache clear, or use --force."
    exit 1
  fi
fi

log "Starting (DRY_RUN=$DRY_RUN)..."

# --- NuGet (CLI + XDG locations) ---
# Note: `dotnet nuget locals all --clear` would clear everything at once; we clear per-local for keep-* flags.
if command -v dotnet >/dev/null 2>&1; then
  _nuget_local_clear() {
    local name="$1"
    if [[ $DRY_RUN -eq 1 ]]; then
      log "dry-run: dotnet nuget locals $name --clear"
    else
      dotnet nuget locals "$name" --clear >/dev/null 2>&1 || log "warning: dotnet nuget locals $name --clear failed (ok if folder missing)."
    fi
  }
  _nuget_local_clear http-cache
  _nuget_local_clear temp
  _nuget_local_clear plugins-cache
  if [[ $NUGET_PACKAGES -eq 1 ]]; then
    log "Clearing NuGet global-packages via dotnet + ~/.nuget/packages ..."
    _nuget_local_clear global-packages
    empty_dir_contents "${HOME}/.nuget/packages"
  fi
else
  log "dotnet not in PATH; skipping dotnet nuget locals --clear"
  if [[ $NUGET_PACKAGES -eq 1 ]]; then
    log "Removing ${HOME}/.nuget/packages (no dotnet CLI)..."
    empty_dir_contents "${HOME}/.nuget/packages"
  fi
fi

empty_dir_contents "${HOME}/.local/share/NuGet/http-cache"
run_rm_rf "${HOME}/.local/share/NuGet/v3-cache"

# --- XDG cache (includes JetBrains Rider cache under ~/.cache/JetBrains) ---
if [[ -d "${HOME}/.cache" ]]; then
  log "Emptying ${HOME}/.cache ..."
  empty_dir_contents "${HOME}/.cache"
fi

# --- ~/.local/share: only safe NuGet + optional JetBrains ---
if [[ $JETBRAINS_LOCAL -eq 1 ]]; then
  log "Removing ${HOME}/.local/share/JetBrains (Rider/IDE local data)..."
  run_rm_rf "${HOME}/.local/share/JetBrains"
fi

# --- Cursor (VS Code–style paths) ---
log "Trimming Cursor caches under ${HOME}/.config/Cursor ..."
empty_dir_contents "${HOME}/.config/Cursor/Cache"
empty_dir_contents "${HOME}/.config/Cursor/GPUCache"
empty_dir_contents "${HOME}/.config/Cursor/CachedData"
empty_dir_contents "${HOME}/.config/Cursor/logs"
empty_dir_contents "${HOME}/.config/Cursor/DawnWebGPUCache"
empty_dir_contents "${HOME}/.config/Cursor/DawnGraphiteCache"

if [[ -d "$cursor_global_dir" ]]; then
  run_rm_rf "${cursor_global_dir}/state.vscdb.backup"
  if [[ $CURSOR_DB_RESET -eq 1 ]]; then
    log "Removing Cursor global state DB (state.vscdb*) — chat/extension global state reset."
    run_rm_rf "${cursor_global_dir}/state.vscdb"
    run_rm_rf "${cursor_global_dir}/state.vscdb-wal"
    run_rm_rf "${cursor_global_dir}/state.vscdb-shm"
  else
    log "Keeping ${cursor_global_dir}/state.vscdb* (--keep-cursor-global-db)."
  fi
fi

log "Done."
if [[ $DRY_RUN -eq 1 ]]; then
  log "This was a dry run; nothing was deleted."
fi
