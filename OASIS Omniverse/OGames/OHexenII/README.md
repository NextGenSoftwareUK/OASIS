# OHexenII — Hexen II + OASIS STAR API

**OHexenII** is a fork of [uhexen2](https://uhexen2.sourceforge.io/) with the **OASIS STAR API** integrated, bringing Hexen II into the OASIS Omniverse. Keys, inventory, XP, and quests are shared across all 20 OASIS Omniverse OGames.

Engine: uhexen2 (Quake-based Hexen II port — GPL-2.0)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, SDL2. Hexen II game data (`data1/` directory from retail).
2. **Build:**
   ```bat
   BUILD_OHEXEN2.bat
   ```
3. **Run:** Place alongside the `data1/` game data directory.
4. **STAR API:** Console command: `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OHEXEN2.sh
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

| Hexen II item | Cross-game key | Other games |
|---------------|---------------|-------------|
| Silver key | `silver_key` | OQuake Silver Key, OWolf3D Silver Key |
| Gold key | `gold_key` | OQuake Gold Key, OWolf3D Gold Key |
| Seven Portals key | `oasis_seven_portals` | OASIS-exclusive cross-game quest item |

---

## Architecture

Hexen II uses the **Quake engine** (id Tech 1) internally, so the integration pattern mirrors OQuake:

```
ohexen2_ogengine_integration.cpp   (progs.dat QuakeC hooks + engine patches)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

---

## Map editor

Hexen II maps use the **Quake BSP** format. Use **TrenchBroom** (via `EditorIntegrations/TrenchBroom/`) to place OASIS portal and trigger entities — the same editor used for OQuake and OQuake2.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OHexenII is based on **uhexen2** (GPL-2.0) by O.Sezer and contributors.  
Hexen II is copyright Raven Software / id Software. You must own a copy to use OHexenII.
