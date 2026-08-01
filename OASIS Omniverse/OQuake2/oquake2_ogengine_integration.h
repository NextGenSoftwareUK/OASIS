/**
 * OQuake2 - OASIS STAR API Integration
 *
 * Integrates Yamagi Quake II with the OASIS STAR API so keys collected in
 * OQuake, ODOOM, or other OASIS Omniverse games can open doors in OQuake2
 * and vice versa — cross-game keys, quests, inventory, and XP.
 *
 * Integration Points:
 * - Key pickup -> add to STAR inventory (blue_key, red_key)
 * - Door touch -> check local key first, then cross-game
 * - Monster killed -> XP + optional mint
 * - Health/armor/ammo pickup -> STAR inventory tracking
 * - Weapon pickup -> STAR inventory tracking
 * - In-game console "star" command
 *
 * OASIS thing type range: 6000-6899
 * Portal thing type: 5900 (shared cross-game)
 */

#ifndef OQUAKE2_OGENGINE_INTEGRATION_H
#define OQUAKE2_OGENGINE_INTEGRATION_H

#include "ogengine.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Thing type constants for OQuake2 items — canonical numbers from OGAssetCatalog */
#define OQUAKE2_THING_PORTAL            5900   /* shared cross-game portal */

/* Keys (6001-6003) */
#define OQUAKE2_THING_BLUE_KEY          6001
#define OQUAKE2_THING_RED_KEY           6002
#define OQUAKE2_THING_COMMANDERS_HEAD   6003

/* Weapons (6011-6020) */
#define OQUAKE2_THING_BLASTER           6011
#define OQUAKE2_THING_SHOTGUN           6012
#define OQUAKE2_THING_SUPER_SHOTGUN     6013
#define OQUAKE2_THING_MACHINEGUN        6014
#define OQUAKE2_THING_CHAINGUN          6015
#define OQUAKE2_THING_GRENADE_LAUNCHER  6016
#define OQUAKE2_THING_ROCKET_LAUNCHER   6017
#define OQUAKE2_THING_HYPERBLASTER      6018
#define OQUAKE2_THING_RAILGUN           6019
#define OQUAKE2_THING_BFG10K            6020

/* Ammo (6021-6026) */
#define OQUAKE2_THING_BULLETS           6021
#define OQUAKE2_THING_SHELLS            6022
#define OQUAKE2_THING_GRENADES          6023
#define OQUAKE2_THING_ROCKETS           6024
#define OQUAKE2_THING_CELLS             6025
#define OQUAKE2_THING_SLUGS             6026

/* Health (6031-6033) */
#define OQUAKE2_THING_SMALL_HEALTH      6031
#define OQUAKE2_THING_MEDIUM_HEALTH     6032
#define OQUAKE2_THING_MEGA_HEALTH       6033

/* Armor (6041-6043) */
#define OQUAKE2_THING_JACKET_ARMOR      6041
#define OQUAKE2_THING_COMBAT_ARMOR      6042
#define OQUAKE2_THING_BODY_ARMOR        6043

/* Monsters (6101-6112) */
#define OQUAKE2_THING_SOLDIER           6101
#define OQUAKE2_THING_INFANTRY          6102
#define OQUAKE2_THING_GUNNER            6103
#define OQUAKE2_THING_BERSERKER         6104
#define OQUAKE2_THING_GLADIATOR         6105
#define OQUAKE2_THING_FLYER             6106
#define OQUAKE2_THING_MEDIC             6107
#define OQUAKE2_THING_PARASITE          6108
#define OQUAKE2_THING_BRAIN             6109
#define OQUAKE2_THING_SUPERTANK         6110
#define OQUAKE2_THING_TANK              6111
#define OQUAKE2_THING_MAKRON            6112

/* Key item name constants (cross-game canonical names) */
#define OQUAKE2_ITEM_BLUE_KEY  "blue_key"
#define OQUAKE2_ITEM_RED_KEY   "red_key"

/* -------------------------------------------------------------------------
 * Lifecycle
 * ------------------------------------------------------------------------- */

/** Initialize STAR API integration. Call once at game startup (e.g. in Com_Init or main init). */
void OQuake2_STAR_Init(void);

/** Cleanup STAR API. Call at game shutdown. */
void OQuake2_STAR_Cleanup(void);

/* -------------------------------------------------------------------------
 * Key / door hooks
 * ------------------------------------------------------------------------- */

/** Call when the player picks up a key item. key_name is "blue_key" or "red_key". */
void OQuake2_STAR_OnKeyPickup(const char* key_name);

/**
 * Call when a key-locked door is triggered and the player does not have the key locally.
 * Returns 1 if STAR inventory had the key (door should open), 0 otherwise.
 */
int OQuake2_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name);

/* -------------------------------------------------------------------------
 * Item pickup hooks
 * ------------------------------------------------------------------------- */

/**
 * Call when any item is picked up (weapon, armor, health, ammo).
 * item_name: canonical item name (e.g. "Shotgun", "Combat Armor", "Bullets")
 * item_type: "Weapon", "Armor", "Health", "Ammo"
 * quantity: amount picked up (1 for most items, actual count for ammo)
 * description: optional human-readable description (may be NULL)
 */
void OQuake2_STAR_OnItemPickup(const char* item_name, const char* item_type,
                                int quantity, const char* description);

/**
 * Call when a pickup item could not be applied (player at max health/armor/ammo).
 * Engine should remove the entity after calling so it does not remain on the floor.
 * Same semantics as OQuake_STAR_OnPickupLeftOnFloor.
 */
void OQuake2_STAR_OnPickupLeftOnFloor(const char* item_name, const char* item_type,
                                       int quantity, const char* description);

/**
 * Call before running a pickup touch function. Returns:
 *   0 = no intercept (proceed normally)
 *   1 = intercept: skip touch and free item entity
 * Engine must handle (player, item) and (item, player) orderings.
 */
int OQuake2_STAR_InterceptTouchPickupAtMax(void* item_ent, void* player_ent);

/* -------------------------------------------------------------------------
 * Monster kill hooks
 * ------------------------------------------------------------------------- */

/** Call when any monster is killed. Queues XP + optional mint. */
void OQuake2_STAR_OnMonsterKilled(const char* monster_classname);

/** Call when a boss monster is killed (Makron, Jorg). Same as OnMonsterKilled; kept for API parity. */
void OQuake2_STAR_OnBossKilled(const char* boss_name);

/* -------------------------------------------------------------------------
 * Frame pump and polling
 * ------------------------------------------------------------------------- */

/**
 * Call every frame (e.g. from main game loop) to poll pending STAR operations.
 * No-op when not in game or before Init. Handles async auth callbacks, inventory
 * refresh results, and XP toasts.
 */
void OQuake2_STAR_PollItems(void);

/* -------------------------------------------------------------------------
 * HUD / overlay draw hooks
 * ------------------------------------------------------------------------- */

/**
 * Draw the OASIS inventory overlay (I key). Call from the HUD draw path.
 * ctx is the game's render/draw context pointer (cast to void* for ABI neutrality).
 */
void OQuake2_STAR_DrawInventoryOverlay(void* ctx);

/** Draw the beam-in status line (top-left HUD). Call from HUD draw. */
void OQuake2_STAR_DrawBeamedInStatus(void* ctx);

/** Draw the active quest tracker line (top-left HUD, below beam-in). Call from HUD draw. */
void OQuake2_STAR_DrawQuestTracker(void* ctx);

/** Draw avatar XP in top-right corner when beamed in. Call from HUD draw. */
void OQuake2_STAR_DrawXpStatus(void* ctx);

/** Draw toast message at top-center (e.g. "Already at max health"). Call from HUD draw. */
void OQuake2_STAR_DrawToast(void* ctx);

/** Draw version/status line in corner. Call from HUD draw. */
void OQuake2_STAR_DrawVersionStatus(void* ctx);

/* -------------------------------------------------------------------------
 * Popup state queries (for engine input blocking)
 * ------------------------------------------------------------------------- */

/**
 * Returns 1 if the quest popup (Q key) is open, 0 otherwise.
 * Engine should suppress player movement while any popup is open.
 */
int OQuake2_STAR_IsQuestPopupOpen(void);

/**
 * Returns 1 if the inventory popup (I key) is open, 0 otherwise.
 * Engine should suppress player movement while any popup is open.
 */
int OQuake2_STAR_IsInventoryPopupOpen(void);

/* -------------------------------------------------------------------------
 * Misc queries
 * ------------------------------------------------------------------------- */

/** Returns 1 if the anorak/avatar face should replace the player face in the status bar. */
int OQuake2_STAR_ShouldUseAnorakFace(void);

/** Write the current beamed-in username to buf (null-terminated). Returns bytes written or 0. */
const char* OQuake2_STAR_GetUsername(void);

/* -------------------------------------------------------------------------
 * Console command
 * ------------------------------------------------------------------------- */

/** In-game console "star" command handler. Registered by Init. */
void OQuake2_STAR_Console_f(void);

#ifdef __cplusplus
}
#endif

#endif /* OQUAKE2_OGENGINE_INTEGRATION_H */
