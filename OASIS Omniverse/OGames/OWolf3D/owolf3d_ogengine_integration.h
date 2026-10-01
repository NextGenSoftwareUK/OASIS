#pragma once
/**
 * OASIS integration for ECWolf (OWolf3D), built on OGLib/oglib_game.h.
 * Built only when CMake option OASIS_STAR_API is ON; engine hooks are #ifdef OASIS_STAR_API.
 */

void OWolf3D_STAR_Init(void);                      /* wl_main.cpp, after InitGame() */
void OWolf3D_STAR_Tick(void);                      /* wl_play.cpp PlayLoop, once per frame */
void OWolf3D_STAR_OnKill(const char* class_name);  /* actor.cpp AActor::Die, counted kills */
/** ECWolf has no console: "--star <command>" on the command line queues e.g. "beamin user pass". */
void OWolf3D_STAR_QueueCommand(const char* command);
