/**
 * OHexen - OASIS STAR API Integration Header
 *
 * Hook these functions into the UZDoom source tree to bring Hexen into the
 * OASIS Omniverse.  OHexen uses UZDoom (the GZDoom fork that powers ODOOM /
 * OHeretic) so the integration pattern mirrors uzdoom_ogengine_integration.h.
 *
 * Hexen-specific considerations:
 *   - Three player classes: Fighter, Cleric, Mage — reported in STAR as the
 *     avatar's current "class" attribute.
 *   - Hub-based level progression — pass hub_tag to the Init call so STAR
 *     can associate quests/portals with the correct hub cluster.
 *   - Puzzle items (KeyPuzzSkull, KeyPuzzFire, …) are treated as key-items
 *     for cross-game inventory rather than physical keys.
 *
 * Minimum hook sites in the GZDoom source:
 *   src/gamedata/a_keys.cpp  AInventory::CallTryPickup path
 *                              → OHexen_STAR_OnItemPickup
 *   src/playsim/p_spec.cpp   P_ActivateLine locked-door check
 *                              → OHexen_STAR_CheckDoorAccess
 *   src/playsim/actor.cpp    AActor::Die / P_KillMobj
 *                              → OHexen_STAR_OnMonsterKilled
 *   src/g_game.cpp           G_Ticker, once per tic
 *                              → OHexen_STAR_Tick
 *   src/d_main.cpp           D_Display, once per frame
 *                              → OHexen_STAR_DrawHUDStatus
 *   src/g_game.cpp           startup / G_InitNew, shutdown
 *                              → OHexen_STAR_Init / OHexen_STAR_Cleanup
 *   src/g_input.cpp          key event handler
 *                              → OHexen_STAR_HandleKey
 *   src/playsim/p_user.cpp   player class selection / P_SetClass
 *                              → OHexen_STAR_OnClassSelected
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/** Call at game startup (after GZDoom DLL is loaded). */
void OHexen_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);

/** Call at game shutdown. */
void OHexen_STAR_Cleanup(void);

/**
 * Call once per game tic from G_Ticker.
 * Flushes queued STAR events and fires pending callbacks.
 */
void OHexen_STAR_Tick(void);

/**
 * Call from AInventory::CallTryPickup when an actor picks up an item.
 * actor_class  — GZDoom internal class name, e.g. "ArtiPork", "KeyAxe", "ArtiDisk"
 * picker_name  — player name / avatar username
 */
void OHexen_STAR_OnItemPickup(const char* actor_class, const char* picker_name);

/**
 * Call from the locked-door / Use-puzzle path in P_ActivateLine.
 * Returns 1 if cross-game inventory grants access, 0 otherwise.
 * key_class — GZDoom key class name, e.g. "KeyAxe", "KeyPuzzSkull"
 */
int OHexen_STAR_CheckDoorAccess(const char* key_class, const char* player_name);

/**
 * Call from AActor::Die when a monster is killed.
 * monster_class — GZDoom class name, e.g. "Centaur", "Ettin", "Heresiarch"
 * killer_name   — player name / avatar username
 */
void OHexen_STAR_OnMonsterKilled(const char* monster_class, const char* killer_name);

/**
 * Call from D_Display to draw OASIS HUD overlays.
 * screen_w / screen_h — current render resolution.
 */
void OHexen_STAR_DrawHUDStatus(int screen_w, int screen_h);

/**
 * Call from the key event handler.  Returns 1 if OASIS consumed the key.
 * key_code — GZDoom key code (matches KEY_* constants).
 */
int OHexen_STAR_HandleKey(int key_code);

/**
 * Call from P_SetClass when the player selects their class at game start.
 * class_name — "fighter", "cleric", or "mage"  (lower-case as GZDoom reports)
 * player_name — avatar username
 */
void OHexen_STAR_OnClassSelected(const char* class_name, const char* player_name);

/** Returns 1 when OASIS is fully logged in and ready. */
int OHexen_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
