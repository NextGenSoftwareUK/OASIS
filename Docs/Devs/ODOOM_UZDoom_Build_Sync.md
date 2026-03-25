# ODOOM vs UZDoom — why the timer, toggles, and quests “go in circles”

## The core issue: two trees, one is canonical

You maintain integration files under **`OASIS Omniverse/ODOOM/`** (this repo).  
The **engine that actually runs** is built from **`UZDOOM_SRC`** (your UZDoom checkout, often `$HOME/Source/UZDoom`).

Those are **not the same folder**. The build script **copies** from ODOOM into UZDoom before compile:

| Canonical (edit here) | Copied to (what CMake compiles) |
|------------------------|----------------------------------|
| `ODOOM/odoom_inventory_popup.zs` | `$UZDOOM_SRC/wadsrc/static/zscript/ui/statusbar/odoom_inventory_popup.zs` |
| `ODOOM/uzdoom_star_integration.cpp` | `$UZDOOM_SRC/src/uzdoom_star_integration.cpp` |
| (+ other files listed in `BUILD_ODOOM.sh` / `BUILD ODOOM.bat`) | |

If you change the ODOOM repo but **do not** run the copy step (full `BUILD_ODOOM.sh` / batch file, or at least the `cp` lines), the game you launch still uses **old** ZScript and **old** C++ from the UZDoom tree.

**Symptom:** “I removed the right timer in the repo but it’s still on screen” → the running build never picked up your `.zs`.

**Symptom:** “B/X/Z toggles don’t work” → the running binary is still linked with an old `uzdoom_star_integration.cpp` (or input path not rebuilt).

## STAR native library must match `star_api.h` (no stale `star_api.so` / `star_api.dll`)

The ODOOM binary is **dynamically linked** against functions declared in `OASIS Omniverse/STARAPIClient/star_api.h`. At launch, the loader resolves those symbols from **`star_api.so`** (Linux), **`star_api.dylib`** (macOS), or **`star_api.dll`** (Windows) on the library search path (typically **the same directory as the executable** in `ODOOM/build/`).

**There is no optional code path for missing exports.** If `uzdoom_star_integration.cpp` calls `star_api_start_quest_then_set_active_objective` (or any newer export) but the `.so` next to the game was built from an **older** STARAPIClient, you get an immediate runtime error such as:

`symbol lookup error: undefined symbol: star_api_start_quest_then_set_active_objective`

That is not a bug in ODOOM logic — it means the **packaged native library is stale** relative to the header and C++ integration.

### Why copies go stale

- **Two artifacts:** the repo’s `STARAPIClient/star_api.h` and `uzdoom_star_integration.cpp` update in git, but **`ODOOM/build/star_api.so`** (or `ODOOM/star_api.so`) is only refreshed when you **publish STARAPIClient** and run **`BUILD_ODOOM.sh`** (or equivalent copy steps).
- **Hand-running the binary** from `ODOOM/build/` without a full script pass can pair a **new** `ODOOM` with an **old** `star_api.so` copied earlier.
- **Deleting only `ODOOM/build/`** does not rebuild the C# native library; redeploy STARAPIClient when exports change.

### Required workflow when a new native export is added

1. Add **`[UnmanagedCallersOnly]`** in `StarApiClient.cs`, declare it in **`star_api.h`**, and list it in **`star_api.def`** (Windows).
2. **Rebuild and deploy** the native library:
   - Linux/macOS: `bash OASIS Omniverse/STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh -ForceBuild`
   - Or: `BUILD_STAR_CLIENT=1 ./BUILD_ODOOM.sh` from `OASIS Omniverse/ODOOM`
3. Run **`BUILD_ODOOM.sh`** through packaging so **`ODOOM/build/`** contains the **same** `.so` the linker used.

**Verification (automated):** `STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh` maintains a **`REQUIRED_STAR_EXPORTS`** list. Before trusting “native library is up to date”, the script checks the **existing** `star_api.so` with `nm` / `objdump` / `readelf`. If any required symbol is missing, it **forces `dotnet publish`** even when file mtimes say “unchanged” (mtimes lie after `git pull`, checkout, or skew). **`BUILD_ODOOM.sh`** also refuses to continue without `nm`, `objdump`, or `readelf` so the check cannot be skipped silently.

**When you add a new game-used native export:** add the symbol name to **`REQUIRED_STAR_EXPORTS`** in `build-and-deploy-star-api-unix.sh` and to the **`dumpbin /exports`** check list in `Scripts/publish_and_deploy_star_api.ps1` (Windows), or the skip logic can still ship a stale DLL.

### Policy for agents and contributors

- **Do not** add runtime optional resolution or “best effort” fallbacks for missing `star_api_*` symbols in ODOOM C++.
- **Do** treat “undefined symbol” at launch as **deploy/build drift**; rebuild STARAPIClient, redeploy, repackage ODOOM.
- General agent policy (no hacks / fallbacks for any subsystem): **`Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md`** and the top of **`AGENTS.md`**.

## Where the “right timer” ZScript actually lives (why `cmake --build` sometimes changes nothing)

The overlay is **not** read from `OASIS Omniverse/ODOOM/*.zs` at runtime. ZScript is **compiled into engine resource archives** (`.pk3` / similar) when UZDoom builds. Those archives are produced under **`$UZDOOM_SRC/build/`** (e.g. `uzdoom.pk3` and related files — exact names depend on the target).

`BUILD_ODOOM.sh` then **copies** `*.pk3` from `$UZDOOM_SRC/build` into **`OASIS Omniverse/ODOOM/build/`** next to the staged `ODOOM` executable.

So:

- **Cleaning only CMake objects** (or “rebuild” without invalidating pk3 inputs) can leave an **old pk3** on disk. The game loads that → old HUD (right timer) persists.
- **Deleting only `OASIS Omniverse/ODOOM/build/`** removes the **staged** copy of the exe + pk3s. It does **not** remove the **source** of staleness: **`$UZDOOM_SRC/build/*.pk3`**. The next packaging step **re-copies** whatever pk3s are still in the UZDoom tree — if those are still old, the timer is still there.

**Nuclear but reliable (ZScript + binary both refresh):**

1. Copy integration: run the `cp` lines from `BUILD_ODOOM.sh` (or run the full script through the copy step).
2. Wipe the **UZDoom** CMake output: remove **`$UZDOOM_SRC/build`** entirely (or delete all **`*.pk3`** there plus run a clean rebuild).
3. Run **`BUILD_ODOOM.sh`** end-to-end so it configures, builds UZDoom, then **re-copies** fresh pk3 + `ODOOM` + `libstar_api.so` into **`ODOOM/build/`**.

**Targeted (try first):** delete **`$UZDOOM_SRC/build/*.pk3`** (and any `game_*.ipk3` if present), then `cmake --build` again — if the build system still skips lump rebuild, fall back to deleting the whole **`$UZDOOM_SRC/build`** directory.

## Why deleting `ODOOM/build` seemed to fix HUD but then quests “broke”

`ODOOM/build/` is a **launch bundle**, not the compiler workspace. It should contain together:

- The **`ODOOM`** executable (copy of `uzdoom`)
- **`*.pk3`** copied from `$UZDOOM_SRC/build` (ZScript + game data lumps)
- **`libstar_api.so`** / **`star_api.so`** next to the exe (Linux loader / `RUN_ODOOM.sh` expects this)

If you **delete `ODOOM/build` without** running the full packaging step (or you only partially restore it):

- **`RUN_ODOOM.sh`** may fall back to **`$UZDOOM_SRC/build/uzdoom`** — different cwd and pk3 layout; behaviour can change vs running from `ODOOM/build`.
- **`libstar_api.so`** may be missing next to the binary you actually run → STAR fails to load or loads wrong → **quests/inventory look “broken”** even though quest ZScript is fine.
- You can end up with a **new** `uzdoom` binary (new C++ / exports) paired with an **old** `libstar_api.so` (or the reverse) → native API mismatch → quest list and other STAR paths fail.

**Rule:** Treat **`BUILD_ODOOM.sh` all the way through “Packaging output”** as one atomic step: STAR deploy → copy integration → build UZDoom → **copy pk3 + exe + STAR .so into `ODOOM/build/`**. Don’t hand-assemble `ODOOM/build` from random pieces.

**`libstar_api.so` vs `star_api.so` (Linux):** The ODOOM binary’s ELF **`DT_NEEDED` is `libstar_api.so`**, while NativeAOT publishes **`star_api.so`**. Packaging must **`cp` fresh `star_api.so` → `libstar_api.so`** in **`ODOOM/build/`** every time. If you only refresh `star_api.so` in that folder but leave an older `libstar_api.so`, the loader uses the old file → **`undefined symbol`** for new exports even though `nm` on `star_api.so` looks fine. `RUN_ODOOM.sh` now re-syncs `libstar_api.so` from `star_api.so` whenever the latter exists.

**Rule:** After changing quest API or `StarApiClient` exports, rebuild/deploy STAR and then rebuild ODOOM **in one coherent pass** (the official script does STAR deploy then copy then UZDoom build).

## What to do (minimal checklist)

1. Edit files in **`OASIS Omniverse/ODOOM/`** (and STARAPIClient if needed).
2. Run **`BUILD_ODOOM.sh`** (Linux/macOS) or **`BUILD ODOOM.bat`** (Windows) so the **install integration files** step runs — or manually run the same `cp` commands toward `$UZDOOM_SRC`.
3. Build UZDoom (the script does this; or your usual CMake step **after** the copy).
4. If ZScript still looks wrong, do a **clean** UZDoom build so `zscript` / `gzdoom.pk3` / ipk3 outputs are regenerated (incremental builds can occasionally leave you with odd states; a clean is heavier but deterministic).

## Right-side level timer vs left (native) timer

- The **duplicate** MM:SS on the **right** was drawn from **`RenderOverlay`** in `odoom_inventory_popup.zs`. Removing it **only affects the game** after that updated `.zs` is **copied and compiled** into the engine resources.
- A **left** clock (if present) comes from **native** status bar code in your **UZDoom** fork (e.g. `shared_sbar.cpp`), gated by `odoom_hud_show_timer`. That is separate from the ZScript overlay.

## B / X / Z toggles

Toggles are implemented in **`uzdoom_star_integration.cpp`** (raw key edge detection + `odoom_hud_show_*` CVars). They only behave as intended in the binary built from the **copied** `uzdoom_star_integration.cpp`. Stale config binding `B`/`X`/`Z` to old CCMDs can also cause double-toggles — use empty defaults from a fresh `defaultbind` pass or clear those binds in `gzdoom.ini`.

## See also

- `Docs/Devs/ODOOM_Quest_List_STAR.md` — quest list CVars and invariants (don’t “fix” scrolling by random rebuilds without sync).
