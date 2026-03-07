/**
 * ODOOM - OASIS STAR API Integration
 *
 * Provides integration hooks for ODOOM (UZDoom-based, GZDoom fork) to connect with the
 * OASIS STAR API for cross-game item sharing (keycards, doors, quests).
 *
 * Integration points:
 * - Keycard collection (P_TouchSpecialThing)
 * - Door/lock checks (P_CheckKeys)
 * - Init at startup, cleanup at shutdown
 */

#ifndef UZDOOM_STAR_INTEGRATION_H
#define UZDOOM_STAR_INTEGRATION_H

#ifdef __cplusplus
extern "C" {
#endif

/** PreTouchSpecial return value for generic inventory (health, armor, ammo, etc.). Engine uses this to force-destroy actor when not consumed. */
#define STAR_PICKUP_GENERIC_ITEM 9001
/** PreTouchSpecial return value for weapons only. Engine must NOT destroy (so CallTouch gives weapon to player); we still call PostTouchSpecial to add/mint in STAR. */
#define STAR_PICKUP_WEAPON 9002

/** When true (always_allow_pickup_if_max=1), engine force-destroys generic pickups so they go to STAR inventory even when full. When false, original Doom behavior (full = can't pick up). */
int UZDoom_STAR_AlwaysAllowPickup(void);

struct AActor;

/** Initialize STAR API integration. Call once at game startup (e.g. in D_DoomMain). */
void UZDoom_STAR_Init(void);

/** Cleanup STAR API. Call at game shutdown (e.g. in D_Cleanup). */
void UZDoom_STAR_Cleanup(void);

/**
 * Call before special->CallTouch(toucher). If special is a keycard, returns key number (1-4);
 * otherwise returns 0. Use the return value in UZDoom_STAR_PostTouchSpecial after CallTouch. (Symbol names stay UZDoom_* for engine compatibility.)
 */
int UZDoom_STAR_PreTouchSpecial(struct AActor* special);

/** Call after CallTouch with the key number from PreTouchSpecial (0 = no key). */
void UZDoom_STAR_PostTouchSpecial(int keynum);

/**
 * Call from engine when a boss monster is killed to mint a boss NFT (WEB4).
 * Pass the boss name (e.g. "Cyberdemon", "SpiderMastermind"). No-op if not initialized.
 */
void UZDoom_STAR_OnBossKilled(const char* boss_name);

/**
 * Call from engine when any monster is killed. If that monster has mint=1 in oasisstar.json
 * (mint_monsters list), mints an NFT and adds "[NFT] MonsterName" to STAR inventory as type "Monster".
 * Call from actor death handling when the dying actor is a monster (e.g. flags3 & MF3_ISMONSTER).
 */
void UZDoom_STAR_OnMonsterKilled(const char* monster_name);

/**
 * If local key check failed, try cross-game inventory. Returns true if door/lock
 * should be opened (STAR API had the key and used it).
 */
int UZDoom_STAR_CheckDoorAccess(struct AActor* owner, int keynum, int remote);

/**
 * Read-only check for HUD/status bar: returns 1 if STAR has this key (keynum 1-4).
 * Call from P_CheckKeys when quiet==true so key icons show for keys from STAR inventory after load.
 */
int UZDoom_STAR_PlayerHasKey(int keynum);

/** Log when EV_DoDoor is about to check keys (lock 1-4). Call from a_doors.cpp EV_DoDoor before P_CheckKeys. */
void ODOOM_STAR_LogEvDoDoorLock(int lock);
/** Log when P_ActivateLine is about to check keys (line->locknumber). Call from p_spec.cpp before P_CheckKeys when line->locknumber > 0. */
void ODOOM_STAR_LogLineDoorKeyCheck(int keynum);
/** Log every time P_ActivateLine runs (E use, push, etc.). Call at start of P_ActivateLine. */
void ODOOM_STAR_LogActivateLineUse(int activationType, int special, int locknumber);
/** Log when Door_LockedRaise (special 13) runs with its lock arg so we see map lock value. Call from p_lnspec LS_Door_LockedRaise. */
void ODOOM_STAR_LogDoorLockedRaiseLock(int lock);

/** Call every frame from status bar (when OASIS_STAR_API): polls async auth/inventory and when inventory open, clear key bindings (OQuake-style). */
void ODOOM_InventoryInputCaptureFrame(void);

/** Call after TryRunTics so STAR health/armor apply runs after the tic and is not overwritten; applies deferred use-item health/armor. */
void ODOOM_PostTic(void);
/** Call after every game tic (inside TryRunTics loop) to re-apply stored health/armor so engine overwrites don't stick. */
void ODOOM_PostOneTic(void);

/** Call from engine input when building ticcmd: set odoom_key_* CVars from raw key state (for ZScript). q = key Q for quest popup. */
void ODOOM_InventorySetKeyState(int up, int down, int left, int right, int use, int a, int c, int z, int x, int i, int o, int p, int q, int enter, int pgup, int pgdown, int home, int endkey);

/** Whether to show OASIS anorak face in status bar. Only set by star face on/off and beam-in/out. */
int UZDoom_STAR_GetShowAnorakFace(void);

#ifdef __cplusplus
}
#endif

#endif /* UZDOOM_STAR_INTEGRATION_H */
