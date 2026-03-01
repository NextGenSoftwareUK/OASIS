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
- **On key door:** When the player touches a key door and does *not* have the key locally, call  
  `OQuake_STAR_CheckDoorAccess(door_targetname, "silver_key")` or `"gold_key"`.  
  If it returns `1`, open the door (and optionally consume the key via STAR API).

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

For a full Windows walkthrough, see **Docs/WINDOWS_INTEGRATION.md**.
