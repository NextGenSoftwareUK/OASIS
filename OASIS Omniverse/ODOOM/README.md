# ODOOM – Doom + OASIS STAR API

**ODOOM** is a fork of [UZDoom](https://github.com/UZDoom/UZDoom) with the **OASIS STAR API** integrated for cross-game features in the OASIS Omniverse. Keys collected in **OQuake** can open doors in ODOOM and vice versa.

ODOOM uses a native Windows/SDL2 stack with proper sound, music, and mouse handling. By NextGen World Ltd.

## Quick start

1. **Prerequisites:** Visual Studio 2019+, CMake, Python 3, UZDoom clone (e.g. `C:\Source\UZDoom`). See [WINDOWS_INTEGRATION.md](WINDOWS_INTEGRATION.md).

2. **Build:** From this folder run:
   ```batch
   BUILD ODOOM.bat
   ```
   Output: **ODOOM.exe** and DLLs in `ODOOM\build\`. Put your WAD (e.g. doom2.wad) there.

3. **Run:** Use **BUILD & RUN ODOOM.bat** to build (if needed) and launch, or run `build\ODOOM.exe` directly.

4. **STAR API:** Set `STAR_USERNAME` / `STAR_PASSWORD` or `STAR_API_KEY` / `STAR_AVATAR_ID` for cross-game keys and inventory.

## Documentation

| Document | Description |
|----------|-------------|
| [WINDOWS_INTEGRATION.md](WINDOWS_INTEGRATION.md) | Full Windows setup, build, STAR API, troubleshooting |
| [CREDITS_AND_LICENSE.md](CREDITS_AND_LICENSE.md) | Credits to UZDoom and license obligations (GPL-3.0) |
| [RELEASE_NOTES.md](RELEASE_NOTES.md) | Version history and release notes |
| [CONTRIBUTING.md](CONTRIBUTING.md) | How to contribute |
| [LICENSE](LICENSE) | License summary and link to GPL-3.0 |

## Version

Version and build number are set in **odoom_version.txt** (line 1 = version, line 2 = build). The build script regenerates headers and launcher text from this file.

## Cross-game keys

- ODOOM **red_keycard** ↔ OQuake **silver_key**
- ODOOM **blue/yellow keycard** ↔ OQuake **gold_key**

See [WINDOWS_INTEGRATION.md](WINDOWS_INTEGRATION.md) for STAR API setup and in-game console commands (`star version`, `star inventory`, etc.).
