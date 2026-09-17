/**
 * ODOOM3-BFG - OASIS STAR API Integration
 *
 * Minimal C-linkage hooks for RBDOOM-3-BFG (id Tech 4/5) to connect with the
 * OASIS STAR API for cross-game item sharing, quests, and NFTs.
 *
 * Integration points (all in neo/d3xp/):
 *   Init/Shutdown : idGameLocal::Init() / idGameLocal::Shutdown()
 *   Per-frame tick: idGameLocal::RunFrame()
 *   Item pickup   : idPlayer::GiveInventoryItem()
 *   Door key check: idGameLocal::RequirementMet()
 *   Monster kill  : idAI::Killed()
 *
 * BUILD SYNC: COPY_TO_RBDOOM3_AND_BUILD.ps1 copies this file plus
 * d3doom_ogengine_integration.cpp, star_api.h, star_sync.h, and OGLib/ to
 * C:\Source\ODOOM3-BFG\neo\d3xp\ before compiling.
 */

#ifndef D3DOOM_OGENGINE_INTEGRATION_H
#define D3DOOM_OGENGINE_INTEGRATION_H

#ifdef __cplusplus
extern "C" {
#endif

/** Call once at end of idGameLocal::Init(). Loads oasisstar.json, initialises
 *  the STAR API client, registers the "star" console command. */
void D3Doom_STAR_Init(void);

/** Call once at start of idGameLocal::Shutdown(). Flushes queued jobs, saves
 *  session to oasisstar.json, shuts down the STAR API client. */
void D3Doom_STAR_Cleanup(void);

/** Call every frame from idGameLocal::RunFrame(). Drives star_sync callbacks
 *  on the main thread, consumes mint/error results, and polls console logs. */
void D3Doom_STAR_Tick(void);

/**
 * Call from idPlayer::GiveInventoryItem() when ITEM_GIVE_UPDATE_STATE is set.
 *
 * @param inv_name       Value of item's "inv_name" spawnArg (e.g. "Blue Key").
 * @param inv_classname  Entity classname (e.g. "item_key_blue"). May be NULL.
 * @param is_carry_item  1 when item has "inv_carry 1" (keycards, PDAs). 0 otherwise.
 */
void D3Doom_STAR_OnItemPickup(const char* inv_name,
                               const char* inv_classname,
                               int         is_carry_item);

/**
 * Call from idGameLocal::RequirementMet() when FindInventoryItem() returns NULL.
 * Checks STAR cross-game inventory for the required item.
 *
 * @param required_inv_name  Value of door's "requires" spawnArg (inv_name of key).
 * @return                   1 if STAR had the item and it was used; 0 otherwise.
 */
int D3Doom_STAR_CheckDoorAccess(const char* required_inv_name);

/**
 * Call from idAI::Killed(). Reports kill to STAR (XP, optional NFT mint).
 * No-op if the entity def name is not in the monster table.
 *
 * @param entity_def_name  Result of GetEntityDefName() on the killed AI.
 * @param engine_is_boss   1 if the engine already classified this as a boss kill.
 */
void D3Doom_STAR_OnMonsterKilled(const char* entity_def_name, int engine_is_boss);

/* -----------------------------------------------------------------------
 * HUD / GUI state queries
 * -------------------------------------------------------------------- */

/** Returns 1 if the player is currently beamed in to OASIS. */
int D3Doom_STAR_IsBeamedIn(void);

/** Returns the authenticated OASIS username, or "" if not beamed in. */
const char* D3Doom_STAR_GetUsername(void);

/** Returns the player's current OASIS XP total. */
int D3Doom_STAR_GetXP(void);

/** Returns the integration version string, e.g. "ODOOM3-BFG 1.0.0". */
const char* D3Doom_STAR_GetVersionString(void);

/* -----------------------------------------------------------------------
 * Popup state
 * -------------------------------------------------------------------- */

/** Returns 1 if the OASIS Inventory popup is currently open. */
int D3Doom_STAR_IsInventoryPopupOpen(void);

/** Returns 1 if the OASIS Quest popup is currently open. */
int D3Doom_STAR_IsQuestPopupOpen(void);

/** Toggle the Inventory popup (bind to I key in idKeyInput / idUserCmdGen). */
void D3Doom_STAR_ToggleInventoryPopup(void);

/** Toggle the Quest popup (bind to Q key). */
void D3Doom_STAR_ToggleQuestPopup(void);

/**
 * Returns 1 when game input (movement / attack) should be blocked
 * because an OASIS popup is open.  Check in idPlayer::Think() or
 * idGameLocal::RunFrame() before applying user commands.
 */
int D3Doom_STAR_ShouldBlockInput(void);

/**
 * Returns 1 when the OASIS avatar image should replace the player's PDA
 * / status portrait.  Tie to the beamed-in HUD face element in the Doom 3 GUI.
 */
int D3Doom_STAR_ShouldUseAvatarFace(void);

/* -----------------------------------------------------------------------
 * HUD drawing
 *
 * Call from idGameLocal::Draw() or the HUD GUI script render path.
 * render_system is cast to idRenderSystem* inside the implementation;
 * pass NULL to use the global ::renderSystem pointer.
 * -------------------------------------------------------------------- */

/** Draw beamed-in label, XP counter, version string, and toast notifications. */
void D3Doom_STAR_DrawHUDStatus(void* render_system);

/** Draw full-screen Inventory or Quest popup over the 3-D scene. */
void D3Doom_STAR_DrawPopupOverlay(void* render_system);

/* -----------------------------------------------------------------------
 * Input handling
 * -------------------------------------------------------------------- */

/**
 * Forward key events to the OASIS integration layer.
 * Call from idKeyInput::ExecKeyBinding() or idUsercmdGen::Key() before
 * normal game key processing.
 *
 * @param key   idKeyNum value (e.g. K_I, K_Q, K_ESCAPE).
 * @param down  1 = pressed, 0 = released.
 */
void D3Doom_STAR_HandleKey(int key, int down);
void D3Doom_STAR_CheckIncomingTeleport(void);

#ifdef __cplusplus
}
#endif

#endif /* D3DOOM_OGENGINE_INTEGRATION_H */
