# OBlood — Blood + OASIS STAR API

**OBlood** is a fork of [Raze](https://github.com/ZDoom/Raze) targeting the **Blood** source port with the **OASIS STAR API** integrated, bringing Blood into the OASIS Omniverse. Keys, inventory, XP, and quests are shared across all 20 OASIS Omniverse OGames.

Engine: Raze (BUILD engine reimplementation — GPL-2.0, supports Blood, Duke Nukem 3D, Exhumed, Shadow Warrior)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, ninja. Blood game data (`BLOOD.RFF` etc.).
2. **Build:**
   ```bat
   BUILD_OBLOOD.bat
   ```
3. **Run:** Set `BLOOD_DATA` to your game data directory and launch `raze.exe`.
4. **STAR API:** In-game console: `star beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OBLOOD.sh
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

HUD overlays: username label (top-left), XP counter (top-right), toast notifications (centre).

---

## Cross-game keys

| Blood item | Cross-game key | Other games |
|------------|---------------|-------------|
| Silver Key | `silver_key` | ODOOM Yellow Key, OQuake Silver Key, ODuke3D Yellow Card |
| Gold Key | `gold_key` | ODOOM Blue Key, OQuake Gold Key, ODuke3D Blue Card |
| Fire Key | `fire_key` | OASIS-exclusive cross-game quest item |
| Eye Key | `eye_key` | OASIS-exclusive cross-game quest item |

---

## Architecture

```
oblood_ogengine_integration.cpp   (Raze/Blood engine hooks)
         ↓
    OGLib  (shared C library — config, session, sync shims)
         ↓
  OGEngineClient  (C# NativeAOT → ogengine.dll)
         ↓
  WEB4 / WEB5 OASIS APIs
```

Hook sites: `GameInterface::StartGame`, `GameInterface::Ticker`, player item pickup, door check, actor kill.

---

## Map editor

Blood maps use the **BUILD** engine format. Use **Mapster32** (via `EditorIntegrations/Mapster32/`) to place `oasis_portal` and `oasis_trigger` entities.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OBlood is based on **Raze** (GPL-2.0) by Randy Heit and contributors.  
Blood is copyright GT Interactive Software / Atari. You must own a copy to use OBlood.
