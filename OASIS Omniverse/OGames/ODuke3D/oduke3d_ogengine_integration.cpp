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
#include "duke3d.h"

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

/*
 * OASIS Omniverse Hub - protocol lives in ogengine_hub_frame (OGEngineClient); see
 * Docs/OMNIVERSE_HUB_IPC.md. The main loop keeps running while paused, so the Hub can
 * pause and unpause. Arrivals load via the console "map" command, then move the player.
 */
static char g_hub_pending_map[64];
static int32_t g_hub_pending_pos[3];
static bool g_hub_pending_spawn = false;

/* "maps/E1L1.MAP" -> "E1L1": the name the console "map" command accepts. */
static void ODuke_HubCurrentMap(char* out, size_t size)
{
    const char* name = currentboardfilename;
    for (const char* p = currentboardfilename; *p; ++p)
        if (*p == '/' || *p == '\\') name = p + 1;
    Bstrncpyz(out, name, size);
    char* dot = strrchr(out, '.');
    if (dot && !Bstrcasecmp(dot, ".map")) *dot = 0;
}

/* Same steps as the in-game pause key (sector.cpp). */
static void ODuke_HubSetPaused(bool pause)
{
    ud.pause_on = pause ? 1 : 0;
    if (pause)
    {
        if (ud.recstat != 2) S_PauseMusic(true);
        S_PauseSounds(true);
    }
    else
    {
        if (ud.config.MusicToggle) S_PauseMusic(false);
        S_PauseSounds(false);
    }
}

static void ODuke_HubApplyPendingSpawn(void)
{
    if (!g_hub_pending_spawn) return;
    auto* ps = g_player[myconnectindex].ps;
    if (!ps || !(ps->gm & MODE_GAME)) return;
    char current[64];
    ODuke_HubCurrentMap(current, sizeof(current));
    if (g_hub_pending_map[0] && Bstrcasecmp(current, g_hub_pending_map) != 0) return;
    if (g_hub_pending_pos[0] || g_hub_pending_pos[1] || g_hub_pending_pos[2])
    {
        vec3_t pos = { g_hub_pending_pos[0], g_hub_pending_pos[1], g_hub_pending_pos[2] };
        actor[ps->i].bpos = ps->opos = ps->pos = pos;
        sprite[ps->i].x = pos.x;
        sprite[ps->i].y = pos.y;
        updatesector(pos.x, pos.y, &ps->cursectnum);
        changespritesect(ps->i, ps->cursectnum);
    }
    g_hub_pending_spawn = false;
    g_hub_pending_map[0] = 0;
}

static void ODuke_HubFrame(void)
{
    ODuke_HubApplyPendingSpawn();
    char current[64];
    ODuke_HubCurrentMap(current, sizeof(current));
    ogengine_hub_frame_t hub;
    if (!ogengine_hub_frame("ODuke3D", current, ud.pause_on ? 1 : 0, &hub)) return;

    if (hub.pause_change > 0 && !ud.pause_on) ODuke_HubSetPaused(true);
    else if (hub.pause_change < 0 && ud.pause_on) ODuke_HubSetPaused(false);

    if (hub.has_arrive)
    {
        g_hub_pending_pos[0] = (int32_t)hub.x;
        g_hub_pending_pos[1] = (int32_t)hub.y;
        g_hub_pending_pos[2] = (int32_t)hub.z;
        g_hub_pending_spawn = true;
        Bstrncpyz(g_hub_pending_map, hub.arrive_map, sizeof(g_hub_pending_map));
        if (hub.arrive_map[0])
        {
            char cmd[96];
            Bsnprintf(cmd, sizeof(cmd), "map %s", hub.arrive_map);
            OSD_Dispatch(cmd);
        }
        OSD_Printf("[OASIS] Hub arrive: map=%s pos=%.0f/%.0f/%.0f\n", hub.arrive_map[0] ? hub.arrive_map : "(current)", hub.x, hub.y, hub.z);
    }
}

void ODuke3D_STAR_Tick(void)
{
    oglib_game_tick();
    ODuke_HubFrame();
}

void ODuke3D_STAR_OnKills(int count)
{
    for (int i = 0; i < count; i++)
        oglib_game_on_kill("DukeEnemy");
}

#endif /* OASIS_STAR_API */
