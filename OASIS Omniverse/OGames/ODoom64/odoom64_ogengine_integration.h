#ifndef ODOOM64_OGENGINE_INTEGRATION_H
#define ODOOM64_OGENGINE_INTEGRATION_H
/**
 * OASIS integration for DOOM 64 EX+ (ODoom64), built on OGLib/oglib_game.h.
 * Compiled only with OASIS_STAR_API (MSBuild /p:OasisStarApi=true or CMake
 * -DOASIS_STAR_API=ON); the engine hooks are #ifdef OASIS_STAR_API.
 */

void ODoom64_STAR_Init(void);              /* d_main.c D_DoomMain, before D_DoomLoop() */
void ODoom64_STAR_Tick(void);              /* d_main.c D_MiniLoop, once per frame */
void ODoom64_STAR_OnKill(int mobj_type);   /* p_inter.c P_KillMobj, player's counted kills */

#endif
