# Linux installers

- **Build (on Linux):** `./build-installer.sh` — publishes for the target arch and **always** produces a proper installer in `publish/installers/`.
- **Build for ARM64:** `./build-installer.sh linux-arm64`.

**What you get (every run):**
- **Tarball installer:** `star-cli-1.0.0-linux-x64.tar.gz` — extract and run `./install.sh` to install. No extra tools required.
- **.deb / .rpm (optional):** If **fpm** is installed (`gem install fpm`), you also get `.deb` and `.rpm` packages.

**Prerequisites:** .NET 8 SDK. For .deb/.rpm: Ruby and **fpm**; for RPM, `rpm` must be installed.

**Install (after building):**
- **Tarball:** `tar xzf star-cli-1.0.0-linux-x64.tar.gz && ./install.sh`
- Debian/Ubuntu: `sudo dpkg -i publish/installers/star-cli_1.0.0_amd64.deb`
- RHEL/Fedora: `sudo rpm -i publish/installers/star-cli-1.0.0-1.x86_64.rpm`

---

**Where is STAR installed?**
- **Command on PATH:** `/usr/local/bin/star` (wrapper script that runs the real binary)
- **App directory (binary, shipped DNA JSON, DNATemplates):** `/usr/local/lib/oasis-star-cli/`
- **Uninstall:** `/usr/local/lib/oasis-star-cli/uninstall.sh` (prompts for `sudo` if needed), or `./uninstall.sh` from an extracted tarball — same behavior
- **User config / writable data (after first run):** `~/.local/share/oasis-star-cli/` (e.g. copied DNA JSON)
- **Application menu entry:** `/usr/share/applications/oasis-star-cli.desktop`

**If STAR fails with “HolonDNATemplate.cs does not exist”:** the install is missing `DNATemplates`. Rebuild the tarball with `./build-installer.sh` and run `./install.sh` again (it now copies the full tree and verifies the template file).

**How do I run it?**
- **From a terminal:** run `star` (if `/usr/local/bin` is in your PATH, which it usually is).
- **From the application menu:** open your app launcher (e.g. “Activities” or “Applications”) and search for **“OASIS STAR CLI”** — it opens a terminal running STAR (like the Windows Start Menu shortcut).
