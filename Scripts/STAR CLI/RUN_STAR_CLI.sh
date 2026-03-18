#!/usr/bin/env bash
# Run the STAR CLI. Uses published binary if present (publish/linux-x64/star or publish/osx-*),
# otherwise runs via dotnet run.
# Run from repo root: Scripts/STAR CLI/RUN_STAR_CLI.sh   or   ./Scripts/STAR CLI/RUN_STAR_CLI.sh -- version
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_DIR="$REPO_ROOT/STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
PUBLISH_DIR="$PROJECT_DIR/publish"

# Prefer published binary so DNA folder is next to star
if [ -x "$PUBLISH_DIR/linux-x64/star" ]; then
  exec "$PUBLISH_DIR/linux-x64/star" "$@"
fi
if [ -x "$PUBLISH_DIR/linux-arm64/star" ]; then
  exec "$PUBLISH_DIR/linux-arm64/star" "$@"
fi
if [ -x "$PUBLISH_DIR/osx-arm64/star" ]; then
  exec "$PUBLISH_DIR/osx-arm64/star" "$@"
fi
if [ -x "$PUBLISH_DIR/osx-x64/star" ]; then
  exec "$PUBLISH_DIR/osx-x64/star" "$@"
fi

# Fallback: dotnet run from project dir (DNA resolved from project output)
cd "$PROJECT_DIR"
exec dotnet run -c Release -- "$@"
