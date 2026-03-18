#!/usr/bin/env bash
# Start only the WEB4 (ONODE) OASIS API. Linux/macOS equivalent of start_web4_api.bat.
# Uses port 7777 (matches ODOOM/oasisstar.json and start_web4_and_web5_apis.ps1).
# Stops any existing process on port 7777 so dotnet run can bind.

set -e



SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/include/pause_on_exit.inc.sh"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WEB4_PROJECT="$REPO_ROOT/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"
WEB4_DIR="$REPO_ROOT/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
PORT=7777

echo ""
echo "========================================"
echo "Starting WEB4 OASIS API"
echo "========================================"
echo ""

if [[ ! -f "$WEB4_PROJECT" ]]; then
  echo "Error: WEB4 project not found at:"
  echo "  $WEB4_PROJECT"
  exit 1
fi

echo "Checking for existing WEB4 OASIS API on port $PORT..."
if command -v lsof &>/dev/null; then
  pids=$(lsof -ti:$PORT 2>/dev/null) || true
  if [[ -n "$pids" ]]; then
    echo "$pids" | xargs kill -9 2>/dev/null || true
    echo "Stopped existing process on port $PORT."
  fi
elif command -v fuser &>/dev/null; then
  fuser -k $PORT/tcp 2>/dev/null || true
  echo "Port check completed."
fi

echo ""
echo "Starting WEB4 OASIS API on http://localhost:$PORT..."
echo "Press Ctrl+C to stop the API."
echo ""

cd "$WEB4_DIR"
dotnet run --no-launch-profile --project "$WEB4_PROJECT" -c Release --urls "http://localhost:$PORT"
exit $?
