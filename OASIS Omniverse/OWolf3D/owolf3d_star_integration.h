/*
** owolf3d_star_integration.h
** OWolf3D — OASIS STAR API Integration (ECWolf C++ hook layer)
**
** Drop this file (and owolf3d_star_integration.cpp) into the ECWolf source
** tree (src/), add it to CMakeLists.txt, and place the six hook calls listed
** in Docs/INTEGRATION_INSTRUCTIONS.md.
*/

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/* ── Lifecycle ─────────────────────────────────────────────────────────── */
void OWolf3D_STAR_Init(void);
void OWolf3D_STAR_Cleanup(void);
void OWolf3D_STAR_Tick(void);

/* ── Game events ────────────────────────────────────────────────────────── */
/* classname : ECWolf DECORATE class name (e.g. "Guard", "Hans", "Hitler")  */
/* is_boss   : 1 if the actor has the +AMBUSH flag (Wolf3D boss convention) */
void OWolf3D_STAR_OnActorKilled(const char *classname, int is_boss);

/* classname : DECORATE class name of the inventory item                     */
/* inv_name  : human-readable name (or empty string if unavailable)          */
void OWolf3D_STAR_OnItemPickup(const char *classname, const char *inv_name);

/* keynum    : Wolf3D LOCKDEFS lock number (1 = GoldKey, 2 = SilverKey)     */
/* Returns 1 if STAR provides this key (from a cross-game session), 0 otherwise */
int  OWolf3D_STAR_CheckDoorAccess(int keynum);

/* ── Query ──────────────────────────────────────────────────────────────── */
int         OWolf3D_STAR_IsBeamedIn(void);
const char *OWolf3D_STAR_GetUsername(void);
int         OWolf3D_STAR_GetXP(void);
const char *OWolf3D_STAR_GetVersionString(void);

/* ── GUI state ──────────────────────────────────────────────────────────── */
int  OWolf3D_STAR_IsInventoryPopupOpen(void);
int  OWolf3D_STAR_IsQuestPopupOpen(void);
void OWolf3D_STAR_ToggleInventoryPopup(void);
void OWolf3D_STAR_ToggleQuestPopup(void);
int  OWolf3D_STAR_ShouldBlockInput(void);
int  OWolf3D_STAR_ShouldUseAvatarFace(void);

/* ── HUD / draw (call from WolfStatusBar::DrawStatusBar) ───────────────── */
void OWolf3D_STAR_DrawHUDStatus(void);      /* beamed-in, XP, version, toasts */
void OWolf3D_STAR_DrawPopupOverlay(void);   /* inventory / quest full-screen popup */

/* ── Input (call from CheckKeys in wl_play.cpp) ────────────────────────── */
/* Reads Keyboard[] directly — pass nothing, returns nothing.               */
void OWolf3D_STAR_HandleKeys(void);

#ifdef __cplusplus
}
#endif
