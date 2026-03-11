# OQuake – Quake + OASIS STAR API

OQuake is Quake integrated with the **OASIS STAR API** so keys collected in **ODOOM** can open doors in Quake and vice versa (cross-game keys).

**OQuake is based on vkQuake.** Full credit to [vkQuake](https://github.com/Novum/vkQuake) (Novum). vkQuake is GPL-2.0. See **[Docs/CREDITS_AND_LICENSE.md](Docs/CREDITS_AND_LICENSE.md)** for credits and license obligations.

## Quick start

1. **Build and copy integration:** From the OASIS repo root run:
   ```bat
   "OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"
   ```
   This builds the STAR API if needed, copies OQuake files into `C:\Source\quake-rerelease-qc` and (if `VKQUAKE_SRC` is set) builds vkQuake with STAR. You need the **Vulkan SDK** and **Visual Studio**; run from **Developer Command Prompt for VS 2022** so MSBuild is in PATH.

2. **Run the game:** Use **RUN OQUAKE.bat** to launch OQuake with your Steam Quake basedir (edit the script to set `OQUAKE_BASEDIR` and `VKQUAKE_SRC` if needed).

3. **Game data:** vkQuake needs Quake game data (id1 with pak0.pak, pak1.pak, and gfx.wad). Use `-basedir` to point to your Steam Quake install or copy the data next to the exe. See **Docs/WINDOWS_INTEGRATION.md** for details.

## Documentation

| Document | Description |
|----------|-------------|
| [Docs/WINDOWS_INTEGRATION.md](Docs/WINDOWS_INTEGRATION.md) | Full Windows setup, game data, and troubleshooting |
| [Docs/CREDITS_AND_LICENSE.md](Docs/CREDITS_AND_LICENSE.md) | Credits to vkQuake and license obligations (GPL-2.0) |
| [Docs/RELEASE_NOTES.md](Docs/RELEASE_NOTES.md) | Version history and release notes |
| [Docs/CONTRIBUTING.md](Docs/CONTRIBUTING.md) | How to contribute |
| [LICENSE](LICENSE) | License summary and link to GPL-2.0 |
| [Docs/FILES_AND_VERSIONS.md](Docs/FILES_AND_VERSIONS.md) | Full file list and why there are multiple version files |

## Files in this folder

| File | Purpose |
|------|--------|
| **BUILD_OQUAKE.bat** | Build star_api, copy OQuake + STAR into quake-rerelease-qc and vkQuake, build vkQuake (OQUAKE.exe + star_api.dll copied to `build\`) |
| **RUN OQUAKE.bat** | If already built, launches OQuake with Steam basedir; otherwise runs BUILD_OQUAKE.bat then launches |
| **Code/oquake_star_integration.c/h** | STAR API integration (init, key pickup, door check) |
| **Code/oquake_version.h** | OQuake version/build for branding (from Version/oquake_version.txt) |
| **Code/engine_oquake_hooks.c.example** | Example engine hooks (Host_Init, key pickup, door) |
| **Scripts/** | generate_oquake_version.ps1, COPY_TO_QUAKE_AND_BUILD.ps1 |
| **Version/** | oquake_version.txt (edit for version), version_display.txt (generated) |
| **Docs/** | All documentation (.md) |
| **Images/** | face_anorak.png, oasis_splash.png |
| **vkquake_oquake/** | vkQuake-specific files (pr_ext_oquake.c, apply script, integration doc) |

## Engine (vkQuake)

**vkQuake** is the recommended engine (Vulkan, 2021 rerelease support). Set `VKQUAKE_SRC=C:\Source\vkQuake` in BUILD_OQUAKE.bat; the script will clone and build vkQuake and copy **OQUAKE.exe** and **star_api.dll** into `OQuake\build\`. See **Docs/ENGINE_RECOMMENDATION.md** for why vkQuake and **vkquake_oquake/VKQUAKE_OQUAKE_INTEGRATION.md** for host.c/pr_ext.c edits.

## Cross-game keys and inventory

- OQuake **silver_key** can open **ODOOM red** doors; ODOOM **red_keycard** can open OQuake **silver** doors.
- OQuake **gold_key** can open **ODOOM blue/yellow** doors; ODOOM **blue/yellow keycard** can open OQuake **gold** doors.
- **Ammo** (shells, nails, rockets, cells) syncs with the **actual pickup amount** (e.g. Shells +20 per box); totals persist correctly after quit/reload.
- In-game console **add_item** messages are shown only when **star debug** is on (`star debug on` / `star debug off` / `star debug status` in console).

Set **STAR_USERNAME** / **STAR_PASSWORD** or **STAR_API_KEY** / **STAR_AVATAR_ID** for the STAR API.
