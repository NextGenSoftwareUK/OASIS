# ODuke3D Integration Instructions

High-level steps to integrate the OASIS STAR API into an EDuke32 fork so it becomes **ODuke3D** (cross-game keys with ODOOM, OQuake, ODOOM3, ODOOM3-BFG, and ODuke3D-RT).

## 1. Get the integration files

From **OASIS Omniverse\ODuke3D** you need:

- `oduke3d_star_integration.c`
- `oduke3d_star_integration.h`
- `star_sync.c` and `star_sync.h` (from **OASIS Omniverse\STARAPIClient**)
- `star_api.h` (from STARAPIClient)
- `star_api.lib` and `star_api.dll` (from STARAPIClient publish)
- `OGLib/` headers (from **OASIS Omniverse\OGLib**)

## 2. Add to the EDuke32 build

Copy the files above into `source/duke3d/src/` (for `oduke3d_star_integration.*`, `star_sync.*`, `star_api.h`, `OGLib/`).

Add `oduke3d_star_integration.c` and `star_sync.c` to `source/duke3d/Makefile` (or CMakeLists.txt if your fork uses CMake). Link `star_api.lib` and `winhttp.lib` (Windows) or `star_api.so` (Linux).

## 3. Engine C code hooks

Include `oduke3d_star_integration.h` in the appropriate source files:

### Init / shutdown (game.cpp or app_main.cpp)
```c
#include "oduke3d_star_integration.h"

// At end of app_main():
ODuke3D_STAR_Init();

// In G_GameExit() before engine teardown:
ODuke3D_STAR_Cleanup();
```

### Per-frame tick (game.cpp G_Tics)
```c
// Each game tic:
ODuke3D_STAR_Tick();
```

### Key pickup (player.cpp / P_CheckInventory)
```c
// When player picks up a key card — before or after applying locally:
// Blue card:
ODuke3D_STAR_OnKeyPickup("blue_key");
// Red card:
ODuke3D_STAR_OnKeyPickup("red_key");
// Yellow card:
ODuke3D_STAR_OnKeyPickup("yellow_key");
```

### Door access (sector.cpp / G_OperateSectors)
```c
// When a locked door requires a key the player does NOT have locally:
if (ODuke3D_STAR_CheckDoorAccess("blue_key")) {
    // open the door
}
```

### Actor kill (actors.cpp / A_DamageObject)
```c
// When actor->extra <= 0 (dead):
ODuke3D_STAR_OnActorKilled(actor->picnum, /* engine_is_boss= */ 0);
```

### HUD draw (display.cpp or screen.cpp)
```c
// After the normal status bar has been drawn:
ODuke3D_STAR_DrawHUDStatus();
ODuke3D_STAR_DrawPopupOverlay();
```

### Key handling (game.cpp or input processing)
```c
// Forward key events before normal input processing:
ODuke3D_STAR_HandleKey(scan_code, is_pressed);

// Block game input when a popup is open:
if (ODuke3D_STAR_ShouldBlockInput()) {
    // skip movement / fire / use processing
    return;
}
```

### Avatar face in status bar (status bar draw)
```c
// Replace Duke face tile when OASIS avatar is active:
int face_tile = ODuke3D_STAR_ShouldUseAvatarFace() ? OASFACE : normal_face_tile;
// draw face_tile at the status bar face position
```

> **OASFACE tile:** Add a 32×32 art tile named `OASFACE` to your Duke3D art (e.g. via `duke3d.grp` extension or custom `.grp`). When beamed in, the integration returns 1 from `ShouldUseAvatarFace()` so the engine draws that tile instead of the normal Duke face.

## 4. oasisstar.json

Place `oasisstar.json` (from this folder) next to `eduke32.exe`. It configures monster XP values, boss minting, and key cross-game mappings.  Edit the `"oduke3d"` section to tune values.

## 5. Run

Set `STAR_USERNAME`/`STAR_PASSWORD` or `STAR_API_KEY`/`STAR_AVATAR_ID`, then launch ODuke3D with your Duke3D data (e.g. `eduke32.exe -j C:\Duke3D`).  The console should show:

```
[DUKE3D] OASIS STAR API: Authenticated. Cross-game keys enabled.
```

Cross-game keys from ODOOM, OQuake, ODOOM3, ODOOM3-BFG, or ODuke3D-RT will then open doors in ODuke3D and vice versa.

## 6. Cross-game key mapping

| ODuke3D | ODOOM | OQuake | ODOOM3 / ODOOM3-BFG | ODuke3D-RT |
|---------|-------|--------|----------------------|------------|
| Blue key card | blue/yellow keycard | gold_key | blue_key / yellow_key | blue_key |
| Red key card | red keycard | silver_key | red_key | red_key |
| Yellow key card | yellow keycard | gold_key | yellow_key | yellow_key |
