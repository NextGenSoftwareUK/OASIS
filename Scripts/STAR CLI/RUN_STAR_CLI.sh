#!/usr/bin/env bash
# Run the STAR CLI. Uses published binary if present (publish/linux-x64/star or publish/osx-*),
# otherwise runs via dotnet run.
# After publish, DNA/ must sit next to star (not only inside the single-file bundle) so
# OASIS_DNA.json and SecretKey persist — rebuild with BUILD_STAR_CLI.sh if beam-in fails.
set -e


SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/../include/pause_on_exit.inc.sh"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_DIR="$REPO_ROOT/STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
PUBLISH_DIR="$PROJECT_DIR/publish"

# Require DNA beside star — old publish-only layouts often had star without DNA/ (beam-in fails).
if [[ -x "$PUBLISH_DIR/linux-x64/star" && -f "$PUBLISH_DIR/linux-x64/DNA/OASIS_DNA.json" ]]; then
  cd "$PUBLISH_DIR/linux-x64"
  ./star "$@"
  exit $?
fi
if [[ -x "$PUBLISH_DIR/linux-arm64/star" && -f "$PUBLISH_DIR/linux-arm64/DNA/OASIS_DNA.json" ]]; then
  cd "$PUBLISH_DIR/linux-arm64"
  ./star "$@"
  exit $?
fi
if [[ -x "$PUBLISH_DIR/osx-arm64/star" && -f "$PUBLISH_DIR/osx-arm64/DNA/OASIS_DNA.json" ]]; then
  cd "$PUBLISH_DIR/osx-arm64"
  ./star "$@"
  exit $?
fi
if [[ -x "$PUBLISH_DIR/osx-x64/star" && -f "$PUBLISH_DIR/osx-x64/DNA/OASIS_DNA.json" ]]; then
  cd "$PUBLISH_DIR/osx-x64"
  ./star "$@"
  exit $?
fi
if [[ -x "$PUBLISH_DIR/win-x64/star.exe" && -f "$PUBLISH_DIR/win-x64/DNA/OASIS_DNA.json" ]]; then
  cd "$PUBLISH_DIR/win-x64"
  ./star.exe "$@"
  exit $?
fi
if [[ -x "$PUBLISH_DIR/win-arm64/star.exe" && -f "$PUBLISH_DIR/win-arm64/DNA/OASIS_DNA.json" ]]; then
  cd "$PUBLISH_DIR/win-arm64"
  ./star.exe "$@"
  exit $?
fi

cd "$PROJECT_DIR"
dotnet run -c Release -- "$@"
exit $?
