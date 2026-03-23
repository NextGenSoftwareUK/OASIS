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
