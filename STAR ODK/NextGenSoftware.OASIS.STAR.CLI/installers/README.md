# STAR CLI – Platform-specific installers (Option 3)

This folder contains scripts to build **proper native installers** for Windows, macOS, and Linux — like the Windows Inno Setup installer, each platform gets a real installable package.

**Getting Started (installer + manual setup):** For step-by-step guides per platform, see the repo docs:
- [STAR CLI Getting Started — Windows](../../../Docs/Devs/STAR_CLI_GettingStarted_Windows.md)
- [STAR CLI Getting Started — Linux](../../../Docs/Devs/STAR_CLI_GettingStarted_Linux.md)
- [STAR CLI Getting Started — macOS](../../../Docs/Devs/STAR_CLI_GettingStarted_Mac.md)

| Platform | Location | What you get |
|----------|----------|----------------|
| **Windows** | `windows/` | **Inno Setup** `.exe` installer (wizard, PATH option, uninstall). |
| **macOS** | `macos/` | **.pkg** installer (double-click) + **.dmg** disk image for distribution. |
| **Linux** | `linux/` | **.tar.gz** with `install.sh` (always), plus **.deb** / **.rpm** when `fpm` is installed. |

All scripts publish the STAR CLI (single-file, self-contained) for the target platform first, then package it. Output goes to `publish/installers/` (and for Windows, the Inno script also expects publish output under `publish/win-x64/`).

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
2. **.pkg** installer: `publish/installers/star-cli-1.0.0-osx-arm64.pkg` (or `osx-x64` on Intel). Double-click to run the installer wizard.
3. **.dmg** disk image: `star-cli-1.0.0-osx-arm64.dmg` — open and double-click the .pkg inside, or use for distribution.

### Linux (run on Linux)

1. Run:
   ```bash
   chmod +x installers/linux/build-installer.sh
   ./installers/linux/build-installer.sh
   ```
2. You always get a **tarball** installer: `publish/installers/star-cli-1.0.0-linux-x64.tar.gz`  
   Extract and run `./install.sh` to install to `/usr/local/bin`.
3. If **fpm** is installed (`gem install fpm`), you also get:  
   `star-cli_1.0.0_amd64.deb` and `star-cli-1.0.0-1.x86_64.rpm`  
   Install: `sudo dpkg -i star-cli_1.0.0_amd64.deb` or `sudo rpm -i star-cli-1.0.0-1.x86_64.rpm`

---

## Prerequisites

- **All:** .NET 8 SDK.
- **Windows:** Inno Setup 6 (optional) for the EXE installer.
- **macOS:** None beyond .NET 8 (uses built-in `pkgbuild` and `hdiutil` for .pkg and .dmg).
- **Linux:** No extra tools required for the tarball installer. For .deb/.rpm: Ruby and **fpm** (`gem install fpm`); `rpm` for RPM builds.

Every run produces at least one proper installer: Windows → .exe (if Inno installed), macOS → .pkg + .dmg, Linux → .tar.gz (extract and run `install.sh`), plus .deb/.rpm when fpm is available.

See each platform’s subfolder for more detail (e.g. `windows/`, `macos/README.md`, `linux/README.md`).
