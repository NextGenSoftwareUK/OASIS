# OWolf3D — Windows Integration Guide (Manual diff)

These are the exact changes applied to `C:\Source\OWolf3D\src\` by the build script.
If you prefer to apply them manually rather than running `COPY_TO_ECWOLF_AND_BUILD.ps1`,
follow the diffs below. All paths are relative to `C:\Source\OWolf3D\src\`.

**Cross-game partners:** ODOOM, OQuake, ODOOM3, ODOOM3-BFG, ODuke3D, ODuke3D-RT, and OWolf3D — seven OASIS Omniverse games.
Press **I** in-game for the OASIS Inventory popup, **Q** for Quests.

---

## 1. `CMakeLists.txt` — Add source file and link star_api

### 1a. Add integration source to the `initial_sources(...)` list

Find the last entry in the list:
```cmake
    zstring.cpp
)
```
Change to:
```cmake
    zstring.cpp
    owolf3d_ogengine_integration.cpp
)
```

### 1b. Add star_api link and compile definition after the closing `)`

At the end of `CMakeLists.txt` (or after the `initial_sources(...)` block), add:
```cmake
# OWolf3D: OASIS STAR API link
if(WIN32)
    target_link_libraries(engine PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/ogengine.lib")
    target_compile_definitions(engine PRIVATE OASIS_STAR_SYNC_IN_CLIENT=1)
endif()
```

---

## 2. `wl_main.cpp` — Init and Cleanup hooks

### 2a. Add include after existing includes near top of file

```cpp
#include "owolf3d_ogengine_integration.h"
```

### 2b. `InitGame()` — call OWolf3D_STAR_Init at the end

Find the end of `static void InitGame()` (the last statement before the closing `}`):
```cpp
    // ... existing last init code ...
}
```
Add:
```cpp
    // ... existing last init code ...
    OWolf3D_STAR_Init();   // ← ADD
}
```

### 2c. `Quit()` — call OWolf3D_STAR_Cleanup before SDL_Quit

Find `void Quit()`:
```cpp
void Quit ()
{
    // ... shutdown code ...
    SDL_Quit();
```
Change to:
```cpp
void Quit ()
{
    // ... shutdown code ...
    OWolf3D_STAR_Cleanup();   // ← ADD before SDL_Quit
    SDL_Quit();
```

---

## 3. `wl_game.cpp` — Tick hook (per-frame)

### 3a. Add include

```cpp
#include "owolf3d_ogengine_integration.h"
```

### 3b. `GameLoop()` — call OWolf3D_STAR_Tick each frame

Find the top of the main game loop inside `bool GameLoop()`:
```cpp
    // ... top of loop body ...
    IN_ProcessEvents();
```
Add before `IN_ProcessEvents()`:
```cpp
    OWolf3D_STAR_Tick();   // ← ADD
    IN_ProcessEvents();
```

---

## 4. `actor.cpp` — Monster kill hook

### 4a. Add include

```cpp
#include "owolf3d_ogengine_integration.h"
```

### 4b. `AActor::Die()` — report kill after FL_COUNTKILL

Find the `FL_COUNTKILL` increment block (around line 238):
```cpp
    if(flags & FL_COUNTKILL)
        gamestate.killcount++;
    flags &= ~FL_SHOOTABLE;
```
Add after:
```cpp
    if(flags & FL_COUNTKILL)
        gamestate.killcount++;
    OWolf3D_STAR_OnActorKilled(            // ← ADD
        GetClass()->GetName().GetChars(),
        (flags & FL_AMBUSH) ? 1 : 0);
    flags &= ~FL_SHOOTABLE;
```

---

## 5. `g_shared/a_inventory.cpp` — Item pickup hook

### 5a. Add include

```cpp
#include "owolf3d_ogengine_integration.h"
```

### 5b. `AInventory::TryPickup()` — report item after it's granted

Find the return path inside `AInventory::TryPickup()` where the item is given (around line 260):
```cpp
    toucher->GiveInventory(this);
    GoAwayAndDie();
    return true;
```
Change to:
```cpp
    toucher->GiveInventory(this);
    OWolf3D_STAR_OnItemPickup(             // ← ADD
        GetClass()->GetName().GetChars(),
        pickupMessage.GetChars());
    GoAwayAndDie();
    return true;
```

---

## 6. `g_shared/a_keys.cpp` — Cross-game door access

### 6a. Add include

```cpp
#include "owolf3d_ogengine_integration.h"
```

### 6b. `P_CheckKeys()` — STAR fallback when key is missing

Find the final `return false;` at the end of `P_CheckKeys()`:
```cpp
    // If we get here, that means the actor isn't holding an appropriate key.
    // ...
    return false;
}
```
Change to:
```cpp
    // If we get here, that means the actor isn't holding an appropriate key.
    // ...
    if (OWolf3D_STAR_CheckDoorAccess(keynum)) return true;   // ← ADD
    return false;
}
```

---

## 7. `wl_play.cpp` — Popup key handling

### 7a. Add include

```cpp
#include "owolf3d_ogengine_integration.h"
```

### 7b. `CheckKeys()` — forward keys to STAR handler

Near the top of `void CheckKeys()`, before the existing key processing:
```cpp
void CheckKeys (void)
{
    // ... screenfaded / demoplayback guard ...
    OWolf3D_STAR_HandleKeys();   // ← ADD after the faded/demo guard
```

### 7c. Block player input when popup is open

Find movement control in `wl_play.cpp` (inside the per-frame player update, look for `ControlMovement`):
```cpp
    ControlMovement(players[ConsolePlayer].mo);
```
Wrap to skip movement when a popup is open:
```cpp
    if (!OWolf3D_STAR_ShouldBlockInput())
        ControlMovement(players[ConsolePlayer].mo);
```

---

## 8. `g_wolf/wolf_sbar.cpp` — HUD / GUI overlays

### 8a. Add include

```cpp
#include "owolf3d_ogengine_integration.h"
```

### 8b. `WolfStatusBar::DrawStatusBar()` — draw OASIS HUD at the end

Find the end of `void WolfStatusBar::DrawStatusBar()`:
```cpp
    DrawItems ();
}
```
Add:
```cpp
    DrawItems ();
    OWolf3D_STAR_DrawHUDStatus();    // ← ADD — beamed-in, XP, version, toasts
    OWolf3D_STAR_DrawPopupOverlay(); // ← ADD — inventory / quest popup
}
```

### 8c. `WolfStatusBar::UpdateFace()` — avatar portrait when beamed in

Find the face-drawing decision near the top of `void WolfStatusBar::UpdateFace(int damage)`:
```cpp
void WolfStatusBar::UpdateFace (int damage)
{
```
Add at the very start of the function body:
```cpp
void WolfStatusBar::UpdateFace (int damage)
{
    if (OWolf3D_STAR_ShouldUseAvatarFace()) {   // ← ADD
        // TODO: draw avatar portrait using TexMan("OASFACE")
        return;
    }
```

---

## 9. Verify the build

After applying all edits, run the build script:

```bat
cd "C:\Source\OASIS2\OASIS Omniverse\OWolf3D"
BUILD_OWOLF3D.bat
```

Then launch with `RUN_OWOLF3D.bat`, and in the in-game console (`~` key):
```
star version
star beamin yourUsername yourPassword
star inventory
```

In-game press **I** for the OASIS Inventory popup, **Q** for Quests.
