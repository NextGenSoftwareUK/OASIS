/**
 * OBlood - OASIS STAR API Integration Header
 *
 * Hook these functions into the Raze source tree (Blood backend) to bring
 * Blood into the OASIS Omniverse.  Blood runs inside the same Raze / GZDoom
 * C++ infrastructure as OShadowWarrior — hook patterns, CCMD system, input
 * API, and screen draw API are identical.
 *
 * Minimum hook sites in the Raze source:
 *   src/common/engine/  startup / shutdown
 *                              → OBlood_STAR_Init / Cleanup
 *   source/blood/src/game.cpp  GameTicker() once per tic
 *                              → OBlood_STAR_Tick
 *   source/blood/src/actor.cpp item touch / pickup path
 *                              → OBlood_STAR_OnItemPickup
 *   source/blood/src/actor.cpp enemy death path
 *                              → OBlood_STAR_OnEnemyKilled
 *   source/blood/src/view.cpp  DrawHud() per-frame draw
 *                              → OBlood_STAR_DrawHUDStatus
 *   source/blood/src/input.cpp key event handler
 *                              → OBlood_STAR_HandleKey
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

void OBlood_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);
void OBlood_STAR_Cleanup(void);
void OBlood_STAR_Tick(void);
void OBlood_STAR_OnItemPickup(int item_type, const char* item_name);
void OBlood_STAR_OnEnemyKilled(int enemy_type, const char* enemy_name, const char* killer);
void OBlood_STAR_DrawHUDStatus(int screen_w, int screen_h);
int  OBlood_STAR_HandleKey(int key_code);
int  OBlood_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
