#!/usr/bin/env bash
# Build the STAR CLI (dotnet build).
set -e


SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/../include/pause_on_exit.inc.sh"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_DIR="$REPO_ROOT/STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
CONFIG="${1:-Release}"

echo "Building STAR CLI ($CONFIG) in $PROJECT_DIR"
dotnet build "$PROJECT_DIR/NextGenSoftware.OASIS.STAR.CLI.csproj" -c "$CONFIG"
echo "Build complete. Run with: Scripts/STAR CLI/RUN_STAR_CLI.sh"
exit $?
