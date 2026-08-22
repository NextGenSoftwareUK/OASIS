# OMorrowind — Morrowind + OASIS STAR API

**OMorrowind** is a fork of [OpenMW](https://openmw.org/) (the open-source Morrowind engine reimplementation) with the **OASIS STAR API** integrated. The first **Generation 2 OGame** — an open-world RPG in the OASIS Omniverse alongside 20 FPS OGames.

Walk through a portal in **ODOOM** and step out into **Vvardenfell**. Unique Morrowind items (Daedric Crescent, Wraithguard, Skeleton Key) enter your shared OASIS inventory. Creature kills award cross-game XP. OASIS quests can weave across Morrowind and any other OGame.

Engine: OpenMW (C++17 Morrowind reimplementation — GPL-3.0)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2022, CMake 3.15+, Qt 5.15+, Boost 1.78+, SDL2. Morrowind game data (`Data Files/` from retail or OpenMW launcher).
2. **Build:**
   ```bat
   BUILD_OMORROWIND.bat
   ```
3. **Run:** Point the OpenMW launcher at your Morrowind `Data Files/` directory.
4. **STAR API:** In-game console (`~`): `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OMORROWIND.sh
```

---

## OASIS features

| Key | Action |
|-----|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup (OASIS cross-game quests only) |
| **↑ / ↓** | Navigate popup list |
| **Esc** | Close popup |

HUD overlays (rendered via MyGUI): username label (top-left), XP counter (top-right), toast notifications (centre).

The native Morrowind journal and quest log are unaffected — OASIS quests appear as a separate overlay.

---

## Cross-game portals

Portals are placed via the **OpenMW-CS** editor as `oasis_portal_enter` script objects on door entities or region transitions. The portal script calls `OMorrowind_STAR_RequestTeleport` which triggers the `ogengine_request_teleport` C ABI, sending the player to the target OGame.

| Portal type | Morrowind hook |
|-------------|---------------|
| Cell door | `World::teleportToRandomInnRoom` hook |
| Region trigger | MWSCRIPT `oasis_portal_enter` attached to a trigger activator |
| Fast travel | Ship / silt strider destination override |

---

## Notable cross-game items

Key items and legendary artifacts from Morrowind enter the shared OASIS inventory and can affect other OGames:

| Morrowind item | Cross-game ID | Effect in other OGames |
|----------------|--------------|------------------------|
| Skeleton Key | `mw_skeleton_key` | Opens any locked door in any OGame |
| Wraithguard | `mw_wraithguard` | Legendary weapon — cross-game NFT |
| Sunder | `mw_sunder` | Legendary weapon — cross-game NFT |
| Keening | `mw_keening` | Legendary weapon — cross-game NFT |
| Daedric Crescent | `mw_daedric_crescent` | Legendary weapon — cross-game NFT |
| Ebony Mail | `mw_ebony_mail` | Legendary armor — cross-game NFT |

---

## Architecture

```
omorrowind_ogengine_integration.cpp   (OpenMW engine hooks — apps/openmw/)
         ↓
lua/player.lua + lua/global.lua       (OpenMW Lua addon — OASIS.omwaddon)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

Hook sites in OpenMW source:
- `apps/openmw/engine.cpp` — `Engine::go()` → `OMorrowind_STAR_Init`
- `apps/openmw/engine.cpp` — `Engine::frame()` → `OMorrowind_STAR_Tick`
- `apps/openmw/mwworld/inventorystore.cpp` — `InventoryStore::add()` → `OMorrowind_STAR_OnItemPickup`
- `apps/openmw/mwmechanics/combat.cpp` — actor death → `OMorrowind_STAR_OnActorKilled`
- `apps/openmw/mwgui/hud.cpp` — `HUD::onFrame()` → `OMorrowind_STAR_DrawHUDStatus`
- `apps/openmw/mwinput/inputmanagerimp.cpp` — `keyPressed()` → `OMorrowind_STAR_HandleKey`

---

## Lua addon (OASIS.omwaddon)

The `lua/` subfolder contains the OpenMW Lua companion addon scripts. Package them into `OASIS.omwaddon` (a zip archive) and place alongside the OpenMW executable:

```
OASIS.omwaddon/
├── mod.conf           — addon manifest
├── scripts/
│   ├── OASIS/
│   │   ├── global.lua   — portal cell transition handler
│   │   └── player.lua   — HUD widget and quest overlay
```

The Lua scripts communicate with the C++ integration layer via OpenMW's engine extensions API.

---

## Files

| File | Purpose |
|------|---------|
| `omorrowind_ogengine_integration.h` | Public hook API |
| `omorrowind_ogengine_integration.cpp` | Full integration implementation |
| `oasisstar.json` | Creature XP table + notable item / key mapping |
| `BUILD_OMORROWIND.bat/.sh` | Build entry points |
| `lua/global.lua` | Portal and cell-transition Lua handler |
| `lua/player.lua` | HUD and quest overlay Lua handler |

---

## Map editor

**OpenMW-CS** (ships with OpenMW) — attach `oasis_portal_enter` scripts to door or activator objects. OpenMW-CS supports the full Morrowind Construction Set workflow.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OMorrowind is based on **OpenMW** (GPL-3.0) by the OpenMW contributors.  
Morrowind is copyright Bethesda Softworks. You must own a copy to use OMorrowind.
