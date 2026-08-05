/**
 * OQuake3 - OASIS STAR API Integration
 *
 * Integrates Quake3e (open-source Quake III Arena engine) with the OASIS STAR API.
 * Provides cross-game inventory tracking, XP, quest progression, and optional
 * NFT minting for rune pickups, item pickups, and bot/monster kills.
 *
 * Integration Points:
 * - Rune pickup -> add to STAR inventory (rune_strength, rune_haste, etc.)
 * - Item pickup (weapon, armor, health, ammo, powerup) -> STAR inventory tracking
 * - Bot/player kill in PvE mode -> XP + optional NFT mint
 * - In-game console "star" command
 *
 * Note: Quake III Arena is an arena/deathmatch game — it has no traditional key/door
 * locks. Runes (from Q3:TA Harvester/Overload modes) fill the "key" role in OASIS.
 * Cross-game portal connections use thing type 5900.
 *
 * OASIS thing type range: 7000-7899
 * Portal thing type: 5900 (shared cross-game)
 *
 * Base engine: Quake3e (https://github.com/ec-/Quake3e, GPL-2.0)
 * Original Quake III Arena: id Software / Bethesda Softworks
 */

#ifndef OQUAKE3_OGENGINE_INTEGRATION_H
#define OQUAKE3_OGENGINE_INTEGRATION_H

#include "ogengine.h"

#ifdef __cplusplus
extern "C" {
#endif

/* -------------------------------------------------------------------------
 * Thing type constants — OQuake3 range: 7000-7899
 * Portal thing type: 5900 (shared cross-game)
 * ------------------------------------------------------------------------- */

#define OQUAKE3_THING_PORTAL            5900   /* shared cross-game portal */

/* Runes (7001-7004) — Quake III: Team Arena / Harvester mode */
#define OQUAKE3_THING_RUNE_STRENGTH     7001
#define OQUAKE3_THING_RUNE_HASTE        7002
#define OQUAKE3_THING_RUNE_REGENERATION 7003
#define OQUAKE3_THING_RUNE_RESISTANCE   7004

/* Weapons (7011-7019) — canonical OGAssetCatalog order */
#define OQUAKE3_THING_ROCKET_LAUNCHER   7011
#define OQUAKE3_THING_RAILGUN           7012
#define OQUAKE3_THING_SHOTGUN           7013
#define OQUAKE3_THING_LIGHTNING_GUN     7014
#define OQUAKE3_THING_PLASMA_GUN        7015
#define OQUAKE3_THING_BFG               7016
#define OQUAKE3_THING_GAUNTLET          7017
#define OQUAKE3_THING_MACHINEGUN        7018
#define OQUAKE3_THING_GRENADE_LAUNCHER  7019

/* Ammo (7021-7028) */
#define OQUAKE3_THING_AMMO_ROCKETS      7021
#define OQUAKE3_THING_AMMO_SLUGS        7022
#define OQUAKE3_THING_AMMO_SHELLS       7023
#define OQUAKE3_THING_AMMO_LIGHTNING    7024
#define OQUAKE3_THING_AMMO_PLASMA       7025
#define OQUAKE3_THING_AMMO_BULLETS      7026
#define OQUAKE3_THING_AMMO_GRENADES     7027
#define OQUAKE3_THING_AMMO_BFG          7028

/* Health (7031-7034) */
#define OQUAKE3_THING_SMALL_HEALTH      7031
#define OQUAKE3_THING_HEALTH            7032
#define OQUAKE3_THING_LARGE_HEALTH      7033
#define OQUAKE3_THING_MEGA_HEALTH       7034

/* Armor (7041-7043) */
#define OQUAKE3_THING_ARMOR_SHARD       7041
#define OQUAKE3_THING_YELLOW_ARMOR      7042
#define OQUAKE3_THING_RED_ARMOR         7043

/* PowerUps (7051-7056) */
#define OQUAKE3_THING_QUAD_DAMAGE       7051
#define OQUAKE3_THING_REGENERATION      7052
#define OQUAKE3_THING_HASTE             7053
#define OQUAKE3_THING_BATTLE_SUIT       7054
#define OQUAKE3_THING_FLIGHT            7055
#define OQUAKE3_THING_INVISIBILITY      7056

/* Bots (7061-7064) */
#define OQUAKE3_THING_BOT_GRUNT         7061
#define OQUAKE3_THING_BOT_KLESK         7062
#define OQUAKE3_THING_BOT_XAERO         7063
#define OQUAKE3_THING_BOT_ORBB          7064

/* -------------------------------------------------------------------------
 * Rune item name constants (cross-game canonical names)
 * ------------------------------------------------------------------------- */
#define OQUAKE3_ITEM_RUNE_STRENGTH      "rune_strength"
#define OQUAKE3_ITEM_RUNE_HASTE         "rune_haste"
#define OQUAKE3_ITEM_RUNE_REGENERATION  "rune_regeneration"
#define OQUAKE3_ITEM_RUNE_RESISTANCE    "rune_resistance"

/* -------------------------------------------------------------------------
 * Context struct passed to HUD draw functions
 * (cast from whatever context pointer the Q3 renderer provides)
 * ------------------------------------------------------------------------- */
typedef struct {
    void*  render_ctx;   /* renderer/backend context; may be NULL */
    int    screen_w;
    int    screen_h;
    float  scale;
} oq3_cb_context_t;

/* -------------------------------------------------------------------------
 * Lifecycle
 * ------------------------------------------------------------------------- */

/** Initialize STAR API integration. Call once at game startup. */
void OQuake3_STAR_Init(void);

/** Cleanup STAR API. Call at game shutdown. */
void OQuake3_STAR_Cleanup(void);

/* -------------------------------------------------------------------------
 * Rune hooks (replaces key/door hooks from Q1/Q2)
 * ------------------------------------------------------------------------- */

/**
 * Call when the player picks up a rune.
 * rune_name: one of "rune_strength", "rune_haste", "rune_regeneration", "rune_resistance"
 */
void OQuake3_STAR_OnRunePickup(const char* rune_name);

/**
 * Returns 1 if the player has the given rune in the OASIS cross-game inventory.
 * Use for Harvester/Overload mode rune checks.
 */
int OQuake3_STAR_HasRune(const char* rune_name);

/* -------------------------------------------------------------------------
 * Item pickup hooks
 * ------------------------------------------------------------------------- */

/**
 * Call when any item is picked up (weapon, armor, health, ammo, powerup).
 * item_name: canonical item name (e.g. "Railgun", "Red Armor", "Quad Damage")
 * item_type: "Weapon", "Armor", "Health", "Ammo", "Powerup", "Rune"
 * quantity: amount picked up
 * description: optional human-readable description (may be NULL)
 */
void OQuake3_STAR_OnItemPickup(const char* item_name, const char* item_type,
                                int quantity, const char* description);

/**
 * Call when a pickup could not be applied (player at max health/armor/ammo).
 * Engine should remove the entity after calling.
 */
void OQuake3_STAR_OnPickupLeftOnFloor(const char* item_name, const char* item_type,
                                       int quantity, const char* description);

/**
 * Call before running a pickup touch function.
 * Returns 1 = intercept (skip touch, free entity); 0 = proceed normally.
 */
int OQuake3_STAR_InterceptTouchPickupAtMax(void* item_ent, void* player_ent);

/* -------------------------------------------------------------------------
 * Bot / player kill hooks
 * ------------------------------------------------------------------------- */

/**
 * Call when a bot is killed in single-player / PvE mode.
 * bot_classname: e.g. "bot_grunt", "bot_klesk", "bot_xaero", "bot_orbb"
 */
void OQuake3_STAR_OnBotKilled(const char* bot_classname);

/**
 * Call when any player is fragged (PvP). Used for XP tracking only.
 * victim_name: name of the fragged player/bot
 * is_bot: 1 if the victim is a bot, 0 if human
 */
void OQuake3_STAR_OnPlayerFragged(const char* victim_name, int is_bot);

/* -------------------------------------------------------------------------
 * Frame pump and polling
 * ------------------------------------------------------------------------- */

/**
 * Call every frame to poll pending STAR operations. No-op before Init.
 * Handles async auth callbacks, inventory refresh, and console log drain.
 */
void OQuake3_STAR_PollItems(void);

/* -------------------------------------------------------------------------
 * HUD / overlay draw hooks
 * ------------------------------------------------------------------------- */

void OQuake3_STAR_DrawInventoryOverlay(oq3_cb_context_t* ctx);
void OQuake3_STAR_DrawBeamedInStatus(oq3_cb_context_t* ctx);
void OQuake3_STAR_DrawQuestTracker(oq3_cb_context_t* ctx);
void OQuake3_STAR_DrawXpStatus(oq3_cb_context_t* ctx);
void OQuake3_STAR_DrawToast(oq3_cb_context_t* ctx);
void OQuake3_STAR_DrawVersionStatus(oq3_cb_context_t* ctx);

/* -------------------------------------------------------------------------
 * Popup state queries (for engine input blocking)
 * ------------------------------------------------------------------------- */

/** Returns 1 if the quest popup is open. Suppress player movement when 1. */
int OQuake3_STAR_IsQuestPopupOpen(void);

/** Returns 1 if the inventory popup is open. Suppress player movement when 1. */
int OQuake3_STAR_IsInventoryPopupOpen(void);

/* -------------------------------------------------------------------------
 * Misc queries
 * ------------------------------------------------------------------------- */

/** Returns 1 if the anorak/avatar face should replace the player HUD face. */
int OQuake3_STAR_ShouldUseAnorakFace(void);

/** Returns the current beamed-in username, or empty string if not beamed in. */
const char* OQuake3_STAR_GetUsername(void);

/* -------------------------------------------------------------------------
 * Cross-game teleportation
 * ------------------------------------------------------------------------- */

/** Call at map load. Polls for an incoming teleport from OmniverseKernel.
 *  Warps the player to the target position via g_entities[0].client->ps.origin + trap_LinkEntity. */
void OQuake3_STAR_CheckIncomingTeleport(void);

/* -------------------------------------------------------------------------
 * Console command
 * ------------------------------------------------------------------------- */

/** In-game console "star" command handler. Registered by Init. */
void OQuake3_STAR_Console_f(void);

#ifdef __cplusplus
}
#endif

#endif /* OQUAKE3_OGENGINE_INTEGRATION_H */
