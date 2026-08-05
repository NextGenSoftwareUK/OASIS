/**
 * OStrife - OASIS STAR API Integration Header
 *
 * Hook these functions into the UZDoom source tree to bring Strife into the
 * OASIS Omniverse.  GZDoom/UZDoom support Strife natively — hook patterns
 * are identical to OHeretic and OHexen.
 *
 * Strife-specific notes:
 *   - Gold coins are the primary currency — reported to STAR as inventory items
 *   - Quest items (quest_item type) are flagged as key-items for cross-game carry
 *   - The Sigil of the One God (the ultimate weapon) is treated as a unique
 *     cross-game trophy item
 *   - Dialog/reputation system: entity type "strife_ally" / "strife_enemy"
 *     can be exposed as STAR relationship events (future phase)
 *
 * Minimum hook sites in the UZDoom/GZDoom source:
 *   src/gamedata/a_keys.cpp      AInventory::CallTryPickup
 *                                        → OStrife_STAR_OnItemPickup
 *   src/playsim/actor.cpp        AActor::Die
 *                                        → OStrife_STAR_OnEnemyKilled
 *   src/g_game.cpp               G_Ticker once per tic
 *                                        → OStrife_STAR_Tick
 *   src/rendering/                per-frame HUD draw
 *                                        → OStrife_STAR_DrawHUDStatus
 *   startup / shutdown                   → OStrife_STAR_Init / Cleanup
 *   key event handler                    → OStrife_STAR_HandleKey
 *
 * Base engine: UZDoom (GZDoom fork)  https://github.com/UZDoom/UZDoom  (GPL-3.0)
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void OStrife_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);
void OStrife_STAR_Cleanup(void);
void OStrife_STAR_Tick(void);
void OStrife_STAR_OnItemPickup(const char* class_name, const char* item_name);
void OStrife_STAR_OnEnemyKilled(const char* actor_class, const char* killer_name);
void OStrife_STAR_DrawHUDStatus(int screen_w, int screen_h);
int  OStrife_STAR_HandleKey(int key_code);
int  OStrife_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
