# ODOOM – Doom + OASIS STAR API

**ODOOM** is a fork of [UZDoom](https://github.com/UZDoom/UZDoom) with the **OASIS STAR API** integrated for cross-game features in the OASIS Omniverse. Keys collected in **OQuake** can open doors in ODOOM and vice versa.

ODOOM uses a native Windows/SDL2 stack with proper sound, music, and mouse handling. By NextGen World Ltd.

## Quick start

### macOS (Homebrew)

1. **Build:** From this folder run:
   ```bash
   ./build_odoom_mac.sh
   ```
   Installs all Homebrew deps, clones UZDoom, compiles it, and sets up OASIS mods automatically.
   Output: **uzdoom.app** in `~/Source/UZDoom/build/`.

2. **Run:**
   ```bash
   ~/Source/UZDoom/build/run_odoom_mac.sh
   ```
   Or `open ~/Source/UZDoom/build/uzdoom.app` directly.

3. **WAD:** The build script auto-downloads **Freedoom** (free). To use your own: copy `doom2.wad` to `~/Source/UZDoom/build/`.

4. **STAR API:** Export credentials before running:
   ```bash
   export STAR_USERNAME=youruser STAR_PASSWORD=yourpass
   ~/Source/UZDoom/build/run_odoom_mac.sh
   ```

### Windows

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
| [FILES_AND_VERSIONS.md](FILES_AND_VERSIONS.md) | Full file list and why there are multiple version files |

## Version

Version and build number are set in **odoom_version.txt** (line 1 = version, line 2 = build). The build script regenerates headers and launcher text from this file.

## Cross-game keys

- OQuake **gold_key** and **silver_key** are shared to STAR inventory from ODOOM pickups.
- OQuake **gold_key** and **silver_key** are OQuake-only door keys (they do not unlock ODOOM doors).

See [WINDOWS_INTEGRATION.md](WINDOWS_INTEGRATION.md) for STAR API setup and in-game console commands (`star version`, `star inventory`, etc.).

## Beamed-in face (OASFACE)

When you beam in, the status bar can show a custom face. The game looks for a texture named **OASFACE**. If it’s missing, the normal Doom guy face is used.

**How to add OASFACE:**

- **Ready-made:** Run `python create_odoom_face_pk3.py` from the ODOOM folder (or use the existing `odoom_face.pk3` there). Copy `odoom_face.pk3` next to ODOOM.exe so the beamed-in face appears.
- **PK3 (custom):** Put a 32×32 image as `textures/OASFACE.png` in a zip, rename to `odoom_face.pk3`. See [textures/README.txt](textures/README.txt).
- **WAD:** In SLADE, add a graphic lump named **OASFACE**, save your WAD, then run ODOOM with `-file your.wad`.
