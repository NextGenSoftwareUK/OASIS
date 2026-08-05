/**
 * ODuke3D - OASIS STAR API Integration
 *
 * C-linkage hooks for EDuke32 (classic Duke Nukem 3D port) to connect with
 * the OASIS STAR API for cross-game item sharing, quests, XP, and NFTs.
 *
 * ODuke3D is a fork of EDuke32 (https://eduke32.com).  Full credit to the
 * EDuke32 team (EDuke32 contributors, Jonathon Fowler, Richard Gobeille, etc.)
 * EDuke32 is licensed under GPL-2.0.  ODuke3D must comply with that license.
 *
 * Integration points (source/duke3d/src/):
 *   Init/Shutdown   : app_main() and G_GameExit()
 *   Per-frame tick  : G_Tics()
 *   Key pickup      : P_CheckInventory() for key item slots
 *   Door access     : G_OperateSectors() locked-door branch
 *   Actor kill      : A_DamageObject() when actor->extra <= 0
 *   HUD draw        : G_DrawRooms() / display-rest hook after status bar
 *   Key events      : G_ProcessInput() / KB_KeyPressed() path
 *   Face tile       : G_DrawStatusBar() face-sprite selection
 *
 * BUILD SYNC: COPY_TO_EDUKE32_AND_BUILD.ps1 copies this file plus
 * oduke3d_ogengine_integration.c, star_api.h, star_sync.h, and OGLib/ to
 * C:\Source\ODuke3D\source\duke3d\src\ before building.
 */

#ifndef ODUKE3D_OGENGINE_INTEGRATION_H
#define ODUKE3D_OGENGINE_INTEGRATION_H

#ifdef __cplusplus
extern "C" {
#endif

/* -----------------------------------------------------------------------
 * Lifecycle
 * -------------------------------------------------------------------- */

/** Call once at startup after engine init (e.g. end of app_main). */
void ODuke3D_STAR_Init(void);

/** Call once at shutdown before engine teardown (e.g. G_GameExit). */
void ODuke3D_STAR_Cleanup(void);

/** Call every game tic from G_Tics(). Drives star_sync and toast timers. */
void ODuke3D_STAR_Tick(void);

/* -----------------------------------------------------------------------
 * Cross-game item / door events
 * -------------------------------------------------------------------- */

/**
 * Call when the player picks up a key / access card.
 * @param key_type  "blue_key", "red_key", or "yellow_key"
 */
void ODuke3D_STAR_OnKeyPickup(const char *key_type);

/**
 * Call when the player opens a locked door and does NOT have the key locally.
 * Returns 1 if STAR had the key cross-game and it was consumed (open the door);
 * 0 if the door stays locked.
 * @param key_type  "blue_key", "red_key", or "yellow_key"
 */
int ODuke3D_STAR_CheckDoorAccess(const char *key_type);

/**
 * Call when an actor (enemy) is killed.
 * @param picnum          actor->picnum tile number of the killed actor.
 * @param engine_is_boss  1 if the engine already classifies this as a boss kill.
 */
void ODuke3D_STAR_OnActorKilled(int picnum, int engine_is_boss);

/* -----------------------------------------------------------------------
 * HUD / GUI state queries
 * -------------------------------------------------------------------- */

/** Returns 1 if the player is currently beamed in to OASIS. */
int ODuke3D_STAR_IsBeamedIn(void);

/** Returns the authenticated OASIS username, or "" if not beamed in. */
const char *ODuke3D_STAR_GetUsername(void);

/** Returns the player's current OASIS XP total. */
int ODuke3D_STAR_GetXP(void);

/** Returns the integration version string, e.g. "ODuke3D 1.0.0". */
const char *ODuke3D_STAR_GetVersionString(void);

/* -----------------------------------------------------------------------
 * Popup state
 * -------------------------------------------------------------------- */

/** Returns 1 if the OASIS Inventory popup is currently open. */
int ODuke3D_STAR_IsInventoryPopupOpen(void);

/** Returns 1 if the OASIS Quest popup is currently open. */
int ODuke3D_STAR_IsQuestPopupOpen(void);

/** Toggle the Inventory popup (bind to I key in G_ProcessInput). */
void ODuke3D_STAR_ToggleInventoryPopup(void);

/** Toggle the Quest popup (bind to Q key in G_ProcessInput). */
void ODuke3D_STAR_ToggleQuestPopup(void);

/**
 * Returns 1 when game input (movement / fire / use) should be blocked
 * because an OASIS popup is open.  Check this in P_ProcessInput.
 */
int ODuke3D_STAR_ShouldBlockInput(void);

/* -----------------------------------------------------------------------
 * Avatar face overlay
 * -------------------------------------------------------------------- */

/**
 * Returns 1 when the OASIS avatar face tile should replace the normal
 * Duke Nukem face in the status bar.  When 1, draw OASFACE instead of
 * the standard TILE_PLAYER picnum in G_DrawStatusBar.
 */
int ODuke3D_STAR_ShouldUseAvatarFace(void);

/* -----------------------------------------------------------------------
 * HUD drawing  (call from EDuke32 display path)
 * -------------------------------------------------------------------- */

/**
 * Draw OASIS status overlays: beamed-in label, XP counter, version string,
 * toast notifications.  Call from G_DrawRooms() / G_DrawDisplayRest() after
 * the normal status bar has been drawn.  Uses printext256() internally.
 */
void ODuke3D_STAR_DrawHUDStatus(void);

/**
 * Draw full-screen popup overlays (Inventory / Quest).
 * Call immediately after ODuke3D_STAR_DrawHUDStatus().
 */
void ODuke3D_STAR_DrawPopupOverlay(void);

/* -----------------------------------------------------------------------
 * Input handling
 * -------------------------------------------------------------------- */

/**
 * Forward key events to the OASIS integration layer.
 * Call from G_ProcessInput() or KB_KeyPressed() before normal game input.
 * @param sc    EDuke32 scan code (e.g. sc_I, sc_Q, sc_Escape).
 * @param down  1 = key pressed, 0 = released.
 */
void ODuke3D_STAR_HandleKey(int sc, int down);
void ODuke3D_STAR_CheckIncomingTeleport(void);

#ifdef __cplusplus
}
#endif

#endif /* ODUKE3D_OGENGINE_INTEGRATION_H */
