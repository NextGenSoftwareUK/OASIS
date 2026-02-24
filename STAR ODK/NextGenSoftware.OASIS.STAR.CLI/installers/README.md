# STAR CLI – Platform-specific installers (Option 3)

This folder contains scripts to build **native installers** for Windows, macOS, and Linux. Run the scripts on (or for) each platform to produce installable packages.

| Platform | Location | What you get |
|----------|----------|----------------|
| **Windows** | `windows/` | **Inno Setup** EXE installer: adds `star.exe` to Program Files and optionally to system PATH. |
| **macOS** | `macos/` | **.pkg** installer: installs `star` to `/usr/local/bin`. |
| **Linux** | `linux/` | **.deb** and **.rpm** packages (via **fpm**): install `star` to `/usr/local/bin`. |

All scripts publish the STAR CLI (single-file, self-contained) for the target platform first, then build the installer. Output goes to `publish/installers/` (and for Windows, the Inno script also expects publish output under `publish/win-x64/`).

**DNA folder rule:** Wherever `star` (or `star.exe`) is installed, a **DNA** folder must sit next to it with config/default JSON files. Only **JSON** files are included (no `.cs` or other source). This is enforced in `NextGenSoftware.OASIS.STAR.CLI.csproj` (Content: `DNA\**\*.json`); all installers copy the DNA folder from that publish output.

---

## Quick start

### Windows (run on Windows)

1. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php) (optional; if missing, only the binary is published).
2. Run:
   ```bat
   installers\windows\build-installer.bat
   ```
   Or from PowerShell: `.\installers\windows\build-installer.ps1`
3. Installer: `publish\installers\star-cli-1.0.0-win-x64.exe`

### macOS (run on macOS)

1. In Terminal:
   ```bash
   chmod +x installers/macos/build-installer.sh
   ./installers/macos/build-installer.sh
   ```
2. Installer: `publish/installers/star-cli-1.0.0-osx-arm64.pkg` (or `osx-x64` on Intel). Double-click to install.

### Linux (run on Linux)

1. Install **fpm**: `gem install fpm`
2. Run:
   ```bash
   chmod +x installers/linux/build-installer.sh
   ./installers/linux/build-installer.sh
   ```
3. Packages: `publish/installers/star-cli_1.0.0_amd64.deb` and `star-cli-1.0.0-1.x86_64.rpm`  
   Install: `sudo dpkg -i star-cli_1.0.0_amd64.deb` or `sudo rpm -i star-cli-1.0.0-1.x86_64.rpm`

---

## Prerequisites

- **All:** .NET 8 SDK.
- **Windows:** Inno Setup 6 (optional) for the EXE installer.
- **macOS:** None beyond .NET 8 (uses built-in `pkgbuild`).
- **Linux:** Ruby and **fpm** (`gem install fpm`) for .deb/.rpm; `rpm` for RPM builds.

See each platform’s subfolder for more detail (e.g. `windows/`, `macos/README.md`, `linux/README.md`).
