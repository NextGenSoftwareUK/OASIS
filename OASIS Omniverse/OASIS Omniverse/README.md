# OASIS Omniverse Unity Host Shell

This Unity project lives at:

- `C:/Source/OASIS-master/OASIS Omniverse/OASIS Omniverse`

It boots into a floating space hub with two glowing spinning portals:

- `ODOOM`
- `OQUAKE`

Walking through a portal activates the corresponding preloaded game process with no title/menu transition.

## What is implemented

- Space hub generated at runtime (starfield + first-person controller).
- Two emissive spinning portals with trigger volumes and floating labels.
- Native game process preloading for:
  - `../ODOOM/build/ODOOM.exe`
  - `../OQuake/build/OQUAKE.exe`
- Window embedding into the Unity host window (Windows).
- Process cache with memory-aware stale unload policy:
  - Configurable stale timeout (default 10 min)
  - Configurable low-memory threshold (default 2048 MB available)
- Shared HUD overlay (global `I` key) across hub/hosted games.
- Live inventory + quest fetch integration from WEB4/WEB5 APIs.

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

## Open in Unity

1. Open Unity Hub.
2. Add project from `C:/Source/OASIS-master/OASIS Omniverse/OASIS Omniverse`.
3. Use Unity `2022.3.55f1` (or compatible 2022 LTS).
4. Press Play.

## Notes

- External game hosting is Windows-only for window embedding APIs.
- ODOOM/OQUAKE are native executables, so in-memory Unity scene loading is not possible for these binaries; this host shell uses preloaded process caching + instant activation to maintain immersion.

