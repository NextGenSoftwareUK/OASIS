#!/usr/bin/env bash
# Run the Demo Quest Seed to seed the STAR API with demo quests for ODOOM/OQuake.
# Linux/macOS equivalent of RUN_DEMO_QUEST_SEED.bat / run_demo_quest_seed.ps1.
# Ensure STAR API (WEB5) and OASIS API (WEB4) are running. Optional: -NoBuild / --no-build
#
# With PowerShell 7+ installed, forwards to run_demo_quest_seed.ps1 for identical behavior.
# Otherwise runs dotnet build / dotnet run here.

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
cd "$SCRIPT_DIR"

PS1="$SCRIPT_DIR/run_demo_quest_seed.ps1"
if command -v pwsh >/dev/null 2>&1 && [[ -f "$PS1" ]]; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$PS1" "$@"
  exit $?
fi

CONFIGURATION="Release"
NO_BUILD=0
remaining=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    -NoBuild|--no-build) NO_BUILD=1; shift ;;
    -Configuration|-c)
      CONFIGURATION="${2:?missing value after $1}"
      shift 2
      ;;
    *)
      remaining+=("$1")
      shift
      ;;
  esac
done
if [[ ${#remaining[@]} -gt 0 ]]; then
  echo "Unknown argument(s): ${remaining[*]}" >&2
  echo "Usage: $0 [-Configuration|-c Release] [-NoBuild|--no-build]" >&2
  exit 2
fi

STARAPI_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$STARAPI_ROOT/TestProjects/DemoQuestSeed/DemoQuestSeed.csproj"
if [[ ! -f "$PROJECT" ]]; then
  echo "ERROR: DemoQuestSeed project not found at: $PROJECT" >&2
  exit 1
fi

echo ""
echo "=============================================="
echo "  OASIS STAR API - Demo Quest Seed"
echo "=============================================="
echo ""

cd "$STARAPI_ROOT"
if [[ "$NO_BUILD" -eq 0 ]]; then
  echo "Building DemoQuestSeed ($CONFIGURATION)..."
  dotnet build "$PROJECT" -c "$CONFIGURATION" --nologo -v q
  echo ""
fi

echo "Running Demo Quest Seed..."
dotnet run --project "$PROJECT" -c "$CONFIGURATION" --no-build
exit $?
