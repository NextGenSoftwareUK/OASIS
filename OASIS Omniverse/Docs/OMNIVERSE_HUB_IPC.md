# Omniverse Hub ↔ Game IPC

How the OASIS Omniverse Hub (Unity) and hosted OGames exchange state through
temp files, what each side guarantees, and the open audit items.

## Signal files

All files live in the OS temp directory (`Path.GetTempPath()`), keyed by the
OASIS avatar GUID. Every writer writes to `<name>.tmp` and renames it into place,
so a reader never sees a partially written file.

| File | Writer | Reader | Meaning |
|------|--------|--------|---------|
| `oasis_avatar_state_{avatarId}.json` | Game (`ogengine_hub_notify_avatar_state`) | Hub, every 1 s | Live XP, karma, active game, current map. Hub treats it as stale after 5 s. |
| `oasis_teleport_{avatarId}.json` | Game (`ogengine_hub_request_teleport`) | Hub, every 0.5 s | Request to switch to another game. Game won't overwrite an unconsumed request younger than 30 s. |
| `oasis_teleport_arrive_{avatarId}.json` | Hub, after activating a game | Game (`ogengine_hub_consume_arrive_file`) | Map + spawn point to load. Consumed by atomic rename to `.consuming`; Hub deletes leftovers older than 30 s. |
| `oasis_hub_hidden_{avatarId}.json` | Hub (F1 / Return to Hub) | Game (`ogengine_hub_is_hidden`) | Hub is in front; game should pause. Hub touches it every 10 s; games ignore it after 60 s (Hub crash). |
| `oasis_hub_refresh_{avatarId}.json` | Any local tool | Hub, every 2 s | Re-scan installed games now. Optional — the Hub also detects new game executables on its own. |

Payload fields are JSON strings/numbers; string values are escaped (`\"`, `\\`).

## Game-side contract (C/C++)

Declared in `OGEngineClient/ogengine.h`, exported from `ogengine.dll` / `.so`.
Games call **one function every frame** and apply its result:

```c
ogengine_hub_frame_t hub;
if (ogengine_hub_frame("<GameId>", current_map, game_is_paused, &hub)) {
    if (hub.pause_change > 0 && !paused) pause_game();
    if (hub.pause_change < 0 &&  paused) unpause_game();
    if (hub.has_arrive) {
        if (hub.arrive_map[0]) load_map(hub.arrive_map);
        /* then, once that map is running: move player to hub.x/y/z (all 0 = default spawn) */
    }
}
```

`ogengine_hub_frame` (C#: `HubFrameDriver`, tested in `HubFrameDriverTests`) owns the
protocol: it polls every 0.5 s, publishes XP/karma/game/map, only undoes pauses the
Hub caused, and validates arrival map names. Map names from the arrive file are
untrusted (any local process can write the temp dir), so only `[A-Za-z0-9_]{1,32}`
is ever returned; an arrival for the map already running returns an empty map.

The lower-level `ogengine_hub_notify_avatar_state`, `ogengine_hub_is_hidden`,
`ogengine_hub_consume_arrive_file` and `ogengine_hub_request_teleport` remain for
games that need finer control or want to request a portal jump themselves.

Reference implementations: `OGames/ODOOM/uzdoom_ogengine_integration.cpp`
(`ODOOM_HubBridgeFrame`) and `OGames/OQuake/Code/oquake_ogengine_integration.c`
(`OQ_HubBridgeFrame`).

Game builds copy `ogengine.h` from `OGEngineClient/` — never edit the per-game copies.
`NativeWrapper/` is deprecated and keeps its own old header on purpose.

`ogengine_get_avatar_karma` takes `int64_t*`. It was declared `long*`, which is
32-bit on Windows while the export writes 64 bits; use `int64_t` and print with
`%lld` / `(long long)`.

## Hub side

- `OmniverseKernel` polls the files above from `Update()`.
- `InstalledGamesMonitor` (pure C#, tested in `OASIS Hub/Tests/HubRuntime.Tests`)
  checks each configured game executable every 2 s; when one appears or
  disappears, portals are refreshed and the player is told.
- Games are installed with STAR CLI / ODK today (STARNET store planned), not OPORTAL.

## Tests and CI

- `OGEngineClient/TestProjects/OGEngine.Client.Tests` — bridge behaviour (`OmniverseHubBridgeTests`).
- `OASIS Hub/Tests/HubRuntime.Tests` — install detection.
- Both run in the `test-omniverse` job of `.github/workflows/ci-cd.yml`.

## Audit — 2026-10-02

| # | Item | Status |
|---|------|--------|
| 1 | IPC files written in place; a reader could see a half-written file and lose an arrival | Fixed — write-then-rename on both sides |
| 2 | Only ODOOM and OQuake call the Hub bridge; 11 other integrated games do not | Partly fixed — `ogengine_hub_frame` added; ODOOM and OQuake use it. Other games: see table below |
| 3 | `ogengine.h` copies in OQuake2, OQuake2-RTX, OQuake3 out of date | Fixed — synced (NativeWrapper is deprecated and left as is) |
| 4 | `async void` Hub methods could fail silently | Fixed — bodies catch and log |
| 5 | Three hand-written JSON writers | Fixed — Hub uses one `IpcJson` helper |
| 6 | Any local process can request a game switch via the teleport file | Accepted for now; game side validates map names |
| 7 | ~143 MB of game binaries tracked in git under `OGames` | Open — needs a decision (Git LFS or release downloads) |
| 8 | Windows build scripts fail cryptically without `vswhere.exe` on PATH or real Python 3 | Fixed — scripts locate vswhere and name what is missing |
| 9 | `test-omniverse` CI job not yet observed green on GitHub | Open — check after next push |
| 10 | CRLF/LF churn in the Hub repo | Fixed — `.gitattributes` added |
| 11 | `ogengine_get_avatar_karma` declared `long*` (32-bit on Windows) but writes 64 bits — stack overwrite in every Windows caller | Fixed — `int64_t*`; ODOOM/OQuake callers updated |
| 12 | Teleport request coordinates used the system number format (`10,5` on de-DE → invalid JSON) | Fixed — invariant culture, with test |
| 13 | A portal stayed disabled for the session if entering it threw | Fixed — `PortalTrigger` resets in `finally` |

Not yet verified in a live session: HUD live state, pause on hide, portal
arrival map/spawn, install detection toast.
