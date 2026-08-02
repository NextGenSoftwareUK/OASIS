# Windows Integration Guide for ODuke3D (Duke Nukem 3D + STAR API)

This guide covers integrating the OASIS STAR API into your EDuke32 fork so it becomes **ODuke3D**: cross-game keys and inventory shared across **ODOOM**, **OQuake**, **ODOOM3** (Doom 3 classic), **ODOOM3-BFG** (Doom 3 BFG), and **ODuke3D-RT** (ray-traced Duke).

## Credits and license

**ODuke3D is based on EDuke32.** Full credit goes to [EDuke32](https://eduke32.com) (Jonathon Fowler, Richard Gobeille, and contributors). EDuke32 is licensed under the **GNU General Public License v2.0 (GPL-2.0)**. When you build or distribute ODuke3D, comply with that license and give appropriate credit.

**Duke Nukem 3D game data** is property of Gearbox Software / 3D Realms. You must own a licensed copy.

## Prerequisites

1. **MinGW-w64** with `gcc` and `mingw32-make` in PATH — **or** Visual Studio 2019+ with C++ workload for MSVC builds
2. **SDL2** development headers and libraries (EDuke32 depends on SDL2)
3. **ODuke3D source** (EDuke32 fork) at `C:\Source\ODuke3D`
4. **STAR API credentials** (username/password or API key)

## Step 1: Build OGEngineClient

From the OASIS root run **BUILD_AND_DEPLOY_STAR_CLIENT.bat** or:

```powershell
cd C:\Source\OASIS-master
dotnet publish "OASIS Omniverse\OGEngineClient\OGEngineClient.csproj" `
    -c Release -r win-x64 -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
```

Output: `OASIS Omniverse\OGEngineClient\bin\Release\net8.0\win-x64\publish\ogengine.dll` and `native\ogengine.lib`.

## Step 2: Set Environment Variables

```batch
set STAR_USERNAME=your_oasis_username
set STAR_PASSWORD=your_oasis_password
REM — or — 
set OGENGINE_KEY=your_api_key
set STAR_AVATAR_ID=your_avatar_id
```

## Step 3: Build ODuke3D

From **OASIS Omniverse\ODuke3D** run:

```batch
BUILD_ODUKE3D.bat
```

This copies integration files into `C:\Source\ODuke3D\source\duke3d\src\` and runs `mingw32-make EDUKE32_STANDALONE=1`. Output: `eduke32.exe` in the ODuke3D source root.

Or use `Scripts\COPY_TO_EDUKE32_AND_BUILD.ps1` for manual control:

```powershell
.\Scripts\COPY_TO_EDUKE32_AND_BUILD.ps1 -EDuke32Src "C:\Source\ODuke3D"
```

## Step 4: Add Engine Hooks

See **[INTEGRATION_INSTRUCTIONS.md](INTEGRATION_INSTRUCTIONS.md)** for the exact hook points.  Key locations in EDuke32 source:

| Hook | File | Function |
|------|------|----------|
| Init | `source/duke3d/src/game.cpp` | `app_main()` end |
| Cleanup | `source/duke3d/src/game.cpp` | `G_GameExit()` |
| Tick | `source/duke3d/src/game.cpp` | `G_Tics()` |
| Key pickup | `source/duke3d/src/player.cpp` | `P_CheckInventory()` |
| Door access | `source/duke3d/src/sector.cpp` | `G_OperateSectors()` |
| Actor kill | `source/duke3d/src/actors.cpp` | `A_DamageObject()` |
| HUD draw | `source/duke3d/src/screentext.cpp` | after `G_DrawStatusBar()` |
| Key input | `source/duke3d/src/game.cpp` | `G_ProcessInput()` |
| Face tile | `source/duke3d/src/screentext.cpp` | status bar face draw |

## Step 5: Run

Place `duke3d.grp` (Duke Nukem 3D game data) at `C:\Duke3D\duke3d.grp`, then:

```batch
RUN_ODUKE3D.bat
```

Or:

```batch
C:\Source\ODuke3D\eduke32.exe -j C:\Duke3D
```

Console should show:

```
[DUKE3D] OASIS STAR API: Authenticated. Cross-game keys enabled.
[DUKE3D] ODuke3D 1.0.0 initialised.
```

## Cross-Game Key Table

| ODuke3D door | Keys that open it |
|-------------|-------------------|
| Blue key card | ODuke3D blue_key, ODOOM blue/yellow keycard, OQuake gold_key, ODOOM3 blue_key/yellow_key, ODOOM3-BFG blue_key, ODuke3D-RT blue_key |
| Red key card | ODuke3D red_key, ODOOM red keycard, OQuake silver_key, ODOOM3 red_key, ODOOM3-BFG red_key, ODuke3D-RT red_key |
| Yellow key card | ODuke3D yellow_key, ODOOM yellow keycard, OQuake gold_key, ODOOM3 yellow_key, ODOOM3-BFG yellow_key, ODuke3D-RT yellow_key |

## OASIS GUI Controls

| Key | Action |
|-----|--------|
| **I** | Toggle OASIS Inventory popup |
| **Q** | Toggle OASIS Quest popup |
| **Esc** | Close any open OASIS popup |
| **↑ / ↓** | Navigate items in popup |
| **U** | Use selected item |
| **A** | Send item to OASIS Avatar |
| **C** | Send item to OASIS Clan |

## Troubleshooting

- **`make`: command not found** — Install MinGW-w64 and add `C:\mingw64\bin` to PATH.
- **SDL2 not found** — EDuke32 requires SDL2 development headers; install via MinGW package manager or copy SDL2 into the source tree.
- **ogengine.lib/dll not found** — Build OGEngineClient first (Step 1).
- **No cross-game keys** — Ensure STAR env vars are set and console shows "Authenticated".
- **duke3d.grp not found** — Use `-j path\to\gamedata` to point to your Duke Nukem 3D data.

**Credits:** ODuke3D is based on [EDuke32](https://eduke32.com) (GPL-2.0). Duke Nukem 3D © Gearbox Software / 3D Realms.
