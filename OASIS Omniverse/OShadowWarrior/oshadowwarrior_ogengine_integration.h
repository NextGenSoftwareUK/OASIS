/**
 * OShadowWarrior - OASIS STAR API Integration Header
 *
 * Hook these functions into the Raze source tree to bring Shadow Warrior
 * Classic into the OASIS Omniverse.
 *
 * Raze (https://github.com/ZDoom/Raze) is the ZDoom-team's Build engine
 * reimplementation — Shadow Warrior Classic runs inside the same GZDoom
 * infrastructure as OHeretic and OHexen, so the hook patterns, CCMD system,
 * input API, and screen draw API are identical.
 *
 * Minimum hook sites in the Raze source:
 *   src/common/engine/  startup / shutdown
 *                              → OShadowWarrior_STAR_Init / Cleanup
 *   source/sw/src/game.cpp    GameTicker() once per tic
 *                              → OShadowWarrior_STAR_Tick
 *   source/sw/src/actor.cpp   DoPickupItem() / CheckPickupSprite()
 *                              → OShadowWarrior_STAR_OnItemPickup
 *   source/sw/src/actor.cpp   KillEnemy() death path
 *                              → OShadowWarrior_STAR_OnEnemyKilled
 *   source/sw/src/draw.cpp    DrawHud() per-frame draw
 *                              → OShadowWarrior_STAR_DrawHUDStatus
 *   source/sw/src/input.cpp   key event handler
 *                              → OShadowWarrior_STAR_HandleKey
 *
 * Editor: Mapster32 — the shared OASIS Mapster32 companion tool
 *         (oasis_m32_tool) is unchanged and covers portals, quests and assets.
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/** Call at game startup (after Raze engine initialisation). */
void OShadowWarrior_STAR_Init(const char* star_api_base_url,
                               const char* oasis_json_path);

/** Call at game shutdown. */
void OShadowWarrior_STAR_Cleanup(void);

/**
 * Call once per game tic from GameTicker().
 * Flushes queued STAR events and fires pending callbacks on the main thread.
 */
void OShadowWarrior_STAR_Tick(void);

/**
 * Call from DoPickupItem() when Lo Wang picks up an item.
 * sprite_type — Raze / Shadow Warrior sprite type constant
 * item_name   — human-readable name for STAR, e.g. "medkit", "uzi"
 */
void OShadowWarrior_STAR_OnItemPickup(int sprite_type, const char* item_name);

/**
 * Call from KillEnemy() when an enemy dies.
 * enemy_type  — Raze / Shadow Warrior enemy type constant
 * enemy_name  — human-readable name, e.g. "Ninja", "Coolie"
 * killer      — player name / avatar username
 */
void OShadowWarrior_STAR_OnEnemyKilled(int enemy_type, const char* enemy_name,
                                        const char* killer);

/**
 * Call from DrawHud() each frame to render OASIS HUD overlays.
 * screen_w / screen_h — current render resolution.
 */
void OShadowWarrior_STAR_DrawHUDStatus(int screen_w, int screen_h);

/**
 * Call from the input key handler.  Returns 1 if OASIS consumed the key.
 * key_code — GZDoom KEY_* constant.
 */
int OShadowWarrior_STAR_HandleKey(int key_code);

/** Returns 1 when OASIS is fully logged in and ready. */
int OShadowWarrior_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
