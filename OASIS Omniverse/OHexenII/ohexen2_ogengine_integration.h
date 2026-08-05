/**
 * OHexenII - OASIS STAR API Integration Header
 *
 * Hook these functions into the uhexen2 (Hammer of Thyrion) source tree to
 * bring Hexen II into the OASIS Omniverse.  Hexen II uses the Quake engine
 * (id Tech 2 derivative), so hook patterns are similar to OQuake2 but with
 * Hexen II's four-class structure and inventory system.
 *
 * Hexen II-specific notes:
 *   - Four player classes: Paladin, Crusader, Necromancer, Assassin
 *     → reported to STAR as avatar "player_class" attribute on selection
 *   - Puzzle items (Orb, Crown, Axe, etc.) are key-items for cross-game carry
 *   - Hub-based level progression: pass hub ID to Init
 *
 * Minimum hook sites in the uhexen2 source:
 *   engine/h2/sv_main.c   SV_Frame() once per server frame
 *                                 → OHexenII_STAR_Tick
 *   game/h2/items.c       item touch / pickup functions
 *                                 → OHexenII_STAR_OnItemPickup
 *   game/h2/monster.c     monster death (monster_die)
 *                                 → OHexenII_STAR_OnMonsterKilled
 *   game/h2/cl_hud.c      HUD draw path
 *                                 → OHexenII_STAR_DrawHUDStatus
 *   engine/h2/host.c      Host_Init / Host_Shutdown
 *                                 → OHexenII_STAR_Init / Cleanup
 *   engine/h2/in_sdl.c    key event handler
 *                                 → OHexenII_STAR_HandleKey
 *   game/h2/player.c      class selection callback
 *                                 → OHexenII_STAR_OnClassSelected
 *
 * Base engine: uhexen2 (Hammer of Thyrion)
 *              https://github.com/sezero/uhexen2  (GPL-2.0)
 *
 * OASIS thing-type range : 6000-6899
 * Portal thing type      : 5900
 */

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void OHexenII_STAR_Init(const char* star_api_base_url, const char* oasis_json_path);
void OHexenII_STAR_Cleanup(void);
void OHexenII_STAR_Tick(void);
void OHexenII_STAR_OnItemPickup(const char* item_classname, const char* item_name);
void OHexenII_STAR_OnMonsterKilled(const char* monster_class, const char* killer);
void OHexenII_STAR_OnClassSelected(const char* class_name, const char* player_name);
void OHexenII_STAR_DrawHUDStatus(int screen_w, int screen_h);
int  OHexenII_STAR_HandleKey(int key_code);
int  OHexenII_STAR_IsReady(void);

#ifdef __cplusplus
}
#endif
