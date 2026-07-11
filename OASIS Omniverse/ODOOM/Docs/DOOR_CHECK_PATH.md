# Red door / locked door – code path and verification

This doc describes **exactly** where the engine checks keys for locked doors and how to confirm the ODOOM STAR path is in your build.

## Call chain when you press E on a locked door

1. **Player presses E**  
   Engine finds the line in front of the player and calls `P_ActivateLine` (`p_spec.cpp`).

2. **First key check (often skipped for classic Doom)**  
   `P_ActivateLine` does:
   - `if (line->locknumber > 0 && !P_CheckKeys(mo, line->locknumber, remote)) return false;`  
   In **classic Doom** maps, `line->locknumber` is usually **0** (only UDMF sets it from the `lock` key). So this check is often skipped.

3. **Line special runs**  
   `P_ExecuteSpecial(line->special, line->args[0], ..., line->args[3], ...)` is called.  
   For locked doors the special is e.g. **Door_LockedRaise** (13) or **Generic_Door** with lock in args. The handler calls **`EV_DoDoor`** (`a_doors.cpp`) with `lock = args[3]` (1 = red, 2 = blue, 3 = yellow).

4. **Key check that matters for classic Doom**  
   In **`EV_DoDoor`** (`a_doors.cpp`):
   - `if (lock != 0 && !P_CheckKeys(thing, lock, tag != 0)) return false;`  
   So for a **red door**, `lock == 1` and `P_CheckKeys(thing, 1, ...)` is called here.

5. **Inside P_CheckKeys** (`a_keys.cpp`)  
   The **STAR** block (when `OASIS_STAR_API` is defined) runs first when **!quiet** (player pressed E), so you always get a log and STAR can open the door; then the engine key is checked. When **quiet** (HUD probe), engine key then `UZDoom_STAR_PlayerHasKey`:
   - If **!quiet** (player pressed E) → **`UZDoom_STAR_CheckDoorAccess(owner, keynum, remote)`** first (logs and can open via STAR), then `lock->check(owner)`.
   - If **quiet** → `lock->check(owner)` then `UZDoom_STAR_PlayerHasKey(keynum)` (HUD/key icon).

So the **correct path** for “E on red door” is:  
**P_ActivateLine → P_ExecuteSpecial → EV_DoDoor → P_CheckKeys → UZDoom_STAR_CheckDoorAccess**.

## How to verify your build

After a **full clean build** (patch applied, then build):

1. **Run ODOOM**, beam in, go to a **red locked door** and press **E** (without having the red key in the **current map**).

2. **Console logs to look for** (in order):
   - **`[ODOOM STAR] EV_DoDoor lock=1 (before P_CheckKeys)`**  
     → Confirms **EV_DoDoor** is reached (door path and patch of `a_doors.cpp` are in the binary).
   - **`[ODOOM STAR door v2] E on door keynum=1`**  
     → Confirms **P_CheckKeys** called our STAR block and **UZDoom_STAR_CheckDoorAccess** ran (patch of `a_keys.cpp` and `OASIS_STAR_API` are in the binary).

3. **If you see neither log**
   - Ensure **BUILD ODOOM.bat** was run so that the **patch** is applied to your **UZDoom** source (the script patches `a_keys.cpp` and `a_doors.cpp` under `UZDOOM_SRC`).
   - Ensure CMake was run with **OASIS_STAR_API=ON** and **STAR_API_DIR** set so that `OASIS_STAR_API` is **#defined** for the `zdoom` target (otherwise the STAR blocks in `a_keys.cpp` and `a_doors.cpp` are compiled out).
   - Run **`.\Scripts\verify_a_keys_patch.ps1 -UZDOOM_SRC "C:\Source\UZDoom"`** (or your UZDoom path) to confirm `a_keys.cpp` contains the STAR block.

4. **If you see EV_DoDoor log but not door v2**
   - The door path is correct, but `a_keys.cpp` was not patched or not recompiled (no STAR block), or the build is using an old patch order. Re-run **BUILD ODOOM.bat** (patch applies the “STAR first when !quiet” order so E on door always hits CheckDoorAccess), then do a **full rebuild** so `a_keys.cpp` is recompiled.

5. **If you see both logs but the door still doesn’t open**
   - STAR is being asked; the remaining issue is STAR logic (e.g. key name matching, auth, or consume). Check `g_star_debug_logging` and API/backend.

## Summary

- **Red door key check** for classic Doom runs in **EV_DoDoor** → **P_CheckKeys** → **UZDoom_STAR_CheckDoorAccess**.
- **EV_DoDoor** is in **`src/playsim/mapthinkers/a_doors.cpp`**; **P_CheckKeys** is in **`src/gamedata/a_keys.cpp`**.
- The patch script modifies both files and adds the diagnostic log in **EV_DoDoor** so you can confirm the path with the two console messages above.
