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
#include <string>
#include <vector>

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

void OWolf3D_STAR_Tick(void)
{
	oglib_game_tick();
}

void OWolf3D_STAR_OnKill(const char* class_name)
{
	oglib_game_on_kill(class_name);
}
