# ORtCW — Return to Castle Wolfenstein + OASIS STAR API

**ORtCW** is a fork of [iortcw](https://github.com/iortcw/iortcw) with the **OASIS STAR API** integrated, bringing Return to Castle Wolfenstein into the OASIS Omniverse. Keys, inventory, XP, and quests are shared across all 20 OASIS Omniverse OGames.

Engine: iortcw (id Tech 3 — GPL-2.0)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+. RtCW game data (`main/` directory from retail or Steam).
2. **Build:**
   ```bat
   BUILD_ORTCW.bat
   ```
3. **Run:** Point the executable at your `main/` game data directory.
4. **STAR API:** Open console (`~`): `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_ORTCW.sh
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

| RtCW item | Cross-game key | Other games |
|-----------|---------------|-------------|
| Gold key | `gold_key` | OWolf3D Gold Key, OQuake Gold Key |
| Silver key | `silver_key` | OWolf3D Silver Key, OQuake Silver Key |
| Treasure items | `oasis_treasure_*` | XP-rewarding OASIS cross-game loot |

---

## Architecture

RtCW uses **id Tech 3** (same engine as Quake 3), so the integration pattern mirrors OQuake3:

```
ortcw_ogengine_integration.cpp   (cgame/game DLL hooks)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

Hook sites: `G_InitGame`, `G_ShutdownGame`, `ClientConnect`, `ClientThink`, `Pickup_*` functions.

---

## Map editor

**NetRadiant** / **OQuake3Editor** (supports id Tech 3 / RtCW map format) — see `EditorIntegrations/NetRadiant/` for OASIS entity definitions. The same editor used for OQuake3.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

ORtCW is based on **iortcw** (GPL-2.0) by iortcw contributors.  
Return to Castle Wolfenstein is copyright id Software / Activision. You must own a copy to use ORtCW.
