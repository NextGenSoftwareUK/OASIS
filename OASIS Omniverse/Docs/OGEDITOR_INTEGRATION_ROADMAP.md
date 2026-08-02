# OGEditor Integration Roadmap

This document covers the current state of OASIS Omniverse editor integration, the planned deep-integration layers, and the long-term goal of merging all forked editors into a single unified **OGEditor**.

---

## 1. Editor Inventory

| Editor | Base Project | Primary Games | OASIS Status |
|--------|-------------|---------------|--------------|
| **OQuakeEditor** | TrenchBroom | OQuake, OQuake2, OQuake3 | OASIS entity definitions only |
| **OQuake3Editor** | NetRadiant | OQuake3 | OASIS entity definitions only |
| **ODOOM3Editor** | DarkRadiant | ODOOM3, ODOOM3-BFG | OASIS entity definitions only |
| **UltimateDoomBuilder (UDB)** | UltimateDoomBuilder | ODOOM, ODuke3D, OWolf3D | Full OASISEditorSDK + OGEditorSDK (C#) |

---

## 2. OASIS Entity Definitions (Current)

Each editor has OASIS entity definitions in place. The canonical entities across all editors are:

### Portal entities (thing type 5900 — shared across all OGames)

| Entity | Type | Purpose |
|--------|------|---------|
| `oasis_portal_enter` | Brush entity (trigger volume) | Teleport **source** — player walks into this to travel to another map/game |
| `oasis_portal_exit` | Point entity | Teleport **destination** — player arrives here when teleporting in from another map/game |
| `oasis_portal` | Point entity (legacy) | Single undifferentiated portal point — use enter/exit pair for new maps |

### Scripting a cross-map / cross-game teleport

**In the source map** (e.g. an OQuake2 map):
```
Entity: oasis_portal_enter (brush, drawn around the portal doorway)
  oasis_game_id  = "odoom"         ← destination game
  oasis_map      = "e1m1"          ← destination map name
  oasis_exit_name = "portal_from_q2" ← must match targetname of the exit entity
  message        = "Enter the OASIS..."
```

**In the destination map** (e.g. an ODOOM map):
```
Entity: oasis_portal_exit (point, placed at the landing spot)
  targetname = "portal_from_q2"   ← matches oasis_exit_name above
  angle      = 90                  ← player faces north on arrival
```

The OGEngine STAR API resolves the connection at runtime: when the player steps into `oasis_portal_enter`, the engine calls `OGEngine_TeleportTo(game_id, map, exit_name)` which instructs the hub to load the correct OGame and spawn the player at the named exit.

### Per-game key entities

| Game | Entities |
|------|---------|
| OQuake | `oasis_key_silver` (5001), `oasis_key_gold` (5002) |
| OQuake2 / OQuake2-RTX | `oasis_key_blue` (6001), `oasis_key_red` (6002), `oasis_key_commanders_head` (6003) |
| ODOOM3 | `oasis_key_blue` (10001), `oasis_key_yellow` (10002), `oasis_key_red` (10003) |

---

## 3. What Is NOT Yet Implemented

The following integration layers do not yet exist in any editor and represent the work needed to reach the OGEditor vision:

- Cross-editor launch menus (no editor can currently open another)
- OGEngine connection panel (no editor shows OGEngine status or live inventory)
- STAR Web API browser (no editor can query the OASIS asset catalog from the server)
- Shared asset catalog JSON (catalog lives only in UDB's C# OASISAssetCatalog.cs)
- Map registration with STAR API (editors cannot publish a map's portal topology)
- Any form of inter-editor communication or shared state

---

## 4. Planned Integration Layers

### Phase 1 — Cross-Launch Protocol (Near-term)

Each editor gains a top-level **OASIS** menu with items to open the current map (or a selected file) in another editor. The editors discover each other via a shared config file:

**`%APPDATA%\OASIS\editor_config.json`** (Windows) / `~/.oasis/editor_config.json` (Linux/macOS):
```json
{
  "editors": {
    "oquake_editor":   "C:\\Source\\OQuakeEditor\\build\\OQuakeEditor.exe",
    "oquake3_editor":  "C:\\Source\\OQuake3Editor\\install\\netradiant.exe",
    "odoom3_editor":   "C:\\Source\\ODOOM3Editor\\install\\darkradiant.exe",
    "udb":             "C:\\Source\\UltimateDoomBuilder\\build\\Builder.exe"
  },
  "ogengine_url":  "http://localhost:8888",
  "star_api_url":  "http://localhost:7777"
}
```

**OASIS menu spec (identical across all editors):**
```
OASIS
├── Open Map In ▶
│   ├── OQuakeEditor (TrenchBroom)
│   ├── OQuake3Editor (NetRadiant)
│   ├── ODOOM3Editor (DarkRadiant)
│   └── UltimateDoomBuilder
├── ─────────────────────────────
├── Connect to OGEngine...
├── STAR API Status
├── ─────────────────────────────
├── Browse OASIS Assets...
├── View Portal Connections...
├── Register Map with OASIS...
└── About OGEditor Integration
```

**Implementation path per editor:**
- **OQuakeEditor (TrenchBroom)**: C++ plugin in `app/TrenchBroom/src/`, reads `editor_config.json`, spawns process via `QProcess`
- **OQuake3Editor (NetRadiant)**: C plugin module in `contrib/oasis/`, uses `g_spawn_async`
- **ODOOM3Editor (DarkRadiant)**: C++ plugin in `plugins/oasis/`, uses wxWidgets process spawn
- **UDB**: C# plugin in `Source/Plugins/OASISPlugin/`, uses `System.Diagnostics.Process`

---

### Phase 2 — OGEngine Panel (Near-term)

A dockable panel added to each editor that shows live OGEngine state. The panel polls `http://localhost:8888/api/status` (or the URL from `editor_config.json`).

**Panel sections:**

```
┌─ OGEngine ──────────────────────────────┐
│ ● Connected  http://localhost:8888       │
│ Avatar: PlayerOne  XP: 4,200            │
├─ Cross-game Inventory ──────────────────┤
│ 🔑 Blue Key (OQuake2)                   │
│ 🔑 Silver Key (OQuake)                  │
│ ⚔  Rocket Launcher (OQuake2)            │
├─ Active Portals ────────────────────────┤
│ portal_to_doom  → ODOOM / e1m1          │
│ portal_from_q2  ← OQuake2 / base1      │
└─────────────────────────────────────────┘
```

**API calls the panel makes:**
- `GET /api/status` — connection + avatar info
- `GET /api/inventory/{avatar_id}` — cross-game items
- `GET /api/portals?map={current_map}` — portal topology for current map

---

### Phase 3 — Shared Asset Catalog JSON (Medium-term)

Currently the canonical OASIS thing type catalog lives only in UDB's C# code:
`UltimateDoomBuilder/Source/OASISEditorSDK/OASISAssetCatalog.cs`

This needs to be exported as a machine-readable JSON file that all editors can consume regardless of their implementation language:

**`OASIS Omniverse/OGEngineClient/oasis_asset_catalog.json`** (to be generated):
```json
{
  "version": "1.0",
  "games": {
    "oquake": {
      "thing_range": [5001, 5899],
      "assets": [
        { "thing_type": 5001, "name": "Silver Key",  "classname": "item_key_silver", "category": "Keys" },
        { "thing_type": 5002, "name": "Gold Key",    "classname": "item_key_gold",   "category": "Keys" }
      ]
    },
    "oquake2": {
      "thing_range": [6001, 6899],
      "assets": [
        { "thing_type": 6001, "name": "Blue Key",    "classname": "item_key_blue_key", "category": "Keys" },
        { "thing_type": 6002, "name": "Red Key",     "classname": "item_key_red_key",  "category": "Keys" },
        ...
      ]
    }
  },
  "shared": {
    "portal": { "thing_type": 5900, "classname": "oasis_portal_enter" }
  }
}
```

Each editor reads this JSON on startup and auto-populates its entity/thing browser with OASIS entries, so the FGD/ENT/DEF files become optional fallbacks rather than the primary source of truth.

---

### Phase 4 — STAR Web API Browser (Medium-term)

An integrated browser panel allowing level designers to query the live OASIS STAR Web API without leaving the editor:

- Search assets by name, game, or category
- Click an asset to place it in the current map at the cursor position
- Preview cross-game portal connections (which exit points exist in connected maps)
- See which OASIS keys/items have been collected by registered avatars (useful for testing quest logic)
- Register the current map's portal topology with the STAR API so the Omniverse hub knows about it

**STAR Web API endpoints used:**
| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/assets` | GET | List all OASIS assets, filterable by game |
| `/api/portals` | GET/POST | Query/register portal connections for a map |
| `/api/maps/register` | POST | Publish a map's metadata to the OASIS hub |
| `/api/avatar/{id}/inventory` | GET | Check cross-game inventory for playtesting |
| `/api/quests` | GET | Browse active quests that touch this game |

---

## 5. OGEditor Merger (Long-term Vision)

The long-term goal is a single **OGEditor** application that handles all OASIS Omniverse game formats, replacing OQuakeEditor, OQuake3Editor, ODOOM3Editor, and UDB as separate tools.

### Recommended base: TrenchBroom (OQuakeEditor)

TrenchBroom is the most modern and actively maintained of the four base editors:
- C++17, Qt 6, cross-platform (Windows/macOS/Linux)
- Plugin architecture already partially present
- Best rendering quality and UX of the group
- Already handles Quake 1, 2, and 3 formats (via different GameConfig.cfg)
- Open MIT-ish license

NetRadiant and DarkRadiant are older C codebases with less active upstream development. UDB is C# and Windows-only.

### Merger phases

**Phase A — Unified entity browser**
All four editors share the `oasis_asset_catalog.json` and display OASIS things in a single filterable panel grouped by game.

**Phase B — Multi-format support in TrenchBroom**
Add Doom/Doom2 WAD format support to TrenchBroom (or fork UltimateDoomBuilder's map I/O into a shared library). Goal: OQuakeEditor can open and save Doom maps.

**Phase C — idTech4 / Doom3 format**
Add idTech4 `.map` format support. DarkRadiant already handles this; port its map parser to the unified editor.

**Phase D — OGEditor release**
Ship OGEditor as a standalone tool:
- Single installer covers all OASIS Omniverse game formats
- Built-in OGEngine panel (Phase 2 above)
- Built-in STAR Web API browser (Phase 4 above)
- Cross-game portal topology visualizer
- Map submission/publication flow to the OASIS hub

### Shared plugin architecture for OGEditor

```
OGEditor/
├── core/                    ← TrenchBroom base (modified)
├── formats/
│   ├── quake/               ← BSP, WAD, PAK (from TrenchBroom)
│   ├── quake2/              ← BSP2, PAK, .wal (from TrenchBroom)
│   ├── quake3/              ← BSP46, .pk3, .shader (from TrenchBroom/NetRadiant)
│   ├── doom/                ← WAD, Doom map format (from UDB)
│   ├── doom3/               ← idTech4 .map, .def, .pk4 (from DarkRadiant)
│   └── duke3d/              ← .map, .grp (new)
├── oasis/
│   ├── asset_catalog/       ← reads oasis_asset_catalog.json
│   ├── ogengine_panel/      ← Phase 2 panel
│   ├── star_api_browser/    ← Phase 4 panel
│   └── portal_visualizer/   ← cross-map portal graph
└── plugins/                 ← per-game plugins (OQuake, OQuake2, etc.)
```

---

## 6. Implementation Priority

| Priority | Task | Effort | Owner |
|----------|------|--------|-------|
| 1 | Export `oasis_asset_catalog.json` from UDB OASISAssetCatalog.cs | Small | OASIS team |
| 2 | Add OASIS menu + cross-launch to OQuakeEditor (TrenchBroom plugin) | Medium | OQuakeEditor |
| 3 | Add OGEngine panel to OQuakeEditor | Medium | OQuakeEditor |
| 4 | Port OASIS menu + panel to OQuake3Editor, ODOOM3Editor | Medium | Editor team |
| 5 | STAR Web API browser panel | Large | OASIS team |
| 6 | Begin TrenchBroom multi-format work (Doom WAD support) | Large | OGEditor team |
| 7 | OGEditor unified release | Very Large | All teams |

---

## 7. Cross-Editor Map Conversion

Until OGEditor exists, mappers working across games need a way to convert geometry between formats. The recommended workflow:

1. Block out geometry in **OQuakeEditor** (TrenchBroom) — best CSG tools
2. Export as `.map` (common Quake map text format)
3. Convert to target format:
   - Quake → Quake2: mostly compatible, re-texture
   - Quake → Doom3: use `map2doom3` converter (TBD — to be built)
   - Quake2 → Quake3: re-texture, update entity classnames
4. Open converted file in the target editor for entity placement

The STAR Web API's `/api/maps/register` endpoint accepts a format-agnostic map descriptor (JSON) so portal topology is preserved across format conversions.

---

## 8. Related Documents

- `OASIS Omniverse/Docs/ARCHITECTURE.md` — OGEngine and STAR API architecture
- `OASIS Omniverse/Docs/OGENGINE_VISION_AND_ROADMAP.md` — OGEngine long-term vision
- `OASIS Omniverse/Docs/INTEGRATION_GUIDE.md` — per-game engine hook integration guide
- `UltimateDoomBuilder/Source/OASISEditorSDK/OASISAssetCatalog.cs` — canonical thing type catalog
- `OQuakeEditor/app/TrenchBroom/resources/games/*/OASIS_*.fgd` — TrenchBroom entity defs
- `OQuake3Editor/contrib/oasis/oasis_oquake3.ent` — NetRadiant entity defs
- `ODOOM3Editor/install/resources/oasis_odoom3.def` — DarkRadiant entity defs
