# OASIS STAR in OQuake & ODOOM — user guide

This guide is for **players and testers** using **OQuake** (Quake + vkQuake) and **ODOOM** (Doom + UZDoom) with **OASIS STAR** (inventory, quests, NFT hooks, beam-in). Developers: see **`STAR_Quest_System_Developer_Guide.md`** for APIs and integration.

---

## 1. First-time setup

1. **Game data**
   - **OQuake:** You need a legal Quake **id1** folder (`pak0.pak`, etc.). Point the launcher at it with **`OQUAKE_BASEDIR`** or `~/.config/oquake/basedir` (see `OQuake/RUN_OQUAKE.sh` / README).
   - **ODOOM:** Standard Doom IWAD + UZDoom setup per project docs.

2. **Config file — `oasisstar.json`**  
   Lives in each game’s **build** folder (see **§8** for every key and what it does). **`star config`** / **`star config save`** in-game show or persist the same values.

3. **Beam-in (log in)**  
   You must **beam in** so STAR knows your avatar; pickups, quests, and inventory sync are gated until then.

---

## 2. Beam-in / beam-out

### OQuake (console)

- Open the **console** (default `~` or Quake key).
- **`star beamin <username> <password>`** — Log in (async; wait for “Beam-in successful” or check HUD message).
- **`star beamin`** — Uses environment variables **`STAR_USERNAME`** / **`STAR_PASSWORD`** or API key fields from config if set.
- **`star beamout`** — Clear session / disconnect.

### ODOOM

- Equivalent **`star beamin`** / **`star beamout`** commands in the console (see on-screen **`star help`** output).

### Tips

- If login fails, check **`star_api_url`** and **`oasis_api_url`** (WEB4 is required for SSO and token refresh).
- **`star debug on`** — Extra logging (and `star_api.log` where enabled) for support.

---

## 3. Inventory (STAR)

### OQuake

- **`I`** — Toggle **OASIS inventory** overlay (default bind if key was free at first run).
- Tabs for keys, powerups, weapons, etc. (depends on build).
- **`C`** / **`F`** — Use **health** / **armor** from inventory when supported (polls in HUD path).
- Items can show **[NFT]** when minted (see mint settings in `oasisstar.json`).

### ODOOM

- Inventory overlay and keys are described in project docs; behaviour mirrors OQuake patterns (STAR sync, send to avatar/clan where implemented).

### Console

- **`star inventory`** — List STAR inventory.
- **`star has <item>`** — Check an item by name.
- **`star use <item>`** — Use an item (if server rules allow).

---

## 4. Quests

### What you need to know

- Quests live on the **STAR server**; the game shows them in a **quest list** and a **small HUD tracker** when you have an **active quest** set.
- You should **beam in** first; the client loads your **active quest / objective** from your avatar profile when possible.

### OQuake — keys and UI

| Action | Default |
|--------|---------|
| Open / close **quest list** | **`Q`** |
| Open **inventory** | **`I`** (if bound) |
| **Tracker**: cycle objective / “All” / hide | **`O`** (when quest list is **closed** and a quest is tracked) |
| **Filters** in quest list | On-screen toggles (Not started / In progress / Completed) |
| **Select quest row** | Up / Down arrows |
| **Open details** (sub-quests, objectives, prereqs) | **Enter** on a quest |
| **Start tracking** / set active objective | **Enter** on objective or **`K`** on a quest row (see on-screen hints) |
| Close detail / back | **Backspace** (typical) |

Exact labels may vary slightly by build; if in doubt, open **`star`** with no args for the command list.

### ODOOM — keys

- **`Q`** — Quest list popup.
- Filters / scroll / selection — See **`OASIS Omniverse/Docs/ODOOM_Quest_List_STAR.md`** for B/N/M filters and scroll behaviour (developer-oriented but accurate for keys).

### Quest progress during play

- **Kills, pickups, keys, XP** can advance **objectives** automatically when a quest is **active** and the server quest definition matches your game (progress is sent in the background).
- **OQuake** also sends **level time** periodically for time-limited objectives.
- While the **quest list** is open, the client **pauses** automatic progress sync to avoid conflicting with the UI; close the list to resume normal updates.

### Console (advanced)

- **`star quest ...`** — Subcommands for start / objective / complete (see **`star`** help). Most players use the in-game UI instead.

---

## 5. Face / XP / HUD

- **Beam-in face** (anorak): toggled with **`star face on|off|status`** and related CVars; OQuake uses `face_anorak.png` staged into game **`id1/gfx/`** (see OQuake README / `RUN_OQUAKE.sh`).
- **XP** may show on the HUD after profile load; **`star_api_refresh_avatar_profile`** runs as part of beam-in flow.

---

## 6. Useful `star` commands (OQuake examples)

Run **`star`** alone for the full list. Common ones:

| Command | Purpose |
|---------|---------|
| `star version` / `star status` | Integration health |
| `star config` | Show URLs and flags |
| `star config save` | Write `oasisstar.json` / config |
| `star seturl` / `star setoasisurl` | Change API bases |
| `star reloadconfig` | Reload JSON |
| `star debug on|off` | Verbose STAR logging |

---

## 7. Troubleshooting

| Symptom | What to try |
|---------|-------------|
| “Not beamed in” / no pickups to STAR | Run **`star beamin`**; check WEB4 URL and credentials. |
| Quest list empty | Server has no quests for avatar, or filters hide all; beam-in again; **`star debug on`**. |
| Tracker missing | Set active quest from quest UI (**Enter** / **K**); ensure profile save succeeded. |
| Inventory out of date | Close/reopen **`I`**; **`star inventory`**; check network. |
| Linux OQuake face missing | Ensure **`RUN_OQUAKE.sh`** can find your Quake **`id1`** folder (syncs `face_anorak.png` there). |

---

## 8. `oasisstar.json` reference (ODOOM & OQuake)

Both games read this JSON from their **build** folder when present (paths: `OASIS Omniverse/ODOOM/build/oasisstar.json`, `OASIS Omniverse/OQuake/build/oasisstar.json`). Values are usually `0` / `1` unless noted. Edit with the game closed if possible; otherwise use **`star reloadconfig`** after saving.

### 8.1 APIs, transport, and debug

| Key | Games | Meaning |
|-----|--------|---------|
| **`star_api_url`** | Both | WEB5 STAR API base URL (quests, STAR inventory, progress). |
| **`oasis_api_url`** | Both | WEB4 OASIS API base URL (login, avatar, token refresh). |
| **`star_transport`** | Both | How the native client talks to STAR (e.g. `remote`). |
| **`oasis_dna_path`** | Both | Optional path hint for OASIS DNA / local tooling (see developer transport docs if you use it). |
| **`config_file`** | OQuake | Internal label for config format; written when OQuake saves JSON. Safe to leave as `json`. |
| **`beam_face`** | Both | `1` = show beam-in / anorak-style face where supported; `0` = off. |
| **`star_debug`** | ODOOM | `1` = extra STAR logging when the game applies this file at load. |

### 8.2 Session (private — do not share)

| Key | Games | Meaning |
|-----|--------|---------|
| **`jwt_token`** | Both | Access token after beam-in (long string). Legacy alias: **`saved_jwt`**. |
| **`refresh_token`** | Both | Refresh token for renewing the session. |
| **`beamedin_avatar`** | Both | Username / label last beamed in. Legacy alias: **`saved_username`**. |

If the session expires, the client clears tokens on save so the next launch asks you to beam in again.

### 8.3 Pickups: use now vs bank in STAR (health, armor, powerups)

These control **whether the game engine applies the pickup immediately** or **sends it to STAR** when you are **below** max. They do **not** replace **`stack_*`** (see §8.5).

| Key | Games | `0` | `1` |
|-----|--------|-----|-----|
| **`use_health_on_pickup`** | Both | Below max health: pickup goes to **STAR** (bank); engine does not heal you on touch. | Below max: **normal game** — engine applies health on pickup. |
| **`use_armor_on_pickup`** | Both | Below max armor: **bank in STAR** only. | Below max: **normal** — engine applies armor. |
| **`use_powerup_on_pickup`** | Both | Below max (where relevant): **bank** megahealth-style / powerup pickups in STAR. | Below max: **normal** — engine applies the powerup. |

**At max** (full health, full armor, etc.): **`always_allow_pickup_if_max`** decides whether you can still pick the item up **into STAR** (`1`) or vanilla “can’t pick up” / stays on floor (`0`). Legacy OQuake alias: **`always_allow_pickup`** (same as **`always_allow_pickup_if_max`**).

**`always_add_items_to_inventory`**: `1` = **also** add a STAR row when the **engine already used** the pickup (you can get both the in-game effect and a STAR line). `0` = only add to STAR when the integration’s logic says so (e.g. at max banking, intercept path), not every time the world pickup is consumed.

**`max_health`** / **`max_armor`**: Caps used when **using** health/armor from the STAR overlay (and related checks). Sensible defaults: OQuake often `100`/`100`, ODOOM often `200`/`200`.

### 8.4 Quest cache refresh (ODOOM)

| Key | Meaning |
|-----|---------|
| **`quest_progress_refresh`** | `client` (or anything other than `server` / `full` / `1`) = merge quest progress in the client cache after updates. `server` / `full` / `1` = prefer a fuller server refresh mode. See **`STAR_Quest_System_Developer_Guide.md`** for detail. |

### 8.5 `stack_*` keys (what they actually do)

**Important:** `stack_armor`, `stack_weapons`, `stack_powerups`, `stack_keys` (and OQuake’s **`stack_sigils`**) are **not** the same as “use on pickup vs save for later.” They select **how OQuake queues STAR sync** when the game reports a pickup (event style vs simple single-step style). In practice, many powerup lines do not carry a `+number` in the description, so `1` vs `0` can look similar.

| Key | Games | `1` (“stack” in `star config`) | `0` (“unlock”) |
|-----|--------|-------------------------------|----------------|
| **`stack_weapons`** | Both read | OQuake: event-style sync for weapon discoveries. | OQuake: unlock-style sync. |
| **`stack_powerups`** | Both read | OQuake: event-style sync for powerup stat updates. | OQuake: unlock-style sync. |
| **`stack_keys`** | Both read | OQuake: event-style sync for keys. | OQuake: unlock-style sync. |
| **`stack_armor`** | Both read | OQuake: **suppresses** duplicate armor lines from one code path (armor amounts still come from the separate armor-delta logic). | OQuake: unlock-style from the armor-flag path. |
| **`stack_sigils`** | **OQuake only** | Same pattern as other `stack_*` for Quake sigil pieces. | Same. |

**ODOOM:** These `stack_*` values are **loaded from JSON into engine settings** and appear in **`star config`**, but the main **`PostTouch` → STAR queue** path does **not** branch on them the way OQuake’s stat-sync path does. Merging duplicate item names on the STAR API still uses the client’s add-item behaviour. Treat ODOOM `stack_*` as **reserved for parity** until a future build wires them the same as OQuake.

### 8.6 Mint: category toggles

| Key | Meaning |
|-----|---------|
| **`mint_weapons`** | `1` = mint NFT when a weapon pickup is set up to mint (WEB4 NFTHolon path). |
| **`mint_armor`** | Same for armor category. |
| **`mint_powerups`** | Same for powerups. |
| **`mint_keys`** | Same for keys. |
| **`nft_provider`** | Provider name (e.g. `SolanaOASIS`). |
| **`send_to_address_after_minting`** | Optional wallet address to receive minted NFTs. |

### 8.7 Mint: per-monster toggles

Keys look like **`mint_monster_<id>`** (e.g. **`mint_monster_oquake_shambler`**, **`mint_monster_odoom_cyberdemon`**). **`1`** = allow mint when that monster kill is configured to mint; **`0`** = off. The exact list is whatever each game ships in its monster table; your `oasisstar.json` may list many rows. **If a key is missing**, defaults are typically **on (`1`)** for monsters the game knows about.

### 8.8 Cross-game beam-in maps (ammo & weapons)

Optional **string** values: comma-separated **`From=To`** pairs using the same **display names** STAR stores (without the ` (ODOOM)` / ` (OQUAKE)` suffix). Override these if you want different pairings.

| Key | Purpose |
|-----|---------|
| **`cross_game_doom_ammo_to_quake`** | **OQuake** reads **Doom-sourced** ammo rows and maps them onto Quake **Shells / Nails / Rockets / Cells**. |
| **`cross_game_quake_ammo_to_doom`** | **ODOOM** reads **Quake-sourced** ammo rows and maps them onto Doom **Bullets / Shells / Rockets / Cells** (engine inventory types). |
| **`cross_game_doom_weapon_to_quake`** | **OQuake**: Doom-sourced **Weapon** rows → vkQuake **`give`** digits **`2`–`8`** for id1 weapons (see **`CROSS_GAME_POWERUP_WEAPON_MAP.md`**). In deathmatch, **`give` does nothing** in vanilla Quake — the client ORs **`cl.items`** as a best-effort fallback. |
| **`cross_game_quake_weapon_to_doom`** | **ODOOM**: Quake-sourced **Weapon** rows → **`give`** class names (e.g. **`PlasmaRifle`**, **`BFG9000`**). |

Defaults include **Bullets↔Nails** (chaingun / nailgun), **BFG9000↔Lightning Gun**, **Plasma Rifle↔Super Nailgun**, and **Grenade Launcher→PlasmaRifle** alongside **Super Shotgun** / **Rocket Launcher** alignment. Transfer runs **once per beam-in session** after inventory is ready; use **beam out / beam in** to refresh from STAR after playing the other game. See **`CROSS_GAME_POWERUP_WEAPON_MAP.md`** for the full table.

**Deathmatch:** **OQuake** uses **`give s/n/r/c`** for ammo in **non-DM** only; in **DM** it updates client ammo stats directly (vanilla `give` is disabled). **ODOOM** relies on **`give`** for new ammo types; in DM you may need **`sv_cheats 1`** or an existing ammo inventory — see the cross-game doc.

### 8.9 Quick mental model

- **Bank pickups / vanilla pickups:** **`use_*_on_pickup`**, **`always_allow_pickup_if_max`**, **`always_add_items_to_inventory`**, **`max_health`**, **`max_armor`**.
- **Duplicate STAR rows / sync style (OQuake):** **`stack_*`** (and **`stack_sigils`**).
- **NFTs:** **`mint_*`** and **`mint_monster_*`**.

Use **`star config`** in-game to see the effective values after load.

---

## Changelog

| Date | Note |
|------|------|
| 2026-03-27 | Initial user guide for OQuake + ODOOM STAR features. |
| 2026-03-27 | Added §8 full `oasisstar.json` reference (keys, pickup vs stack, ODOOM `stack_*` caveat). |
| 2026-03-27 | Added §8.8 cross-game beam-in map keys (`cross_game_*`) and doc cross-link. |
