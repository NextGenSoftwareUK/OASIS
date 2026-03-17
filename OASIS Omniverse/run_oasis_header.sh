#!/usr/bin/env bash
# Print OASIS banner (coloured header + slogan). Centre-aligned, same design as show_oasis_header.ps1.
# Usage: ./run_oasis_header.sh [ ODOOM | OQUAKE | BUILD | RUN_ODOOM | RUN_OQUAKE ] [ version ]
#   version = optional (shown as "Title  vversion" for ODOOM/OQUAKE/BUILD).

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODE="${1:-}"
VERSION="${2:-}"
WIDTH=60
SLOGAN="Enabling full interoperable games across the OASIS Omniverse!"

# ANSI colours (same style as PowerShell: cyan title, dark gray subtitle, magenta slogan)
C_CYAN='\033[36m'
C_DARKGRAY='\033[90m'
C_MAGENTA='\033[35m'
C_RESET='\033[0m'

center() {
  local s="$1"
  local len="${#s}"
  local pad=0
  if [[ $len -lt $WIDTH ]]; then
    pad=$(( (WIDTH - len) / 2 ))
  fi
  printf "  %*s%s\n" $pad "" "$s"
}

print_banner_bash() {
  local title="$1"
  local subtitle="${2:-}"
  local show_slogan="${3:-1}"
  echo ""
  printf "  ${C_CYAN}%s${C_RESET}\n" "$(printf '=%.0s' $(seq 1 $WIDTH))"
  printf "${C_CYAN}%s${C_RESET}\n" "$(center "$title")"
  if [[ -n "$subtitle" ]]; then
    printf "${C_DARKGRAY}%s${C_RESET}\n" "$(center "$subtitle")"
  fi
  printf "  ${C_CYAN}%s${C_RESET}\n" "$(printf '=%.0s' $(seq 1 $WIDTH))"
  if [[ "$show_slogan" == "1" ]]; then
    printf "${C_MAGENTA}%s${C_RESET}\n" "$(center "$SLOGAN")"
  fi
  echo ""
}

# Resolve title and optional subtitle from mode + version
TITLE=""
SUB=""
SHOW_SLOGAN=1
case "$MODE" in
  ODOOM)
    TITLE="O A S I S   O D O O M"
    [[ -n "$VERSION" ]] && TITLE="$TITLE  v$VERSION"
    ;;
  OQUAKE)
    TITLE="O A S I S   O Q U A K E"
    [[ -n "$VERSION" ]] && TITLE="$TITLE  v$VERSION"
    ;;
  BUILD)
    TITLE="T H E   O A S I S   O M N I V E R S E"
    [[ -n "$VERSION" ]] && TITLE="$TITLE  v$VERSION"
    SUB="STARAPIClient + ODOOM + OQuake"
    ;;
  RUN_ODOOM)
    TITLE="O A S I S   O D O O M   |   Run"
    ;;
  RUN_OQUAKE)
    TITLE="O A S I S   O Q U A K E   |   Run"
    ;;
  *)
    exit 0
    ;;
esac

# Prefer PowerShell banner when available (matches Windows exactly)
if [[ -f "$ROOT/show_oasis_header.ps1" ]] && command -v pwsh &>/dev/null; then
  case "$MODE" in
    RUN_ODOOM|RUN_OQUAKE)
      pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Title "$TITLE" -Subtitle ""
      ;;
    BUILD)
      pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Title "T H E   O A S I S   O M N I V E R S E" -Subtitle "$SUB" -Version "$VERSION"
      ;;
    ODOOM)
      pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Title "O A S I S   O D O O M" -Subtitle "" -Version "$VERSION"
      ;;
    OQUAKE)
      pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Title "O A S I S   O Q U A K E" -Subtitle "" -Version "$VERSION"
      ;;
    *)
      pwsh -NoProfile -ExecutionPolicy Bypass -File "$ROOT/show_oasis_header.ps1" -Title "$TITLE" -Subtitle ""
      ;;
  esac
else
  print_banner_bash "$TITLE" "$SUB" "$SHOW_SLOGAN"
fi
