# STAR CLI – Cross-Platform Installers & Packaging

This guide covers the best ways to distribute and install the STAR CLI on **Windows**, **macOS**, and **Linux**.

---

## Option 1: .NET global tool (recommended for most users)

One cross-platform install method: users with the .NET 8 SDK run:

```bash
dotnet tool install -g NextGenSoftware.OASIS.STAR.CLI
```

Then run the CLI as:

```bash
star
```

- **Windows / macOS / Linux**: same command; the tool is installed into the user’s `dotnet` tools folder (e.g. `%USERPROFILE%\.dotnet\tools` on Windows, `~/.dotnet/tools` on Unix) and should be on PATH.
- **Update**: `dotnet tool update -g NextGenSoftware.OASIS.STAR.CLI`
- **Uninstall**: `dotnet tool uninstall -g NextGenSoftware.OASIS.STAR.CLI`

### Publishing the tool (maintainers)

1. **Push to NuGet** (public install from nuget.org):

   ```bash
   cd "STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
   dotnet pack -c Release
   dotnet nuget push "bin\Release\NextGenSoftware.OASIS.STAR.CLI.*.nupkg" --api-key YOUR_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
   ```

2. **Local or private feed** – install from a local folder:

   ```bash
   dotnet pack -c Release -o ./nupkgs
   dotnet tool install -g NextGenSoftware.OASIS.STAR.CLI --add-source ./nupkgs
   ```

No platform-specific installers are required for this option.

---

## Option 2: Single-file executables (no .NET SDK required)

Build one executable per OS/architecture. Users can put the file on PATH or you can wrap it in platform-specific installers.

### Build commands (run from repo root)

From **PowerShell** (Windows; cross-compiles all from one machine):

```powershell
cd "c:\Source\OASIS-master\STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
$out = ".\publish"
$rids = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
foreach ($r in $rids) {
  dotnet publish -c Release -r $r -p:PublishSingleFile=true -p:SelfContained=true -o "$out\$r"
}
```

From **Bash** (Linux/macOS):

```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
OUT="./publish"
for rid in win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64; do
  dotnet publish -c Release -r "$rid" -p:PublishSingleFile=true -p:SelfContained=true -o "$OUT/$rid"
done
```

Output layout:

- `publish/win-x64/star.exe` (Windows x64)
- `publish/linux-x64/star` (Linux x64)
- `publish/osx-arm64/star` (macOS Apple Silicon)
- etc.

Users can:

- Copy the executable to a folder on PATH, or
- Use it as the payload for platform-specific installers (see below).

---

## Option 3: Platform-specific installers

Use the single-file (or framework-dependent) output from Option 2 as the “app” and wrap it in native installers for a traditional install experience. **Ready-to-use scripts** are in the STAR CLI repo under `installers/`:

| Platform   | Script / config | Output |
|-----------|------------------|--------|
| **Windows** | `installers/windows/build-installer.ps1` (or `.bat`) + `star-cli.iss` (Inno Setup) | `publish/installers/star-cli-1.0.0-win-x64.exe` |
| **macOS**   | `installers/macos/build-installer.sh` (uses `pkgbuild`) | `publish/installers/star-cli-1.0.0-osx-arm64.pkg` (or osx-x64) |
| **Linux**   | `installers/linux/build-installer.sh` (uses **fpm**) | `publish/installers/star-cli_1.0.0_amd64.deb` and `.rpm` |

See **`STAR ODK/NextGenSoftware.OASIS.STAR.CLI/installers/README.md`** for step-by-step usage and prerequisites.

### Windows (Inno Setup)

1. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php).
2. From the STAR CLI project dir run: `installers\windows\build-installer.bat` (or the PowerShell script). This publishes win-x64 and compiles the Inno script. The installer adds `star.exe` to Program Files and optionally to system PATH.

### macOS (.pkg)

1. On macOS, run: `./installers/macos/build-installer.sh` (optionally pass `osx-arm64` or `osx-x64`).
2. The .pkg installs `star` to `/usr/local/bin`. For a DMG wrapper, use [create-dmg](https://github.com/create-dmg/create-dmg) or similar. For **Homebrew**, add a formula that downloads the published tarball and installs the binary.

### Linux (.deb / .rpm)

1. Install **fpm**: `gem install fpm`.
2. Run: `./installers/linux/build-installer.sh` (or pass `linux-arm64` for ARM).
3. Install: `sudo dpkg -i star-cli_1.0.0_amd64.deb` or `sudo rpm -i star-cli-1.0.0-1.x86_64.rpm`. For **Snap** or **Flatpak**, add a manifest that references the published binary.

---

## Summary

| Goal                               | Approach                    |
|------------------------------------|-----------------------------|
| Easiest for devs / power users     | **Option 1** – `dotnet tool install -g NextGenSoftware.OASIS.STAR.CLI` |
| No .NET SDK, just one executable   | **Option 2** – single-file publish per RID, then copy to PATH or use in installers |
| Traditional installers per OS     | **Option 3** – use Option 2 output inside WiX/Inno, productbuild/Homebrew, fpm/Snap |

The STAR CLI project is already configured for **Option 1** (PackAsTool, ToolCommandName `star`) and is ready for **Option 2** by passing `-p:PublishSingleFile=true -r <rid>` when publishing.
