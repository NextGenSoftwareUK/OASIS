#!/usr/bin/env bash
# Build Linux .deb (and optionally .rpm) for STAR CLI.
# Prerequisites: .NET 8 SDK; fpm (gem install fpm).
# Usage: ./build-installer.sh [linux-x64|linux-arm64]  (default: linux-x64)

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
ARCH="${1:-linux-x64}"

echo "Building Linux Installer for $ARCH (STAR CLI)..."
mkdir -p "$INSTALLER_OUT"

# Always publish (force overwrite) so DNA + DNATemplates and binary are up to date
echo "Publishing $ARCH (overwriting existing if present)..."
dotnet publish "$PROJECT_DIR/NextGenSoftware.OASIS.STAR.CLI.csproj" \
  -c Release -r "$ARCH" \
  -p:PublishSingleFile=true -p:SelfContained=true \
  -o "$PUBLISH_DIR/$ARCH"

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

# Stage: STAR in its own directory /usr/local/lib/oasis-star-cli (star, DNA, DNATemplates); symlink in /usr/local/bin for PATH
STAR_LIB="/usr/local/lib/oasis-star-cli"
echo "Packaging installer (tarball + .deb/.rpm)..."
PKG_ROOT="$INSTALLER_OUT/pkgroot"
rm -rf "$PKG_ROOT"
mkdir -p "$PKG_ROOT$STAR_LIB"
cp "$STAR_BIN" "$PKG_ROOT$STAR_LIB/star"
chmod 755 "$PKG_ROOT$STAR_LIB/star"
if [ -d "$PUBLISH_DIR/$ARCH/DNA" ]; then
  cp -R "$PUBLISH_DIR/$ARCH/DNA" "$PKG_ROOT$STAR_LIB/"
fi
if [ -d "$PUBLISH_DIR/$ARCH/DNATemplates" ]; then
  cp -R "$PUBLISH_DIR/$ARCH/DNATemplates" "$PKG_ROOT$STAR_LIB/"
fi
# Wrapper script so "star" is on PATH; exec ensures ProcessPath resolves to the real binary (so STAR finds DNATemplates under its lib dir)
mkdir -p "$PKG_ROOT/usr/local/bin"
cat > "$PKG_ROOT/usr/local/bin/star" << 'WRAPPER'
#!/bin/sh
exec /usr/local/lib/oasis-star-cli/star "$@"
WRAPPER
chmod 755 "$PKG_ROOT/usr/local/bin/star"

# Uninstall script: remove STAR CLI (new layout and legacy /usr/local/bin layout)
UNINSTALL_SH='#!/bin/sh
# Uninstall OASIS STAR CLI from this system (re-execs with sudo if needed)
set -e
if [ "$(id -u)" -ne 0 ]; then
  exec sudo "$0" "$@"
fi
# Pause before exit so the window does not vanish (run from terminal or file manager)
_pause() {
  echo ""
  echo "========================================"
  echo "  Press Enter to exit"
  echo "========================================"
  read -r _ </dev/tty 2>/dev/null || read -r _ 2>/dev/null || sleep 15
}
trap _pause EXIT

STAR_LIB="/usr/local/lib/oasis-star-cli"
echo "Uninstalling OASIS STAR CLI..."
rm -f /usr/local/bin/star
rm -rf "$STAR_LIB"
rm -f /usr/share/applications/oasis-star-cli.desktop
# Legacy layout: remove old install when everything was under /usr/local/bin
rm -rf /usr/local/bin/DNA /usr/local/bin/DNATemplates
echo "STAR CLI uninstalled."
echo "  User data (if you want to remove it): rm -rf ~/.local/share/oasis-star-cli"
'
echo "$UNINSTALL_SH" > "$PKG_ROOT$STAR_LIB/uninstall.sh"
chmod 755 "$PKG_ROOT$STAR_LIB/uninstall.sh"
echo "$UNINSTALL_SH" > "$PKG_ROOT/uninstall.sh"
chmod 755 "$PKG_ROOT/uninstall.sh"

# .desktop file so STAR appears in the application menu (like Windows Start Menu)
mkdir -p "$PKG_ROOT/usr/share/applications"
cat > "$PKG_ROOT/usr/share/applications/oasis-star-cli.desktop" << 'DESKTOP'
[Desktop Entry]
Name=OASIS STAR CLI
Comment=OASIS STAR CLI - command-line development tools and low/no-code generator
Exec=/usr/local/bin/star
Terminal=true
Type=Application
Categories=Development;
DESKTOP

# Install script for tarball users (extract then run ./install.sh)
cat > "$PKG_ROOT/install.sh" << INSTALLSH
#!/bin/sh
# Install OASIS STAR CLI from this tarball: app under $STAR_LIB, 'star' command via symlink in /usr/local/bin
# Pause before exit so the window does not vanish (run from terminal or file manager)
_pause() {
  echo ""
  echo "========================================"
  echo "  Press Enter to exit"
  echo "========================================"
  read -r _ </dev/tty 2>/dev/null || read -r _ 2>/dev/null || sleep 15
}
trap _pause EXIT

ROOT="\$(cd "\$(dirname "\$0")" && pwd)"
echo "Installing STAR CLI to $STAR_LIB..."
sudo mkdir -p $STAR_LIB
if ! sudo cp "\$ROOT$STAR_LIB/star" $STAR_LIB/star; then
  echo "ERROR: Failed to copy star binary." 1>&2
  exit 1
fi
sudo chmod 755 $STAR_LIB/star
# Copy full DNA / DNATemplates trees (do not use .../* — fragile in sh and can miss nested files)
if [ -d "\$ROOT$STAR_LIB/DNA" ]; then
  sudo rm -rf $STAR_LIB/DNA
  sudo cp -a "\$ROOT$STAR_LIB/DNA" $STAR_LIB/
fi
if [ -d "\$ROOT$STAR_LIB/DNATemplates" ]; then
  sudo rm -rf $STAR_LIB/DNATemplates
  sudo cp -a "\$ROOT$STAR_LIB/DNATemplates" $STAR_LIB/
fi
if [ -f "\$ROOT$STAR_LIB/uninstall.sh" ]; then
  sudo cp "\$ROOT$STAR_LIB/uninstall.sh" $STAR_LIB/uninstall.sh
  sudo chmod 755 $STAR_LIB/uninstall.sh
fi
if [ -f "\$ROOT/usr/local/bin/star" ]; then
  sudo cp "\$ROOT/usr/local/bin/star" /usr/local/bin/star
  sudo chmod 755 /usr/local/bin/star
fi
if [ -f "\$ROOT/usr/share/applications/oasis-star-cli.desktop" ]; then
  sudo mkdir -p /usr/share/applications
  sudo cp "\$ROOT/usr/share/applications/oasis-star-cli.desktop" /usr/share/applications/
fi
if [ ! -f $STAR_LIB/DNATemplates/CSharpDNATemplates/HolonDNATemplate.cs ]; then
  echo "ERROR: DNATemplates are missing under $STAR_LIB (installer tarball may be incomplete)." 1>&2
  echo "  Rebuild with: installers/linux/build-installer.sh" 1>&2
  echo "  Or copy DNATemplates from a full publish output next to star." 1>&2
  exit 1
fi
echo ""
echo "Installation complete."
echo "  STAR installed under: $STAR_LIB"
echo "  Run from terminal: star"
echo "  Or find 'OASIS STAR CLI' in your application menu."
echo "  To uninstall later: sudo $STAR_LIB/uninstall.sh"
INSTALLSH
chmod 755 "$PKG_ROOT/install.sh"

# Always produce a tarball so there is always a proper "installer" (no fpm required)
TARBALL="$INSTALLER_OUT/star-cli-${VERSION}-${ARCH}.tar.gz"
( cd "$PKG_ROOT" && tar czf "$TARBALL" . )
echo "Tarball installer: $TARBALL  (extract and run ./install.sh)"

# .deb and .rpm (optional; requires fpm: gem install fpm)
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
  echo "fpm not found (gem install fpm). .deb/.rpm skipped; use tarball above."
fi

rm -rf "$PKG_ROOT"
echo "Done."
