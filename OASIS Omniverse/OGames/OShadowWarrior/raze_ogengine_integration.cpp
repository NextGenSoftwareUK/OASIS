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

void Raze_STAR_Tick(void)
{
	oglib_game_tick();
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
