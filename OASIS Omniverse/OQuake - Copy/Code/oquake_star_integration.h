/**
 * OQuake - OASIS STAR API Integration
 *
 * Integrates Quake with the OASIS STAR API so keys collected in ODOOM
 * can open doors in OQuake and vice versa.
 *
 * Integration Points:
 * - Key pickup -> add to STAR inventory (silver_key, gold_key)
 * - Door touch -> check local key first, then cross-game (Doom keycards)
 */

#ifndef OQUAKE_STAR_INTEGRATION_H
#define OQUAKE_STAR_INTEGRATION_H

#include "star_api.h"

#ifdef __cplusplus
extern "C" {
#endif

#define OQUAKE_ITEM_SILVER_KEY "silver_key"
#define OQUAKE_ITEM_GOLD_KEY   "gold_key"

typedef struct cb_context_s cb_context_t;

void OQuake_STAR_Init(void);
void OQuake_STAR_Cleanup(void);
void OQuake_STAR_OnKeyPickup(const char* key_name);
/** Only report pickups when in_real_game is 1 (e.g. sv.active && !cls.demoplayback). Use from engine to avoid tracking during demos/menu. */
void OQuake_STAR_OnItemsChangedEx(unsigned int old_items, unsigned int new_items, int in_real_game);
void OQuake_STAR_OnItemsChanged(unsigned int old_items, unsigned int new_items);
void OQuake_STAR_OnStatsChangedEx(
    int old_shells, int new_shells, int old_nails, int new_nails,
    int old_rockets, int new_rockets, int old_cells, int new_cells,
    int old_health, int new_health, int old_armor, int new_armor, int in_real_game);
void OQuake_STAR_OnStatsChanged(
    int old_shells, int new_shells,
    int old_nails, int new_nails,
    int old_rockets, int new_rockets,
    int old_cells, int new_cells,
    int old_health, int new_health,
    int old_armor, int new_armor);
/** Call every frame (e.g. from Host_Frame) so item/stats pickups are reported even if sbar isn't drawn. No-op when not in game or during demo. */
void OQuake_STAR_PollItems(void);
int  OQuake_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name);
void OQuake_STAR_Console_f(void); /* in-game console "star" command - registered by Init */
void OQuake_STAR_DrawInventoryOverlay(cb_context_t* cbx);
int OQuake_STAR_ShouldUseAnorakFace(void);
const char* OQuake_STAR_GetUsername(void);
void OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx);
void OQuake_STAR_DrawVersionStatus(cb_context_t* cbx);
/** Draw avatar XP in top-right (when beamed in). Call from same HUD path as DrawVersionStatus. */
void OQuake_STAR_DrawXpStatus(cb_context_t* cbx);
/** Call from engine or QuakeC when a monster is killed: queues XP + optional mint + add to inventory (async). */
void OQuake_STAR_OnMonsterKilled(const char* monster_name);
/** Call from engine or QuakeC when a boss is killed (same as OnMonsterKilled; kept for backward compat). */
void OQuake_STAR_OnBossKilled(const char* boss_name);
/** Called from ED_Free hook only: checks progs/state and forwards monster_* entities to OnMonsterKilled. Pass edict*. */
void OQuake_STAR_OnEntityFreed(void* ent);

#ifdef __cplusplus
}
#endif

#endif /* OQUAKE_STAR_INTEGRATION_H */
