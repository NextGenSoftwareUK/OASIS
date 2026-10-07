#pragma once
/**
 * OASIS integration for EDuke32 (ODuke3D), built on OGLib/oglib_game.h.
 * Compiled only with OASIS_STAR_API (MSBuild /p:OasisStarApi=true or
 * make OASIS_STAR_API=1); the engine hooks are #ifdef OASIS_STAR_API.
 */

void ODuke3D_STAR_Init(void);          /* game.cpp app_main, before EVENT_INITCOMPLETE */
void ODuke3D_STAR_Tick(void);          /* game.cpp main loop, once per frame */
void ODuke3D_STAR_OnKills(int count);  /* player.cpp P_AddKills */
