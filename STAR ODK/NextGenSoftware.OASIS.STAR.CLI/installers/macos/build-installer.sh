#!/usr/bin/env bash
# Build macOS installer (.pkg) for STAR CLI.
# Prerequisites: .NET 8 SDK. Run on macOS (or cross-publish from Windows then run this on macOS).
# Usage: ./build-installer.sh [osx-x64|osx-arm64]  (default: detect current arch and build)

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
PROJECT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
PUBLISH_DIR="$PROJECT_DIR/publish"
INSTALLER_OUT="$PUBLISH_DIR/installers"
VERSION="${VERSION:-1.0.0}"
ARCH="${1:-}"

mkdir -p "$INSTALLER_OUT"

if [ -z "$ARCH" ]; then
  ARCH=$(uname -m)
  if [ "$ARCH" = "arm64" ]; then
    ARCH="osx-arm64"
  else
    ARCH="osx-x64"
  fi
fi

if [ ! -d "$PUBLISH_DIR/$ARCH" ] || [ ! -f "$PUBLISH_DIR/$ARCH/star" ]; then
  echo "Publishing $ARCH..."
  dotnet publish "$PROJECT_DIR/NextGenSoftware.OASIS.STAR.CLI.csproj" \
    -c Release -r "$ARCH" \
    -p:PublishSingleFile=true -p:SelfContained=true \
    -o "$PUBLISH_DIR/$ARCH"
fi

STAR_BIN="$PUBLISH_DIR/$ARCH/star"
if [ ! -f "$STAR_BIN" ]; then
  echo "Error: $STAR_BIN not found." >&2
  exit 1
fi

# Stage: star in /usr/local/bin; DNA and DNATemplates next to it (same rule as Windows/Linux)
PKG_ROOT="$INSTALLER_OUT/pkgroot"
rm -rf "$PKG_ROOT"
mkdir -p "$PKG_ROOT/usr/local/bin"
cp "$STAR_BIN" "$PKG_ROOT/usr/local/bin/star"
chmod 755 "$PKG_ROOT/usr/local/bin/star"
if [ -d "$PUBLISH_DIR/$ARCH/DNA" ]; then
  cp -R "$PUBLISH_DIR/$ARCH/DNA" "$PKG_ROOT/usr/local/bin/"
fi
if [ -d "$PUBLISH_DIR/$ARCH/DNATemplates" ]; then
  cp -R "$PUBLISH_DIR/$ARCH/DNATemplates" "$PKG_ROOT/usr/local/bin/"
fi

# Build .pkg (flat package) — proper macOS installer (double-click to run Installer wizard)
OUTPUT_PKG="$INSTALLER_OUT/star-cli-${VERSION}-${ARCH}.pkg"
pkgbuild --root "$PKG_ROOT" \
  --identifier com.oasis.star.cli \
  --version "$VERSION" \
  --install-location / \
  "$OUTPUT_PKG"
echo "Package installer: $OUTPUT_PKG"

# Build .dmg for distribution (disk image containing the .pkg; double-click to open then run the .pkg)
DMG_NAME="star-cli-${VERSION}-${ARCH}"
DMG_ROOT="$INSTALLER_OUT/dmgroot"
rm -rf "$DMG_ROOT"
mkdir -p "$DMG_ROOT"
cp "$OUTPUT_PKG" "$DMG_ROOT/"
# Optional: symlink to /Applications so users can drag .pkg there if they prefer
ln -s /Applications "$DMG_ROOT/Applications" 2>/dev/null || true
OUTPUT_DMG="$INSTALLER_OUT/${DMG_NAME}.dmg"
rm -f "$OUTPUT_DMG"
hdiutil create -volname "STAR CLI $VERSION" -srcfolder "$DMG_ROOT" -ov -format UDZO "$OUTPUT_DMG"
rm -rf "$DMG_ROOT"
echo "Disk image: $OUTPUT_DMG"

rm -rf "$PKG_ROOT"
echo "Done."
