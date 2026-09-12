# STAR CLI — Getting Started (Linux)

This guide gets you up and running with the **STAR CLI** on **Linux**, either using the .deb/.rpm installer (built from source) or a manual clone-and-build setup.

---

## What is STAR CLI?

The STAR CLI is the command-line tool for the OASIS ecosystem. It lets you create and manage OAPPs, Quests, NFTs, inventory items, and other STARNETHolons; authenticate with your avatar; and work with the DNA system. See [STAR CLI Quick Start](./STAR_CLI_QUICK_START_GUIDE.md) and [STAR CLI Documentation](./STAR_CLI_DOCUMENTATION.md) for full usage.

---

## Option A: Install using .deb or .rpm (recommended for end users)

The Linux installer script builds a single-file `star` binary and packages it as **.deb** (Debian/Ubuntu) and **.rpm** (RHEL/Fedora). You can build the packages on Linux and install with your package manager.

### Prerequisites

- **.NET 8 SDK** — [Install on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
  ```bash
  # Ubuntu/Debian
  wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
  sudo dpkg -i packages-microsoft-prod.deb
  sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
  ```
- **Ruby** and **fpm** (to build .deb/.rpm):
  ```bash
  sudo apt-get install -y ruby ruby-dev build-essential
  gem install fpm
  ```
  For RPM builds, `rpm` must be installed (e.g. on Fedora: `sudo dnf install rpm-build`).

### Build the installer from the OASIS repo

1. Clone the repository (if you haven’t already):
   ```bash
   git clone https://github.com/NextGenSoftwareUK/OASIS.git
   cd OASIS
   ```

2. Run the Linux installer script:
   ```bash
   cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
   chmod +x installers/linux/build-installer.sh
   ./installers/linux/build-installer.sh
   ```

3. Output:
   - **With fpm:** `publish/installers/star-cli_1.0.0_amd64.deb` and `publish/installers/star-cli-1.0.0-1.x86_64.rpm`
   - **Without fpm:** The script still publishes the binary to `publish/linux-x64/star`; you can copy it to `/usr/local/bin` and copy the `DNA` folder next to it.

### Install

- **Debian/Ubuntu:**
  ```bash
  sudo dpkg -i publish/installers/star-cli_1.0.0_amd64.deb
  ```

- **RHEL/Fedora:**
  ```bash
  sudo rpm -i publish/installers/star-cli-1.0.0-1.x86_64.rpm
  ```

- **ARM64:** Build with `./installers/linux/build-installer.sh linux-arm64`, then install the generated .deb or .rpm.

After install, `star` is in `/usr/local/bin` (and the DNA folder is placed there). Run `star --version` or `star` to verify.

---

## Option B: Manual setup (git clone and run)

Best if you want to develop or contribute to STAR CLI.

### Prerequisites

- **.NET 8 SDK** — see [Install .NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
- **Git** — `sudo apt-get install git` (or equivalent).

### Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/NextGenSoftwareUK/OASIS.git
   cd OASIS
   ```

2. Go to the STAR CLI project:
   ```bash
   cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
   ```

3. Build and run:
   ```bash
   dotnet build
   dotnet run
   ```

   Or publish a single-file executable and run it:
   ```bash
   dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true -o publish/linux-x64
   ./publish/linux-x64/star
   ```

4. Optional: add `publish/linux-x64` to your PATH or copy `star` and the `DNA` folder to `/usr/local/bin`.

---

## Option C: .NET global tool (if published to NuGet)

If the package is published to NuGet:

```bash
dotnet tool install -g NextGenSoftware.OASIS.STAR.CLI
star
```

Update: `dotnet tool update -g NextGenSoftware.OASIS.STAR.CLI`  
Uninstall: `dotnet tool uninstall -g NextGenSoftware.OASIS.STAR.CLI`

---

## Verify installation

- **After .deb/.rpm or copying to PATH:** Open a new terminal and run:
  ```bash
  star --version
  ```
  Or run `star` and follow the prompts.

- **Manual run:** From the project directory, `dotnet run` starts the CLI.

---

## Next steps

- **[STAR CLI Quick Start Guide](./STAR_CLI_QUICK_START_GUIDE.md)** — First commands, auth, creating OAPPs and STARNETHolons.
- **[STAR CLI Documentation](./STAR_CLI_DOCUMENTATION.md)** — Full command reference.
- **[STAR CLI Installers & Packaging](./STAR_CLI_INSTALLERS_AND_PACKAGING.md)** — All install options.
- **[DNA System Guide](./DNA_SYSTEM_GUIDE.md)** — DNA and dependency management.

---

## Troubleshooting

| Issue | What to do |
|-------|------------|
| **"dotnet" not found** | Install .NET 8 SDK and ensure it’s on PATH (e.g. `export PATH="$PATH:$HOME/.dotnet"`). |
| **fpm not found** | Install Ruby and run `gem install fpm`. Without fpm, use the published binary from `publish/linux-x64/star`. |
| **"star" not found after install** | Ensure `/usr/local/bin` is on your PATH. Run `/usr/local/bin/star` to test. |
| **DNA folder missing** | The installer and publish output include a `DNA` folder; it must sit next to the `star` binary. |
