# STAR CLI — Getting Started (Windows)

This guide gets you up and running with the **STAR CLI** on **Windows**, either using the official installer or a manual clone-and-build setup.

---

## What is STAR CLI?

The STAR CLI is the command-line tool for the OASIS ecosystem. It lets you create and manage OAPPs, Quests, NFTs, inventory items, and other STARNETHolons; authenticate with your avatar; and work with the DNA system. See [STAR CLI Quick Start](./STAR_CLI_QUICK_START_GUIDE.md) and [STAR CLI Documentation](./STAR_CLI_DOCUMENTATION.md) for full usage.

---

## Option A: Install using the Windows installer (recommended for end users)

### Prerequisites

- **.NET 8 SDK** (required to build the installer from source). [Download](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Inno Setup 6** (optional). If installed, the build produces an EXE installer; otherwise you get the published `star.exe` only. [Download](https://jrsoftware.org/isinfo.php).

### Build the installer from the OASIS repo

1. Clone the OASIS repository (if you haven’t already):
   ```cmd
   git clone https://github.com/NextGenSoftwareUK/OASIS.git
   cd OASIS
   ```

2. From the STAR CLI project directory, run:
   ```cmd
   cd "STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
   installers\windows\build-installer.bat
   ```
   Or in PowerShell:
   ```powershell
   .\installers\windows\build-installer.ps1
   ```

3. Output:
   - **With Inno Setup:** `publish\installers\star-cli-1.0.0-win-x64.exe` — run this to install STAR CLI to Program Files and optionally add it to PATH.
   - **Without Inno Setup:** `publish\win-x64\star.exe` — copy this (and the `DNA` folder next to it) to a folder on your PATH.

### Install

- Double-click `star-cli-1.0.0-win-x64.exe` and follow the wizard, or
- Copy `publish\win-x64\star.exe` and the `publish\win-x64\DNA` folder to a directory on your PATH.

---

## Option B: Manual setup (git clone and run)

Best if you want to develop or contribute to STAR CLI.

### Prerequisites

- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Git** — [Download](https://git-scm.com/download/win).

### Steps

1. Clone the repository:
   ```cmd
   git clone https://github.com/NextGenSoftwareUK/OASIS.git
   cd OASIS
   ```

2. Go to the STAR CLI project:
   ```cmd
   cd "STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
   ```

3. Build and run:
   ```cmd
   dotnet build
   dotnet run
   ```

   To run the published single-file executable instead:
   ```cmd
   dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o publish\win-x64
   publish\win-x64\star.exe
   ```

4. Optional: add the published folder to your user PATH so you can run `star` from anywhere.

---

## Option C: .NET global tool (if published to NuGet)

If the package is published to NuGet:

```cmd
dotnet tool install -g NextGenSoftware.OASIS.STAR.CLI
star
```

Update: `dotnet tool update -g NextGenSoftware.OASIS.STAR.CLI`  
Uninstall: `dotnet tool uninstall -g NextGenSoftware.OASIS.STAR.CLI`

---

## Verify installation

- **Installer or published exe:** Open a new Command Prompt or PowerShell and run:
  ```cmd
  star --version
  ```
  Or run `star` and follow the prompts (avatar login, provider selection).

- **Manual run:** From the project directory, `dotnet run` starts the CLI.

---

## Next steps

- **[STAR CLI Quick Start Guide](./STAR_CLI_QUICK_START_GUIDE.md)** — First commands, auth, creating OAPPs and STARNETHolons.
- **[STAR CLI Documentation](./STAR_CLI_DOCUMENTATION.md)** — Full command reference.
- **[STAR CLI Installers & Packaging](./STAR_CLI_INSTALLERS_AND_PACKAGING.md)** — All install options (global tool, single-file, installers).
- **[DNA System Guide](./DNA_SYSTEM_GUIDE.md)** — DNA and dependency management.

---

## Troubleshooting

| Issue | What to do |
|-------|------------|
| **"dotnet" not recognized** | Install .NET 8 SDK and restart the terminal. |
| **"star" not recognized after install** | Add the install directory to PATH, or run with full path (e.g. `"C:\Program Files\OASIS STAR CLI\star.exe"`). |
| **Inno Setup not found** | Install Inno Setup 6 for the EXE installer; otherwise use `publish\win-x64\star.exe` from the build. |
| **DNA folder missing** | Ensure the `DNA` folder (with JSON configs) sits next to `star.exe`; the publish step includes it. |
