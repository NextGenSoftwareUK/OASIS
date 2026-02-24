#!/usr/bin/env bash
# Build macOS installer (.pkg) for STAR CLI.
# Prerequisites: .NET 8 SDK. Run on macOS (or cross-publish from Windows then run this on macOS).
# Usage: ./build-installer.sh [osx-x64|osx-arm64]  (default: detect current arch and build)

set -e
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

# Stage: star in /usr/local/bin; DNA folder (JSON only) next to it (same rule as Windows/Linux)
PKG_ROOT="$INSTALLER_OUT/pkgroot"
rm -rf "$PKG_ROOT"
mkdir -p "$PKG_ROOT/usr/local/bin"
cp "$STAR_BIN" "$PKG_ROOT/usr/local/bin/star"
chmod 755 "$PKG_ROOT/usr/local/bin/star"
if [ -d "$PUBLISH_DIR/$ARCH/DNA" ]; then
  cp -R "$PUBLISH_DIR/$ARCH/DNA" "$PKG_ROOT/usr/local/bin/"
fi

# Build .pkg (flat package)
OUTPUT_PKG="$INSTALLER_OUT/star-cli-${VERSION}-${ARCH}.pkg"
pkgbuild --root "$PKG_ROOT" \
  --identifier com.oasis.star.cli \
  --version "$VERSION" \
  --install-location / \
  "$OUTPUT_PKG"

rm -rf "$PKG_ROOT"
echo "Installer: $OUTPUT_PKG"
echo "Done."
