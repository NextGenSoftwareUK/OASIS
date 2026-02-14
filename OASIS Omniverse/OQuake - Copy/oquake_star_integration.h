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
int  OQuake_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name);
void OQuake_STAR_Console_f(void); /* in-game console "star" command - registered by Init */
void OQuake_STAR_DrawInventoryOverlay(cb_context_t* cbx);
void OQuake_STAR_PollInventoryInput(void);
int OQuake_STAR_ShouldUseAnorakFace(void);
const char* OQuake_STAR_GetUsername(void);
void OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx);

#ifdef __cplusplus
}
#endif

#endif /* OQUAKE_STAR_INTEGRATION_H */
