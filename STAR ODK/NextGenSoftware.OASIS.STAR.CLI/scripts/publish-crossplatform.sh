#!/usr/bin/env bash
# Build STAR CLI for all platforms (single-file, self-contained).
# Run from repo root: bash "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/scripts/publish-crossplatform.sh"
# Requires: .NET 8 SDK

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
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
PUBLISH_DIR="$PROJECT_DIR/publish"

RIDS="win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64"

for rid in $RIDS; do
  out="$PUBLISH_DIR/$rid"
  echo "Publishing $rid -> $out"
  dotnet publish "$PROJECT_DIR/NextGenSoftware.OASIS.STAR.CLI.csproj" \
    -c Release \
    -r "$rid" \
    -p:PublishSingleFile=true \
    -p:SelfContained=true \
    -o "$out"
done

echo "Done. Output: $PUBLISH_DIR"
echo "  Windows: star.exe in win-x64 / win-arm64"
echo "  Linux:   star in linux-x64 / linux-arm64"
echo "  macOS:   star in osx-x64 / osx-arm64"
