#!/usr/bin/env bash
# Build Linux .deb (and optionally .rpm) for STAR CLI.
# Prerequisites: .NET 8 SDK; fpm (gem install fpm).
# Usage: ./build-installer.sh [linux-x64|linux-arm64]  (default: linux-x64)

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
PUBLISH_DIR="$PROJECT_DIR/publish"
INSTALLER_OUT="$PUBLISH_DIR/installers"
VERSION="${VERSION:-1.0.0}"
ARCH="${1:-linux-x64}"

mkdir -p "$INSTALLER_OUT"

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

# Map RID to deb/rpm architecture names
DEB_ARCH="$ARCH"
case "$ARCH" in
  linux-x64)   DEB_ARCH="amd64"; RPM_ARCH="x86_64" ;;
  linux-arm64) DEB_ARCH="arm64"; RPM_ARCH="aarch64" ;;
  *)           RPM_ARCH="$ARCH" ;;
esac

# Stage: star in /usr/local/bin; DNA folder (JSON only) next to it (same rule as Windows/macOS)
PKG_ROOT="$INSTALLER_OUT/pkgroot"
rm -rf "$PKG_ROOT"
mkdir -p "$PKG_ROOT/usr/local/bin"
cp "$STAR_BIN" "$PKG_ROOT/usr/local/bin/star"
chmod 755 "$PKG_ROOT/usr/local/bin/star"
if [ -d "$PUBLISH_DIR/$ARCH/DNA" ]; then
  cp -R "$PUBLISH_DIR/$ARCH/DNA" "$PKG_ROOT/usr/local/bin/"
fi

# .deb and .rpm (requires fpm: gem install fpm)
if command -v fpm >/dev/null 2>&1; then
  ( cd "$PKG_ROOT" && fpm -s dir -t deb \
    -n star-cli \
    -v "$VERSION" \
    -a "$DEB_ARCH" \
    -p "$INSTALLER_OUT/star-cli_${VERSION}_${DEB_ARCH}.deb" \
    --description "OASIS STAR CLI - command-line development tools and low/no-code generator" \
    --url "https://github.com/NextGenSoftware/OASIS" \
    --vendor "NextGen Software" \
    usr )
  echo "Debian package: $INSTALLER_OUT/star-cli_${VERSION}_${DEB_ARCH}.deb"

  ( cd "$PKG_ROOT" && fpm -s dir -t rpm \
    -n star-cli \
    -v "$VERSION" \
    -a "$RPM_ARCH" \
    -p "$INSTALLER_OUT/star-cli-${VERSION}-1.${RPM_ARCH}.rpm" \
    --description "OASIS STAR CLI - command-line development tools and low/no-code generator" \
    --url "https://github.com/NextGenSoftware/OASIS" \
    --vendor "NextGen Software" \
    usr )
  echo "RPM package: $INSTALLER_OUT/star-cli-${VERSION}-1.${RPM_ARCH}.rpm"
else
  echo "fpm not found. Install with: gem install fpm"
  echo "Staged files are in $PKG_ROOT (usr/local/bin/star). Package manually or install with: sudo cp $PKG_ROOT/usr/local/bin/star /usr/local/bin/"
fi

rm -rf "$PKG_ROOT"
echo "Done."
