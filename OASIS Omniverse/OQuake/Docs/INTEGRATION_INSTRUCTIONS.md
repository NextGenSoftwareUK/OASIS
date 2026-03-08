# OQuake integration instructions

High-level steps to integrate the OASIS STAR API into a Quake engine so it becomes **OQuake** (cross-game keys with ODOOM).

## 1. Get the integration files

From **OASIS Omniverse\OQuake** you need:

- `oquake_star_integration.c`
- `oquake_star_integration.h`
- `Code/oquake_version.h`
- `star_sync.c` and `star_sync.h` (generic async layer from **OASIS Omniverse\STARAPIClient**)
- `star_api.h` (from **STARAPIClient**)
- `star_api.lib` and `star_api.dll` (from STARAPIClient publish, or Doom folder if already built)

If using **vkQuake**, also use:

- `vkquake_oquake\pr_ext_oquake.c`
- `vkquake_oquake\apply_oquake_to_vkquake.ps1` (copies files into vkQuake Quake folder)

## 2. Engine C code

- **Include** `oquake_star_integration.h` in your host or main init.
- **Call** `OQuake_STAR_Init()` at startup (e.g. after `PR_Init()` in `Host_Init()`).
- **Call** `OQuake_STAR_Cleanup()` at shutdown (e.g. in `Host_Shutdown()`).
- **On key pickup:** When the player gets silver or gold key, call  
  `OQuake_STAR_OnKeyPickup("silver_key")` or `OQuake_STAR_OnKeyPickup("gold_key")`.
- **On key door:** When the player **uses** (e.g. presses E on) a key door and does *not* have the key locally, call  
  `OQuake_STAR_CheckDoorAccess(door_targetname, "silver_key")` or `"gold_key"`.  
  If it returns `1`, open the door (the key is consumed via STAR API). For parity with ODOOM, call only when the player presses use on the door, not on touch or level load.

## 3. QuakeC / extension builtins

If your QuakeC (e.g. quake-rerelease-qc) uses extension builtins:

- `OQuake_OnKeyPickup(keyname)` → maps to `ex_OQuake_OnKeyPickup`
- `OQuake_CheckDoorAccess(doorname, requiredkey)` → maps to `ex_OQuake_CheckDoorAccess`

Add **pr_ext_oquake.c** (or equivalent) to the build and register these two builtins in your `pr_ext.c` (or equivalent) extension table so they call `OQuake_STAR_OnKeyPickup` and `OQuake_STAR_CheckDoorAccess`. See **vkquake_oquake\VKQUAKE_OQUAKE_INTEGRATION.md** for vkQuake-specific steps.

## 4. Build

- Add `oquake_star_integration.c`, `star_sync.c` (and, for vkQuake, `pr_ext_oquake.c`) to the engine project.
- Link **star_api.lib** (and on Windows, **winhttp.lib**).
- Ensure **star_api.dll** is next to the built exe when running.

## 5. Run

Set **STAR_USERNAME** / **STAR_PASSWORD** or **STAR_API_KEY** / **STAR_AVATAR_ID**, then run the engine with your Quake game data (e.g. `-basedir` to Steam Quake). Keys picked up in OQuake or ODOOM will then open doors in the other game when the STAR API is authenticated.

**oasisstar.json** (in build or game dir) can set **max_health**, **max_armor** (default 100), **always_allow_pickup_if_max** (1 = when at max health/armor still pick up into STAR and remove from floor, like Doom; 0 = original Quake behaviour, item stays on floor when full), and **always_add_items_to_inventory** (1 = always add to STAR even when the engine uses the pickup, so player gets both; 0 = only add when at max, or when at max and always_allow_pickup_if_max=1. When 1, overrides always_allow_pickup_if_max.). Use-from-inventory respects max_health/max_armor and shows a toast when already at max.

**Health/armor same as ODOOM:** Add to STAR only when the engine would leave the item on the floor (did not apply to player). When the engine applies the pickup (stats increase), do not add to STAR. When the player is full and touches health/armor/ammo, the engine should call **OQuake_STAR_OnPickupLeftOnFloor**(item_name, item_type, quantity) so the item is added to STAR, then remove the entity so the item is not left on the floor. QuakeC builtin: `void(string item_name, string item_type, float quantity) OQuake_OnPickupLeftOnFloor = #0:ex_OQuake_OnPickupLeftOnFloor;`

**Health at 100% – two hooks:** Detection when standing on health at max uses two possible code paths. (1) **SV_Impact** in **sv_phys.c**: the apply script patches the server physics so that before each touch is run, `OQuake_STAR_InterceptTouchPickupAtMax(e1, e2)` is called; if the player is at max health/armor the item is added to STAR and the entity is freed. (2) **Touch builtin** in **pr_cmds.c**: fallback patch before `PR_ExecuteProgram(e->v.touch)` so that if the Touch builtin is invoked with (item, player), we intercept and free the item. If health-at-max is still not detected, ensure **apply_oquake_to_vkquake.ps1** was run against your vkQuake source and that both **sv_phys.c** and **pr_cmds.c** were patched (check script output). Some engine builds may use a different collision/touch path; the console will show `[OQuake STAR] InterceptTouch HEALTH touch:` when the intercept is called.

For a full Windows walkthrough, see **Docs/WINDOWS_INTEGRATION.md**.
