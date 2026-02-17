# OASIS Omniverse Unity Host Shell

This Unity project lives at:

- `C:/Source/OASIS-master/OASIS Omniverse/OASIS Omniverse`

It boots into a floating space hub with two glowing spinning portals:

- `ODOOM`
- `OQUAKE`

Walking through a portal activates the corresponding preloaded game process with no title/menu transition.

## What is implemented

- Space hub generated at runtime (starfield + first-person controller).
- Dedicated startup scene asset: `Assets/Scenes/OmniverseHub.unity`.
- Two emissive spinning portals with trigger volumes and floating labels.
- Cinematic portal glow post-process pass on the player camera.
- Native game process preloading for:
  - `../ODOOM/build/ODOOM.exe`
  - `../OQuake/build/OQUAKE.exe`
- Window embedding into the Unity host window (Windows).
- Process cache with memory-aware stale unload policy:
  - Configurable stale timeout (default 10 min)
  - Configurable low-memory threshold (default 2048 MB available)
- Shared HUD overlay (global `I` key) across hub/hosted games.
- Omniverse Control Center with tabs for:
  - Inventory (WEB5/WEB4 inventory APIs)
  - Cross-game quests (WEB5 Quest API)
  - Cross-game assets/NFTs (WEB5 + WEB4 NFT APIs)
  - Avatar profile (WEB4/WEB5 avatar APIs)
  - Karma timeline (WEB4 Karma API with source/reason/date/amount)
  - Global settings (audio/music/voice/master, graphics, key bindings)
- Global settings persistence:
  - Local (PlayerPrefs)
  - Remote sync via WEB4 Settings API (`/api/settings/user/preferences` with fallback routes)
- Applying settings rebuilds preloaded ODOOM/OQUAKE sessions so global launch settings propagate.
- List UX features in Control Center:
  - Search/filter box
  - Pagination (Prev/Next + page indicator)
  - Sort controls (field + asc/desc), including status/date/source
  - Per-tab view presets (save/apply/delete)
  - Per-avatar preset persistence via WEB4 Settings API
  - Preset import/export via clipboard JSON
  - Import schema/version validation + legacy migration
  - One-click built-in template views (critical quests, newest karma, etc.)
  - Per-tab refresh
  - Quest priority/status color coding
- Always-visible mini HUD:
  - Cross-quest tracker widget in the top-right corner
  - Auto-refresh every 20 seconds
  - Toggle with backtick key (`) by default
  - Compact objective progress-bar mode toggle with `=` key
  - Uses quest objective/progress fields from API responses when present
- Overlay UX:
  - Control Center and Quest Tracker are both draggable and resizable at runtime
  - Panel position/size persistence per avatar via WEB4 settings
  - Quick layout buttons in Settings:
    - Reset all panel layouts
    - Snap Control Center to TL/TR/Center
    - Snap Quest Tracker to TL/TR/Center
  - Keyboard shortcuts (Ctrl+Alt):
    - `1/2/3` => Control Center TL/TR/Center
    - `7/8/9` => Quest Tracker TL/TR/Center
    - `0` => Reset both layouts
  - Snap transitions are animated (smooth glide)
  - Toast notifications appear for snap/reset actions
  - Toasts include severity styles (`success`, `warning`, `error`)
  - Toast queue with stacked display for rapid actions (no overwrite)
  - Toast queue settings are configurable in Settings (`max visible`, `duration`) and persisted per avatar
  - Toast entries animate in/out and smoothly reflow when the stack changes

## Configuration

Edit `Assets/StreamingAssets/omniverse_host_config.json`:

- `avatarId`: your avatar GUID.
- `apiKey`: STAR/OASIS bearer token if required.
- `staleGameMinutes`: stale cache TTL.
- `lowMemoryAvailableMbThreshold`: unload pressure threshold.
- `games`: executable path, working dir, default map arg, portal placement/color.

## Controls

- `WASD` + mouse: move in the hub.
- Walk through portal collider to activate ODOOM/OQUAKE.
- `I`: open/close shared inventory and cross-quest HUD overlay.
- `F1`: hide active hosted game windows and return visual focus to hub.
- Hotkeys are configurable in the Settings tab.

## Open in Unity

1. Open Unity Hub.
2. Add project from `C:/Source/OASIS-master/OASIS Omniverse/OASIS Omniverse`.
3. Use Unity `2022.3.55f1` (or compatible 2022 LTS).
4. Press Play.

## Notes

- External game hosting is Windows-only for window embedding APIs.
- ODOOM/OQUAKE are native executables, so in-memory Unity scene loading is not possible for these binaries; this host shell uses preloaded process caching + instant activation to maintain immersion.

