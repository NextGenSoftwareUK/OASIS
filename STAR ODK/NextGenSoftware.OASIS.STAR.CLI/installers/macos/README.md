# macOS installer

- **Build (on macOS):** `./build-installer.sh` — builds for current architecture (arm64 or x64).
- **Build for a specific arch:** `./build-installer.sh osx-arm64` or `./build-installer.sh osx-x64`.
- **Output:** `publish/installers/star-cli-1.0.0-osx-arm64.pkg` (or osx-x64). Double-click to install; `star` is placed in `/usr/local/bin`.

For a DMG wrapper, use [create-dmg](https://github.com/create-dmg/create-dmg) or similar after building the .pkg.
