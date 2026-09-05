# OHexen — Hexen + OASIS STAR API

**OHexen** is a fork of **UZDoom** (a GZDoom variant) targeting Hexen with the **OASIS STAR API** integrated, bringing Hexen into the OASIS Omniverse. Keys, inventory, XP, and quests are shared across all 20 OASIS Omniverse OGames.

Engine: UZDoom (GZDoom fork — GPL-3.0, supports Doom, Heretic, Hexen, Strife)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, zlib, SDL2. Hexen game data (`HEXEN.WAD`).
2. **Build:**
   ```bat
   BUILD_OHEXEN.bat
   ```
3. **Run:** Place `HEXEN.WAD` alongside the executable, or pass `-iwad HEXEN.WAD`.
4. **STAR API:** Open console (`~`): `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OHEXEN.sh
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

| Hexen item | Cross-game key | Other games |
|------------|---------------|-------------|
| Steel key | `blue_keycard` | ODOOM Blue Key, ODuke3D Blue Card |
| Cave key | `yellow_keycard` | ODOOM Yellow Key |
| Axe key | `red_keycard` | ODOOM Red Key, ODuke3D Red Card |
| Fire key | `fire_key` | OBlood Fire Key, OASIS quest item |
| Emerald key 1 / 2 | `oasis_emerald_key` | OASIS-exclusive cross-game quest item |
| Silver key | `silver_key` | OQuake Silver Key, OWolf3D Silver Key |
| Rusty key | `oasis_rusty_key` | OASIS-exclusive |
| Horn key | `oasis_horn_key` | OASIS-exclusive |
| Swamp key | `oasis_swamp_key` | OASIS-exclusive |
| Castle key | `oasis_castle_key` | OASIS-exclusive |

---

## Architecture

```
ohexen_ogengine_integration.cpp   (UZDoom engine hooks)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

---

## Map editor

**UltimateDoomBuilder** — see `EditorIntegrations/UltimateDoomBuilder/` for OASIS entity definitions. Hexen uses the Hexen map format; UDB supports it natively.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OHexen is based on **GZDoom / UZDoom** (GPL-3.0) by Randy Heit, Graf Zahl, and contributors.  
Hexen is copyright Raven Software / id Software. You must own a copy to use OHexen.
