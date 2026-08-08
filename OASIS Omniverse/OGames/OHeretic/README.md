# OHeretic — Heretic + OASIS STAR API

**OHeretic** is a fork of **UZDoom** (a GZDoom variant) targeting Heretic with the **OASIS STAR API** integrated, bringing Heretic into the OASIS Omniverse. Keys, inventory, XP, and quests are shared across all 20 OASIS Omniverse OGames.

Engine: UZDoom (GZDoom fork — GPL-3.0, supports Doom, Heretic, Hexen, Strife)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, zlib, SDL2. Heretic game data (`HERETIC.WAD`).
2. **Build:**
   ```bat
   BUILD_OHERETIC.bat
   ```
3. **Run:** Place `HERETIC.WAD` alongside the executable, or pass `-iwad HERETIC.WAD`.
4. **STAR API:** Open console (`~`): `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OHERETIC.sh
```

---

## OASIS features

| Key / console | Action |
|---------------|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup |
| **↑ / ↓** | Navigate popup list |
| **Esc** | Close popup |
| `` star_beamin `` | Log in to OASIS |

HUD overlays: username label (top-left), XP counter (top-right), toast notifications (centre).

---

## Cross-game keys

| Heretic item | Cross-game key | Other games |
|--------------|---------------|-------------|
| Yellow key | `yellow_keycard` | ODOOM Yellow Key, ODuke3D Yellow Card |
| Green key | `green_keycard` | OASIS-exclusive (maps to ODOOM Blue Key) |
| Blue key | `blue_keycard` | ODOOM Blue Key, ODuke3D Blue Card |

---

## Architecture

```
oheretic_ogengine_integration.cpp   (ZScript/engine hooks in GZDoom/UZDoom)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

Hook sites: `D_DoomMain`, game ticker, item pickup, key check, actor death.

---

## Map editor

**UltimateDoomBuilder** — see `EditorIntegrations/UltimateDoomBuilder/` for OASIS entity definitions. Heretic uses the Doom map format; UDB supports it natively.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OHeretic is based on **GZDoom / UZDoom** (GPL-3.0) by Randy Heit, Graf Zahl, and contributors.  
Heretic is copyright Raven Software / id Software. You must own a copy to use OHeretic.
