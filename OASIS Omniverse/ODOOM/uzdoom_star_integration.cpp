/**
 * ODOOM - OASIS STAR API Integration Implementation
 *
 * Build this file as part of ODOOM (UZDoom) with STAR_API_DIR pointing to OASIS NativeWrapper.
 * Keycard pickups are reported to STAR; door/lock checks can use cross-game inventory.
 * In-game console: "star" command for testing (star version, star inventory, star add, etc.).
 */

#include "uzdoom_star_integration.h"
#include "star_api.h"
#include "odoom_branding.h"

#include <cstdlib>
#include <cstdio>
#include <cstring>
#include <cstdarg>

/* ODOOM (UZDoom) headers for key detection */
#include "gamedata/a_keys.h"
#include "playsim/actor.h"
#include "gamedata/info.h"
#include "vm.h"
#include "c_dispatch.h"
#include "c_console.h"
#include "printf.h"

static star_api_config_t g_star_config;
static bool g_star_initialized = false;
static bool g_star_client_ready = false;
static bool g_star_debug_logging = true;
static bool g_star_logged_runtime_auth_failure = false;
static bool g_star_logged_missing_auth_config = false;
static const int STAR_PICKUP_OQUAKE_GOLD_KEY = 5005;
static const int STAR_PICKUP_OQUAKE_SILVER_KEY = 5013;

static void StarLogInfo(const char* fmt, ...) {
	char msg[1024];
	va_list args;
	va_start(args, fmt);
	vsnprintf(msg, sizeof(msg), fmt, args);
	va_end(args);
	std::printf("STAR API: %s\n", msg);
	Printf(PRINT_NONOTIFY, TEXTCOLOR_GREEN "STAR API: %s\n", msg);
}

static void StarLogError(const char* fmt, ...) {
	char msg[1024];
	va_list args;
	va_start(args, fmt);
	vsnprintf(msg, sizeof(msg), fmt, args);
	va_end(args);
	std::printf("STAR API ERROR: %s\n", msg);
	Printf(PRINT_NONOTIFY, TEXTCOLOR_RED "STAR API ERROR: %s\n", msg);
}

static void StarLogRuntimeAuthFailureOnce(const char* reason) {
	if (g_star_logged_runtime_auth_failure) return;
	g_star_logged_runtime_auth_failure = true;
	StarLogError("Not authenticated. STAR sync disabled until beam-in succeeds. Reason: %s", reason ? reason : "(unknown)");
}

static bool HasValue(const char* s) {
	return s && s[0] != '\0';
}

static bool StarTryInitializeAndAuthenticate(bool verbose) {
	const bool logVerbose = verbose;
	if (g_star_initialized) return true;

	g_star_config.base_url = "https://star-api.oasisplatform.world/api";
	g_star_config.api_key = getenv("STAR_API_KEY");
	g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
	g_star_config.timeout_seconds = 10;
	if (logVerbose) {
		StarLogInfo(
			"Init/auth start (base_url=%s, has_api_key=%s, has_avatar_id=%s, has_username=%s, has_password=%s)",
			g_star_config.base_url ? g_star_config.base_url : "(null)",
			HasValue(g_star_config.api_key) ? "yes" : "no",
			HasValue(g_star_config.avatar_id) ? "yes" : "no",
			HasValue(getenv("STAR_USERNAME")) ? "yes" : "no",
			HasValue(getenv("STAR_PASSWORD")) ? "yes" : "no"
		);
	}

	if (!g_star_client_ready) {
		if (logVerbose) StarLogInfo("Calling star_api_init...");
		star_api_result_t init_result = star_api_init(&g_star_config);
		if (init_result != STAR_API_SUCCESS) {
			if (logVerbose) StarLogError("star_api_init failed: %s", star_api_get_last_error());
			return false;
		}
		g_star_client_ready = true;
		if (logVerbose) StarLogInfo("star_api_init succeeded (interop DLL/API ready).");
	}

	const char* username = getenv("STAR_USERNAME");
	const char* password = getenv("STAR_PASSWORD");
	if (HasValue(username) && HasValue(password)) {
		if (logVerbose) StarLogInfo("Beaming in... authenticating avatar via SSO.");
		star_api_result_t auth = star_api_authenticate(username, password);
		if (auth == STAR_API_SUCCESS) {
			g_star_initialized = true;
			g_star_logged_runtime_auth_failure = false;
			g_star_logged_missing_auth_config = false;
			if (logVerbose) StarLogInfo("Beam-in successful (SSO). Cross-game features enabled.");
			return true;
		}
		if (logVerbose) StarLogError("Beam-in failed (SSO): %s", star_api_get_last_error());
	}

	// API key + avatar mode: verify we can access inventory for this avatar.
	if (HasValue(g_star_config.api_key) && HasValue(g_star_config.avatar_id)) {
		if (logVerbose) StarLogInfo("Beaming in... verifying API key/avatar via star_api_get_inventory.");
		star_item_list_t* list = nullptr;
		star_api_result_t inv = star_api_get_inventory(&list);
		if (inv == STAR_API_SUCCESS) {
			g_star_initialized = true;
			g_star_logged_runtime_auth_failure = false;
			g_star_logged_missing_auth_config = false;
			size_t count = list ? list->count : 0;
			if (list) star_api_free_item_list(list);
			if (logVerbose) StarLogInfo("Beam-in successful (API key/avatar). Inventory items=%zu. Cross-game features enabled.", count);
			return true;
		}
		if (list) star_api_free_item_list(list);
		if (logVerbose) StarLogError("Beam-in failed (API key/avatar verify): %s", star_api_get_last_error());
		return false;
	}

	if (logVerbose && !g_star_logged_missing_auth_config) {
		g_star_logged_missing_auth_config = true;
		StarLogError("No authentication configured. Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID.");
	}
	return false;
}

static const char* GetKeycardName(int keynum) {
	switch (keynum) {
		case 1: return "red_keycard";
		case 2: return "blue_keycard";
		case 3: return "yellow_keycard";
		case 4: return "skull_key";
		default: return nullptr;
	}
}

static const char* GetKeycardDescription(int keynum) {
	switch (keynum) {
		case 1: return "Red Keycard - Opens red doors";
		case 2: return "Blue Keycard - Opens blue doors";
		case 3: return "Yellow Keycard - Opens yellow doors";
		case 4: return "Skull Key - Opens skull-marked doors";
		default: return "Key";
	}
}

void UZDoom_STAR_Init(void) {
	StarLogInfo("STAR bootstrap: Beaming in...");
	StarTryInitializeAndAuthenticate(true);

	/* OASIS / ODOOM welcome splash in console (same style as OQuake) */
	Printf("\n");
	Printf("  ================================================\n");
	Printf("            O A S I S   O D O O M  " ODOOM_VERSION_STR "\n");
	Printf("               By NextGen World Ltd\n");
	Printf("  ================================================\n");
	Printf("\n");
	Printf("  " GAMENAME " " ODOOM_VERSION_STR "\n");
	Printf("  STAR API - Enabling full interoperable games across the OASIS Omniverse!\n");
	Printf("  Type 'star' in console for STAR commands.\n");
	Printf("\n");
	Printf("  Welcome to ODOOM!\n");
	Printf("\n");
}

void UZDoom_STAR_Cleanup(void) {
	if (g_star_client_ready) {
		star_api_cleanup();
		g_star_client_ready = false;
		g_star_initialized = false;
		StarLogInfo("Cleaned up STAR API client.");
	}
}

int UZDoom_STAR_PreTouchSpecial(struct AActor* special) {
	if (!special) return 0;
	if (!StarTryInitializeAndAuthenticate(false)) {
		StarLogRuntimeAuthFailureOnce(star_api_get_last_error());
		return 0;
	}

	// OQUAKE runtime key actors in ODOOM: report exact shared-key ids.
	static PClassActor* oqGoldKeyClass = nullptr;
	static PClassActor* oqSilverKeyClass = nullptr;
	if (!oqGoldKeyClass) oqGoldKeyClass = PClass::FindActor("OQGoldKey");
	if (!oqSilverKeyClass) oqSilverKeyClass = PClass::FindActor("OQSilverKey");
	if (oqGoldKeyClass && special->IsKindOf(oqGoldKeyClass)) {
		StarLogInfo("Pickup detected: OQGoldKey (id=%d).", STAR_PICKUP_OQUAKE_GOLD_KEY);
		return STAR_PICKUP_OQUAKE_GOLD_KEY;
	}
	if (oqSilverKeyClass && special->IsKindOf(oqSilverKeyClass)) {
		StarLogInfo("Pickup detected: OQSilverKey (id=%d).", STAR_PICKUP_OQUAKE_SILVER_KEY);
		return STAR_PICKUP_OQUAKE_SILVER_KEY;
	}

	auto kt = PClass::FindActor(NAME_Key);
	if (!kt || !special->IsKindOf(kt)) return 0;

	AActor* def = GetDefaultByType(special->GetClass());
	if (!def) return 0;

	int keynum = def->special1;
	if (keynum <= 0 || keynum > 4) return 0;
	StarLogInfo("Pickup detected: Doom key special1=%d.", keynum);

	return keynum;
}

void UZDoom_STAR_PostTouchSpecial(int keynum) {
	if (keynum <= 0) return;
	if (!StarTryInitializeAndAuthenticate(false)) {
		StarLogRuntimeAuthFailureOnce(star_api_get_last_error());
		return;
	}

	const char* name = nullptr;
	const char* desc = nullptr;
	if (keynum == STAR_PICKUP_OQUAKE_GOLD_KEY) {
		name = "gold_key";
		desc = "Gold key - Opens gold doors in OQuake";
	} else if (keynum == STAR_PICKUP_OQUAKE_SILVER_KEY) {
		name = "silver_key";
		desc = "Silver key - Opens silver doors in OQuake";
	} else if (keynum >= 1 && keynum <= 4) {
		name = GetKeycardName(keynum);
		desc = GetKeycardDescription(keynum);
	}
	if (!name || !desc) return;

	StarLogInfo("Calling star_api_add_item(name=%s, game=ODOOM, type=KeyItem)...", name);
	star_api_result_t result = star_api_add_item(name, desc, "ODOOM", "KeyItem");
	if (result == STAR_API_SUCCESS) {
		StarLogInfo("star_api_add_item success: %s added to shared inventory.", name);
	} else {
		StarLogError("star_api_add_item failed for %s: %s", name, star_api_get_last_error());
	}
}

int UZDoom_STAR_CheckDoorAccess(struct AActor* owner, int keynum, int remote) {
	if (!owner || keynum <= 0) return 0;
	if (!StarTryInitializeAndAuthenticate(false)) {
		StarLogRuntimeAuthFailureOnce(star_api_get_last_error());
		return 0;
	}

	/* 1) Check Doom keycard in cross-game inventory */
	const char* keyname = GetKeycardName(keynum);
	if (keyname && star_api_has_item(keyname)) {
		StarLogInfo("Door access granted via shared inventory key: %s", keyname);
		bool used = star_api_use_item(keyname, "odoom_door");
		if (!used) {
			StarLogError("star_api_use_item failed for %s: %s", keyname, star_api_get_last_error());
		}
		return 1;
	}

	// IMPORTANT: OQuake keys are intentionally NOT valid for ODOOM doors.
	// Gold/silver keys only open their matching doors in OQuake.

	return 0;
}

//-----------------------------------------------------------------------------
// STAR console command (star <subcmd> [args...]) for testing the STAR API
//-----------------------------------------------------------------------------
static bool StarInitialized(void) {
	return g_star_initialized;
}

CCMD(star)
{
	if (argv.argc() < 2) {
		Printf("\n");
		Printf("  Welcome to ODOOM!\n");
		Printf("\n");
		Printf(TEXTCOLOR_GREEN "STAR API console commands (ODOOM):\n");
		Printf("\n");
		Printf("  star version        - Show integration and API status\n");
		Printf("  star status         - Show init state and last error\n");
		Printf("  star inventory      - List items in STAR inventory\n");
		Printf("  star has <item>     - Check if you have an item (e.g. red_keycard)\n");
		Printf("  star add <item> [desc] [type] - Add item (e.g. star add red_keycard)\n");
		Printf("  star use <item> [context]     - Use item (e.g. star use red_keycard door)\n");
		Printf("  star quest start <id>        - Start a quest\n");
		Printf("  star quest objective <quest> <obj> - Complete quest objective\n");
		Printf("  star quest complete <id>    - Complete a quest\n");
		Printf("  star bossnft <name> [desc]   - Create boss NFT\n");
		Printf("  star deploynft <nft_id> <game> [loc] - Deploy boss NFT\n");
		Printf("  star pickup keycard <red|blue|yellow|skull> - Add keycard (convenience)\n");
		Printf("  star debug on|off|status - Toggle STAR debug logging in console\n");
		Printf("  star beamin   - Log in / authenticate (uses STAR_USERNAME/PASSWORD or API key)\n");
		Printf("  star beamout  - Log out / disconnect from STAR\n");
		Printf("\n");
		return;
	}
	const char* sub = argv[1];
	if (strcmp(sub, "pickup") == 0) {
		if (argv.argc() < 4 || strcmp(argv[2], "keycard") != 0) {
			Printf("Usage: star pickup keycard <red|blue|yellow|skull>\n");
			return;
		}
		const char* color = argv[3];
		const char* name = nullptr;
		const char* desc = nullptr;
		if (strcmp(color, "red") == 0)    { name = "red_keycard";  desc = "Red Keycard - Opens red doors"; }
		else if (strcmp(color, "blue") == 0)   { name = "blue_keycard";  desc = "Blue Keycard - Opens blue doors"; }
		else if (strcmp(color, "yellow") == 0) { name = "yellow_keycard"; desc = "Yellow Keycard - Opens yellow doors"; }
		else if (strcmp(color, "skull") == 0)  { name = "skull_key";      desc = "Skull Key - Opens skull-marked doors"; }
		else { Printf("Unknown keycard: %s. Use red|blue|yellow|skull.\n", color); return; }
		star_api_result_t r = star_api_add_item(name, desc, "ODOOM", "KeyItem");
		if (r == STAR_API_SUCCESS) Printf("Added %s to STAR inventory.\n", name);
		else Printf("Failed: %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "version") == 0) {
		Printf(TEXTCOLOR_GREEN "STAR API integration 1.0 (ODOOM)\n");
		Printf("  Initialized: %s\n", StarInitialized() ? "yes" : "no");
		if (!StarInitialized()) Printf("  Last error: %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "status") == 0) {
		Printf("STAR API initialized: %s\n", StarInitialized() ? "yes" : "no");
		Printf("STAR API client ready: %s\n", g_star_client_ready ? "yes" : "no");
		Printf("STAR debug logging: %s\n", g_star_debug_logging ? "on" : "off");
		Printf("Last error: %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "debug") == 0) {
		if (argv.argc() < 3 || strcmp(argv[2], "status") == 0) {
			Printf("STAR debug logging is %s\n", g_star_debug_logging ? "on" : "off");
			Printf("Usage: star debug on|off|status\n");
			return;
		}
		if (strcmp(argv[2], "on") == 0) {
			g_star_debug_logging = true;
			StarLogInfo("Debug logging enabled.");
			return;
		}
		if (strcmp(argv[2], "off") == 0) {
			g_star_debug_logging = false;
			Printf("STAR API: Debug logging disabled.\n");
			return;
		}
		Printf("Unknown debug option: %s. Use on|off|status.\n", argv[2]);
		return;
	}
	if (strcmp(sub, "inventory") == 0) {
		if (!StarInitialized()) { Printf("STAR API not initialized. %s\n", star_api_get_last_error()); return; }
		star_item_list_t* list = nullptr;
		star_api_result_t r = star_api_get_inventory(&list);
		if (r != STAR_API_SUCCESS) {
			Printf("Failed to get inventory: %s\n", star_api_get_last_error());
			return;
		}
		if (!list || list->count == 0) { Printf("Inventory is empty.\n"); if (list) star_api_free_item_list(list); return; }
		Printf("STAR inventory (%zu items):\n", list->count);
		for (size_t i = 0; i < list->count; i++) {
			Printf("  %s - %s (%s, %s)\n", list->items[i].name, list->items[i].description, list->items[i].game_source, list->items[i].item_type);
		}
		star_api_free_item_list(list);
		return;
	}
	if (strcmp(sub, "has") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star has <item_name>\n"); return; }
		bool has = star_api_has_item(argv[2]);
		Printf("Has '%s': %s\n", argv[2], has ? "yes" : "no");
		return;
	}
	if (strcmp(sub, "add") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star add <item_name> [description] [item_type]\n"); return; }
		const char* name = argv[2];
		const char* desc = argv.argc() > 3 ? argv[3] : "Added from console";
		const char* type = argv.argc() > 4 ? argv[4] : "Miscellaneous";
		star_api_result_t r = star_api_add_item(name, desc, "ODOOM", type);
		if (r == STAR_API_SUCCESS) Printf("Added '%s' to STAR inventory.\n", name);
		else Printf("Failed to add '%s': %s\n", name, star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "use") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star use <item_name> [context]\n"); return; }
		const char* ctx = argv.argc() > 3 ? argv[3] : "console";
		bool ok = star_api_use_item(argv[2], ctx);
		Printf("Use '%s' (context %s): %s\n", argv[2], ctx, ok ? "ok" : "failed");
		if (!ok) Printf("  %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "quest") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star quest start|objective|complete ...\n"); return; }
		const char* qsub = argv[2];
		if (strcmp(qsub, "start") == 0) {
			if (argv.argc() < 4) { Printf("Usage: star quest start <quest_id>\n"); return; }
			star_api_result_t r = star_api_start_quest(argv[3]);
			Printf(r == STAR_API_SUCCESS ? "Quest started.\n" : "Failed: %s\n", star_api_get_last_error());
			return;
		}
		if (strcmp(qsub, "objective") == 0) {
			if (argv.argc() < 5) { Printf("Usage: star quest objective <quest_id> <objective_id>\n"); return; }
			star_api_result_t r = star_api_complete_quest_objective(argv[3], argv[4], "ODOOM");
			Printf(r == STAR_API_SUCCESS ? "Objective completed.\n" : "Failed: %s\n", star_api_get_last_error());
			return;
		}
		if (strcmp(qsub, "complete") == 0) {
			if (argv.argc() < 4) { Printf("Usage: star quest complete <quest_id>\n"); return; }
			star_api_result_t r = star_api_complete_quest(argv[3]);
			Printf(r == STAR_API_SUCCESS ? "Quest completed.\n" : "Failed: %s\n", star_api_get_last_error());
			return;
		}
		Printf("Unknown: star quest %s. Use start|objective|complete.\n", qsub);
		return;
	}
	if (strcmp(sub, "bossnft") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star bossnft <boss_name> [description]\n"); return; }
		const char* name = argv[2];
		const char* desc = argv.argc() > 3 ? argv[3] : "Boss from UZDoom";
		char nft_id[64] = {};
		star_api_result_t r = star_api_create_boss_nft(name, desc, "ODOOM", "{}", nft_id);
		if (r == STAR_API_SUCCESS) Printf("Boss NFT created. ID: %s\n", nft_id[0] ? nft_id : "(none)");
		else Printf("Failed: %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "deploynft") == 0) {
		if (argv.argc() < 4) { Printf("Usage: star deploynft <nft_id> <target_game> [location]\n"); return; }
		const char* loc = argv.argc() > 4 ? argv[4] : "";
		star_api_result_t r = star_api_deploy_boss_nft(argv[2], argv[3], loc);
		Printf(r == STAR_API_SUCCESS ? "NFT deploy requested.\n" : "Failed: %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "beamin") == 0) {
		if (StarInitialized()) {
			Printf("Already logged in. Use 'star beamout' to log out first.\n");
			return;
		}
		StarLogInfo("Beaming in...");
		if (StarTryInitializeAndAuthenticate(true)) {
			Printf("Beam-in successful. Cross-game features enabled.\n");
			return;
		}
		Printf("Beam-in failed: %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "beamout") == 0) {
		if (!StarInitialized() && !g_star_client_ready) {
			Printf("Not logged in. Use 'star beamin' to log in.\n");
			return;
		}
		star_api_cleanup();
		g_star_client_ready = false;
		g_star_initialized = false;
		Printf("Logged out (beamout). Use 'star beamin' to log in again.\n");
		return;
	}
	Printf("Unknown STAR subcommand: %s. Type 'star' for list.\n", sub);
}
