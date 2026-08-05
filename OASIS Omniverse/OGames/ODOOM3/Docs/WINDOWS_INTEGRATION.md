# ODOOM3 — Windows Integration Guide (Manual diff)

These are the exact changes applied to `C:\Source\ODOOM3\neo\` by the build script.
If you prefer to apply them manually rather than running `COPY_TO_DHEWM3_AND_BUILD.ps1`,
follow the diffs below.

---

## 1. `neo/CMakeLists.txt`

### Add integration source to `src_game`

Find the line:
```cmake
	game/physics/Push.cpp
)
```
Change to:
```cmake
	game/physics/Push.cpp
	game/d3doom3_ogengine_integration.cpp
)
```

### Add compile definition and star_api link to `base` target

Find the block (inside `if(BASE AND NOT HARDLINK_GAME)`):
```cmake
	target_include_directories(base PRIVATE "${CMAKE_SOURCE_DIR}/game")
	set_target_properties(base PROPERTIES LINK_FLAGS "${ldflags}")
	set_target_properties(base PROPERTIES INSTALL_NAME_DIR "@executable_path")
	if (AROS)
		target_link_libraries(base idlib dynmod)
	else()
		target_link_libraries(base idlib)
	endif()
```
Change to:
```cmake
	target_include_directories(base PRIVATE "${CMAKE_SOURCE_DIR}/game")
	target_compile_definitions(base PRIVATE OASIS_STAR_SYNC_IN_CLIENT=1)
	set_target_properties(base PROPERTIES LINK_FLAGS "${ldflags}")
	set_target_properties(base PROPERTIES INSTALL_NAME_DIR "@executable_path")
	if (AROS)
		target_link_libraries(base idlib dynmod)
	else()
		target_link_libraries(base idlib)
		if(MSVC)
			target_link_libraries(base "${CMAKE_SOURCE_DIR}/game/ogengine.lib")
		endif()
	endif()
```

---

## 2. `neo/game/Game_local.cpp`

### Add include after existing includes

After the last `#include` line near the top:
```cpp
#include "framework/Licensee.h" // DG: for ID__DATE__

#include "Game_local.h"
```
Add:
```cpp
#include "d3doom3_ogengine_integration.h"
```

### Init hook

At the end of `idGameLocal::Init()`, before the closing `}`:
```cpp
	common->GetAdditionalFunction(idCommon::FT_UpdateDebugger,(idCommon::FunctionPointer*) &updateDebuggerFnPtr,NULL);
}
```
Change to:
```cpp
	common->GetAdditionalFunction(idCommon::FT_UpdateDebugger,(idCommon::FunctionPointer*) &updateDebuggerFnPtr,NULL);
	D3Doom3_STAR_Init();
}
```

### Shutdown hook

In `idGameLocal::Shutdown()`, after the `!common` guard:
```cpp
	if ( !common ) {
		return;
	}

	Printf( "----- Game Shutdown -----\n" );
```
Change to:
```cpp
	if ( !common ) {
		return;
	}
	D3Doom3_STAR_Cleanup();

	Printf( "----- Game Shutdown -----\n" );
```

### RunFrame hook

In `idGameLocal::RunFrame()`, after `player = GetLocalPlayer();`:
```cpp
	player = GetLocalPlayer();
```
Change to:
```cpp
	player = GetLocalPlayer();
	D3Doom3_STAR_Tick();
```

### RequirementMet hook

In `idGameLocal::RequirementMet()`, change the `else` branch:
```cpp
			} else {
				return false;
			}
```
Change to:
```cpp
			} else {
				if ( D3Doom3_STAR_CheckDoorAccess( requires.c_str() ) ) { return true; }
				return false;
			}
```

---

## 3. `neo/game/Player.cpp`

### Add include after existing includes

After the last `#include` line near the top, add:
```cpp
#include "d3doom3_ogengine_integration.h"
```

### GiveInventoryItem hook

In `idPlayer::GiveInventoryItem(idDict *item)`, after `inventory.items.Append(...)`:
```cpp
	inventory.items.Append( new idDict( *item ) );
```
Change to:
```cpp
	inventory.items.Append( new idDict( *item ) );
	D3Doom3_STAR_OnItemPickup(
		item->GetString( "inv_name" ),
		item->GetString( "classname" ),
		item->GetBool( "inv_carry" ) ? 1 : 0 );
```

---

## 4. `neo/game/ai/AI.cpp`

### Add include after existing includes

After `#include "ai/AI.h"`, add:
```cpp
#include "d3doom3_ogengine_integration.h"
```

### Killed hook

In `idAI::Killed()`, after the `AI_DEAD` early-return guard:
```cpp
	if ( AI_DEAD ) {
		AI_PAIN = true;
		AI_DAMAGE = true;
		return;
	}
```
Change to:
```cpp
	if ( AI_DEAD ) {
		AI_PAIN = true;
		AI_DAMAGE = true;
		return;
	}
	D3Doom3_STAR_OnMonsterKilled( GetEntityDefName(), 0 );
```

---

## GUI / HUD hooks — Inventory popup, Quest popup, HUD overlays

The following hooks add the full OASIS GUI to ODOOM3 (dhewm3), matching the feature set of ODOOM and OQuake:
inventory popup (I), quest popup (Q), beamed-in label, XP, version string, toasts, avatar face.

### Game_local.cpp — Draw() — status overlays

In `idGameLocal::Draw()` (or equivalent HUD render path), after the HUD GUI:

```cpp
D3Doom3_STAR_DrawHUDStatus( ::renderSystem );    // beamed-in, XP, version, toasts
D3Doom3_STAR_DrawPopupOverlay( ::renderSystem );  // inventory / quest full-screen popup
```

### Key forwarding

In dhewm3's key processing (`framework/UsercmdGen.cpp` or `sys/win32/win_input.cpp`):

```cpp
D3Doom3_STAR_HandleKey( key, down );
// 'i' = inventory popup, 'q' = quest popup, ESC = close
```

### Block input when popup open

In `idPlayer::Think()` or usercmd processing:

```cpp
if ( D3Doom3_STAR_ShouldBlockInput() ) {
    usercmd.buttons = 0;
    usercmd.forwardmove = usercmd.rightmove = usercmd.upmove = 0;
}
```

### Avatar face

Where the player portrait / flashlight face is drawn in the HUD:

```cpp
const char* face_mat = D3Doom3_STAR_ShouldUseAvatarFace()
    ? "textures/oasis/avatar_face"
    : "textures/hud/player_face";
```

---

**Cross-game partners:** ODOOM, OQuake, ODOOM3-BFG, ODuke3D, ODuke3D-RT, OWolf3D — seven OASIS Omniverse games total.
Press **I** in-game for the OASIS Inventory popup, **Q** for Quests.
