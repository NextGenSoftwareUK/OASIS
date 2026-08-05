/**
 * OShadowWarrior - OASIS STAR API Integration Header
 *
 * Hook these functions into the VoidSW source tree (based on JFShadowWarrior /
 * Shadow Warrior Classic source release) to bring Shadow Warrior into the
 * OASIS Omniverse.
 *
 * VoidSW is a Build engine game (like EDuke32) so the integration closely
 * mirrors oduke3d_ogengine_integration.h.  The editor is Mapster32 — the
 * OASIS Mapster32 companion tool (oasis_m32_tool) is shared with ODuke3D.
 *
 * Minimum hook sites in the VoidSW source (src/):
 *   sw.c / GameMain()         → OShadowWarrior_STAR_Init / Cleanup
 *   game.c / DoGamePlay()     → OShadowWarrior_STAR_Tick  (once per tic)
 *   actor.c / DoPickupItem()  → OShadowWarrior_STAR_OnItemPickup
 *   actor.c / KillEnemy()     → OShadowWarrior_STAR_OnEnemyKilled
 *   draw.c / DrawStatus()     → OShadowWarrior_STAR_DrawHUDStatus
 *   input.c / gameInput()     → OShadowWarrior_STAR_HandleKey
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/** Call at game startup (after engine initialisation). */
void OShadowWarrior_STAR_Init(const char* star_api_base_url,
                               const char* oasis_json_path);

/** Call at game shutdown. */
void OShadowWarrior_STAR_Cleanup(void);

/**
 * Call once per game tic.
 * Flushes queued STAR events and fires pending callbacks on the main thread.
 */
void OShadowWarrior_STAR_Tick(void);

/**
 * Call from DoPickupItem() when Lo Wang picks up a weapon, ammo, or health item.
 * item_id   — VoidSW sprite picnum constant (ITEM_ARMOR, ITEM_HEART, etc.)
 * item_name — human-readable name for STAR, e.g. "medkit", "heart", "uzi"
 */
void OShadowWarrior_STAR_OnItemPickup(int item_id, const char* item_name);

/**
 * Call from KillEnemy() when an enemy dies.
 * enemy_id   — VoidSW enemy type (ENEMYTYPE_COOLIE, ENEMYTYPE_NINJA, etc.)
 * enemy_name — human-readable name for XP table lookup, e.g. "Ninja", "Coolie"
 * killer     — player name / avatar username
 */
void OShadowWarrior_STAR_OnEnemyKilled(int enemy_id, const char* enemy_name,
                                        const char* killer);

/**
 * Call from DrawStatus() each frame to render OASIS HUD overlays.
 * screen_w / screen_h — current Build engine render resolution.
 */
void OShadowWarrior_STAR_DrawHUDStatus(int screen_w, int screen_h);

/**
 * Call from gameInput() key handler.  Returns 1 if OASIS consumed the key.
 * scan_code — Build engine / SDL scan code.
 */
int OShadowWarrior_STAR_HandleKey(int scan_code);

/** Returns 1 when OASIS is fully logged in and ready. */
int OShadowWarrior_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
