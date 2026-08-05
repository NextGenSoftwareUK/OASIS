/**
 * OMorrowind - OASIS STAR API Integration Header
 *
 * Hook these functions into the OpenMW source tree to bring Morrowind into
 * the OASIS Omniverse.  OpenMW is a C++ reimplementation of Morrowind
 * (GPL-3.0) and provides stable hook sites for item management, combat, and
 * UI rendering.
 *
 * Minimum hook sites in the OpenMW source:
 *   apps/openmw/mwworld/inventorystore.cpp
 *       MWWorld::InventoryStore::add()        → OMorrowind_STAR_OnItemPickup
 *
 *   apps/openmw/mwmechanics/combat.cpp
 *       MWMechanics::projectileHit / npcHit   → OMorrowind_STAR_OnActorKilled
 *
 *   apps/openmw/engine.cpp
 *       EngineWrapper::frame()  (main loop)   → OMorrowind_STAR_Tick
 *       EngineWrapper::go()     (startup)      → OMorrowind_STAR_Init
 *       EngineWrapper::~Engine  (shutdown)     → OMorrowind_STAR_Cleanup
 *
 *   apps/openmw/mwgui/hud.cpp
 *       HUD::onFrame()                        → OMorrowind_STAR_DrawHUDStatus
 *
 *   apps/openmw/mwinput/inputmanagerimp.cpp
 *       InputManager::keyPressed()            → OMorrowind_STAR_HandleKey
 *
 *   apps/openmw/mwworld/worldimp.cpp
 *       World::teleportToRandomInnRoom()      → (portal hook — OASIS portal
 *       or a MWSCRIPT/Lua script that calls the STAR API via ogengine_* C ABI)
 *
 * OpenMW also exposes a stable Lua scripting API (0.47+).  An OASIS.omwaddon
 * companion that calls the C ABI via FFI is planned as a follow-up deliverable.
 *
 * OASIS item type range : 6000-6899  (reserved in STAR)
 * Portal script ID      : "oasis_portal_enter" (MWSCRIPT id)
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Call at engine startup (EngineWrapper::go).
 * star_api_base_url  — e.g. "http://localhost:5000"
 * oasis_json_path    — path to oasisstar.json for avatar persistence
 */
void OMorrowind_STAR_Init(const char* star_api_base_url,
                           const char* oasis_json_path);

/** Call at engine shutdown. */
void OMorrowind_STAR_Cleanup(void);

/**
 * Call once per frame from EngineWrapper::frame().
 * Flushes queued STAR events and fires pending callbacks on the main thread.
 */
void OMorrowind_STAR_Tick(void);

/**
 * Call from MWWorld::InventoryStore::add() when the player acquires an item.
 * item_id   — OpenMW / Morrowind item ID string, e.g. "misc_skeleton_key"
 * item_name — display name, e.g. "Skeleton Key"
 * quantity  — number of items added (usually 1)
 */
void OMorrowind_STAR_OnItemPickup(const char* item_id, const char* item_name,
                                   int quantity);

/**
 * Call from the combat system when an actor (NPC or creature) dies.
 * actor_id   — OpenMW actor ref ID, e.g. "scamp"
 * actor_name — display name, e.g. "Scamp"
 * killer     — player name / avatar username ("" if environment kill)
 */
void OMorrowind_STAR_OnActorKilled(const char* actor_id, const char* actor_name,
                                    const char* killer);

/**
 * Call from HUD::onFrame() to render OASIS overlay widgets.
 * screen_w / screen_h — current render resolution.
 */
void OMorrowind_STAR_DrawHUDStatus(int screen_w, int screen_h);

/**
 * Call from InputManager::keyPressed().  Returns 1 if OASIS consumed the event.
 * sdl_scancode — SDL_Scancode value from the key event.
 */
int OMorrowind_STAR_HandleKey(int sdl_scancode);

/** Returns 1 when OASIS is fully logged in and ready. */
int OMorrowind_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
