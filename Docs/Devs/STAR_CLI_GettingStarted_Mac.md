# STAR CLI — Getting Started (macOS)

This guide gets you up and running with the **STAR CLI** on **macOS** (Intel or Apple Silicon), either using the .pkg installer or a manual clone-and-build setup.

---

## What is STAR CLI?

The STAR CLI is the command-line tool for the OASIS ecosystem. It lets you create and manage OAPPs, Quests, NFTs, inventory items, and other STARNETHolons; authenticate with your avatar; and work with the DNA system. See [STAR CLI Quick Start](./STAR_CLI_QUICK_START_GUIDE.md) and [STAR CLI Documentation](./STAR_CLI_DOCUMENTATION.md) for full usage.

---

## Option A: Install using the .pkg installer (recommended for end users)

The macOS installer script builds a single-file `star` binary and packages it as a **.pkg**. Double-click to install; `star` is placed in `/usr/local/bin`.

### Prerequisites

- **.NET 8 SDK** — [Install on macOS](https://learn.microsoft.com/en-us/dotnet/core/install/macos).
  ```bash
  # Homebrew
  brew install dotnet@8
  ```
  Or download the installer from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).

### Build the installer from the OASIS repo

1. Clone the repository (if you haven’t already):
   ```bash
   git clone https://github.com/NextGenSoftwareUK/OASIS.git
   cd OASIS
   ```

2. Run the macOS installer script:
   ```bash
   cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
   chmod +x installers/macos/build-installer.sh
   ./installers/macos/build-installer.sh
   ```

   The script detects your architecture (Apple Silicon or Intel) and builds for it. To force an architecture:
   ```bash
   ./installers/macos/build-installer.sh osx-arm64   # Apple Silicon
   ./installers/macos/build-installer.sh osx-x64     # Intel
   ```

3. Output: `publish/installers/star-cli-1.0.0-osx-arm64.pkg` (or `osx-x64` on Intel).

### Install

- Double-click the .pkg and follow the installer. This installs `star` to `/usr/local/bin` and includes the DNA folder.
- Open Terminal and run:
  ```bash
  star --version
  star
  ```

---

## Option B: Manual setup (git clone and run)

Best if you want to develop or contribute to STAR CLI.

### Prerequisites

- **.NET 8 SDK** — `brew install dotnet@8` or [download](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Git** — `brew install git` or Xcode Command Line Tools.

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
   # Apple Silicon
   dotnet publish -c Release -r osx-arm64 -p:PublishSingleFile=true -p:SelfContained=true -o publish/osx-arm64
   ./publish/osx-arm64/star

   # Intel
   dotnet publish -c Release -r osx-x64 -p:PublishSingleFile=true -p:SelfContained=true -o publish/osx-x64
   ./publish/osx-x64/star
   ```

4. Optional: add the publish folder to your PATH or copy `star` and the `DNA` folder to `/usr/local/bin`.

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

- **After .pkg install:** Open Terminal (or a new tab) and run:
  ```bash
  star --version
  star
  ```

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
| **"dotnet" not found** | Install .NET 8 SDK and restart Terminal. Ensure `dotnet` is on PATH. |
| **"star" not found after .pkg install** | Ensure `/usr/local/bin` is on your PATH. Run `/usr/local/bin/star` to test. |
| **Apple Silicon vs Intel** | Use `./installers/macos/build-installer.sh osx-arm64` or `osx-x64` explicitly if needed. |
| **DNA folder missing** | The installer and publish output include a `DNA` folder next to the `star` binary. |
