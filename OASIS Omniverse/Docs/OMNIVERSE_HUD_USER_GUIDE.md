# OASIS Omniverse HUD — Player Guide

> **Developers:** for architecture, class relationships, data flow, and how to extend the HUD, see [OMNIVERSE_HUD_DEVELOPER_GUIDE.md](OMNIVERSE_HUD_DEVELOPER_GUIDE.md).

The **OASIS Omniverse Control Center** is a Steam/Xbox-style overlay that sits on top of every OGame. It works the same way in ODOOM, OQuake, ODOOM3, ODuke3D, OWolf3D, OQuake2, OQuake2-RTX, OQuake3, and any future OGame — press `I` anywhere and your shared OASIS profile, inventory, quests, NFTs, karma, friends, and cross-game teleport appear without leaving the game.

---

## Always-visible HUD elements

Two elements are visible at all times, even when the Control Center is closed.

### Runtime Status Strip

A thin bar pinned to the very top of the screen. It polls every 0.6 seconds and shows:

```
API LIVE | Circuit OK | AUTH-OK | 42ms    Host Sessions: 3 | Active: ODOOM | Free RAM: 4096MB
```

| Field | Meaning |
|-------|---------|
| `API LIVE` / `API CACHE` | Whether the last STAR API call was a live server response or served from cache |
| `Circuit OK` / `OPEN` | Circuit breaker state — OPEN means the API is being skipped after repeated failures |
| `AUTH-OK` / `AUTH-ERR` | Whether your session token is still valid |
| `Latency` | Round-trip time of the last STAR API call |
| `Host Sessions` | Number of OGame processes currently managed by the hub |
| `Active` | Which OGame window is currently in the foreground |
| `Free RAM` | Available physical memory — used to decide which games to keep preloaded |

Colour coding: **cyan** = healthy, **amber** = degraded (cache or partial failures), **red** = circuit open or auth expired.

You can hide the status strip in the Settings tab.

### Toast notifications

Short notification banners that slide in from the top-centre of the screen and disappear automatically. Three severity levels:

| Icon | Colour | Used for |
|------|--------|---------|
| `[+]` | Blue-tinted | Success — item picked up, quest complete, preset saved |
| `[~]` | Amber-tinted | Warning — no active game, preset not found |
| `[!]` | Red-tinted | Error — API failure, settings save failed |

You can configure how many toasts appear at once (1–8) and how long each stays visible (0.4–8 seconds) in the Settings tab.

---

## Opening the Control Center

Press **`I`** (default) to toggle the Control Center open or closed.

The panel opens to 90% of your screen by default. It is **draggable** (grab the title bar) and **resizable** (drag any edge or corner). Your layout is saved automatically and restored the next time you open it.

You can change the open/close key in the Settings tab.

---

## Tabs

The Control Center has nine tabs across the top.

### Inventory

Your **shared cross-game inventory** — items collected in any OGame appear here regardless of which game you are currently playing.

- **Search**: filter by item name, description, type, or source game
- **Sort**: by Name, Type, or Source — ascending or descending
- **Pagination**: 10 items per page; use Prev / Next or save a preset to jump straight to a filtered view

Each item shows: `Name [Type] from SourceGame`

### Quests

All **active cross-game quests** from the STAR API, spanning every OGame in the Omniverse.

- Colour-coded by **priority**: red = critical/boss/urgent, orange = high, yellow = medium, blue = normal
- Colour-coded by **status**: green = complete, amber = in progress/active, red = failed/blocked, grey = other
- Sort by Name, Status, or Priority
- Built-in templates: **Critical Quests First** (sorts by priority, pre-filters to critical/urgent/boss), **Active Quests** (sorts by status, pre-filters to active/in-progress/started)

### NFTs

Your **cross-game NFT and digital asset collection**. Items minted from in-game pickups (keys, weapons, monsters — based on your `oasisstar.json` mint settings) appear here alongside any NFTs granted through the STAR platform.

- Each entry shows: `Name [Type] | SourceGame` with description
- Sort by Name, Type, or Source
- Built-in templates: **Assets by Source**, **Boss NFTs**

### Avatar

Your **OASIS avatar profile** as stored in the WEB4 OASIS platform:

| Field | Description |
|-------|-------------|
| ID | Unique avatar GUID |
| Username | Your OASIS login name |
| Name | First and last name |
| Email | Account email |
| Title | Avatar title / rank |

### Karma

Your **karma timeline** — every karma event earned across all OGames and OASIS activities.

- **Total karma** shown at the top
- Each entry: `[Date] Source | Amount | Reason`
- Amounts are colour-coded: **green** = positive, **red** = negative
- Sort by Date (newest/oldest), Source, or Amount
- Built-in templates: **Newest Karma First**, **Highest Karma First**

### Settings

All configurable options for the HUD and the hosted game sessions. Press **Save & Apply** to save and immediately propagate changes to all running OGames.

**Audio**

| Control | Description |
|---------|-------------|
| Master Volume | Overall volume for all hosted games |
| Music Volume | Background music |
| Sound FX Volume | Game sound effects |
| Voice Volume | Voice/dialogue audio |

**Display**

| Control | Description |
|---------|-------------|
| Graphics Preset | Low / Medium / High / Ultra |
| Fullscreen | Toggle fullscreen mode |
| UI Font Scale | Scale the Control Center panel (0.8× to 1.5×) |
| High Contrast UI | Darker panel background for better readability |
| Show Runtime Status Strip | Show or hide the top status bar |

**Hotkeys** — all keys are rebindable by typing a key name (e.g. `TAB`, `F2`, `CTRL+H`)

| Setting | Default | Action |
|---------|---------|--------|
| Open Control Center | `I` | Toggle Control Center open/close |
| Hide Hosted Game | `F1` | Minimise the current OGame window |
| Return to Hub | `CTRL+H` | Exit current game and return to the 3D hub |

**Toast notifications**

| Setting | Default | Range |
|---------|---------|-------|
| Toast Max Visible | 3 | 1–8 |
| Toast Duration (sec) | 1.7 | 0.4–8.0 |

**Panel layout quick actions**

Snap panels to preset positions instantly (also available via keyboard shortcuts):

| Button | Action |
|--------|--------|
| Control Center TL / TR / C | Snap Control Center to top-left, top-right, or centre |
| Quest Tracker TL / TR / C | Snap the Quest Tracker mini-HUD to top-left, top-right, or centre |
| Reset Layouts | Return all panels to their default positions and sizes |

### Diagnostics

A live **runtime health snapshot** — useful for troubleshooting connection and performance issues.

**API Gateway**
- Circuit Open state and consecutive failure count
- Last error message
- Whether the last result came from cache
- Auth token expiry status
- Last API call latency (ms) and timestamp of last successful call

**Host Process Runtime**
- Number of OGame sessions managed
- Currently active game ID
- Available physical RAM
- Process restart count
- Window recovery count (times the host had to re-embed a game window)
- Last maintenance message and timestamp

**Export**

Two buttons appear only on the Diagnostics tab:

| Button | What it exports |
|--------|----------------|
| Copy Diag | Full JSON snapshot (runtime health + last 120 log lines) copied to clipboard |
| Copy Diag (Sanitized) | Same snapshot with GUIDs, tokens, emails, URLs, and API keys redacted — safe to paste into a bug report |

### Friends

Your **clan member list** from the STAR platform.

Shows total clan size and how many members are currently online, then lists each member:

```
[ON] username (Leader)  karma:1420  in:ODOOM
[off] anotherplayer     karma:340
```

| Indicator | Meaning |
|-----------|---------|
| `[ON]` | Member is currently online |
| `[off]` | Member is offline |
| `(Role)` | Clan role if not plain Member (e.g. Leader, Officer) |
| `karma:N` | Member's total karma score |
| `in:GameId` | Which OGame the member is currently playing |

Use **Refresh** to reload the clan list from the STAR API.

### Teleport

A **quick-travel panel** showing all configured OGames as coloured cards (each tinted with that game's portal colour from `oasis_star_assets.json`). Click a card to teleport instantly.

- Clicking a card fires an `EnterPortalAsync` call to `OmniverseKernel` and closes the Control Center
- Optional **Map name** field: type a map name before pressing **Go** to request a specific starting map within that game (logged as `[OASIS Teleport] Map-targeted teleport: game=X map=Y`)
- Up to 10 game cards are displayed, arranged in a 2-column grid

---

## Preset system

Any tab that shows a list (Inventory, Quests, NFTs, Karma) supports **view presets** — saved combinations of search filter + sort field + sort direction.

| Control | Action |
|---------|--------|
| Preset name field | Type a name for your preset |
| Save Preset | Save the current search + sort as a named preset |
| Preset dropdown | Select a saved preset |
| Apply | Apply the selected preset |
| Delete | Delete the selected preset |
| Template dropdown | Pick from built-in templates for the current tab |
| Template button | Apply the selected template (also fills in the name field) |
| Export | Copy all your presets to clipboard as JSON |
| Import | Load presets from clipboard JSON |

The last applied preset per tab is remembered and restored automatically when you switch back to that tab.

---

## Hotkeys summary

| Key | Action |
|-----|--------|
| `I` | Toggle Control Center open / close |
| `F1` | Hide the current hosted game window |
| `Ctrl+H` | Return to Hub (shows confirmation dialog) |
| `Enter` / `Escape` | Confirm / cancel the Return to Hub dialog |
| `Ctrl+Alt+0` | Reset all panel layouts to defaults |
| `Ctrl+Alt+1` | Snap Control Center to top-left |
| `Ctrl+Alt+2` | Snap Control Center to top-right |
| `Ctrl+Alt+3` | Snap Control Center to centre |
| `Ctrl+Alt+7` | Snap Quest Tracker to top-left |
| `Ctrl+Alt+8` | Snap Quest Tracker to top-right |
| `Ctrl+Alt+9` | Snap Quest Tracker to centre |

All three main hotkeys (open/close, hide game, return to hub) are rebindable in the Settings tab.

---

## Return to Hub

When you are inside an OGame, a **Return to Hub** button appears in the Control Center title bar. Press it (or press `Ctrl+H`) to exit the current game and return to the 3D OASIS space hub. A confirmation dialog appears first — press **Yes** or `Enter` to confirm, **No** or `Escape` to cancel.

If no game is active the button is hidden and the hotkey shows a "No active game" warning toast.

---

## Quest Tracker mini-HUD

Separate from the Control Center, the **Quest Tracker** is a compact always-visible widget (default top-right of screen) that shows your current active quests without having to open the full overlay. It refreshes automatically every 20 seconds. You can reposition it independently of the Control Center using the Quest Tracker layout buttons in the Settings tab, or with `Ctrl+Alt+7/8/9`.
