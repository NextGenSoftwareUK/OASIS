/**
 * OHeretic - OASIS STAR API Integration Header
 *
 * Hook these functions into the UZDoom source tree to bring Heretic into the
 * OASIS Omniverse.  OHeretic uses UZDoom (the GZDoom fork that powers ODOOM)
 * so the integration pattern mirrors uzdoom_ogengine_integration.h exactly.
 *
 * Minimum hook sites in the UZDoom/GZDoom source:
 *   src/gamedata/a_keys.cpp  AInventory::CallTryPickup path
 *                              → OHeretic_STAR_OnItemPickup
 *   src/playsim/p_spec.cpp   P_ActivateLine locked-door check
 *                              → OHeretic_STAR_CheckDoorAccess
 *   src/playsim/actor.cpp    AActor::Die / P_KillMobj
 *                              → OHeretic_STAR_OnMonsterKilled
 *   src/g_game.cpp           G_Ticker, once per tic
 *                              → OHeretic_STAR_Tick
 *   src/d_main.cpp           D_Display, once per frame
 *                              → OHeretic_STAR_DrawHUDStatus
 *   src/g_game.cpp           startup / G_InitNew, shutdown
 *                              → OHeretic_STAR_Init / OHeretic_STAR_Cleanup
 *   src/g_input.cpp          key event handler
 *                              → OHeretic_STAR_HandleKey
 *
 * OASIS thing-type range : 6000-6899  (shared with ODOOM, OQuake, etc.)
 * Portal thing type      : 5900       (shared across all OASIS Omniverse games)
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/** Call at game startup (after GZDoom DLL is loaded). */
void OHeretic_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);

/** Call at game shutdown. */
void OHeretic_STAR_Cleanup(void);

/**
 * Call once per game tic from G_Ticker.
 * Flushes queued STAR events and fires pending callbacks.
 */
void OHeretic_STAR_Tick(void);

/**
 * Call from AInventory::CallTryPickup when an actor picks up an item.
 * actor_class  — GZDoom internal class name, e.g. "ArtiTome", "KeyGreen"
 * picker_name  — player name / avatar username
 */
void OHeretic_STAR_OnItemPickup(const char* actor_class, const char* picker_name);

/**
 * Call from the locked-door path in P_ActivateLine.
 * Returns 1 if cross-game inventory grants access, 0 otherwise.
 * key_class — GZDoom key class name, e.g. "KeyGreen", "KeyYellow", "KeyBlue"
 */
int OHeretic_STAR_CheckDoorAccess(const char* key_class, const char* player_name);

/**
 * Call from AActor::Die when a monster is killed.
 * monster_class — GZDoom class name, e.g. "Gargoyle", "Golem", "Maulotaur"
 * killer_name   — player name / avatar username
 */
void OHeretic_STAR_OnMonsterKilled(const char* monster_class, const char* killer_name);

/**
 * Call from D_Display to draw OASIS HUD overlays (inventory, XP, toast messages).
 * screen_w / screen_h — current render resolution.
 */
void OHeretic_STAR_DrawHUDStatus(int screen_w, int screen_h);

/**
 * Call from the key event handler.  Returns 1 if OASIS consumed the key.
 * key_code — GZDoom key code (matches KEY_* constants).
 */
int OHeretic_STAR_HandleKey(int key_code);

/** Returns 1 when OASIS is fully logged in and ready. */
int OHeretic_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
