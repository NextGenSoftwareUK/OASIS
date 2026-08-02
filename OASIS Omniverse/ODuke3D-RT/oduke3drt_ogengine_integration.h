/**
 * ODuke3D-RT - OASIS STAR API Integration
 *
 * C-linkage hooks for Duke-RT (a Vulkan ray-tracing fork of EDuke32) to
 * connect with the OASIS STAR API for cross-game item sharing, quests,
 * XP, and NFTs.
 *
 * ODuke3D-RT is a fork of Duke-RT (https://github.com/fgsfdsfgs/duke-rt),
 * which is itself a Vulkan ray-tracing modification of EDuke32.  Full
 * credit to the Duke-RT and EDuke32 teams (GPL-2.0).
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
 *   RT-specific     : r_lightmaps.cpp or vulkan_rt.cpp for RT-aware overlays
 *
 * BUILD SYNC: COPY_TO_DUKERT_AND_BUILD.ps1 copies this file plus
 * oduke3drt_ogengine_integration.c, star_api.h, star_sync.h, and OGLib/ to
 * C:\Source\ODuke3D-RT\source\duke3d\src\ before building.
 */

#ifndef ODUKE3DRT_OGENGINE_INTEGRATION_H
#define ODUKE3DRT_OGENGINE_INTEGRATION_H

#ifdef __cplusplus
extern "C" {
#endif

/* -----------------------------------------------------------------------
 * Lifecycle
 * -------------------------------------------------------------------- */

/** Call once at startup after engine/Vulkan init. */
void ODuke3DRT_STAR_Init(void);

/** Call once at shutdown before engine/Vulkan teardown. */
void ODuke3DRT_STAR_Cleanup(void);

/** Call every game tic from G_Tics(). Drives star_sync and toast timers. */
void ODuke3DRT_STAR_Tick(void);

/* -----------------------------------------------------------------------
 * Cross-game item / door events
 * -------------------------------------------------------------------- */

/**
 * Call when the player picks up a key / access card.
 * @param key_type  "blue_key", "red_key", or "yellow_key"
 */
void ODuke3DRT_STAR_OnKeyPickup(const char *key_type);

/**
 * Call when the player opens a locked door and does NOT have the key locally.
 * Returns 1 if STAR had the key cross-game and it was consumed (open the door).
 * @param key_type  "blue_key", "red_key", or "yellow_key"
 */
int ODuke3DRT_STAR_CheckDoorAccess(const char *key_type);

/**
 * Call when an actor (enemy) is killed.
 * @param picnum          actor->picnum tile number.
 * @param engine_is_boss  1 if the engine classifies this as a boss kill.
 */
void ODuke3DRT_STAR_OnActorKilled(int picnum, int engine_is_boss);

/* -----------------------------------------------------------------------
 * HUD / GUI state queries
 * -------------------------------------------------------------------- */

int         ODuke3DRT_STAR_IsBeamedIn(void);
const char *ODuke3DRT_STAR_GetUsername(void);
int         ODuke3DRT_STAR_GetXP(void);
const char *ODuke3DRT_STAR_GetVersionString(void);

int  ODuke3DRT_STAR_IsInventoryPopupOpen(void);
int  ODuke3DRT_STAR_IsQuestPopupOpen(void);
void ODuke3DRT_STAR_ToggleInventoryPopup(void);
void ODuke3DRT_STAR_ToggleQuestPopup(void);

/**
 * Returns 1 when game input should be blocked because an OASIS popup is open.
 * Check in P_ProcessInput before applying movement / fire / use.
 */
int ODuke3DRT_STAR_ShouldBlockInput(void);

/**
 * Returns 1 when the OASIS avatar face tile should replace the Duke face
 * in the status bar HUD.
 */
int ODuke3DRT_STAR_ShouldUseAvatarFace(void);

/* -----------------------------------------------------------------------
 * HUD drawing  (call from EDuke32 / Duke-RT display path)
 * -------------------------------------------------------------------- */

/**
 * Draw OASIS status overlays: beamed-in label, XP, version, toasts.
 * Call after G_DrawStatusBar() in G_DrawRooms() / G_DrawDisplayRest().
 * Uses printext256() or the RT UI draw path internally.
 */
void ODuke3DRT_STAR_DrawHUDStatus(void);

/**
 * Draw full-screen popup overlays (Inventory / Quest).
 * Call immediately after ODuke3DRT_STAR_DrawHUDStatus().
 */
void ODuke3DRT_STAR_DrawPopupOverlay(void);

/* -----------------------------------------------------------------------
 * Input handling
 * -------------------------------------------------------------------- */

/**
 * Forward key events to the OASIS integration layer.
 * Call from G_ProcessInput() before normal game input.
 * @param sc    EDuke32 scan code (sc_I, sc_Q, sc_Escape, sc_UpArrow, sc_DownArrow).
 * @param down  1 = pressed, 0 = released.
 */
void ODuke3DRT_STAR_HandleKey(int sc, int down);
void ODuke3DRT_STAR_CheckIncomingTeleport(void);

#ifdef __cplusplus
}
#endif

#endif /* ODUKE3DRT_OGENGINE_INTEGRATION_H */
