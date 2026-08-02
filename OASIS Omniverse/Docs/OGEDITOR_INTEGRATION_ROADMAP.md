# OGEditor Integration Roadmap

This document covers the current state of OASIS Omniverse editor integration, the planned deep-integration layers, and the long-term goal of merging all forked editors into a single unified **OGEditor** — with **UltimateDoomBuilder (UDB)** as the established primary base.

For the full platform vision, see `OGENGINE_VISION_AND_ROADMAP.md`.

---

## 1. Editor Inventory

| Editor | Base | Language | Primary Games | OASIS Status |
|--------|------|----------|---------------|--------------|
| **UltimateDoomBuilder** | UDB | C# / .NET 4.7.2 | ODOOM, ODuke3D, OWolf3D | **Primary base. Full OGEditorSDK, OASISStarPanel, OASISPortalPanel, OASISMapConverter, OGMapSidecar** |
| **OQuakeEditor** | TrenchBroom | C++ / Qt | OQuake, OQuake2, OQuake3 | OASIS entity definitions only |
| **OQuake3Editor** | NetRadiant | C | OQuake3 | OASIS entity definitions only |
| **ODOOM3Editor** | DarkRadiant | C++ / wxWidgets | ODOOM3, ODOOM3-BFG | OASIS entity definitions only |

---

## 2. Architecture: UDB as the Hub

UDB is not just one of four equal editors — it is the **intelligence hub** for the entire OGEditor system. The OGEditorSDK it hosts is compiled to a native C ABI DLL (`ogeditor_api.dll` / `libogeditor_api.so`) via .NET NativeAOT, exposing every OASIS capability to any editor regardless of implementation language.

```
┌──────────────────────────────────────────────────────────────┐
│                   UltimateDoomBuilder (UDB)                  │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │                    OGEditorSDK (.NET)                   │ │
│  │  OGAssetCatalog   OGMapSidecar   OGStarApiClient        │ │
│  │  OGEntityMappings                                        │ │
│  │  Native/ → ogeditor_api.h + NativeExports.cs            │ │
│  └──────────────────────┬──────────────────────────────────┘ │
│                         │ NativeAOT compile                  │
└─────────────────────────┼────────────────────────────────────┘
                          │
               ogeditor_api.dll / libogeditor_api.so
                          │
          ┌───────────────┼───────────────┐
          │               │               │
   OQuakeEditor    OQuake3Editor   ODOOM3Editor
  (TrenchBroom)   (NetRadiant)   (DarkRadiant)
   C++ FFI call    C FFI call     C++ FFI call
          │               │               │
          └───────────────┴───────────────┘
                          │
              All route through the same
              OGStarApiClient → STAR Web API
```

This means:
- **OASIS intelligence lives once** — in OGEditorSDK / UDB
- Satellite editors call `ogeditor_api.dll` for asset lookup, portal registration, STAR API calls, entity mapping, and sidecar I/O
- Adding a new game asset type, fixing a thing type number, or changing STAR API behaviour only needs to change in one place

---

## 3. What UDB Already Has

### OGEditorSDK (`Source/OGEditorSDK/`)

| File | Purpose |
|------|---------|
| `OGAssetCatalog.cs` | ~140-asset canonical catalog, all 10 OGames, thing types 5001–10999 |
| `OGEntityMappings.cs` | Classname ↔ OASIS thing type tables for Q1/Q2/Q3/Duke/Wolf |
| `OGMapSidecar.cs` | `.oasis.json` sidecar file reader/writer for portal topology and map metadata |
| `OGStarApiClient.cs` | HTTP client for the WEB5 STAR Web API |
| `Native/ogeditor_api.h` | C ABI header for NativeAOT exports |
| `Native/NativeExports.cs` | NativeAOT export declarations → `ogeditor_api.dll` |

### UDB Plugins (`Source/Plugins/UDBScript/`)

| File | Purpose |
|------|---------|
| `Controls/OASISStarPanel.cs` | Dockable panel: OASIS asset browser, thing type lookup, OGEngine status |
| `Controls/OASISPortalPanel.cs` | Portal placement UI: enter/exit pair creation, cross-game routing |
| `OASISMapConverter.cs` | Entity conversion between Q1/Q2/Q3/Duke/Wolf ↔ ODOOM thing types |
| `OASISMapSidecar.cs` | UDB-side `.oasis.json` read/write, portal pair persistence |

### OASISEditorSDK (`Source/OASISEditorSDK/`)
| File | Purpose |
|------|---------|
| `OASISAssetCatalog.cs` | Full thing type catalog (canonical reference, now superseded by OGAssetCatalog) |

---

## 4. What the Satellite Editors Currently Have

Each of the three satellite editors has **OASIS entity definitions only** — nothing more:

- `oasis_portal_enter` — brush trigger entity (teleport source)
- `oasis_portal_exit` — point entity (teleport destination)
- Per-game key entities

These definitions let level designers place OASIS entities in the editor. The **runtime behaviour** (connecting to OGEngine, registering with STAR API, persisting portal topology) is not yet wired up in any satellite editor.

---

## 5. Integration Phases

### Phase 1 — ogeditor_api.dll Integration in Satellite Editors

Each satellite editor loads `ogeditor_api.dll` at startup and calls into it for all OASIS intelligence. This replaces the need to duplicate any OASIS logic in C++ or C.

**API surface exposed by `ogeditor_api.h`** (to be expanded — see `OGEDITOR_PLUGIN_GUIDE.md`):

```c
// Asset catalog
int  ogeditor_get_thing_type(const char* game_id, const char* classname);
int  ogeditor_get_assets_for_game(const char* game_id,
                                   OGAsset* out_buf, int buf_size);

// Portal pair management
int  ogeditor_register_portal_pair(const char* src_game, const char* src_map,
                                    const char* exit_name,
                                    const char* dst_game, const char* dst_map,
                                    float exit_x, float exit_y, float exit_z,
                                    float exit_angle);
int  ogeditor_get_portals_for_map(const char* game_id, const char* map_name,
                                   OGPortal* out_buf, int buf_size);

// Sidecar
int  ogeditor_sidecar_load(const char* map_path);
int  ogeditor_sidecar_save(const char* map_path);

// STAR Web API
int  ogeditor_star_connect(const char* api_url, const char* jwt);
int  ogeditor_star_get_inventory(const char* avatar_id,
                                  OGItem* out_buf, int buf_size);
int  ogeditor_star_register_map(const char* game_id, const char* map_name,
                                 const char* map_json);

// OGEngine status
int  ogeditor_ogengine_connect(const char* ogengine_url);
int  ogeditor_ogengine_get_status(OGEngineStatus* out);
```

**Discovery**: Each satellite editor looks for `ogeditor_api.dll` in:
1. Same directory as the editor executable
2. `%APPDATA%\OASIS\bin\` (Windows) / `~/.oasis/bin/` (Linux/macOS)
3. Path from `%APPDATA%\OASIS\editor_config.json` → `"ogeditor_api_path"`

If not found, OASIS features degrade gracefully — entity definitions still work, the OASIS panel shows "OGEditorSDK not found" and a download link.

---

### Phase 2 — OASIS Menu + Cross-Launch

Each editor gains a top-level **OASIS** menu. Editors discover each other via the shared config:

**`%APPDATA%\OASIS\editor_config.json`** (Windows) / `~/.oasis/editor_config.json` (Linux/macOS):
```json
{
  "editors": {
    "udb":           "C:\\Source\\UltimateDoomBuilder\\build\\Builder.exe",
    "oquake_editor": "C:\\Source\\OQuakeEditor\\build\\OQuakeEditor.exe",
    "oquake3_editor":"C:\\Source\\OQuake3Editor\\install\\netradiant.exe",
    "odoom3_editor": "C:\\Source\\ODOOM3Editor\\install\\darkradiant.exe"
  },
  "ogeditor_api_path": "C:\\Source\\UltimateDoomBuilder\\build\\ogeditor_api.dll",
  "ogengine_url":      "http://localhost:8888",
  "star_api_url":      "http://localhost:7777"
}
```

**OASIS menu (identical across all editors):**
```
OASIS
├── Open Portal Partner In ▶      [cross-family: open the linked destination map]
│   ├── UltimateDoomBuilder
│   ├── OQuakeEditor (TrenchBroom)
│   ├── OQuake3Editor (NetRadiant)
│   └── ODOOM3Editor (DarkRadiant)
├── Convert Map To ▶              [same-family only: Quake↔Quake2↔Quake3, Doom↔Doom3]
│   ├── Quake / Quake2 format
│   ├── Quake3 format
│   └── Doom 3 format
├── ─────────────────────────────
├── Connect to OGEngine...        [calls ogeditor_ogengine_connect()]
├── STAR API Status               [calls ogeditor_ogengine_get_status()]
├── Sign In to OASIS...           [calls ogeditor_star_connect()]
├── ─────────────────────────────
├── Browse OASIS Assets...        [opens OASISStarPanel]
├── View Portal Connections...    [opens OASISPortalPanel]
├── Register Map with OASIS...    [calls ogeditor_star_register_map()]
└── About OGEditor Integration
```

### "Open Portal Partner In" — important distinction

**The map formats used by each OGame engine family are incompatible.** A Doom WAD (2D sectors) cannot be opened in TrenchBroom. A Quake BSP (3D brushes) cannot be opened in UDB. These tools are built for completely different geometry systems.

"Open Portal Partner In" therefore does **not** open the current map file in another editor. Instead it reads the selected `oasis_portal_enter` entity's destination (`oasis_game_id` + `oasis_map`) from the `.oasis.json` sidecar, looks up that map file path from the STAR API map registry, and launches the **appropriate editor for the destination game** with **that destination map**. This lets a level designer keep both sides of a portal pair open simultaneously in the correct tool.

Example: editing `base3.bsp` in OQuakeEditor, you select an `oasis_portal_enter` pointing to `odoom/e1m1`. Click "Open Portal Partner In → UltimateDoomBuilder" — UDB opens with `e1m1.wad`. Both editors are now open, each with the right map in the right tool.

### "Convert Map To" — same geometry family only

`.map` source files for Quake, Quake2, and Quake3 are all brush-based text formats close enough that conversion is practical — entity classnames and some keys differ, but the geometry representation is the same. `OASISMapConverter` remaps entity classnames and OASIS keys between these engines.

Doom/Doom3 are a separate compatible pair (sector-based → idTech4 format shares conceptual proximity).

**Cross-family conversion (Quake ↔ Doom, Quake ↔ Duke3D, etc.) is not supported** — the geometry primitives are incompatible at a fundamental level and no automated conversion would produce a usable map.

---

### Phase 3 — OASISStarPanel in Satellite Editors

UDB already has `OASISStarPanel.cs` as a dockable panel. The satellite editors need equivalent panels that call `ogeditor_api.dll` rather than calling .NET code directly.

**Panel spec:**

```
┌─ OASIS ─────────────────────────────────────────┐
│ OGEngine  ● Connected  localhost:8888            │
│ STAR API  ● Connected  localhost:7777            │
│ Avatar    PlayerOne  ✦ XP: 4,200                │
├─ Asset Browser ─────────────────────────────────┤
│ Game: [OQuake2 ▼]  Category: [All ▼]            │
│ 🔍 [_______________________]                     │
│                                                  │
│  6001  Blue Key          item_key_blue_key       │
│  6002  Red Key           item_key_red_key        │
│  6011  Blaster           weapon_blaster          │
│  6012  Shotgun           weapon_shotgun          │
│  ...                                             │
├─ Cross-game Inventory ──────────────────────────┤
│  🔑 Blue Key (OQuake2)                          │
│  🔑 Silver Key (OQuake)                         │
│  ⚔  Rocket Launcher (OQuake2)                   │
├─ Active Portals ────────────────────────────────┤
│  → base1 / exit_to_doom   →  ODOOM / e1m1       │
│  ← e1m1 / portal_from_q2  ←  OQuake2 / base1   │
└─────────────────────────────────────────────────┘
```

In TrenchBroom (OQuakeEditor), this panel is a `wxPanel` or `QDockWidget` in a new plugin at `plugins/oasis/`. It calls `ogeditor_api.dll` via `dlopen`/`LoadLibrary` and polls every 5 seconds.

In NetRadiant (OQuake3Editor), it's a module in `contrib/oasis/` using GTK widgets.

In DarkRadiant (ODOOM3Editor), it's a plugin in `plugins/oasis/` using the existing plugin API.

---

### Phase 4 — OASISPortalPanel in Satellite Editors

UDB already has `OASISPortalPanel.cs`. The satellite editors need equivalent portal UI that:

1. Reads existing portal pairs from the `.oasis.json` sidecar via `ogeditor_sidecar_load()`
2. Displays them as a connected graph (source map → destination map/game)
3. Lets the designer click an `oasis_portal_enter` entity in the map and auto-fill its `oasis_exit_name` from a drop-down of known exits in the target game
4. Registers new portal pairs with the STAR API via `ogeditor_star_register_map()`
5. Shows a warning if an `oasis_portal_enter` has no matching `oasis_portal_exit` anywhere in the known OASIS map registry

---

### Phase 5 — OASISMapConverter in Satellite Editors

UDB already has `OASISMapConverter.cs` for converting entity classnames between game formats. The satellite editors expose this via "Convert Map To" in the OASIS menu, calling `ogeditor_convert_map()`.

Format conversion capability matrix:

| From → To | Quake | Quake2 | Quake3 | Doom | Doom3 | Duke3D |
|-----------|-------|--------|--------|------|-------|--------|
| Quake     | —     | ✓      | ✓      | ✗    | ✗     | ✗      |
| Quake2    | ✓     | —      | ✓      | ✗    | ✗     | ✗      |
| Quake3    | ✓     | ✓      | —      | ✗    | ✗     | ✗      |
| Doom      | ✗     | ✗      | ✗      | —    | ✓     | ✓      |
| Doom3     | ✗     | ✗      | ✗      | ✓    | —     | ✗      |
| Duke3D    | ✗     | ✗      | ✗      | ✓    | ✗     | —      |

✓ = geometry conversion supported (same primitive family, entity classnames remapped).
✗ = incompatible geometry — 3D brushes (Quake family) vs 2D sectors (Doom/Duke) vs tiles (Wolf3D).
     Cross-family editors are linked via "Open Portal Partner In", not map conversion.

---

## 6. OGEditor Unified Release (Long-term)

The long-term goal is shipping **OGEditor** as a single installer that replaces the four separate editors. The path forward uses **UDB as the primary base** with all four satellite editors eventually folded in as format modules — because UDB already hosts the OGEditorSDK intelligence layer.

See `OGEDITOR_PLUGIN_GUIDE.md` for per-editor implementation detail and `OGEDITOR_PORTAL_SYSTEM.md` for the full portal system specification.

### Merger phases

| Phase | Work | Outcome |
|-------|------|---------|
| A | Satellite editors load `ogeditor_api.dll` (Phase 1 above) | All editors share one OASIS intelligence source |
| B | OASIS menu + cross-launch in all editors (Phase 2) | Designers move freely between editors |
| C | OASISStarPanel + OASISPortalPanel in satellite editors (Phases 3–4) | Full OASIS UI everywhere |
| D | UDB gains Quake BSP / Q3 BSP / Doom3 .map import-export | UDB opens any OASIS game's maps |
| E | Ship OGEditor: single installer, all formats, full OASIS integration | OGEditor v1.0 |

---

## 7. Related Documents

| Document | Contents |
|----------|---------|
| `OGEDITOR_PORTAL_SYSTEM.md` | Portal entity spec, runtime flow, OGMapSidecar format, STAR API registration |
| `OGEDITOR_PLUGIN_GUIDE.md` | Per-editor implementation guide for ogeditor_api.dll integration |
| `OGEDITOR_ASSET_CATALOG.md` | Asset catalog JSON schema, generation from OGAssetCatalog.cs, consumption |
| `OGENGINE_VISION_AND_ROADMAP.md` | Full OGEngine platform vision |
| `ARCHITECTURE.md` | OASIS Omniverse technical architecture |
| `INTEGRATION_GUIDE.md` | Per-game engine hook integration |
