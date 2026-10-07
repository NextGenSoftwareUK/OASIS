/**
 * OExhumed - OASIS STAR API Integration Header
 *
 * Hook these functions into the Raze source tree (Exhumed/PowerSlave backend)
 * to bring Exhumed into the OASIS Omniverse.  Exhumed runs inside the same
 * Raze / GZDoom C++ infrastructure as OShadowWarrior and OBlood — hook
 * patterns, CCMD system, and screen API are identical.
 *
 * Minimum hook sites in the Raze source:
 *   src/common/engine/            startup / shutdown
 *                                        → OExhumed_STAR_Init / Cleanup
 *   source/exhumed/src/game.cpp   GameTicker() once per tic
 *                                        → OExhumed_STAR_Tick
 *   source/exhumed/src/items.cpp  item touch path
 *                                        → OExhumed_STAR_OnItemPickup
 *   source/exhumed/src/enemy.cpp  enemy death path
 *                                        → OExhumed_STAR_OnEnemyKilled
 *   source/exhumed/src/view.cpp   DrawHud() per-frame draw
 *                                        → OExhumed_STAR_DrawHUDStatus
 *   source/exhumed/src/input.cpp  key event handler
 *                                        → OExhumed_STAR_HandleKey
 *
 * Base engine: Raze  https://github.com/ZDoom/Raze  (GPL-3.0)
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void OExhumed_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);
void OExhumed_STAR_Cleanup(void);
void OExhumed_STAR_Tick(void);
void OExhumed_STAR_OnItemPickup(int item_type, const char* item_name);
void OExhumed_STAR_OnEnemyKilled(int enemy_type, const char* enemy_name, const char* killer);
void OExhumed_STAR_DrawHUDStatus(int screen_w, int screen_h);
int  OExhumed_STAR_HandleKey(int key_code);
int  OExhumed_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
