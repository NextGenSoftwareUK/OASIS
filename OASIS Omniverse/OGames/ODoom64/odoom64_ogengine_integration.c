/**
 * OASIS integration for DOOM 64 EX+ on the shared OGLib game core (oglib_game.h):
 * oasisstar.json, saved session, offline sync, beam-in/out, kill XP and the
 * "star" console command — the ODOOM/OQuake pattern.
 *
 * Copied into <doom64>/src/engine/ by BUILD_ODOOM64 (OGames is the source of truth).
 */
#include "odoom64_ogengine_integration.h"

#define OGLIB_GAME_IMPL
#define OGLIB_CONFIG_IMPL
#include "oasis/oglib_game.h"

#include <stdlib.h>
#include <string.h>

#include "doomdef.h"
#include "info.h"
#include "con_console.h"
#include "g_actions.h"

static int g_odoom64_started = 0;

/* Display names keyed by DOOM 64 monster type; bosses mint an NFT. */
static const oglib_game_monster_t kDoom64Monsters[] = {
	{ "Zombieman", "Zombieman", 10, 0 },
	{ "ShotgunGuy", "Shotgun Guy", 15, 0 },
	{ "Imp", "Imp", 20, 0 },
	{ "NightmareImp", "Nightmare Imp", 30, 0 },
	{ "Demon", "Demon", 25, 0 },
	{ "Spectre", "Spectre", 30, 0 },
	{ "LostSoul", "Lost Soul", 10, 0 },
	{ "Cacodemon", "Cacodemon", 50, 0 },
	{ "PainElemental", "Pain Elemental", 45, 0 },
	{ "HellKnight", "Hell Knight", 80, 0 },
	{ "BaronOfHell", "Baron of Hell", 150, 1 },
	{ "Mancubus", "Mancubus", 90, 0 },
	{ "Arachnotron", "Arachnotron", 80, 0 },
	{ "Cyberdemon", "Cyberdemon", 1000, 1 },
	{ "MotherDemon", "Mother Demon", 1500, 1 },
	{ NULL, NULL, 0, 0 },
};

static const char* ODoom64_MonsterName(int type)
{
	switch (type) {
	case MT_POSSESSED1: return "Zombieman";
	case MT_POSSESSED2: return "ShotgunGuy";
	case MT_IMP1: return "Imp";
	case MT_IMP2: return "NightmareImp";
	case MT_DEMON1: return "Demon";
	case MT_DEMON2: return "Spectre";
	case MT_SKULL: return "LostSoul";
	case MT_CACODEMON: return "Cacodemon";
	case MT_PAIN: return "PainElemental";
	case MT_BRUISER1: return "BaronOfHell";
	case MT_BRUISER2: return "HellKnight";
	case MT_MANCUBUS: return "Mancubus";
	case MT_BABY: return "Arachnotron";
	case MT_CYBORG: return "Cyberdemon";
	case MT_RESURRECTOR: return "MotherDemon";
	default: return NULL;
	}
}

static void ODoom64_Print(const char* line, void* user)
{
	(void)user;
	CON_Printf(WHITE, "%s\n", line);
}

/* star beamin <user> <pass> | beamout | status | inventory | offline <...> | debug <on|off> */
static CMD(Star)
{
	char args[512] = "";
	int i;
	(void)data;
	for (i = 0; param[i]; i++) {
		if (i) strncat(args, " ", sizeof(args) - strlen(args) - 1);
		strncat(args, param[i], sizeof(args) - strlen(args) - 1);
	}
	oglib_game_command(args);
}

static void ODoom64_Shutdown(void)
{
	oglib_game_shutdown();
}

void ODoom64_STAR_Init(void)
{
	oglib_game_desc_t desc;
	if (g_odoom64_started) return;
	g_odoom64_started = 1;

	memset(&desc, 0, sizeof(desc));
	desc.game_source = "ODOOM64";
	desc.display_name = "ODoom64";
	desc.config_path = "oasisstar.json";
	desc.print = ODoom64_Print;
	desc.monsters = kDoom64Monsters;
	G_AddCommand("star", CMD_Star, 0);
	if (oglib_game_init(&desc))
		atexit(ODoom64_Shutdown);
}

void ODoom64_STAR_Tick(void)
{
	oglib_game_tick();
}

void ODoom64_STAR_OnKill(int mobj_type)
{
	const char* name = ODoom64_MonsterName(mobj_type);
	if (name) oglib_game_on_kill(name);
}
