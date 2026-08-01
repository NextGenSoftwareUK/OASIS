# Windows Integration Guide for OQuake (Quake + STAR API)

This guide is for integrating the OASIS STAR API into your Quake fork so that **OQuake** can share keys with **ODOOM**, **ODOOM3** (Doom 3 classic, dhewm3 fork), **ODOOM3-BFG** (Doom 3 BFG, RBDOOM-3-BFG fork), **ODuke3D** (Duke Nukem 3D, EDuke32 fork), **ODuke3D-RT** (Duke Nukem 3D ray-traced, Duke-RT fork), and **OWolf3D** (Wolfenstein 3D, ECWolf fork): keys collected in any of the seven OASIS Omniverse games open doors in the others.

## Credits and license

**OQuake is based on vkQuake.** Full credit goes to the [vkQuake](https://github.com/Novum/vkQuake) project (Novum). vkQuake is licensed under the GNU General Public License v2.0 (GPL-2.0). When you build or distribute OQuake, you must comply with vkQuake’s license and give appropriate credit. See **[CREDITS_AND_LICENSE.md](CREDITS_AND_LICENSE.md)** in Docs for details.

**Loading / splash screen:** OQuake shows an **OASIS / OQuake** text splash in the console at startup (like ODOOM). A graphic splash image (**`Images/oasis_splash.png`**) is provided for use as a loading screen in vkQuake; see **vkquake_oquake\VKQUAKE_OQUAKE_INTEGRATION.md** section 7 for how to integrate it.

**Quake source (QuakeC):** `C:\Source\quake-rerelease-qc`  
**OQuake integration files:** `OASIS Omniverse\OQuake` (Code/, Scripts/, Version/, Docs/, Images/, vkquake_oquake/).

**One-click script (same as ODOOM/UZDoom):** From `C:\Source\OASIS-master` run:

```bat
"OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"
```

This builds the STAR API if needed, then copies all OQuake files and star_api into `C:\Source\quake-rerelease-qc`. Use `BUILD_OQUAKE.bat run` to launch your engine after setting `QUAKE_ENGINE_EXE` in the script.

## Game data (fixing "couldn't load gfx.wad")

vkQuake needs the **Quake game data**. The engine uses a **basedir** (default: the folder containing `vkquake.exe`) and expects:

- An **id1** subdirectory containing **pak0.pak** and **pak1.pak** (from the original Quake or a re-release).
- **gfx.wad** in the basedir or in **id1** (depending on your data layout).

**Option A – Use a separate game directory (recommended)**  
Create a folder for game data (e.g. `C:\Quake`). Inside it put:

- **id1\pak0.pak**, **id1\pak1.pak**
- **gfx.wad** (in the same folder as the exe, or in **id1**)

Then run vkQuake with:

```bat
vkquake.exe -basedir C:\Quake
```

**Option B – Copy game data next to the exe**  
From your Quake install (Steam, GOG, or original), copy into the vkQuake output folder (e.g. `Build-vkQuake\x64\Release`):

- The **id1** folder (with pak0.pak, pak1.pak)
- **gfx.wad** (if your Quake install has it in the root, copy it into the Release folder or into id1)

You must own Quake to use the game data; get it from [Steam](https://store.steampowered.com/app/2310/Quake/), [GOG](https://www.gog.com/game/quake_the_offering), or similar.

## Prerequisites

1. **Visual Studio** (2019 or later) with C++ development tools
2. **CMake** (3.10 or later)
3. **Quake engine source** that builds the executable which runs the QuakeC from `quake-rerelease-qc`
4. **STAR API credentials** (same as ODOOM: SSO or API key)
5. **Vulkan SDK** (for vkQuake) – https://vulkan.lunarg.com/sdk/home

**STAR API client:** Use **STARAPIClient** only (see `OASIS Omniverse/STARAPIClient/README.md`). Do not use NativeWrapper.

## Step 1: Build the STAR API client (STARAPIClient)

From OASIS root run **BUILD_AND_DEPLOY_STAR_CLIENT.bat** or:

```powershell
cd C:\Source\OASIS-master
dotnet publish "OASIS Omniverse\STARAPIClient\STARAPIClient.csproj" -c Release -r win-x64 -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
```

Output: `OASIS Omniverse\STARAPIClient\bin\Release\net8.0\win-x64\publish\star_api.dll` and `native\star_api.lib`.

## Step 2: Set Environment Variables

Same as ODOOM (see `Doom\WINDOWS_INTEGRATION.md` Step 2). Use `STAR_USERNAME`/`STAR_PASSWORD` or `STAR_API_KEY`/`STAR_AVATAR_ID`.

## Step 3: Copy OQuake Integration Files to Your Quake Tree

Run **BUILD_OQUAKE.bat** – it copies integration and star_api into `QUAKE_SRC` (quake-rerelease-qc) and, if **VKQUAKE_SRC** is set, into vkQuake's Quake folder and builds vkQuake. Run from **Developer Command Prompt for VS 2022** so MSBuild is in PATH.

Or copy manually: see **Scripts/COPY_TO_QUAKE_AND_BUILD.ps1** and **vkquake_oquake\apply_oquake_to_vkquake.ps1**.

## Step 4: Engine Modifications (C Code)

QuakeC cannot call C functions directly. The **engine** must:

1. Call `OQuake_STAR_Init()` at startup and `OQuake_STAR_Cleanup()` at shutdown.
2. When the player picks up a key: `OQuake_STAR_OnKeyPickup("silver_key")` or `"gold_key"`.
3. When the player touches a key door and **does not** have the required key locally: `OQuake_STAR_CheckDoorAccess(door_targetname, "silver_key")` or `"gold_key"`. If it returns 1, open the door.

Include in the engine (e.g. `host.c`): `#include "oquake_star_integration.h"`. See **Code/engine_oquake_hooks.c.example** and **vkquake_oquake\VKQUAKE_OQUAKE_INTEGRATION.md** for vkQuake.

## Step 5: QuakeC and engine builtins

The QuakeC in quake-rerelease-qc declares `OQuake_OnKeyPickup` and `OQuake_CheckDoorAccess` as extension builtins (`#0:ex_OQuake_*`). The engine must register these in pr_ext.c and implement them via **pr_ext_oquake.c** (calling `OQuake_STAR_OnKeyPickup` and `OQuake_STAR_CheckDoorAccess`). See **VKQUAKE_OQUAKE_INTEGRATION.md** in vkquake_oquake.

## Step 6: Build System

Add `oquake_star_integration.c` and `pr_ext_oquake.c` to the engine project. Link `star_api.lib` and `winhttp.lib` (Windows). Ensure `star_api.dll` is next to the built exe.

## Cross-Game Key Mapping

All four OASIS Omniverse games share keys via the STAR API. Equivalences are configured in `oasisstar.json` on the server side; representative defaults:

| OQuake door | Keys that open it (local + cross-game) |
|-------------|----------------------------------------|
| Silver key  | OQuake silver_key, ODOOM red_keycard / skull_key, ODOOM3 red_key, ODOOM3-BFG red_key |
| Gold key    | OQuake gold_key, ODOOM blue_keycard / yellow_keycard, ODOOM3 blue_key / yellow_key, ODOOM3-BFG blue_key / yellow_key |

| ODOOM door  | Keys that open it (local + cross-game) |
|-------------|----------------------------------------|
| Red         | ODOOM red_keycard, OQuake silver_key, ODOOM3 red_key, ODOOM3-BFG red_key |
| Blue / Yellow | ODOOM blue/yellow keycard, OQuake gold_key, ODOOM3 blue_key / yellow_key, ODOOM3-BFG blue_key / yellow_key |

| ODOOM3 door | Keys that open it (local + cross-game) |
|-------------|----------------------------------------|
| Red key     | ODOOM3 red_key, OQuake silver_key, ODOOM red_keycard / skull_key, ODOOM3-BFG red_key, ODuke3D/RT red_key |
| Blue / Yellow key | ODOOM3 blue_key / yellow_key, OQuake gold_key, ODOOM blue/yellow keycard, ODOOM3-BFG blue_key / yellow_key, ODuke3D/RT blue_key / yellow_key |

| ODuke3D / ODuke3D-RT door | Keys that open it (local + cross-game) |
|---------------------------|----------------------------------------|
| Blue key card  | ODuke3D blue_key, OQuake gold_key, ODOOM blue/yellow keycard, ODOOM3/ODOOM3-BFG blue_key/yellow_key, ODuke3D-RT blue_key, OWolf3D gold_key |
| Red key card   | ODuke3D red_key, OQuake silver_key, ODOOM red_keycard, ODOOM3/ODOOM3-BFG red_key, ODuke3D-RT red_key, OWolf3D silver_key |
| Yellow key card| ODuke3D yellow_key, OQuake gold_key, ODOOM yellow keycard, ODOOM3/ODOOM3-BFG yellow_key, ODuke3D-RT yellow_key, OWolf3D gold_key |

| OWolf3D door | Keys that open it (local + cross-game) |
|--------------|----------------------------------------|
| Gold Key (lock 1) | OWolf3D gold_key, OQuake gold_key, ODOOM blue/yellow keycard, ODOOM3/ODOOM3-BFG blue_key, ODuke3D/RT blue_key |
| Silver Key (lock 2) | OWolf3D silver_key, OQuake silver_key, ODOOM red_keycard, ODOOM3/ODOOM3-BFG red_key, ODuke3D/RT red_key |

## Testing

1. Run OQuake with STAR env vars set. Console should show: `OQuake STAR API: Authenticated. Cross-game keys enabled.`
2. Pick up silver key in OQuake → console: `OQuake STAR API: Added silver_key to cross-game inventory.`
3. In ODOOM pick up red keycard, then in OQuake open a silver door without the local key → door should open via cross-game key.

## Troubleshooting

- **star_api.lib/dll not found:** Build **STARAPIClient** (run BUILD_AND_DEPLOY_STAR_CLIENT.bat, or answer Y when BUILD_OQUAKE.bat asks to build and deploy STARAPIClient first).
- **MSBuild not in PATH:** Open **Developer Command Prompt for VS 2022** and run BUILD_OQUAKE.bat from there.
- **No cross-game keys:** Ensure STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID are set and init succeeds.
- **gfx.wad / id1:** Use `-basedir` to point to your Steam Quake install or copy id1 and gfx.wad next to the exe.

**Credits:** OQuake is based on [vkQuake](https://github.com/Novum/vkQuake) (Novum, GPL-2.0). See **Docs/CREDITS_AND_LICENSE.md**.
