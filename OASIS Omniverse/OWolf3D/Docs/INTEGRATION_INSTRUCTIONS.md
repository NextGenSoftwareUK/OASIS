# OWolf3D — OASIS STAR Integration Instructions

**Port:** ECWolf (extended Wolf3D source port, C++, CMake)
**Game source ID:** `OWOLF3D`
**Build output:** `ecwolf.exe` (monolithic executable)

**Cross-game partners:** ODOOM, OQuake, ODOOM3, ODOOM3-BFG, ODuke3D, ODuke3D-RT, and **OWolf3D** — seven OASIS Omniverse games total.
Keys, inventory, XP, and quests are shared across all seven.

---

## Prerequisites

| Tool | Version |
|------|---------|
| Visual Studio | 2019 or later (C++ Desktop workload) |
| CMake | 3.15+ |
| SDL2 | 2.x |
| Wolf3D / Spear of Destiny data | original game data files |

- ECWolf source cloned at `C:\Source\OWolf3D` (or set `OWOLF3D_SRC`)
- OGEngineClient built; `ogengine.dll` / `ogengine.lib` in `OASIS Omniverse\OGEngineClient\`

---

## Build (one command)

```bat
BUILD_OWOLF3D.bat
```

Or for CI/batch mode:

```bat
BUILD_OWOLF3D.bat batch
```

The script:
1. Copies `owolf3d_ogengine_integration.h/.cpp` → `C:\Source\OWolf3D\src\`
2. Copies `OGLib\` headers → `C:\Source\OWolf3D\src\OGLib\`
3. Copies `ogengine.h`, `ogengine_sync.h`, `ogengine.lib`, `ogengine.dll` → `src\`
4. Patches `src\CMakeLists.txt` to include the new source file and link `ogengine.lib`
5. CMake-configures and builds the `engine` target (→ `ecwolf.exe`)
6. Deploys `ogengine.dll` and `oasisstar.json` next to the output

---

## Engine hook locations (7 hook calls total)

| File | Function | Hook |
|------|----------|------|
| `src/wl_main.cpp` | `InitGame()` — end of function | `OWolf3D_STAR_Init()` |
| `src/wl_main.cpp` | `Quit()` — before `SDL_Quit()` | `OWolf3D_STAR_Cleanup()` |
| `src/wl_game.cpp` | `GameLoop()` — top of main loop body | `OWolf3D_STAR_Tick()` |
| `src/actor.cpp` | `AActor::Die()` — after `FL_COUNTKILL` check | `OWolf3D_STAR_OnActorKilled(classname, isBoss)` |
| `src/g_shared/a_inventory.cpp` | `AInventory::TryPickup()` — after item granted | `OWolf3D_STAR_OnItemPickup(classname, inv_name)` |
| `src/g_shared/a_keys.cpp` | `P_CheckKeys()` — in the `return false` path | `OWolf3D_STAR_CheckDoorAccess(keynum)` |
| `src/wl_play.cpp` | `CheckKeys()` — near the top | `OWolf3D_STAR_HandleKeys()` |

Plus these draw and input hooks — see `Docs/WINDOWS_INTEGRATION.md`:

| File | Function | Hook |
|------|----------|------|
| `src/g_wolf/wolf_sbar.cpp` | `WolfStatusBar::DrawStatusBar()` — end | `OWolf3D_STAR_DrawHUDStatus()` + `OWolf3D_STAR_DrawPopupOverlay()` |
| `src/wl_agent.cpp` | player movement / `ControlMovement()` | `OWolf3D_STAR_ShouldBlockInput()` |
| `src/g_wolf/wolf_sbar.cpp` | `WolfStatusBar::UpdateFace()` | `OWolf3D_STAR_ShouldUseAvatarFace()` |

---

## Key number → cross-game mapping

| LOCKDEFS keynum | Class | Cross-game key |
|-----------------|-------|---------------|
| 1 | `GoldKey` | `gold_key` |
| 2 | `SilverKey` | `silver_key` |

---

## Monster class names (ECWolf DECORATE)

ECWolf identifies actors by DECORATE class name (string), retrieved with:
```cpp
actor->GetClass()->GetName().GetChars()
```

The integration does a case-insensitive lookup against `oasisstar.json → owolf3d.monsters[]`.

| Class name | Display name | XP | Boss |
|---|---|---|---|
| `Guard` | Brown Guard | 100 | — |
| `GreenGuard` | Green Guard | 100 | — |
| `Dog` | Dog | 50 | — |
| `Officer` | Officer | 400 | — |
| `WolfensteinSS` | SS Guard | 500 | — |
| `Mutant` | Mutant | 700 | — |
| `Hans` | Hans Grosse | 5000 | ✓ |
| `Schabbs` | Dr. Schabbs | 5000 | ✓ |
| `Gift` | Gen. Fettgesicht | 5000 | ✓ |
| `FatFace` | Fat Face | 5000 | ✓ |
| `Hitler` | Hitler | 5000 | ✓ |
| `MechaHitler` | Mecha-Hitler | 5000 | ✓ |
| `Gretel` | Gretel Grosse | 5000 | ✓ |
| `Trans` | Trans Grosse | 5000 | ✓ |
| `UberMutant` | Uber-Mutant (Spear) | 5000 | ✓ |
| `Wilhelm` | Barnacle Wilhelm (Spear) | 5000 | ✓ |
| `DeathKnight` | Death Knight (Spear) | 5000 | ✓ |
| `AngelOfDeath` | Angel of Death (Spear) | 5000 | ✓ |

---

## In-game console commands

| Command | Description |
|---------|-------------|
| `star` | Show command list |
| `star version` | Show STAR integration version |
| `star status` | Show init/auth status |
| `star beamin <user> <pass>` | Authenticate with OASIS |
| `star beamout` | Log out |
| `star inventory` | List STAR cross-game inventory |
| `star add <name>` | Add item (testing) |
| `star debug on\|off` | Toggle debug logging |

---

## Configuration (oasisstar.json)

Placed next to `ecwolf.exe` at runtime. Edit `owolf3d.monsters[]` to tune XP values.
