/**
 * OASIS integration for Raze, built on the shared OGLib game core (oglib_game.h),
 * which implements the ODOOM/OQuake pattern: oasisstar.json, saved session,
 * offline sync (edge), beam-in/out, kills and the "star" console command.
 *
 * Copied into <raze>/source/core/ by the OShadowWarrior / OBlood / OExhumed build
 * scripts (OGames is the source of truth).
 */
#include "raze_ogengine_integration.h"

#define OGLIB_GAME_IMPL
#define OGLIB_CONFIG_IMPL
#include "oasis/oglib_game.h"

#include <cstdlib>

#include "c_dispatch.h"
#include "coreactor.h"
#include "gamecontrol.h"
#include "gamestate.h"
#include "mapinfo.h"
#include "printf.h"

static const char* Raze_GameSource(void)
{
	if (isBlood()) return "OBLOOD";
	if (isExhumed()) return "OEXHUMED";
	if (isSWALL()) return "OSHADOWWARRIOR";
	if (isRR()) return "OREDNECKRAMPAGE";
	if (isNam()) return "ONAM";
	if (isWW2GI()) return "OWW2GI";
	return "ODUKE3D";
}

static const char* Raze_DisplayName(void)
{
	if (isBlood()) return "OBlood";
	if (isExhumed()) return "OExhumed";
	if (isSWALL()) return "OShadowWarrior";
	if (isRR()) return "ORedneckRampage";
	if (isNam()) return "ONam";
	if (isWW2GI()) return "OWW2GI";
	return "ODuke3D";
}

/* Exhumed reports kills by monster name (see Raze_STAR_OnKillName calls in games/exhumed). */
static const oglib_game_monster_t kRazeMonsters[] = {
	{ "Anubis", "Anubis Zombie", 30, 0 },
	{ "Fish", "Piranha", 10, 0 },
	{ "LavaDude", "Magmantis", 35, 0 },
	{ "Lion", "Am-mit", 25, 0 },
	{ "Mummy", "Mummy", 20, 0 },
	{ "Rex", "Bastet", 40, 0 },
	{ "Roach", "Scarab", 10, 0 },
	{ "Scorp", "Selkis", 300, 1 },
	{ "Set", "Set", 400, 1 },
	{ "Spider", "Spider", 10, 0 },
	{ "Wasp", "Wasp", 15, 0 },
	{ "Queen", "Kilmaatikahn", 500, 1 },
	{ nullptr, nullptr, 0, 0 },
};

static void Raze_Print(const char* line, void* /*user*/)
{
	Printf("%s\n", line);
}

void Raze_STAR_Init(void)
{
	static bool started = false;
	if (started) return;
	started = true;

	oglib_game_desc_t desc = {};
	desc.game_source = Raze_GameSource();
	desc.display_name = Raze_DisplayName();
	desc.config_path = "oasisstar.json";
	desc.print = Raze_Print;
	desc.monsters = kRazeMonsters;
	if (oglib_game_init(&desc))
		std::atexit([] { oglib_game_shutdown(); });
}

/*
 * OASIS Omniverse Hub - protocol lives in ogengine_hub_frame (OGEngineClient); see
 * Docs/OMNIVERSE_HUB_IPC.md. Raze's main loop keeps running while paused, so the Hub
 * can pause and unpause. Arrivals use Raze's own "map" and "warptocoords" commands.
 */
static FString g_hub_pending_map;
static float g_hub_pending_pos[3];
static bool g_hub_pending_spawn = false;

static void Raze_HubApplyPendingSpawn(void)
{
	if (!g_hub_pending_spawn || gamestate != GS_LEVEL || !currentLevel) return;
	if (g_hub_pending_map.IsNotEmpty() && g_hub_pending_map.CompareNoCase(currentLevel->labelName) != 0) return;
	if (g_hub_pending_pos[0] != 0 || g_hub_pending_pos[1] != 0 || g_hub_pending_pos[2] != 0)
		C_DoCommand(FStringf("warptocoords %f %f %f", g_hub_pending_pos[0], g_hub_pending_pos[1], g_hub_pending_pos[2]).GetChars());
	g_hub_pending_spawn = false;
	g_hub_pending_map = "";
}

static void Raze_HubFrame(void)
{
	Raze_HubApplyPendingSpawn();
	const char* mapName = (gamestate == GS_LEVEL && currentLevel) ? currentLevel->labelName.GetChars() : "";
	ogengine_hub_frame_t hub;
	if (!ogengine_hub_frame(Raze_DisplayName(), mapName, paused ? 1 : 0, &hub)) return;

	if ((hub.pause_change > 0 && !paused) || (hub.pause_change < 0 && paused))
		C_DoCommand("pause");

	if (hub.has_arrive) {
		g_hub_pending_pos[0] = hub.x; g_hub_pending_pos[1] = hub.y; g_hub_pending_pos[2] = hub.z;
		g_hub_pending_spawn = true;
		g_hub_pending_map = hub.arrive_map;
		if (hub.arrive_map[0])
			C_DoCommand(FStringf("map %s", hub.arrive_map).GetChars());
		Printf("[OASIS] Hub arrive: map=%s pos=%.0f/%.0f/%.0f\n", hub.arrive_map[0] ? hub.arrive_map : "(current)", hub.x, hub.y, hub.z);
	}
}

void Raze_STAR_Tick(void)
{
	oglib_game_tick();
	Raze_HubFrame();
}

void Raze_STAR_OnKill(DCoreActor* killed)
{
	if (killed)
		oglib_game_on_kill(killed->GetClass()->TypeName.GetChars());
}

void Raze_STAR_OnKillName(const char* name)
{
	oglib_game_on_kill(name);
}

/* star beamin <user> <pass> | beamout | status | inventory | offline <status|on|off|sync-and-off> | debug <on|off> */
CCMD(star)
{
	FString args;
	for (int i = 1; i < argv.argc(); i++)
	{
		if (i > 1) args += ' ';
		args += argv[i];
	}
	oglib_game_command(args.GetChars());
}
