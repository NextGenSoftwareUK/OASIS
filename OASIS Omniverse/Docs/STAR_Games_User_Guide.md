# OASIS STAR in OQuake & ODOOM — user guide

This guide is for **players and testers** using **OQuake** (Quake + vkQuake) and **ODOOM** (Doom + UZDoom) with **OASIS STAR** (inventory, quests, NFT hooks, beam-in). Developers: see **`STAR_Quest_System_Developer_Guide.md`** for APIs and integration.

---

## 1. First-time setup

1. **Game data**
   - **OQuake:** You need a legal Quake **id1** folder (`pak0.pak`, etc.). Point the launcher at it with **`OQUAKE_BASEDIR`** or `~/.config/oquake/basedir` (see `OQuake/RUN_OQUAKE.sh` / README).
   - **ODOOM:** Standard Doom IWAD + UZDoom setup per project docs.

2. **Config file — `oasisstar.json`**  
   Lives next to the game executable / in the build folder (OQuake) or as loaded by ODOOM. Typical keys:
   - **`star_api_url`** — WEB5 STAR API (quests, STAR features).
   - **`oasis_api_url`** — WEB4 OASIS API (login, avatar, inventory, saving active quest on profile).
   - **`jwt_token`** / **`refresh_token`** — Saved after successful beam-in (keep private).
   - Optional: mint flags, stack behaviour, NFT provider, **`quest_progress_refresh`** (ODOOM: `client` or `server`).

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

## Changelog

| Date | Note |
|------|------|
| 2026-03-27 | Initial user guide for OQuake + ODOOM STAR features. |
