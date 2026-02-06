# STAR × IsoCity: Integration Plan

**Goal:** Use STAR to generate interoperable game worlds and drive [IsoCity](https://github.com/amilich/isometric-city) as one client. No dependency on Glif; STAR is the single source of truth for worlds, avatar, and persistence.

**Answer:** Yes — we can use STAR with IsoCity. STAR provides: world records (one per city), avatar identity, and load/save of city state. IsoCity is the game client that consumes that data.

---

## 1. What STAR Provides (Interoperable Game World)

| STAR/OASIS | Role for any game client |
|------------|---------------------------|
| **World** | One holon per “game world” (here: one city slot). `name`, `description`, `metaData` (e.g. `cityState` JSON, optional `sceneImageUrl`). Stored via Data API or World API. |
| **Avatar** | Identity: who owns the world, who is playing. Auth + profile (id, username, portrait, karma, level). Same avatar across STAR-driven games. |
| **Persistence** | Load/save world state (e.g. city save) in OASIS so the same world follows the avatar across devices and clients. |
| **Missions/Quests** | (When available) Optional objectives and rewards; client reports completion → karma/XP. |

So **STAR = interoperable game world**: one identity (avatar), many worlds (holons), persistent state, optional objectives. IsoCity is one client that reads and writes that data.

---

## 2. IsoCity Side: What We Need

- **List “my cities”** → call STAR/ONODE for worlds owned by or associated with the avatar.
- **Load a city** → get world holon by id; read `metaData.cityState` (or linked blob); pass into IsoCity.
- **Save a city** → send current city state to ONODE; update world holon `metaData` or dedicated endpoint.
- **Show “who is playing”** → get avatar (id, username, portrait) from OASIS and show in UI (e.g. “Mayor: {username}”).
- **(Later)** Report objectives → when Missions/Quests exist, call STAR when objectives are met.

IsoCity’s core loop (build, simulate, save/load) stays; we add a **STAR adapter** that uses ONODE for list/load/save and avatar.

---

## 3. ONODE Side: What We Need

Existing today:

- **Avatar:** `POST /api/avatar/authenticate`, `GET /api/avatar/get-avatar-detail-by-id/{id}`.
- **Data:** `GET /api/data/load-holon/{id}`, `POST /api/data/save-holon` (holon with `metaData`).
- **World (optional):** `POST /api/world/generate-from-prompt` creates a world holon (Glif is optional; we could add a “create world” that only creates the holon with name/description).

**Phase 1 ONODE implemented:**

- **List worlds for avatar:** Either `GET /api/data/load-holons-for-parent/{avatarId}` with appropriate holon type, or a dedicated `GET /api/world/my-worlds` that returns worlds for the authenticated avatar. (Depends how world holons are parented; currently they may use `ParentHolonId = Guid.Empty` and be associated via provider/context — need to confirm and possibly add a simple “my worlds” endpoint.)
- **Create world (no Glif):** `POST /api/world` body `{ "name": "My City", "description": "..." }` → `{ result: { worldId, name, description } }`.
- **Save city state:** Either store city JSON in world holon’s `metaData["cityState"]` and use existing `save-holon`, or add `PUT /api/world/{worldId}/state` that updates only the state blob for that world (and ensures avatar owns it).

---

## 4. Implementation Plan

### Phase 1: STAR as source of worlds and persistence (minimal ONODE + IsoCity)

**1.1 ONODE (done)**

- **List worlds:** `GET /api/world/my-worlds` — returns worlds for authenticated avatar.
- **Create world:** `POST /api/world` body `{ "name": "My City", "description": "..." }` — creates STAR world holon, returns `worldId`.
- **Save city state:** `PUT /api/world/{worldId}/state` body `{ "cityState": { ... } }` — updates `metaData["cityState"]` with ownership check.

**1.2 IsoCity (fork or wrapper)**

- **STAR config:** Base URL for ONODE (e.g. `https://your-onode.com`), and optionally a way to pass JWT (e.g. query param or cookie set by launcher).
- **Auth:** On startup, if JWT present, call `GET /api/avatar/get-avatar-detail-by-id/{id}` (avatar id from JWT or from launcher) to get username, portrait. Show “Mayor: {username}” (and optional portrait) in UI.
- **List cities:** Call “my worlds” endpoint (or load-holons-for-parent with correct type). Show list; each item = world id + name (+ optional description).
- **Load city:** User picks a world → `GET /api/data/load-holon/{worldId}` → read `metaData.cityState`; if present, deserialize and load into IsoCity; if absent, start empty city and associate with that `worldId`.
- **Save city:** Serialize current city state (IsoCity’s existing save format or a thin wrapper). Either (a) load world holon, set `metaData["cityState"]`, call `POST /api/data/save-holon`, or (b) call `PUT /api/world/{worldId}/state` with the blob. Ensure request is authenticated and scoped to owner.
- **New city:** Call “create world” (or generate-from-prompt with a name-only flow) → get `worldId` → start empty city with that `worldId`, then save when user first saves.

**1.3 Launcher (minimal)**

- Authenticate user: `POST /api/avatar/authenticate` → get JWT + avatar id.
- Open IsoCity (e.g. in iframe or new tab) with config: `ONODE_BASE_URL`, `JWT` (or session cookie), optional `worldId` if “Load city” was chosen. IsoCity reads config and does list/load/save as above.

**Outcome:** One avatar, multiple cities stored as STAR worlds; load/save in OASIS; playable in IsoCity with “Mayor: {avatar}”.

---

### Phase 2: Smoother UX and ownership model (done)

- **World–avatar association:** Ensure every world holon is stored with a clear “owner” (e.g. `CreatedByAvatarId` or parent to avatar’s “worlds” holon) so “my worlds” is a simple query.
- **Naming:** Let user rename a city (update world holon `name` via Data API or `PATCH /api/world/{id}`).
- **Delete city:** Optional soft-delete or delete world holon (with ownership check).
- **Error handling:** Offline, auth expiry, conflict on save — show clear messages and optionally retry.

---

### Phase 3: Optional Missions/Quests (when STAR supports them)

- When Missions/Quests APIs are available (e.g. create mission, create quest, complete quest):
  - **ONODE:** Define a “city objectives” mission type and quests (e.g. “Reach 500 population”, “Build airport”). Link quests to world or mission.
  - **IsoCity:** Thin bridge that checks game state (population, buildings) and calls STAR to complete quests when conditions are met; show objectives in UI.
  - **Karma/XP:** Quest completion updates avatar karma/level in STAR.

This phase is optional and can follow once STAR Missions/Quests are exposed and stable.

---

## 5. Summary

| Question | Answer |
|----------|--------|
| Can we use STAR with IsoCity? | **Yes.** STAR = interoperable game world (worlds + avatar + persistence). IsoCity = one client that consumes STAR data. |
| Is Glif crucial? | **No.** Worlds can be created with name/description only; optional `sceneImageUrl` later if we want a background image. |
| What is the first milestone? | Phase 1: “My cities” from STAR, load/save city state in OASIS, avatar as mayor in IsoCity. |
| What do we build first? | (1) ONODE: way to list worlds for avatar + optional create-world (no Glif) + save city state (holon metaData or dedicated endpoint). (2) IsoCity: STAR adapter (auth, list, load, save, new city) and minimal launcher that passes JWT + config. |

This plan uses STAR as the single source of truth for interoperable game worlds and keeps IsoCity as the playable client, with no dependency on Glif.
