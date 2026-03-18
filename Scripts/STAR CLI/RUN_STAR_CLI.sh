#!/usr/bin/env bash
# Run the STAR CLI. Uses published binary if present (publish/linux-x64/star or publish/osx-*),
# otherwise runs via dotnet run.
set -e


SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/../include/pause_on_exit.inc.sh"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_DIR="$REPO_ROOT/STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
PUBLISH_DIR="$PROJECT_DIR/publish"

if [ -x "$PUBLISH_DIR/linux-x64/star" ]; then
  cd "$PUBLISH_DIR/linux-x64"
  ./star "$@"
  exit $?
fi
if [ -x "$PUBLISH_DIR/linux-arm64/star" ]; then
  cd "$PUBLISH_DIR/linux-arm64"
  ./star "$@"
  exit $?
fi
if [ -x "$PUBLISH_DIR/osx-arm64/star" ]; then
  cd "$PUBLISH_DIR/osx-arm64"
  ./star "$@"
  exit $?
fi
if [ -x "$PUBLISH_DIR/osx-x64/star" ]; then
  cd "$PUBLISH_DIR/osx-x64"
  ./star "$@"
  exit $?
fi

cd "$PROJECT_DIR"
dotnet run -c Release -- "$@"
exit $?
