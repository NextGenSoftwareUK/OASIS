/**
 * ODoom64 - OASIS STAR API Integration Header
 *
 * Hook these functions into the Doom64 EX+ source tree to bring Doom 64
 * into the OASIS Omniverse.  Doom64 EX+ is an id Tech 1 derivative (C),
 * so hook patterns are similar to classic Doom but in the EX+ source layout.
 *
 * Minimum hook sites in the Doom64 EX+ source:
 *   src/doom64/p_inter.c    P_TouchSpecialThing() item pickup
 *                                   → ODoom64_STAR_OnItemPickup
 *   src/doom64/p_inter.c    P_KillMobj() monster death
 *                                   → ODoom64_STAR_OnMonsterKilled
 *   src/doom64/g_game.c     G_Ticker() once per tic
 *                                   → ODoom64_STAR_Tick
 *   src/doom64/st_stuff.c   ST_Ticker() HUD update
 *                                   → ODoom64_STAR_DrawHUDStatus
 *   startup / D_DoomMain()          → ODoom64_STAR_Init
 *   shutdown / I_Quit()             → ODoom64_STAR_Cleanup
 *   src/doom64/g_game.c     key/button handler → ODoom64_STAR_HandleKey
 *
 * Doom 64 monster set differs from classic Doom — notably the Mother Demon
 * (final boss) and Nightmare Demon (purple spectre variant).
 *
 * Base engine: Doom64 EX+  https://github.com/svkaiser/Doom64EX-Plus  (GPL-2.0)
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void ODoom64_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);
void ODoom64_STAR_Cleanup(void);
void ODoom64_STAR_Tick(void);
void ODoom64_STAR_OnItemPickup(int mo_type, const char* item_name);
void ODoom64_STAR_OnMonsterKilled(int mo_type, const char* monster_name, const char* killer);
void ODoom64_STAR_DrawHUDStatus(int screen_w, int screen_h);
int  ODoom64_STAR_HandleKey(int key_code);
int  ODoom64_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
