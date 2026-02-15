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
void OQuake_STAR_OnItemsChanged(unsigned int old_items, unsigned int new_items);
void OQuake_STAR_OnStatsChanged(
    int old_shells, int new_shells,
    int old_nails, int new_nails,
    int old_rockets, int new_rockets,
    int old_cells, int new_cells,
    int old_health, int new_health,
    int old_armor, int new_armor);
int  OQuake_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name);
void OQuake_STAR_Console_f(void); /* in-game console "star" command - registered by Init */
void OQuake_STAR_DrawInventoryOverlay(cb_context_t* cbx);
int OQuake_STAR_ShouldUseAnorakFace(void);
const char* OQuake_STAR_GetUsername(void);
void OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx);

#ifdef __cplusplus
}
#endif

#endif /* OQUAKE_STAR_INTEGRATION_H */
