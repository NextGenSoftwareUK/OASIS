/**
 * OQuake2-RTX - OASIS STAR API Integration
 *
 * Integrates NVIDIA's Q2 RTX (ray-traced Quake 2 remaster) with the OASIS STAR API.
 * Q2 RTX is built on the Yamagi Quake II codebase with NVIDIA's Vulkan RTX renderer.
 * This integration shares the same 6xxx OASIS thing type range as OQuake2 — it is the
 * same game with a different renderer.
 *
 * Integration Points:
 * - Key pickup -> add to STAR inventory (blue_key, red_key)
 * - Door touch -> check local key first, then cross-game inventory
 * - Weapon/armor/health/ammo pickup -> STAR inventory tracking
 * - Monster killed -> XP + optional NFT mint
 * - In-game console "star" command
 *
 * OASIS thing type range: 6000-6899 (same as OQuake2 — shared game content)
 * Portal thing type: 5900 (shared cross-game)
 *
 * Base engine: Q2 RTX (NVIDIA, Vulkan RTX, based on Yamagi Q2 / GPL-2.0)
 * See https://github.com/NVIDIA/Q2RTX for the base engine.
 */

#ifndef OQUAKE2RTX_OGENGINE_INTEGRATION_H
#define OQUAKE2RTX_OGENGINE_INTEGRATION_H

#include "ogengine.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Thing type constants — same as OQuake2 (shared game content, different renderer) */
#define OQUAKE2RTX_THING_PORTAL            5900   /* shared cross-game portal */

/* Keys (6001-6003) */
#define OQUAKE2RTX_THING_BLUE_KEY          6001
#define OQUAKE2RTX_THING_RED_KEY           6002
#define OQUAKE2RTX_THING_COMMANDERS_HEAD   6003

/* Weapons (6011-6020) */
#define OQUAKE2RTX_THING_BLASTER           6011
#define OQUAKE2RTX_THING_SHOTGUN           6012
#define OQUAKE2RTX_THING_SUPER_SHOTGUN     6013
#define OQUAKE2RTX_THING_MACHINEGUN        6014
#define OQUAKE2RTX_THING_CHAINGUN          6015
#define OQUAKE2RTX_THING_GRENADE_LAUNCHER  6016
#define OQUAKE2RTX_THING_ROCKET_LAUNCHER   6017
#define OQUAKE2RTX_THING_HYPERBLASTER      6018
#define OQUAKE2RTX_THING_RAILGUN           6019
#define OQUAKE2RTX_THING_BFG10K            6020

/* Ammo (6021-6026) */
#define OQUAKE2RTX_THING_BULLETS           6021
#define OQUAKE2RTX_THING_SHELLS            6022
#define OQUAKE2RTX_THING_GRENADES          6023
#define OQUAKE2RTX_THING_ROCKETS           6024
#define OQUAKE2RTX_THING_CELLS             6025
#define OQUAKE2RTX_THING_SLUGS             6026

/* Health (6031-6033) */
#define OQUAKE2RTX_THING_SMALL_HEALTH      6031
#define OQUAKE2RTX_THING_MEDIUM_HEALTH     6032
#define OQUAKE2RTX_THING_MEGA_HEALTH       6033

/* Armor (6041-6043) */
#define OQUAKE2RTX_THING_JACKET_ARMOR      6041
#define OQUAKE2RTX_THING_COMBAT_ARMOR      6042
#define OQUAKE2RTX_THING_BODY_ARMOR        6043

/* Monsters (6101-6112) */
#define OQUAKE2RTX_THING_SOLDIER           6101
#define OQUAKE2RTX_THING_INFANTRY          6102
#define OQUAKE2RTX_THING_GUNNER            6103
#define OQUAKE2RTX_THING_BERSERKER         6104
#define OQUAKE2RTX_THING_GLADIATOR         6105
#define OQUAKE2RTX_THING_FLYER             6106
#define OQUAKE2RTX_THING_MEDIC             6107
#define OQUAKE2RTX_THING_PARASITE          6108
#define OQUAKE2RTX_THING_BRAIN             6109
#define OQUAKE2RTX_THING_SUPERTANK         6110
#define OQUAKE2RTX_THING_TANK              6111
#define OQUAKE2RTX_THING_MAKRON            6112

/* Key item name constants (cross-game canonical names) */
#define OQUAKE2RTX_ITEM_BLUE_KEY  "blue_key"
#define OQUAKE2RTX_ITEM_RED_KEY   "red_key"

/* -------------------------------------------------------------------------
 * Lifecycle
 * ------------------------------------------------------------------------- */

/** Initialize STAR API integration. Call once at game startup. */
void OQuake2RTX_STAR_Init(void);

/** Cleanup STAR API. Call at game shutdown. */
void OQuake2RTX_STAR_Cleanup(void);

/* -------------------------------------------------------------------------
 * Key / door hooks
 * ------------------------------------------------------------------------- */

/** Call when the player picks up a key item. key_name: "blue_key" or "red_key". */
void OQuake2RTX_STAR_OnKeyPickup(const char* key_name);

/**
 * Call when a key-locked door is triggered and the player lacks the key locally.
 * Returns 1 if STAR inventory had the key (door should open), 0 otherwise.
 */
int OQuake2RTX_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name);

/* -------------------------------------------------------------------------
 * Item pickup hooks
 * ------------------------------------------------------------------------- */

/**
 * Call when any item is picked up (weapon, armor, health, ammo).
 * item_name: canonical item name (e.g. "Railgun", "Body Armor")
 * item_type: "Weapon", "Armor", "Health", "Ammo"
 * quantity: amount picked up
 * description: optional human-readable description (may be NULL)
 */
void OQuake2RTX_STAR_OnItemPickup(const char* item_name, const char* item_type,
                                   int quantity, const char* description);

/**
 * Call when a pickup could not be applied (player at max health/armor/ammo).
 * Engine should remove the entity after calling.
 */
void OQuake2RTX_STAR_OnPickupLeftOnFloor(const char* item_name, const char* item_type,
                                          int quantity, const char* description);

/**
 * Call before running a pickup touch function.
 * Returns 1 = intercept (skip touch, free entity); 0 = proceed normally.
 */
int OQuake2RTX_STAR_InterceptTouchPickupAtMax(void* item_ent, void* player_ent);

/* -------------------------------------------------------------------------
 * Monster kill hooks
 * ------------------------------------------------------------------------- */

/** Call when any monster is killed. Queues XP + optional NFT mint. */
void OQuake2RTX_STAR_OnMonsterKilled(const char* monster_classname);

/** Call when a boss monster is killed (Makron, Jorg). Kept for API parity. */
void OQuake2RTX_STAR_OnBossKilled(const char* boss_name);

/* -------------------------------------------------------------------------
 * Frame pump and polling
 * ------------------------------------------------------------------------- */

/**
 * Call every frame to poll pending STAR operations. No-op before Init.
 * Handles async auth callbacks, inventory refresh, and console log drain.
 */
void OQuake2RTX_STAR_PollItems(void);

/* -------------------------------------------------------------------------
 * HUD / overlay draw hooks
 * (Q2 RTX uses Vulkan; pass the appropriate render context pointer or NULL)
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_DrawInventoryOverlay(void* ctx);
void OQuake2RTX_STAR_DrawBeamedInStatus(void* ctx);
void OQuake2RTX_STAR_DrawQuestTracker(void* ctx);
void OQuake2RTX_STAR_DrawXpStatus(void* ctx);
void OQuake2RTX_STAR_DrawToast(void* ctx);
void OQuake2RTX_STAR_DrawVersionStatus(void* ctx);

/* -------------------------------------------------------------------------
 * Popup state queries (for engine input blocking)
 * ------------------------------------------------------------------------- */

/** Returns 1 if the quest popup (Q key) is open. Suppress player movement when 1. */
int OQuake2RTX_STAR_IsQuestPopupOpen(void);

/** Returns 1 if the inventory popup (I key) is open. Suppress player movement when 1. */
int OQuake2RTX_STAR_IsInventoryPopupOpen(void);

/* -------------------------------------------------------------------------
 * Misc queries
 * ------------------------------------------------------------------------- */

/** Returns 1 if the anorak/avatar face should replace the player HUD face. */
int OQuake2RTX_STAR_ShouldUseAnorakFace(void);

/** Returns the current beamed-in username, or empty string if not beamed in. */
const char* OQuake2RTX_STAR_GetUsername(void);

/* -------------------------------------------------------------------------
 * Console command
 * ------------------------------------------------------------------------- */

/** In-game console "star" command handler. Registered by Init. */
void OQuake2RTX_STAR_Console_f(void);

#ifdef __cplusplus
}
#endif

#endif /* OQUAKE2RTX_OGENGINE_INTEGRATION_H */
