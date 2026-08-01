# ODuke3D-RT Integration Instructions

High-level steps to integrate the OASIS STAR API into a Duke-RT fork so it becomes **ODuke3D-RT** (cross-game keys with ODOOM, OQuake, ODOOM3, ODOOM3-BFG, and ODuke3D).

Duke-RT is a Vulkan ray-tracing fork of EDuke32. The OASIS integration hooks are identical to **ODuke3D** — only the prefix (`ODuke3DRT_STAR_*` vs `ODuke3D_STAR_*`) and the game source string (`"ODUKE3DRT"`) differ.

## 1. Get the integration files

From **OASIS Omniverse\ODuke3D-RT**:

- `oduke3drt_star_integration.c`
- `oduke3drt_star_integration.h`
- `star_sync.c` / `star_sync.h` (from **OASIS Omniverse\STARAPIClient**)
- `star_api.h`, `star_api.lib`, `star_api.dll`
- `OGLib/` headers (from **OASIS Omniverse\OGLib**)

## 2. Add to the Duke-RT build

Duke-RT uses **CMake**.  Add `oduke3drt_star_integration.c` and `star_sync.c` to the `CMakeLists.txt` in `source/duke3d/` (or to the game target's source list).  Link `star_api.lib` and `winhttp.lib` (Windows), or `star_api.so` (Linux).

Copy OGLib headers to `source/duke3d/src/OGLib/`.

## 3. Engine C code hooks

Hook locations in `source/duke3d/src/` are identical to ODuke3D:

```c
// Init / shutdown (game.cpp)
ODuke3DRT_STAR_Init();    // end of app_main()
ODuke3DRT_STAR_Cleanup(); // G_GameExit()

// Per-frame (game.cpp G_Tics)
ODuke3DRT_STAR_Tick();

// Key pickup (player.cpp P_CheckInventory)
ODuke3DRT_STAR_OnKeyPickup("blue_key");   // blue access card
ODuke3DRT_STAR_OnKeyPickup("red_key");    // red access card
ODuke3DRT_STAR_OnKeyPickup("yellow_key"); // yellow access card

// Door access (sector.cpp G_OperateSectors)
if (ODuke3DRT_STAR_CheckDoorAccess("blue_key")) { /* open door */ }

// Actor kill (actors.cpp A_DamageObject when extra <= 0)
ODuke3DRT_STAR_OnActorKilled(actor->picnum, /* engine_is_boss= */ 0);

// HUD draw (after G_DrawStatusBar in display path)
ODuke3DRT_STAR_DrawHUDStatus();
ODuke3DRT_STAR_DrawPopupOverlay();

// Key input (G_ProcessInput / KB path)
ODuke3DRT_STAR_HandleKey(scan_code, is_pressed);

// Block movement when popup open (P_ProcessInput)
if (ODuke3DRT_STAR_ShouldBlockInput()) return;

// Face tile in status bar
int face_tile = ODuke3DRT_STAR_ShouldUseAvatarFace() ? OASFACE : normal_face;
```

## 4. RT-specific HUD notes

Duke-RT renders the 3-D world via Vulkan ray tracing, but the 2-D overlay / HUD layer remains the standard EDuke32 `printext256()` and `rotatesprite()` path.  OASIS overlays use `printext256()` in that layer and do not need any Vulkan-specific changes.

If the Duke-RT fork adds a new UI/overlay system that bypasses `printext256()`, adapt `ODuke3DRT_STAR_DrawHUDStatus()` and `DrawPopupOverlay()` accordingly.

## 5. Cross-game key mapping

Same as ODuke3D — see `../ODuke3D/Docs/INTEGRATION_INSTRUCTIONS.md`.

## 6. Run

Set STAR credentials, then launch with `eduke32.exe -j C:\Duke3D`. Console should show:

```
[DUKE3D-RT] OASIS STAR API: Authenticated. Cross-game keys enabled.
[DUKE3D-RT] ODuke3D-RT 1.0.0 initialised.
```
