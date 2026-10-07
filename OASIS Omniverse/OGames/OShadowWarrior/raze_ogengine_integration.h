#pragma once
/**
 * OASIS integration for Raze (Shadow Warrior, Blood, Exhumed/PowerSlave, Duke Nukem 3D,
 * Redneck Rampage, NAM, WW2GI). One integration for every Raze game, like ODOOM for UZDoom.
 * Built only when CMake option OASIS_STAR_API is ON; engine hooks are #ifdef OASIS_STAR_API.
 */

class DCoreActor;

void Raze_STAR_Init(void);                  /* gamecontrol.cpp RunGame(), before MainLoop() */
void Raze_STAR_Tick(void);                  /* mainloop.cpp MainLoop(), after TryRunTics() */
void Raze_STAR_OnKill(DCoreActor* killed);  /* kill sites that have the actor */
void Raze_STAR_OnKillName(const char* name);/* kill sites that only know the monster name */
