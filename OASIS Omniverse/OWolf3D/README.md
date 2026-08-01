# OWolf3D — Wolfenstein 3D + OASIS STAR API

**OWolf3D** is a fork of [ECWolf](https://maniacsvault.net/ecwolf/) with the **OASIS STAR API** integrated,
bringing Wolfenstein 3D into the cross-game OASIS Omniverse.
Keys, inventory, XP, and quests are shared across all seven OASIS Omniverse games:
**ODOOM**, **OQuake**, **ODOOM3**, **ODOOM3-BFG**, **ODuke3D**, **ODuke3D-RT**, and **OWolf3D**.

Engine: ECWolf (extended Wolf3D source port — GPL-2.0, supports Wolf3D, Spear of Destiny, Noah's Ark)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, Wolf3D/Spear data files (`wolf3d.exe` etc.).
2. **Build:** From this folder run:
   ```bat
   BUILD_OWOLF3D.bat
   ```
3. **Run:** `RUN_OWOLF3D.bat` — or set `WOLF3D_DATA` to your game data directory.
4. **STAR API:** In-game console: `star beamin <username> <password>`

### Linux / macOS

```bash
export OWOLF3D_SRC=$HOME/Source/OWolf3D
./BUILD_OWOLF3D.sh
./RUN_OWOLF3D.sh
```

---

## OASIS GUI features

| Key | Action |
|-----|--------|
| **I** | Open / close OASIS Inventory popup |
| **Q** | Open / close OASIS Quest popup |
| **↑ / ↓** | Navigate popup list |
| **U** | Use selected inventory item |
| **A** | Send selected item to Avatar |
| **C** | Send selected item to Clan |
| **Esc** | Close popup |

---

## Cross-game keys

Wolf3D keys can unlock doors in any other OASIS Omniverse game, and vice versa:

| Wolf3D item | Cross-game key | Other games |
|-------------|---------------|-------------|
| Gold Key | `gold_key` | ODOOM Blue Key, OQuake Gold Key, ODuke3D Blue Key Card… |
| Silver Key | `silver_key` | ODOOM Yellow Key, OQuake Silver Key, ODuke3D Yellow Key Card… |

---

## Architecture

```
ECWolf (C++, src/)
      │
      │  7 hook calls  (actor.cpp, a_inventory.cpp, a_keys.cpp,
      │                  wolf_sbar.cpp, wl_main.cpp, wl_game.cpp, wl_play.cpp)
      ▼
owolf3d_star_integration.cpp   ← delta file (OASIS Omniverse/OWolf3D/)
      │
      ├── star_api.h            ← C ABI (links against star_api.dll)
      ├── star_sync.h           ← async auth/inventory helpers
      └── OGLib/oglib.h         ← header-only utility library
                │
                ▼
          star_api.dll          ← STARAPIClient (C# NativeAOT)
                │
                ▼
         OASIS STAR API
         (HTTP WEB5/WEB4)
```

---

## HUD overlays

When beamed in, OWolf3D adds:
- **Top-left:** `[ username ]` label (green)
- **Top-right:** XP counter (gold)
- **Bottom-right:** OWolf3D version string (dark grey)
- **Centre:** toast notifications (kill XP, item pickups, status)
- **I popup:** full inventory list with use/send controls
- **Q popup:** active quest list with description pane
- **Face:** status bar face switches to beamed-in avatar portrait

---

## Files

| File | Purpose |
|------|---------|
| `owolf3d_star_integration.h` | Public hook API |
| `owolf3d_star_integration.cpp` | Full integration implementation |
| `oasisstar.json` | Monster XP/mint table + key mapping |
| `BUILD_OWOLF3D.bat/.sh` | Build entry points |
| `RUN_OWOLF3D.bat/.sh` | Launch entry points |
| `Scripts/COPY_TO_ECWOLF_AND_BUILD.ps1` | Copy + CMake build script |
| `Docs/INTEGRATION_INSTRUCTIONS.md` | Hook location reference |
| `Docs/WINDOWS_INTEGRATION.md` | Step-by-step engine diff guide |

---

OWolf3D is based on **ECWolf** (GPL-2.0) by Braden Obrzut and contributors.  
Wolfenstein 3D is a registered trademark of id Software.
