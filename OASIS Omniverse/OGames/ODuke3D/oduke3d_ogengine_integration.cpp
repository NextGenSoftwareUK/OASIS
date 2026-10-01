/**
 * OASIS integration for EDuke32 (ODuke3D) on the shared OGLib game core
 * (oglib_game.h): oasisstar.json, saved session, offline sync, beam-in/out,
 * kill XP and the "star" console command — the ODOOM/OQuake pattern.
 *
 * EDuke32 counts kills through P_AddKills (mostly from CON scripts) without the
 * monster type, so kills are reported as "Duke enemy" with the default XP.
 *
 * EDuke32's makefile compiles every .cpp here, so the whole file is guarded.
 * Copied into <eduke32>/source/duke3d/src/ by BUILD_ODUKE3D (OGames is the source of truth).
 */
#ifdef OASIS_STAR_API

#include "oduke3d_ogengine_integration.h"

#define OGLIB_GAME_IMPL
#define OGLIB_CONFIG_IMPL
#include "oasis/oglib_game.h"

#include <cstdlib>
#include <cstring>

#include "osd.h"

static bool g_oduke_started = false;

static void ODuke_Print(const char* line, void* /*user*/)
{
    OSD_Printf("%s\n", line);
}

/* star beamin <user> <pass> | beamout | status | inventory | offline <...> | debug <on|off> */
static int osdcmd_star(osdcmdptr_t parm)
{
    char args[512] = "";
    for (int i = 0; i < parm->numparms; i++)
    {
        if (i) strncat(args, " ", sizeof(args) - strlen(args) - 1);
        strncat(args, parm->parms[i], sizeof(args) - strlen(args) - 1);
    }
    oglib_game_command(args);
    return OSDCMD_OK;
}

void ODuke3D_STAR_Init(void)
{
    if (g_oduke_started) return;
    g_oduke_started = true;

    OSD_RegisterFunction("star", "star <beamin|beamout|status|inventory|offline|debug>: OASIS commands", osdcmd_star);

    oglib_game_desc_t desc = {};
    desc.game_source = "ODUKE3D";
    desc.display_name = "ODuke3D";
    desc.config_path = "oasisstar.json";
    desc.print = ODuke_Print;
    if (oglib_game_init(&desc))
        std::atexit([] { oglib_game_shutdown(); });
}

void ODuke3D_STAR_Tick(void)
{
    oglib_game_tick();
}

void ODuke3D_STAR_OnKills(int count)
{
    for (int i = 0; i < count; i++)
        oglib_game_on_kill("DukeEnemy");
}

#endif /* OASIS_STAR_API */
