# OQuake – Quake + OASIS STAR API

OQuake is Quake integrated with the **OASIS STAR API** so keys collected in **ODOOM** can open doors in Quake and vice versa (cross-game keys).

**OQuake is based on vkQuake.** Full credit to [vkQuake](https://github.com/Novum/vkQuake) (Novum). vkQuake is GPL-2.0. See **[CREDITS_AND_LICENSE.md](CREDITS_AND_LICENSE.md)** for credits and license obligations.

## Quick start

1. **Build and copy integration:** From the OASIS repo root run:
   ```bat
   "OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"
   ```
   This builds the STAR API if needed, copies OQuake files into `C:\Source\quake-rerelease-qc` and (if `VKQUAKE_SRC` is set) builds vkQuake with STAR. You need the **Vulkan SDK** and **Visual Studio**; run from **Developer Command Prompt for VS 2022** so MSBuild is in PATH.

2. **Run the game:** Use **BUILD & RUN OQUAKE.bat** to launch OQuake with your Steam Quake basedir (edit the script to set `OQUAKE_BASEDIR` and `VKQUAKE_SRC` if needed).

3. **Game data:** vkQuake needs Quake game data (id1 with pak0.pak, pak1.pak, and gfx.wad). Use `-basedir` to point to your Steam Quake install or copy the data next to the exe. See **WINDOWS_INTEGRATION.md** for details.

## Files in this folder

| File | Purpose |
|------|--------|
| **BUILD_OQUAKE.bat** | Build star_api, copy OQuake + STAR into quake-rerelease-qc and vkQuake, build vkQuake (OQUAKE.exe + star_api.dll copied to `build\`) |
| **BUILD & RUN OQUAKE.bat** | If already built, launches OQuake with Steam basedir; otherwise runs BUILD_OQUAKE.bat then launches |
| **oquake_star_integration.c/h** | STAR API integration (init, key pickup, door check) |
| **oquake_version.h** | OQuake version/build for branding |
| **engine_oquake_hooks.c.example** | Example engine hooks (Host_Init, key pickup, door) |
| **WINDOWS_INTEGRATION.md** | Full Windows setup, game data, and troubleshooting |
| **vkquake_oquake/** | vkQuake-specific files (pr_ext_oquake.c, apply script, integration doc) |

## Engine (vkQuake)

**vkQuake** is the recommended engine (Vulkan, 2021 rerelease support). Set `VKQUAKE_SRC=C:\Source\vkQuake` in BUILD_OQUAKE.bat; the script will clone and build vkQuake and copy **OQUAKE.exe** and **star_api.dll** into `OASIS Omniverse\OQuake\build\`. See **ENGINE_RECOMMENDATION.md** for why vkQuake and **VKQUAKE_OQUAKE_INTEGRATION.md** (in vkquake_oquake) for host.c/pr_ext.c edits.

## Cross-game keys

- OQuake **silver_key** can open **ODOOM red** doors; ODOOM **red_keycard** can open OQuake **silver** doors.
- OQuake **gold_key** can open **ODOOM blue/yellow** doors; ODOOM **blue/yellow keycard** can open OQuake **gold** doors.

Set **STAR_USERNAME** / **STAR_PASSWORD** or **STAR_API_KEY** / **STAR_AVATAR_ID** for the STAR API.
