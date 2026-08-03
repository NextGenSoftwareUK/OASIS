# OGEditor Portal System

Full specification for the OASIS Omniverse cross-map and cross-game teleport system — entity placement, runtime flow, OGMapSidecar format, and STAR API registration.

---

## 1. Overview

A portal pair links a **source trigger** in one map to an **arrival point** in another map, which may be in a different OGame entirely. The two halves are:

| Entity | Type | Role |
|--------|------|------|
| `oasis_portal_enter` | Brush entity (trigger volume) | The player walks into this to initiate teleportation |
| `oasis_portal_exit` | Point entity | The spawn point where the player arrives |

The link between them is established by a shared **exit name**: `oasis_portal_enter.oasis_exit_name` must match `oasis_portal_exit.targetname` in the destination map. The OASIS hub (Unity) and OGEngine STAR API use this name to route the player at runtime.

---

## 2. Entity Reference

### oasis_portal_enter

A brush entity — the mapper draws it around the portal frame or doorway. When the player's bounding box overlaps this brush, the engine calls `OGEngine_TeleportTo()`.

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `target_game` | choices | Yes | Destination OGame ID — one of: `ODOOM`, `OQUAKE`, `OQUAKE2`, `OQUAKE3`, `ODOOM3`, `ODOOM3BFG`, `ODUKE3D`, `ODUKE3DRT`, `OWOLF3D` (FGD: `oasis_entities.fgd`; choices list contains all 10 OGames) |
| `oasis_map` | string | Yes | Destination map name as the engine knows it (e.g. `e1m1`, `base1`, `mars_city1`) |
| `oasis_exit_name` | string | Yes | Must match `targetname` on the `oasis_portal_exit` in the destination map |
| `message` | string | No | Text shown to player on approach (optional HUD hint) |
| `targetname` | string | No | Used to trigger this portal from another entity (e.g. a button) |

### oasis_portal_exit

A point entity placed at the exact position where the player will land.

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `targetname` | string | Yes | Must match `oasis_exit_name` on the `oasis_portal_enter` in the source map |
| `angle` | integer | No | Spawn direction in degrees — 0=east, 90=north, 180=west, 270=south (default 0) |

---

## 3. Placement Guide

### Intra-game teleport (same OGame, different maps)

A classic example: the hub level of OQuake2 has portals leading to each episode.

**Hub map (`hub.bsp`):**
```
Entity: oasis_portal_enter  (brush drawn around the glowing doorway)
  target_game    = "OQUAKE2"
  oasis_map      = "base1"
  oasis_exit_name = "hub_exit_to_base1"
  message        = "Base 1 — Unit 1"
```

**Destination map (`base1.bsp`):**
```
Entity: oasis_portal_exit  (point at the start of base1)
  targetname = "hub_exit_to_base1"
  angle      = 270                    ← player faces west (into the level)
```

### Cross-game teleport (different OGames)

A portal in an OQuake2 map leading to an ODOOM map.

**OQuake2 map (`base3.bsp`):**
```
Entity: oasis_portal_enter  (drawn around a pentagram-shaped doorway)
  target_game    = "ODOOM"
  oasis_map      = "e1m1"
  oasis_exit_name = "from_quake2"
  message        = "The rift tears open..."
```

**ODOOM map (`e1m1.wad`, sector-level spawn):**
```
Thing: oasis_portal_exit  (thing type 5900, placed at the landing spot)
  targetname = "from_quake2"
  angle      = 0                      ← player faces east
```

### Return portal (bidirectional connection)

A fully bidirectional link needs **two** enter/exit pairs — one in each map:

```
base3.bsp  →  oasis_portal_enter (→ odoom/e1m1)      + oasis_portal_exit (← odoom/e1m1)
e1m1.wad   →  oasis_portal_enter (→ oquake2/base3)   + oasis_portal_exit (← oquake2/base3)
```

---

## 4. Runtime Flow

When the player steps into an `oasis_portal_enter` brush:

```
1. Engine trigger fires
   └─ The engine's Touch_Item / trigger system detects player overlap

2. Engine calls OGEngine_TeleportTo()
   └─ Defined in the game's ogengine_integration.h:
      OQuake2_STAR_TeleportTo(game_id, map, exit_name)
      OQuake3_STAR_TeleportTo(game_id, map, exit_name)
      etc.

3. OGEngine receives the request (HTTP POST to /api/teleport)
   Body: { "src_game": "oquake2", "src_map": "base3",
           "dst_game": "odoom",   "dst_map": "e1m1",
           "exit_name": "from_quake2", "avatar_id": "..." }

4. OGEngine validates the portal pair
   └─ Checks STAR API: does this (src_game, src_map, exit_name) exist?
   └─ If cross-game: checks the OASIS hub can load dst_game

5. State preservation
   └─ OGEngine records current player state to cross-game inventory:
      health, armour, held weapons, OASIS keys, XP
   └─ State carried by the avatar profile in the STAR API

6. OASIS hub (Unity) is notified
   └─ SignalR push to the active hub session:
      { "action": "load_ogame", "game": "odoom", "map": "e1m1",
        "spawn_at": "from_quake2", "avatar_id": "..." }

7. Hub switches active OGame
   └─ If same OGame: sends changelevel command to the engine
   └─ If different OGame: unloads current engine, launches destination engine

8. Destination engine loads the target map
   └─ OGEngine calls OGGame_SpawnAtExit(exit_name) after map load
   └─ Engine spawns player at the oasis_portal_exit with matching targetname
   └─ Player facing = angle key value

9. OGEngine restores cross-game state
   └─ Health, armour, and OASIS keys are restored from avatar profile
   └─ Cross-game weapons: only if weapon is valid in the destination game
```

---

## 5. OGMapSidecar Format

Every map that contains OASIS portal entities should have an accompanying sidecar file. This is how editors and the STAR API persist and share portal topology without modifying the map format itself.

**File location:** Same directory as the map file, named `oasis_{mapname}.json`.

**Examples:**
- `base3.bsp` → `oasis_base3.json`
- `e1m1.wad` → `oasis_e1m1.json`
- `mars_city1.map` → `oasis_mars_city1.json`

**Schema:**

```json
{
  "schema_version": "1.0",
  "game_id": "oquake2",
  "map_name": "base3",
  "portals": {
    "enter": [
      {
        "exit_name": "from_quake2",
        "dst_game":  "odoom",
        "dst_map":   "e1m1",
        "brush_center": [128.0, 256.0, 48.0],
        "message":   "The rift tears open..."
      }
    ],
    "exit": [
      {
        "targetname": "hub_exit_to_base3",
        "position":   [64.0, 128.0, 0.0],
        "angle":      270
      }
    ]
  },
  "keys_required": ["blue_key"],
  "xp_granted_on_entry": 0,
  "registered_at": "2026-08-02T12:00:00Z",
  "star_map_id": "oquake2:base3:a3f9c..."
}
```

**UDB reads and writes this automatically** via `OGMapSidecar.cs` / `OASISMapSidecar.cs` whenever the map is saved.

**Satellite editors** (TrenchBroom, NetRadiant, DarkRadiant) read/write via `ogeditor_sidecar_load()` / `ogeditor_sidecar_save()` from `OGEditorClient.dll`.

**Entity definition files (done):** `oasis_entities.fgd` and `oasis_entities.def` are now at:
```
C:\Source\UltimateDoomBuilder\Source\Plugins\UDBScript\Assets\oasis_entities.fgd
C:\Source\UltimateDoomBuilder\Source\Plugins\UDBScript\Assets\oasis_entities.def
```
The FGD defines `oasis_portal_enter` with a full `choices` list for `target_game` covering all 10 OGames:
```
target_game(choices) : "Target OGame ID" : "ODOOM" = [
    "ODOOM":"ODOOM (Doom/Doom2)"
    "OQUAKE":"OQUAKE (Quake)"
    "OQUAKE2":"OQUAKE2 (Quake 2)"
    "OQUAKE3":"OQUAKE3 (Quake 3)"
    "ODOOM3":"ODOOM3 (Doom 3)"
    "ODOOM3BFG":"ODOOM3BFG (Doom 3 BFG)"
    "ODUKE3D":"ODUKE3D (Duke Nukem 3D)"
    "ODUKE3DRT":"ODUKE3DRT (Duke Nukem 3D RT)"
    "OWOLF3D":"OWOLF3D (Wolfenstein 3D)"
]
```

**Companion editor launch (done):** The UDB `OGEnginePanel.cs` panel now includes a launch button that opens the companion editor directly from within UDB.

---

## 6. STAR Web API Registration

When a map is saved in any editor with OASIS portals, the editor should call the STAR Web API to register the portal topology. This makes the portal connections visible to:
- Other editors (the portal panel can show "known exits in e1m1")
- The OASIS hub (so it can validate teleport requests)
- The STARNET web interface (so players can browse the portal network)

**Endpoint:** `POST /api/maps/register`

```json
{
  "game_id":  "oquake2",
  "map_name": "base3",
  "portals": {
    "enter": [
      {
        "exit_name": "from_quake2",
        "dst_game":  "odoom",
        "dst_map":   "e1m1"
      }
    ],
    "exit": [
      {
        "targetname": "hub_exit_to_base3",
        "angle":      270
      }
    ]
  }
}
```

**Response:**
```json
{
  "star_map_id": "oquake2:base3:a3f9c...",
  "warnings": [
    "oasis_portal_exit 'from_quake2' not yet registered in odoom:e1m1"
  ]
}
```

Warnings are shown in the editor's OASIS panel so the mapper knows which exit points still need their partner maps saved and registered.

---

## 7. Portal Validation Rules

The editor enforces these rules and warns (not errors) on violations:

| Rule | Severity |
|------|---------|
| `oasis_portal_enter.oasis_exit_name` is empty | Error |
| `oasis_portal_enter.oasis_game_id` is not a known OGame ID | Warning |
| `oasis_portal_enter.oasis_map` is empty | Warning |
| No `oasis_portal_exit` with a matching `targetname` exists in the STAR API portal registry | Warning |
| `oasis_portal_exit.targetname` is empty | Error |
| Two `oasis_portal_exit` entities have the same `targetname` in the same map | Error |

---

## 8. Portal Network Visualizer (Planned — Phase 3)

The OASISPortalPanel in UDB will eventually render a directed graph of the entire known portal network:

```
[OQuake: start] ──→ [OQuake: e1m1] ──→ [OQuake: e2m1]
                                              │
                                              ↓
                                      [OQuake2: base1] ──→ [OQuake2: base2]
                                              │
                                              ↓
                                       [ODOOM: e1m1] ──→ [ODOOM: e1m2]
```

Nodes are maps. Edges are portal pairs. Clicking a node opens that map in the appropriate editor. Red nodes = maps with unmatched exits. This graph is built from the STAR Web API's portal registry.

---

## 9. Cross-game State Preservation Rules

What carries across a portal teleport:

| State | Carry | Notes |
|-------|-------|-------|
| OASIS keys | Always | Stored in avatar inventory on STAR API |
| OASIS XP | Always | Stored on avatar profile |
| Health | Clamped | Clamped to destination game's max health on arrival |
| Armour | Clamped | Clamped to destination game's max armour on arrival |
| Weapons | Game-specific | Only if a weapon with the same OASIS thing type exists in dst game |
| Ammo | Partial | Converted if ammo type exists in dst game; otherwise dropped |
| Quest progress | Always | Quest objectives are cross-game and stored in STAR API |
| Map secrets | Local | Not carried — secrets are per-map |

---

## 10. Engine Hook Requirement

For teleportation to work, each engine must implement `OGGame_TeleportTo()` in its OGEngine integration layer (the `*_ogengine_integration.c` file). This function is called by the trigger logic and must:

1. Call `ogengine_teleport_to(game_id, map, exit_name, avatar_id)`
2. Block movement/input while the OGEngine processes the request
3. On confirmation: save any local state to the OGEngine (via `ogengine_sync_pump()`)
4. On timeout/failure: show an error message to the player and cancel the teleport

**Status (2026-08-02):** `CheckIncomingTeleport()` and spawn-event polling have been added to all 7 game integrations (ODOOM, OQuake, ODOOM3, ODOOM3-BFG, ODuke3D, OWolf3D, and ODuke3D-RT). Each integration calls its `{Prefix}_STAR_CheckIncomingTeleport()` function at map load to detect and handle incoming cross-game teleports, and polls `ogengine_poll_spawn_event()` each frame via the tick function, logging incoming spawn requests via `oglib_log`.
