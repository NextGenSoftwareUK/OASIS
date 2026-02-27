# Phase 2: Multi-Game Quest System (WEB5 STAR Quest API)

## Overview

Phase 2 is the **multi-game quest system** backed by the **WEB5 STAR API** (Quest API). The flow is:

1. **WEB5 STAR API** – Hosts the Quest API (create quests, start/complete quests, complete objectives). This is the backend OASIS STAR service (e.g. `https://oasisweb4.one/star/api` or local WEB5).
2. **STARAPIClient** – C# client that talks to WEB5 over HTTP and exposes a **C ABI** (`star_api_*`) for native games. Quest methods in the client call WEB5’s quest endpoints.
3. **ODOOM & OQuake** – Use **STARAPIClient** (`star_api.dll`) to start quests, complete objectives when the player does the required action (e.g. pick up keycard), and complete quests. No direct HTTP from the games; all quest calls go through the client.

This doc describes the **WEB5 STAR Quest API** we are plugging into STARAPIClient, and how ODOOM and OQuake consume it via the client.

---

## WEB5 STAR Quest API (backend)

The STAR API (WEB5) exposes REST endpoints for quests. STARAPIClient calls these; games do not call them directly.

| Action | HTTP | Endpoint |
|--------|------|----------|
| Start a quest | POST | `{base}/api/quests/{questId}/start` |
| Complete an objective | POST | `{base}/api/quests/{questId}/objectives/{objectiveId}/complete` (body: `gameSource`) |
| Complete a quest | POST | `{base}/api/quests/{questId}/complete` |
| Create quest (e.g. cross-game) | POST | `{base}/api/quests/create` (body: name, description, objectives, etc.) |
| Get active quests | GET | `{base}/api/quests/by-status/InProgress` |

`{base}` is the STAR API base URL (e.g. from `oasisstar.json`: `star_api_url`). Auth uses the same avatar/API key as inventory (Bearer or X-Avatar-Id).

Quest definitions (id, name, description, objectives, rewards) are managed by the backend. Games use the client to **start** quests, **complete objectives** when the player does the in-game action, and **complete** the quest when all objectives are done.

---

## STARAPIClient (C ABI for games)

STARAPIClient implements the WEB5 Quest API and exposes these to native code via the C ABI (used by ODOOM and OQuake):

| C function | Purpose |
|------------|---------|
| `star_api_start_quest(const char* quest_id)` | Start a quest by ID. Calls WEB5 `POST …/quests/{questId}/start`. |
| `star_api_complete_quest_objective(const char* quest_id, const char* objective_id, const char* game_source)` | Mark an objective complete (e.g. “collect red keycard”). Calls WEB5 `POST …/objectives/{objectiveId}/complete` with `gameSource` (e.g. `"Doom"`, `"Quake"`). |
| `star_api_complete_quest(const char* quest_id)` | Complete the quest and claim rewards. Calls WEB5 `POST …/quests/{questId}/complete`. |

The C# client also supports batching objective completions (`QueueCompleteQuestObjectiveAsync` / `FlushQuestObjectiveJobsAsync`) and getting active quests (`GetActiveQuestsAsync`); these may be exposed on the C ABI in a future update for in-game quest UIs.

---

## How ODOOM and OQuake use it

- **No direct WEB5 calls** – All quest traffic goes through STARAPIClient (`star_api.dll`).
- **When the player does the thing** – For example, ODOOM calls `star_api_complete_quest_objective(quest_id, "doom_red_keycard", "Doom")` when the player picks up the red keycard; OQuake does the same for Quake objectives (e.g. silver key) with `game_source` `"Quake"`.
- **Quest IDs and objective IDs** – Come from the backend (or config). The game only needs to know which `quest_id` and `objective_id` to pass for each in-game event.
- **Start / complete** – Games call `star_api_start_quest(quest_id)` when the player accepts a quest, and `star_api_complete_quest(quest_id)` when all objectives are done (or the backend can infer completion from objectives).

So: **WEB5 STAR Quest API** is the source of truth; **STARAPIClient** is what we plug into next and expose to ODOOM/OQuake via the C API above.

---

## Quest structure (WEB5 / STARAPIClient)

Quest and objective shape is defined by the WEB5 API. Conceptually:

- **Quest** – Id, name, description, status (e.g. InProgress, Completed), list of objectives, optional rewards.
- **Objective** – Id, description, game source (e.g. doom, quake), item or action required, completion status.

Example cross-game quest (structure aligned with WEB5/STARAPIClient):

```json
{
  "id": "cross_dimensional_keycard_hunt",
  "name": "Cross-Dimensional Keycard Hunt",
  "description": "Collect keycards from multiple dimensions to unlock the Master Keycard",
  "type": "CrossGame",
  "status": "InProgress",
  "objectives": [
    {
      "id": "doom_red_keycard",
      "description": "Collect red keycard in Doom",
      "game": "doom",
      "itemRequired": "red_keycard",
      "isCompleted": false
    },
    {
      "id": "quake_silver_key",
      "description": "Collect silver key in Quake",
      "game": "quake",
      "itemRequired": "silver_key",
      "isCompleted": false
    }
  ],
  "rewards": {
    "items": ["master_keycard"],
    "karma": 100,
    "experience": 50
  }
}
```

Creation and listing are done via WEB5 (or admin tools). Games use the C API to start, complete objectives, and complete the quest.

---

## Example flow in a game (ODOOM / OQuake)

1. **Quest available** – Backend or config defines the quest; player accepts. Game calls `star_api_start_quest("cross_dimensional_keycard_hunt")`.
2. **Objective 1 (Doom)** – Player picks up red keycard. ODOOM integration calls `star_api_complete_quest_objective("cross_dimensional_keycard_hunt", "doom_red_keycard", "Doom")`.
3. **Objective 2 (Quake)** – Player picks up silver key. OQuake integration calls `star_api_complete_quest_objective("cross_dimensional_keycard_hunt", "quake_silver_key", "Quake")`.
4. **Quest complete** – When all objectives are complete (either tracked client-side or inferred by backend), game calls `star_api_complete_quest("cross_dimensional_keycard_hunt")`. WEB5 applies rewards (e.g. add master_keycard to inventory).

---

## Integration points in ODOOM / OQuake

### ODOOM (Doom integration)

When the player picks up a keycard (or other quest-relevant item), the Doom/ODOOM integration already adds the item to inventory. In addition, it can call the quest API:

```c
// When player picks up red keycard – add to inventory and optionally complete objective
star_api_add_item("red_keycard", "Red Keycard", "Doom", "KeyItem", NULL, 1, 1);
star_api_complete_quest_objective("cross_dimensional_keycard_hunt", "doom_red_keycard", "Doom");
```

Quest IDs and objective IDs can be configured per map or per quest so the same pickup logic drives multiple quests.

### OQuake (Quake integration)

Same idea: on key (or item) pickup, add to inventory and complete the matching objective:

```c
// When player picks up silver key
star_api_add_item("silver_key", "Silver Key", "Quake", "KeyItem", NULL, 1, 1);
star_api_complete_quest_objective("cross_dimensional_keycard_hunt", "quake_silver_key", "Quake");
```

---

## Quest types (backend-defined)

The WEB5 Quest API can support different quest types; games just call start/objective/complete. Examples:

- **Collection quests** – Objectives like “collect red_keycard”, “collect silver_key”; games call `star_api_complete_quest_objective` when the item is picked up.
- **Exploration / location** – Objective “visit E1M1 secret”; game calls `star_api_complete_quest_objective` when the player enters the trigger.
- **Combat / achievements** – Objective “kill 10 imps”; game calls `star_api_complete_quest_objective` when count is reached.

Rewards (items, NFTs, karma, XP) are defined and applied on the backend when the quest is completed via `star_api_complete_quest`.

---

## Summary

- **WEB5 STAR API** – Provides the Quest API (REST). Next step is to ensure STARAPIClient is fully wired to these endpoints and that ODOOM/OQuake use only the client.
- **STARAPIClient** – Talks to WEB5 and exposes **star_api_start_quest**, **star_api_complete_quest_objective**, **star_api_complete_quest** to native games.
- **ODOOM & OQuake** – Use STARAPIClient (`star_api.dll`) for all quest operations; no direct WEB5 access. They call the C API when the player starts a quest, completes an objective, or completes the quest.

For setup, build, and config (including `star_api_url` for WEB5), see [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md) and [STARAPIClient/README.md](STARAPIClient/README.md).
