# Using STAR to Generate a DOOM-Style Game

**Goal:** Describe what it would look like to use STAR (worlds, missions, quests, avatar) to **generate** and **drive** a DOOM-style FPS: episode/level structure, theme/skybox from prompt, and one OASIS avatar as the player.

---

## 1. High-Level Picture

- **STAR** holds the **game structure and content**: one **World** (theme/atmosphere from a prompt), one **Mission** (episode), and **Quests** (levels). The **DOOM-style client** (existing DOOM port or minimal FPS template) reads that structure and optional generated art; it does not define the “campaign” itself.
- **Flow:** User says “Generate a DOOM-style game: hell fortress” → STAR creates World + Mission + Quests (levels) → User hits “Play” → Launcher loads that campaign from STAR and starts the DOOM-style engine with level list + theme (e.g. skybox from our generated image).

---

## 2. STAR Side: What We Generate

| STAR concept   | DOOM-style meaning        | How it’s created / used |
|----------------|---------------------------|---------------------------|
| **World**      | Episode theme / atmosphere| `POST /api/world/generate-from-prompt` with e.g. “hell fortress demonic base” → `worldId`, `sceneImageUrl`. That image = skybox/background or menu art for the episode. |
| **Mission**    | Episode (e.g. “Episode 1”)| `POST /api/missions`: name “Episode 1: Hell Fortress”, description, type, difficulty. Holds the “campaign”; children = Quests (levels). |
| **Quests**     | Levels (E1M1, E1M2, …)    | `POST /api/quests` per level: name “E1M1: Hangar”, `parentMissionId`, objectives (“Find exit”, “Kill all monsters”, “Find secret”), rewards (karma/XP). Optional `metadata.mapId` or `metadata.seed` for which map file or procedural level to load. |
| **Avatar**     | Player                    | Already defined: identity, portrait, karma, level. Launcher passes avatar (or JWT) so the game shows “who is playing” and syncs XP/karma (see [OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION](OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION.md)). |
| **Inventory**  | (Optional) Keys, unlocks  | STAR Inventory API can represent keycards, weapon unlocks, etc., if the client supports it. |

So “STAR generates a DOOM-style game” = we create **one World** (theme image + holon) + **one Mission** (episode) + **N Quests** (levels with objectives/rewards and optional map/seed). The **game client** is a DOOM-style engine that consumes this structure.

---

## 3. DOOM-Style Client Side

- **Engine:** Existing DOOM port (e.g. [Chocolate Doom](https://www.chocolate-doom.org/wiki/index.php/Chocolate_Doom), [doom-wasm](https://github.com/cloudflare/doom-wasm)) or a minimal “DOOM-like” FPS template (Unity/Godot/Three.js with FPS controls and simple level format).
- **Launcher / bridge:**
  1. User selects a “campaign” (e.g. a Mission id, or “last generated”).
  2. Launcher calls OASIS: get Mission by id, get Quests for that mission (levels in order), get World by id (or from mission metadata) for theme.
  3. Launcher passes to the game:
     - **Level list:** ordered list of quests (id, name, metadata.mapId or seed).
     - **Theme:** World’s `sceneImageUrl` (for skybox, menu background, or intermission).
     - **Avatar:** avatar id, display name, portrait URL, karma/level (for HUD/menu).
- **Game runtime:**
  - Load level 1: use `metadata.mapId` or `metadata.seed` to pick a map (e.g. built-in E1M1 or procedural). If the engine supports it, use `sceneImageUrl` as skybox or atmosphere.
  - On level exit / objective complete: launcher (or in-game bridge) calls STAR: complete quest, update avatar XP/karma.
  - Next level = next quest; repeat. When all quests are complete, complete the mission in STAR.

So the **game** is STAR-defined (mission + quests + world theme); the **engine** provides DOOM-style FPS gameplay and renders levels (stock maps or procedural from seed).

---

## 4. End-to-End Flow

**Step 1 — Generate the “game” (backend or tool):**

1. `POST /api/world/generate-from-prompt`  
   Body: `{ "prompt": "hell fortress demonic base", "name": "Hell Fortress" }`  
   → `worldId`, `sceneImageUrl`.
2. `POST /api/missions`  
   Body: `{ "name": "Episode 1: Hell Fortress", "description": "Survive the demonic base", "type": "Quest", "metadata": { "worldId": "<worldId>" } }`  
   → `missionId`.
3. For each level (e.g. E1M1, E1M2, E1M3):  
   `POST /api/quests`  
   Body: `{ "name": "E1M1: Hangar", "parentMissionId": "<missionId>", "objectives": [...], "rewards": { "karma": 50 }, "metadata": { "mapId": "E1M1" } }`  
   → `questId`.  
   (Use `metadata.mapId` for stock maps or `metadata.seed` for procedural.)

**Step 2 — Play (launcher + DOOM-style client):**

1. User clicks “Play DOOM-style: Hell Fortress” (or selects mission id).
2. Launcher: authenticate (JWT), then `GET /api/missions/{id}`, `GET /api/quests?missionId=...` (or load quests for mission), `GET /api/data/load-holon/{worldId}` (or thin world endpoint) for theme.
3. Launcher starts the DOOM-style engine with: level list (from quests), theme URL (`sceneImageUrl`), avatar (id, name, portrait, karma/level).
4. Engine runs level 1 (e.g. E1M1); on exit, launcher calls `POST /api/quests/{id}/complete`, updates avatar XP; repeat for next quest.
5. When all quests are complete, launcher calls `POST /api/missions/{id}/complete`.

Result: **STAR has generated** the “game” (world + mission + quests); the **DOOM-style client** executes it with the same avatar and optional generated theme.

---

## 5. What It Would Look Like (User-Facing)

- **Create:** User types “Generate a DOOM-style game: hell fortress” (or “cyber base”, “mars base”). Backend (or MCP/CLI) creates World (theme image) + Mission + Quests (e.g. 3–9 levels) and returns a link or campaign id.
- **Play:** User opens launcher, picks “Hell Fortress” (or the generated campaign). Launcher loads mission/quests/world from STAR and starts the DOOM-style game with:
  - Level order and objectives from STAR.
  - Skybox/menu art from our generated `sceneImageUrl` (if the engine supports it).
  - OASIS avatar as player (name, portrait, karma/level) and XP/karma synced on level complete.

So it **looks like** a DOOM-style game whose **episode and levels** (and optional look) come from STAR; gameplay is classic FPS (stock or procedural maps) driven by STAR missions/quests and avatar.

---

## 6. Implementation Hooks (Concise)

- **Backend:** Reuse `POST /api/world/generate-from-prompt`; add (or use existing) Missions and Quests APIs to create one mission and N quests with `parentMissionId`, objectives, rewards, and `metadata.mapId` / `metadata.seed`. Optional: one “generate DOOM-style campaign” endpoint that does world + mission + quests in one go.
- **Launcher:** Same as in [OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION](OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION.md): authenticate, fetch avatar profile; add “fetch campaign” (mission + quests + world) and pass level list + theme + avatar into the DOOM-style client.
- **DOOM port / FPS template:** Read level list and optional theme URL from launcher (or config written by launcher); load maps by `mapId` or generate from `seed`; on level exit, notify launcher so STAR quest/mission completion and avatar XP are updated.

---

## 7. Summary

Using STAR to generate a DOOM-style game means:

- **World** = theme/atmosphere (from prompt → scene image + STAR world holon).
- **Mission** = one episode; **Quests** = levels (with objectives, rewards, optional map/seed).
- **Avatar** = player (identity + karma/level); sync on level complete.
- **DOOM-style client** = engine that receives mission/quests/world from STAR and runs the levels; STAR defines *what* the game is, the engine defines *how* it plays.

No need to generate DOOM map geometry in STAR; we generate **structure** (mission, quests) and **theme** (world image). Maps can remain stock (e.g. E1M1) or come from a procedural generator keyed by quest `metadata.seed`.
