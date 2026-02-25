# vkQuake OQuake (STAR API) integration

This folder contains files and instructions to build **OQuake**: vkQuake with OASIS STAR API so keys from ODOOM can open doors in Quake and vice versa.

**OQuake is based on vkQuake.** Full credit to [vkQuake](https://github.com/Novum/vkQuake) (Novum). vkQuake is licensed under GPL-2.0. When building or distributing OQuake, comply with vkQuake's license and give appropriate credit. See **../CREDITS_AND_LICENSE.md** for details.

## Overview

1. **Copy** OQuake + STAR files into vkQuake's `Quake/` directory.
2. **Add** `pr_ext_oquake.c` to the build and **register** the two OQuake builtins in the engine's extension table.
3. **Call** `OQuake_STAR_Init()` at startup and `OQuake_STAR_Cleanup()` at shutdown.
4. **Link** `star_api.lib` and ship `star_api.dll` next to the exe.

After this, the QuakeC in quake-rerelease-qc (which declares `OQuake_OnKeyPickup` and `OQuake_CheckDoorAccess` as `#0:ex_OQuake_*`) will call into the STAR API.

---

## 1. Files to add under vkQuake `Quake/`

Copy these into `VKQUAKE_SRC/Quake/` (from OASIS Omniverse):

| File | Source |
|------|--------|
| `oquake_star_integration.c` | `OQuake/oquake_star_integration.c` |
| `oquake_star_integration.h` | `OQuake/oquake_star_integration.h` |
| `oquake_version.h` | `OQuake/oquake_version.h` (generated from **`OQuake/oquake_version.txt`** – OQuake's version source; run build or `generate_oquake_version.ps1` to regenerate) |
| `star_api.h` | `NativeWrapper/star_api.h` |
| `pr_ext_oquake.c` | `OQuake/vkquake_oquake/pr_ext_oquake.c` |

Also copy `star_api.dll` and `star_api.lib` (from Doom folder or NativeWrapper build) next to the **built** vkquake.exe; the build only needs the `.lib` for linking.

---

## 2. host.c – init and shutdown

**Include** (near top, with other includes):

```c
#include "oquake_star_integration.h"
```

**In `Host_Init()`** call `OQuake_STAR_Init()` **after** the console is initialized (e.g. after `Con_Init()` or equivalent) so the "Welcome to OQuake" and STAR hint appear in the in-game console:

```c
/* ... after Con_Init() or when console is ready ... */
OQuake_STAR_Init ();
```

**In `Host_Shutdown()`** (e.g. at start of the function, before or after `NET_Shutdown`):

```c
OQuake_STAR_Cleanup ();
```

---

## 3. pr_ext.c – OQuake builtins

**Externs** (near the other `extern void PF_*` at top of pr_ext.c):

```c
extern void PF_OQuake_OnKeyPickup (void);
extern void PF_OQuake_CheckDoorAccess (void);
```

**Extension builtin table**: vkQuake maps QC extension function **names** to builtin function pointers when progs are loaded. You need to add the two OQuake names to that table.

- Search in `pr_ext.c` for where extension builtins are registered (e.g. a table of `{ "ex_bprint", PF_bprint }` or a block that calls something like `PR_RegisterExtBuiltin("ex_...", PF_...)`).
- Add two entries so that:
  - `"ex_OQuake_OnKeyPickup"` → `PF_OQuake_OnKeyPickup`
  - `"ex_OQuake_CheckDoorAccess"` → `PF_OQuake_CheckDoorAccess`

Exact location and format depend on the vkQuake version; look for `ex_` or `PR_EnableExtensions` / `PR_FindExtFunction` and add the OQuake pair in the same way as existing extension builtins.

---

## 4. Build system

### Meson

In the `Quake` subdir’s `meson.build` (or wherever `pr_ext.c` is listed), add:

- `oquake_star_integration.c`
- `pr_ext_oquake.c`  
  (No need to add `oquake_version.h` to the build; it is included by `oquake_star_integration.c`.)

Ensure the target that builds the Quake lib/executable links `star_api.lib` (e.g. `link_with` or `link_args`). If `star_api` is built as a DLL, add something like:

```meson
link_args : ['-lstar_api']  # or the path to star_api.lib
```

### Windows Visual Studio

- Add `oquake_star_integration.c` and `pr_ext_oquake.c` to the Quake project (e.g. the vkquake or game lib project that already has `pr_ext.c`). Ensure `oquake_version.h` is in the same Quake folder so the include finds it.
- In Project → Properties → Linker → Input → Additional Dependencies, add `star_api.lib` (or full path to it).
- Ensure `star_api.dll` is in the same directory as the exe when running (BUILD_OQUAKE.bat can copy it there).

---

## 4a. host.c – item/stats poll (recommended; apply script does this)

So that pickups (armor, ammo, keys, etc.) are reported to STAR **every frame**, the apply script patches **host.c** to call **`OQuake_STAR_PollItems()`** right after `CL_ReadFromServer()`. That way items are tracked even if the status bar (sbar) is never drawn or your build doesn’t patch sbar.c.

If you apply OQuake manually, add this in **`_Host_Frame`** (in host.c), right after `CL_ReadFromServer ();`:

```c
OQuake_STAR_PollItems ();
```

Ensure **`#include "oquake_star_integration.h"`** is at the top of host.c (needed for `OQuake_STAR_Init` and `OQuake_STAR_PollItems`).

---

## 4b. sbar.c – do not report pickups here when using the host.c poll

If you use **OQuake_STAR_PollItems()** in host.c (section 4a, recommended), **do not** also call `OQuake_STAR_OnItemsChangedEx` / `OQuake_STAR_OnStatsChangedEx` from sbar.c. Otherwise each pickup is reported twice (e.g. armor shows +2 instead of +1).

When the apply script has patched host.c with the poll, sbar.c should not call the STAR item/stats hooks. The poll in host.c is the single place that reports pickups.

---

## 5. One-shot script (optional)

From `OASIS Omniverse\OQuake`, run:

```powershell
.\vkquake_oquake\apply_oquake_to_vkquake.ps1 -VkQuakeSrc "C:\Source\vkQuake"
```

This copies the required files into `VkQuakeSrc\Quake\`. You still need to **edit host.c and pr_ext.c** and **add the sources + link star_api** in your build system (steps 2–4). The script does not modify host.c or pr_ext.c automatically so you can apply those edits once and keep them.

---

## 6. Show OQuake in the game (window title / bottom-right version)

The **bottom-right of the console** (and the `version` command) in vkQuake shows the engine name and version from the `pr_engine` cvar in **host.c**.

**The apply script does this automatically:** when you run `apply_oquake_to_vkquake.ps1` (or BUILD_OQUAKE.bat, which calls it), the script patches **host.c** to:
1. Add `#include "oquake_version.h"` after `#include "quakedef.h"`.
2. Change `pr_engine` so its default is `OQUAKE_VERSION_STR " (" ENGINE_NAME_AND_VER ")"`, which displays e.g. **OQuake 1.0 (Build 1) (vkQuake 1.10.0)**.

When you run **BUILD_OQUAKE.bat**, you’re asked at the start: **Full clean/rebuild (C) or incremental build (I)? [I]**. Choose **C** for a full rebuild (e.g. so the console version shows “OQuake 1.0 (Build 1) (vkQuake …)”); choose **I** or press Enter for a faster incremental build. The apply script also clears the build cache when it patches host.c so the next build picks up the new version string. After a full rebuild, the bottom-right of the console (and the `version` command) will show e.g. **OQuake 1.0 (Build 1) (vkQuake 1.10.0)**.

No manual host.c edits are needed unless the script fails (e.g. vkQuake layout changed).

### Manual patch (if needed)

If the script did not patch host.c (or you prefer to patch by hand):

1. Add `#include "oquake_version.h"` near the top of `host.c`.
2. Find `static cvar_t pr_engine = {"pr_engine", ENGINE_NAME_AND_VER, CVAR_NONE};` and replace with:
   ```c
   static cvar_t pr_engine = {"pr_engine", OQUAKE_VERSION_STR " (" ENGINE_NAME_AND_VER ")", CVAR_NONE};
   ```

### Window title

If the window title is set elsewhere (e.g. in the Windows or SDL layer), include `oquake_version.h` there and use `OQUAKE_VERSION_STR` (or the same composite string) where the title is set.

---

## 7. Loading / splash screen (like ODOOM)

OQuake shows an **OASIS / OQuake** text splash in the console at startup (from `OQuake_STAR_Init()`). For a **graphic** loading/splash screen like ODOOM (UZDoom):

1. **Asset**  
   Use the provided **`oasis_splash.png`** in the OQuake folder (same style as ODOOM’s `oasis_banner.png`). It is 320×200 so it fits classic resolutions. You can also reuse `OASIS Omniverse\UZDoom\oasis_banner.png` if you have it.

2. **Where to hook in vkQuake**  
   In vkQuake (Novum/vkQuake), the loading screen is usually drawn in the **screen** layer (e.g. `SCR_*` in `screen.c` or equivalent). Find where the engine draws “Loading…” or a loading bar when loading a map.

3. **What to do**  
   - **Option A:** Replace the default loading texture with `oasis_splash.png`: load the PNG as a texture and draw it full-screen (or centered) during load, then draw “OQuake” + version and a loading bar on top if desired.  
   - **Option B:** If the engine has a “splash image” path or convar, point it to `oasis_splash.png` so it appears at startup or during load.

4. **Build script**  
   `BUILD_OQUAKE.bat` does not copy the splash into vkQuake automatically (vkQuake has no wad/pk3 like UZDoom). Copy `oasis_splash.png` into your vkQuake data or binary folder and load it from there in your patched loading-screen code.

Once integrated, the OASIS splash will appear during loading and match the professional look of ODOOM.

---

## 8. Verify

1. Build vkQuake with the above changes.
2. Copy `star_api.dll` next to the engine exe (e.g. `OQUAKE.exe` or `vkquake.exe`).
3. Build or copy quake-rerelease-qc progs (with `OQuake_OnKeyPickup` / `OQuake_CheckDoorAccess` in defs.qc) into the game dir.
4. Run with STAR env set (`STAR_USERNAME` / `STAR_PASSWORD` or `STAR_API_KEY` / `STAR_AVATAR_ID`).
5. In-game: pick up a key in OQuake and/or ODOOM; doors that use the OQuake builtins should open with cross-game keys.

---

---

## 9. Anorak face when beamed in, inventory overlay (I key), and Send to Avatar / Send to Clan

All of the **logic** for these features lives in **OASIS**, in `OQuake/oquake_star_integration.c` (and its header). The apply script copies that file into vkQuake and patches **host.c** only. For the following to **appear in-game**, the **vkQuake engine** must call into the integration:

| Feature | Where the logic lives (OASIS) | What vkQuake must do |
|--------|------------------------------|------------------------|
| **I key** for inventory | `OQuake_STAR_Init()` binds `oasis_inventory_toggle` to key **I** if unbound (see around line 1582 in oquake_star_integration.c). | Nothing extra: once host.c calls `OQuake_STAR_Init()`, the binding is registered. |
| **Inventory popup** (tabs, list, status) | `OQuake_STAR_DrawInventoryOverlay(cb_context_t* cbx)` | Somewhere in the 2D HUD path (e.g. **gl_screen.c** or **r_screen.c**), call `OQuake_STAR_DrawInventoryOverlay(cbx)` so the overlay and **Send to Avatar / Send to Clan** popups are drawn. |
| **Send to Avatar / Send to Clan** | Same overlay: Z=Send Avatar, X=Send Clan; popup uses `g_inventory_send_popup` and `star_sync` send-item API. | Same as above: drawing the inventory overlay draws the send popups. |
| **Beamed In: &lt;username&gt;** text | `OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx)` | In the same 2D HUD path, call `OQuake_STAR_DrawBeamedInStatus(cbx)` (e.g. once per frame when in game). |
| **Anorak face** when beamed in | `OQuake_STAR_ShouldUseAnorakFace()` and cvar `oasis_star_anorak_face` | In **sbar.c**, where the status bar face is drawn, if `OQuake_STAR_ShouldUseAnorakFace()` is true, draw the **face_anorak** pic instead of the normal health face. |

So: **the code is not missing from OASIS** — it is all in `oquake_star_integration.c`. What can be “missing” is the **engine hooks** in **vkQuake’s** `sbar.c` and the 2D drawing file (e.g. **gl_screen.c**). If you re-cloned vkQuake or reverted it, those edits live only in **C:\\Source\\vkQuake** (or wherever your vkQuake source is), not in the OASIS repo.

### 9a. sbar.c – anorak face when beamed in

1. Add at the top with the other includes:
   ```c
   #include "oquake_star_integration.h"
   ```
2. Add a static pic pointer (e.g. next to `sb_face_invis`):
   ```c
   static qpic_t *sb_face_anorak;
   ```
3. In **Sbar_LoadPics**, after loading the other faces (e.g. after `sb_face_quad = Draw_PicFromWad ("face_quad");`), load the anorak face. If your engine can load from the game dir (e.g. `id1/gfx/face_anorak.png`), use that; otherwise ensure `face_anorak` is in your gfx WAD:
   ```c
   sb_face_anorak = Draw_PicFromWad ("face_anorak");  /* or Draw_PicFromFile / engine-specific */
   if (sb_face_anorak == pic_nul)
       sb_face_anorak = NULL;
   ```
   (Use `pic_nul` or whatever your engine uses for “failed to load”.)
4. Where the status bar **face** is drawn (after the invis/invuln/quad special cases, just before `Sbar_DrawPic (cbx, x, y, sb_faces[f][anim]);`), add:
   ```c
   if (OQuake_STAR_ShouldUseAnorakFace() && sb_face_anorak)
   {
       Sbar_DrawPic (cbx, x, y, sb_face_anorak);
       return;
   }
   ```
   Then the normal `Sbar_DrawPic (cbx, x, y, sb_faces[f][anim]);` runs when not beamed in.

The apply script copies **face_anorak.png** into the Quake install dir (`id1/gfx/`). Your engine must load it (e.g. from that path or from a WAD that includes it) for the anorak face to show.

### 9b. gl_screen.c (or equivalent) – inventory overlay and Beamed In text

You need a **cb_context_t** and the same drawing API the status bar uses (`Draw_String`, `Draw_Fill`, etc.). In vkQuake, the sbar is drawn with `cb_context_t *cbx`. Find where the 2D HUD is drawn (e.g. after the status bar or in the same pass).

1. Add:
   ```c
   #include "oquake_star_integration.h"
   ```
2. Where you have `cbx` and are drawing the HUD (e.g. after `Sbar_Draw` or in the same function that draws the sbar):
   ```c
   OQuake_STAR_DrawBeamedInStatus (cbx);   /* "Beamed In: <username>" at bottom-left */
   OQuake_STAR_DrawInventoryOverlay (cbx); /* inventory panel + Send to Avatar/Clan popups */
   ```
   Order depends on desired layering; typically draw Beamed In first, then the overlay so the overlay can sit on top.

After rebuilding vkQuake with these changes, the anorak face when beamed in, the I key inventory, and the Send to Avatar / Send to Clan popups from the inventory will work, using the logic already in **OASIS Omniverse/OQuake/oquake_star_integration.c**.

---

## QuakeC side (quake-rerelease-qc)

Already done if you use the provided defs/items/doors:

- `defs.qc`: `OQuake_OnKeyPickup = #0:ex_OQuake_OnKeyPickup`, `OQuake_CheckDoorAccess = #0:ex_OQuake_CheckDoorAccess`
- `items.qc`: after giving key, call `OQuake_OnKeyPickup("silver_key")` or `"gold_key"`.
- `doors.qc`: if player lacks local key, call `OQuake_CheckDoorAccess(..., "silver_key")` or `"gold_key"`; if it returns 1, open the door.

No changes needed there once the engine builtins are registered.
