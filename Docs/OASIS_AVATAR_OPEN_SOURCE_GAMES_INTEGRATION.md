# OASIS Avatar × Open Source Games: Single Avatar Across DOOM, Half-Life & Beyond

**Goal:** Allow one OASIS avatar (identity + portrait + karma/level) to be playable in multiple open-source games (DOOM, Half-Life, Quake, etc.) so the same player sees their identity and optional stats across titles.

---

## 1. OASIS Avatar API (Relevant Surface)

Your avatar system already exposes everything games need for **identity** and **optional gamification**.

### 1.1 Identity & Auth

| Endpoint | Purpose |
|----------|---------|
| `POST /api/avatar/register` | Create avatar (username, email, password, firstName, lastName) |
| `POST /api/avatar/authenticate` | Login → returns **JWT** + avatar `id`, `username`, `firstName`, `lastName` |
| `GET /api/avatar/get-by-id/{id}` | Get avatar by GUID (requires Bearer token) |
| `GET /api/avatar/get-by-username/{username}` | Lookup by username |

**Core identity fields (from `IAvatar`):**

- `AvatarId` (GUID) — globally unique, same across all games
- `Username`, `Email`, `FirstName`, `LastName`, `FullName`
- `AvatarType` (User, Wizard, Agent, System)

### 1.2 Detail: Portrait, Model, Karma, Level

| Endpoint | Purpose |
|----------|---------|
| `GET /api/avatar/get-avatar-detail-by-id/{id}` | Full detail: karma, level, XP, **Portrait**, **Model3D**, UmaJson, stats, etc. |
| `GET /api/avatar/get-avatar-portrait/{id}` | Portrait image (base64 or URL) |
| `POST /api/avatar/update-avatar-detail-by-id/{id}` | Update karma/XP/stats (e.g. after gameplay) |

**Relevant `IAvatarDetail` fields for games:**

- **Identity:** `Username`, `Email`, `FirstName`, `LastName`, `FullName`
- **Visual:** `Portrait` (2D image), `Model3D` (3D model URL/path), `UmaJson` (avatar config)
- **Gamification:** `Karma`, `Level` (derived from karma), `XP`, `Stats` (HP, Mana, Stamina), `Achievements`, `Inventory`

So a **single avatar** is already a unified identity with optional portrait/model and karma/level that can be reused across any game that can call your API or consume a small “game profile” payload.

---

## 2. How the Open-Source Games Represent “Player”

### 2.1 Half-Life (GoldSrc) — [ValveSoftware/halflife](https://github.com/ValveSoftware/halflife)

- **Server (game DLL):** Player is `CBasePlayer` in `dlls/player.cpp`, extending `CBaseEntity`. Entity has `edict_t` / `entvars_t` (pev): `origin`, `angles`, `model` (path to .mdl), `netname` (display name), etc.
- **Client:** `cl_dll/` renders the entity; player model is chosen by model path and skin. No built-in “account” — name/model are per-session or from Steam/local config.
- **Integration hook:** Identity and model can be driven by:
  - **Launcher/bridge:** Before starting the game, write a small config (e.g. `cl_name`, model path) from OASIS.
  - **Mod:** A small mod or wrapper that reads OASIS avatar (e.g. by JWT or avatar id), sets `pev->netname` and optionally replaces/sets the player model path from `AvatarDetail.Model3D` or a converted asset.

So: **one OASIS avatar** = one GUID + username + display name + optional portrait/model. Half-Life only needs to map “this session’s player” to that identity and, if desired, to a specific model/skin.

### 2.2 DOOM (id Software) — [id-Software/DOOM](https://github.com/id-Software/DOOM)

- **Player struct:** `player_t` in `linuxdoom-1.10/d_player.h`. Contains `mobj_t* mo` (the map object for the player), `health`, `armorpoints`, `killcount`, `itemcount`, `secretcount`, frags, weapons, ammo, etc. No “account” or “avatar” — just in-memory state.
- **Rendering:** Sprites; player appearance is typically fixed (marine). No built-in “username” in the original source; that’s usually added by source ports (e.g. for multiplayer).
- **Integration hook:** Identity and optional stats can be:
  - **Launcher/bridge:** Pass OASIS avatar id + username into the game (e.g. env vars, config file, or a small wrapper that a source port reads).
  - **Source port / patch:** Add a `player_t` field or global for “OASIS avatar id” / “display name”; optionally map karma/level to a simple “score” or “rank” for intermission/HUD.

So: **one OASIS avatar** = same GUID + username (+ optional karma/level). DOOM (or a port) only needs to know “which avatar is playing” and optionally show name/karma/level.

### 2.3 Quake (and others)

- Same idea: Quake has `client_t` / player entity; identity is usually a “name” string. An OASIS-aware launcher or mod can set that name from the avatar and optionally sync karma/level to a custom HUD or stats file.

---

## 3. Single Avatar Playable in Both (and More): Design

### 3.1 Principle

- **One OASIS avatar** = one GUID + username + optional portrait/model + optional karma/level/XP.
- Each game does **not** create its own account; it **resolves** “who is playing” via OASIS (JWT or avatar id + API key) and then uses a small **game-facing profile** (identity + optional visuals + optional stats).

### 3.2 Minimal “Game Avatar Profile” (recommended contract)

Define a small JSON that any game (DOOM, Half-Life, Quake, etc.) can consume. Your API can expose it from existing endpoints or add a thin “game profile” endpoint.

**Suggested shape:**

```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "username": "david",
  "displayName": "David",
  "portraitUrl": "https://... or base64 data URL",
  "model3DUrl": "optional URL for 3D model",
  "karma": 150,
  "level": 5,
  "xp": 2500
}
```

- **avatarId** — same in every game; source of truth.
- **username / displayName** — for in-game name (Half-Life `netname`, DOOM “player name”, etc.).
- **portraitUrl** — for menus, scoreboards, lobbies.
- **model3DUrl** — optional; Half-Life (or a mod) could map this to a model path; DOOM might ignore or use for a custom HUD avatar.
- **karma / level / xp** — optional; games can show “OASIS Level 5” or use for unlocks/rank.

You can derive this from:

- `GET /api/avatar/get-by-id/{id}` (identity)
- `GET /api/avatar/get-avatar-detail-by-id/{id}` (karma, level, XP, Portrait, Model3D)

So a **single avatar** is playable in both DOOM and Half-Life by having both games (or their launcher) fetch this same profile by `avatarId` (or by JWT → avatar id → then profile).

### 3.3 How each game “allows” the single avatar

| Layer | DOOM | Half-Life |
|-------|------|-----------|
| **Who is playing** | Resolve avatar id + username from launcher/config or env | Same (launcher/config or mod reads JWT/avatar id) |
| **Display name** | Set from `displayName` / `username` (in your port or wrapper) | Set `pev->netname` (or equivalent) from same |
| **Portrait** | Optional: menu or intermission avatar image from `portraitUrl` | Optional: scoreboard/lobby image from `portraitUrl` |
| **Model** | N/A (sprite-based) or custom HUD | Optional: map `model3DUrl` to .mdl or use a known “OASIS” model |
| **Karma/Level** | Optional: show in intermission/HUD | Optional: show in HUD or stats |

No duplication of identity: **one avatar, one GUID, one API**; each game only implements the minimal mapping above.

---

## 4. Implementation Strategies

### 4.1 Launcher / Bridge (fastest, no game source required)

- **OASIS launcher** (or small desktop app):
  - User logs in with OASIS (username/password or OAuth) → get JWT.
  - Resolve avatar: `GET /api/avatar/authenticate` then `GET /api/avatar/get-avatar-detail-by-id/{id}`.
  - Build the **game avatar profile** JSON; write it to a well-known file (e.g. `oasis_avatar.json`) and/or set env vars (`OASIS_AVATAR_ID`, `OASIS_USERNAME`, `OASIS_DISPLAY_NAME`, `OASIS_PORTRAIT_URL`, `OASIS_LEVEL`, etc.).
  - Launch DOOM or Half-Life (or Quake). Games (or wrappers) read the file/env and use the values for name, portrait, level.

- **Benefit:** One avatar works in every game that supports the contract; no need to patch DOOM/Half-Life source immediately.

### 4.2 Game-specific integration

- **Half-Life (GoldSrc):**
  - **Option A:** Mod or wrapper DLL that on spawn reads `oasis_avatar.json` (or env), sets `pev->netname` and optionally replaces player model path (if you provide or map `model3DUrl` to a .mdl).
  - **Option B:** Steam-style “local config” written by launcher: `name "David"` and optionally `model "path/to/model"`.

- **DOOM:**
  - **Option A:** Source port (e.g. based on Chocolate Doom / PrBoom) that reads `oasis_avatar.json` or env and adds:
    - A “player name” (for multiplayer or main menu).
    - Optional HUD line “OASIS Level 5” and/or portrait in intermission.
  - **Option B:** Wrapper that injects avatar id/name into a config the port already supports.

### 4.3 Optional: “Game profile” endpoint

To keep games simple and consistent, add a single endpoint that returns the minimal game profile:

- **GET** `/api/avatar/game-profile/{avatarId}`  
  **or**  
  **GET** `/api/avatar/game-profile` (with `Authorization: Bearer <JWT>`)

Response: the JSON above (`avatarId`, `username`, `displayName`, `portraitUrl`, `model3DUrl`, `karma`, `level`, `xp`).  
Launcher or games then need one HTTP call and no knowledge of the rest of the Avatar API.

---

## 5. DOOM Browser Launcher: XP (No Time-Based Awards)

The **doom-browser** launcher (OASIS Avatar + js-dos DOOM) awards **XP** only for explicit actions:

- **Play session:** 25 XP when exiting after ≥15 seconds.
- **MISSION button:** 50 XP (test / “mission complete”).

**No XP for time.** The stock DOOM binary runs inside js-dos with no API to read game memory or events, so we cannot detect kills. There is no time-based or “kill proxy” XP.

**Per-kill XP** requires one of:

1. **Custom DOOM build** that writes kill count (or “kills this session”) to a file in the emulated DOS FS (e.g. `KILLS.TXT`). The launcher would use the js-dos **Command Interface** (`ci.fsReadFile`) when available, poll that file, and call the OASIS update-XP API when the count increases.
2. **Source port compiled to WebAssembly** (e.g. Chocolate Doom + Emscripten) that posts a message to the parent window on each kill (e.g. `window.postMessage({ type: 'doom-kill' }, '*')`). The launcher would listen and award XP per message.

### How difficult is a custom DOOM build?

| Approach | Difficulty | Effort | Notes |
|----------|------------|--------|--------|
| **A. Patch original DOOM (linuxdoom-1.10) for js-dos** | **Medium** | 1–2 days | Find the single place where the player gets credit for a kill (e.g. in `p_inter.c` or the damage path where `player->killcount` is incremented). Add a few lines of C: open a file (e.g. `KILLS.TXT`), write the new count or “+1”, close. Rebuild the game for DOS so it still runs in the existing js-dos bundle (or rebuild the .jsdos bundle). The launcher then needs to obtain the js-dos Command Interface and poll that file. Main friction: getting the same DOS build toolchain and producing a drop-in .jsdos. |
| **B. Use an existing WASM port (e.g. Chocolate Doom in browser)** | **Easy–Medium** | 0.5–1 day | Projects like [chocolate-browser-doom](https://github.com/browser-doom/chocolate-browser-doom) and [doom-wasm](https://github.com/cloudflare/doom-wasm) already run DOOM in the browser via Emscripten. Fork one, add a kill callback: in the C code path where a monster dies and the killer is the player, call an Emscripten-exposed JS function that does `window.postMessage({ type: 'doom-kill' }, '*')`. The OASIS launcher already has the listener pattern; you’d swap the game iframe/canvas to the WASM build and award XP on each message. No DOSBox, no file polling. |
| **C. Build Chocolate Doom for WASM yourself** | **Medium** | 2–4 days | Same end state as B, but you set up Emscripten, clone Chocolate Doom, fix CMake/configure for `wasm32`, add the kill→postMessage hook, and integrate. More control, more setup. |

**Option B implemented.** The launcher has **Play DOOM (per-kill XP)**. See `doom-browser/doom-wasm/README.md`: clone [cloudflare/doom-wasm](https://github.com/cloudflare/doom-wasm), apply `doom-wasm/p_inter.patch`, build with Emscripten, copy built files into `doom-browser/doom-wasm-build/`. The embed page posts `doom-kill` to the parent; the launcher awards 5 XP per kill.

**Recommendation:** Option **B** is the fastest path to true per-kill XP: start from an existing browser DOOM port, add a small C hook where the player’s kill is recorded, expose it to JS via Emscripten, and have the launcher listen for `doom-kill` and call the OASIS XP API.

---

## 6. Summary

- **OASIS already has** a single identity (GUID, username, name) and optional portrait/model and karma/level via Avatar + AvatarDetail APIs.
- **Half-Life** and **DOOM** don’t have avatars; they have a player entity and in-memory state. They only need to be given “who is playing” (avatar id + display name) and optionally portrait/model/karma/level.
- **Single avatar in both games:**
  - Use one OASIS account/avatar for the user.
  - Expose a small **game avatar profile** (identity + optional portrait + optional karma/level) from existing or new endpoint.
  - Use a **launcher or bridge** to authenticate with OASIS, fetch that profile, and pass it into each game via a file or env.
  - Optionally add **mods or source patches** so each game displays the name, portrait, and (if desired) karma/level from that profile.

That way, one player, one avatar, one API — playable in DOOM, Half-Life, Quake, and any other title that adopts the same small contract.

---

## 7. References

- OASIS Avatar API: `Docs/Devs/docs-new/web4-oasis-api/authentication-identity/avatar-api.md`
- OASIS Avatar identity/detail: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Avatar/IAvatar.cs`, `IAvatarDetail.cs`, `Holons/AvatarDetail.cs`
- Half-Life 1 SDK: [ValveSoftware/halflife](https://github.com/ValveSoftware/halflife) — `dlls/player.cpp`, `cl_dll/`; [CBasePlayer](https://developer.valvesoftware.com/wiki/CBasePlayer)
- DOOM source: [id-Software/DOOM](https://github.com/id-Software/DOOM) — `linuxdoom-1.10/d_player.h` (`player_t`), `p_mobj.h` (mobj_t)
