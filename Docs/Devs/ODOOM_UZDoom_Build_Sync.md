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

## Why deleting the build folder seemed to fix HUD but “broke” quests

- **Clean rebuild** forces ZScript and C++ to recompile from whatever is **currently** under `$UZDOOM_SRC` **after** the last copy.
- If you clean-build **without** copying fresh ODOOM files first, you only made the **stale** snapshot compile cleanly.
- If you copy **and** clean-build, HUD and C++ match — **unless** `star_api` (NativeAOT `.so`) is out of sync with the headers/exports the new C++ expects. Then you get quest/API glitches that look like “quest list broke” when the real issue is **mixed STAR client + engine** versions.

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
