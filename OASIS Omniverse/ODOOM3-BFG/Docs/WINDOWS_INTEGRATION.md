# ODOOM3-BFG: Engine Hook Guide (RBDOOM-3-BFG on Windows)

Step-by-step diff guide for applying all engine hook points.
Run `Scripts\COPY_TO_RBDOOM3_AND_BUILD.ps1` **before** editing engine files
so the integration headers are already in `neo\d3xp\`.

All file paths are relative to `C:\Source\ODOOM3-BFG\neo\d3xp\`.

**Cross-game partners:** ODOOM, OQuake, ODOOM3, **ODuke3D** (Duke Nukem 3D, EDuke32 fork), and **ODuke3D-RT** (Duke Nukem 3D ray-traced) — six OASIS Omniverse games total.

---

## 1. Game_local.cpp — Init, Shutdown, RunFrame, RequirementMet

### 1a. Include the integration header

At the top of `Game_local.cpp`, after the other game includes, add:

```cpp
#include "d3doom_star_integration.h"
```

### 1b. idGameLocal::Init() — call D3Doom_STAR_Init

Find the end of `idGameLocal::Init()` (around line 380, before the closing brace).
Add the call as the **last** statement in the function:

```cpp
    // ... existing Init code ...

    D3Doom_STAR_Init();   // ← ADD THIS
}
```

### 1c. idGameLocal::Shutdown() — call D3Doom_STAR_Cleanup

Find the **start** of `idGameLocal::Shutdown()` (around line 393), add as the
**first** statement:

```cpp
void idGameLocal::Shutdown() {
    D3Doom_STAR_Cleanup();   // ← ADD THIS

    // ... existing Shutdown code ...
```

### 1d. idGameLocal::RunFrame() — call D3Doom_STAR_Tick

Find `idGameLocal::RunFrame()` (around line 2631). Add near the top of the
function, after the frame counter increment:

```cpp
    D3Doom_STAR_Tick();   // ← ADD (after gameLocal.framenum++ or similar)
```

### 1e. idGameLocal::RequirementMet() — STAR cross-game key fallback

Find `idGameLocal::RequirementMet()` (around line 4455). Look for the final
`else { return false; }` block at the end of the function. Change it from:

```cpp
    } else {
        return false;
    }
```

to:

```cpp
    } else {
        if ( D3Doom_STAR_CheckDoorAccess( requires.c_str() ) ) { return true; }
        return false;
    }
```

---

## 2. Player.cpp — Item pickup hook

### 2a. Include the integration header

Add at the top of `Player.cpp` (after the existing includes):

```cpp
#include "d3doom_star_integration.h"
```

### 2b. idPlayer::GiveInventoryItem() — report to STAR

Find `idPlayer::GiveInventoryItem()` (around line 4671). After the line
`inventory.items.Append( item );` (or equivalent — where the engine commits
the item to the player's inventory list), add:

```cpp
    if ( giveFlags & ITEM_GIVE_UPDATE_STATE ) {
        D3Doom_STAR_OnItemPickup(
            item->GetString( "inv_name" ),
            item->GetString( "classname" ),
            item->GetBool( "inv_carry" ) ? 1 : 0 );
    }
```

---

## 3. ai/AI.cpp — Monster kill hook

### 3a. Include the integration header

Add at the top of `ai/AI.cpp`:

```cpp
#include "d3doom_star_integration.h"
```

### 3b. idAI::Killed() — report kill to STAR

Find `idAI::Killed()` (around line 3877). Add near the **top** of the function
(after the opening brace, before the existing logic):

```cpp
    D3Doom_STAR_OnMonsterKilled( GetEntityDefName(), 0 );
```

---

## 4. CMakeLists.txt — Add sources and link star_api

Open `neo/CMakeLists.txt`. Find the `GAMED3XP_SOURCES` list (around line 694).
Add the integration source file:

```cmake
set( GAMED3XP_SOURCES
    # ... existing entries ...
    d3xp/d3doom_star_integration.cpp   # ← ADD
)
```

Also add the header to `GAMED3XP_INCLUDES` (or wherever other d3xp headers are listed):

```cmake
set( GAMED3XP_INCLUDES
    # ... existing entries ...
    d3xp/d3doom_star_integration.h     # ← ADD
)
```

Add the OGLib include path to the d3game target:

```cmake
target_include_directories( d3game PRIVATE
    # ... existing ...
    ${CMAKE_CURRENT_SOURCE_DIR}/d3xp   # ← ADD (gives access to OGLib/ subdir)
)
```

Link the STAR API import library:

```cmake
target_link_libraries( d3game PRIVATE
    # ... existing ...
    ${CMAKE_CURRENT_SOURCE_DIR}/d3xp/star_api.lib   # ← ADD (Windows)
)
```

Add the compile definition so star_sync is resolved from the DLL:

```cmake
target_compile_definitions( d3game PRIVATE
    OASIS_STAR_SYNC_IN_CLIENT=1
)
```

---

## 5. HUD / GUI hooks — Inventory popup, Quest popup, HUD overlays

The following hooks add the full OASIS GUI to ODOOM3-BFG, matching the feature set of ODOOM and OQuake:
inventory popup (I), quest popup (Q), beamed-in face, XP display, version display, toasts.

### 5a. idGameLocal.cpp — Draw() — status overlays

Find or create `idGameLocal::Draw()` (the function that draws the in-game HUD).
Add after the HUD GUI has been rendered:

```cpp
#include "d3doom_star_integration.h"

void idGameLocal::Draw( int clientNum ) {
    // ... existing HUD draw code ...

    D3Doom_STAR_DrawHUDStatus( ::renderSystem );   // beamed-in, XP, version, toasts
    D3Doom_STAR_DrawPopupOverlay( ::renderSystem ); // inventory / quest popup
}
```

### 5b. idKeyInput.cpp / idUsercmdGen — Key forwarding

In the key processing path (before normal game key bindings), add:

```cpp
#include "d3doom_star_integration.h"

// In key-press handler:
D3Doom_STAR_HandleKey( key, down );

// 'i' opens inventory popup, 'q' opens quest popup, ESC closes both.
```

### 5c. idPlayer::Think() — Block input when popup open

Near the start of movement / weapon fire processing:

```cpp
if ( D3Doom_STAR_ShouldBlockInput() ) {
    usercmd.buttons = 0;
    usercmd.forwardmove = usercmd.rightmove = usercmd.upmove = 0;
}
```

### 5d. HUD face — Avatar image when beamed in

In the HUD GUI script (`guis/hud.gui`) or in the C++ draw path where the player
portrait is drawn:

```cpp
// C++ HUD draw:
const char* face_material = D3Doom_STAR_ShouldUseAvatarFace()
    ? "textures/oasis/avatar_face"   // OASIS avatar material
    : "textures/hud/player_face";    // normal Doom 3 face
// draw face_material at the HUD face position
```

---

## 6. Verify the build

After applying all edits, run the build script:

```bat
cd "C:\Source\OASIS2\OASIS Omniverse\ODOOM3-BFG"
BUILD_ODOOM3BFG.bat
```

If CMake was already configured, only the changed sources will recompile.
The build produces `build-vs2019-win64\Release\d3game.dll`.

Launch with `RUN_ODOOM3BFG.bat`, then in the game console (CTRL+ALT+~):
```
star version
star beamin yourUsername yourPassword
star inventory
```

In-game press **I** for the OASIS Inventory popup, **Q** for quests.
