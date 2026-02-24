# Linux installers

- **Build (on Linux):** `./build-installer.sh` — publishes linux-x64 and builds .deb + .rpm in `publish/installers/`.
- **Build for ARM64:** `./build-installer.sh linux-arm64`.
- **Prerequisites:** .NET 8 SDK, Ruby, and **fpm** (`gem install fpm`). For .rpm building, `rpm` must be installed.

**Install (after building):**
- Debian/Ubuntu: `sudo dpkg -i publish/installers/star-cli_1.0.0_amd64.deb`
- RHEL/Fedora: `sudo rpm -i publish/installers/star-cli-1.0.0-1.x86_64.rpm`

Without fpm, the script still publishes the binary; you can copy `publish/linux-x64/star` to `/usr/local/bin` manually.
