# Windows installer (Inno Setup)

- **Build:** Run `build-installer.bat` or `build-installer.ps1` from the STAR CLI project directory. This publishes `win-x64` and, if Inno Setup 6 is installed, produces an EXE installer.
- **Prerequisites:** .NET 8 SDK; [Inno Setup 6](https://jrsoftware.org/isinfo.php) (optional; if missing, only the binary is published to `publish/win-x64/`).
- **Output:** `publish/installers/star-cli-1.0.0-win-x64.exe`. The installer installs to `Program Files\OASIS STAR CLI` and can add it to the system PATH.

To change the version, edit the `#define MyAppVersion` line in `star-cli.iss` (and/or pass version from the build script in a future update).
