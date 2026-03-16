/**
 * ODOOM - OASIS STAR API Integration Implementation
 *
 * Build this file as part of ODOOM (UZDoom) with STAR API from STARAPIClient.
 * Keycard pickups are reported to STAR; door/lock checks can use cross-game inventory.
 * In-game console: "star" command for testing (star version, star inventory, star add, etc.).
 *
 * Minimal hooks: pickups call star_api_queue_add_item; overlay calls star_api_get_inventory.
 * All sync, local delta, and background flush are in the C# StarApiClient.
 */

#include "uzdoom_star_integration.h"
#include "star_api.h"
#ifndef STAR_API_HAS_SEND_ITEM
/* Forward declare send-item API when using an older star_api.h (e.g. in UZDoom tree). Link with updated star_api.lib. */
extern "C" {
star_api_result_t star_api_send_item_to_avatar(const char* target_username_or_avatar_id, const char* item_name, int quantity, const char* item_id);
star_api_result_t star_api_send_item_to_clan(const char* clan_name_or_target, const char* item_name, int quantity, const char* item_id);
}
#endif
#ifndef STAR_API_HAS_QUEUE_PICKUP_WITH_MINT
/* Forward declare when star_api.h is old or from a tree that lacks it. Link with updated star_api.lib. */
extern "C" {
void star_api_queue_pickup_with_mint(const char* item_name, const char* description, const char* game_source, const char* item_type, int do_mint, const char* provider, const char* send_to_address_after_minting, int quantity);
}
#endif
#ifndef STAR_API_HAS_CONSUME_LAST_MINT
extern "C" {
int star_api_consume_last_mint_result(char* item_name_out, size_t item_name_size, char* nft_id_out, size_t nft_id_size, char* hash_out, size_t hash_size);
}
#endif
#include "star_sync.h"
#include "odoom_branding.h"

#include <cstdlib>
#include <cstdio>
#include <cstring>
#include <cstdarg>
#include <string>
#include <algorithm>
#include <cctype>
#include <map>
#include <thread>
#include <mutex>
#include <atomic>

/* ODOOM (UZDoom) headers for key detection */
#include "gamedata/a_keys.h"
#include "playsim/actor.h"
#include "playsim/a_pickups.h"
#include "playsim/p_local.h"
#include "gamedata/info.h"
#include "vm.h"
#include "c_dispatch.h"
#include "c_console.h"
#include "c_cvars.h"
#include "m_argv.h"
#include "printf.h"
#include "i_time.h"
#include "g_levellocals.h"
#include "playsim/d_player.h"

#ifdef _WIN32
#include <windows.h>
#include <wincred.h>
#pragma comment(lib, "Credui.lib")
/* Use Windows virtual key codes for ODOOM_GetRawKeyDown(GetAsyncKeyState). */
#define ODOOM_K_UP        VK_UP
#define ODOOM_K_DOWN      VK_DOWN
#define ODOOM_K_LEFT      VK_LEFT
#define ODOOM_K_RIGHT     VK_RIGHT
#define ODOOM_K_RETURN    VK_RETURN
#define ODOOM_K_PAGEUP    VK_PRIOR
#define ODOOM_K_PAGEDOWN  VK_NEXT
#define ODOOM_K_HOME      VK_HOME
#define ODOOM_K_END       VK_END
#define ODOOM_K_BACKSPACE VK_BACK
#else
/* Use engine key codes (GK_*) so code compiles on Linux and macOS. Provided by engine keydef headers. */
#define ODOOM_K_UP        GK_UP
#define ODOOM_K_DOWN      GK_DOWN
#define ODOOM_K_LEFT      GK_LEFT
#define ODOOM_K_RIGHT     GK_RIGHT
#define ODOOM_K_RETURN    GK_RETURN
#if defined(GK_PAGEUP)
#define ODOOM_K_PAGEUP    GK_PAGEUP
#define ODOOM_K_PAGEDOWN  GK_PAGEDOWN
#elif defined(GK_PRIOR)
#define ODOOM_K_PAGEUP    GK_PRIOR
#define ODOOM_K_PAGEDOWN  GK_NEXT
#else
/* Fallback if engine uses other names; define to a harmless value so build succeeds. */
#define ODOOM_K_PAGEUP    0
#define ODOOM_K_PAGEDOWN  0
#endif
#define ODOOM_K_HOME      GK_HOME
#define ODOOM_K_END       GK_END
#if defined(GK_BACKSPACE)
#define ODOOM_K_BACKSPACE GK_BACKSPACE
#else
#define ODOOM_K_BACKSPACE 0
#endif
#endif

/* Forward declaration so code before the definition (e.g. ODOOM_SaveJsonConfig) can call StarLogInfo. */
static void StarLogInfo(const char* fmt, ...);

/* When ODOOM_STAR_API_SESSION_IMPL is defined, provide JWT/session APIs by forwarding to star_api.dll at runtime. Avoids load-time "Entry Point Not Found" when DLL export list lags. */
#ifdef ODOOM_STAR_API_SESSION_IMPL
extern "C" {
#ifdef _WIN32
static star_api_result_t star_api_authenticate_with_jwt_out_impl(const char* user, const char* pass, char* jwt_buf, size_t jwt_size) {
	typedef star_api_result_t (__cdecl *fn_t)(const char*, const char*, char*, size_t);
	static fn_t fn;
	if (!fn) {
		HMODULE h = GetModuleHandleA("star_api.dll");
		if (h) fn = (fn_t)(void*)GetProcAddress(h, "star_api_authenticate_with_jwt_out");
	}
	return fn ? fn(user, pass, jwt_buf, jwt_size) : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_authenticate_with_jwt_out(const char* user, const char* pass, char* jwt_buf, size_t jwt_size) { return star_api_authenticate_with_jwt_out_impl(user, pass, jwt_buf, jwt_size); }

static star_api_result_t star_api_set_saved_session_impl(const char* jwt) {
	typedef star_api_result_t (__cdecl *fn_t)(const char*);
	static fn_t fn;
	if (!fn) {
		HMODULE h = GetModuleHandleA("star_api.dll");
		if (h) fn = (fn_t)(void*)GetProcAddress(h, "star_api_set_saved_session");
	}
	return fn ? fn(jwt) : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_set_saved_session(const char* jwt) { return star_api_set_saved_session_impl(jwt); }

static star_api_result_t star_api_restore_session_impl(void) {
	typedef star_api_result_t (__cdecl *fn_t)(void);
	static fn_t fn;
	if (!fn) {
		HMODULE h = GetModuleHandleA("star_api.dll");
		if (h) fn = (fn_t)(void*)GetProcAddress(h, "star_api_restore_session");
	}
	return fn ? fn() : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_restore_session(void) { return star_api_restore_session_impl(); }

static int star_api_get_current_username_impl(char* buf, size_t buf_size) {
	typedef int (__cdecl *fn_t)(char*, size_t);
	static fn_t fn;
	if (!fn) {
		HMODULE h = GetModuleHandleA("star_api.dll");
		if (h) fn = (fn_t)(void*)GetProcAddress(h, "star_api_get_current_username");
	}
	return fn ? fn(buf, buf_size) : 0;
}
int star_api_get_current_username(char* buf, size_t buf_size) { return star_api_get_current_username_impl(buf, buf_size); }

static int star_api_get_current_jwt_impl(char* buf, size_t buf_size) {
	typedef int (__cdecl *fn_t)(char*, size_t);
	static fn_t fn;
	if (!fn) {
		HMODULE h = GetModuleHandleA("star_api.dll");
		if (h) fn = (fn_t)(void*)GetProcAddress(h, "star_api_get_current_jwt");
	}
	return fn ? fn(buf, buf_size) : 0;
}
int star_api_get_current_jwt(char* buf, size_t buf_size) { return star_api_get_current_jwt_impl(buf, buf_size); }

static star_api_result_t star_api_set_refresh_token_impl(const char* refresh_token) {
	typedef star_api_result_t (__cdecl *fn_t)(const char*);
	static fn_t fn;
	if (!fn) {
		HMODULE h = GetModuleHandleA("star_api.dll");
		if (h) fn = (fn_t)(void*)GetProcAddress(h, "star_api_set_refresh_token");
	}
	return fn ? fn(refresh_token) : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_set_refresh_token(const char* refresh_token) { return star_api_set_refresh_token_impl(refresh_token); }

static int star_api_get_current_refresh_token_impl(char* buf, size_t buf_size) {
	typedef int (__cdecl *fn_t)(char*, size_t);
	static fn_t fn;
	if (!fn) {
		HMODULE h = GetModuleHandleA("star_api.dll");
		if (h) fn = (fn_t)(void*)GetProcAddress(h, "star_api_get_current_refresh_token");
	}
	return fn ? fn(buf, buf_size) : 0;
}
int star_api_get_current_refresh_token(char* buf, size_t buf_size) { return star_api_get_current_refresh_token_impl(buf, buf_size); }
#else
#include <dlfcn.h>
static star_api_result_t star_api_authenticate_with_jwt_out_impl(const char* user, const char* pass, char* jwt_buf, size_t jwt_size) {
	typedef star_api_result_t (*fn_t)(const char*, const char*, char*, size_t);
	static fn_t fn;
	if (!fn) {
		void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
		if (!h) h = dlopen(nullptr, RTLD_NOW);
		if (h) fn = (fn_t)dlsym(h, "star_api_authenticate_with_jwt_out");
	}
	return fn ? fn(user, pass, jwt_buf, jwt_size) : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_authenticate_with_jwt_out(const char* user, const char* pass, char* jwt_buf, size_t jwt_size) { return star_api_authenticate_with_jwt_out_impl(user, pass, jwt_buf, jwt_size); }

static star_api_result_t star_api_set_saved_session_impl(const char* jwt) {
	typedef star_api_result_t (*fn_t)(const char*);
	static fn_t fn;
	if (!fn) {
		void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
		if (!h) h = dlopen(nullptr, RTLD_NOW);
		if (h) fn = (fn_t)dlsym(h, "star_api_set_saved_session");
	}
	return fn ? fn(jwt) : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_set_saved_session(const char* jwt) { return star_api_set_saved_session_impl(jwt); }

static star_api_result_t star_api_restore_session_impl(void) {
	typedef star_api_result_t (*fn_t)(void);
	static fn_t fn;
	if (!fn) {
		void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
		if (!h) h = dlopen(nullptr, RTLD_NOW);
		if (h) fn = (fn_t)dlsym(h, "star_api_restore_session");
	}
	return fn ? fn() : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_restore_session(void) { return star_api_restore_session_impl(); }

static int star_api_get_current_username_impl(char* buf, size_t buf_size) {
	typedef int (*fn_t)(char*, size_t);
	static fn_t fn;
	if (!fn) {
		void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
		if (!h) h = dlopen(nullptr, RTLD_NOW);
		if (h) fn = (fn_t)dlsym(h, "star_api_get_current_username");
	}
	return fn ? fn(buf, buf_size) : 0;
}
int star_api_get_current_username(char* buf, size_t buf_size) { return star_api_get_current_username_impl(buf, buf_size); }

static int star_api_get_current_jwt_impl(char* buf, size_t buf_size) {
	typedef int (*fn_t)(char*, size_t);
	static fn_t fn;
	if (!fn) {
		void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
		if (!h) h = dlopen(nullptr, RTLD_NOW);
		if (h) fn = (fn_t)dlsym(h, "star_api_get_current_jwt");
	}
	return fn ? fn(buf, buf_size) : 0;
}
int star_api_get_current_jwt(char* buf, size_t buf_size) { return star_api_get_current_jwt_impl(buf, buf_size); }

static star_api_result_t star_api_set_refresh_token_impl(const char* refresh_token) {
	typedef star_api_result_t (*fn_t)(const char*);
	static fn_t fn;
	if (!fn) {
		void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
		if (!h) h = dlopen(nullptr, RTLD_NOW);
		if (h) fn = (fn_t)dlsym(h, "star_api_set_refresh_token");
	}
	return fn ? fn(refresh_token) : (star_api_result_t)STAR_API_ERROR_NOT_INITIALIZED;
}
star_api_result_t star_api_set_refresh_token(const char* refresh_token) { return star_api_set_refresh_token_impl(refresh_token); }

static int star_api_get_current_refresh_token_impl(char* buf, size_t buf_size) {
	typedef int (*fn_t)(char*, size_t);
	static fn_t fn;
	if (!fn) {
		void* h = dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD);
		if (!h) h = dlopen(nullptr, RTLD_NOW);
		if (h) fn = (fn_t)dlsym(h, "star_api_get_current_refresh_token");
	}
	return fn ? fn(buf, buf_size) : 0;
}
int star_api_get_current_refresh_token(char* buf, size_t buf_size) { return star_api_get_current_refresh_token_impl(buf, buf_size); }
#endif
}
#endif

static star_api_config_t g_star_config;
static bool g_star_initialized = false;
static bool g_star_client_ready = false;
/** When true, user explicitly beamed out; do not auto re-auth on door/touch until they run "star beamin" again. */
static bool g_star_user_beamed_out = false;
/** Obsolete: was used to avoid calling star_api_refresh_avatar_xp() twice; now we only call star_api_refresh_avatar_profile() once on beam-in. */
static bool g_star_refresh_xp_called_this_session = false;
static bool g_star_debug_logging = true;
static bool g_star_logged_runtime_auth_failure = false;
static bool g_star_logged_missing_auth_config = false;
/** Set true when restore-session path runs; frame pump sets tracker to "Loading..." once CVars are safe. */
static bool g_odoom_pending_loading_tracker = false;
static bool g_star_cli_loaded = false;
static std::string g_star_override_username;
static std::string g_star_override_password;
static std::string g_star_override_jwt;
static std::string g_star_override_api_key;
static std::string g_star_override_avatar_id;
static std::string g_star_effective_api_key;
static std::string g_star_effective_avatar_id;
static std::string g_star_effective_username;
static std::string g_star_effective_password;
static const int STAR_PICKUP_OQUAKE_GOLD_KEY = 5005;
static const int STAR_PICKUP_OQUAKE_SILVER_KEY = 5013;
/* STAR_PICKUP_GENERIC_ITEM is from uzdoom_star_integration.h (#define 9001) */
static std::string g_star_pending_item_name;
static std::string g_star_pending_item_desc;
static std::string g_star_pending_item_type;
static int g_star_pending_item_amount = 1;
static bool g_star_has_pending_item = false;
static std::string g_star_last_pickup_name;
static std::string g_star_last_pickup_type;
static std::string g_star_last_pickup_desc;
static bool g_star_has_last_pickup = false;
/** Debounce generic pickups: avoid spamming when standing on health/items at full. Only queue same (name,type) once per 0.5s. */
static std::string g_star_last_generic_key;
static int g_star_last_generic_tic = -99999;
static const int g_star_generic_debounce_ticks = 18;  /* ~0.5s at 35 tics/sec */
/** Player stats before touch: only add to STAR when engine would leave item on floor (did not apply to player). */
static int g_star_pre_touch_health = -1;
static int g_star_pre_touch_armor = -1;
/** When user presses E on a STAR item in inventory, we store name/type/description for the use-item callback to apply Health/Armor. */
static std::string g_star_use_pending_name;
static std::string g_star_use_pending_type;
static std::string g_star_use_pending_description;
/** Deferred apply: set when use-item succeeds; applied at start of next frame. Re-apply for several frames so HUD/status bar sees the update. */
static std::string g_star_deferred_apply_name;
static std::string g_star_deferred_apply_type;
static std::string g_star_deferred_apply_description;
static int g_star_deferred_apply_frames = 0;  /* number of frames left to re-apply (e.g. 3) */
static int g_star_deferred_health_value = -1; /* target health to re-assign on next frames (avoids engine overwrite) */
static int g_star_deferred_armor_value = -1;
static bool g_star_deferred_apply_health = false;
static bool g_star_deferred_apply_armor = false;
static bool g_star_face_suppressed_for_session = false;
/** Single source of truth for status bar face; only set by star face on/off and beam-in/out. */
static bool g_star_show_anorak_face = false;

/** True when we started async SSO auth so beamin command can show "Authenticating..." instead of "Beam-in failed". */
static bool g_star_async_auth_pending = false;
static bool StarInitialized(void);
CVAR(Bool, oasis_star_anorak_face, false, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Bool, oasis_star_beam_face, true, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(String, odoom_star_api_url, "https://star-api.oasisplatform.world/api", CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Int, odoom_oq_monster_yoffset, -50, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_global, 1.0f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_dog, 0.50f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_zombie, 0.33f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_demon, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_shambler, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_grunt, 0.40f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_fish, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_ogre, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_enforcer, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_spawn, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_knight, 0.60f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_scrag, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Float, odoom_oq_monster_scale_shub, 1.00f, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(String, odoom_star_username, "", 0)
CVAR(String, odoom_oasis_api_url, "https://api.oasisplatform.world", CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
/* Stack (1) = each pickup adds quantity; Unlock (0) = one per type. Ammo always stacks. Shared with OQuake; sigils are OQuake-only. */
CVAR(Int, odoom_star_stack_armor, 1, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_stack_weapons, 1, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_stack_powerups, 1, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_stack_keys, 1, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
/* Mint NFT when collecting: 1 = on, 0 = off. Not archived so oasisstar.json wins over ini. */
CVAR(Int, odoom_star_mint_weapons, 0, CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_mint_armor, 0, CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_mint_powerups, 0, CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_mint_keys, 0, CVAR_GLOBALCONFIG)
CVAR(String, odoom_star_nft_provider, "SolanaOASIS", CVAR_GLOBALCONFIG)
CVAR(String, odoom_star_send_to_address_after_minting, "", CVAR_GLOBALCONFIG)
/** 1 = always allow pickup (add to STAR inventory and remove from floor even when full); 0 = original Doom (full health/armor = can't pick up). */
CVAR(Int, odoom_star_always_allow_pickup_if_max, 1, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
/** 1 = always add pickups to STAR even when engine uses them (player gets both); 0 = only add when engine doesn't use (e.g. at max). Same as OQuake. */
CVAR(Int, odoom_star_always_add_items_to_inventory, 0, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
/** Max health/armor when using items from inventory (e.g. 200). Can set higher if desired. */
CVAR(Int, odoom_star_max_health, 200, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_max_armor, 200, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
/** 0 = below max: send to STAR inventory only (don't let engine use). 1 = standard. At max: use always_allow_pickup_if_max. Same as OQuake. */
CVAR(Int, odoom_star_use_health_on_pickup, 0, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_use_armor_on_pickup, 0, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)
CVAR(Int, odoom_star_use_powerup_on_pickup, 0, CVAR_ARCHIVE | CVAR_GLOBALCONFIG)

/** Per-monster mint flag: 1 = mint NFT when killed, 0 = off. Keys = normalized config key (e.g. odoom_zombieman, oquake_ogre). */
static std::map<std::string, int> g_odoom_mint_monster_flags;
struct ODOOM_MonsterEntry { const char* engineName; const char* configKey; const char* displayName; int xp; int isBoss; };
/** Engine class name, config key, display name (no (ODOOM) prefix; shown elsewhere), XP on kill, isBoss. See Docs/MONSTER_XP_TABLE.md. */
static const ODOOM_MonsterEntry ODOOM_MONSTERS[] = {
	{ "ZombieMan",           "odoom_zombieman",           "ZombieMan",        10, 0 },
	{ "ShotgunGuy",          "odoom_shotgunguy",          "ShotgunGuy",       15, 0 },
	{ "ChaingunGuy",         "odoom_chaingunguy",         "ChaingunGuy",      15, 0 },
	{ "Demon",               "odoom_demon",               "Demon",           25, 0 },
	{ "Spectre",             "odoom_spectre",             "Spectre",         30, 0 },
	{ "DoomImp",             "odoom_doomimp",             "DoomImp",        20, 0 },
	{ "Imp",                 "odoom_imp",                 "Imp",            20, 0 },
	{ "Cacodemon",           "odoom_cacodemon",           "Cacodemon",      50, 0 },
	{ "BaronOfHell",         "odoom_baronofhell",         "BaronOfHell",    150, 1 },
	{ "HellKnight",          "odoom_hellknight",          "HellKnight",     80, 0 },
	{ "LostSoul",            "odoom_lostsoul",             "LostSoul",       10, 0 },
	{ "PainElemental",       "odoom_painelemental",       "PainElemental",  45, 0 },
	{ "Revenant",            "odoom_revenant",            "Revenant",      60, 0 },
	{ "Mancubus",            "odoom_mancubus",            "Mancubus",      90, 0 },
	{ "Arachnotron",         "odoom_arachnotron",         "Arachnotron",     80, 0 },
	{ "Archvile",            "odoom_archvile",             "Archvile",     120, 0 },
	{ "SpiderMastermind",    "odoom_spidermastermind",    "SpiderMastermind", 800, 1 },
	{ "Cyberdemon",          "odoom_cyberdemon",          "Cyberdemon",   1000, 1 },
	/* OQ* entries: display names match Quake canonical names so inventory is consistent across games (per-game stacking via (ODOOM)/(OQUAKE) in name). */
	{ "OQMonsterDog",        "oquake_dog",                "Rottweiler",     15, 0 },
	{ "OQMonsterZombie",     "oquake_zombie",             "Zombie",         20, 0 },
	{ "OQMonsterDemon",      "oquake_demon",              "Fiend",          40, 0 },
	{ "OQMonsterShambler",   "oquake_shambler",           "Shambler",      200, 1 },
	{ "OQMonsterGrunt",      "oquake_grunt",              "Grunt",          25, 0 },
	{ "OQMonsterFish",       "oquake_fish",               "Rotfish",        30, 0 },
	{ "OQMonsterOgre",       "oquake_ogre",               "Ogre",           70, 0 },
	{ "OQMonsterEnforcer",   "oquake_enforcer",           "Enforcer",       60, 0 },
	{ "OQMonsterSpawn",      "oquake_spawn",              "Spawn",         100, 0 },
	{ "OQMonsterKnight",     "oquake_knight",             "Knight",         80, 0 },
	{ "OQMonsterScrag",      "oquake_scrag",              "Scrag",          60, 0 },
	{ "OQMonsterShub",       "oquake_shub",               "Shub-Niggurath", 500, 1 },
	{ nullptr, nullptr, nullptr, 0, 0 }
};

/* Config: ODOOM stores STAR options in the engine config. Typical path: Documents\\My Games\\UZDoom
 * (or OneDrive\\Documents\\My Games\\UZDoom) - ini file there is written on exit. STAR cvars use
 * CVAR_ARCHIVE so they should appear in that ini; if not visible, use oasisstar.json (loaded/saved
 * when found) for parity with OQuake and cross-game config sharing. */
static std::string g_odoom_json_config_path;
/** Frames until we re-apply oasisstar.json so mint etc. override ini. Set in Init when json loaded. */
static int g_odoom_reapply_json_frames = -1;
/** Persisted session for restore on next launch (loaded/saved from oasisstar.json). JWT not logged. */
static char g_odoom_saved_username[128] = {};
static char g_odoom_saved_jwt[2048] = {};
static char g_odoom_saved_refresh_token[2048] = {};

/** When init (e.g. star_api_init) has failed, we skip retrying until user runs beamin again to avoid spamming "couldn't find the host". */
static bool g_star_init_failed_this_session = false;

/** Frames since beam-in (or STAR became initialized). Used to avoid consuming key when opening door for a short time after beam-in. */
static int g_star_frames_since_beamin = 99999;
/** Always consume key when opening door (was 300-frame grace after beam-in; now 0 so key is used and HUD updates). */
static const int STAR_DOOR_CONSUME_GRACE_FRAMES = 0;
/** Set true in OnAuthDone when beam-in succeeds; next frame we refresh gold/silver key CVars once so they appear with Doom keycards. */
static bool g_star_just_beamed_in = false;

/* Inventory overlay: when open, temporarily clear key bindings (OQuake-style) so arrows/keys only drive the popup.
 * We read raw key state here and set odoom_key_* CVars so ZScript can drive selection/use/send/tabs. */
static bool g_odoom_inventory_bindings_captured = false;

/* Send popup (OQuake-style): text input buffer for username/clan name */
static const int ODOOM_SEND_INPUT_MAX = 64;
static std::string g_odoom_send_input_buffer;
static bool g_odoom_send_key_was_down[256];
static bool g_odoom_send_popup_was_open = false;
/* Last send item name/qty so we can update local cache on success without hitting the API */
static std::string g_odoom_last_sent_item_name;
static int g_odoom_last_sent_qty = 0;

static void StarApplyBeamFacePreference(void);

/*-----------------------------------------------------------------------------
 * OASIS STAR Config - oasisstar.json (parity with OQuake)
 *-----------------------------------------------------------------------------*/
static bool ODOOM_FindConfigFile(const char* filename, std::string& out_path) {
	FILE* test = fopen(filename, "r");
	if (test) { fclose(test); out_path = filename; return true; }
	const char* locations[] = { "build/", "../build/", "OASIS Omniverse/ODOOM/build/", nullptr };
	for (int i = 0; locations[i]; i++) {
		char buf[512];
		snprintf(buf, sizeof(buf), "%s%s", locations[i], filename);
		test = fopen(buf, "r");
		if (test) { fclose(test); out_path = buf; return true; }
	}
#ifdef _WIN32
	char exe_path[MAX_PATH] = {0};
	if (GetModuleFileNameA(nullptr, exe_path, sizeof(exe_path))) {
		std::string exe_dir(exe_path);
		size_t last = exe_dir.find_last_of("\\/");
		if (last != std::string::npos) {
			exe_dir.resize(last);
			out_path = exe_dir + "\\"; out_path += filename;
			test = fopen(out_path.c_str(), "r");
			if (test) { fclose(test); return true; }
			out_path = exe_dir + "\\build\\"; out_path += filename;
			test = fopen(out_path.c_str(), "r");
			if (test) { fclose(test); return true; }
		}
	}
#endif
	return false;
}

static bool ODOOM_ExtractJsonValue(const char* json, const char* key, char* value, int maxlen) {
	char search[128];
	snprintf(search, sizeof(search), "\"%s\"", key);
	const char* pos = strstr(json, search);
	if (!pos) return false;
	pos += strlen(search);
	while (*pos && (*pos == ' ' || *pos == ':' || *pos == '\t')) pos++;
	if (*pos == '"') {
		pos++;
		int n = 0;
		while (*pos && *pos != '"' && *pos != '\n' && *pos != '\r' && n < maxlen - 1) {
			if (*pos == '\\' && pos[1]) { pos++; if (*pos == 'n') value[n++] = '\n'; else if (*pos == 't') value[n++] = '\t'; else if (*pos == '\\') value[n++] = '\\'; else if (*pos == '"') value[n++] = '"'; else value[n++] = *pos; }
			else value[n++] = *pos;
			pos++;
		}
		value[n] = '\0';
		return n > 0;
	}
	int n = 0;
	while (*pos && *pos != ',' && *pos != '}' && *pos != '\n' && *pos != '\r' && *pos != ' ' && n < maxlen - 1)
		value[n++] = *pos++;
	value[n] = '\0';
	return n > 0;
}

static bool ODOOM_LoadJsonConfig(const char* json_path) {
	FILE* f = fopen(json_path, "r");
	if (!f) return false;
	char json[4096] = {0};
	size_t len = fread(json, 1, sizeof(json) - 1, f);
	fclose(f);
	if (len == 0) return false;
	json[len] = '\0';
	bool loaded = false;
	char value[256];
	if (ODOOM_ExtractJsonValue(json, "star_api_url", value, (int)sizeof(value))) {
		odoom_star_api_url = value;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "oasis_api_url", value, (int)sizeof(value))) {
		odoom_oasis_api_url = value;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "beam_face", value, (int)sizeof(value))) {
		oasis_star_beam_face = (atoi(value) != 0);
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "stack_armor", value, (int)sizeof(value))) {
		odoom_star_stack_armor = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "stack_weapons", value, (int)sizeof(value))) {
		odoom_star_stack_weapons = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "stack_powerups", value, (int)sizeof(value))) {
		odoom_star_stack_powerups = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "stack_keys", value, (int)sizeof(value))) {
		odoom_star_stack_keys = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "mint_weapons", value, (int)sizeof(value))) {
		odoom_star_mint_weapons = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "mint_armor", value, (int)sizeof(value))) {
		odoom_star_mint_armor = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "mint_powerups", value, (int)sizeof(value))) {
		odoom_star_mint_powerups = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "mint_keys", value, (int)sizeof(value))) {
		odoom_star_mint_keys = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "nft_provider", value, (int)sizeof(value))) {
		odoom_star_nft_provider = value;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "send_to_address_after_minting", value, (int)sizeof(value))) {
		odoom_star_send_to_address_after_minting = value;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "always_allow_pickup_if_max", value, (int)sizeof(value))) {
		odoom_star_always_allow_pickup_if_max = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "always_add_items_to_inventory", value, (int)sizeof(value))) {
		odoom_star_always_add_items_to_inventory = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "max_health", value, (int)sizeof(value))) {
		int v = atoi(value);
		odoom_star_max_health = (v > 0) ? v : 200;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "max_armor", value, (int)sizeof(value))) {
		int v = atoi(value);
		odoom_star_max_armor = (v > 0) ? v : 200;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "use_health_on_pickup", value, (int)sizeof(value))) {
		odoom_star_use_health_on_pickup = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "use_armor_on_pickup", value, (int)sizeof(value))) {
		odoom_star_use_armor_on_pickup = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	if (ODOOM_ExtractJsonValue(json, "use_powerup_on_pickup", value, (int)sizeof(value))) {
		odoom_star_use_powerup_on_pickup = (atoi(value) != 0) ? 1 : 0;
		loaded = true;
	}
	/* Per-monster mint: mint_monster_odoom_zombieman, mint_monster_oquake_ogre, etc. Default 1 if key missing. */
	for (int i = 0; ODOOM_MONSTERS[i].engineName; i++) {
		char key[128];
		std::snprintf(key, sizeof(key), "mint_monster_%s", ODOOM_MONSTERS[i].configKey);
		if (ODOOM_ExtractJsonValue(json, key, value, (int)sizeof(value)))
			g_odoom_mint_monster_flags[ODOOM_MONSTERS[i].configKey] = (atoi(value) != 0) ? 1 : 0;
		else
			g_odoom_mint_monster_flags[ODOOM_MONSTERS[i].configKey] = 1;  /* default 1 */
		loaded = true;
	}
	/* Persisted session for autologin (beamedin_avatar + jwt_token). Fallback to old keys for compatibility. */
	if (((ODOOM_ExtractJsonValue(json, "beamedin_avatar", value, (int)sizeof(value)) || ODOOM_ExtractJsonValue(json, "saved_username", value, (int)sizeof(value))) && value[0])) {
		std::strncpy(g_odoom_saved_username, value, sizeof(g_odoom_saved_username) - 1);
		g_odoom_saved_username[sizeof(g_odoom_saved_username) - 1] = '\0';
		loaded = true;
	}
	{
		char value_jwt[2048];
		value_jwt[0] = '\0';
		if ((ODOOM_ExtractJsonValue(json, "jwt_token", value_jwt, (int)sizeof(value_jwt)) || ODOOM_ExtractJsonValue(json, "saved_jwt", value_jwt, (int)sizeof(value_jwt))) && value_jwt[0]) {
			std::strncpy(g_odoom_saved_jwt, value_jwt, sizeof(g_odoom_saved_jwt) - 1);
			g_odoom_saved_jwt[sizeof(g_odoom_saved_jwt) - 1] = '\0';
			loaded = true;
		}
	}
	{
		char value_rt[2048];
		value_rt[0] = '\0';
		if (ODOOM_ExtractJsonValue(json, "refresh_token", value_rt, (int)sizeof(value_rt)) && value_rt[0]) {
			std::strncpy(g_odoom_saved_refresh_token, value_rt, sizeof(g_odoom_saved_refresh_token) - 1);
			g_odoom_saved_refresh_token[sizeof(g_odoom_saved_refresh_token) - 1] = '\0';
			loaded = true;
		}
	}
	if (loaded) {
		/* Apply mint and nft_provider to engine cvars so they persist (ini may have loaded 0 before this). */
		UCVarValue u;
		FBaseCVar* v = nullptr;
		u.Int = odoom_star_mint_weapons ? 1 : 0; v = FindCVar("odoom_star_mint_weapons", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_mint_armor ? 1 : 0; v = FindCVar("odoom_star_mint_armor", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_mint_powerups ? 1 : 0; v = FindCVar("odoom_star_mint_powerups", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_mint_keys ? 1 : 0; v = FindCVar("odoom_star_mint_keys", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		v = FindCVar("odoom_star_nft_provider", nullptr);
		if (v && v->GetRealType() == CVAR_String) {
			UCVarValue vs; vs.String = (char*)(const char*)odoom_star_nft_provider;
			v->SetGenericRep(vs, CVAR_String);
		}
		v = FindCVar("odoom_star_send_to_address_after_minting", nullptr);
		if (v && v->GetRealType() == CVAR_String) {
			UCVarValue vs; vs.String = (char*)(const char*)odoom_star_send_to_address_after_minting;
			v->SetGenericRep(vs, CVAR_String);
		}
		u.Int = odoom_star_always_add_items_to_inventory ? 1 : 0; v = FindCVar("odoom_star_always_add_items_to_inventory", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_max_health; v = FindCVar("odoom_star_max_health", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_max_armor; v = FindCVar("odoom_star_max_armor", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_use_health_on_pickup ? 1 : 0; v = FindCVar("odoom_star_use_health_on_pickup", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_use_armor_on_pickup ? 1 : 0; v = FindCVar("odoom_star_use_armor_on_pickup", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
		u.Int = odoom_star_use_powerup_on_pickup ? 1 : 0; v = FindCVar("odoom_star_use_powerup_on_pickup", nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(u, CVAR_Int);
	}
	return loaded;
}

static bool ODOOM_SaveJsonConfig(const char* json_path) {
	FILE* f = fopen(json_path, "w");
	if (!f) return false;
	const char* star_url = (const char*)odoom_star_api_url;
	const char* oasis_url = (const char*)odoom_oasis_api_url;
	fprintf(f, "{\n");
	fprintf(f, "  \"star_api_url\": \"%s\",\n", star_url ? star_url : "");
	fprintf(f, "  \"oasis_api_url\": \"%s\",\n", oasis_url ? oasis_url : "");
	fprintf(f, "  \"beam_face\": %d,\n", oasis_star_beam_face ? 1 : 0);
	fprintf(f, "  \"stack_armor\": %d,\n", odoom_star_stack_armor ? 1 : 0);
	fprintf(f, "  \"stack_weapons\": %d,\n", odoom_star_stack_weapons ? 1 : 0);
	fprintf(f, "  \"stack_powerups\": %d,\n", odoom_star_stack_powerups ? 1 : 0);
	fprintf(f, "  \"stack_keys\": %d,\n", odoom_star_stack_keys ? 1 : 0);
	fprintf(f, "  \"mint_weapons\": %d,\n", odoom_star_mint_weapons ? 1 : 0);
	fprintf(f, "  \"mint_armor\": %d,\n", odoom_star_mint_armor ? 1 : 0);
	fprintf(f, "  \"mint_powerups\": %d,\n", odoom_star_mint_powerups ? 1 : 0);
	fprintf(f, "  \"mint_keys\": %d,\n", odoom_star_mint_keys ? 1 : 0);
	{
		const char* prov = (const char*)odoom_star_nft_provider;
		if (!prov) prov = "";
		fprintf(f, "  \"nft_provider\": \"");
		for (; *prov; prov++) {
			if (*prov == '"' || *prov == '\\') fputc('\\', f);
			fputc((unsigned char)*prov, f);
		}
		fprintf(f, "\",\n");
	}
	{
		const char* send_addr = (const char*)odoom_star_send_to_address_after_minting;
		if (!send_addr) send_addr = "";
		fprintf(f, "  \"send_to_address_after_minting\": \"");
		for (; *send_addr; send_addr++) {
			if (*send_addr == '"' || *send_addr == '\\') fputc('\\', f);
			fputc((unsigned char)*send_addr, f);
		}
		fprintf(f, "\",\n");
	}
	{
		int ap = 1, aa = 0, mh = 200, ma = 200;
		FBaseCVar* v = FindCVar("odoom_star_always_allow_pickup_if_max", nullptr);
		if (v && v->GetRealType() == CVAR_Int) ap = v->GetGenericRep(CVAR_Int).Int ? 1 : 0;
		v = FindCVar("odoom_star_always_add_items_to_inventory", nullptr);
		if (v && v->GetRealType() == CVAR_Int) aa = v->GetGenericRep(CVAR_Int).Int ? 1 : 0;
		v = FindCVar("odoom_star_max_health", nullptr);
		if (v && v->GetRealType() == CVAR_Int) mh = v->GetGenericRep(CVAR_Int).Int;
		v = FindCVar("odoom_star_max_armor", nullptr);
		if (v && v->GetRealType() == CVAR_Int) ma = v->GetGenericRep(CVAR_Int).Int;
		if (mh <= 0) mh = 200;
		if (ma <= 0) ma = 200;
		fprintf(f, "  \"always_allow_pickup_if_max\": %d,\n", ap);
		fprintf(f, "  \"always_add_items_to_inventory\": %d,\n", aa);
		fprintf(f, "  \"max_health\": %d,\n", mh);
		fprintf(f, "  \"max_armor\": %d,\n", ma);
		fprintf(f, "  \"use_health_on_pickup\": %d,\n", odoom_star_use_health_on_pickup ? 1 : 0);
		fprintf(f, "  \"use_armor_on_pickup\": %d,\n", odoom_star_use_armor_on_pickup ? 1 : 0);
		fprintf(f, "  \"use_powerup_on_pickup\": %d,\n", odoom_star_use_powerup_on_pickup ? 1 : 0);
	}
	int nmonsters = 0;
	while (ODOOM_MONSTERS[nmonsters].engineName) nmonsters++;
	/* Persisted session (beamedin_avatar + jwt_token) for autologin. */
	if (g_star_initialized) {
		char uname[128] = {};
		char jwt[2048] = {};
		if (star_api_get_current_username(uname, sizeof(uname)) > 0 && uname[0]) {
			std::strncpy(g_odoom_saved_username, uname, sizeof(g_odoom_saved_username) - 1);
			g_odoom_saved_username[sizeof(g_odoom_saved_username) - 1] = '\0';
		} else if (!g_star_effective_username.empty()) {
			/* Fallback: DLL may not export get_current_username; use current session username so beamedin_avatar is written. */
			std::strncpy(g_odoom_saved_username, g_star_effective_username.c_str(), sizeof(g_odoom_saved_username) - 1);
			g_odoom_saved_username[sizeof(g_odoom_saved_username) - 1] = '\0';
		}
		if (star_api_get_current_jwt(jwt, sizeof(jwt)) > 0 && jwt[0]) {
			std::strncpy(g_odoom_saved_jwt, jwt, sizeof(g_odoom_saved_jwt) - 1);
			g_odoom_saved_jwt[sizeof(g_odoom_saved_jwt) - 1] = '\0';
		} else if (g_odoom_saved_username[0]) {
			static int s_odoom_jwt_missing_logged = 0;
			if (s_odoom_jwt_missing_logged++ == 0)
				StarLogInfo("ODOOM: Could not get JWT from STAR API (autologin may not work). Rebuild STARAPIClient and run BUILD_AND_DEPLOY_STAR_CLIENT.bat so star_api.dll exports session APIs.");
		}
		{
			char rt[2048] = {};
			if (star_api_get_current_refresh_token(rt, sizeof(rt)) > 0 && rt[0]) {
				std::strncpy(g_odoom_saved_refresh_token, rt, sizeof(g_odoom_saved_refresh_token) - 1);
				g_odoom_saved_refresh_token[sizeof(g_odoom_saved_refresh_token) - 1] = '\0';
			}
		}
	}
	const bool have_session = g_odoom_saved_username[0] || g_odoom_saved_jwt[0];
	for (int i = 0; i < nmonsters; i++) {
		const char* ckey = ODOOM_MONSTERS[i].configKey;
		auto it = g_odoom_mint_monster_flags.find(ckey);
		int v = (it != g_odoom_mint_monster_flags.end()) ? it->second : 1;
		fprintf(f, "  \"mint_monster_%s\": %d%s\n", ckey, v ? 1 : 0, (i < nmonsters - 1 || have_session) ? "," : "");
	}
	if (have_session) {
		if (g_odoom_saved_username[0]) {
			fprintf(f, "  \"beamedin_avatar\": \"");
			for (const char* p = g_odoom_saved_username; *p; p++) {
				if (*p == '"' || *p == '\\') fputc('\\', f);
				fputc((unsigned char)*p, f);
			}
			fprintf(f, "\"");
		}
		if (g_odoom_saved_jwt[0]) {
			if (g_odoom_saved_username[0]) fprintf(f, ",\n");
			fprintf(f, "  \"jwt_token\": \"");
			for (const char* p = g_odoom_saved_jwt; *p; p++) {
				if (*p == '"' || *p == '\\') fputc('\\', f);
				fputc((unsigned char)*p, f);
			}
			fprintf(f, "\"");
		}
		if (g_odoom_saved_refresh_token[0]) {
			if (g_odoom_saved_username[0] || g_odoom_saved_jwt[0]) fprintf(f, ",\n");
			fprintf(f, "  \"refresh_token\": \"");
			for (const char* p = g_odoom_saved_refresh_token; *p; p++) {
				if (*p == '"' || *p == '\\') fputc('\\', f);
				fputc((unsigned char)*p, f);
			}
			fprintf(f, "\"");
		}
		fprintf(f, "\n");
	}
	fprintf(f, "}\n");
	fclose(f);
	return true;
}

/** Write current STAR cvars to oasisstar.json. Used on exit, star config save, star seturl/setoasisurl, star face. */
static void ODOOM_SaveStarConfigToFiles(void) {
	std::string path;
	if (!g_odoom_json_config_path.empty())
		path = g_odoom_json_config_path;
	else if (ODOOM_FindConfigFile("oasisstar.json", path))
		g_odoom_json_config_path = path;
	else {
		/* Prefer exe dir for new file */
#ifdef _WIN32
		char exe_path[MAX_PATH] = {0};
		if (GetModuleFileNameA(nullptr, exe_path, sizeof(exe_path))) {
			std::string exe_dir(exe_path);
			size_t last = exe_dir.find_last_of("\\/");
			if (last != std::string::npos) {
				exe_dir.resize(last);
				path = exe_dir + "\\oasisstar.json";
			}
		}
#endif
		if (path.empty()) path = "oasisstar.json";
	}
	if (!path.empty() && ODOOM_SaveJsonConfig(path.c_str()))
		g_odoom_json_config_path = path;
}

/** Return 1 if key is currently down, 0 otherwise. Windows: GetAsyncKeyState(VK_*). Linux/macOS: engine GK_*; returns 0 until engine key API is wired. */
static int ODOOM_GetRawKeyDown(int vk_or_ascii)
{
#ifdef _WIN32
	SHORT s = GetAsyncKeyState(vk_or_ascii);
	return (s & 0x8000) ? 1 : 0;
#else
	(void)vk_or_ascii;
	return 0;  /* Linux/macOS: TODO wire to engine key state (GK_*) when API available */
#endif
}

/** Max bytes to pass to the inventory list CVar. Engine string CVars can have a small fixed buffer; exceeding it causes "attempted to write past end of stream". Use 1K to stay under typical limits. */
static const size_t ODOOM_INVENTORY_CVAR_MAX_BYTES = 1024;
/** Max bytes for quest list string (Q\tid\tname\tdesc\tstatus\tpct\n and O\t...\n lines). */
static const size_t ODOOM_QUEST_LIST_MAX_BYTES = 16384;
/** Max bytes to assign to odoom_quest_list CVar. Engine string CVars have a fixed buffer; exceeding it causes "Attempted to write past end of stream". */
static const size_t ODOOM_QUEST_CVAR_MAX_BYTES = 4096;
/** Max items in one window so we stay under the byte limit; ZScript scrolls by requesting different scroll_offset. */
static const size_t ODOOM_INVENTORY_WINDOW_ITEMS = 24;

/** Tab indices matching ZScript TAB_KEYS etc. Used to filter items per tab. Items=5, Monsters=6 (last). */
static const int ODOOM_TAB_KEYS = 0, ODOOM_TAB_POWERUPS = 1, ODOOM_TAB_WEAPONS = 2, ODOOM_TAB_AMMO = 3, ODOOM_TAB_ARMOR = 4, ODOOM_TAB_ITEMS = 5, ODOOM_TAB_MONSTERS = 6;

/** Return true if item matches the given tab (same logic as ZScript IsStarItemInTab). */
static bool ODOOM_ItemMatchesTab(const char* item_type, const char* name, int tab) {
	auto contains = [](const char* haystack, const char* needle) {
		return haystack && needle && std::strstr(haystack, needle) != nullptr;
	};
	auto containsKey = [&](const char* s) {
		return contains(s, "Key") || contains(s, "key");
	};
	if (tab == ODOOM_TAB_KEYS) return containsKey(item_type) || containsKey(name);
	if (tab == ODOOM_TAB_POWERUPS) return contains(item_type, "Powerup");
	if (tab == ODOOM_TAB_WEAPONS) return contains(item_type, "Weapon");
	if (tab == ODOOM_TAB_AMMO) return contains(item_type, "Ammo");
	if (tab == ODOOM_TAB_ARMOR) return contains(item_type, "Armor");
	if (tab == ODOOM_TAB_MONSTERS) return contains(item_type, "Monster") || (name && (std::strstr(name, "[NFT]") != nullptr || std::strstr(name, "[BOSSNFT]") != nullptr));
	if (tab == ODOOM_TAB_ITEMS) {
		return !containsKey(item_type) && !containsKey(name)
			&& !contains(item_type, "Powerup") && !contains(item_type, "Weapon")
			&& !contains(item_type, "Ammo") && !contains(item_type, "Armor")
			&& !contains(item_type, "Monster");
	}
	return true; /* unknown tab: show all */
}

/** Health/armor amount for (+X) description. Returns 0 if not health/armor. Implemented later in file. */
static int GetHealthOrArmorAmount(const char* className);
/** Hardcoded Doom ammo amount for (+X) description. Returns 0 to use default 1. Implemented later in file. */
static int GetHardcodedAmmoAmount(const char* className);

/** Push inventory list to CVars for ZScript overlay. list may be null (clears overlay). Caller keeps ownership.
 * Filters by current tab (odoom_star_inventory_tab), then sends total filtered count and a window [scroll_offset, scroll_offset+N)
 * so all items in that tab are reachable by scrolling. ZScript sets scroll_offset and tab each frame. */
static void ODOOM_PushInventoryToCVars(const star_item_list_t* list) {
	static char listBuf[ODOOM_INVENTORY_CVAR_MAX_BYTES];
	FBaseCVar* countVar = FindCVar("odoom_star_inventory_count", nullptr);
	FBaseCVar* listVar = FindCVar("odoom_star_inventory_list", nullptr);
	FBaseCVar* scrollVar = FindCVar("odoom_star_inventory_scroll_offset", nullptr);
	FBaseCVar* tabVar = FindCVar("odoom_star_inventory_tab", nullptr);
	if (!countVar || !listVar) return;

	if (!list || !list->items || list->count == 0) {
		UCVarValue u; u.Int = 0;
		countVar->SetGenericRep(u, CVAR_Int);
		UCVarValue v; v.String = (char*)("");
		listVar->SetGenericRep(v, CVAR_String);
		return;
	}

	int scrollOffset = 0;
	int tab = ODOOM_TAB_KEYS;
	if (scrollVar && scrollVar->GetRealType() == CVAR_Int)
		scrollOffset = scrollVar->GetGenericRep(CVAR_Int).Int;
	if (tabVar && tabVar->GetRealType() == CVAR_Int)
		tab = tabVar->GetGenericRep(CVAR_Int).Int;
	if (scrollOffset < 0) scrollOffset = 0;
	if (tab < 0 || tab > ODOOM_TAB_MONSTERS) tab = ODOOM_TAB_KEYS;

	size_t n = list->count;
	size_t filteredCount = 0;
	for (size_t i = 0; i < n; i++) {
		const star_item_t* it = &list->items[i];
		if (ODOOM_ItemMatchesTab(it->item_type, it->name, tab))
			filteredCount++;
	}

	UCVarValue u; u.Int = (int)filteredCount;
	countVar->SetGenericRep(u, CVAR_Int);

	size_t off = 0;
	size_t maxOff = sizeof(listBuf) - 320;
	if (maxOff > ODOOM_INVENTORY_CVAR_MAX_BYTES - 1)
		maxOff = ODOOM_INVENTORY_CVAR_MAX_BYTES - 1;

	auto copySafe = [](char* dst, const char* src, int maxLen) {
		int j = 0;
		while (src[j] && j < maxLen - 1) {
			char c = src[j];
			if (c == '\t' || c == '\n' || c == '\r') c = ' ';
			dst[j++] = c;
		}
		dst[j] = '\0';
	};

	size_t filteredIndex = 0;
	for (size_t i = 0; i < n && off < maxOff; i++) {
		const star_item_t* it = &list->items[i];
		if (!ODOOM_ItemMatchesTab(it->item_type, it->name, tab)) continue;
		if (filteredIndex < (size_t)scrollOffset) {
			filteredIndex++;
			continue;
		}
		if ((filteredIndex - (size_t)scrollOffset) >= ODOOM_INVENTORY_WINDOW_ITEMS) break;

		char name[320], desc[256], type[64], game[64];
		bool isNft = (it->nft_id[0] != '\0');
		if (isNft) {
			snprintf(name, sizeof(name), "[NFT] %s", it->name);
			copySafe(name, name, 320);
		} else {
			copySafe(name, it->name, 256);
		}
		copySafe(desc, it->description, 256);
		/* OQUAKE-style: ensure health/armor/ammo show (+X) in description when missing */
		if (it->item_type[0] && (!it->description[0] || !strstr(it->description, "(+"))) {
			int amt = GetHealthOrArmorAmount(it->name);
			if (amt <= 0 && (strstr(it->item_type, "Ammo") || strstr(it->item_type, "ammo")))
				amt = GetHardcodedAmmoAmount(it->name);
			if (amt > 0) {
				snprintf(desc, sizeof(desc), "%s (+%d)", it->name[0] ? it->name : "Item", amt);
				copySafe(desc, desc, 256);
			}
		}
		copySafe(type, it->item_type, 64);
		copySafe(game, it->game_source, 64);
		int qty = (it->quantity > 0) ? it->quantity : 1;
		int wr = snprintf(listBuf + off, (size_t)(sizeof(listBuf) - off), "%s\t%s\t%s\t%s\t%d\n", name, desc, type, game, qty);
		if (wr > 0 && (size_t)wr < sizeof(listBuf) - off)
			off += (size_t)wr;
		else
			break;
		filteredIndex++;
	}
	listBuf[off] = '\0';
	UCVarValue v; v.String = listBuf;
	listVar->SetGenericRep(v, CVAR_String);
}

/** Set odoom_star_has_gold_key / odoom_star_has_silver_key from inventory list so ZScript can give OQ keys for HUD. When !initialized or list==null, clear to 0. Also updates odoom_star_avatar_xp from star_api_get_avatar_xp. */
static void ODOOM_UpdateStarKeyHudCVars(const star_item_list_t* list) {
	if (!g_star_initialized || !list) {
		FBaseCVar* g = FindCVar("odoom_star_has_gold_key", nullptr);
		FBaseCVar* s = FindCVar("odoom_star_has_silver_key", nullptr);
		FBaseCVar* xpVar = FindCVar("odoom_star_avatar_xp", nullptr);
		if (g && g->GetRealType() == CVAR_Int) { UCVarValue u; u.Int = 0; g->SetGenericRep(u, CVAR_Int); }
		if (s && s->GetRealType() == CVAR_Int) { UCVarValue u; u.Int = 0; s->SetGenericRep(u, CVAR_Int); }
		if (xpVar && xpVar->GetRealType() == CVAR_Int) { UCVarValue u; u.Int = 0; xpVar->SetGenericRep(u, CVAR_Int); }
		return;
	}
	int hasGold = 0, hasSilver = 0;
	if (list->items) {
		for (size_t i = 0; i < list->count; i++) {
			const char* n = list->items[i].name;
			if (!n) continue;
			std::string lower;
			for (const char* p = n; *p; ++p)
				lower.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(*p == '_' ? ' ' : *p))));
			if (lower.find("gold") != std::string::npos && (lower.find("key") != std::string::npos || lower.find("keycard") != std::string::npos))
				hasGold = 1;
			if (lower.find("silver") != std::string::npos && (lower.find("key") != std::string::npos || lower.find("keycard") != std::string::npos))
				hasSilver = 1;
		}
	}
	FBaseCVar* g = FindCVar("odoom_star_has_gold_key", nullptr);
	FBaseCVar* s = FindCVar("odoom_star_has_silver_key", nullptr);
	if (g && g->GetRealType() == CVAR_Int) { UCVarValue u; u.Int = hasGold; g->SetGenericRep(u, CVAR_Int); }
	if (s && s->GetRealType() == CVAR_Int) { UCVarValue u; u.Int = hasSilver; s->SetGenericRep(u, CVAR_Int); }
	int xp = 0;
	if (star_api_get_avatar_xp(&xp))
	{
		FBaseCVar* xpVar = FindCVar("odoom_star_avatar_xp", nullptr);
		if (xpVar && xpVar->GetRealType() == CVAR_Int) { UCVarValue u; u.Int = xp; xpVar->SetGenericRep(u, CVAR_Int); }
	}
}

/** Refresh overlay from client (get_inventory returns API + pending merged in C#). When not beamed in, push empty so no phantom inventory/keys. */
static void ODOOM_RefreshOverlayFromClient(void) {
	if (!g_star_initialized) {
		ODOOM_UpdateStarKeyHudCVars(nullptr);  /* clears gold/silver CVars */
		ODOOM_PushInventoryToCVars(nullptr);  /* empty list, count 0 */
		return;
	}
	star_item_list_t* list = nullptr;
	if (star_api_get_inventory(&list) != STAR_API_SUCCESS || !list) return;
	ODOOM_UpdateStarKeyHudCVars(list);
	ODOOM_PushInventoryToCVars(list);
	star_api_free_item_list(list);
}

static void StarLogInfo(const char* fmt, ...);

/** Fetch quests from API and push to CVars. Sets odoom_quest_list, odoom_quest_count. Uses top-level quests only (like Quake) so main list shows parents; sub-quests appear in detail panel. Tracker shows quest only when odoom_quest_tracker_quest_id is set. */
static void ODOOM_RefreshQuestCVars(void) {
	FBaseCVar* listVar = FindCVar("odoom_quest_list", nullptr);
	FBaseCVar* countVar = FindCVar("odoom_quest_count", nullptr);
	FBaseCVar* trackerTitleVar = FindCVar("odoom_quest_tracker_title", nullptr);
	FBaseCVar* trackerObjVar = FindCVar("odoom_quest_tracker_objective", nullptr);
	FBaseCVar* trackerIdVar = FindCVar("odoom_quest_tracker_quest_id", nullptr);
	if (!listVar || !countVar) return;

	static char questBuf[ODOOM_QUEST_LIST_MAX_BYTES];
	int n = star_api_get_top_level_quests_string(questBuf, sizeof(questBuf));
	if (n < 0 || !g_star_initialized) {
		UCVarValue v; v.String = (char*)"";
		listVar->SetGenericRep(v, CVAR_String);
		UCVarValue c; c.Int = 0;
		countVar->SetGenericRep(c, CVAR_Int);
		if (trackerTitleVar && trackerTitleVar->GetRealType() == CVAR_String) { UCVarValue t; t.String = (char*)""; trackerTitleVar->SetGenericRep(t, CVAR_String); }
		if (trackerObjVar && trackerObjVar->GetRealType() == CVAR_String) { UCVarValue o; o.String = (char*)""; trackerObjVar->SetGenericRep(o, CVAR_String); }
		return;
	}
	if (n >= (int)sizeof(questBuf))
		n = (int)sizeof(questBuf) - 1;
	questBuf[n] = '\0';

	/* Truncate to CVar-safe length so engine does not overflow ("Attempted to write past end of stream"). */
	size_t assignLen = (size_t)n;
	if (assignLen > ODOOM_QUEST_CVAR_MAX_BYTES) {
		assignLen = ODOOM_QUEST_CVAR_MAX_BYTES;
		/* Find last newline in [0, assignLen) so we don't cut mid-line */
		size_t lastNl = 0;
		for (size_t i = 0; i < assignLen; i++)
			if (questBuf[i] == '\n') lastNl = i + 1;
		if (lastNl > 0)
			assignLen = lastNl;
	}

	/* Debug: log bytes received and quest count (throttle to avoid spam) */
	{
		static int s_last_n = -1;
		static int s_last_quest_count = -1;
		int questCountForLog = 0;
		{ const char* pp = questBuf; const char* endBuf = questBuf + n; while (pp < endBuf && *pp) {
			const char* lineEnd = (const char*)memchr(pp, '\n', (size_t)(endBuf - pp));
			size_t lineLen = lineEnd ? (size_t)(lineEnd - pp) : (size_t)(endBuf - pp);
			if (lineLen >= 2 && pp[0] == 'Q' && pp[1] == '\t') questCountForLog++;
			if (lineLen >= 3 && pp[0] == '-' && pp[1] == '-' && pp[2] == '-') { pp += 3; if (pp < endBuf && *pp == '\n') pp++; continue; }
			pp = lineEnd ? lineEnd + 1 : pp + lineLen;
		} }
		if (n != s_last_n || questCountForLog != s_last_quest_count) {
			s_last_n = n; s_last_quest_count = questCountForLog;
			std::string preview;
			for (int i = 0; i < n && i < 280; i++) {
				char c = questBuf[i];
				if (c == '\n') preview += "\\n";
				else if (c == '\r') preview += "\\r";
				else if (c == '\t') preview += "|";
				else if (c >= 32 && c < 127) preview += c;
				else preview += ".";
			}
			StarLogInfo("[Quests] ODOOM: bytes_from_api=%d assign_to_cvar=%zu quest_lines_parsed=%d preview=%.260s", n, assignLen, questCountForLog, preview.c_str());
		}
	}

	static std::string s_questListValue;
	s_questListValue.assign(questBuf, assignLen);
	UCVarValue v; v.String = (char*)s_questListValue.c_str();
	listVar->SetGenericRep(v, CVAR_String);

	std::string wantId;
	if (trackerIdVar && trackerIdVar->GetRealType() == CVAR_String) {
		const char* sid = trackerIdVar->GetGenericRep(CVAR_String).String;
		if (sid && sid[0]) wantId.assign(sid);
	}

	int questCount = 0;
	std::string trackerTitle, trackerObjective;
	std::string currentId, currentTitle;
	bool inTargetBlock = false;
	const char* p = questBuf;
	const char* endBuf = questBuf + n;
	while (p < endBuf && *p) {
		const char* lineEnd = (const char*)memchr(p, '\n', (size_t)(endBuf - p));
		size_t lineLen = lineEnd ? (size_t)(lineEnd - p) : (size_t)(endBuf - p);
		if (lineLen >= 3 && p[0] == '-' && p[1] == '-' && p[2] == '-') {
			inTargetBlock = false;
			p += 3;
			if (p < endBuf && *p == '\n') p++;
			continue;
		}
		if (lineLen >= 2 && p[0] == 'Q' && p[1] == '\t') {
			questCount++;
			currentId.clear();
			currentTitle.clear();
			/* Q, id, name, desc, status, pct - field 0=id, 1=name */
			const char* f = p + 2;
			const char* t0 = (const char*)memchr(f, '\t', (size_t)((p + lineLen) - f));
			if (t0 && t0 - f > 0) currentId.assign(f, (size_t)(t0 - f));
			if (t0 && t0 < p + lineLen) {
				const char* t1 = (const char*)memchr(t0 + 1, '\t', (size_t)((p + lineLen) - (t0 + 1)));
				if (t1 && t1 - (t0 + 1) > 0) currentTitle.assign(t0 + 1, (size_t)(t1 - (t0 + 1)));
			}
			if (currentTitle.size() > 120) currentTitle.resize(120);
			inTargetBlock = !wantId.empty() && (currentId == wantId);
			if (inTargetBlock) {
				trackerTitle = currentTitle;
				trackerObjective.clear();
			}
			p = lineEnd ? lineEnd + 1 : p + lineLen;
			continue;
		}
		if (lineLen >= 2 && p[0] == 'O' && p[1] == '\t' && inTargetBlock && trackerObjective.empty()) {
			/* O, id, desc, done - first incomplete objective (done=0); desc is 2nd field after id */
			const char* f0 = p + 2;
			size_t rest0 = (size_t)((p + lineLen) - f0);
			const char* f1 = (const char*)memchr(f0, '\t', rest0);
			if (f1 && f1 < p + lineLen) {
				f1++;
				size_t rest1 = (size_t)((p + lineLen) - f1);
				const char* f2 = (const char*)memchr(f1, '\t', rest1);
				if (f2 && f2 < p + lineLen) {
					const char* doneStart = f2 + 1;
					if (doneStart < p + lineLen && *doneStart == '0')
						trackerObjective.assign(f1, (size_t)(f2 - f1));
				}
			}
		}
		p = lineEnd ? lineEnd + 1 : p + lineLen;
	}

	/* When list was truncated for CVar, report only the number of quests in the truncated list so ZScript scroll/count match. */
	if (assignLen < (size_t)n) {
		int listCount = 0;
		const char* pp = questBuf;
		const char* endAssign = questBuf + assignLen;
		while (pp < endAssign && *pp) {
			const char* lineEnd = (const char*)memchr(pp, '\n', (size_t)(endAssign - pp));
			size_t lineLen = lineEnd ? (size_t)(lineEnd - pp) : (size_t)(endAssign - pp);
			if (lineLen >= 2 && pp[0] == 'Q' && pp[1] == '\t') listCount++;
			pp = lineEnd ? lineEnd + 1 : pp + lineLen;
		}
		questCount = listCount;
	}

	UCVarValue c; c.Int = questCount;
	countVar->SetGenericRep(c, CVAR_Int);
	/* Only update tracker title when we have real data; if we have tracker_quest_id but list is still loading, keep "Loading..." (set by OnAuthDone/frame pump). */
	if (trackerTitleVar && trackerTitleVar->GetRealType() == CVAR_String) {
		if (!trackerTitle.empty())
			{ UCVarValue t; t.String = (char*)trackerTitle.c_str(); trackerTitleVar->SetGenericRep(t, CVAR_String); }
		else if (wantId.empty())
			{ UCVarValue t; t.String = (char*)""; trackerTitleVar->SetGenericRep(t, CVAR_String); }
	}
	if (trackerObjVar && trackerObjVar->GetRealType() == CVAR_String) {
		UCVarValue o; o.String = (char*)(trackerObjective.empty() ? "" : trackerObjective.c_str());
		trackerObjVar->SetGenericRep(o, CVAR_String);
	}
	/* Tracker objectives (progress lines) and active index for HUD cycle (O key). Skip when placeholder "..." (loading). */
	if (!wantId.empty() && wantId != "...") {
		FBaseCVar* trackerObjLinesVar = FindCVar("odoom_quest_tracker_objectives", nullptr);
		FBaseCVar* trackerActiveVar = FindCVar("odoom_quest_tracker_active_index", nullptr);
		FBaseCVar* trackerActiveIdVar = FindCVar("odoom_quest_tracker_active_objective_id", nullptr);
		static char trackerObjBuf[512];
		int nObj = star_api_get_quest_tracker_objectives_string(wantId.c_str(), trackerObjBuf, sizeof(trackerObjBuf));
		if (nObj < 0) nObj = 0;
		if (nObj >= (int)sizeof(trackerObjBuf)) nObj = (int)sizeof(trackerObjBuf) - 1;
		trackerObjBuf[nObj] = '\0';
		static std::string s_tracker_objectives;
		s_tracker_objectives.assign(trackerObjBuf, (size_t)nObj);
		if (trackerObjLinesVar && trackerObjLinesVar->GetRealType() == CVAR_String) {
			UCVarValue vo; vo.String = (char*)s_tracker_objectives.c_str();
			trackerObjLinesVar->SetGenericRep(vo, CVAR_String);
		}
		/* Set active_index so the quest detail popup opens with the correct objective selected (not the last). */
		bool userHasActiveObjective = false;
		if (trackerActiveIdVar && trackerActiveIdVar->GetRealType() == CVAR_String) {
			const char* aid = trackerActiveIdVar->GetGenericRep(CVAR_String).String;
			userHasActiveObjective = (aid && aid[0] != '\0');
		}
		if (trackerActiveVar && trackerActiveVar->GetRealType() == CVAR_Int) {
			/* Active index from API (resolves from persisted active objective id or first incomplete). Tracker lines are display names only. */
			int activeIdx = star_api_get_quest_tracker_active_objective_index(wantId.c_str());
			if (activeIdx >= 0) {
				UCVarValue va; va.Int = activeIdx;
				trackerActiveVar->SetGenericRep(va, CVAR_Int);
			}
		}
		/* Persist tracker state to API only when ZScript sets odoom_quest_persist_active_now=1 (user pressed Enter on objective or K to set tracker). Avoids persisting wrong value from UI sync or CVar restore. */
		{
			FBaseCVar* persistNowVar = FindCVar("odoom_quest_persist_active_now", nullptr);
			if (persistNowVar && persistNowVar->GetRealType() == CVAR_Int && persistNowVar->GetGenericRep(CVAR_Int).Int != 0) {
				std::string cur_q, cur_o;
				if (trackerIdVar && trackerIdVar->GetRealType() == CVAR_String) {
					const char* c = trackerIdVar->GetGenericRep(CVAR_String).String;
					if (c) cur_q = c;
				}
				if (trackerActiveIdVar && trackerActiveIdVar->GetRealType() == CVAR_String) {
					const char* c = trackerActiveIdVar->GetGenericRep(CVAR_String).String;
					if (c) cur_o = c;
				}
				StarLogInfo("[Quests] ODOOM: Persisting tracker to API (user action): questId=%s objectiveId=%s", cur_q.c_str(), cur_o.c_str());
				star_api_set_active_quest(cur_q.empty() ? nullptr : cur_q.c_str(), cur_o.empty() ? nullptr : cur_o.c_str());
				UCVarValue zero; zero.Int = 0;
				persistNowVar->SetGenericRep(zero, CVAR_Int);
			}
		}
	}
}

/** Max bytes for each quest detail list CVar (prereqs, objectives, subquests). */
static const size_t ODOOM_QUEST_DETAIL_CVAR_MAX = 1024;

/** When odoom_quest_detail_quest_id is set, fill prereqs/objectives/subquests CVars from STAR API for the 2nd (detail) popup. */
static void ODOOM_RefreshQuestDetailCVars(void) {
	FBaseCVar* idVar = FindCVar("odoom_quest_detail_quest_id", nullptr);
	if (!idVar || idVar->GetRealType() != CVAR_String) return;
	const char* id = idVar->GetGenericRep(CVAR_String).String;
	if (!id || !id[0]) return;

	FBaseCVar* prereqVar = FindCVar("odoom_quest_detail_prereqs", nullptr);
	FBaseCVar* objVar = FindCVar("odoom_quest_detail_objectives", nullptr);
	FBaseCVar* subVar = FindCVar("odoom_quest_detail_subquests", nullptr);
	FBaseCVar* reqVar = FindCVar("odoom_quest_detail_requirements", nullptr);
	FBaseCVar* selObjVar = FindCVar("odoom_quest_detail_selected_objective_id", nullptr);
	if (!prereqVar || !objVar || !subVar || !reqVar || !selObjVar) return;

	static char buf[1024];
	int nr = star_api_get_quest_prereqs_string(id, buf, sizeof(buf));
	if (nr < 0) nr = 0;
	if (nr >= (int)sizeof(buf)) nr = (int)sizeof(buf) - 1;
	buf[nr] = '\0';
	size_t len = (size_t)nr;
	if (len > ODOOM_QUEST_DETAIL_CVAR_MAX) len = ODOOM_QUEST_DETAIL_CVAR_MAX;
	static std::string s_prereqs;
	s_prereqs.assign(buf, len);
	UCVarValue vp; vp.String = (char*)s_prereqs.c_str();
	prereqVar->SetGenericRep(vp, CVAR_String);

	int no = star_api_get_quest_objectives_string(id, buf, sizeof(buf));
	if (no < 0) no = 0;
	if (no >= (int)sizeof(buf)) no = (int)sizeof(buf) - 1;
	buf[no] = '\0';
	len = (size_t)no;
	if (len > ODOOM_QUEST_DETAIL_CVAR_MAX) len = ODOOM_QUEST_DETAIL_CVAR_MAX;
	static std::string s_obj;
	s_obj.assign(buf, len);
	UCVarValue vo; vo.String = (char*)s_obj.c_str();
	objVar->SetGenericRep(vo, CVAR_String);

	int ns = star_api_get_quest_sub_quests_string(id, buf, sizeof(buf));
	if (ns < 0) ns = 0;
	if (ns >= (int)sizeof(buf)) ns = (int)sizeof(buf) - 1;
	buf[ns] = '\0';
	len = (size_t)ns;
	if (len > ODOOM_QUEST_DETAIL_CVAR_MAX) len = ODOOM_QUEST_DETAIL_CVAR_MAX;
	static std::string s_sub;
	s_sub.assign(buf, len);
	UCVarValue vs; vs.String = (char*)s_sub.c_str();
	subVar->SetGenericRep(vs, CVAR_String);

	const char* selObj = selObjVar->GetRealType() == CVAR_String ? selObjVar->GetGenericRep(CVAR_String).String : nullptr;
	if (!selObj) selObj = "";
	int nreq = star_api_get_quest_objective_requirements_string(id, selObj, buf, sizeof(buf));
	if (nreq < 0) nreq = 0;
	if (nreq >= (int)sizeof(buf)) nreq = (int)sizeof(buf) - 1;
	buf[nreq] = '\0';
	len = (size_t)nreq;
	if (len > ODOOM_QUEST_DETAIL_CVAR_MAX) len = ODOOM_QUEST_DETAIL_CVAR_MAX;
	static std::string s_req;
	s_req.assign(buf, len);
	UCVarValue vr; vr.String = (char*)s_req.c_str();
	reqVar->SetGenericRep(vr, CVAR_String);
}

static void ODOOM_OnAuthDone(void* user_data);
static void ODOOM_OnSendItemDone(void* user_data);
static void ODOOM_OnUseItemDone(void* user_data);

static void StarLogInfo(const char* fmt, ...);
static void StarLogError(const char* fmt, ...);

/** C# client flushes add_item queue in background; no sync started from ODOOM. */
static void ODOOM_StartInventorySyncIfNeeded(void) {
	/* No-op: heavy lifting (sync, local delta, multithreading) is in C# StarApiClient. */
}

/** Called from main thread by star_sync_pump() when auth completes. */
static void ODOOM_OnAuthDone(void* user_data) {
	(void)user_data;
	int success = 0;
	char username_buf[64] = {};
	char avatar_id_buf[64] = {};
	char error_buf[256] = {};
	if (!star_sync_auth_get_result(&success, username_buf, sizeof(username_buf), avatar_id_buf, sizeof(avatar_id_buf), error_buf, sizeof(error_buf)))
		return;
	char jwt_buf[2048] = {};
	star_sync_auth_get_result_jwt(jwt_buf, sizeof(jwt_buf));
	g_star_async_auth_pending = false;
	if (success) {
		/* Persist JWT from auth result so oasisstar.json has jwt_token for autobeamin (avoids relying on get_current_jwt export). */
		if (jwt_buf[0]) {
			std::strncpy(g_odoom_saved_jwt, jwt_buf, sizeof(g_odoom_saved_jwt) - 1);
			g_odoom_saved_jwt[sizeof(g_odoom_saved_jwt) - 1] = '\0';
		}
		g_star_initialized = true;
		g_star_frames_since_beamin = 0;  /* Grace period: don't consume keys on door checks for a few seconds after beamin. */
		g_star_just_beamed_in = true;    /* Next frame: refresh gold/silver key CVars so OQ keys appear with Doom keycards. */
		g_star_logged_runtime_auth_failure = false;
		g_star_logged_missing_auth_config = false;
		g_star_effective_username = username_buf;
		g_star_effective_avatar_id = avatar_id_buf;
		g_star_config.avatar_id = g_star_effective_avatar_id.empty() ? nullptr : g_star_effective_avatar_id.c_str();
		odoom_star_username = g_star_effective_username.c_str();
		StarApplyBeamFacePreference();
		/* Obsolete: star_api_refresh_avatar_xp() redundant with star_api_refresh_avatar_profile() which does same GET and also loads quest/objective + callback. */
		// if (!g_star_refresh_xp_called_this_session) {
		// 	g_star_refresh_xp_called_this_session = true;
		// 	star_api_refresh_avatar_xp();
		// }
		/* Load avatar (XP + active quest/objective) so we can restore tracker state. Profile load is async so get_active_quest_id is not ready yet; show "Loading..." immediately so tracker appears (like Quake). */
		star_api_refresh_avatar_profile();
		{
			/* Placeholder tracker id so HUD shows "Loading..." before profile returns; frame pump will replace with real id when get_active_quest_id returns. */
			static const char s_tracker_loading_placeholder[] = "...";
			FBaseCVar* trackerIdVar = FindCVar("odoom_quest_tracker_quest_id", nullptr);
			if (trackerIdVar && trackerIdVar->GetRealType() == CVAR_String) {
				UCVarValue u; u.String = (char*)s_tracker_loading_placeholder;
				trackerIdVar->SetGenericRep(u, CVAR_String);
			}
			FBaseCVar* titleVar = FindCVar("odoom_quest_tracker_title", nullptr);
			if (titleVar && titleVar->GetRealType() == CVAR_String) {
				UCVarValue t; t.String = (char*)"Loading...";
				titleVar->SetGenericRep(t, CVAR_String);
			}
			char qid[64] = {};
			char oid[64] = {};
			if (star_api_get_active_quest_id(qid, sizeof(qid)) && qid[0]) {
				if (trackerIdVar && trackerIdVar->GetRealType() == CVAR_String) {
					UCVarValue u; u.String = qid;
					trackerIdVar->SetGenericRep(u, CVAR_String);
				}
				if (star_api_get_active_objective_id(oid, sizeof(oid)) && oid[0]) {
					FBaseCVar* v = FindCVar("odoom_quest_tracker_active_objective_id", nullptr);
					if (v && v->GetRealType() == CVAR_String) {
						UCVarValue u; u.String = oid;
						v->SetGenericRep(u, CVAR_String);
					}
				}
			}
			/* Start loading quest list so tracker title/objective show without opening popup (ODOOM_RefreshQuestCVars will use cache when ready). */
#ifdef ODOOM_STAR_API_HAS_REFRESH_QUEST_BACKGROUND
			star_api_refresh_quest_cache_in_background();
#endif
			ODOOM_RefreshQuestCVars();  /* push once immediately in case cache already has data */
		}
		/* Persist session to oasisstar.json immediately so we stay logged in after restart (or if game crashes before exit). */
		ODOOM_SaveStarConfigToFiles();
		/* C# client flushes queued add_item jobs in background; overlay will refresh from get_inventory when opened. */
		Printf(PRINT_NONOTIFY, "Beam-in successful. Cross-game features enabled.\n");
	} else {
		Printf(PRINT_NONOTIFY, "Beam-in failed: %s\n", error_buf[0] ? error_buf : star_api_get_last_error());
	}
}

/** Called from main thread by star_sync_pump() when send-item completes (same pattern as Quake). */
static void ODOOM_OnSendItemDone(void* user_data) {
	(void)user_data;
	int success = 0;
	char err_buf[384] = {};
	if (!star_sync_send_item_get_result(&success, err_buf, sizeof(err_buf)))
		return;
	static char s_send_status_buf[384];
	if (success) {
		std::strncpy(s_send_status_buf, "Item sent.", sizeof(s_send_status_buf) - 1);
		s_send_status_buf[sizeof(s_send_status_buf) - 1] = '\0';
		Printf(PRINT_NONOTIFY, "Item sent.\n");
		ODOOM_RefreshOverlayFromClient();
		g_odoom_last_sent_item_name.clear();
		g_odoom_last_sent_qty = 0;
	} else {
		std::snprintf(s_send_status_buf, sizeof(s_send_status_buf), "Send failed: %s", err_buf[0] ? err_buf : "Unknown error");
		Printf(PRINT_NONOTIFY, "Send failed: %s\n", err_buf[0] ? err_buf : "Unknown error");
		g_odoom_last_sent_item_name.clear();
		g_odoom_last_sent_qty = 0;
	}
	FBaseCVar* statusVar = FindCVar("odoom_send_status", nullptr);
	if (statusVar && statusVar->GetRealType() == CVAR_String) {
		UCVarValue val; val.String = s_send_status_buf;
		statusVar->SetGenericRep(val, CVAR_String);
	}
	/* Do NOT refetch inventory here; we updated the cache above. Keeps API hits to minimum. */
}

/** Strip UI-only [NFT] / [BOSSNFT] prefix so we match API-stored names. */
static std::string ODOOM_StripNftDisplayPrefix(const std::string& name) {
	const size_t np = (size_t)(-1);
	if (name.empty()) return name;
	size_t start = name.find_first_not_of(" \t");
	if (start == np) return name;
	size_t len = name.size() - start;
	if (len >= 6 && name.compare(start, 6, "[NFT] ") == 0)
		return name.substr(start + 6);
	if (len >= 10 && name.compare(start, 10, "[BOSSNFT] ") == 0)
		return name.substr(start + 10);
	return name;
}

/** Parse (+X) from name/description (like OQUAKE). Returns 0 if not found. */
static int ODOOM_ParseAmountFromDescription(const std::string& s) {
	size_t pos = s.find(" (+");
	if (pos == std::string::npos) return 0;
	pos += 3;
	if (pos >= s.size()) return 0;
	char* end = nullptr;
	long v = strtol(s.c_str() + pos, &end, 10);
	if (end == s.c_str() + pos || v <= 0 || v > 9999) return 0;
	return (int)v;
}

/** Returns true if using this item would exceed config max health/armor (or already at max). Sets *out_msg to the message to show.
 *  If description is non-empty and contains (+X), that amount is used; otherwise name-based heuristics. */
static bool ODOOM_WouldUseExceedMax(const std::string& name, const std::string& type, const char** out_msg, const char* description = nullptr) {
	*out_msg = nullptr;
	FLevelLocals* level = primaryLevel;
	if (!level) return false;
	player_t* player = level->GetConsolePlayer();
	if (!player || !player->mo) return false;
	const size_t np = (size_t)(-1);
	std::string n = ODOOM_StripNftDisplayPrefix(name);
	const bool isHealth = (type.find("Health") != np || type.find("health") != np ||
		n.find("Stimpack") != np || n.find("Medikit") != np || n.find("Health Bonus") != np ||
		n.find("Soul") != np || n.find("Mega") != np || n.find("Health") != np);
	const bool isArmor = (type.find("Armor") != np || type.find("armor") != np ||
		n.find("Armor") != np || n.find("Blue") != np || n.find("Green") != np || n.find("Yellow") != np);
	int configMaxH = 200, configMaxA = 200;
	{ FBaseCVar* v = FindCVar("odoom_star_max_health", nullptr); if (v && v->GetRealType() == CVAR_Int) configMaxH = v->GetGenericRep(CVAR_Int).Int; if (configMaxH <= 0) configMaxH = 200; }
	{ FBaseCVar* v = FindCVar("odoom_star_max_armor", nullptr); if (v && v->GetRealType() == CVAR_Int) configMaxA = v->GetGenericRep(CVAR_Int).Int; if (configMaxA <= 0) configMaxA = 200; }
	if (isHealth) {
		int amount = (description && description[0]) ? ODOOM_ParseAmountFromDescription(std::string(description)) : 0;
		if (amount <= 0) amount = ODOOM_ParseAmountFromDescription(n);
		if (amount <= 0) {
			if (n.find("Stimpack") != np) amount = 10;
			else if (n.find("Medikit") != np) amount = 25;
			else if (n.find("Health Bonus") != np) amount = 1;
			else if (n.find("Soul Sphere") != np || n.find("Soul") != np) amount = 100;
			else if (n.find("Mega") != np && (n.find("Sphere") != np || n.find("Health") != np)) amount = 200;
			else if (n.find("Large Health") != np) amount = 50;
			else if (n.find("Mega Health") != np) amount = 100;
			else if (n.find("Health") != np) amount = 25;
		}
		int cur = player->mo->health;
		if (cur >= configMaxH) { *out_msg = "You cannot use this because you are already at max health."; return true; }
		if (cur + amount > configMaxH) { *out_msg = "You cannot use this because you are already at max health."; return true; }
	}
	if (isArmor) {
		int amount = ODOOM_ParseAmountFromDescription(n);
		if (amount <= 0) {
			if (n.find("Blue") != np || n.find("Mega") != np) amount = 200;
			else if (n.find("Green") != np || n.find("Yellow") != np) amount = 100;
			else amount = 100;
		}
		AActor* arm = player->mo->FindInventory(FName("BasicArmor"), true);
		int cur = arm ? arm->IntVar(FName("Amount")) : 0;
		if (cur >= configMaxA) { *out_msg = "You cannot use this because you are already at max armor."; return true; }
		if (cur + amount > configMaxA) { *out_msg = "You cannot use this because you are already at max armor."; return true; }
	}
	return false;
}

/** Apply health or armor to the console player when using a Health/Armor item from STAR inventory.
 *  Use-from-inventory ALWAYS adds the item's amount; (+X) in name/description overrides name-based amount (like OQUAKE). */
static void ODOOM_ApplyHealthOrArmor(const std::string& name, const std::string& type, const char* description = nullptr) {
	FLevelLocals* level = primaryLevel;
	if (!level) return;
	player_t* player = level->GetConsolePlayer();
	if (!player || !player->mo) return;
	const size_t np = (size_t)(-1);
	std::string n = ODOOM_StripNftDisplayPrefix(name);
	const bool isHealth = (type.find("Health") != np || type.find("health") != np ||
		n.find("Stimpack") != np || n.find("Medikit") != np || n.find("Health Bonus") != np ||
		n.find("Soul") != np || n.find("Mega") != np || n.find("Health") != np);
	const bool isArmor = (type.find("Armor") != np || type.find("armor") != np ||
		n.find("Armor") != np || n.find("Blue") != np || n.find("Green") != np || n.find("Yellow") != np);
	int configMaxH = 200, configMaxA = 200;
	{ FBaseCVar* v = FindCVar("odoom_star_max_health", nullptr); if (v && v->GetRealType() == CVAR_Int) configMaxH = v->GetGenericRep(CVAR_Int).Int; if (configMaxH <= 0) configMaxH = 200; }
	{ FBaseCVar* v = FindCVar("odoom_star_max_armor", nullptr); if (v && v->GetRealType() == CVAR_Int) configMaxA = v->GetGenericRep(CVAR_Int).Int; if (configMaxA <= 0) configMaxA = 200; }
	g_star_deferred_apply_health = false;
	g_star_deferred_apply_armor = false;
	g_star_deferred_health_value = -1;
	g_star_deferred_armor_value = -1;
	if (isHealth) {
		int amount = (description && description[0]) ? ODOOM_ParseAmountFromDescription(std::string(description)) : 0;
		if (amount <= 0) amount = ODOOM_ParseAmountFromDescription(n);
		if (amount <= 0) {
			if (n.find("Stimpack") != np) amount = 10;
			else if (n.find("Medikit") != np) amount = 25;
			else if (n.find("Health Bonus") != np) amount = 1;
			else if (n.find("Soul Sphere") != np || n.find("Soul") != np) amount = 100;
			else if (n.find("Mega") != np && (n.find("Sphere") != np || n.find("Health") != np)) amount = 200;
			else if (n.find("Large Health") != np) amount = 50;
			else if (n.find("Mega Health") != np) amount = 100;
			else if (n.find("Health") != np) amount = 25;
		}
		/* Use engine path so HUD updates; config max allows over 200 when set higher. */
		if (P_GiveBody(player->mo, amount, configMaxH)) {
			g_star_deferred_apply_health = true;
			g_star_deferred_health_value = player->mo->health;
			Printf(PRINT_HIGH, "STAR: used %s, health now %d\n", n.c_str(), player->mo->health);
		}
	}
	if (isArmor) {
		int amount = (description && description[0]) ? ODOOM_ParseAmountFromDescription(std::string(description)) : 0;
		if (amount <= 0) amount = ODOOM_ParseAmountFromDescription(n);
		if (amount <= 0) {
			if (n.find("Blue") != np || n.find("Mega") != np) amount = 200;
			else if (n.find("Green") != np || n.find("Yellow") != np) amount = 100;
			else amount = 100;
		}
		AActor* arm = player->mo->FindInventory(FName("BasicArmor"), true);
		if (arm) {
			int& a = arm->IntVar(FName("Amount"));
			{ int newA = a + amount; int cap = configMaxA; a = (newA < cap) ? newA : cap; }
			g_star_deferred_apply_armor = true;
			g_star_deferred_armor_value = a;
			Printf(PRINT_HIGH, "STAR: used %s, armor now %d\n", n.c_str(), a);
		}
	}
}

/** Find first health (want_health true) or armor (want_health false) item in STAR inventory. Returns true if found and sets out_name/out_type. */
static bool ODOOM_FindFirstHealthOrArmorInInventory(bool want_health, std::string* out_name, std::string* out_type) {
	star_item_list_t* list = nullptr;
	if (star_api_get_inventory(&list) != STAR_API_SUCCESS || !list || !list->items) return false;
	const size_t np = (size_t)(-1);
	for (size_t i = 0; i < list->count; i++) {
		const char* n = list->items[i].name;
		const char* t = list->items[i].item_type;
		if (!n || !t) continue;
		std::string name(n);
		std::string type(t);
		bool is_health = (type.find("Health") != np || type.find("health") != np ||
			name.find("Stimpack") != np || name.find("Medikit") != np || name.find("Health Bonus") != np ||
			name.find("Soul") != np || name.find("Mega") != np || name.find("Health") != np);
		bool is_armor = (type.find("Armor") != np || type.find("armor") != np ||
			name.find("Armor") != np || name.find("Blue") != np || name.find("Green") != np || name.find("Yellow") != np);
		if (want_health && is_health) { *out_name = name; *out_type = type; star_api_free_item_list(list); return true; }
		if (!want_health && is_armor) { *out_name = name; *out_type = type; star_api_free_item_list(list); return true; }
	}
	star_api_free_item_list(list);
	return false;
}

/** Called when use-item from inventory (E on STAR row) completes. Defer Health/Armor apply to next frame so the engine does not overwrite it. */
static void ODOOM_OnUseItemFromInventoryDone(void* user_data) {
	(void)user_data;
	int success = 0;
	char err_buf[384] = {};
	if (!star_sync_use_item_get_result(&success, err_buf, sizeof(err_buf)))
		return;
	if (success && !g_star_use_pending_name.empty()) {
		g_star_deferred_apply_name = g_star_use_pending_name;
		g_star_deferred_apply_type = g_star_use_pending_type;
		g_star_deferred_apply_description = g_star_use_pending_description;
		g_star_deferred_apply_frames = 35; /* re-apply for ~1 sec so engine/voodoo overwrites don't revert health */
	}
	g_star_use_pending_name.clear();
	g_star_use_pending_type.clear();
	g_star_use_pending_description.clear();
	if (success)
		ODOOM_RefreshOverlayFromClient();
	else if (err_buf[0])
		StarLogError("star_api_use_item failed: %s", err_buf);
}

/** Called from main thread by star_sync_pump() when use-item (e.g. door key) completes. */
static void ODOOM_OnUseItemDone(void* user_data) {
	(void)user_data;
	int success = 0;
	char err_buf[384] = {};
	if (!star_sync_use_item_get_result(&success, err_buf, sizeof(err_buf)))
		return;
	if (success)
		ODOOM_RefreshOverlayFromClient();
	else if (err_buf[0])
		StarLogError("star_api_use_item failed: %s", err_buf);
}

/** Called every frame from the main loop (see patch_uzdoom_engine.ps1: d_main and g_game). Must run so send/auth/inventory callbacks are invoked. */
void ODOOM_InventoryInputCaptureFrame(void)
{
	/* Deferred health/armor is applied in ODOOM_PostTic (after the tic) so the HUD is not overwritten. */

	/* Decrement toast frame counters so ZScript shows messages for their duration. */
	{
		FBaseCVar* toastFramesCv = FindCVar("odoom_star_toast_frames", nullptr);
		if (toastFramesCv && toastFramesCv->GetRealType() == CVAR_Int) {
			int f = toastFramesCv->GetGenericRep(CVAR_Int).Int;
			if (f > 0) {
				UCVarValue v; v.Int = f - 1;
				toastFramesCv->SetGenericRep(v, CVAR_Int);
			}
		}
		FBaseCVar* pickupFramesCv = FindCVar("odoom_star_pickup_toast_frames", nullptr);
		if (pickupFramesCv && pickupFramesCv->GetRealType() == CVAR_Int) {
			int f = pickupFramesCv->GetGenericRep(CVAR_Int).Int;
			if (f > 0) {
				UCVarValue v; v.Int = f - 1;
				pickupFramesCv->SetGenericRep(v, CVAR_Int);
			}
		}
	}

	star_sync_pump();

	/* Once STAR is initialized, bind Q to quest popup so it opens the popup instead of e.g. quit. */
	if (g_star_initialized) {
		static bool s_odoom_q_bound_once = false;
		if (!s_odoom_q_bound_once) {
			C_DoCommand("bind Q \"odoom_quest_toggle\"");
			s_odoom_q_bound_once = true;
		}
	}

	/* Show mint result in console when background pickup-with-mint completes (NFT ID + Hash). */
	{
		char item_buf[256] = {}, nft_buf[128] = {}, hash_buf[256] = {};
		if (star_api_consume_last_mint_result(item_buf, sizeof(item_buf), nft_buf, sizeof(nft_buf), hash_buf, sizeof(hash_buf)))
			Printf(PRINT_HIGH, "NFT minted: %s | ID: %s | Hash: %s\n", item_buf, nft_buf, hash_buf[0] ? hash_buf : "(none)");
	}
	/* Show any background errors (mint/add_item failure or pickup not queued) in console. */
	{
		char err_buf[512] = {};
		if (star_api_consume_last_background_error(err_buf, sizeof(err_buf)))
			Printf(PRINT_HIGH, "%s\n", err_buf);
	}
	/* Show STAR log messages in console only when star debug is on. Quest logs are file-only to avoid crashes when consuming. */
	star_api_set_debug(g_star_debug_logging ? 1 : 0);
	if (g_star_debug_logging) {
		char log_buf[512] = {};
		for (int i = 0; i < 5; i++) {
			if (!star_api_consume_console_log(log_buf, sizeof(log_buf)))
				break;
			Printf(PRINT_HIGH, "[STAR] %s\n", log_buf);
		}
	} else {
		char log_buf[512] = {};
		while (star_api_consume_console_log(log_buf, sizeof(log_buf))) {}
	}

	if (g_star_frames_since_beamin < STAR_DOOR_CONSUME_GRACE_FRAMES)
		g_star_frames_since_beamin++;

	/* Re-apply oasisstar.json after a short delay so mint etc. override ini load. */
	if (g_odoom_reapply_json_frames == 0) {
		g_odoom_reapply_json_frames = -1;
		if (!g_odoom_json_config_path.empty())
			ODOOM_LoadJsonConfig(g_odoom_json_config_path.c_str());
	} else if (g_odoom_reapply_json_frames > 0) {
		g_odoom_reapply_json_frames--;
	}

	/* ZScript reads this so it only gives/shows OQuake keys when beamed in. */
	{
		FBaseCVar* beamedVar = FindCVar("odoom_star_beamed_in", nullptr);
		if (beamedVar && beamedVar->GetRealType() == CVAR_Int) {
			UCVarValue u; u.Int = g_star_initialized ? 1 : 0;
			beamedVar->SetGenericRep(u, CVAR_Int);
		}
	}

	/* Quest: set active from popup first (ZScript set odoom_quest_set_active_id + odoom_quest_set_active_do_it=1 last frame). Process before key capture so we don't miss the one-frame edge. */
	{
		FBaseCVar* setActiveDoVar = FindCVar("odoom_quest_set_active_do_it", nullptr);
		FBaseCVar* setActiveIdVar = FindCVar("odoom_quest_set_active_id", nullptr);
		if (g_star_initialized && setActiveDoVar && setActiveDoVar->GetRealType() == CVAR_Int && setActiveDoVar->GetGenericRep(CVAR_Int).Int != 0) {
			const char* questId = nullptr;
			if (setActiveIdVar && setActiveIdVar->GetRealType() == CVAR_String)
				questId = setActiveIdVar->GetGenericRep(CVAR_String).String;
			if (questId && questId[0]) {
				star_api_start_quest(questId);
				/* Do not refresh here: C# client updates cache when StartQuestAsync completes (UpdateQuestStatusInCache). Next 60-frame refresh or cache read will show updated list (like Quake). */
			}
			UCVarValue zero; zero.Int = 0;
			setActiveDoVar->SetGenericRep(zero, CVAR_Int);
			if (setActiveIdVar && setActiveIdVar->GetRealType() == CVAR_String) {
				UCVarValue empty; empty.String = (char*)"";
				setActiveIdVar->SetGenericRep(empty, CVAR_String);
			}
		}
	}

	FBaseCVar* openVar = FindCVar("odoom_inventory_open", nullptr);
	FBaseCVar* questPopupVar = FindCVar("odoom_quest_popup_open", nullptr);
	const bool open = (openVar && openVar->GetRealType() == CVAR_Int && openVar->GetGenericRep(CVAR_Int).Int != 0);
	const bool questPopupOpen = (questPopupVar && questPopupVar->GetRealType() == CVAR_Int && questPopupVar->GetGenericRep(CVAR_Int).Int != 0);
	const bool anyPopupOpen = open || questPopupOpen;

	/* Refresh overlay from client every frame while open (merge is in-memory, so pickups show immediately). When not beamed in we push empty. */
	if (open) {
		ODOOM_RefreshOverlayFromClient();
		/* Use STAR item from inventory (E on selected STAR row): ZScript set odoom_star_use_do_it=1, name and type. */
		if (!star_sync_use_item_in_progress()) {
			FBaseCVar* doCv = FindCVar("odoom_star_use_do_it", nullptr);
			if (doCv && doCv->GetRealType() == CVAR_Int && doCv->GetGenericRep(CVAR_Int).Int != 0) {
				FBaseCVar* nameCv = FindCVar("odoom_star_use_item_name", nullptr);
				FBaseCVar* typeCv = FindCVar("odoom_star_use_item_type", nullptr);
				FBaseCVar* descCv = FindCVar("odoom_star_use_item_description", nullptr);
				const char* nameStr = (nameCv && nameCv->GetRealType() == CVAR_String) ? nameCv->GetGenericRep(CVAR_String).String : "";
				const char* typeStr = (typeCv && typeCv->GetRealType() == CVAR_String) ? typeCv->GetGenericRep(CVAR_String).String : "Item";
				const char* descStr = (descCv && descCv->GetRealType() == CVAR_String) ? descCv->GetGenericRep(CVAR_String).String : "";
				if (nameStr && nameStr[0]) {
					std::string nameS(nameStr);
					std::string typeS(typeStr ? typeStr : "");
					const char* blockMsg = nullptr;
					if (ODOOM_WouldUseExceedMax(nameS, typeS, &blockMsg, descStr && descStr[0] ? descStr : nullptr)) {
						if (blockMsg) {
							/* Show only in toast once; don't reset if already showing (avoids spam). */
							FBaseCVar* toastFramesCv = FindCVar("odoom_star_toast_frames", nullptr);
							int showFrames = (toastFramesCv && toastFramesCv->GetRealType() == CVAR_Int) ? toastFramesCv->GetGenericRep(CVAR_Int).Int : 0;
							if (showFrames <= 0) {
								FBaseCVar* toastMsgCv = FindCVar("odoom_star_toast_message", nullptr);
								if (toastMsgCv && toastMsgCv->GetRealType() == CVAR_String) {
									UCVarValue val; val.String = (char*)blockMsg;
									toastMsgCv->SetGenericRep(val, CVAR_String);
								}
								if (toastFramesCv && toastFramesCv->GetRealType() == CVAR_Int) {
									UCVarValue v; v.Int = 105; /* 3 sec at 35 fps */
									toastFramesCv->SetGenericRep(v, CVAR_Int);
								}
							}
						}
						UCVarValue u; u.Int = 0;
						doCv->SetGenericRep(u, CVAR_Int);
					} else {
						g_star_use_pending_name = nameStr;
						g_star_use_pending_type = typeStr ? typeStr : "";
						g_star_use_pending_description = (descStr && descStr[0]) ? descStr : "";
						star_sync_use_item_start(nameStr, "odoom_use", ODOOM_OnUseItemFromInventoryDone, nullptr);
						UCVarValue u; u.Int = 0;
						doCv->SetGenericRep(u, CVAR_Int);
					}
				}
			}
		}
	} else if (g_star_initialized) {
		/* First frame after beam-in: refresh gold/silver key CVars immediately so OQ keys appear with Doom keycards (no wait for console close). */
		if (g_star_just_beamed_in) {
			g_star_just_beamed_in = false;
			star_item_list_t* list = nullptr;
			if (star_api_get_inventory(&list) == STAR_API_SUCCESS && list) {
				ODOOM_UpdateStarKeyHudCVars(list);
				star_api_free_item_list(list);
			}
		}
		/* When overlay closed, periodically refresh gold/silver key CVars so HUD shows OQuake keys after load.
		 * Use 10 frames (~0.3 s) so keys appear soon after beam-in; C# client serves from cache. */
		static int s_key_hud_frames = 0;
		if (++s_key_hud_frames >= 10) {
			s_key_hud_frames = 0;
			star_item_list_t* list = nullptr;
			if (star_api_get_inventory(&list) == STAR_API_SUCCESS && list) {
				ODOOM_UpdateStarKeyHudCVars(list);
				star_api_free_item_list(list);
			}
		}
	}

	if (anyPopupOpen && !g_odoom_inventory_bindings_captured)
	{
		/* Clear arrow, movement, and inventory key bindings so game doesn't receive them (OQuake-style). Also when quest popup is open so arrows/Enter drive quest list. */
		C_DoCommand("bind uparrow \"\"");
		C_DoCommand("bind downarrow \"\"");
		C_DoCommand("bind leftarrow \"\"");
		C_DoCommand("bind rightarrow \"\"");
		C_DoCommand("bind W \"\"");
		C_DoCommand("bind S \"\"");
		C_DoCommand("bind A \"\"");
		C_DoCommand("bind D \"\"");
		C_DoCommand("bind E \"\"");
		C_DoCommand("bind C \"\"");
		C_DoCommand("bind F \"\"");
		C_DoCommand("bind Z \"\"");
		C_DoCommand("bind X \"\"");
		C_DoCommand("bind Q \"\"");  /* Q = quest popup; prevent engine from using it (e.g. quit) */
		/* I, O, P cleared so they only affect popup; ZScript will read odoom_key_i/o/p from raw state */
		C_DoCommand("bind I \"\"");
		C_DoCommand("bind O \"\"");
		C_DoCommand("bind P \"\"");
		C_DoCommand("bind enter \"\"");
		C_DoCommand("bind \"KP-Enter\" \"\"");
		C_DoCommand("bind pgup \"\"");
		C_DoCommand("bind pgdn \"\"");
		C_DoCommand("bind home \"\"");
		C_DoCommand("bind end \"\"");
		C_DoCommand("bind K \"\"");  /* K = Start quest / Set tracker in quest popup; prevent engine from using it */
		C_DoCommand("bind \"1\" \"\"");
		C_DoCommand("bind \"2\" \"\"");
		C_DoCommand("bind \"3\" \"\"");
		g_odoom_inventory_bindings_captured = true;
	}
		else if (!anyPopupOpen && g_odoom_inventory_bindings_captured)
	{
		/* Restore bindings for keys we cleared when opening overlay or quest popup. Do not touch 0-9; game handles weapon slots by default. */
		C_DoCommand("bind uparrow \"+forward\"");
		C_DoCommand("bind downarrow \"+back\"");
		C_DoCommand("bind leftarrow \"+left\"");
		C_DoCommand("bind rightarrow \"+right\"");
		C_DoCommand("bind W \"+forward\"");
		C_DoCommand("bind S \"+back\"");
		C_DoCommand("bind A \"+moveleft\"");
		C_DoCommand("bind D \"+moveright\"");
		C_DoCommand("bind E \"+use\"");
		C_DoCommand("bind A \"+moveleft\"");
		C_DoCommand("bind C \"odoom_use_health\"");
		C_DoCommand("bind F \"odoom_use_armor\"");
		C_DoCommand("bind Z \"+user4\"");
		C_DoCommand("bind X \"+reload\"");
		C_DoCommand("bind I \"+user1\"");
		C_DoCommand("bind O \"+user2\"");
		C_DoCommand("bind P \"+user3\"");
		C_DoCommand("bind enter \"+use\"");
		C_DoCommand("bind \"KP-Enter\" \"+use\"");
		C_DoCommand("bind Q \"odoom_quest_toggle\"");  /* Q opens quest popup (fallback if raw key not available) */
		C_DoCommand("bind K \"\"");  /* leave K unbound so user can bind for quest Start/Set if desired */
		C_DoCommand("bind pgup \"\"");
		C_DoCommand("bind pgdn \"\"");
		C_DoCommand("bind home \"\"");
		C_DoCommand("bind end \"\"");
		C_DoCommand("bind \"1\" \"weapon 1\"");
		C_DoCommand("bind \"2\" \"weapon 2\"");
		C_DoCommand("bind \"3\" \"weapon 3\"");
		g_odoom_inventory_bindings_captured = false;
	}

	/* Always feed raw key state into CVars so ZScript can open inventory with I (keyIPressed) when closed and drive popup when open. */
	{
		int up   = ODOOM_GetRawKeyDown(ODOOM_K_UP);
		int down = ODOOM_GetRawKeyDown(ODOOM_K_DOWN);
		int left = ODOOM_GetRawKeyDown(ODOOM_K_LEFT);
		int right= ODOOM_GetRawKeyDown(ODOOM_K_RIGHT);
		int use  = ODOOM_GetRawKeyDown('E');
		int a    = ODOOM_GetRawKeyDown('A');
		int c    = ODOOM_GetRawKeyDown('C');
		int z    = ODOOM_GetRawKeyDown('Z');
		int x    = ODOOM_GetRawKeyDown('X');
		int i    = ODOOM_GetRawKeyDown('I');
		int o    = ODOOM_GetRawKeyDown('O');
		int p    = ODOOM_GetRawKeyDown('P');
		int keyS = ODOOM_GetRawKeyDown('S');
		int keyT = ODOOM_GetRawKeyDown('T');
		int enter= ODOOM_GetRawKeyDown(ODOOM_K_RETURN);
		int pgup  = ODOOM_GetRawKeyDown(ODOOM_K_PAGEUP);
		int pgdown= ODOOM_GetRawKeyDown(ODOOM_K_PAGEDOWN);
		int home  = ODOOM_GetRawKeyDown(ODOOM_K_HOME);
		int endkey= ODOOM_GetRawKeyDown(ODOOM_K_END);
		int q     = ODOOM_GetRawKeyDown('Q');
		/* Use uppercase so GetAsyncKeyState gets correct VK (0x42/0x4E/0x4D on Windows) */
		int keyB  = ODOOM_GetRawKeyDown('B');
		int keyN  = ODOOM_GetRawKeyDown('N');
		int keyM  = ODOOM_GetRawKeyDown('M');
		int keyK  = ODOOM_GetRawKeyDown('K');
		int backspace = ODOOM_GetRawKeyDown(ODOOM_K_BACKSPACE);
		/* Merge Enter into use so ZScript sees keyUsePressed for both E and Enter (confirm/close) */
		use = (use || enter) ? 1 : 0;
		ODOOM_InventorySetKeyState(up, down, left, right, use, a, c, z, x, i, o, p, keyS, keyT, q, enter, pgup, pgdown, home, endkey, keyB, keyN, keyM, keyK, backspace);
		/* K = Start/Set quest: drive from C++ using odoom_quest_selected_id (ZScript sets every frame) so we don't rely on one-frame CVar handoff. */
		{
			static int s_key_k_was_down = 0;
			if (g_star_initialized && keyK)
			{
				FBaseCVar* questPopupVar = FindCVar("odoom_quest_popup_open", nullptr);
				int qOpen = (questPopupVar && questPopupVar->GetRealType() == CVAR_Int && questPopupVar->GetGenericRep(CVAR_Int).Int != 0);
				if (!s_key_k_was_down && qOpen)
				{
					FBaseCVar* selIdVar = FindCVar("odoom_quest_selected_id", nullptr);
					if (selIdVar && selIdVar->GetRealType() == CVAR_String)
					{
						const char* id = selIdVar->GetGenericRep(CVAR_String).String;
						if (id && id[0])
						{
							star_api_start_quest(id);
							ODOOM_RefreshQuestCVars();
						}
					}
				}
				s_key_k_was_down = 1;
			}
			else
				s_key_k_was_down = 0;
		}
		/* Quest popup is driven by ZScript only (same as inventory I key): ZScript reads odoom_key_q and toggles; C++ does not set odoom_quest_popup_open. */
	}

	/* Quest: invalidate when popup opens and refresh once to trigger single API request; then refresh every 60 frames only while popup is open so API is hit once. */
	if (g_star_initialized) {
		FBaseCVar* questPopupVar = FindCVar("odoom_quest_popup_open", nullptr);
		int questPopupOpen = (questPopupVar && questPopupVar->GetRealType() == CVAR_Int) ? questPopupVar->GetGenericRep(CVAR_Int).Int : 0;
		static int s_quest_popup_was_open = 0;
		static int s_quest_refresh_frames = 0;
		if (questPopupOpen && !s_quest_popup_was_open) {
#ifdef ODOOM_STAR_API_HAS_REFRESH_QUEST_BACKGROUND
			star_api_refresh_quest_cache_in_background();
#else
			/* Without new API: do not invalidate – show existing cache and let 60-frame refresh update when fetch completes. */
#endif
			ODOOM_RefreshQuestCVars();  /* push current cache to CVars so list shows immediately */
			s_quest_refresh_frames = 0; /* do not run 60-frame refresh this frame */
		}
		s_quest_popup_was_open = questPopupOpen;
		/* Only poll cache every 60 frames while popup is open (no extra API call). */
		if (questPopupOpen) {
			if (++s_quest_refresh_frames >= 60) {
				s_quest_refresh_frames = 0;
				ODOOM_RefreshQuestCVars();
			}
			/* Refresh detail popup lists (prereqs, objectives, subquests) when 2nd popup is open (ZScript sets odoom_quest_detail_quest_id). */
			ODOOM_RefreshQuestDetailCVars();
		} else {
			/* Tracker HUD: when popup is closed, set tracker from API if we have active quest (e.g. after restore session) or replace "..." placeholder when profile loads. */
			/* Deferred "Loading..." for autobeam-in: set placeholder/title here so we never touch CVars during init. */
			if (g_odoom_pending_loading_tracker && g_star_initialized) {
				static const char s_loading_placeholder[] = "...";
				FBaseCVar* tidVar = FindCVar("odoom_quest_tracker_quest_id", nullptr);
				FBaseCVar* titleVar = FindCVar("odoom_quest_tracker_title", nullptr);
				if (tidVar && tidVar->GetRealType() == CVAR_String && titleVar && titleVar->GetRealType() == CVAR_String) {
					UCVarValue u; u.String = (char*)s_loading_placeholder;
					tidVar->SetGenericRep(u, CVAR_String);
					UCVarValue t; t.String = (char*)"Loading...";
					titleVar->SetGenericRep(t, CVAR_String);
				}
				g_odoom_pending_loading_tracker = false;
			}
			FBaseCVar* trackerIdVar = FindCVar("odoom_quest_tracker_quest_id", nullptr);
			const char* trackerId = (trackerIdVar && trackerIdVar->GetRealType() == CVAR_String) ? trackerIdVar->GetGenericRep(CVAR_String).String : nullptr;
			bool trackerIsPlaceholder = (trackerId && std::strcmp(trackerId, "...") == 0);
			if (!trackerId || !trackerId[0] || trackerIsPlaceholder) {
				char qid[64] = {};
				char oid[64] = {};
				if (star_api_get_active_quest_id(qid, sizeof(qid)) && qid[0]) {
					if (trackerIdVar && trackerIdVar->GetRealType() == CVAR_String) {
						UCVarValue u; u.String = qid;
						trackerIdVar->SetGenericRep(u, CVAR_String);
					}
					/* Show "Loading..." until RefreshQuestCVars fills real title (like Quake). */
					FBaseCVar* titleVar = FindCVar("odoom_quest_tracker_title", nullptr);
					if (titleVar && titleVar->GetRealType() == CVAR_String) {
						UCVarValue t; t.String = (char*)"Loading...";
						titleVar->SetGenericRep(t, CVAR_String);
					}
					if (star_api_get_active_objective_id(oid, sizeof(oid)) && oid[0]) {
						FBaseCVar* oidVar = FindCVar("odoom_quest_tracker_active_objective_id", nullptr);
						if (oidVar && oidVar->GetRealType() == CVAR_String) {
							UCVarValue u; u.String = oid;
							oidVar->SetGenericRep(u, CVAR_String);
						}
					}
#ifdef ODOOM_STAR_API_HAS_REFRESH_QUEST_BACKGROUND
					star_api_refresh_quest_cache_in_background();
#endif
					ODOOM_RefreshQuestCVars();
				}
			} else {
				/* Tracker id already set: refresh CVars periodically so tracker shows correct name/objective without opening popup (like Quake). */
				static int s_tracker_refresh_frames = 0;
				if (++s_tracker_refresh_frames >= 60) {
					s_tracker_refresh_frames = 0;
					ODOOM_RefreshQuestCVars();
				}
			}
		}
	}

	/* Send popup: text input buffer (OQuake-style) and execute send when ZScript requests */
	FBaseCVar* sendOpenVar = FindCVar("odoom_send_popup_open", nullptr);
	/* Capture typed name only while send popup is open. */
	bool sendOpen = false;
	if (sendOpenVar && sendOpenVar->GetRealType() == CVAR_Int)
		sendOpen = (sendOpenVar->GetGenericRep(CVAR_Int).Int != 0);
	if (sendOpen && !g_odoom_send_popup_was_open)
	{
		g_odoom_send_input_buffer.clear();
		for (int i = 0; i < 256; i++) g_odoom_send_key_was_down[i] = false;
		/* Suppress the opener key so A/C/Z/X don't get typed into name on popup open. */
		g_odoom_send_key_was_down['A'] = (ODOOM_GetRawKeyDown('A') != 0);
		g_odoom_send_key_was_down['C'] = (ODOOM_GetRawKeyDown('C') != 0);
		g_odoom_send_key_was_down['Z'] = (ODOOM_GetRawKeyDown('Z') != 0);
		g_odoom_send_key_was_down['X'] = (ODOOM_GetRawKeyDown('X') != 0);
		g_odoom_send_popup_was_open = true;
	}
	if (!sendOpen) {
		g_odoom_send_popup_was_open = false;
		g_odoom_send_input_buffer.clear();
	}

	/* Send status for ZScript: "Sending...", "Item sent.", or "Send failed: ...". Clear when popup closed. */
	FBaseCVar* statusVar = FindCVar("odoom_send_status", nullptr);
	if (statusVar && statusVar->GetRealType() == CVAR_String) {
		static char s_send_status_buf[384];
		if (!sendOpen) {
			s_send_status_buf[0] = '\0';
			UCVarValue val; val.String = s_send_status_buf;
			statusVar->SetGenericRep(val, CVAR_String);
		} else {
			/* Run pump again so send callback is processed as soon as the background thread finishes (keeps UI responsive). */
			if (star_sync_send_item_in_progress())
				star_sync_pump();
		}
		if (sendOpen && star_sync_send_item_in_progress()) {
			std::strncpy(s_send_status_buf, "Sending...", sizeof(s_send_status_buf) - 1);
			s_send_status_buf[sizeof(s_send_status_buf) - 1] = '\0';
			UCVarValue val; val.String = s_send_status_buf;
			statusVar->SetGenericRep(val, CVAR_String);
		}
	}

	if (sendOpen)
	{
		int lastChar = 0;  /* one newly pressed char per frame for ZScript (0=none, 8=backspace, else ASCII) */
#ifdef _WIN32
		/* Backspace */
		{
			int vk = VK_BACK;
			int down = ODOOM_GetRawKeyDown(vk);
			if (down && !g_odoom_send_key_was_down[vk & 0xFF])
			{
				lastChar = 8;
				if (!g_odoom_send_input_buffer.empty())
					g_odoom_send_input_buffer.pop_back();
			}
			g_odoom_send_key_was_down[vk & 0xFF] = (down != 0);
		}
		if (lastChar == 0) {
		/* A-Z (lowercase) */
		for (int vk = 'A'; vk <= 'Z'; vk++)
		{
			int down = ODOOM_GetRawKeyDown(vk);
			if (down && !g_odoom_send_key_was_down[vk & 0xFF] && (int)g_odoom_send_input_buffer.size() < ODOOM_SEND_INPUT_MAX)
			{
				lastChar = (GetAsyncKeyState(VK_SHIFT) & 0x8000) ? vk : (vk - 'A' + 'a');
				g_odoom_send_input_buffer += (char)lastChar;
			}
			g_odoom_send_key_was_down[vk & 0xFF] = (down != 0);
		}
		}
		if (lastChar == 0) {
		/* 0-9 */
		for (int vk = '0'; vk <= '9'; vk++)
		{
			int down = ODOOM_GetRawKeyDown(vk);
			if (down && !g_odoom_send_key_was_down[vk & 0xFF] && (int)g_odoom_send_input_buffer.size() < ODOOM_SEND_INPUT_MAX)
			{
				lastChar = vk;
				g_odoom_send_input_buffer += (char)vk;
			}
			g_odoom_send_key_was_down[vk & 0xFF] = (down != 0);
		}
		}
		if (lastChar == 0) {
		/* Space */
		{
			int vk = VK_SPACE;
			int down = ODOOM_GetRawKeyDown(vk);
			if (down && !g_odoom_send_key_was_down[vk & 0xFF] && (int)g_odoom_send_input_buffer.size() < ODOOM_SEND_INPUT_MAX)
			{
				lastChar = ' ';
				g_odoom_send_input_buffer += ' ';
			}
			g_odoom_send_key_was_down[vk & 0xFF] = (down != 0);
		}
		}
		if (lastChar == 0) {
		/* - . */
		{
			int vk_minus = VK_OEM_MINUS;
			int vk_period = VK_OEM_PERIOD;
			int dmin = ODOOM_GetRawKeyDown(vk_minus);
			int dper = ODOOM_GetRawKeyDown(vk_period);
			if (dmin && !g_odoom_send_key_was_down[vk_minus & 0xFF] && (int)g_odoom_send_input_buffer.size() < ODOOM_SEND_INPUT_MAX)
			{
				lastChar = '-';
				g_odoom_send_input_buffer += '-';
			}
			else if (dper && !g_odoom_send_key_was_down[vk_period & 0xFF] && (int)g_odoom_send_input_buffer.size() < ODOOM_SEND_INPUT_MAX)
			{
				lastChar = '.';
				g_odoom_send_input_buffer += '.';
			}
			g_odoom_send_key_was_down[vk_minus & 0xFF] = (dmin != 0);
			g_odoom_send_key_was_down[vk_period & 0xFF] = (dper != 0);
		}
		}
#endif
		/* ZScript reads this and appends to its display string (string CVar may not work in all builds) */
		FBaseCVar* lastCharVar = FindCVar("odoom_send_last_char", nullptr);
		if (lastCharVar && lastCharVar->GetRealType() == CVAR_Int)
		{
			UCVarValue u; u.Int = lastChar;
			lastCharVar->SetGenericRep(u, CVAR_Int);
		}
		/* Also write full line to string CVar for display/send */
		FBaseCVar* lineVar = FindCVar("odoom_send_input_line", nullptr);
		if (lineVar && lineVar->GetRealType() == CVAR_String)
		{
			static char s_send_line_buf[ODOOM_SEND_INPUT_MAX + 1];
			size_t len = g_odoom_send_input_buffer.size();
			if (len > (size_t)ODOOM_SEND_INPUT_MAX) len = (size_t)ODOOM_SEND_INPUT_MAX;
			std::memcpy(s_send_line_buf, g_odoom_send_input_buffer.c_str(), len);
			s_send_line_buf[len] = '\0';
			UCVarValue val;
			val.String = s_send_line_buf;
			lineVar->SetGenericRep(val, CVAR_String);
		}
	}

	/* Execute send when ZScript set odoom_send_do_it=1 */
	FBaseCVar* doItVar = FindCVar("odoom_send_do_it", nullptr);
	if (doItVar && doItVar->GetRealType() == CVAR_Int && doItVar->GetGenericRep(CVAR_Int).Int != 0)
	{
		FBaseCVar* targetVar = FindCVar("odoom_send_target", nullptr);
		FBaseCVar* classVar = FindCVar("odoom_send_item_class", nullptr);
		FBaseCVar* qtyVar = FindCVar("odoom_send_quantity", nullptr);
		FBaseCVar* toClanVar = FindCVar("odoom_send_to_clan", nullptr);
		const char* target = (targetVar && targetVar->GetRealType() == CVAR_String) ? targetVar->GetGenericRep(CVAR_String).String : "";
		const char* itemClass = (classVar && classVar->GetRealType() == CVAR_String) ? classVar->GetGenericRep(CVAR_String).String : "";
		int qty = (qtyVar && qtyVar->GetRealType() == CVAR_Int) ? qtyVar->GetGenericRep(CVAR_Int).Int : 1;
		int toClan = (toClanVar && toClanVar->GetRealType() == CVAR_Int) ? toClanVar->GetGenericRep(CVAR_Int).Int : 0;
		if (qty < 1) qty = 1;
		if (target && target[0] && itemClass && itemClass[0])
		{
			const char* starItemName = (std::strncmp(itemClass, "STAR:", 5) == 0) ? (itemClass + 5) : nullptr;
			if (starItemName && starItemName[0])
			{
				/* STAR item send: use star_sync (background thread + callback via pump, same as Quake). */
				if (StarInitialized())
				{
					if (star_sync_send_item_in_progress())
						Printf("Send already in progress; try again shortly.\n");
					else
					{
						g_odoom_last_sent_item_name = starItemName;
						g_odoom_last_sent_qty = qty;
						star_sync_send_item_start(target, starItemName, qty, toClan ? 1 : 0, nullptr, ODOOM_OnSendItemDone, nullptr);
						/* Show "Sending..." in popup; keep popup open until callback sets result (ZScript shows status). */
						FBaseCVar* sv = FindCVar("odoom_send_status", nullptr);
						if (sv && sv->GetRealType() == CVAR_String) {
							static const char sending[] = "Sending...";
							UCVarValue val; val.String = (char*)sending;
							sv->SetGenericRep(val, CVAR_String);
						}
					}
				}
				else
					Printf("STAR API not initialized; cannot send item. Beam in first.\n");
			}
			else
			{
				/* Local (Doom) item send */
				if (toClan)
					Printf("Send to clan: \"%s\" item \"%s\" x%d (local send not yet implemented).\n", target, itemClass, qty);
				else
					Printf("Send to avatar: \"%s\" item \"%s\" x%d (local send not yet implemented).\n", target, itemClass, qty);
			}
		}
		{ UCVarValue u; u.Int = 0; doItVar->SetGenericRep(u, CVAR_Int); }
		/* Do not close send popup here; ZScript keeps it open and shows Sending.../result, then user closes. */
		g_odoom_send_popup_was_open = false;
	}
}

/** Re-apply stored health/armor to the console player. Called from PostTic (once per frame) and PostOneTic (every tic) so engine overwrites don't stick. */
static void ODOOM_ReapplyStoredHealthArmor(void) {
	FLevelLocals* level = primaryLevel;
	player_t* player = level ? level->GetConsolePlayer() : nullptr;
	if (!player || !player->mo || (!g_star_deferred_apply_health && !g_star_deferred_apply_armor)) return;
	if (g_star_deferred_apply_health && g_star_deferred_health_value >= 0) {
		player->health = g_star_deferred_health_value;
		player->mo->health = g_star_deferred_health_value;
	}
	if (g_star_deferred_apply_armor && g_star_deferred_armor_value >= 0) {
		AActor* arm = player->mo->FindInventory(FName("BasicArmor"), true);
		if (arm)
			arm->IntVar(FName("Amount")) = g_star_deferred_armor_value;
	}
}

/** Called after every game tic (inside TryRunTics loop). Re-applies stored health/armor so whatever overwrites it during the tic is corrected before the next tic. */
void ODOOM_PostOneTic(void) {
	if (g_star_deferred_apply_frames <= 0 || g_star_deferred_apply_frames >= 35) return;
	ODOOM_ReapplyStoredHealthArmor();
}

/** Called after TryRunTics so health/armor apply runs after the tic. First frame applies via engine; then we re-apply for ~1 s (PostOneTic does per-tic re-apply). */
void ODOOM_PostTic(void)
{
	if (g_star_deferred_apply_frames <= 0)
		return;
	FLevelLocals* level = primaryLevel;
	player_t* player = level ? level->GetConsolePlayer() : nullptr;
	if (g_star_deferred_apply_frames >= 34 && !g_star_deferred_apply_name.empty()) {
		/* First frame: apply via engine and store target values. (+X) from description when present. */
		const char* desc = g_star_deferred_apply_description.empty() ? nullptr : g_star_deferred_apply_description.c_str();
		ODOOM_ApplyHealthOrArmor(g_star_deferred_apply_name, g_star_deferred_apply_type, desc);
		g_star_deferred_apply_frames = 33; /* 33 more frames; PostOneTic re-applies every tic within each frame */
	} else if (player && player->mo && (g_star_deferred_apply_health || g_star_deferred_apply_armor)) {
		ODOOM_ReapplyStoredHealthArmor();
		g_star_deferred_apply_frames--;
	} else {
		g_star_deferred_apply_frames = 0;
	}
	if (g_star_deferred_apply_frames <= 0) {
		g_star_deferred_apply_name.clear();
		g_star_deferred_apply_type.clear();
		g_star_deferred_apply_description.clear();
		g_star_deferred_health_value = -1;
		g_star_deferred_armor_value = -1;
		g_star_deferred_apply_health = false;
		g_star_deferred_apply_armor = false;
		ODOOM_RefreshOverlayFromClient();
	}
}

/** Called from engine input code when building ticcmd: set key state CVars for ZScript. */
void ODOOM_InventorySetKeyState(int up, int down, int left, int right, int use, int a, int c, int z, int x, int i, int o, int p, int keyS, int keyT, int q, int enter, int pgup, int pgdown, int home, int endkey, int keyB, int keyN, int keyM, int keyK, int backspace)
{
	UCVarValue val;
	FBaseCVar* v;
#define SET_KEY_CVAR(name, vint) do { val.Int = (vint); v = FindCVar(name, nullptr); if (v && v->GetRealType() == CVAR_Int) v->SetGenericRep(val, CVAR_Int); } while(0)
	SET_KEY_CVAR("odoom_key_up", up);
	SET_KEY_CVAR("odoom_key_down", down);
	SET_KEY_CVAR("odoom_key_left", left);
	SET_KEY_CVAR("odoom_key_right", right);
	SET_KEY_CVAR("odoom_key_pgup", pgup);
	SET_KEY_CVAR("odoom_key_pgdown", pgdown);
	SET_KEY_CVAR("odoom_key_home", home);
	SET_KEY_CVAR("odoom_key_end", endkey);
	SET_KEY_CVAR("odoom_key_b", keyB);
	SET_KEY_CVAR("odoom_key_n", keyN);
	SET_KEY_CVAR("odoom_key_m", keyM);
	SET_KEY_CVAR("odoom_key_use", use);
	SET_KEY_CVAR("odoom_key_a", a);
	SET_KEY_CVAR("odoom_key_c", c);
	SET_KEY_CVAR("odoom_key_z", z);
	SET_KEY_CVAR("odoom_key_x", x);
	SET_KEY_CVAR("odoom_key_i", i);
	SET_KEY_CVAR("odoom_key_o", o);
	SET_KEY_CVAR("odoom_key_p", p);
	SET_KEY_CVAR("odoom_key_s", keyS);
	SET_KEY_CVAR("odoom_key_t", keyT);
	SET_KEY_CVAR("odoom_key_q", q);
	SET_KEY_CVAR("odoom_key_enter", enter);
	SET_KEY_CVAR("odoom_key_k", keyK);
	SET_KEY_CVAR("odoom_key_backspace", backspace);
#undef SET_KEY_CVAR
}

int UZDoom_STAR_GetShowAnorakFace(void)
{
	return g_star_show_anorak_face ? 1 : 0;
}

static std::string TrimAscii(const std::string& s) {
	size_t start = 0;
	while (start < s.size() && std::isspace(static_cast<unsigned char>(s[start]))) start++;
	size_t end = s.size();
	while (end > start && std::isspace(static_cast<unsigned char>(s[end - 1]))) end--;
	return s.substr(start, end - start);
}

static bool EqualsNoCase(const std::string& a, const std::string& b) {
	if (a.size() != b.size()) return false;
	for (size_t i = 0; i < a.size(); i++) {
		unsigned char ca = static_cast<unsigned char>(a[i]);
		unsigned char cb = static_cast<unsigned char>(b[i]);
		if (std::tolower(ca) != std::tolower(cb)) return false;
	}
	return true;
}

/** Health/armor amount for (+X) description (same as ODOOM_ApplyHealthOrArmor). Returns 0 if not health/armor. */
static int GetHealthOrArmorAmount(const char* className) {
	if (!className || !className[0]) return 0;
	/* Health */
	if (strstr(className, "Stimpack")) return 10;
	if (strstr(className, "Medikit")) return 25;
	if (strstr(className, "HealthBonus")) return 1;
	if (strstr(className, "SoulSphere") || strstr(className, "Soul")) return 100;
	if (strstr(className, "Megasphere") || (strstr(className, "Mega") && !strstr(className, "Sphere"))) return 200;
	if (strstr(className, "Large") && strstr(className, "Health")) return 50;
	if (strstr(className, "Mega") && strstr(className, "Health")) return 100;
	if (strstr(className, "Health")) return 25;
	/* Armor */
	if (strstr(className, "Blue") || strstr(className, "Mega")) return 200;
	if (strstr(className, "Green") || strstr(className, "Yellow")) return 100;
	if (strstr(className, "Armor")) return 100;
	return 0;
}

/** Hardcoded Doom ammo pickup amounts for demo (from doomammo.zs / Doom Wiki). Returns 0 to use default 1. */
static int GetHardcodedAmmoAmount(const char* className) {
	if (!className || !className[0]) return 0;
	/* Bullets */
	if (strstr(className, "ClipBox") || strstr(className, "BoxOfBullets")) return 50;
	if (strstr(className, "Clip") || strstr(className, "Bullet")) return 10;
	/* Shells */
	if (strstr(className, "ShellBox") || strstr(className, "BoxOfShells")) return 20;
	if (strstr(className, "Shell") && !strstr(className, "Shotgun")) return 4;
	/* Rockets */
	if (strstr(className, "RocketBox") || strstr(className, "BoxOfRockets")) return 5;
	if (strstr(className, "Rocket")) return 1;
	/* Cells */
	if (strstr(className, "CellPack") || strstr(className, "BulkCell")) return 100;
	if (strstr(className, "Cell")) return 20;
	return 0;
}

/** Map Doom class name to short display/API name (game shown in brackets in UI). Same pattern as OQuake. */
static std::string ToStarItemName(const char* className) {
	if (!className || !className[0]) return "Item";
	const char* c = className;
	/* Ammo (check weapons first so "RocketLauncher" is not matched as "Rockets") */
	if (strstr(c, "Clip") || strstr(c, "Bullet")) return "Bullets";
	if (strstr(c, "Shell") && !strstr(c, "Shotgun")) return "Shells";
	if (strstr(c, "Cell")) return "Cells";
	/* Weapons before ammo so RocketLauncher -> Rocket Launcher, not Rockets */
	if (strstr(c, "RocketLauncher")) return "Rocket Launcher";
	if (strstr(c, "Rocket") && !strstr(c, "Launcher")) return "Rockets";
	/* Armor */
	if (strstr(c, "GreenArmor")) return "Green Armor";
	if (strstr(c, "BlueArmor") || strstr(c, "BlueSphere")) return "Blue Armor";
	/* Health */
	if (strstr(c, "Stimpack")) return "Stimpack";
	if (strstr(c, "Medikit")) return "Medikit";
	if (strstr(c, "HealthBonus")) return "Health Bonus";
	if (strstr(c, "SoulSphere")) return "Soul Sphere";
	if (strstr(c, "Megasphere")) return "Mega Sphere";
	/* Powerups */
	if (strstr(c, "InvulnSphere") || strstr(c, "Invulnerability")) return "Invulnerability";
	if (strstr(c, "Berserk")) return "Berserk";
	if (strstr(c, "Backpack")) return "Backpack";
	/* Weapons */
	if (strstr(c, "Fist")) return "Fist";
	if (strstr(c, "Pistol")) return "Pistol";
	if (strstr(c, "Shotgun")) return "Shotgun";
	if (strstr(c, "Chaingun")) return "Chaingun";
	/* RocketLauncher already handled above (before generic Rocket) */
	if (strstr(c, "PlasmaRifle")) return "Plasma Rifle";
	if (strstr(c, "BFG")) return "BFG9000";
	if (strstr(c, "Chainsaw")) return "Chainsaw";
	/* Fallback: title-case style from class name */
	std::string out;
	for (const char* p = className; *p; ++p) {
		unsigned char ch = static_cast<unsigned char>(*p);
		if (std::isalnum(ch)) out.push_back(static_cast<char>(p == className ? std::toupper(ch) : std::tolower(ch)));
		else if (ch == '_' || ch == ' ') out.push_back(' ');
	}
	while (!out.empty() && out.back() == ' ') out.pop_back();
	return out.empty() ? "Item" : out;
}

static bool IsMockAnorakCredentials(const std::string& username, const std::string& password) {
	return EqualsNoCase(TrimAscii(username), "anorak") && TrimAscii(password) == "test!";
}

static const char* StarAuthSourceLabel(void) {
	if (!g_star_override_username.empty() || !g_star_override_password.empty() ||
		!g_star_override_jwt.empty() || !g_star_override_api_key.empty() || !g_star_override_avatar_id.empty()) {
		return "cli";
	}
	return "env";
}

static bool ArgEquals(const char* a, const char* b) {
	if (!a || !b) return false;
#ifdef _WIN32
	return _stricmp(a, b) == 0;
#else
	return strcasecmp(a, b) == 0;
#endif
}

#ifdef _WIN32
static std::string WideToUtf8(const wchar_t* w) {
	if (!w || !w[0]) return std::string();
	int size = WideCharToMultiByte(CP_UTF8, 0, w, -1, nullptr, 0, nullptr, nullptr);
	if (size <= 1) return std::string();
	std::string out;
	out.resize(static_cast<size_t>(size - 1));
	WideCharToMultiByte(CP_UTF8, 0, w, -1, &out[0], size, nullptr, nullptr);
	return out;
}

static bool PromptForStarCredentials(std::string& usernameOut, std::string& passwordOut) {
	CREDUI_INFOW ui = {};
	ui.cbSize = sizeof(ui);
	ui.pszCaptionText = L"OASIS STAR Beam In";
	ui.pszMessageText = L"Enter your STAR username and password";

	wchar_t username[512] = {};
	wchar_t password[512] = {};
	BOOL save = FALSE;
	DWORD rc = CredUIPromptForCredentialsW(
		&ui,
		L"OASIS_STAR_API",
		nullptr,
		0,
		username,
		static_cast<ULONG>(sizeof(username) / sizeof(username[0])),
		password,
		static_cast<ULONG>(sizeof(password) / sizeof(password[0])),
		&save,
		CREDUI_FLAGS_GENERIC_CREDENTIALS |
		CREDUI_FLAGS_ALWAYS_SHOW_UI |
		CREDUI_FLAGS_DO_NOT_PERSIST |
		CREDUI_FLAGS_EXCLUDE_CERTIFICATES
	);

	if (rc != NO_ERROR) return false;

	usernameOut = WideToUtf8(username);
	passwordOut = WideToUtf8(password);
	SecureZeroMemory(password, sizeof(password));
	return !usernameOut.empty() && !passwordOut.empty();
}
#endif

static const char* GetCliOptionValue(const char* opt) {
	if (!Args || !opt) return nullptr;
	for (int i = 1; i < Args->NumArgs() - 1; i++) {
		const char* arg = Args->GetArg(i);
		if (!ArgEquals(arg, opt)) continue;
		const char* val = Args->GetArg(i + 1);
		if (!val || val[0] == '\0') return nullptr;
		return val;
	}
	return nullptr;
}

static void RefreshStarCliOverridesFromExeArgs(void) {
	if (g_star_cli_loaded) return;
	g_star_cli_loaded = true;

	const char* v = nullptr;
	v = GetCliOptionValue("-star_username"); if (!v) v = GetCliOptionValue("-star_user");
	if (v) g_star_override_username = v;
	v = GetCliOptionValue("-star_password"); if (!v) v = GetCliOptionValue("-star_pass");
	if (v) g_star_override_password = v;
	v = GetCliOptionValue("-star_jwt"); if (!v) v = GetCliOptionValue("-star_token");
	if (v) g_star_override_jwt = v;
	v = GetCliOptionValue("-star_api_key");
	if (v) g_star_override_api_key = v;
	v = GetCliOptionValue("-star_avatar_id");
	if (v) g_star_override_avatar_id = v;
}

static void StarLogInfo(const char* fmt, ...) {
	char msg[1024];
	va_list args;
	va_start(args, fmt);
	vsnprintf(msg, sizeof(msg), fmt, args);
	va_end(args);
	std::printf("STAR API: %s\n", msg);
	Printf(PRINT_NONOTIFY, TEXTCOLOR_GREEN "STAR API: %s\n", msg);
	if (g_star_debug_logging) {
		char buf[1100];
		std::snprintf(buf, sizeof(buf), "STAR API: %s", msg);
		star_api_log_to_file(buf);
	}
}

static void StarLogError(const char* fmt, ...) {
	char msg[1024];
	va_list args;
	va_start(args, fmt);
	vsnprintf(msg, sizeof(msg), fmt, args);
	va_end(args);
	std::printf("STAR API ERROR: %s\n", msg);
	Printf(PRINT_NONOTIFY, TEXTCOLOR_RED "STAR API ERROR: %s\n", msg);
	{
		char buf[1100];
		std::snprintf(buf, sizeof(buf), "STAR API ERROR: %s", msg);
		star_api_log_to_file(buf);
	}
}

static void StarLogRuntimeAuthFailureOnce(const char* reason) {
	if (g_star_logged_runtime_auth_failure) return;
	g_star_logged_runtime_auth_failure = true;
	StarLogError("Not authenticated. STAR sync disabled until beam-in succeeds. Reason: %s", reason ? reason : "(unknown)");
}

static bool HasValue(const char* s) {
	return s && s[0] != '\0';
}

static bool StarShouldUseAnorakFace(void) {
	const char* activeName = HasValue(odoom_star_username) ? odoom_star_username : "";
	std::string u = TrimAscii(activeName);
	return oasis_star_beam_face && !g_star_face_suppressed_for_session && (EqualsNoCase(u, "anorak") || EqualsNoCase(u, "dellams"));
}

/** True if current user is dellams or anorak (for add, bossnft, deploynft). */
static bool StarAllowPrivilegedCommands(void) {
	std::string u = HasValue((const char*)odoom_star_username) ? std::string((const char*)odoom_star_username) : "";
	if (u.empty() && !g_star_effective_username.empty()) u = g_star_effective_username;
	u = TrimAscii(u);
	return EqualsNoCase(u, "dellams") || EqualsNoCase(u, "anorak");
}

static void StarApplyBeamFacePreference(void) {
	g_star_show_anorak_face = g_star_initialized && StarShouldUseAnorakFace();
	oasis_star_anorak_face = g_star_show_anorak_face;
}

static bool StarTryInitializeAndAuthenticate(bool verbose) {
	const bool logVerbose = verbose;
	if (g_star_initialized)
		return true;
	/* After explicit beam out, do not auto re-auth on door/touch; only "star beamin" or startup can auth again. */
	if (!logVerbose && g_star_user_beamed_out) {
		return false;
	}
	/* Avoid retrying init every touch/door when host is unreachable - only retry when user explicitly runs beamin. */
	if (!logVerbose && g_star_init_failed_this_session && !g_star_client_ready) {
		return false;
	}
	if (logVerbose)
		g_star_init_failed_this_session = false;

	RefreshStarCliOverridesFromExeArgs();

	const char* env_username = getenv("STAR_USERNAME");
	const char* env_password = getenv("STAR_PASSWORD");
	const char* env_api_key = getenv("STAR_API_KEY");
	const char* env_jwt = getenv("STAR_JWT_TOKEN");
	if (!HasValue(env_jwt)) env_jwt = getenv("STAR_JWT");
	const char* env_avatar_id = getenv("STAR_AVATAR_ID");

	g_star_effective_username =
		!g_star_override_username.empty() ? g_star_override_username :
		(HasValue(env_username) ? env_username : "");
	g_star_effective_password =
		!g_star_override_password.empty() ? g_star_override_password :
		(HasValue(env_password) ? env_password : "");
	g_star_effective_api_key =
		!g_star_override_api_key.empty() ? g_star_override_api_key :
		(!g_star_override_jwt.empty() ? g_star_override_jwt :
		(HasValue(env_api_key) ? env_api_key :
		(HasValue(env_jwt) ? env_jwt : "")));
	g_star_effective_avatar_id =
		!g_star_override_avatar_id.empty() ? g_star_override_avatar_id :
		(HasValue(env_avatar_id) ? env_avatar_id : "");

	const char* configuredBaseUrl = odoom_star_api_url;
	g_star_config.base_url = HasValue(configuredBaseUrl) ? configuredBaseUrl : "https://star-api.oasisplatform.world/api";
	g_star_config.api_key = g_star_effective_api_key.empty() ? nullptr : g_star_effective_api_key.c_str();
	g_star_config.avatar_id = g_star_effective_avatar_id.empty() ? nullptr : g_star_effective_avatar_id.c_str();
	g_star_config.timeout_seconds = 30;
	if (logVerbose) {
		StarLogInfo(
			"Init/auth start (base_url=%s, has_api_key=%s, has_avatar_id=%s, has_username=%s, has_password=%s)",
			g_star_config.base_url ? g_star_config.base_url : "(null)",
			HasValue(g_star_config.api_key) ? "yes" : "no",
			HasValue(g_star_config.avatar_id) ? "yes" : "no",
			!g_star_effective_username.empty() ? "yes" : "no",
			!g_star_effective_password.empty() ? "yes" : "no"
		);
	}

	if (!g_star_client_ready) {
		if (logVerbose) StarLogInfo("Calling star_api_init...");
		star_api_result_t init_result = star_api_init(&g_star_config);
		if (init_result != STAR_API_SUCCESS) {
			g_star_init_failed_this_session = true;
			if (logVerbose) StarLogError("star_api_init failed: %s", star_api_get_last_error());
			return false;
		}
		g_star_client_ready = true;
		g_star_init_failed_this_session = false;
		if (logVerbose) StarLogInfo("star_api_init succeeded (interop DLL/API ready).");
	}
	/* Always (re)apply WEB4 OASIS URL when set so auth/refresh use the correct host (e.g. after reloadconfig or if init ran before oasisstar.json loaded). */
	{
		const char* oasis_url = (const char*)odoom_oasis_api_url;
		if (HasValue(oasis_url)) {
			star_api_set_oasis_base_url(oasis_url);
			if (logVerbose) StarLogInfo("WEB4 OASIS API URL set to: %s (for mint/auth/refresh).", oasis_url);
		}
	}

	const char* username = g_star_effective_username.empty() ? nullptr : g_star_effective_username.c_str();
	const char* password = g_star_effective_password.empty() ? nullptr : g_star_effective_password.c_str();
	if (HasValue(username) && HasValue(password)) {
		if (star_sync_auth_in_progress()) {
			if (logVerbose) StarLogInfo("SSO auth already in progress.");
			return false;
		}
		if (logVerbose) StarLogInfo("Beaming in... starting async SSO authentication.");
		star_sync_auth_start(username, password, ODOOM_OnAuthDone, nullptr);
		g_star_async_auth_pending = true;
		return false; /* Result will be applied in ODOOM_STAR_PollAsyncAuth. */
	}

	// API key + avatar mode: accept credentials and start inventory in background (no blocking call).
	if (HasValue(g_star_config.api_key) && HasValue(g_star_config.avatar_id)) {
		g_star_initialized = true;
		g_star_init_failed_this_session = false;
		g_star_logged_runtime_auth_failure = false;
		g_star_logged_missing_auth_config = false;
		if (!g_star_effective_username.empty())
			odoom_star_username = g_star_effective_username.c_str();
		else if (!g_star_effective_avatar_id.empty())
			odoom_star_username = g_star_effective_avatar_id.c_str();
		else
			odoom_star_username = "Avatar";
		StarApplyBeamFacePreference();
		if (logVerbose) StarLogInfo("Beam-in (API key/avatar).");
		return true;
	}

	// Restore session from oasisstar.json so user stays logged in between sessions.
	if (g_odoom_saved_jwt[0]) {
		star_api_result_t result = star_api_set_saved_session(g_odoom_saved_jwt);
		if (result == STAR_API_SUCCESS) {
			if (g_odoom_saved_refresh_token[0])
				star_api_set_refresh_token(g_odoom_saved_refresh_token);
			result = star_api_restore_session();
			if (result == STAR_API_SUCCESS) {
				g_star_initialized = true;
				g_star_init_failed_this_session = false;
				g_star_logged_runtime_auth_failure = false;
				g_star_logged_missing_auth_config = false;
				if (g_odoom_saved_username[0])
					g_star_effective_username = g_odoom_saved_username;
				odoom_star_username = g_star_effective_username.empty() ? "Avatar" : g_star_effective_username.c_str();
				StarApplyBeamFacePreference();
				star_api_refresh_avatar_profile();
				g_odoom_pending_loading_tracker = true;  /* Frame pump will set "Loading..." when CVars are ready */
				if (logVerbose) StarLogInfo("Restoring saved session for %s.", g_odoom_saved_username[0] ? g_odoom_saved_username : "(avatar)");
				return true;
			}
		}
		if (logVerbose) StarLogError("Saved session invalid: %s", star_api_get_last_error());
	}

	if (logVerbose && !g_star_logged_missing_auth_config) {
		g_star_logged_missing_auth_config = true;
		StarLogError("No authentication configured. Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID.");
	}
	return false;
}

static const char* GetKeycardName(int keynum) {
	switch (keynum) {
		case 1: return "Red Keycard";
		case 2: return "Blue Keycard";
		case 3: return "Yellow Keycard";
		case 4: return "Skull Key";
		default: return nullptr;
	}
}

/** Name variants that the API or other games might store. Used so door check and HUD find keys regardless of name format. */
static const char* const* GetKeycardNameVariants(int keynum, int* outCount) {
	static const char* red[] = { "Red Keycard", "red_keycard", "Red keycard", "Red Key Card", "Red Key" };
	static const char* blue[] = { "Blue Keycard", "blue_keycard", "Blue keycard", "Blue Key Card", "Blue Key" };
	static const char* yellow[] = { "Yellow Keycard", "yellow_keycard", "Yellow keycard", "Yellow Key Card", "Yellow Key" };
	static const char* skull[] = { "Skull Key", "skull_key", "Skull key" };
	switch (keynum) {
		case 1: *outCount = 5; return red;
		case 2: *outCount = 5; return blue;
		case 3: *outCount = 5; return yellow;
		case 4: *outCount = 3; return skull;
		case 129: *outCount = 5; return red;    /* ZDoom extended lock 129 = red keycard */
		case 130: *outCount = 5; return blue;    /* ZDoom extended lock 130 = blue keycard */
		case 131: *outCount = 5; return yellow;  /* ZDoom extended lock 131 = yellow keycard */
		default: *outCount = 0; return nullptr;
	}
}

/** Substrings (lowercase) that must appear in item name for each key. Fallback when variant has_item fails (e.g. API name differs). */
static bool KeyNameContainsKeycard(int keynum, const char* itemName) {
	if (!itemName || !itemName[0]) return false;
	std::string lower;
	for (const char* p = itemName; *p; ++p) {
		char c = *p;
		lower.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(c == '_' ? ' ' : c))));
	}
	auto has = [&lower](const char* sub) {
		std::string s(sub);
		for (auto& c : s) c = static_cast<char>(std::tolower(static_cast<unsigned char>(c)));
		return lower.find(s) != std::string::npos;
	};
	switch (keynum) {
		case 1: case 129: return has("red") && (has("key") || has("keycard"));
		case 2: case 130: return has("blue") && (has("key") || has("keycard"));
		case 3: case 131: return has("yellow") && (has("key") || has("keycard"));
		case 4: return has("skull") && has("key");
		default: return false;
	}
}

/** Returns true if STAR inventory has this key (any name variant). If outName is non-null, set to the *actual* item name from the API list so use_item can find and consume it (C# matches by exact name). */
static bool ODOOM_STAR_HasKeycard(int keynum, const char** outName) {
	const char* engineKeyName = (keynum > 4) ? P_GetKeyNameForLock(keynum) : nullptr;
	star_item_list_t* list = nullptr;
	if (star_api_get_inventory(&list) != STAR_API_SUCCESS || !list || !list->items) {
		/* No list: try variant names for has_item only (outName would be wrong for use_item). */
		if (!outName) {
			int n = 0;
			const char* const* names = GetKeycardNameVariants(keynum, &n);
			if (names && n > 0) {
				for (int i = 0; i < n; i++) {
					if (star_api_has_item(names[i])) return true;
				}
			}
		}
		return false;
	}
	static char matched_name[256];
	matched_name[0] = '\0';
	bool found = false;
	for (size_t i = 0; i < list->count; i++) {
		const star_item_t* it = &list->items[i];
		if (KeyNameContainsKeycard(keynum, it->name)) {
			std::strncpy(matched_name, it->name, sizeof(matched_name) - 1);
			matched_name[sizeof(matched_name) - 1] = '\0';
			found = true;
			break;
		}
		/* Engine key name fallback: only accept if item name contains engine key string AND matches this door's key type (red/blue/yellow), so e.g. "Keycard" never matches blue for a red door. */
		if (engineKeyName && it->name && it->name[0]) {
			std::string lowerItem;
			for (const char* p = it->name; *p; ++p)
				lowerItem.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(*p == '_' ? ' ' : *p))));
			std::string lowerKey(engineKeyName);
			for (auto& c : lowerKey) c = static_cast<char>(std::tolower(static_cast<unsigned char>(c)));
			if (lowerItem.find(lowerKey) != std::string::npos && KeyNameContainsKeycard(keynum, it->name)) {
				std::strncpy(matched_name, it->name, sizeof(matched_name) - 1);
				matched_name[sizeof(matched_name) - 1] = '\0';
				found = true;
				break;
			}
		}
	}
	star_api_free_item_list(list);
	if (found && outName) *outName = matched_name;
	return found;
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
	star_sync_init();
	/* Load STAR options from oasisstar.json if present (parity with OQuake). */
	{
		std::string path;
		if (ODOOM_FindConfigFile("oasisstar.json", path) && ODOOM_LoadJsonConfig(path.c_str())) {
			g_odoom_json_config_path = path;
			g_odoom_reapply_json_frames = 70;  /* Re-apply after ~2s so mint etc. override ini */
			StarLogInfo("Loaded STAR config from: %s", path.c_str());
		}
	}
	/* Always start with default Doom face until explicit beam-in. */
	g_star_show_anorak_face = false;
	oasis_star_anorak_face = false;
	// Safe default bind for ODOOM inventory popup toggle/tab controls.
	// defaultbind does not override user-customized bindings.
	C_DoCommand("defaultbind i +user1");
	C_DoCommand("defaultbind o +user2");
	C_DoCommand("defaultbind p +user3");
	C_DoCommand("defaultbind c odoom_use_health");
	C_DoCommand("defaultbind f odoom_use_armor");

	StarLogInfo("STAR bootstrap: Beaming in...");
	if (StarTryInitializeAndAuthenticate(true)) { /* C# client handles inventory; overlay refreshes from get_inventory when opened. */ }
	Printf(PRINT_NONOTIFY, "STAR: debug=%s auth_source=%s initialized=%s\n",
		g_star_debug_logging ? "on" : "off",
		StarAuthSourceLabel(),
		g_star_initialized ? "yes" : "no");

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
	Printf("  Locked doors: press E on the door to use a keycard (key only used when you press E).\n");
	Printf("\n");
	Printf("  Welcome to ODOOM!\n");
	Printf("\n");
}

void UZDoom_STAR_Cleanup(void) {
	ODOOM_SaveStarConfigToFiles();
	ODOOM_PushInventoryToCVars(nullptr);
	star_sync_cleanup();
	g_star_async_auth_pending = false;
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
	if (kt && special->IsKindOf(kt)) {
		AActor* def = GetDefaultByType(special->GetClass());
		if (!def) return 0;

		int keynum = def->special1;
		if (keynum <= 0 || keynum > 4) return 0;
		StarLogInfo("Pickup detected: Doom key special1=%d.", keynum);
		return keynum;
	}

	// Generic inventory sync path: health, armor, ammo, weapons. Engine CallTouch runs first (gives item to player);
	// when engine didn't consume we destroy and add to STAR. PostTouchSpecial adds to STAR/mint for all (including weapons).
	auto invType = PClass::FindActor(NAME_Inventory);
	if (invType && special->IsKindOf(invType)) {
		const char* cls = special->GetClass()->TypeName.GetChars();
		const char* type = "Item";
		auto weaponType = PClass::FindActor(NAME_Weapon);
		auto ammoType = PClass::FindActor(NAME_Ammo);
		if (weaponType && special->IsKindOf(weaponType)) type = "Weapon";
		else if (ammoType && special->IsKindOf(ammoType)) type = "Ammo";
		else if (cls && (strstr(cls, "Armor") || strstr(cls, "armor"))) type = "Armor";
		else if (cls && (strstr(cls, "Health") || strstr(cls, "health") || strstr(cls, "Medikit") || strstr(cls, "Stimpack"))) type = "Health";

		g_star_pending_item_name = ToStarItemName(cls);
		{
			int hamt = GetHealthOrArmorAmount(cls);
			if (hamt > 0 && (type == std::string("Health") || type == std::string("Armor"))) {
				g_star_pending_item_desc = g_star_pending_item_name + " (+" + std::to_string(hamt) + ")";
				g_star_pending_item_amount = 1;  /* 1 qty like OQUAKE; (+X) in description */
			} else {
				g_star_pending_item_desc = std::string("Picked up ") + (cls ? cls : "Item");
				int amt = GetHardcodedAmmoAmount(cls);
				g_star_pending_item_amount = (amt > 0) ? amt : 1;
			}
		}
		g_star_pending_item_type = type;
		g_star_has_pending_item = true;
		/* Store player stats before touch: we only add to STAR when engine would leave item on floor (did not apply). */
		{
			FLevelLocals* level = primaryLevel;
			player_t* pl = level ? level->GetConsolePlayer() : nullptr;
			if (pl && pl->mo) {
				g_star_pre_touch_health = pl->mo->health;
				AActor* arm = pl->mo->FindInventory(FName("BasicArmor"), true);
				g_star_pre_touch_armor = arm ? arm->IntVar(FName("Amount")) : 0;
			} else {
				g_star_pre_touch_health = -1;
				g_star_pre_touch_armor = -1;
			}
		}
		StarLogInfo("Pickup detected: %s (type=%s, amount=%d).", cls ? cls : "Inventory", type, g_star_pending_item_amount);
		/* Weapons: return STAR_PICKUP_WEAPON so the engine never destroys the actor (CallTouch gives weapon to player); we still run PostTouchSpecial to add/mint in STAR. */
		if (weaponType && special->IsKindOf(weaponType))
			return STAR_PICKUP_WEAPON;
		/* Always return GENERIC_ITEM so engine runs CallTouch; we only add to STAR in PostTouchSpecial when engine didn't consume (e.g. at max). Avoids standing-on-pickup spam and ensures item is destroyed by game logic. use_armor_on_pickup/use_health_on_pickup are respected when at max (add to STAR if allow_pickup_if_max). */
		return STAR_PICKUP_GENERIC_ITEM;
	}

	return 0;
}

void UZDoom_STAR_PostTouchSpecial(int keynum) {
	if (keynum <= 0) return;
	if (!StarTryInitializeAndAuthenticate(false)) {
		StarLogRuntimeAuthFailureOnce(star_api_get_last_error());
		return;
	}

	const char* name = nullptr;
	const char* desc = nullptr;
	const char* itemType = "KeyItem";
	if (keynum == STAR_PICKUP_OQUAKE_GOLD_KEY) {
		name = "Gold Key";
		desc = "Gold key - Opens gold doors in OQuake";
	} else if (keynum == STAR_PICKUP_OQUAKE_SILVER_KEY) {
		name = "Silver Key";
		desc = "Silver key - Opens silver doors in OQuake";
	} else if (keynum >= 1 && keynum <= 4) {
		name = GetKeycardName(keynum);
		desc = GetKeycardDescription(keynum);
	} else if ((keynum == STAR_PICKUP_GENERIC_ITEM || keynum == STAR_PICKUP_WEAPON) && g_star_has_pending_item) {
		name = g_star_pending_item_name.c_str();
		desc = g_star_pending_item_desc.c_str();
		itemType = g_star_pending_item_type.empty() ? "Item" : g_star_pending_item_type.c_str();
	}
	if (!name || !desc) return;

	/* When always_add_items_to_inventory=1, always add. Otherwise only add when engine didn't use it; at max only add if always_allow_pickup_if_max=1. */
	if (keynum == STAR_PICKUP_GENERIC_ITEM && itemType && g_star_pre_touch_health >= 0 && !odoom_star_always_add_items_to_inventory) {
		FLevelLocals* level = primaryLevel;
		player_t* pl = level ? level->GetConsolePlayer() : nullptr;
		if (pl && pl->mo) {
			int config_max_health = 200, config_max_armor = 200;
			int allow_if_max = 1;
			{
				FBaseCVar* v = FindCVar("odoom_star_max_health", nullptr);
				if (v && v->GetRealType() == CVAR_Int) config_max_health = v->GetGenericRep(CVAR_Int).Int;
				if (config_max_health <= 0) config_max_health = 200;
				v = FindCVar("odoom_star_max_armor", nullptr);
				if (v && v->GetRealType() == CVAR_Int) config_max_armor = v->GetGenericRep(CVAR_Int).Int;
				if (config_max_armor <= 0) config_max_armor = 200;
				v = FindCVar("odoom_star_always_allow_pickup_if_max", nullptr);
				if (v && v->GetRealType() == CVAR_Int) allow_if_max = v->GetGenericRep(CVAR_Int).Int ? 1 : 0;
			}
			int cur_health = pl->mo->health;
			AActor* arm = pl->mo->FindInventory(FName("BasicArmor"), true);
			int cur_armor = arm ? arm->IntVar(FName("Amount")) : 0;
			if (strstr(itemType, "Health") || strstr(itemType, "health")) {
				if (cur_health > g_star_pre_touch_health) {
					g_star_has_pending_item = false;
					g_star_pending_item_name.clear();
					g_star_pending_item_desc.clear();
					g_star_pending_item_type.clear();
					g_star_pending_item_amount = 1;
					return; /* Engine used it on player; don't add to STAR. */
				}
				if (g_star_pre_touch_health >= config_max_health && !allow_if_max) {
					g_star_has_pending_item = false;
					g_star_pending_item_name.clear();
					g_star_pending_item_desc.clear();
					g_star_pending_item_type.clear();
					g_star_pending_item_amount = 1;
					return; /* At config max and ifmax=0: don't add to STAR. */
				}
			}
			if (strstr(itemType, "Armor") || strstr(itemType, "armor")) {
				if (cur_armor > g_star_pre_touch_armor) {
					g_star_has_pending_item = false;
					g_star_pending_item_name.clear();
					g_star_pending_item_desc.clear();
					g_star_pending_item_type.clear();
					g_star_pending_item_amount = 1;
					return; /* Engine used it on player; don't add to STAR. */
				}
				if (g_star_pre_touch_armor >= config_max_armor && !allow_if_max) {
					g_star_has_pending_item = false;
					g_star_pending_item_name.clear();
					g_star_pending_item_desc.clear();
					g_star_pending_item_type.clear();
					g_star_pending_item_amount = 1;
					return; /* At config max and ifmax=0: don't add to STAR. */
				}
			}
		}
	}

	/* Debounce generic/weapon pickups so standing on a stimpack (etc.) doesn't spam: only queue same item once per 0.5s. */
	if (keynum == STAR_PICKUP_GENERIC_ITEM || keynum == STAR_PICKUP_WEAPON) {
		std::string key = std::string(name) + "|" + (itemType ? itemType : "Item");
		int now = I_GetTime();
		if (key == g_star_last_generic_key && (now - g_star_last_generic_tic) < g_star_generic_debounce_ticks)
			return;
		g_star_last_generic_key = key;
		g_star_last_generic_tic = now;
	}

	/* C# client does all heavy lifting: queue pickup (mint if enabled, then add_item) or queue add_item only. */
	bool isKey = (keynum >= 1 && keynum <= 4) || keynum == STAR_PICKUP_OQUAKE_GOLD_KEY || keynum == STAR_PICKUP_OQUAKE_SILVER_KEY;
	bool isWeapon = itemType && (strstr(itemType, "Weapon") != nullptr || strstr(itemType, "weapon") != nullptr);
	bool isArmor = itemType && (strstr(itemType, "Armor") != nullptr || strstr(itemType, "armor") != nullptr);
	bool isPowerup = itemType && (strstr(itemType, "owerup") != nullptr || strstr(itemType, "Health") != nullptr);
	bool doMint = (isKey && odoom_star_mint_keys) || (isWeapon && odoom_star_mint_weapons) || (isArmor && odoom_star_mint_armor) || (isPowerup && odoom_star_mint_powerups);
	int qty = 1;
	if ((keynum == STAR_PICKUP_GENERIC_ITEM || keynum == STAR_PICKUP_WEAPON) && g_star_has_pending_item) {
		/* Health/armor: 1 qty with (+X) in desc (like OQUAKE); ammo/weapons use amount or 1 */
		if (isArmor || (itemType && (strstr(itemType, "Health") != nullptr || strstr(itemType, "health") != nullptr)))
			qty = 1;
		else
			qty = (g_star_pending_item_amount > 0) ? g_star_pending_item_amount : 1;
	}
	const char* provider = (const char*)odoom_star_nft_provider;
	if (!provider || !provider[0]) provider = "SolanaOASIS";
	const char* send_to_addr = (const char*)odoom_star_send_to_address_after_minting;
	if (send_to_addr && !send_to_addr[0]) send_to_addr = nullptr;

	g_star_last_pickup_name = name;
	g_star_last_pickup_type = itemType;
	g_star_last_pickup_desc = desc;
	g_star_has_last_pickup = true;
	if (doMint)
		star_api_queue_pickup_with_mint(name, desc, "ODOOM", itemType ? itemType : "KeyItem", 1, provider, send_to_addr, qty);
	else
		star_api_queue_add_item(name, desc, "ODOOM", itemType ? itemType : "KeyItem", nullptr, qty, 1);

	/* Use the same path the engine uses: PrintPickupMessage (status bar message + Printf) and S_Sound (pickup sound). */
	if ((keynum == STAR_PICKUP_GENERIC_ITEM || keynum == STAR_PICKUP_WEAPON) && desc && desc[0]) {
		FString msg(desc);
		PrintPickupMessage(true, msg);
		FLevelLocals* level = primaryLevel;
		if (level) {
			player_t* pl = level->GetConsolePlayer();
			if (pl && pl->mo)
				S_Sound(pl->mo, CHAN_ITEM, (EChanFlags)(CHANF_NOPAUSE | CHANF_MAYBE_LOCAL), "misc/i_pkup", 1.f, 1.f);
		}
	}

	/* Quest objective completion (fire-and-forget; no callback needed). */
	static const char ODOOM_DEFAULT_QUEST_ID[] = "cross_dimensional_keycard_hunt";
	if (keynum >= 1 && keynum <= 3) {
		const char* obj = (keynum == 1) ? "doom_red_keycard" : (keynum == 2) ? "doom_blue_keycard" : "doom_yellow_keycard";
		star_api_complete_quest_objective(ODOOM_DEFAULT_QUEST_ID, obj, "ODOOM");
	} else if (keynum == STAR_PICKUP_OQUAKE_SILVER_KEY) {
		star_api_complete_quest_objective(ODOOM_DEFAULT_QUEST_ID, "quake_silver_key", "ODOOM");
	} else if (keynum == STAR_PICKUP_OQUAKE_GOLD_KEY) {
		star_api_complete_quest_objective(ODOOM_DEFAULT_QUEST_ID, "quake_gold_key", "ODOOM");
	}

	if (keynum == STAR_PICKUP_GENERIC_ITEM || keynum == STAR_PICKUP_WEAPON) {
		g_star_has_pending_item = false;
		g_star_pending_item_name.clear();
		g_star_pending_item_desc.clear();
		g_star_pending_item_type.clear();
		g_star_pending_item_amount = 1;
	}
}

/* Use button (E) in ticcmd_t.buttons. Matches engine BT_USE = 2. */
#define ODOOM_BT_USE 2

int UZDoom_STAR_CheckDoorAccess(struct AActor* owner, int keynum, int remote) {
	(void)remote;
	if (!owner || keynum <= 0) return 0;

	/* Only open and consume when the player is actually pressing E on the door.
	 * The engine can call P_CheckKeys(!quiet) from other paths (e.g. sector re-check), so we require use button. */
	player_t* pl = owner->player;
	if (!pl) {
		/* Fallback: owner may be console player's mo; get console player for single-player. */
		FLevelLocals* level = primaryLevel;
		if (!level) return 0;
		pl = level->GetConsolePlayer();
		if (!pl || pl->mo != owner) return 0;
	}
	if (!(pl->cmd.buttons & ODOOM_BT_USE)) return 0;

	if (!StarTryInitializeAndAuthenticate(false)) {
		StarLogRuntimeAuthFailureOnce(star_api_get_last_error());
		return 0;
	}

	const char* keyname = nullptr;
	if (!ODOOM_STAR_HasKeycard(keynum, &keyname)) {
		return 0;
	}

	/* Consume key matching this door (red door = red keycard only). */
	bool keyMatchesDoor = (keyname && KeyNameContainsKeycard(keynum, keyname));
	if (keyname && keyMatchesDoor) {
		star_sync_use_item_start(keyname, "odoom_door", ODOOM_OnUseItemDone, nullptr);
		/* Minimal logging: one line to file and console when door is opened with key. */
		char buf[256];
		std::snprintf(buf, sizeof(buf), "[ODOOM STAR] door keynum=%d opened with key=\"%s\"", keynum, keyname);
		star_api_log_to_file(buf);
		Printf(PRINT_HIGH, TEXTCOLOR_GREEN "%s\n", buf);
	}
	return 1;
}

/** Read-only check for HUD/status bar: returns true if STAR has this key (so key icon can be drawn). Call when quiet==true in P_CheckKeys. */
int UZDoom_STAR_PlayerHasKey(int keynum) {
	if (keynum <= 0) return 0;
	if (!StarTryInitializeAndAuthenticate(false)) return 0;
	return ODOOM_STAR_HasKeycard(keynum, nullptr) ? 1 : 0;
}

int UZDoom_STAR_AlwaysAllowPickup(void) {
	FBaseCVar* v = FindCVar("odoom_star_always_allow_pickup_if_max", nullptr);
	if (v && v->GetRealType() == CVAR_Int) return (v->GetGenericRep(CVAR_Int).Int != 0) ? 1 : 0;
	return 1;
}

/** Called from a_doors.cpp EV_DoDoor before P_CheckKeys. No-op to avoid log spam (engine calls every tic). */
void ODOOM_STAR_LogEvDoDoorLock(int lock) {
	(void)lock;
}

void ODOOM_STAR_LogLineDoorKeyCheck(int keynum) {
	(void)keynum;
}

void ODOOM_STAR_LogActivateLineUse(int activationType, int special, int locknumber) {
	(void)activationType;
	(void)special;
	(void)locknumber;
}

void ODOOM_STAR_LogDoorLockedRaiseLock(int lock) {
	(void)lock;
}

void UZDoom_STAR_OnBossKilled(const char* boss_name) {
	if (!boss_name || !boss_name[0] || !g_star_initialized) return;
	if (!StarTryInitializeAndAuthenticate(false)) return;
	char nft_id[128] = {};
	char desc[256];
	std::snprintf(desc, sizeof(desc), "Boss defeated in ODOOM: %s", boss_name);
	const char* prov = (const char*)odoom_star_nft_provider;
	star_api_result_t r = star_api_create_monster_nft(boss_name, desc, "ODOOM", "{}", prov && prov[0] ? prov : nullptr, nft_id);
	if (r == STAR_API_SUCCESS && nft_id[0])
		Printf(PRINT_HIGH, "WEB4 OASIS API: Boss NFT created for \"%s\". ID: %s\n", boss_name, nft_id);
	else if (r != STAR_API_SUCCESS) {
		const char* err = star_api_get_last_error();
		Printf(PRINT_HIGH, "WEB4 OASIS API: Boss NFT failed for \"%s\": %s\n", boss_name, err && err[0] ? err : "unknown");
	}
}

static bool ODOOM_StrEqNoCase(const char* a, const char* b) {
	if (!a || !b) return (a == b);
	while (*a && *b) {
		if (std::tolower(static_cast<unsigned char>(*a)) != std::tolower(static_cast<unsigned char>(*b))) return false;
		++a; ++b;
	}
	return (*a == *b);
}

static const ODOOM_MonsterEntry* ODOOM_FindMonsterByEngineName(const char* engine_name) {
	if (!engine_name || !engine_name[0]) return nullptr;
	/* Map common engine names to table entries (UZDoom/GZDoom may use different class names). */
	if (ODOOM_StrEqNoCase(engine_name, "FormerHuman") || ODOOM_StrEqNoCase(engine_name, "FormerHumanTrooper")) engine_name = "ZombieMan";
	if (ODOOM_StrEqNoCase(engine_name, "FormerHumanSergeant")) engine_name = "ShotgunGuy";
	for (int i = 0; ODOOM_MONSTERS[i].engineName; i++)
		if (ODOOM_StrEqNoCase(ODOOM_MONSTERS[i].engineName, engine_name)) return &ODOOM_MONSTERS[i];
	return nullptr;
}
static bool ODOOM_ShouldMintMonster(const char* monster_name) {
	if (!monster_name || !monster_name[0]) return false;
	const ODOOM_MonsterEntry* e = ODOOM_FindMonsterByEngineName(monster_name);
	if (!e) return false;
	auto it = g_odoom_mint_monster_flags.find(e->configKey);
	if (it != g_odoom_mint_monster_flags.end()) return it->second != 0;
	return true;  /* default 1 for known monsters */
}

void UZDoom_STAR_OnMonsterKilled(const char* monster_name) {
	if (!monster_name || !monster_name[0] || !g_star_initialized) return;
	const ODOOM_MonsterEntry* e = ODOOM_FindMonsterByEngineName(monster_name);
	if (!e) {
		Printf(PRINT_HIGH, "ODOOM STAR: unknown monster \"%s\" (no XP/mint)\n", monster_name);
		return;
	}
	if (!StarTryInitializeAndAuthenticate(false)) return;
	int do_mint = ODOOM_ShouldMintMonster(monster_name) ? 1 : 0;
	const char* prov = (const char*)odoom_star_nft_provider;
	if (!prov || !prov[0]) prov = "SolanaOASIS";
	/* All work (XP, mint, add item) runs on C# background thread; never blocks the game. */
	star_api_queue_monster_kill(e->engineName, e->displayName, e->xp, e->isBoss ? 1 : 0, do_mint, prov, "ODOOM");
}

//-----------------------------------------------------------------------------
// STAR console command (star <subcmd> [args...]) for testing the STAR API
//-----------------------------------------------------------------------------
static bool StarInitialized(void) {
	return g_star_initialized;
}

/** Set toast message for ZScript (same as inventory popup "at max" feedback). */
static void ODOOM_SetToastMessage(const char* msg) {
	if (!msg || !msg[0]) return;
	FBaseCVar* toastMsgCv = FindCVar("odoom_star_toast_message", nullptr);
	FBaseCVar* toastFramesCv = FindCVar("odoom_star_toast_frames", nullptr);
	if (toastMsgCv && toastMsgCv->GetRealType() == CVAR_String) {
		UCVarValue val; val.String = (char*)msg;
		toastMsgCv->SetGenericRep(val, CVAR_String);
	}
	if (toastFramesCv && toastFramesCv->GetRealType() == CVAR_Int) {
		UCVarValue v; v.Int = 105; /* ~3 sec at 35 fps, same as popup */
		toastFramesCv->SetGenericRep(v, CVAR_Int);
	}
}

CCMD(odoom_use_health)
{
	if (!g_star_initialized || star_sync_use_item_in_progress()) return;
	std::string name, type;
	if (!ODOOM_FindFirstHealthOrArmorInInventory(true, &name, &type)) {
		Printf("No health item in STAR inventory.\n");
		return;
	}
	const char* blockMsg = nullptr;
	if (ODOOM_WouldUseExceedMax(name, type, &blockMsg)) {
		if (blockMsg) {
			ODOOM_SetToastMessage(blockMsg);
			Printf(PRINT_HIGH, "%s\n", blockMsg);
		}
		return;
	}
	g_star_use_pending_name = name;
	g_star_use_pending_type = type;
	star_sync_use_item_start(name.c_str(), "odoom_use_health", ODOOM_OnUseItemFromInventoryDone, nullptr);
}

CCMD(odoom_use_armor)
{
	if (!g_star_initialized || star_sync_use_item_in_progress()) return;
	std::string name, type;
	if (!ODOOM_FindFirstHealthOrArmorInInventory(false, &name, &type)) {
		Printf("No armor item in STAR inventory.\n");
		return;
	}
	const char* blockMsg = nullptr;
	if (ODOOM_WouldUseExceedMax(name, type, &blockMsg)) {
		if (blockMsg) {
			ODOOM_SetToastMessage(blockMsg);
			Printf(PRINT_HIGH, "%s\n", blockMsg);
		}
		return;
	}
	g_star_use_pending_name = name;
	g_star_use_pending_type = type;
	star_sync_use_item_start(name.c_str(), "odoom_use_armor", ODOOM_OnUseItemFromInventoryDone, nullptr);
}

CCMD(odoom_quest_toggle)
{
	if (!g_star_initialized) return;
	FBaseCVar* popupVar = FindCVar("odoom_quest_popup_open", nullptr);
	if (popupVar && popupVar->GetRealType() == CVAR_Int) {
		int cur = popupVar->GetGenericRep(CVAR_Int).Int;
		UCVarValue val; val.Int = cur ? 0 : 1;
		popupVar->SetGenericRep(val, CVAR_Int);
	}
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
		Printf("  star lastpickup     - Show most recent synced pickup\n");
		Printf("  star has <item>     - Check if you have an item (e.g. Red Keycard)\n");
		Printf("  star add <item> [desc] [type] - Add item (e.g. star add Red Keycard)\n");
		Printf("  star use <item> [context]     - Use item (e.g. star use Red Keycard door)\n");
		Printf("  star quest start <id>        - Start a quest\n");
		Printf("  star quest objective <quest> <obj> - Complete quest objective\n");
		Printf("  star quest complete <id>    - Complete a quest\n");
		Printf("  star bossnft <name> [desc]   - Create boss NFT\n");
		Printf("  star deploynft <nft_id> <game> [loc] - Deploy boss NFT\n");
		Printf("  star pickup ifmax <0|1> - At max: 1=pick up into STAR, 0=original Doom (leave on floor)\n");
		Printf("  star pickup all <0|1> - 1=always add to STAR even when engine uses it, 0=only when at max\n");
		Printf("  star pickup keycard <red|blue|yellow|skull> - Add keycard to STAR inventory (admin only)\n");
		Printf("  star debug on|off|status - Toggle STAR debug logging in console\n");
		Printf("  star face on|off|status - Toggle beamed-in face switch (default on)\n");
		Printf("  star config        - Show current STAR config (URLs, beam face, stack, mint NFT, provider, max_health, max_armor)\n");
		Printf("  star config save   - Write config to oasisstar.json now (also saved on exit)\n");
		Printf("  star stack <armor|weapons|powerups|keys> <0|1> - Stack (1) or unlock (0) per category\n");
		Printf("  star mint <armor|weapons|powerups|keys> <0|1> - Mint NFT when collecting (1=on, 0=off)\n");
		Printf("  star mint monster <name> <0|1> - Mint NFT when killing (e.g. star mint monster odoom_cacodemon 0)\n");
		Printf("  star nftprovider <name> - Default NFT mint provider (e.g. SolanaOASIS)\n");
		Printf("  star seturl <url>       - Set STAR API URL (saved to config)\n");
		Printf("  star setoasisurl <url>  - Set OASIS API URL (saved to config)\n");
		Printf("  star reloadconfig  - Reload from oasisstar.json\n");
		Printf("  star beamin [user pass]|[jwt <token> [avatar]] [-noface] - Beam in/authenticate\n");
		Printf("  star beamout  - Log out / disconnect from STAR\n");
		Printf("\n");
		return;
	}
	const char* sub = argv[1];
	if (strcmp(sub, "pickup") == 0) {
		Printf("\n");
		if (argv.argc() >= 4 && strcmp(argv[2], "ifmax") == 0) {
			int on = (argv[3][0] == '1' && argv[3][1] == '\0') ? 1 : 0;
			odoom_star_always_allow_pickup_if_max = on;
			ODOOM_SaveStarConfigToFiles();
			Printf("Pick up when at max (always_allow_pickup_if_max) set to %s. Config saved.\n", on ? "1" : "0");
			Printf("\n");
			return;
		}
		if (argv.argc() >= 4 && strcmp(argv[2], "all") == 0) {
			int on = (argv[3][0] == '1' && argv[3][1] == '\0') ? 1 : 0;
			odoom_star_always_add_items_to_inventory = on;
			ODOOM_SaveStarConfigToFiles();
			Printf("Always add to STAR (always_add_items_to_inventory) set to %s. Config saved.\n", on ? "1" : "0");
			Printf("\n");
			return;
		}
		if (argv.argc() < 4 || strcmp(argv[2], "keycard") != 0) {
			Printf("Usage: star pickup ifmax <0|1> - At max: 1=still pick up into STAR, 0=original Doom (leave on floor)\n");
			Printf("       star pickup all <0|1> - 1=always add to STAR even when engine uses it, 0=only when at max\n");
			Printf("       star pickup keycard <red|blue|yellow|skull> - Add keycard to STAR inventory (admin only)\n");
			Printf("\n");
			return;
		}
		if (!StarAllowPrivilegedCommands()) { Printf("Only dellams or anorak can use star pickup keycard.\n"); Printf("\n"); return; }
		const char* color = argv[3];
		const char* name = nullptr;
		const char* desc = nullptr;
		if (strcmp(color, "red") == 0)    { name = "Red Keycard";  desc = "Red Keycard - Opens red doors"; }
		else if (strcmp(color, "blue") == 0)   { name = "Blue Keycard";  desc = "Blue Keycard - Opens blue doors"; }
		else if (strcmp(color, "yellow") == 0) { name = "Yellow Keycard"; desc = "Yellow Keycard - Opens yellow doors"; }
		else if (strcmp(color, "skull") == 0)  { name = "Skull Key";      desc = "Skull Key - Opens skull-marked doors"; }
		else { Printf("Unknown keycard: %s. Use red|blue|yellow|skull.\n", color); Printf("\n"); return; }
		star_api_queue_add_item(name, desc, "ODOOM", "KeyItem", nullptr, 1, 1);
		star_api_result_t r = star_api_flush_add_item_jobs();
		if (r == STAR_API_SUCCESS) Printf("Added %s to STAR inventory.\n", name);
		else Printf("Failed: %s\n", star_api_get_last_error());
		Printf("\n");
		return;
	}
	if (strcmp(sub, "version") == 0) {
		Printf("\n");
		Printf(TEXTCOLOR_GREEN "STAR API integration 1.0 (ODOOM)\n");
		Printf("  Initialized: %s\n", StarInitialized() ? "yes" : "no");
		Printf("  Auth source: %s\n", StarAuthSourceLabel());
		if (!StarInitialized()) Printf("  Last error: %s\n", star_api_get_last_error());
		Printf("\n");
		return;
	}
	if (strcmp(sub, "status") == 0) {
		Printf("\n");
		Printf("STAR API initialized: %s\n", StarInitialized() ? "yes" : "no");
		Printf("STAR API client ready: %s\n", g_star_client_ready ? "yes" : "no");
		Printf("STAR debug logging: %s\n", g_star_debug_logging ? "on" : "off");
		Printf("STAR auth source: %s\n", StarAuthSourceLabel());
		Printf("Last error: %s\n", star_api_get_last_error());
		Printf("\n");
		return;
	}
	if (strcmp(sub, "debug") == 0) {
		Printf("\n");
		if (argv.argc() < 3 || strcmp(argv[2], "status") == 0) {
			Printf("STAR debug logging is %s\n", g_star_debug_logging ? "on" : "off");
			Printf("Usage: star debug on|off|status\n");
			Printf("\n");
			return;
		}
		if (strcmp(argv[2], "on") == 0) {
			g_star_debug_logging = true;
			star_api_set_debug(1);
			StarLogInfo("Debug logging enabled.");
			Printf("\n");
			return;
		}
		if (strcmp(argv[2], "off") == 0) {
			g_star_debug_logging = false;
			star_api_set_debug(0);
			Printf("STAR API: Debug logging disabled.\n");
			Printf("\n");
			return;
		}
		Printf("Unknown debug option: %s. Use on|off|status.\n", argv[2]);
		Printf("\n");
		return;
	}
	if (strcmp(sub, "face") == 0) {
		Printf("\n");
		if (argv.argc() < 3 || strcmp(argv[2], "status") == 0) {
			Printf("Beam-in face switch is %s\n", oasis_star_beam_face ? "on" : "off");
			Printf("Usage: star face on|off|status\n");
			Printf("\n");
			return;
		}
		if (strcmp(argv[2], "on") == 0) {
			oasis_star_beam_face = true;
			g_star_face_suppressed_for_session = false;
			g_star_show_anorak_face = (g_star_initialized && StarShouldUseAnorakFace());
			oasis_star_anorak_face = g_star_show_anorak_face;
			ODOOM_SaveStarConfigToFiles();
			Printf("Beam-in face switch enabled.\n");
			Printf("\n");
			return;
		}
		if (strcmp(argv[2], "off") == 0) {
			oasis_star_beam_face = false;
			g_star_show_anorak_face = false;
			oasis_star_anorak_face = false;
			ODOOM_SaveStarConfigToFiles();
			Printf("Beam-in face switch disabled.\n");
			Printf("\n");
			return;
		}
		Printf("Unknown face option: %s. Use on|off|status.\n", argv[2]);
		Printf("\n");
		return;
	}
	if (strcmp(sub, "inventory") == 0) {
		Printf("\n");
		if (!StarInitialized()) { Printf("STAR API not initialized. %s\n", star_api_get_last_error()); Printf("\n"); return; }
		star_sync_pump();
		if (star_sync_inventory_in_progress()) {
			Printf("Syncing... (run 'star inventory' again in a moment)\n");
			Printf("\n");
			return;
		}
		star_item_list_t* list = nullptr;
		if (star_api_get_inventory(&list) == STAR_API_SUCCESS && list) {
			size_t count = list->count;
			if (count == 0) { Printf("Inventory is empty.\n"); star_api_free_item_list(list); Printf("\n"); return; }
			Printf("STAR inventory (%zu items):\n", count);
			for (size_t i = 0; i < count; i++) {
				int qty = (list->items[i].quantity > 0) ? list->items[i].quantity : 1;
				Printf("  %s - %s (type=%s, game=%s, qty=%d)\n", list->items[i].name, list->items[i].description, list->items[i].item_type, list->items[i].game_source, qty);
			}
			star_api_free_item_list(list);
			Printf("\n");
			return;
		}
		Printf("No inventory loaded. Beam in first.\n");
		Printf("\n");
		return;
	}
	if (strcmp(sub, "lastpickup") == 0) {
		Printf("\n");
		if (!g_star_has_last_pickup) {
			Printf("No pickup has been synced to STAR yet in this session.\n");
			Printf("\n");
			return;
		}
		Printf("Last STAR-synced pickup:\n");
		Printf("  name: %s\n", g_star_last_pickup_name.c_str());
		Printf("  type: %s\n", g_star_last_pickup_type.c_str());
		Printf("  desc: %s\n", g_star_last_pickup_desc.c_str());
		Printf("\n");
		return;
	}
	if (strcmp(sub, "has") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star has <item_name>\n"); return; }
		bool has = star_api_has_item(argv[2]);
		Printf("Has '%s': %s\n", argv[2], has ? "yes" : "no");
		return;
	}
	if (strcmp(sub, "add") == 0) {
		if (!StarAllowPrivilegedCommands()) { Printf("Only dellams or anorak can use star add.\n"); return; }
		if (argv.argc() < 3) { Printf("Usage: star add <item_name> [description] [item_type]\n"); return; }
		const char* name = argv[2];
		const char* desc = argv.argc() > 3 ? argv[3] : "Added from console";
		const char* type = argv.argc() > 4 ? argv[4] : "Miscellaneous";
		star_api_queue_add_item(name, desc, "ODOOM", type, nullptr, 1, 1);
		Printf("Queued '%s' for sync.\n", name);
		return;
	}
	if (strcmp(sub, "use") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star use <item_name> [context]\n"); return; }
		const char* ctx = argv.argc() > 3 ? argv[3] : "console";
		star_api_queue_use_item(argv[2], ctx);
		int r = star_api_flush_use_item_jobs();
		bool ok = (r == STAR_API_SUCCESS);
		Printf("Use '%s' (context %s): %s\n", argv[2], ctx, ok ? "ok" : "failed");
		if (!ok) Printf("  %s\n", star_api_get_last_error());
		return;
	}
	if (strcmp(sub, "quest") == 0) {
		if (argv.argc() == 2) {
			/* "star quest" with no subcommand: open the quest popup */
			FBaseCVar* popupVar = FindCVar("odoom_quest_popup_open", nullptr);
			if (popupVar && popupVar->GetRealType() == CVAR_Int) {
				UCVarValue val; val.Int = 1;
				popupVar->SetGenericRep(val, CVAR_Int);
				Printf("Quest popup opened.\n");
			}
			return;
		}
		if (argv.argc() < 3) { Printf("Usage: star quest [start|objective|complete ...]  (no args = open popup)\n"); return; }
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
		if (!StarAllowPrivilegedCommands()) { Printf("Only dellams or anorak can use star bossnft.\n"); return; }
		if (argv.argc() < 3) { Printf("Usage: star bossnft <boss_name> [description]\n"); return; }
		const char* name = argv[2];
		const char* desc = argv.argc() > 3 ? argv[3] : "Boss from UZDoom";
		char nft_id[64] = {};
		const char* prov = (const char*)odoom_star_nft_provider;
		star_api_result_t r = star_api_create_monster_nft(name, desc, "ODOOM", "{}", prov && prov[0] ? prov : nullptr, nft_id);
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
		Printf("\n");
		g_star_user_beamed_out = false;  /* User explicitly beaming in; allow auth. */
		bool hasRuntimeCredentials = false;
		bool usingJwt = false;
		bool noFaceThisLogin = false;
		if (argv.argc() == 2) {
#ifdef _WIN32
			std::string promptUser;
			std::string promptPass;
			if (PromptForStarCredentials(promptUser, promptPass)) {
				// Explicitly prefer secure popup credentials for this beam-in attempt.
				g_star_override_jwt.clear();
				g_star_override_api_key.clear();
				g_star_override_username = promptUser;
				g_star_override_password = promptPass;
				hasRuntimeCredentials = true;
				StarLogInfo("Using credentials from secure beam-in dialog.");
			} else {
				Printf("Beam-in cancelled.\n");
				Printf("\n");
				return;
			}
#else
			Printf("Provide credentials to beam in:\n");
			Printf("  star beamin <username> <password> [-noface]\n");
			Printf("  star beamin jwt <token> [avatar_id] [-noface]\n");
			Printf("\n");
			return;
#endif
		}
		// Optional runtime overrides:
		// - star beamin <username> <password>
		// - star beamin jwt <token> [avatar_id]
		if (argv.argc() >= 4 && strcmp(argv[2], "jwt") != 0) {
			g_star_override_username = argv[2];
			g_star_override_password = argv[3];
			hasRuntimeCredentials = true;
			StarLogInfo("Using runtime credentials from console command.");
		} else if (argv.argc() >= 4 && strcmp(argv[2], "jwt") == 0) {
			g_star_override_jwt = argv[3];
			if (argv.argc() >= 5 && argv[4][0] != '-') g_star_override_avatar_id = argv[4];
			usingJwt = true;
			StarLogInfo("Using runtime JWT token from console command.");
		}
		for (int i = 2; i < argv.argc(); i++) {
			if (ArgEquals(argv[i], "-noface") || ArgEquals(argv[i], "noface")) {
				noFaceThisLogin = true;
				break;
			}
		}
		const bool applyFaceThisLogin = oasis_star_beam_face && !noFaceThisLogin;
		g_star_face_suppressed_for_session = noFaceThisLogin;

		// Temporary local mock path (no STAR API call):
		// username=anorak password=test! enables custom HUD face.
		if (IsMockAnorakCredentials(g_star_override_username, g_star_override_password)) {
			g_star_initialized = true;
			g_star_frames_since_beamin = 0;
			g_star_logged_runtime_auth_failure = false;
			g_star_logged_missing_auth_config = false;
			g_star_effective_username = "anorak";
			odoom_star_username = "anorak";
			StarApplyBeamFacePreference();
			if (!applyFaceThisLogin) {
				g_star_show_anorak_face = false;
				oasis_star_anorak_face = false;
			}
			Printf("Beam-in successful (mock). Welcome, anorak.\n");
			Printf("\n");
			return;
		}

		if (StarInitialized() && !hasRuntimeCredentials && !usingJwt) {
			Printf("You are already beamed in. Use 'star beamout' first if you want to switch account.\n");
			Printf("\n");
			return;
		}

		g_star_show_anorak_face = false;
		oasis_star_anorak_face = false;
		if (hasRuntimeCredentials || usingJwt) {
			// Force a fresh authentication attempt for account switching.
			g_star_initialized = false;
		}
		StarLogInfo("Beaming in...");
		if (StarTryInitializeAndAuthenticate(true)) {
			if (!applyFaceThisLogin) {
				g_star_show_anorak_face = false;
				oasis_star_anorak_face = false;
			}
			Printf("Beam-in successful. Cross-game features enabled.\n");
			Printf("\n");
			return;
		}
		if (g_star_async_auth_pending) {
			Printf("Authenticating... Please wait.\n");
			Printf("\n");
			return;
		}
		g_star_face_suppressed_for_session = false;
		Printf("Beam-in failed: %s\n", star_api_get_last_error());
		Printf("\n");
		return;
	}
	if (strcmp(sub, "beamout") == 0) {
		Printf("\n");
		if (!StarInitialized() && !g_star_client_ready) {
			Printf("You are not beamed in.\n");
			Printf("\n");
			return;
		}
		if (g_star_client_ready) {
			star_api_cleanup();
		}
		g_star_client_ready = false;
		g_star_initialized = false;
		g_star_user_beamed_out = true;  /* Stay logged out until user runs "star beamin" again. */
		g_star_refresh_xp_called_this_session = false;  /* Next beam-in will call refresh once. */
		g_star_init_failed_this_session = false;
		g_star_async_auth_pending = false;
		g_star_face_suppressed_for_session = false;
		g_star_effective_username.clear();
		g_star_effective_password.clear();
		g_star_show_anorak_face = false;
		oasis_star_anorak_face = false;
		odoom_star_username = "";
		Printf("Beam-out successful. Use 'star beamin' to beam in again.\n");
		Printf("\n");
		return;
	}
	if (strcmp(sub, "config") == 0) {
		const char* save_arg = (argv.argc() >= 3) ? argv[2] : nullptr;
		if (save_arg && strcmp(save_arg, "save") == 0) {
			ODOOM_SaveStarConfigToFiles();
			Printf("Config saved to oasisstar.json (if path found). Also saved on exit.\n");
			return;
		}
		const char* star_url = (const char*)odoom_star_api_url;
		const char* oasis_url = (const char*)odoom_oasis_api_url;
		Printf("\n");
		Printf("ODOOM STAR Configuration:\n");
		Printf("  STAR API URL: %s\n", star_url && star_url[0] ? star_url : "(default: https://star-api.oasisplatform.world/api)");
		Printf("  OASIS API URL: %s\n", oasis_url && oasis_url[0] ? oasis_url : "(default: https://api.oasisplatform.world)");
		Printf("  Beam face: %s\n", oasis_star_beam_face ? "on" : "off");
		Printf("  Stack (1) / Unlock (0) - ammo always stacks:\n");
		Printf("    stack_armor:    %s\n", odoom_star_stack_armor ? "1 (stack)" : "0 (unlock)");
		Printf("    stack_weapons:  %s\n", odoom_star_stack_weapons ? "1 (stack)" : "0 (unlock)");
		Printf("    stack_powerups: %s\n", odoom_star_stack_powerups ? "1 (stack)" : "0 (unlock)");
		Printf("    stack_keys:     %s\n", odoom_star_stack_keys ? "1 (stack)" : "0 (unlock)");
		Printf("  Mint NFT when collecting (1=on, 0=off):\n");
		Printf("    mint_weapons:   %s\n", odoom_star_mint_weapons ? "1" : "0");
		Printf("    mint_armor:    %s\n", odoom_star_mint_armor ? "1" : "0");
		Printf("    mint_powerups: %s\n", odoom_star_mint_powerups ? "1" : "0");
		Printf("    mint_keys:     %s\n", odoom_star_mint_keys ? "1" : "0");
		Printf("  Mint NFT when killing monster (1=on, 0=off). Set: star mint monster <name> <0|1>\n");
		for (int i = 0; ODOOM_MONSTERS[i].engineName; i++) {
			const char* ckey = ODOOM_MONSTERS[i].configKey;
			const char* disp = ODOOM_MONSTERS[i].displayName;
			auto it = g_odoom_mint_monster_flags.find(ckey);
			int v = (it != g_odoom_mint_monster_flags.end()) ? it->second : 1;
			Printf("    %s  mint_monster_%s: %s\n", disp, ckey, v ? "1" : "0");
		}
		Printf("  NFT mint provider: %s\n", (const char*)odoom_star_nft_provider && ((const char*)odoom_star_nft_provider)[0] ? (const char*)odoom_star_nft_provider : "SolanaOASIS");
		Printf("  Send to address after minting: %s\n", (const char*)odoom_star_send_to_address_after_minting && ((const char*)odoom_star_send_to_address_after_minting)[0] ? (const char*)odoom_star_send_to_address_after_minting : "(none)");
		Printf("  max_health: %d  (health pickups only go to STAR inventory when below this; at max they are not stashed)\n", (int)odoom_star_max_health);
		Printf("  max_armor:  %d  (armor pickups only go to STAR inventory when below this; at max they are not stashed)\n", (int)odoom_star_max_armor);
		Printf("  always_allow_pickup_if_max: %s  (1=at max still pick up into STAR; 0=original Doom, leave on floor)\n", odoom_star_always_allow_pickup_if_max ? "1" : "0");
		Printf("  always_add_items_to_inventory: %s  (1=always add to STAR even when engine uses it; 0=only when at max)\n", odoom_star_always_add_items_to_inventory ? "1" : "0");
		Printf("  use_health_on_pickup: %s  (0=below max -> inventory only; 1=standard)\n", odoom_star_use_health_on_pickup ? "1" : "0");
		Printf("  use_armor_on_pickup: %s  (0=below max -> inventory only; 1=standard)\n", odoom_star_use_armor_on_pickup ? "1" : "0");
		Printf("  use_powerup_on_pickup: %s  (0=below max -> inventory only; 1=standard)\n", odoom_star_use_powerup_on_pickup ? "1" : "0");
		Printf("\n");
		Printf("To set: star seturl <url>   star setoasisurl <url>\n");
		Printf("        star pickup ifmax <0|1>   star pickup all <0|1>\n");
		Printf("        star stack <armor|weapons|powerups|keys> <0|1>\n");
		Printf("        star mint <armor|weapons|powerups|keys> <0|1>\n");
		Printf("        star mint monster <name> <0|1>  (e.g. star mint monster odoom_cacodemon 0 or (ODOOM) Cacodemon)\n");
		Printf("        star nftprovider <name>  (e.g. SolanaOASIS)\n");
		Printf("        star max_health <number>   star max_armor <number>  (e.g. star max_health 100)\n");
		Printf("To save now: star config save (also saved on exit)\n");
		Printf("\n");
		return;
	}
	if (strcmp(sub, "mint") == 0) {
		/* star mint monster <name> <0|1> - name = config key (odoom_cacodemon), display ((ODOOM) Cacodemon), or engine name; case-insensitive */
		if (argv.argc() >= 5 && strcmp(argv[2], "monster") == 0) {
			const char* name_arg = argv[3];
			const char* val = argv[4];
			int on = (val[0] == '1' && val[1] == '\0') ? 1 : 0;
			const ODOOM_MonsterEntry* chosen = nullptr;
			size_t na = strlen(name_arg);
			for (int i = 0; ODOOM_MONSTERS[i].engineName; i++) {
				const ODOOM_MonsterEntry* ent = &ODOOM_MONSTERS[i];
				/* match config key (e.g. odoom_cacodemon) */
				if (strlen(ent->configKey) == na) {
					int match = 1;
					for (size_t j = 0; j < na; j++)
						if (tolower((unsigned char)name_arg[j]) != (unsigned char)ent->configKey[j]) { match = 0; break; }
					if (match) { chosen = ent; break; }
				}
				/* match display name (ODOOM) Cacodemon - compare case-insensitive */
				if (strlen(ent->displayName) == na) {
					int match = 1;
					for (size_t j = 0; j < na; j++)
						if (tolower((unsigned char)name_arg[j]) != tolower((unsigned char)ent->displayName[j])) { match = 0; break; }
					if (match) { chosen = ent; break; }
				}
				/* match engine name */
				if (strlen(ent->engineName) == na) {
					int match = 1;
					for (size_t j = 0; j < na; j++)
						if (tolower((unsigned char)name_arg[j]) != tolower((unsigned char)ent->engineName[j])) { match = 0; break; }
					if (match) { chosen = ent; break; }
				}
			}
			if (!chosen) {
				Printf("Unknown monster: %s. Use star config to see list (e.g. odoom_cacodemon, (ODOOM) Cacodemon, oquake_ogre).\n", name_arg);
				return;
			}
			g_odoom_mint_monster_flags[chosen->configKey] = on;
			ODOOM_SaveStarConfigToFiles();
			Printf("Mint NFT for %s (mint_monster_%s) set to %s. Config saved.\n", chosen->displayName, chosen->configKey, on ? "on" : "off");
			return;
		}
		if (argv.argc() < 4) {
			Printf("Usage: star mint <armor|weapons|powerups|keys> <0|1>\n");
			Printf("       star mint monster <MonsterName> <0|1>\n");
			Printf("  1 = mint NFT when collecting/killing that category, 0 = off.\n");
			return;
		}
		const char* cat = argv[2];
		const char* val = argv[3];
		int on = (val[0] == '1' && val[1] == '\0') ? 1 : 0;
		if (strcmp(cat, "armor") == 0) { odoom_star_mint_armor = on; }
		else if (strcmp(cat, "weapons") == 0) { odoom_star_mint_weapons = on; }
		else if (strcmp(cat, "powerups") == 0) { odoom_star_mint_powerups = on; }
		else if (strcmp(cat, "keys") == 0) { odoom_star_mint_keys = on; }
		else {
			Printf("Unknown category: %s. Use armor|weapons|powerups|keys or star mint monster <name> <0|1>.\n", cat);
			return;
		}
		ODOOM_SaveStarConfigToFiles();
		Printf("Mint NFT for %s set to %s. Config saved.\n", cat, on ? "on" : "off");
		return;
	}
	if (strcmp(sub, "nftprovider") == 0) {
		if (argv.argc() < 3) {
			Printf("Usage: star nftprovider <provider_name>\n");
			Printf("  Default: SolanaOASIS. Used when minting NFTs for collected items.\n");
			return;
		}
		odoom_star_nft_provider = argv[2];
		ODOOM_SaveStarConfigToFiles();
		Printf("NFT mint provider set to: %s. Config saved.\n", argv[2]);
		return;
	}
	if (strcmp(sub, "max_health") == 0) {
		if (argv.argc() < 3) {
			Printf("Usage: star max_health <number>\n");
			Printf("  Current: %d. Health pickups only go to STAR inventory when your health is below this; at max they are not stashed. Also in oasisstar.json.\n", (int)odoom_star_max_health);
			return;
		}
		int v = atoi(argv[2]);
		if (v <= 0) { Printf("max_health must be positive (e.g. 100, 200).\n"); return; }
		odoom_star_max_health = v;
		ODOOM_SaveStarConfigToFiles();
		Printf("max_health set to %d. Config saved.\n", v);
		return;
	}
	if (strcmp(sub, "max_armor") == 0) {
		if (argv.argc() < 3) {
			Printf("Usage: star max_armor <number>\n");
			Printf("  Current: %d. Armor pickups only go to STAR inventory when your armor is below this; at max they are not stashed. Also in oasisstar.json.\n", (int)odoom_star_max_armor);
			return;
		}
		int v = atoi(argv[2]);
		if (v <= 0) { Printf("max_armor must be positive (e.g. 100, 200).\n"); return; }
		odoom_star_max_armor = v;
		ODOOM_SaveStarConfigToFiles();
		Printf("max_armor set to %d. Config saved.\n", v);
		return;
	}
	if (strcmp(sub, "stack") == 0) {
		if (argv.argc() < 4) {
			Printf("Usage: star stack <armor|weapons|powerups|keys> <0|1>\n");
			Printf("  1 = stack (each pickup adds quantity), 0 = unlock (one per type). Ammo always stacks.\n");
			return;
		}
		const char* cat = argv[2];
		const char* val = argv[3];
		int on = (val[0] == '1' && val[1] == '\0') ? 1 : 0;
		if (strcmp(cat, "armor") == 0) { odoom_star_stack_armor = on; }
		else if (strcmp(cat, "weapons") == 0) { odoom_star_stack_weapons = on; }
		else if (strcmp(cat, "powerups") == 0) { odoom_star_stack_powerups = on; }
		else if (strcmp(cat, "keys") == 0) { odoom_star_stack_keys = on; }
		else {
			Printf("Unknown category: %s. Use armor|weapons|powerups|keys (sigils are OQuake-only).\n", cat);
			return;
		}
		ODOOM_SaveStarConfigToFiles();
		Printf("%s set to %s (%s). Config saved.\n", cat, on ? "1" : "0", on ? "stack" : "unlock");
		return;
	}
	if (strcmp(sub, "seturl") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star seturl <star_api_url>\n"); return; }
		odoom_star_api_url = argv[2];
		ODOOM_SaveStarConfigToFiles();
		Printf("STAR API URL set to: %s. Config saved.\n", argv[2]);
		return;
	}
	if (strcmp(sub, "setoasisurl") == 0) {
		if (argv.argc() < 3) { Printf("Usage: star setoasisurl <oasis_api_url>\n"); return; }
			odoom_oasis_api_url = argv[2];
			if (g_star_client_ready)
				star_api_set_oasis_base_url(argv[2]);
			ODOOM_SaveStarConfigToFiles();
			Printf("OASIS API URL set to: %s. Config saved.\n", argv[2]);
		return;
	}
	if (strcmp(sub, "reloadconfig") == 0) {
		if (!g_odoom_json_config_path.empty() && ODOOM_LoadJsonConfig(g_odoom_json_config_path.c_str())) {
			Printf("Reloaded config from: %s\n", g_odoom_json_config_path.c_str());
			return;
		}
		std::string path;
		if (ODOOM_FindConfigFile("oasisstar.json", path) && ODOOM_LoadJsonConfig(path.c_str())) {
			g_odoom_json_config_path = path;
			Printf("Reloaded config from: %s\n", path.c_str());
			return;
		}
		Printf("Could not find or load oasisstar.json.\n");
		return;
	}
	if (strcmp(sub, "send_avatar") == 0) {
		if (argv.argc() < 4) { Printf("Usage: star send_avatar <username> <item_class>\n"); return; }
		const char* username = argv[2];
		const char* itemClass = argv[3];
		Printf("Send to avatar: \"%s\" item \"%s\" (STAR send API not yet implemented).\n", username, itemClass);
		return;
	}
	if (strcmp(sub, "send_clan") == 0) {
		if (argv.argc() < 4) { Printf("Usage: star send_clan <clan_name> <item_class>\n"); return; }
		const char* clanName = argv[2];
		const char* itemClass = argv[3];
		Printf("Send to clan: \"%s\" item \"%s\" (STAR send API not yet implemented).\n", clanName, itemClass);
		return;
	}
	Printf("Unknown STAR subcommand: %s. Type 'star' for list.\n", sub);
}

CCMD(stam)
{
	// Convenience alias for common typo.
	if (argv.argc() < 2) {
		Printf("Usage: stam beamin|beamout\n");
		return;
	}
	if (strcmp(argv[1], "beamin") == 0) {
		C_DoCommand("star beamin");
		return;
	}
	if (strcmp(argv[1], "beamout") == 0) {
		C_DoCommand("star beamout");
		return;
	}
	Printf("Unknown STAM subcommand: %s. Use beamin|beamout.\n", argv[1]);
}
