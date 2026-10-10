/**
 * OASIS integration for ECWolf (OWolf3D) on the shared OGLib game core
 * (oglib_game.h): oasisstar.json, saved session, offline sync, beam-in/out
 * and kill XP — the ODOOM/OQuake pattern.
 *
 * ECWolf has no drop-down console, so OASIS commands come from the command
 * line: --star "beamin <user> <pass>", --star "offline on", --star "status" ...
 * After one beam-in the session is saved in oasisstar.json and restored on
 * every later launch.
 *
 * Copied into <ecwolf>/src/ by BUILD_OWOLF3D (OGames is the source of truth).
 */
#include "owolf3d_ogengine_integration.h"

#define OGLIB_GAME_IMPL
#define OGLIB_CONFIG_IMPL
#include "oasis/oglib_game.h"

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <string>
#include <vector>

#include "wl_def.h"
#include "actor.h"
#include "gamemap.h"
#include "wl_agent.h"
#include "wl_game.h"
#include "wl_play.h"

static bool g_owolf_started = false;
static std::vector<std::string> g_owolf_pending;

/* ECWolf actor class names (Wolfenstein 3D and Spear of Destiny). Bosses mint an NFT. */
static const oglib_game_monster_t kWolfMonsters[] = {
	{ "Guard", "Guard", 10, 0 },
	{ "Dog", "Dog", 5, 0 },
	{ "SS", "SS Guard", 20, 0 },
	{ "Mutant", "Mutant", 25, 0 },
	{ "Officer", "Officer", 30, 0 },
	{ "HansGrosse", "Hans Grosse", 300, 1 },
	{ "DrSchabbs", "Dr. Schabbs", 350, 1 },
	{ "FakeHitler", "Fake Hitler", 150, 1 },
	{ "MechaHitler", "Mecha Hitler", 400, 1 },
	{ "Hitler", "Adolf Hitler", 500, 1 },
	{ "Giftmacher", "Otto Giftmacher", 350, 1 },
	{ "Gretel", "Gretel Grosse", 350, 1 },
	{ "FatFace", "General Fettgesicht", 400, 1 },
	{ "TransGrosse", "Trans Grosse", 300, 1 },
	{ "UberMutant", "Uber Mutant", 350, 1 },
	{ "DeathKnight", "Death Knight", 400, 1 },
	{ "Wilhelm", "Barnacle Wilhelm", 350, 1 },
	{ "AngelOfDeath", "Angel of Death", 600, 1 },
	{ nullptr, nullptr, 0, 0 },
};

static void OWolf_Print(const char* line, void* /*user*/)
{
	printf("%s\n", line);
	fflush(stdout);
}

void OWolf3D_STAR_QueueCommand(const char* command)
{
	if (!command || !command[0]) return;
	if (g_owolf_started) oglib_game_command(command);
	else g_owolf_pending.emplace_back(command);
}

void OWolf3D_STAR_Init(void)
{
	if (g_owolf_started) return;
	g_owolf_started = true;

	oglib_game_desc_t desc = {};
	desc.game_source = "OWOLF3D";
	desc.display_name = "OWolf3D";
	desc.config_path = "oasisstar.json";
	desc.print = OWolf_Print;
	desc.monsters = kWolfMonsters;
	if (oglib_game_init(&desc))
		std::atexit([] { oglib_game_shutdown(); });

	for (const std::string& cmd : g_owolf_pending)
		oglib_game_command(cmd.c_str());
	g_owolf_pending.clear();
}

/*
 * OASIS Omniverse Hub - protocol lives in ogengine_hub_frame (OGEngineClient); see
 * Docs/OMNIVERSE_HUB_IPC.md. No Hub pause: ECWolf's pause blocks the play loop that
 * runs this tick, so the Hub could never unpause it.
 */
static char g_hub_pending_map[9];
static float g_hub_pending_x = 0, g_hub_pending_y = 0;
static bool g_hub_pending_spawn = false;

static void OWolf_HubApplyPendingSpawn(void)
{
	if (!g_hub_pending_spawn) return;
	AActor* mo = players[ConsolePlayer].mo;
	if (!mo) return;
	if (g_hub_pending_map[0] && stricmp(gamestate.mapname, g_hub_pending_map) != 0) return;
	if (g_hub_pending_x != 0 || g_hub_pending_y != 0)
		mo->Teleport(FLOAT2FIXED(g_hub_pending_x), FLOAT2FIXED(g_hub_pending_y), mo->angle, true);
	g_hub_pending_spawn = false;
	g_hub_pending_map[0] = 0;
}

static void OWolf_HubFrame(void)
{
	ogengine_hub_frame_t hub;
	OWolf_HubApplyPendingSpawn();
	if (!ogengine_hub_frame("OWolf3D", gamestate.mapname, 0, &hub) || !hub.has_arrive) return;

	g_hub_pending_x = hub.x;
	g_hub_pending_y = hub.y;
	g_hub_pending_spawn = true;
	g_hub_pending_map[0] = 0;
	/* ECWolf map lumps are at most 8 characters; warp the same way the debug "warp" does. */
	if (hub.arrive_map[0]) {
		if (strlen(hub.arrive_map) <= 8 && GameMap::CheckMapExists(hub.arrive_map)) {
			strncpy(g_hub_pending_map, hub.arrive_map, 8);
			g_hub_pending_map[8] = 0;
			strncpy(gamestate.mapname, hub.arrive_map, 8);
			gamestate.mapname[8] = 0;
			playstate = ex_warped;
		} else {
			printf("[OWolf3D] Hub arrive: map '%s' not found\n", hub.arrive_map);
			g_hub_pending_spawn = false;
		}
	}
}

void OWolf3D_STAR_Tick(void)
{
	oglib_game_tick();
	OWolf_HubFrame();
}

void OWolf3D_STAR_OnKill(const char* class_name)
{
	oglib_game_on_kill(class_name);
}
