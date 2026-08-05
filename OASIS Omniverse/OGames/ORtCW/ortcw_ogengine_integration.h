/**
 * ORtCW - OASIS STAR API Integration Header
 *
 * Hook these functions into the iortcw source tree to bring Return to Castle
 * Wolfenstein into the OASIS Omniverse.  iortcw is a Q3-engine derivative —
 * hook patterns closely mirror OQuake3 (trap_* syscalls, G_RunFrame, etc.).
 *
 * Minimum hook sites in the iortcw source:
 *   SP_src/game/g_main.c    G_RunFrame() once per server frame
 *                                   → ORtCW_STAR_Tick
 *   SP_src/game/g_items.c   Touch_Item() item pickup
 *                                   → ORtCW_STAR_OnItemPickup
 *   SP_src/game/g_combat.c  entity death path (player_die / G_Damage)
 *                                   → ORtCW_STAR_OnEnemyKilled
 *   SP_src/cgame/cg_draw.c  CG_DrawActiveFrame() HUD draw
 *                                   → ORtCW_STAR_DrawHUDStatus
 *   SP_src/game/g_main.c    G_InitGame / G_ShutdownGame
 *                                   → ORtCW_STAR_Init / Cleanup
 *   SP_src/cgame/cg_event.c key / event handler
 *                                   → ORtCW_STAR_HandleKey
 *
 * Wolfenstein enemy set: Wehrmacht soldiers, SS Elite Guard, Uber-Soldat,
 * Black Guard, Loper, and boss Heinrich I (Bramburg Dam, final).
 *
 * Base engine: iortcw  https://github.com/iortcw/iortcw  (GPL-3.0)
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void ORtCW_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);
void ORtCW_STAR_Cleanup(void);
void ORtCW_STAR_Tick(void);
void ORtCW_STAR_OnItemPickup(const char* classname, const char* item_name);
void ORtCW_STAR_OnEnemyKilled(const char* enemy_class, const char* enemy_name,
                               const char* killer);
void ORtCW_STAR_DrawHUDStatus(int screen_w, int screen_h);
int  ORtCW_STAR_HandleKey(int key_code);
int  ORtCW_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
