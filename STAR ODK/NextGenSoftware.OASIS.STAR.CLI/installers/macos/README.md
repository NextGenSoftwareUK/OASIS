# macOS installer

- **Build (on macOS):** `./build-installer.sh` — builds for current architecture (arm64 or x64) and produces proper installers.
- **Build for a specific arch:** `./build-installer.sh osx-arm64` or `./build-installer.sh osx-x64`.

**What you get:**
- **.pkg** — `publish/installers/star-cli-1.0.0-osx-arm64.pkg` (or osx-x64). Double-click to run the macOS Installer wizard; `star` is placed in `/usr/local/bin`.
- **.dmg** — `star-cli-1.0.0-osx-arm64.dmg` — disk image containing the .pkg (and an Applications link). Use for distribution; open the DMG and double-click the .pkg to install.
