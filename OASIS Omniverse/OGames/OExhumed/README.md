# OExhumed — Exhumed / Powerslave + OASIS STAR API

**OExhumed** is a fork of [Raze](https://github.com/ZDoom/Raze) targeting the **Exhumed** (also known as Powerslave) source port with the **OASIS STAR API** integrated, bringing this cult BUILD-engine classic into the OASIS Omniverse.

Engine: Raze (BUILD engine reimplementation — GPL-2.0, supports Blood, Duke Nukem 3D, Exhumed, Shadow Warrior)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+. Exhumed game data (`game.dat` / `stuff.dat`).
2. **Build:**
   ```bat
   BUILD_OEXHUMED.bat
   ```
3. **Run:** Set `EXHUMED_DATA` to your game data directory and launch `raze.exe -game exhumed`.
4. **STAR API:** In-game console: `star beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OEXHUMED.sh
```

---

## OASIS features

| Key | Action |
|-----|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup |
| **↑ / ↓** | Navigate popup list |
| **Esc** | Close popup |

HUD overlays: username label (top-left), XP counter (top-right), toast notifications (centre).

---

## Cross-game keys

| Exhumed item | Cross-game key | Other games |
|--------------|---------------|-------------|
| Scarab Amulet | `oasis_relic_scarab` | OASIS-exclusive cross-game quest item |
| Eye of Ra | `oasis_relic_eye` | OASIS-exclusive cross-game quest item |
| Map Scroll | `oasis_map_scroll` | Cross-game map reveal item |

---

## Architecture

```
oexhumed_ogengine_integration.cpp   (Raze/Exhumed engine hooks)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

---

## Map editor

Exhumed maps use the **BUILD** engine format. Use **Mapster32** (via `EditorIntegrations/Mapster32/`) to place OASIS portal and trigger entities.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OExhumed is based on **Raze** (GPL-2.0) by Randy Heit and contributors.  
Exhumed / Powerslave is copyright Lobotomy Software / Playmates Interactive. You must own a copy to use OExhumed.
