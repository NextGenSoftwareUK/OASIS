# STAR × IsoCity: What We Could Do With It

**Goal:** Outline how the open-source [IsoCity](https://github.com/amilich/isometric-city) (and related [IsoCoaster](https://iso-coaster.com)) could integrate with STAR/OASIS: themed cities, avatar as mayor, objectives as quests, and persistent saves in OASIS.

**Repo:** [amilich/isometric-city](https://github.com/amilich/isometric-city) · **License:** MIT · **Live:** [iso-city.com](https://iso-city.com)

---

## 1. What IsoCity Is

- **IsoCity:** Isometric city-building simulation — trains, planes, cars, pedestrians, zoning (Residential, Commercial, Industrial), economy, resources, city growth.
- **IsoCoaster:** Same stack, theme-park variant — roller coasters, rides, guests (same repo).
- **Tech:** Next.js 16, TypeScript, **HTML5 Canvas** (custom isometric engine, no Unity/Godot). Mobile-friendly, save/load for multiple cities.
- **Features:** Tile-based placement, traffic/pedestrian simulation, depth sorting, layer management, state management for save/load.

So it’s a **web-native, playable simulation** with built-in “world” (city or park) and save state — a strong fit for “one STAR world = one city/park” and “OASIS avatar = player/mayor.”

---

## 2. STAR/OASIS Mapping to IsoCity

| STAR/OASIS concept | IsoCity meaning | How we’d use it |
|--------------------|-----------------|------------------|
| **World** | City (or park) instance + theme | One STAR world holon per city: `sceneImageUrl` = theme/background or palette inspiration; optional `metaData.cityState` or link to saved city blob. |
| **Avatar** | Mayor / park owner | OASIS avatar = identity in UI (name, portrait), owner of the city; karma/level as “reputation” or unlocks. Same flow as [OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION](OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION.md). |
| **Mission** | Campaign or “season” | e.g. “Build a metropolis” — one mission containing multiple quests (objectives). |
| **Quests** | City objectives | “Reach 1,000 population”, “Build airport”, “Zone 5 industrial blocks”, “Connect train line”. Complete → karma/XP in STAR. |
| **Save/Load** | City state | Persist city state (tiles, buildings, stats) in OASIS: holon `MetaData`, or separate blob keyed by `worldId` + `avatarId`. |

So we’re not changing IsoCity’s core loop; we **wrap it** with STAR identity, theme, objectives, and persistence.

---

## 3. What We Could Do With It

### 3.1 Prompt → Themed city (what Glif actually does)

**Important:** Glif does **not** generate the isometric city. IsoCity’s city is built from its **own tiles, sprites, and canvas** — roads, buildings, zoning are drawn by the game. Glif produces a **single 2D image** (e.g. “cyberpunk skyline” or “medieval town”). So Glif doesn’t “work with” the isometric view in the sense of creating the playable city content.

What we **can** do with that image:

- **Background behind the grid:** Use the Glif image as a **backdrop** (sky, distant skyline, mood) behind the isometric city. The playable city is still IsoCity’s tiles; the image is atmosphere.
- **Menu / loading screen:** Use it as art on the main menu or loading screen for that “world.”
- **Theme reference (future):** If we add multiple visual themes to IsoCity (e.g. “cyberpunk” vs “medieval” palettes or building sets), we could use the **prompt** (or the image’s palette) to pick one. That would be our logic, not Glif generating the tiles.

So “prompt → themed city” here means: **prompt → one STAR world + one Glif image** that we use for **background/menu/atmosphere**, not as the source of the isometric city itself. The city is still built by the player in IsoCity; we only theme the frame around it.

### 3.2 One STAR world = one city slot

- Each STAR world holon = **one city** (or park) “slot” for that avatar.
- World `name` / `description` = city name and brief.
- Optional: store **city save state** in world holon `MetaData` (e.g. JSON) or in linked OASIS storage keyed by `worldId` + `avatarId`, and load it when the user opens “My cities” and picks that world.

So the player’s **cities are listed from STAR** (worlds owned by avatar); picking a world loads its theme + save.

### 3.3 OASIS avatar as mayor

- **Launcher or in-game:** Authenticate with `POST /api/avatar/authenticate`, then load profile with `GET /api/avatar/get-avatar-detail-by-id/{id}`.
- Pass into IsoCity: **avatar id, display name, portrait URL**, optional karma/level.
- IsoCity UI shows “Mayor: {avatar name}” and optional portrait; optionally use karma/level for “reputation” or unlock conditions (e.g. “Reach level 5 to unlock airports”).

Same pattern as DOOM-style: **one OASIS avatar** = one identity across games; here they’re the “mayor” or “park owner.”

### 3.4 Missions & Quests = city objectives

- **Mission:** e.g. “Year 1: Grow your city” (one mission per “campaign” or play-through).
- **Quests:** “Reach 500 population”, “Build 3 parks”, “Connect rail to industrial zone”, “Unlock airport”.
- IsoCity (or a thin bridge layer) checks objectives; on completion, call STAR: `POST /api/quests/{id}/complete`, update avatar karma/XP.
- When all quests in the mission are done, complete the mission. Optional: new mission = “Year 2” or “New city theme.”

So **STAR defines the goals**; IsoCity’s simulation provides the gameplay that fulfils them.

### 3.5 Save / load city in OASIS

- IsoCity already has **save/load** for multiple cities.
- **Integration:** On “Save”, send city state (serialized tiles, buildings, stats, etc.) to ONODE — e.g. `PUT /api/data/save-holon` updating the world holon’s `MetaData["cityState"]`, or a dedicated `POST /api/world/{worldId}/save-city` that stores the blob.
- On “Load”, launcher or game fetches world by `worldId`, then reads `sceneImageUrl` + saved state and passes them into IsoCity.

So **persistence is in OASIS**, not only in browser local storage; the same avatar can resume from another device.

---

## 4. Tech Fit

- **Next.js + TypeScript + Canvas:** All web; no native binary. Easy to run inside an OAPP iframe or as a standalone app launched from an OASIS launcher.
- **No heavy game engine:** Lighter than Unity/Godot; we only need to pass in config (theme URL, avatar, optional quest list) and optionally intercept save/load and objective completion.
- **Fork or wrapper:** We can either **fork** the repo and add OASIS API calls (auth, load/save world, complete quest) or build a **thin wrapper** (launcher that injects config and handles save/quest calls) with minimal changes to upstream.

---

## 5. Minimal Implementation Path

1. **Launcher:** Authenticate avatar, load “my worlds” (STAR worlds for that avatar). User picks “New city” (creates world via `generate-from-prompt`) or “Load city” (existing world).
2. **Start IsoCity** (our build or fork) with: `sceneImageUrl`, avatar (id, name, portrait), optional mission/quest list (from STAR).
3. **Theme:** Use `sceneImageUrl` as background or theme reference in IsoCity UI.
4. **Save:** On “Save city”, POST city state to ONODE (world holon `MetaData` or dedicated endpoint).
5. **Objectives:** If we add Missions/Quests for this world, IsoCity or bridge checks conditions and calls STAR to complete quests and update karma.

Later: **“Generate city”** button that calls `generate-from-prompt` with a prompt, creates the world, and opens IsoCity with that theme.

---

## 6. Summary

| We could… | How |
|-----------|-----|
| **Use our world API for city theme** | Same `generate-from-prompt` → `sceneImageUrl`; use as menu/background or theme reference in IsoCity. |
| **One STAR world = one city** | World holon = city slot; optional city state in `MetaData` or linked storage. |
| **OASIS avatar as mayor** | Auth + avatar profile in launcher; pass name, portrait, karma/level into IsoCity. |
| **Quests = city objectives** | Missions/Quests define goals; IsoCity or bridge reports completion → STAR karma/XP. |
| **Persist cities in OASIS** | Save/load city state via ONODE so cities follow the avatar across devices. |

IsoCity is a **second open-source template** (with DOOM-style) that STAR can drive: same world + avatar + missions/quests idea, different genre (city sim instead of FPS). The repo is MIT-licensed and web-native, so integration is straightforward.
