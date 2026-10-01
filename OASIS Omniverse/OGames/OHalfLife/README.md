# OHalfLife

**OHalfLife** integrates Valve's Half-Life (1998) into the OASIS STAR API via [Xash3D FWGS](https://github.com/FWGS/xash3d-fwgs) — a cross-platform, open-source reimplementation of the GoldSrc engine — combined with [HLSDK-portable](https://github.com/FWGS/hlsdk-portable), the modernised open-source Half-Life game DLL.

This makes OHalfLife the only original-IP commercial game in the OGames portfolio: Valve open-sourced the HLSDK in 2001 and Xash3D provides the engine layer, so the full game logic is available for extension without any reverse engineering.

## Architecture

```
Half-Life assets (valve/)
        |
        v
  Xash3D FWGS engine  <----  OHalfLife.exe
        |
        v
  HLSDK game DLL (hl.dll / hl.so)
        |
        +-- ohalflife_ogengine_integration.cpp
                |
                v
          OGEngineClient.dll  -->  OASIS STAR API
```

The integration lives entirely inside the **game DLL** layer. Xash3D loads `valve/dlls/hl.dll` at runtime; the five hook sites below add OASIS telemetry without touching any Xash3D engine source.

## Hook Sites

| HLSDK file | Function | OASIS call |
|---|---|---|
| `dlls/game.cpp` | `GameDLLInit()` | `OHalfLife_STAR_Init(...)` |
| `dlls/game.cpp` | `GameDLLShutdown()` | `OHalfLife_STAR_Cleanup()` |
| `dlls/client.cpp` | `StartFrame()` | `OHalfLife_STAR_Tick()` |
| `dlls/monsters.cpp` | `CBaseMonster::Killed(...)` | `OHalfLife_STAR_OnMonsterKilled(...)` |
| `dlls/player.cpp` | `CBasePlayer::AddPlayerItem(.)` | `OHalfLife_STAR_OnItemPickup(...)` |

## Enemy XP Table

| Enemy | Classname | XP |
|---|---|---|
| Headcrab | `headcrab` | 10 |
| Zombie | `zombie` | 20 |
| Barnacle | `barnacle` | 5 |
| Snark | `snark` | 5 |
| Houndeye | `houndeye` | 15 |
| Bullsquid | `bullsquid` | 25 |
| Vortigaunt | `islave` | 30 |
| HECU Marine | `human_grunt` | 20 |
| Alien Controller | `controller` | 35 |
| Alien Grunt | `agrunt` | 40 |
| Alien Assassin | `female_assassin` | 40 |
| Auto-Turret | `turret` | 15 |
| Ichthyosaur | `ichthyosaur` | 45 |
| Tentacle | `tentacle` | 50 |
| Apache Helicopter | `apache` | 60 |
| Gargantua | `gargantua` | 100 |
| Gonarch | `gonarch` | 200 |
| Nihilanth *(final boss)* | `nihilanth` | 500 |

## Key Items (Cross-Game Portal Triggers)

| Item | Classname | OASIS Tag |
|---|---|---|
| HEV Suit | `item_suit` | `OASIS_ITEM_HEV_SUIT` |
| Long Jump Module | `item_longjump` | `OASIS_ITEM_LONGJUMP` |
| Egon Gun | `weapon_egon` | `OASIS_ITEM_EGON_GUN` |
| Gauss Gun | `weapon_gauss` | `OASIS_ITEM_GAUSS_GUN` |
| RPG Launcher | `weapon_rpg` | `OASIS_ITEM_RPG` |

## Cross-Game Portal

Picking up the **HEV Suit** (`item_suit`) in the first minutes of play triggers the OASIS portal sequence — a fitting narrative moment since Freeman puts on the suit before the resonance cascade that opens the Borderworld. Portal destinations are configured in the OASIS Hub.

## Building

### Windows

```
BUILD_OHALFLIFE.bat
```

Set `XASH3D_DIR` and `HLSDK_DIR` env vars if your repos are not in the default locations (`C:\Source\xash3d-fwgs` / `C:\Source\hlsdk-portable`).

### Linux / macOS

```bash
./BUILD_OHALFLIFE.sh
```

### Output

```
build/
  OHalfLife.exe        # Xash3D FWGS engine (renamed)
  OGEngineClient.dll   # OASIS OGEngine client
  oasisstar.json
  valve/
    dlls/
      hl.dll           # HLSDK game DLL with OASIS hooks
```

You must supply your own licensed Half-Life `valve/` data folder alongside the build output.

## Prerequisites

| Component | Repo | Licence |
|---|---|---|
| Xash3D FWGS | [FWGS/xash3d-fwgs](https://github.com/FWGS/xash3d-fwgs) | GPL-3.0 |
| HLSDK-portable | [FWGS/hlsdk-portable](https://github.com/FWGS/hlsdk-portable) | MIT / Valve HLSDK |
| OGEngineClient | `OGEditorSDK/` | OASIS Licence |
| Half-Life assets | Valve / Steam | Commercial |

## OASIS Hub Portal Entry

```json
{
  "gameId": "ohalflife",
  "displayName": "OHALFLIFE",
  "executableRelativePath": "../../OHalfLife/build/OHalfLife.exe",
  "workingDirectoryRelativePath": "../../OHalfLife/build",
  "defaultLevelArgument": "-game valve +map c0a0",
  "baseArguments": "-window",
  "portalX": -20.0,
  "portalZ": 28.0,
  "portalColorR": 1.0,
  "portalColorG": 0.55,
  "portalColorB": 0.0
}
```
