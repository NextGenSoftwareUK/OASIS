/**
 * OQuake - OASIS STAR API Integration Implementation
 *
 * Integrates Quake with the OASIS STAR API so keys collected in ODOOM
 * can open doors in OQuake and vice versa.
 *
 * Integration Points:
 * 1. Key pickup -> add to STAR inventory (silver_key, gold_key)
 * 2. Door touch -> check local key first, then cross-game (Doom keycards)
 * 3. In-game console: "star" command (star version, star inventory, star beamin, etc.)
 */

#include "quakedef.h"
#include "oquake_star_integration.h"
#include "oquake_version.h"
#include "star_sync.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <ctype.h>
#include <time.h>
#include <stdarg.h>
#ifdef _WIN32
#include <windows.h>
#else
#include <sys/stat.h>
#endif

#if defined(_MSC_VER) || !defined(__GLIBC__)
/* memmem is GNU-specific; provide fallback for MSVC and non-GNU. */
static inline void* OQ_memmem(const void* hay, size_t haylen, const void* needle, size_t needlelen) {
    const unsigned char* h = (const unsigned char*)hay;
    const unsigned char* n = (const unsigned char*)needle;
    size_t i;
    if (needlelen == 0) return (void*)h;
    if (haylen < needlelen) return NULL;
    for (i = 0; i <= haylen - needlelen; i++)
        if (memcmp(h + i, n, needlelen) == 0) return (void*)(h + i);
    return NULL;
}
#define memmem(bp, blen, s, slen) OQ_memmem(bp, blen, s, slen)
#endif

#ifndef STAR_API_HAS_SEND_ITEM
/* Forward declare send-item API when using an older star_api.h. Link with updated star_api.lib. */
star_api_result_t star_api_send_item_to_avatar(const char* target_username_or_avatar_id, const char* item_name, int quantity, const char* item_id);
star_api_result_t star_api_send_item_to_clan(const char* clan_name_or_target, const char* item_name, int quantity, const char* item_id);
#endif

/* Forward declare callbacks so they can be used before their definitions. */
static void OQ_OnSendItemDone(void* user_data);
static void OQ_PickupLog(const char* fmt, ...);
static void OQ_StarDebugLog(const char* fmt, ...);
static qboolean g_star_debug_logging = false;

/** Case-insensitive substring search. Defined early so MSVC parses call sites without error. */
static int OQ_ContainsNoCase(const char* haystack, const char* needle) {
    size_t i = 0, j = 0;
    size_t hay_len, needle_len;
    if (!haystack || !needle || !needle[0]) return 0;
    hay_len = strlen(haystack);
    needle_len = strlen(needle);
    if (needle_len > hay_len) return 0;
    for (i = 0; i + needle_len <= hay_len; i++) {
        for (j = 0; j < needle_len; j++) {
            unsigned char hc = (unsigned char)haystack[i + j];
            unsigned char nc = (unsigned char)needle[j];
            if (tolower(hc) != tolower(nc))
                break;
        }
        if (j == needle_len)
            return 1;
    }
    return 0;
}

/** Called from sync worker after each star_api_add_item; logs result to console only when star debug is on. */
static void OQ_AddItemLogCb(const char* item_name, int success, const char* error_message, void* user_data) {
    (void)user_data;
    if (!g_star_debug_logging)
        return;
    if (success)
        Con_Printf("OQuake: add_item '%s' succeeded.\n", item_name ? item_name : "(null)");
    else
        Con_Printf("OQuake: add_item '%s' failed: %s\n", item_name ? item_name : "(null)", error_message && error_message[0] ? error_message : "unknown error");
}

static star_api_config_t g_star_config;
static int g_star_initialized = 0;
/** 1 only after user has run "star beamin" and it succeeded (or async auth callback). Used to gate mint/add so we do not mint shells/shotgun etc. at startup before beamin. */
static int g_star_beamed_in = 0;
/** 1 after we have called star_api_refresh_avatar_xp() once for this beam-in; reset on beam out so we only hit the endpoint once per login. */
static int g_star_refresh_xp_called_this_session = 0;
static int g_star_console_registered = 0;
static char g_star_username[64] = {0};
static char g_json_config_path[512] = {0};
/* Frames until we re-apply oasisstar.json (so mint etc. override config.cfg). Set in Init when json path found. */
static int g_oq_reapply_json_frames = -1;
/* Last pickup synced to STAR (for star lastpickup). */
static char g_star_last_pickup_name[256] = {0};
static char g_star_last_pickup_desc[512] = {0};
static char g_star_last_pickup_type[64] = {0};
static qboolean g_star_has_last_pickup = false;

/* key binding helpers from keys.c; key_message, key_console, key_menu are enum constants from keys.h */
extern char *keybindings[MAX_KEYS];
extern qboolean keydown[MAX_KEYS];
extern int Key_StringToKeynum(const char *str);
extern void Key_SetBinding(int keynum, const char *binding);
extern int key_dest;

cvar_t oasis_star_anorak_face = {"oasis_star_anorak_face", "0", 0}; /* Runtime state - not archived */
cvar_t oasis_star_beam_face = {"oasis_star_beam_face", "1", CVAR_ARCHIVE};
cvar_t oquake_star_config_file = {"oquake_star_config_file", "json", CVAR_ARCHIVE}; /* "json" or "cfg" - which config file to use */
cvar_t oquake_star_api_url = {"oquake_star_api_url", "https://oasisweb4.com/api/star", CVAR_ARCHIVE};
cvar_t oquake_oasis_api_url = {"oquake_oasis_api_url", "https://oasisweb4.com", CVAR_ARCHIVE};
cvar_t oquake_star_username = {"oquake_star_username", "", 0};
cvar_t oquake_star_password = {"oquake_star_password", "", 0};
cvar_t oquake_star_api_key = {"oquake_star_api_key", "", 0};
cvar_t oquake_star_avatar_id = {"oquake_star_avatar_id", "", 0};
/* Stack (1) = each pickup adds to quantity; Unlock (0) = one per type. Ammo always stacks. Default 1. */
cvar_t oquake_star_stack_armor = {"oquake_star_stack_armor", "1", CVAR_ARCHIVE};
cvar_t oquake_star_stack_weapons = {"oquake_star_stack_weapons", "1", CVAR_ARCHIVE};
cvar_t oquake_star_stack_powerups = {"oquake_star_stack_powerups", "1", CVAR_ARCHIVE};
cvar_t oquake_star_stack_keys = {"oquake_star_stack_keys", "1", CVAR_ARCHIVE};
cvar_t oquake_star_stack_sigils = {"oquake_star_stack_sigils", "1", CVAR_ARCHIVE};
/* Mint NFT when collecting: 1 = on, 0 = off. Not CVAR_ARCHIVE so oasisstar.json wins over config.cfg. */
cvar_t oquake_star_mint_weapons = {"oquake_star_mint_weapons", "0", 0};
cvar_t oquake_star_mint_armor = {"oquake_star_mint_armor", "0", 0};
cvar_t oquake_star_mint_powerups = {"oquake_star_mint_powerups", "0", 0};
cvar_t oquake_star_mint_keys = {"oquake_star_mint_keys", "0", 0};
cvar_t oquake_star_nft_provider = {"oquake_star_nft_provider", "SolanaOASIS", 0};
cvar_t oquake_star_send_to_address_after_minting = {"oquake_star_send_to_address_after_minting", "", 0};
/* Max health/armor when using items from inventory (from oasisstar.json). Default 100 (Quake standard). */
cvar_t oquake_star_max_health = {"oquake_star_max_health", "100", 0};
cvar_t oquake_star_max_armor = {"oquake_star_max_armor", "100", 0};
/* 1 = allow pickup when at max: take into STAR and remove from floor (like ODOOM). 0 = original Quake: at max cannot pick up, item stays on floor. JSON: "always_allow_pickup_if_max". */
cvar_t oquake_star_always_allow_pickup_if_max = {"oquake_star_always_allow_pickup_if_max", "1", 0};
/* 1 = always add to STAR even when engine uses it (player gets both). 0 = only add when at max (or when always_allow_pickup_if_max and at max). When 1, overrides always_allow_pickup_if_max. JSON: "always_add_items_to_inventory". */
cvar_t oquake_star_always_add_items_to_inventory = {"oquake_star_always_add_items_to_inventory", "0", 0};
/* 0 = below max: send to STAR inventory only (don't let engine apply). 1 = standard: below max let engine use. At max always use always_allow_pickup_if_max. JSON: "use_health_on_pickup". */
cvar_t oquake_star_use_health_on_pickup = {"oquake_star_use_health_on_pickup", "0", 0};
cvar_t oquake_star_use_armor_on_pickup = {"oquake_star_use_armor_on_pickup", "0", 0};
cvar_t oquake_star_use_powerup_on_pickup = {"oquake_star_use_powerup_on_pickup", "0", 0};

enum {
    OQ_TAB_KEYS = 0,
    OQ_TAB_POWERUPS = 1,
    OQ_TAB_WEAPONS = 2,
    OQ_TAB_AMMO = 3,
    OQ_TAB_ARMOR = 4,
    OQ_TAB_ITEMS = 5,
    OQ_TAB_MONSTERS = 6,
    OQ_TAB_COUNT = 7
};

/* Quake monster table: engine name(s), config key, display name, XP, isBoss. Display names from https://quake.fandom.com/wiki/Monster_(Q1). Config key = mint_monster_oquake_* in oasisstar.json. */
typedef struct oquake_monster_entry_s {
    const char* engine_name;  /* primary engine/class name */
    const char* config_key;
    const char* display_name;
    int xp;
    int is_boss;
} oquake_monster_entry_t;

static const oquake_monster_entry_t OQUAKE_MONSTERS[] = {
    { "monster_dog",       "oquake_dog",       "Rottweiler",    15, 0 },
    { "monster_zombie",    "oquake_zombie",    "Zombie",        20, 0 },
    { "monster_fish",      "oquake_fish",      "Rotfish",       30, 0 },
    { "monster_grunt",     "oquake_grunt",     "Grunt",         25, 0 },
    { "monster_army",      "oquake_grunt",     "Grunt",         25, 0 },  /* Quake progs use monster_army for soldier */
    { "monster_ogre",      "oquake_ogre",      "Ogre",          70, 0 },
    { "monster_enforcer",  "oquake_enforcer",  "Enforcer",      60, 0 },
    { "monster_demon",     "oquake_demon",     "Fiend",         40, 0 },
    { "monster_fiend",     "oquake_demon",     "Fiend",         40, 0 },
    { "monster_shambler",  "oquake_shambler",  "Shambler",    200, 1 },
    { "monster_demon1",    "oquake_shambler",  "Shambler",    200, 1 },   /* alternate classname for Shambler */
    { "monster_spawn",     "oquake_spawn",     "Spawn",        100, 0 },
    { "monster_knight",    "oquake_knight",    "Knight",        80, 0 },
    { "monster_wizard",    "oquake_scrag",     "Scrag",         60, 0 },  /* flying creature; classname wizard */
    { "monster_shub",      "oquake_shub",      "Shub-Niggurath", 500, 1 },
    { "shub_niggurath",    "oquake_shub",      "Shub-Niggurath", 500, 1 },
    { NULL, NULL, NULL, 0, 0 }
};

#define OQ_MONSTER_COUNT ((int)(sizeof(OQUAKE_MONSTERS) / sizeof(OQUAKE_MONSTERS[0])) - 1)

/* Per-monster mint flag: 1 = mint NFT when killed. Index = OQUAKE_MONSTERS index. Default 1. */
static int g_oq_mint_monster_flags[32];
#define OQ_MONSTER_FLAGS_MAX ((int)(sizeof(g_oq_mint_monster_flags) / sizeof(g_oq_mint_monster_flags[0])))

#define OQ_MAX_INVENTORY_ITEMS 256
#define OQ_MAX_OVERLAY_ROWS 8
#define OQ_SEND_TARGET_MAX 63
#define OQ_GROUP_LABEL_MAX 96

typedef struct oquake_inventory_entry_s {
    char name[256];
    char description[512];
    char item_type[64];
    char id[64];  /* STAR inventory item Guid (empty for local-only entries) */
    char game_source[64];  /* e.g. ODOOM, OQUAKE - for display (ODOOM)/(OQUAKE) */
    char nft_id[128];  /* when set, show [NFT] prefix in overlay (persists after reload / from API) */
    int quantity;  /* from API (stack size); use for display so reload shows correct total */
} oquake_inventory_entry_t;

static oquake_inventory_entry_t g_inventory_entries[OQ_MAX_INVENTORY_ITEMS];
static int g_inventory_count = 0;

/* All sync, local delta array, and cache merge are done in the C# client. OQuake only calls star_api_queue_add_item on pickup and star_api_get_inventory to load. */
static int g_inventory_active_tab = OQ_TAB_KEYS;
static qboolean g_inventory_open = false;
static double g_inventory_last_refresh = 0.0;
static char g_inventory_status[128] = "STAR inventory unavailable.";
static int g_inventory_selected_row = 0;
static int g_inventory_scroll_row = 0;

static qboolean g_inventory_key_was_down[MAX_KEYS];
static char g_inventory_send_target[OQ_SEND_TARGET_MAX + 1];
static int g_inventory_send_button = 0; /* 0=Send, 1=Cancel */
static int g_inventory_send_quantity = 1;
enum {
    OQ_SEND_POPUP_NONE = 0,
    OQ_SEND_POPUP_AVATAR = 1,
    OQ_SEND_POPUP_CLAN = 2
};
static int g_inventory_send_popup = OQ_SEND_POPUP_NONE;
#if 0
static unsigned int g_inventory_event_seq = 0;
#endif
static qboolean g_inventory_popup_input_captured = false;
static qboolean g_inventory_send_bindings_captured = false;
static char g_inventory_saved_up_bind[128];
static char g_inventory_saved_down_bind[128];
static char g_inventory_saved_left_bind[128];
static char g_inventory_saved_right_bind[128];
static char g_inventory_saved_all_binds[MAX_KEYS][128];
/* Pending use-item from overlay (E key): applied in callback after async use completes so inventory refresh shows correct qty/removal. */
static char g_oq_use_pending_name[256];
static char g_oq_use_pending_type[64];
static char g_oq_use_pending_description[512];
/* When we apply health/armor from overlay (use-item), set these so OnStatsChangedEx does not re-add the same item (sync/refresh would otherwise add +1 again). */
static double g_oq_health_applied_from_overlay_time = 0.0;
static double g_oq_armor_applied_from_overlay_time = 0.0;
/* Toast message at top center (like ODOOM): show when C/F at max or use from overlay at max. Frames ~175 = ~5 sec at 35 fps. */
#define OQ_TOAST_FRAMES_DEFAULT 175
static char g_oq_toast_message[256] = "";
static int g_oq_toast_frames = 0;
/* Quest popup (Q key), same as ODOOM: filter by status, Start (Not Started) or Set tracker (In Progress). */
static qboolean g_quest_popup_open = false;
static qboolean g_quest_key_was_down = false;
#define OQ_QUEST_MAX 32
static int g_quest_filter_not_started = 1;
static int g_quest_filter_in_progress = 1;
static int g_quest_filter_completed = 1;
static int g_quest_selected_index = 0;
static int g_quest_scroll = 0;
static char g_quest_tracker_id[64] = "";
static char g_quest_tracker_name[128] = "";  /* Display name for HUD tracker. */
static char g_quest_status_message[80] = "";  /* Bottom-right status (e.g. "Starting quest..."). */
static int g_quest_status_frames = 0;
/* C = use health, F = use armor (like ODOOM); polled in draw path so they work regardless of config bindings. */
static qboolean g_c_key_was_down = false;
static qboolean g_f_key_was_down = false;
static int star_initialized(void);
static int OQ_ItemMatchesTab(const oquake_inventory_entry_t* item, int tab);
static void OQ_RefreshInventoryCache(void);
static void OQ_RefreshOverlayFromClient(void);
static void OQ_ClampSelection(int filtered_count);
static void OQ_OnUseItemFromOverlayDone(void* user_data);
static void OQ_SetToastMessage(const char* msg);
static void OQ_UseHealth_f(void);
static void OQ_UseArmor_f(void);
static void OQ_ApplyBeamFacePreference(void);

enum {
    OQ_GROUP_MODE_COUNT = 0,
    OQ_GROUP_MODE_SUM = 1
};

/** Returns 1 if mint is on for this item_type, 0 otherwise. Used for async pickup. */
static int OQ_DoMintForItemType(const char* item_type)
{
    if (!item_type || !item_type[0]) return 0;
    if (strstr(item_type, "Key") || !strcmp(item_type, "KeyItem"))
        return (atoi(oquake_star_mint_keys.string) != 0);
    if (strstr(item_type, "Weapon") || !strcmp(item_type, "Weapon"))
        return (atoi(oquake_star_mint_weapons.string) != 0);
    if (strstr(item_type, "Armor") || !strcmp(item_type, "Armor"))
        return (atoi(oquake_star_mint_armor.string) != 0);
    if (strstr(item_type, "Powerup") || strstr(item_type, "Artifact") || !strcmp(item_type, "Powerup"))
        return (atoi(oquake_star_mint_powerups.string) != 0);
    return 0;
}

/* Minimal hooks: C# client does all heavy lifting. Queue pickup-with-mint (mint then add) or queue add_item only. */
static int OQ_AddInventoryUnlockIfMissing(const char* item_name, const char* description, const char* item_type)
{
    if (!item_name || !item_name[0]) return 0;
    const char* provider = oquake_star_nft_provider.string;
    if (!provider || !provider[0]) provider = "SolanaOASIS";
    const char* send_to_addr = oquake_star_send_to_address_after_minting.string;
    if (send_to_addr && !send_to_addr[0]) send_to_addr = NULL;
    int do_mint = OQ_DoMintForItemType(item_type);
    if (do_mint)
        star_api_queue_pickup_with_mint(item_name, description ? description : "", "Quake", item_type ? item_type : "Item", 1, provider, send_to_addr, 1);
    else
        star_api_queue_add_item(item_name, description ? description : "", "Quake", item_type ? item_type : "Item", NULL, 1, 1);
    return 1;
}
static int OQ_AddInventoryEvent(const char* item_prefix, const char* description, const char* item_type)
{
    int delta = 1;
    if (!item_prefix || !item_prefix[0]) return 0;
    if (description && strchr(description, '+')) {
        const char* p = strrchr(description, '+');
        if (p && p[1] && isdigit((unsigned char)p[1]))
            delta = atoi(p + 1);
    }
    if (delta < 1) delta = 1;
    const char* provider = oquake_star_nft_provider.string;
    if (!provider || !provider[0]) provider = "SolanaOASIS";
    const char* send_to_addr = oquake_star_send_to_address_after_minting.string;
    if (send_to_addr && !send_to_addr[0]) send_to_addr = NULL;
    int do_mint = OQ_DoMintForItemType(item_type);
    if (do_mint)
        star_api_queue_pickup_with_mint(item_prefix, description ? description : "", "Quake", item_type ? item_type : "Item", 1, provider, send_to_addr, delta);
    else
        star_api_queue_add_item(item_prefix, description ? description : "", "Quake", item_type ? item_type : "Item", NULL, delta, 1);
    return 1;
}

static qboolean OQ_KeyPressed(int key)
{
    if (key < 0 || key >= MAX_KEYS)
        return false;
    if (keydown[key] && !g_inventory_key_was_down[key]) {
        g_inventory_key_was_down[key] = true;
        return true;
    }
    if (!keydown[key])
        g_inventory_key_was_down[key] = false;
    return false;
}

static int OQ_BuildFilteredIndices(int* out_indices, int max_indices)
{
    int i;
    int count = 0;
    for (i = 0; i < g_inventory_count; i++) {
        if (!OQ_ItemMatchesTab(&g_inventory_entries[i], g_inventory_active_tab))
            continue;
        if (count < max_indices)
            out_indices[count] = i;
        count++;
    }
    return count;
}

/* Return true if cvar is set to stack (1), false for unlock (0). Ammo always stacks. */
static qboolean OQ_StackArmor(void)    { return atoi(oquake_star_stack_armor.string) != 0; }
static qboolean OQ_StackWeapons(void)  { return atoi(oquake_star_stack_weapons.string) != 0; }
static qboolean OQ_StackPowerups(void) { return atoi(oquake_star_stack_powerups.string) != 0; }
static qboolean OQ_StackKeys(void)     { return atoi(oquake_star_stack_keys.string) != 0; }
static qboolean OQ_StackSigils(void)   { return atoi(oquake_star_stack_sigils.string) != 0; }

/** If description contains "(+N)", add " +N" to label BEFORE " (OQUAKE)" or " (ODOOM)" so result is e.g. "Green Armor +100 (OQUAKE)". */
static void OQ_AppendAmountFromDescription(char* label, size_t label_size, const char* description) {
    const char* p;
    char buf[32];
    int n;
    size_t len, pos, rest_len;
    char* tag;
    if (!label || label_size < 4 || !description || !description[0])
        return;
    p = strstr(description, "(+");
    if (!p || p[2] == '\0' || p[2] == ')')
        return;
    for (n = 0, p += 2; *p >= '0' && *p <= '9' && n < 10; p++)
        buf[n++] = *p;
    if (n == 0 || *p != ')')
        return;
    buf[n] = '\0';
    len = strlen(label);
    /* Insert " +N" before " (OQUAKE)" or " (ODOOM)" if already in label; otherwise append at end. */
    tag = strstr(label, " (OQUAKE)");
    if (!tag) tag = strstr(label, " (ODOOM)");
    if (tag) {
        pos = (size_t)(tag - label);
        rest_len = strlen(tag);
        if (len + 2 + (size_t)n + rest_len < label_size) {
            memmove(label + pos + 2 + (size_t)n, label + pos, rest_len + 1);
            label[pos] = ' ';
            label[pos + 1] = '+';
            memcpy(label + pos + 2, buf, (size_t)n);
        }
    } else {
        if (len + 2 + (size_t)n >= label_size)
            return;
        q_snprintf(label + len, label_size - len, " +%s", buf);
    }
}

static void OQ_AppendGameSourceTag(const oquake_inventory_entry_t* item, char* label, size_t label_size)
{
    const char* gs;
    if (!item || !label || label_size < 2)
        return;
    /* Monster items have game in the name (e.g. "Dog (OQUAKE)"); don't append again. Also skip if already present. */
    if (item->item_type[0] != '\0' && strstr(item->item_type, "Monster"))
        return;
    if (strstr(label, " (OQUAKE)") || strstr(label, " (ODOOM)"))
        return;
    gs = item->game_source;
    if (!gs || !gs[0])
        return;
    if (strstr(gs, "Doom") || strstr(gs, "ODOOM") || strstr(gs, "doom"))
        q_strlcat(label, " (ODOOM)", label_size);
    else if (strstr(gs, "Quake") || strstr(gs, "OQUAKE") || strstr(gs, "quake"))
        q_strlcat(label, " (OQUAKE)", label_size);
}

/** 1 if this item type shows a quantity (stackable); 0 = count. */
static int OQ_IsStackableType(const char* name)
{
    return name && (
        !strcmp(name, "Shells") || !strcmp(name, "Nails") || !strcmp(name, "Rockets") || !strcmp(name, "Cells") ||
        !strcmp(name, "Green Armor") || !strcmp(name, "Yellow Armor") || !strcmp(name, "Red Armor") || !strcmp(name, "Health") ||
        !strcmp(name, "Silver Key") || !strcmp(name, "Gold Key"));
}

/** Fill label, mode and value (available qty) for one inventory entry. Value comes from client (API + pending merged in C#). */
static void OQ_GetGroupedDisplayInfo(const oquake_inventory_entry_t* item, char* label, size_t label_size, int* mode, int* out_value)
{
    if (mode) *mode = OQ_GROUP_MODE_COUNT;
    if (out_value) *out_value = 1;
    if (!item) return;
    if (label && label_size > 0) {
        if (item->nft_id[0] != '\0')
            q_snprintf(label, label_size, "[NFT] %s", item->name);
        else
            q_strlcpy(label, item->name, label_size);
        OQ_AppendAmountFromDescription(label, label_size, item->description);
        OQ_AppendGameSourceTag(item, label, label_size);
    }
    if (mode) *mode = OQ_IsStackableType(item->name) ? OQ_GROUP_MODE_SUM : OQ_GROUP_MODE_COUNT;
    if (out_value) *out_value = (item->quantity > 0 ? item->quantity : 1);
    if (out_value && *out_value < 1) *out_value = 1;
}

/* ----- One row per item type; value from client (API + pending merged in C#). ----- */
static int OQ_BuildGroupedRows(
    int* out_rep_indices, char out_labels[][OQ_GROUP_LABEL_MAX], int* out_modes, int* out_values, qboolean* out_pending, int max_rows)
{
    int filtered_indices[OQ_MAX_INVENTORY_ITEMS];
    int filtered_count = OQ_BuildFilteredIndices(filtered_indices, OQ_MAX_INVENTORY_ITEMS);
    int group_count = 0;
    int i;

    for (i = 0; i < filtered_count && group_count < max_rows; i++) {
        int item_idx = filtered_indices[i];
        const oquake_inventory_entry_t* ent = &g_inventory_entries[item_idx];
        int display_qty = ent->quantity > 0 ? ent->quantity : 1;
        if (display_qty < 1) display_qty = 1;

        out_rep_indices[group_count] = item_idx;
        if (ent->nft_id[0] != '\0')
            q_snprintf(out_labels[group_count], OQ_GROUP_LABEL_MAX, "[NFT] %s", ent->name);
        else
            q_strlcpy(out_labels[group_count], ent->name, OQ_GROUP_LABEL_MAX);
        OQ_AppendAmountFromDescription(out_labels[group_count], OQ_GROUP_LABEL_MAX, ent->description);
        OQ_AppendGameSourceTag(ent, out_labels[group_count], OQ_GROUP_LABEL_MAX);
        out_modes[group_count] = OQ_IsStackableType(ent->name) ? OQ_GROUP_MODE_SUM : OQ_GROUP_MODE_COUNT;
        out_values[group_count] = display_qty;
        out_pending[group_count] = false; /* Client manages pending; no per-row indicator in engine */
        group_count++;
    }
    return group_count;
}

static int OQ_GetSelectedGroupInfo(int* out_rep_index, int* out_mode, int* out_value, char* out_label, size_t out_label_size)
{
    int rep_indices[OQ_MAX_INVENTORY_ITEMS];
    char labels[OQ_MAX_INVENTORY_ITEMS][OQ_GROUP_LABEL_MAX];
    int modes[OQ_MAX_INVENTORY_ITEMS];
    int values[OQ_MAX_INVENTORY_ITEMS];
    qboolean pending[OQ_MAX_INVENTORY_ITEMS];
    int grouped_count = OQ_BuildGroupedRows(rep_indices, labels, modes, values, pending, OQ_MAX_INVENTORY_ITEMS);

    OQ_ClampSelection(grouped_count);
    if (grouped_count <= 0)
        return 0;

    if (out_rep_index)
        *out_rep_index = rep_indices[g_inventory_selected_row];
    if (out_mode)
        *out_mode = modes[g_inventory_selected_row];
    if (out_value)
        *out_value = values[g_inventory_selected_row];
    if (out_label && out_label_size > 0)
        q_strlcpy(out_label, labels[g_inventory_selected_row], out_label_size);

    return 1;
}

static void OQ_UpdatePopupInputCapture(void)
{
    /* Intentionally no-op: inventory popup should not override gameplay bindings. */
}

static void OQ_UpdateSendPopupBindingCapture(void)
{
    int k;
    if (g_inventory_send_popup != OQ_SEND_POPUP_NONE) {
        if (!g_inventory_send_bindings_captured) {
            for (k = 0; k < MAX_KEYS; k++) {
                q_strlcpy(
                    g_inventory_saved_all_binds[k],
                    keybindings[k] ? keybindings[k] : "",
                    sizeof(g_inventory_saved_all_binds[k]));
                Key_SetBinding(k, "");
            }
            Key_ClearStates();
            g_inventory_send_bindings_captured = true;
        }
    } else if (g_inventory_send_bindings_captured) {
        for (k = 0; k < MAX_KEYS; k++)
            Key_SetBinding(k, g_inventory_saved_all_binds[k]);
        Key_ClearStates();
        g_inventory_send_bindings_captured = false;
    }
}

static void OQ_ClampSelection(int filtered_count)
{
    int max_scroll;
    if (filtered_count <= 0) {
        g_inventory_selected_row = 0;
        g_inventory_scroll_row = 0;
        return;
    }

    if (g_inventory_selected_row < 0)
        g_inventory_selected_row = 0;
    if (g_inventory_selected_row >= filtered_count)
        g_inventory_selected_row = filtered_count - 1;

    if (g_inventory_scroll_row > g_inventory_selected_row)
        g_inventory_scroll_row = g_inventory_selected_row;
    if (g_inventory_selected_row >= g_inventory_scroll_row + OQ_MAX_OVERLAY_ROWS)
        g_inventory_scroll_row = g_inventory_selected_row - OQ_MAX_OVERLAY_ROWS + 1;

    if (g_inventory_scroll_row < 0)
        g_inventory_scroll_row = 0;

    max_scroll = filtered_count > OQ_MAX_OVERLAY_ROWS ? filtered_count - OQ_MAX_OVERLAY_ROWS : 0;
    if (g_inventory_scroll_row > max_scroll)
        g_inventory_scroll_row = max_scroll;
}

static oquake_inventory_entry_t* OQ_GetSelectedItem(void)
{
    int rep_index = -1;
    if (!OQ_GetSelectedGroupInfo(&rep_index, NULL, NULL, NULL, 0))
        return NULL;
    return &g_inventory_entries[rep_index];
}

static void OQ_OpenSendPopup(int popup_mode)
{
    int mode = OQ_GROUP_MODE_COUNT;
    int value = 1;
    if (!OQ_GetSelectedItem()) {
        q_strlcpy(g_inventory_status, "No item selected.", sizeof(g_inventory_status));
        return;
    }
    OQ_GetSelectedGroupInfo(NULL, &mode, &value, NULL, 0);
    g_inventory_send_popup = popup_mode;
    g_inventory_send_target[0] = 0;
    g_inventory_send_button = 0;
    g_inventory_send_quantity = 1;
    if (mode != OQ_GROUP_MODE_COUNT || value < 1)
        g_inventory_send_quantity = 1;
}

static void OQ_SendSelectedItem(void)
{
    oquake_inventory_entry_t* item = OQ_GetSelectedItem();
    const char* send_kind;
    int mode = OQ_GROUP_MODE_COUNT;
    int available = 1;
    int qty = 1;
    if (!item) {
        q_strlcpy(g_inventory_status, "No item selected.", sizeof(g_inventory_status));
        return;
    }
    if (!g_inventory_send_target[0]) {
        q_strlcpy(g_inventory_status, "Enter a destination first.", sizeof(g_inventory_status));
        return;
    }

    send_kind = (g_inventory_send_popup == OQ_SEND_POPUP_CLAN) ? "clan" : "avatar";
    OQ_GetSelectedGroupInfo(NULL, &mode, &available, NULL, 0);
    if (mode != OQ_GROUP_MODE_COUNT)
        available = 1;
    if (available < 1)
        available = 1;
    qty = g_inventory_send_quantity;
    if (qty < 1)
        qty = 1;
    if (qty > available)
        qty = available;

    {
        const char* item_id = (item->id[0] != '\0') ? (const char*)item->id : NULL;
        if (star_sync_send_item_in_progress()) {
            q_strlcpy(g_inventory_status, "Send already in progress.", sizeof(g_inventory_status));
        } else {
            star_sync_send_item_start(g_inventory_send_target, item->name, qty,
                (g_inventory_send_popup == OQ_SEND_POPUP_CLAN) ? 1 : 0, item_id, OQ_OnSendItemDone, NULL);
            q_strlcpy(g_inventory_status, "Sending...", sizeof(g_inventory_status));
        }
    }
    g_inventory_send_popup = OQ_SEND_POPUP_NONE;
}

/** Parse amount from description e.g. "Health (+25)" or "Green Armor (+100)". Returns -1 if not found. */
static int OQ_ParseAmountFromDescription(const char* desc) {
    const char* p;
    if (!desc || !desc[0]) return -1;
    p = strstr(desc, "(+");
    if (!p || p[2] == '\0') return -1;
    if (!isdigit((unsigned char)p[2])) return -1;
    return atoi(p + 2);
}

/** Returns 1 if using this health/armor item would exceed max (or already at max). Sets toast message. Uses description "(+X)" for amount when present. */
static int OQ_WouldUseExceedMax(const char* name, const char* type, const char* description, const char** toast_msg) {
    int max_h = 100, max_a = 100;
    int cur_h, cur_a;
    int amount_from_desc;
    *toast_msg = NULL;
    if (oquake_star_max_health.string && oquake_star_max_health.string[0] && atoi(oquake_star_max_health.string) > 0)
        max_h = atoi(oquake_star_max_health.string);
    if (oquake_star_max_armor.string && oquake_star_max_armor.string[0] && atoi(oquake_star_max_armor.string) > 0)
        max_a = atoi(oquake_star_max_armor.string);
    cur_h = cl.stats[STAT_HEALTH];
    cur_a = cl.stats[STAT_ARMOR];
    amount_from_desc = OQ_ParseAmountFromDescription(description);
    {
        int is_health = type && (OQ_ContainsNoCase(type, "health") || OQ_ContainsNoCase(type, "powerup"));
        int is_health_item = name && (OQ_ContainsNoCase(name, "Megahealth") || OQ_ContainsNoCase(name, "Health") || OQ_ContainsNoCase(name, "Stimpack"));
        if (is_health && is_health_item) {
            int amount = (amount_from_desc >= 0) ? amount_from_desc : (OQ_ContainsNoCase(name, "Mega") ? 100 : (OQ_ContainsNoCase(name, "Stimpack") ? 10 : 25));
            if (cur_h >= max_h) { *toast_msg = "You cannot use this because you are already at max health."; return 1; }
            if (cur_h + amount > max_h) { *toast_msg = "You cannot use this because you are already at max health."; return 1; }
        }
    }
    {
        int is_armor = type && (OQ_ContainsNoCase(type, "armor") || (name && OQ_ContainsNoCase(name, "Armor")));
        if (is_armor) {
            int amount = (amount_from_desc >= 0) ? amount_from_desc : ((name && (OQ_ContainsNoCase(name, "Red") || OQ_ContainsNoCase(name, "Mega"))) ? 200 : 100);
            if (cur_a >= max_a) { *toast_msg = "You cannot use this because you are already at max armor."; return 1; }
            if (cur_a + amount > max_a) { *toast_msg = "You cannot use this because you are already at max armor."; return 1; }
        }
    }
    return 0;
}

/** Apply health or armor to local player after use-item (cap at max from config). Uses description "(+X)" for amount when present. */
static void OQ_ApplyHealthOrArmor(const char* name, const char* type, const char* description) {
    int max_h = 100, max_a = 100;
    int amount;
    int amount_from_desc = OQ_ParseAmountFromDescription(description);
    if (oquake_star_max_health.string && oquake_star_max_health.string[0] && atoi(oquake_star_max_health.string) > 0)
        max_h = atoi(oquake_star_max_health.string);
    if (oquake_star_max_armor.string && oquake_star_max_armor.string[0] && atoi(oquake_star_max_armor.string) > 0)
        max_a = atoi(oquake_star_max_armor.string);
    {
        int is_health = type && (OQ_ContainsNoCase(type, "health") || OQ_ContainsNoCase(type, "powerup"));
        int is_health_item = name && (OQ_ContainsNoCase(name, "Megahealth") || OQ_ContainsNoCase(name, "Health") || OQ_ContainsNoCase(name, "Stimpack"));
        if (is_health && is_health_item) {
            amount = (amount_from_desc >= 0) ? amount_from_desc : (OQ_ContainsNoCase(name, "Mega") ? 100 : (OQ_ContainsNoCase(name, "Stimpack") ? 10 : 25));
            cl.stats[STAT_HEALTH] += amount;
            if (cl.stats[STAT_HEALTH] > max_h) cl.stats[STAT_HEALTH] = max_h;
            g_oq_health_applied_from_overlay_time = realtime;
        }
    }
    {
        int is_armor = (type && OQ_ContainsNoCase(type, "armor")) || (name && OQ_ContainsNoCase(name, "Armor"));
        if (is_armor) {
            amount = (amount_from_desc >= 0) ? amount_from_desc : ((name && (OQ_ContainsNoCase(name, "Red") || OQ_ContainsNoCase(name, "Mega"))) ? 200 : 100);
            cl.stats[STAT_ARMOR] += amount;
            if (cl.stats[STAT_ARMOR] > max_a) cl.stats[STAT_ARMOR] = max_a;
            g_oq_armor_applied_from_overlay_time = realtime;
        }
    }
}

/** Called from main thread by star_sync_pump() when use-item from overlay (E key) completes. Apply health/armor and refresh so qty/removal is correct (like ODOOM). */
static void OQ_OnUseItemFromOverlayDone(void* user_data) {
    int success = 0;
    char err_buf[384] = {0};
    (void)user_data;
    if (!star_sync_use_item_get_result(&success, err_buf, sizeof(err_buf)))
        return;
    OQ_StarDebugLog("UseItem callback: success=%d name='%s' err='%s'", success, g_oq_use_pending_name, err_buf[0] ? err_buf : "(none)");
    if (success && g_oq_use_pending_name[0] != '\0') {
        OQ_ApplyHealthOrArmor(g_oq_use_pending_name, g_oq_use_pending_type, g_oq_use_pending_description);
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Used item: %s", g_oq_use_pending_name);
        OQ_StarDebugLog("UseItem: applied to player, refreshing overlay");
    } else if (!success && err_buf[0])
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Use failed: %s", err_buf);
    g_oq_use_pending_name[0] = '\0';
    g_oq_use_pending_type[0] = '\0';
    g_oq_use_pending_description[0] = '\0';
    if (success)
        OQ_RefreshOverlayFromClient();
}

static void OQ_UseSelectedItem(void)
{
    const char* toast_msg = NULL;
    oquake_inventory_entry_t* item = OQ_GetSelectedItem();
    if (!item) {
        q_strlcpy(g_inventory_status, "No item selected.", sizeof(g_inventory_status));
        return;
    }
    /* Keys: no effect from inventory (only on doors). Same as ODOOM. */
    if (OQ_ItemMatchesTab(item, OQ_TAB_KEYS)) {
        q_strlcpy(g_inventory_status, "Keys can only be used on doors.", sizeof(g_inventory_status));
        return;
    }
    /* Ammo: no effect from inventory. Same as ODOOM. */
    if (OQ_ItemMatchesTab(item, OQ_TAB_AMMO)) {
        q_strlcpy(g_inventory_status, "Ammo cannot be used from inventory.", sizeof(g_inventory_status));
        return;
    }
    /* Weapons: switch in game (we cannot switch from C; show message). Do not consume. */
    if (OQ_ItemMatchesTab(item, OQ_TAB_WEAPONS)) {
        q_strlcpy(g_inventory_status, "Select this weapon in game (number keys).", sizeof(g_inventory_status));
        return;
    }
    /* Health/Armor: check max first; toast if already at max, else use and apply. Use description "(+X)" for amount. */
    if (OQ_WouldUseExceedMax(item->name, item->item_type, item->description, &toast_msg)) {
        OQ_SetToastMessage(toast_msg ? toast_msg : "You cannot use this because you are already at max health or armor.");
        if (toast_msg)
            q_strlcpy(g_inventory_status, toast_msg, sizeof(g_inventory_status));
        return;
    }
    if (star_sync_use_item_in_progress()) {
        q_strlcpy(g_inventory_status, "Use in progress...", sizeof(g_inventory_status));
        OQ_StarDebugLog("UseItem (E key): blocked (use already in progress)");
        return;
    }
    q_strlcpy(g_oq_use_pending_name, item->name, sizeof(g_oq_use_pending_name));
    q_strlcpy(g_oq_use_pending_type, item->item_type, sizeof(g_oq_use_pending_type));
    q_strlcpy(g_oq_use_pending_description, item->description, sizeof(g_oq_use_pending_description));
    OQ_StarDebugLog("UseItem (E key): starting async name='%s' type='%s' context=inventory_overlay", item->name, item->item_type ? item->item_type : "");
    star_sync_use_item_start(item->name, "inventory_overlay", OQ_OnUseItemFromOverlayDone, NULL);
    q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Using: %s...", item->name);
}

/** First health item in g_inventory_entries (Health, Megahealth, or Stimpack). Returns NULL if none. */
static const oquake_inventory_entry_t* OQ_FindFirstHealthEntry(void) {
    int i;
    for (i = 0; i < g_inventory_count; i++) {
        const oquake_inventory_entry_t* e = &g_inventory_entries[i];
        if (OQ_ItemMatchesTab(e, OQ_TAB_POWERUPS) && e->name && (OQ_ContainsNoCase(e->name, "Megahealth") || OQ_ContainsNoCase(e->name, "Health") || OQ_ContainsNoCase(e->name, "Stimpack")))
            return e;
        if (e->item_type && OQ_ContainsNoCase(e->item_type, "health"))
            return e;
        if (e->name && (OQ_ContainsNoCase(e->name, "Health") || OQ_ContainsNoCase(e->name, "Stimpack")) && !OQ_ItemMatchesTab(e, OQ_TAB_ARMOR))
            return e;
    }
    return NULL;
}

/** First armor item in g_inventory_entries. Returns NULL if none. */
static const oquake_inventory_entry_t* OQ_FindFirstArmorEntry(void) {
    int i;
    for (i = 0; i < g_inventory_count; i++) {
        if (OQ_ItemMatchesTab(&g_inventory_entries[i], OQ_TAB_ARMOR))
            return &g_inventory_entries[i];
    }
    return NULL;
}

/** Set toast message for on-screen display (like ODOOM). Shows at top center for ~3 seconds. */
static void OQ_SetToastMessage(const char* msg) {
    if (!msg || !msg[0]) return;
    q_strlcpy(g_oq_toast_message, msg, sizeof(g_oq_toast_message));
    g_oq_toast_frames = OQ_TOAST_FRAMES_DEFAULT;
}

static void OQ_UseHealth_f(void) {
    const char* toast_msg = NULL;
    OQ_StarDebugLog("UseHealth (C key): invoked");
    if (!g_star_initialized) { Con_Printf("STAR not initialized. Use star beamin.\n"); OQ_StarDebugLog("UseHealth: skip (not initialized)"); return; }
    if (star_sync_use_item_in_progress()) { Con_Printf("Use in progress...\n"); OQ_StarDebugLog("UseHealth: skip (use in progress)"); return; }
    OQ_RefreshOverlayFromClient();
    const oquake_inventory_entry_t* item = OQ_FindFirstHealthEntry();
    if (!item) { Con_Printf("No health item in STAR inventory.\n"); OQ_StarDebugLog("UseHealth: no health item found (g_inventory_count=%d)", g_inventory_count); return; }
    if (OQ_WouldUseExceedMax(item->name, item->item_type, item->description, &toast_msg)) {
        OQ_SetToastMessage(toast_msg ? toast_msg : "You cannot use this because you are already at max health.");
        Con_Printf("%s\n", toast_msg ? toast_msg : "Already at max health.");
        OQ_StarDebugLog("UseHealth: blocked (would exceed max) msg='%s'", toast_msg ? toast_msg : "");
        return;
    }
    q_strlcpy(g_oq_use_pending_name, item->name, sizeof(g_oq_use_pending_name));
    q_strlcpy(g_oq_use_pending_type, item->item_type, sizeof(g_oq_use_pending_type));
    q_strlcpy(g_oq_use_pending_description, item->description, sizeof(g_oq_use_pending_description));
    OQ_StarDebugLog("UseHealth: starting async name='%s' context=oquake_use_health", item->name);
    star_sync_use_item_start(item->name, "oquake_use_health", OQ_OnUseItemFromOverlayDone, NULL);
    Con_Printf("Using: %s...\n", item->name);
}

static void OQ_UseArmor_f(void) {
    const char* toast_msg = NULL;
    OQ_StarDebugLog("UseArmor (F key): invoked");
    if (!g_star_initialized) { Con_Printf("STAR not initialized. Use star beamin.\n"); OQ_StarDebugLog("UseArmor: skip (not initialized)"); return; }
    if (star_sync_use_item_in_progress()) { Con_Printf("Use in progress...\n"); OQ_StarDebugLog("UseArmor: skip (use in progress)"); return; }
    OQ_RefreshOverlayFromClient();
    const oquake_inventory_entry_t* item = OQ_FindFirstArmorEntry();
    if (!item) { Con_Printf("No armor item in STAR inventory.\n"); OQ_StarDebugLog("UseArmor: no armor item found (g_inventory_count=%d)", g_inventory_count); return; }
    if (OQ_WouldUseExceedMax(item->name, item->item_type, item->description, &toast_msg)) {
        OQ_SetToastMessage(toast_msg ? toast_msg : "You cannot use this because you are already at max armor.");
        Con_Printf("%s\n", toast_msg ? toast_msg : "Already at max armor.");
        OQ_StarDebugLog("UseArmor: blocked (would exceed max) msg='%s'", toast_msg ? toast_msg : "");
        return;
    }
    q_strlcpy(g_oq_use_pending_name, item->name, sizeof(g_oq_use_pending_name));
    q_strlcpy(g_oq_use_pending_type, item->item_type, sizeof(g_oq_use_pending_type));
    q_strlcpy(g_oq_use_pending_description, item->description, sizeof(g_oq_use_pending_description));
    OQ_StarDebugLog("UseArmor: starting async name='%s' context=oquake_use_armor", item->name);
    star_sync_use_item_start(item->name, "oquake_use_armor", OQ_OnUseItemFromOverlayDone, NULL);
    Con_Printf("Using: %s...\n", item->name);
}

static void OQ_HandleSendPopupTyping(void)
{
    int len = (int)strlen(g_inventory_send_target);
    int c;
    if (OQ_KeyPressed(K_BACKSPACE) || OQ_KeyPressed(K_DEL)) {
        if (len > 0)
            g_inventory_send_target[len - 1] = 0;
    }

    for (c = 'a'; c <= 'z'; c++) {
        if (OQ_KeyPressed(c) || OQ_KeyPressed(toupper(c))) {
            if (len < OQ_SEND_TARGET_MAX) {
                g_inventory_send_target[len] = (char)c;
                g_inventory_send_target[len + 1] = 0;
                len++;
            }
        }
    }
    for (c = '0'; c <= '9'; c++) {
        if (OQ_KeyPressed(c)) {
            if (len < OQ_SEND_TARGET_MAX) {
                g_inventory_send_target[len] = (char)c;
                g_inventory_send_target[len + 1] = 0;
                len++;
            }
        }
    }
    if (OQ_KeyPressed(' ') && len < OQ_SEND_TARGET_MAX) {
        g_inventory_send_target[len] = ' ';
        g_inventory_send_target[len + 1] = 0;
        len++;
    }
    if ((OQ_KeyPressed('_') || OQ_KeyPressed('-') || OQ_KeyPressed('.')) && len < OQ_SEND_TARGET_MAX) {
        if (keydown['_'])
            g_inventory_send_target[len] = '_';
        else if (keydown['-'])
            g_inventory_send_target[len] = '-';
        else
            g_inventory_send_target[len] = '.';
        g_inventory_send_target[len + 1] = 0;
    }
}

static const char* OQ_TabShortName(int tab) {
    switch (tab) {
        case OQ_TAB_KEYS: return "Keys";
        case OQ_TAB_POWERUPS: return "Power Ups";
        case OQ_TAB_WEAPONS: return "Weapons";
        case OQ_TAB_AMMO: return "Ammo";
        case OQ_TAB_ARMOR: return "Armor";
        case OQ_TAB_ITEMS: return "Items";
        case OQ_TAB_MONSTERS: return "Monsters";
        default: return "Items";
    }
}

static int OQ_ItemMatchesTab(const oquake_inventory_entry_t* item, int tab) {
    const char* type = item ? item->item_type : NULL;
    const char* name = item ? item->name : NULL;
    /* API may return "KeyItem" or different casing; match type and name case-insensitively so API items show in correct tab. */
    int is_key = OQ_ContainsNoCase(type, "key") || (name && OQ_ContainsNoCase(name, "key"));
    int is_powerup = OQ_ContainsNoCase(type, "powerup") || (name && (OQ_ContainsNoCase(name, "Megahealth") || OQ_ContainsNoCase(name, "Ring") || OQ_ContainsNoCase(name, "Pentagram") || OQ_ContainsNoCase(name, "Biosuit") || OQ_ContainsNoCase(name, "Quad")));
    int is_weapon = OQ_ContainsNoCase(type, "weapon") || (name && (OQ_ContainsNoCase(name, "Shotgun") || OQ_ContainsNoCase(name, "Nailgun") || OQ_ContainsNoCase(name, "Launcher") || OQ_ContainsNoCase(name, "Lightning")));
    int is_ammo = OQ_ContainsNoCase(type, "ammo") || (name && (OQ_ContainsNoCase(name, "Shells") || OQ_ContainsNoCase(name, "Nails") || OQ_ContainsNoCase(name, "Rockets") || OQ_ContainsNoCase(name, "Cells")));
    int is_armor = OQ_ContainsNoCase(type, "armor") || (name && OQ_ContainsNoCase(name, "Armor"));
    int is_monster = OQ_ContainsNoCase(type, "monster") || (name && (strstr(name, "[NFT]") != NULL || strstr(name, "[BOSSNFT]") != NULL));

    switch (tab) {
        case OQ_TAB_KEYS: return is_key;
        case OQ_TAB_POWERUPS: return is_powerup;
        case OQ_TAB_WEAPONS: return is_weapon;
        case OQ_TAB_AMMO: return is_ammo;
        case OQ_TAB_ARMOR: return is_armor;
        case OQ_TAB_ITEMS: return !is_key && !is_powerup && !is_weapon && !is_ammo && !is_armor && !is_monster;
        case OQ_TAB_MONSTERS: return is_monster;
        default: return !is_key && !is_powerup && !is_weapon && !is_ammo && !is_armor && !is_monster;
    }
}

static int OQ_IsMockAnorakCredentials(const char* username, const char* password) {
    if (!username || !password)
        return 0;
    if (strcmp(password, "test!") != 0)
        return 0;
    return q_strcasecmp(username, "anorak") == 0 || q_strcasecmp(username, "avatar") == 0;
}

/* 1 if current user is dellams or anorak (for add, pickup keycard, bossnft, deploynft). */
static qboolean OQ_AllowPrivilegedCommands(void) {
    const char* u = g_star_username[0] ? g_star_username : (oquake_star_username.string && oquake_star_username.string[0] ? oquake_star_username.string : "");
    if (!u || !u[0]) return false;
    return (q_strcasecmp(u, "dellams") == 0 || q_strcasecmp(u, "anorak") == 0);
}

/** Called from main thread by star_sync_pump() when auth completes. */
static void OQ_OnAuthDone(void* user_data) {
    int success = 0;
    char username[64] = {0};
    char avatar_id[64] = {0};
    char error_msg[256] = {0};
    (void)user_data;
    if (!star_sync_auth_get_result(&success, username, sizeof(username), avatar_id, sizeof(avatar_id), error_msg, sizeof(error_msg)))
        return;
    if (success) {
        g_star_initialized = 1;
        q_strlcpy(g_star_username, username, sizeof(g_star_username));
        Cvar_Set("oquake_star_username", username);
        if (avatar_id[0]) {
            Cvar_Set("oquake_star_avatar_id", avatar_id);
            g_star_config.avatar_id = oquake_star_avatar_id.string;
            Con_Printf("Avatar ID: %s\n", avatar_id);
        } else {
            Con_Printf("Warning: Could not get avatar ID: %s\n", error_msg[0] ? error_msg : "Unknown error");
        }
        OQ_ApplyBeamFacePreference();
        if (!g_star_refresh_xp_called_this_session) {
            g_star_refresh_xp_called_this_session = 1;
            star_api_refresh_avatar_xp();
        }
        g_star_beamed_in = 1;
        Con_Printf("Logged in (beamin). Cross-game assets enabled.\n");
        g_inventory_last_refresh = 0.0;
    } else {
        Con_Printf("Beamin (SSO) failed: %s\n", error_msg[0] ? error_msg : "Unknown error");
    }
}

/** Called from main thread by star_sync_pump() when send-item completes. */
static void OQ_OnSendItemDone(void* user_data) {
    int success = 0;
    char err_buf[384] = {0};
    (void)user_data;
    if (!star_sync_send_item_get_result(&success, err_buf, sizeof(err_buf)))
        return;
    if (success) {
        q_strlcpy(g_inventory_status, "Item sent.", sizeof(g_inventory_status));
        Con_Printf("OQuake: Item sent.\n");
        OQ_RefreshOverlayFromClient(); /* Client updated cache; one get_inventory to refresh overlay. */
    } else {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Send failed: %s", err_buf[0] ? err_buf : "Unknown error");
        Con_Printf("OQuake: Send failed: %s\n", err_buf[0] ? err_buf : "Unknown error");
    }
}

static void OQ_CheckAuthenticationComplete(void) {
    if (!g_star_initialized)
        return;
    star_sync_pump();
}

static void OQ_CheckInventoryRefreshComplete(void) {
    if (!g_star_initialized)
        return;
    star_sync_pump();
}

/** Refresh overlay: get_inventory from C# client (returns API + pending merged). */
static void OQ_RefreshOverlayFromClient(void) {
    star_item_list_t* list = NULL;
    g_inventory_count = 0;
    if (star_api_get_inventory(&list) == STAR_API_SUCCESS && list) {
        size_t i;
        for (i = 0; i < list->count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS; i++) {
            oquake_inventory_entry_t* dst = &g_inventory_entries[g_inventory_count];
            q_strlcpy(dst->name, list->items[i].name, sizeof(dst->name));
            q_strlcpy(dst->description, list->items[i].description, sizeof(dst->description));
            q_strlcpy(dst->item_type, list->items[i].item_type, sizeof(dst->item_type));
            q_strlcpy(dst->id, list->items[i].id, sizeof(dst->id));
            q_strlcpy(dst->game_source, list->items[i].game_source, sizeof(dst->game_source));
            q_strlcpy(dst->nft_id, list->items[i].nft_id, sizeof(dst->nft_id));
            dst->quantity = list->items[i].quantity > 0 ? list->items[i].quantity : 1;
            g_inventory_count++;
        }
        star_api_free_item_list(list);
    } else {
        if (!star_initialized())
            q_strlcpy(g_inventory_status, "Offline - use STAR BEAMIN", sizeof(g_inventory_status));
    }
    g_inventory_last_refresh = realtime;
    /* Do not overwrite status when send is in progress so "Sending..." stays visible in bottom-right. */
    if (star_sync_send_item_in_progress())
        return;
    if (g_inventory_count == 0)
        q_strlcpy(g_inventory_status, "STAR inventory is empty.", sizeof(g_inventory_status));
    else if (!star_initialized())
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Local (%d items) - use STAR BEAMIN to sync", g_inventory_count);
    else
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Synced (%d items)", g_inventory_count);
}

/** C# client flushes add_item queue in background; no sync started from Quake. */
static void OQ_StartInventorySyncIfNeeded(void) {
    /* No-op: heavy lifting (sync, local delta, multithreading) is in C# StarApiClient. */
}

/** Refresh overlay from client (get_inventory returns API + pending merged in C#). */
static void OQ_RefreshInventoryCache(void) {
    if (star_sync_inventory_in_progress())
        return;
    if (star_sync_auth_in_progress()) {
        q_strlcpy(g_inventory_status, "Authenticating...", sizeof(g_inventory_status));
        return;
    }
    if (!star_initialized()) {
        /* Still build display from local pending so ammo/armor show when offline. */
        OQ_RefreshOverlayFromClient();
        q_strlcpy(g_inventory_status, "Offline - use STAR BEAMIN", sizeof(g_inventory_status));
        return;
    }
    /* Always refresh overlay (get_inventory returns API + pending from C#). */
    OQ_RefreshOverlayFromClient();
}

static void OQ_QuestToggle_f(void) {
    g_quest_popup_open = !g_quest_popup_open;
}

static void OQ_InventoryToggle_f(void) {
    g_inventory_open = !g_inventory_open;
    if (g_inventory_open) {
        g_inventory_selected_row = 0;
        g_inventory_scroll_row = 0;
        g_inventory_send_popup = OQ_SEND_POPUP_NONE;
        OQ_UpdatePopupInputCapture();
        OQ_UpdateSendPopupBindingCapture();
        OQ_RefreshInventoryCache();
    } else {
        g_inventory_send_popup = OQ_SEND_POPUP_NONE;
        OQ_UpdateSendPopupBindingCapture();
        OQ_UpdatePopupInputCapture();
    }
}

static void OQ_InventoryPrevTab_f(void) {
    if (!g_inventory_open || g_inventory_send_popup != OQ_SEND_POPUP_NONE)
        return;
    g_inventory_active_tab--;
    if (g_inventory_active_tab < 0)
        g_inventory_active_tab = OQ_TAB_COUNT - 1;
    g_inventory_selected_row = 0;
    g_inventory_scroll_row = 0;
}

static void OQ_InventoryNextTab_f(void) {
    if (!g_inventory_open || g_inventory_send_popup != OQ_SEND_POPUP_NONE)
        return;
    g_inventory_active_tab++;
    if (g_inventory_active_tab >= OQ_TAB_COUNT)
        g_inventory_active_tab = 0;
    g_inventory_selected_row = 0;
    g_inventory_scroll_row = 0;
}

static void OQ_PollInventoryHotkeys(void) {
    int grouped_count;
    if (!g_inventory_open)
        return;
    OQ_UpdatePopupInputCapture();
    OQ_UpdateSendPopupBindingCapture();
    if (key_dest == key_message)
        return;
    if (key_dest == key_console)
        return;
    if (key_dest == key_menu)
        return;

    {
        int rep_indices[OQ_MAX_INVENTORY_ITEMS];
        char labels[OQ_MAX_INVENTORY_ITEMS][OQ_GROUP_LABEL_MAX];
        int modes[OQ_MAX_INVENTORY_ITEMS];
        int values[OQ_MAX_INVENTORY_ITEMS];
        qboolean pending[OQ_MAX_INVENTORY_ITEMS];
        grouped_count = OQ_BuildGroupedRows(rep_indices, labels, modes, values, pending, OQ_MAX_INVENTORY_ITEMS);
    }
    OQ_ClampSelection(grouped_count);

    if (g_inventory_send_popup != OQ_SEND_POPUP_NONE) {
        int mode = OQ_GROUP_MODE_COUNT;
        int available = 1;
        OQ_HandleSendPopupTyping();
        OQ_GetSelectedGroupInfo(NULL, &mode, &available, NULL, 0);
        if (mode != OQ_GROUP_MODE_COUNT)
            available = 1;
        if (available < 1)
            available = 1;
        if (g_inventory_send_quantity < 1)
            g_inventory_send_quantity = 1;
        if (g_inventory_send_quantity > available)
            g_inventory_send_quantity = available;

        if (OQ_KeyPressed(K_ESCAPE)) {
            g_inventory_send_popup = OQ_SEND_POPUP_NONE;
            OQ_UpdateSendPopupBindingCapture();
            OQ_UpdatePopupInputCapture();
            return;
        }
        if (OQ_KeyPressed(K_LEFTARROW))
            g_inventory_send_button = 0; /* Send */
        if (OQ_KeyPressed(K_RIGHTARROW))
            g_inventory_send_button = 1; /* Cancel */
        if ((OQ_KeyPressed(K_UPARROW) || OQ_KeyPressed(K_PGUP) || OQ_KeyPressed(K_MWHEELUP)) && g_inventory_send_quantity < available)
            g_inventory_send_quantity++;
        if ((OQ_KeyPressed(K_DOWNARROW) || OQ_KeyPressed(K_PGDN) || OQ_KeyPressed(K_MWHEELDOWN)) && g_inventory_send_quantity > 1)
            g_inventory_send_quantity--;

        if (OQ_KeyPressed(K_ENTER) || OQ_KeyPressed(K_KP_ENTER)) {
            if (g_inventory_send_button == 0)
                OQ_SendSelectedItem();
            else {
                g_inventory_send_popup = OQ_SEND_POPUP_NONE;
                OQ_UpdateSendPopupBindingCapture();
                OQ_UpdatePopupInputCapture();
            }
        }
        return;
    }

    if (OQ_KeyPressed(K_LEFTARROW))
        OQ_InventoryPrevTab_f();
    if (OQ_KeyPressed(K_RIGHTARROW))
        OQ_InventoryNextTab_f();

    if (OQ_KeyPressed(K_UPARROW)) {
        g_inventory_selected_row--;
        OQ_ClampSelection(grouped_count);
    }
    if (OQ_KeyPressed(K_DOWNARROW)) {
        g_inventory_selected_row++;
        OQ_ClampSelection(grouped_count);
    }

    if (OQ_KeyPressed('e') || OQ_KeyPressed('E'))
        OQ_UseSelectedItem();
    if (OQ_KeyPressed('z') || OQ_KeyPressed('Z'))
        OQ_OpenSendPopup(OQ_SEND_POPUP_AVATAR);
    if (OQ_KeyPressed('x') || OQ_KeyPressed('X'))
        OQ_OpenSendPopup(OQ_SEND_POPUP_CLAN);
}

static int star_initialized(void) {
    return g_star_initialized;
}

static const char* get_key_description(const char* key_name) {
    if (strcmp(key_name, OQUAKE_ITEM_SILVER_KEY) == 0)
        return "Silver Key - Opens silver-marked doors";
    if (strcmp(key_name, OQUAKE_ITEM_GOLD_KEY) == 0)
        return "Gold Key - Opens gold-marked doors";
    return "Key from OQuake";
}

static int OQ_ShouldUseAnorakFace(void) {
    const char* activeName = g_star_username[0] ? g_star_username : "";
    return (oasis_star_beam_face.value > 0.5f) &&
           (q_strcasecmp(activeName, "anorak") == 0 || q_strcasecmp(activeName, "avatar") == 0 ||
            q_strcasecmp(activeName, "dellams") == 0);
}

static void OQ_ApplyBeamFacePreference(void) {
    int should_show = g_star_initialized && OQ_ShouldUseAnorakFace();
    Cvar_SetValueQuick(&oasis_star_anorak_face, should_show ? 1 : 0);
}

/*-----------------------------------------------------------------------------
 * OASIS STAR Config - JSON file support (fallback if Quake config.cfg fails)
 *-----------------------------------------------------------------------------*/

/* Forward declarations */
static int OQ_LoadJsonConfig(const char *json_path);

/* Console command to reload config from JSON */
static void OQ_ReloadConfig_f(void) {
    if (g_json_config_path[0]) {
        if (OQ_LoadJsonConfig(g_json_config_path)) {
            /* Re-apply the values to API config */
            const char* config_url = oquake_star_api_url.string;
            if (config_url && config_url[0]) {
                g_star_config.base_url = config_url;
            }
        }
    }
}

/* Helper function to find file in common locations */
static int OQ_FindConfigFile(const char *filename, char *out_path, int maxlen) {
    /* Try direct filename first (current directory / basedir) */
    FILE *test_file = fopen(filename, "r");
    if (test_file) {
        fclose(test_file);
        q_strlcpy(out_path, filename, maxlen);
        return 1;
    }
    
    const char *locations[] = {
        "build/",  /* Relative to exe if in build folder */
        "../build/",  /* One level up from exe */
        "../OASIS Omniverse/OQuake/build/",  /* From basedir, go to OQuake build */
        "../../OASIS Omniverse/OQuake/build/",  /* Two levels up */
        "OASIS Omniverse/OQuake/build/",  /* Relative from repo root */
        NULL
    };
    
    for (int i = 0; locations[i]; i++) {
        char test_path[512];
        q_snprintf(test_path, sizeof(test_path), "%s%s", locations[i], filename);
        test_file = fopen(test_path, "r");
        if (test_file) {
            fclose(test_file);
            q_strlcpy(out_path, test_path, maxlen);
            return 1;
        }
    }
    
    /* Try exe directory */
#ifdef _WIN32
    char exe_path[MAX_PATH] = {0};
    char exe_dir[MAX_PATH] = {0};
    if (GetModuleFileNameA(NULL, exe_path, sizeof(exe_path))) {
        char *last_slash = strrchr(exe_path, '\\');
        if (last_slash) {
            int dir_len = last_slash - exe_path;
            if (dir_len < sizeof(exe_dir)) {
                memcpy(exe_dir, exe_path, dir_len);
                exe_dir[dir_len] = 0;
                char test_path[512];
                q_snprintf(test_path, sizeof(test_path), "%s\\%s", exe_dir, filename);
                test_file = fopen(test_path, "r");
                if (test_file) {
                    fclose(test_file);
                    q_strlcpy(out_path, test_path, maxlen);
                    return 1;
                }
                /* Also try build subdirectory */
                q_snprintf(test_path, sizeof(test_path), "%s\\build\\%s", exe_dir, filename);
                test_file = fopen(test_path, "r");
                if (test_file) {
                    fclose(test_file);
                    q_strlcpy(out_path, test_path, maxlen);
                    return 1;
                }
            }
        }
    }
#endif
    return 0;
}

/* Simple JSON value extractor - finds "key": "value" or "key": value */
static int OQ_ExtractJsonValue(const char *json, const char *key, char *value, int maxlen) {
    char search[128];
    q_snprintf(search, sizeof(search), "\"%s\"", key);
    const char *pos = strstr(json, search);
    if (!pos) return 0;
    
    pos += strlen(search);
    while (*pos && (*pos == ' ' || *pos == ':' || *pos == '\t')) pos++;
    
    if (*pos == '"') {
        pos++;
        int n = 0;
        while (*pos && *pos != '"' && *pos != '\n' && *pos != '\r' && n < maxlen - 1) {
            if (*pos == '\\' && pos[1]) {
                pos++;
                if (*pos == 'n') value[n++] = '\n';
                else if (*pos == 't') value[n++] = '\t';
                else if (*pos == '\\') value[n++] = '\\';
                else if (*pos == '"') value[n++] = '"';
                else value[n++] = *pos;
            } else {
                value[n++] = *pos;
            }
            pos++;
        }
        value[n] = 0;
        return n > 0;
    } else {
        int n = 0;
        while (*pos && *pos != ',' && *pos != '}' && *pos != '\n' && *pos != '\r' && *pos != ' ' && n < maxlen - 1) {
            value[n++] = *pos++;
        }
        value[n] = 0;
        return n > 0;
    }
}

/* Load config from oasisstar.json */
static int OQ_LoadJsonConfig(const char *json_path) {
    FILE *f = fopen(json_path, "r");
    if (!f) {
        return 0;
    }
    
    char json[4096] = {0};
    size_t len = fread(json, 1, sizeof(json) - 1, f);
    fclose(f);
    if (len == 0) {
        return 0;
    }
    json[len] = 0;
    
    char value[256];
    int loaded = 0;
    
    if (OQ_ExtractJsonValue(json, "star_api_url", value, sizeof(value))) {
        Cvar_Set("oquake_star_api_url", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "oasis_api_url", value, sizeof(value))) {
        Cvar_Set("oquake_oasis_api_url", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "config_file", value, sizeof(value))) {
        Cvar_Set("oquake_star_config_file", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "beam_face", value, sizeof(value))) {
        Cvar_SetValueQuick(&oasis_star_beam_face, atoi(value));
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "stack_armor", value, sizeof(value))) {
        Cvar_Set("oquake_star_stack_armor", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "stack_weapons", value, sizeof(value))) {
        Cvar_Set("oquake_star_stack_weapons", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "stack_powerups", value, sizeof(value))) {
        Cvar_Set("oquake_star_stack_powerups", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "stack_keys", value, sizeof(value))) {
        Cvar_Set("oquake_star_stack_keys", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "stack_sigils", value, sizeof(value))) {
        Cvar_Set("oquake_star_stack_sigils", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "mint_weapons", value, sizeof(value))) {
        Cvar_Set("oquake_star_mint_weapons", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "mint_armor", value, sizeof(value))) {
        Cvar_Set("oquake_star_mint_armor", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "mint_powerups", value, sizeof(value))) {
        Cvar_Set("oquake_star_mint_powerups", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "mint_keys", value, sizeof(value))) {
        Cvar_Set("oquake_star_mint_keys", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "max_health", value, sizeof(value))) {
        Cvar_Set("oquake_star_max_health", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "max_armor", value, sizeof(value))) {
        Cvar_Set("oquake_star_max_armor", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "always_allow_pickup_if_max", value, sizeof(value))) {
        Cvar_Set("oquake_star_always_allow_pickup_if_max", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    /* Backward compat: old key "always_allow_pickup" = same as always_allow_pickup_if_max (so existing configs keep working). */
    else if (OQ_ExtractJsonValue(json, "always_allow_pickup", value, sizeof(value))) {
        Cvar_Set("oquake_star_always_allow_pickup_if_max", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "always_add_items_to_inventory", value, sizeof(value))) {
        Cvar_Set("oquake_star_always_add_items_to_inventory", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "use_health_on_pickup", value, sizeof(value))) {
        Cvar_Set("oquake_star_use_health_on_pickup", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "use_armor_on_pickup", value, sizeof(value))) {
        Cvar_Set("oquake_star_use_armor_on_pickup", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "use_powerup_on_pickup", value, sizeof(value))) {
        Cvar_Set("oquake_star_use_powerup_on_pickup", atoi(value) ? "1" : "0");
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "nft_provider", value, sizeof(value))) {
        Cvar_Set("oquake_star_nft_provider", value);
        loaded = 1;
    }
    if (OQ_ExtractJsonValue(json, "send_to_address_after_minting", value, sizeof(value))) {
        Cvar_Set("oquake_star_send_to_address_after_minting", value);
        loaded = 1;
    }
    /* Per-monster mint: mint_monster_oquake_dog, etc. Default 1 if key missing. */
    {
        int i, j;
        for (i = 0; i < OQ_MONSTER_COUNT && i < OQ_MONSTER_FLAGS_MAX; i++) {
            char key[128];
            int v = 1;
            q_snprintf(key, sizeof(key), "mint_monster_%s", OQUAKE_MONSTERS[i].config_key);
            if (OQ_ExtractJsonValue(json, key, value, sizeof(value)))
                v = (atoi(value) != 0) ? 1 : 0;
            for (j = 0; j < OQ_MONSTER_COUNT && j < OQ_MONSTER_FLAGS_MAX; j++)
                if (strcmp(OQUAKE_MONSTERS[j].config_key, OQUAKE_MONSTERS[i].config_key) == 0)
                    g_oq_mint_monster_flags[j] = v;
            loaded = 1;
        }
    }
    return loaded;
}

/* Save config to oasisstar.json */
static int OQ_SaveJsonConfig(const char *json_path) {
    FILE *f = fopen(json_path, "w");
    if (!f) return 0;
    
    const char *config_file = oquake_star_config_file.string;
    const char *star_url = oquake_star_api_url.string;
    const char *oasis_url = oquake_oasis_api_url.string;
    int beam_face = (int)oasis_star_beam_face.value;
    const char *s_armor = oquake_star_stack_armor.string;
    const char *s_weapons = oquake_star_stack_weapons.string;
    const char *s_powerups = oquake_star_stack_powerups.string;
    const char *s_keys = oquake_star_stack_keys.string;
    const char *s_sigils = oquake_star_stack_sigils.string;
    const char *m_weapons = oquake_star_mint_weapons.string;
    const char *m_armor = oquake_star_mint_armor.string;
    const char *m_powerups = oquake_star_mint_powerups.string;
    const char *m_keys = oquake_star_mint_keys.string;
    const char *max_h = oquake_star_max_health.string;
    const char *max_a = oquake_star_max_armor.string;
    const char *always_pickup = oquake_star_always_allow_pickup_if_max.string;
    const char *always_add = oquake_star_always_add_items_to_inventory.string;
    const char *nft_prov = oquake_star_nft_provider.string;
    const char *send_addr = oquake_star_send_to_address_after_minting.string;
    
    fprintf(f, "{\n");
    fprintf(f, "  \"config_file\": \"%s\",\n", config_file && config_file[0] ? config_file : "json");
    fprintf(f, "  \"star_api_url\": \"%s\",\n", star_url ? star_url : "");
    fprintf(f, "  \"oasis_api_url\": \"%s\",\n", oasis_url ? oasis_url : "");
    fprintf(f, "  \"beam_face\": %d,\n", beam_face);
    fprintf(f, "  \"stack_armor\": %s,\n", (s_armor && atoi(s_armor)) ? "1" : "0");
    fprintf(f, "  \"stack_weapons\": %s,\n", (s_weapons && atoi(s_weapons)) ? "1" : "0");
    fprintf(f, "  \"stack_powerups\": %s,\n", (s_powerups && atoi(s_powerups)) ? "1" : "0");
    fprintf(f, "  \"stack_keys\": %s,\n", (s_keys && atoi(s_keys)) ? "1" : "0");
    fprintf(f, "  \"stack_sigils\": %s,\n", (s_sigils && atoi(s_sigils)) ? "1" : "0");
    fprintf(f, "  \"mint_weapons\": %s,\n", (m_weapons && atoi(m_weapons)) ? "1" : "0");
    fprintf(f, "  \"mint_armor\": %s,\n", (m_armor && atoi(m_armor)) ? "1" : "0");
    fprintf(f, "  \"mint_powerups\": %s,\n", (m_powerups && atoi(m_powerups)) ? "1" : "0");
    fprintf(f, "  \"mint_keys\": %s,\n", (m_keys && atoi(m_keys)) ? "1" : "0");
    fprintf(f, "  \"max_health\": %s,\n", max_h && atoi(max_h) > 0 ? max_h : "100");
    fprintf(f, "  \"max_armor\": %s,\n", max_a && atoi(max_a) > 0 ? max_a : "100");
    fprintf(f, "  \"always_allow_pickup_if_max\": %s,\n", (always_pickup && atoi(always_pickup)) ? "1" : "0");
    fprintf(f, "  \"always_add_items_to_inventory\": %s,\n", (always_add && atoi(always_add)) ? "1" : "0");
    fprintf(f, "  \"use_health_on_pickup\": %s,\n", (oquake_star_use_health_on_pickup.string && atoi(oquake_star_use_health_on_pickup.string)) ? "1" : "0");
    fprintf(f, "  \"use_armor_on_pickup\": %s,\n", (oquake_star_use_armor_on_pickup.string && atoi(oquake_star_use_armor_on_pickup.string)) ? "1" : "0");
    fprintf(f, "  \"use_powerup_on_pickup\": %s,\n", (oquake_star_use_powerup_on_pickup.string && atoi(oquake_star_use_powerup_on_pickup.string)) ? "1" : "0");
    fprintf(f, "  \"nft_provider\": \"");
    if (nft_prov && nft_prov[0]) {
        const char* p;
        for (p = nft_prov; *p; p++) {
            if (*p == '"' || *p == '\\') fputc('\\', f);
            fputc((unsigned char)*p, f);
        }
    } else {
        fprintf(f, "SolanaOASIS");
    }
    fprintf(f, "\",\n");
    fprintf(f, "  \"send_to_address_after_minting\": \"");
    if (send_addr && send_addr[0]) {
        const char* p;
        for (p = send_addr; *p; p++) {
            if (*p == '"' || *p == '\\') fputc('\\', f);
            fputc((unsigned char)*p, f);
        }
    }
    fprintf(f, "\"");
    /* mint_monster_oquake_* (unique config_keys only) */
    {
        int i, j;
        for (i = 0; i < OQ_MONSTER_COUNT && i < OQ_MONSTER_FLAGS_MAX; i++) {
            int already = 0;
            for (j = 0; j < i; j++)
                if (strcmp(OQUAKE_MONSTERS[j].config_key, OQUAKE_MONSTERS[i].config_key) == 0) { already = 1; break; }
            if (already) continue;
            fprintf(f, ",\n  \"mint_monster_%s\": %d", OQUAKE_MONSTERS[i].config_key, g_oq_mint_monster_flags[i] ? 1 : 0);
        }
    }
    fprintf(f, "\n}\n");
    
    fclose(f);
    return 1;
}

/* Get file modification time (Windows) */
#ifdef _WIN32
static time_t OQ_GetFileTime(const char *path) {
    WIN32_FIND_DATAA findData;
    HANDLE hFind = FindFirstFileA(path, &findData);
    if (hFind == INVALID_HANDLE_VALUE) return 0;
    FindClose(hFind);
    
    FILETIME ft = findData.ftLastWriteTime;
    ULARGE_INTEGER ul;
    ul.LowPart = ft.dwLowDateTime;
    ul.HighPart = ft.dwHighDateTime;
    return (time_t)(ul.QuadPart / 10000000ULL - 11644473600ULL);
}
#else
static time_t OQ_GetFileTime(const char *path) {
    struct stat st;
    if (stat(path, &st) == 0) return st.st_mtime;
    return 0;
}
#endif

#define OQ_CFG_MAX_SIZE (256 * 1024)

/* Returns 1 if line should be removed (OQuake STAR cvar or our comment). */
static int OQ_IsOQuakeCfgLine(const char *line) {
    while (*line == ' ' || *line == '\t') line++;
    if (strstr(line, "// OQuake STAR API Configuration") != NULL) return 1;
    if (strncmp(line, "set oquake_star_", 15) == 0) return 1;
    if (strncmp(line, "set oasis_star_beam_face", 24) == 0) return 1;
    return 0;
}

/* Save config to Quake config.cfg: update in place (strip old OQuake lines, append one block). */
static int OQ_SaveQuakeConfig(const char *cfg_path) {
    char *buf = NULL;
    size_t cap = OQ_CFG_MAX_SIZE;
    size_t len = 0;
    FILE *f = fopen(cfg_path, "rb");
    if (f) {
        buf = (char *)malloc(cap);
        if (buf) {
            len = fread(buf, 1, cap - 1, f);
            buf[len] = '\0';
        }
        fclose(f);
    }
    f = fopen(cfg_path, "w");
    if (!f) {
        if (buf) free(buf);
        return 0;
    }
    if (buf && len > 0) {
        const char *p = buf;
        while (*p) {
            const char *eol = strchr(p, '\n');
            if (!eol) eol = p + strlen(p);
            if (eol > p) {
                size_t linelen = (size_t)(eol - p);
                if (linelen < 2048) {
                    char line[2048];
                    if (linelen >= sizeof(line)) linelen = sizeof(line) - 1;
                    memcpy(line, p, linelen);
                    line[linelen] = '\0';
                    if (!OQ_IsOQuakeCfgLine(line))
                        fwrite(p, 1, (size_t)(eol - p), f);
                } else {
                    fwrite(p, 1, (size_t)(eol - p), f);
                }
            }
            if (*eol == '\n') eol++;
            p = eol;
        }
        free(buf);
    }
    {
        const char *star_url = oquake_star_api_url.string;
        const char *oasis_url = oquake_oasis_api_url.string;
        fprintf(f, "\n// OQuake STAR API Configuration (auto-generated)\n");
        fprintf(f, "set oquake_star_config_file \"%s\"\n", oquake_star_config_file.string ? oquake_star_config_file.string : "json");
        fprintf(f, "set oquake_star_api_url \"%s\"\n", star_url ? star_url : "");
        fprintf(f, "set oquake_oasis_api_url \"%s\"\n", oasis_url ? oasis_url : "");
        fprintf(f, "set oasis_star_beam_face \"%d\"\n", (int)oasis_star_beam_face.value);
        fprintf(f, "set oquake_star_stack_armor \"%s\"\n", oquake_star_stack_armor.string);
        fprintf(f, "set oquake_star_stack_weapons \"%s\"\n", oquake_star_stack_weapons.string);
        fprintf(f, "set oquake_star_stack_powerups \"%s\"\n", oquake_star_stack_powerups.string);
        fprintf(f, "set oquake_star_stack_keys \"%s\"\n", oquake_star_stack_keys.string);
        fprintf(f, "set oquake_star_stack_sigils \"%s\"\n", oquake_star_stack_sigils.string);
        fprintf(f, "set oquake_star_mint_weapons \"%s\"\n", atoi(oquake_star_mint_weapons.string) ? "1" : "0");
        fprintf(f, "set oquake_star_mint_armor \"%s\"\n", atoi(oquake_star_mint_armor.string) ? "1" : "0");
        fprintf(f, "set oquake_star_mint_powerups \"%s\"\n", atoi(oquake_star_mint_powerups.string) ? "1" : "0");
        fprintf(f, "set oquake_star_mint_keys \"%s\"\n", atoi(oquake_star_mint_keys.string) ? "1" : "0");
        fprintf(f, "set oquake_star_max_health \"%s\"\n", oquake_star_max_health.string ? oquake_star_max_health.string : "100");
        fprintf(f, "set oquake_star_max_armor \"%s\"\n", oquake_star_max_armor.string ? oquake_star_max_armor.string : "100");
        fprintf(f, "set oquake_star_always_allow_pickup_if_max \"%s\"\n", atoi(oquake_star_always_allow_pickup_if_max.string) ? "1" : "0");
        fprintf(f, "set oquake_star_always_add_items_to_inventory \"%s\"\n", atoi(oquake_star_always_add_items_to_inventory.string) ? "1" : "0");
        fprintf(f, "set oquake_star_use_health_on_pickup \"%s\"\n", atoi(oquake_star_use_health_on_pickup.string) ? "1" : "0");
        fprintf(f, "set oquake_star_use_armor_on_pickup \"%s\"\n", atoi(oquake_star_use_armor_on_pickup.string) ? "1" : "0");
        fprintf(f, "set oquake_star_use_powerup_on_pickup \"%s\"\n", atoi(oquake_star_use_powerup_on_pickup.string) ? "1" : "0");
        fprintf(f, "set oquake_star_nft_provider \"%s\"\n", oquake_star_nft_provider.string ? oquake_star_nft_provider.string : "SolanaOASIS");
        fprintf(f, "set oquake_star_send_to_address_after_minting \"%s\"\n", oquake_star_send_to_address_after_minting.string ? oquake_star_send_to_address_after_minting.string : "");
    }
    fclose(f);
    return 1;
}

/** Write current STAR cvars to oasisstar.json and config.cfg. Used on exit, star config save, star stack, star face. */
static void OQ_SaveStarConfigToFiles(void) {
    char json_path[512] = {0}, cfg_path[512] = {0};
    int found_json = g_json_config_path[0] ? 1 : 0;
    if (found_json)
        q_strlcpy(json_path, g_json_config_path, sizeof(json_path));
    else
        found_json = OQ_FindConfigFile("oasisstar.json", json_path, sizeof(json_path));
    if (OQ_FindConfigFile("config.cfg", cfg_path, sizeof(cfg_path)))
        OQ_SaveQuakeConfig(cfg_path);
    if (found_json)
        OQ_SaveJsonConfig(json_path);
}

/* Sync config files - load from newer, save to older */
static void OQ_SyncConfigFiles(const char *cfg_path, const char *json_path) {
    time_t cfg_time = 0, json_time = 0;
    int cfg_exists = 0, json_exists = 0;
    
    if (cfg_path) {
        cfg_time = OQ_GetFileTime(cfg_path);
        cfg_exists = (cfg_time > 0);
    }
    if (json_path) {
        json_time = OQ_GetFileTime(json_path);
        json_exists = (json_time > 0);
    }
    
    if (!cfg_exists && !json_exists) return; /* Neither exists */
    
    if (cfg_exists && json_exists) {
        /* Both exist - load from newer, sync to older */
        if (cfg_time > json_time) {
            /* config.cfg is newer - already loaded, save to JSON */
            OQ_SaveJsonConfig(json_path);
        } else if (json_time > cfg_time) {
            /* JSON is newer - load from JSON, save to config.cfg */
            if (OQ_LoadJsonConfig(json_path)) {
                OQ_SaveQuakeConfig(cfg_path);
            }
        }
    } else if (json_exists && !cfg_exists) {
        /* Only JSON exists - load it */
        OQ_LoadJsonConfig(json_path);
    }
    /* If only cfg exists, it's already loaded */
}

/* Forward declaration */
// static void OQ_DebugMode_f(void); // Temporarily disabled

/*-----------------------------------------------------------------------------
 * OQ_StarConfig_f - Show STAR config. Defined here so it is visible when
 * Cmd_AddCommand(..., OQ_StarConfig_f) is used in OQuake_STAR_Init (MSVC needs def before use).
 *-----------------------------------------------------------------------------*/
static void OQ_StarConfig_f(void) {
    const char* star_url = oquake_star_api_url.string;
    const char* oasis_url = oquake_oasis_api_url.string;
    int using_defaults = 0;
    if (star_url && star_url[0] && strcmp(star_url, "https://star-api.oasisplatform.world/api") == 0)
        using_defaults = 1;
    if (oasis_url && oasis_url[0] && strcmp(oasis_url, "https://api.oasisplatform.world") == 0)
        using_defaults = 1;
    Con_Printf("\n");
    Con_Printf("OQuake STAR Configuration:\n");
    if (using_defaults) {
        Con_Printf("  [WARNING: Using default values - config file may not be loaded]\n");
        Con_Printf("  Try running: exec config.cfg  or  star reloadconfig\n");
        Con_Printf("\n");
    }
    Con_Printf("  Config file: %s\n", oquake_star_config_file.string && oquake_star_config_file.string[0] ? oquake_star_config_file.string : "json");
    Con_Printf("  STAR API URL: %s\n", star_url && star_url[0] ? star_url : "(default: https://oasisweb4.com/star/api)");
    Con_Printf("  OASIS API URL: %s\n", oasis_url && oasis_url[0] ? oasis_url : "(default: https://oasisweb4.com/api)");
    Con_Printf("  Username: %s\n", oquake_star_username.string && oquake_star_username.string[0] ? oquake_star_username.string : "(not set)");
    Con_Printf("  Password: %s\n", oquake_star_password.string && oquake_star_password.string[0] ? "***" : "(not set)");
    Con_Printf("  API Key: %s\n", oquake_star_api_key.string && oquake_star_api_key.string[0] ? "***" : "(not set)");
    Con_Printf("  Avatar ID: %s\n", oquake_star_avatar_id.string && oquake_star_avatar_id.string[0] ? oquake_star_avatar_id.string : "(not set)");
    Con_Printf("  Beam face: %s\n", oasis_star_beam_face.value > 0.5f ? "on" : "off");
    Con_Printf("\n");
    Con_Printf("  Stack (1) / Unlock (0) - ammo always stacks:\n");
    Con_Printf("    stack_armor:    %s\n", (oquake_star_stack_armor.string && atoi(oquake_star_stack_armor.string)) ? "1 (stack)" : "0 (unlock)");
    Con_Printf("    stack_weapons:  %s\n", (oquake_star_stack_weapons.string && atoi(oquake_star_stack_weapons.string)) ? "1 (stack)" : "0 (unlock)");
    Con_Printf("    stack_powerups: %s\n", (oquake_star_stack_powerups.string && atoi(oquake_star_stack_powerups.string)) ? "1 (stack)" : "0 (unlock)");
    Con_Printf("    stack_keys:     %s\n", (oquake_star_stack_keys.string && atoi(oquake_star_stack_keys.string)) ? "1 (stack)" : "0 (unlock)");
    Con_Printf("    stack_sigils:   %s (OQuake only)\n", (oquake_star_stack_sigils.string && atoi(oquake_star_stack_sigils.string)) ? "1 (stack)" : "0 (unlock)");
    Con_Printf("\n");
    Con_Printf("  Mint NFT when collecting (1=on, 0=off):\n");
    Con_Printf("    mint_weapons:   %s\n", (oquake_star_mint_weapons.string && atoi(oquake_star_mint_weapons.string)) ? "1" : "0");
    Con_Printf("    mint_armor:     %s\n", (oquake_star_mint_armor.string && atoi(oquake_star_mint_armor.string)) ? "1" : "0");
    Con_Printf("    mint_powerups:  %s\n", (oquake_star_mint_powerups.string && atoi(oquake_star_mint_powerups.string)) ? "1" : "0");
    Con_Printf("    mint_keys:      %s\n", (oquake_star_mint_keys.string && atoi(oquake_star_mint_keys.string)) ? "1" : "0");
    Con_Printf("\n");
    Con_Printf("  Mint NFT when killing monster (1=on, 0=off). Set: star mint monster <name> <0|1>\n");
    {
        int i, j;
        for (i = 0; i < OQ_MONSTER_COUNT && i < OQ_MONSTER_FLAGS_MAX; i++) {
            int already = 0;
            for (j = 0; j < i; j++)
                if (strcmp(OQUAKE_MONSTERS[j].config_key, OQUAKE_MONSTERS[i].config_key) == 0) { already = 1; break; }
            if (already) continue;
            Con_Printf("    %s  mint_monster_%s: %s\n", OQUAKE_MONSTERS[i].display_name, OQUAKE_MONSTERS[i].config_key, g_oq_mint_monster_flags[i] ? "1" : "0");
        }
    }
    Con_Printf("  NFT mint provider: %s\n", oquake_star_nft_provider.string && oquake_star_nft_provider.string[0] ? oquake_star_nft_provider.string : "SolanaOASIS");
    Con_Printf("  Send to address after minting: %s\n", oquake_star_send_to_address_after_minting.string && oquake_star_send_to_address_after_minting.string[0] ? oquake_star_send_to_address_after_minting.string : "(none)");
    Con_Printf("\n");
    Con_Printf("  max_health: %s  max_armor: %s  \n", oquake_star_max_health.string && oquake_star_max_health.string[0] ? oquake_star_max_health.string : "100", oquake_star_max_armor.string && oquake_star_max_armor.string[0] ? oquake_star_max_armor.string : "100");
    Con_Printf("  always_allow_pickup_if_max: %s  (1=at max still pick up into STAR)\n", (oquake_star_always_allow_pickup_if_max.string && atoi(oquake_star_always_allow_pickup_if_max.string)) ? "1" : "0");
    Con_Printf("  always_add_items_to_inventory: %s  (1=always add to STAR even when engine uses it)\n", (oquake_star_always_add_items_to_inventory.string && atoi(oquake_star_always_add_items_to_inventory.string)) ? "1" : "0");
    Con_Printf("  use_health_on_pickup: %s  (0=below max -> inventory only; 1=standard)\n", (oquake_star_use_health_on_pickup.string && atoi(oquake_star_use_health_on_pickup.string)) ? "1" : "0");
    Con_Printf("  use_armor_on_pickup: %s  (0=below max -> inventory only; 1=standard)\n", (oquake_star_use_armor_on_pickup.string && atoi(oquake_star_use_armor_on_pickup.string)) ? "1" : "0");
    Con_Printf("  use_powerup_on_pickup: %s  (0=below max -> inventory only; 1=standard)\n", (oquake_star_use_powerup_on_pickup.string && atoi(oquake_star_use_powerup_on_pickup.string)) ? "1" : "0");
    Con_Printf("\n");
    Con_Printf("To set: star pickup ifmax <0|1>   star pickup all <0|1>\n");
    Con_Printf("        star stack <armor|weapons|powerups|keys|sigils> <0|1> (sigils = OQuake only)\n");
    Con_Printf("        star mint <armor|weapons|powerups|keys> <0|1>\n");
    Con_Printf("        star mint monster <name> <0|1>  (e.g. star mint monster oquake_ogre 0)\n");
    Con_Printf("        star nftprovider <name>\n");
     Con_Printf("\n");
    Con_Printf("URLs: star seturl <url>   star setoasisurl <url>\n");
    Con_Printf("Config file: star configfile json|cfg\n");
    Con_Printf("To save now: star config save (also saved on exit)\n");
    Con_Printf("Auth: set oquake_star_username \"...\" or star beamin <user> <pass>\n");
    Con_Printf("\n");
}

void OQuake_STAR_Init(void) {
    star_sync_init();
    star_sync_set_add_item_log_cb(OQ_AddItemLogCb, NULL);
    star_api_result_t result;
    const char* username;
    const char* password;

    Cvar_RegisterVariable(&oasis_star_anorak_face);
    Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
    Cvar_RegisterVariable(&oasis_star_beam_face);
    Cvar_RegisterVariable(&oquake_star_config_file); /* Register this first so we can check it */
    Cvar_RegisterVariable(&oquake_star_api_url);
    Cvar_RegisterVariable(&oquake_oasis_api_url);
    Cvar_RegisterVariable(&oquake_star_username);
    Cvar_RegisterVariable(&oquake_star_password);
    Cvar_RegisterVariable(&oquake_star_api_key);
    Cvar_RegisterVariable(&oquake_star_avatar_id);
    Cvar_RegisterVariable(&oquake_star_stack_armor);
    Cvar_RegisterVariable(&oquake_star_stack_weapons);
    Cvar_RegisterVariable(&oquake_star_stack_powerups);
    Cvar_RegisterVariable(&oquake_star_stack_keys);
    Cvar_RegisterVariable(&oquake_star_stack_sigils);
    Cvar_RegisterVariable(&oquake_star_mint_weapons);
    Cvar_RegisterVariable(&oquake_star_mint_armor);
    Cvar_RegisterVariable(&oquake_star_mint_powerups);
    Cvar_RegisterVariable(&oquake_star_mint_keys);
    Cvar_RegisterVariable(&oquake_star_nft_provider);
    Cvar_RegisterVariable(&oquake_star_send_to_address_after_minting);
    Cvar_RegisterVariable(&oquake_star_max_health);
    Cvar_RegisterVariable(&oquake_star_max_armor);
    Cvar_RegisterVariable(&oquake_star_always_allow_pickup_if_max);
    Cvar_RegisterVariable(&oquake_star_always_add_items_to_inventory);
    Cvar_RegisterVariable(&oquake_star_use_health_on_pickup);
    Cvar_RegisterVariable(&oquake_star_use_armor_on_pickup);
    Cvar_RegisterVariable(&oquake_star_use_powerup_on_pickup);

    /* Default all monster mint flags to 1 (load from JSON may override) */
    {
        int i;
        for (i = 0; i < OQ_MONSTER_FLAGS_MAX; i++)
            g_oq_mint_monster_flags[i] = 1;
    }

    if (!g_star_console_registered) {
        Cmd_AddCommand("star", OQuake_STAR_Console_f);
        Cmd_AddCommand("starconfig", OQ_StarConfig_f);
        Cmd_AddCommand("star_config", OQ_StarConfig_f);   /* Alias: use if "star config" is unrecognized (e.g. engine tokenizes) */
        Cmd_AddCommand("star config", OQ_StarConfig_f);   /* Some engines treat "star config" as one command name */
        Cmd_AddCommand("oasis_inventory_toggle", OQ_InventoryToggle_f);
        Cmd_AddCommand("oasis_inventory_prevtab", OQ_InventoryPrevTab_f);
        Cmd_AddCommand("oasis_inventory_nexttab", OQ_InventoryNextTab_f);
        Cmd_AddCommand("oasis_reload_config", OQ_ReloadConfig_f);
        Cmd_AddCommand("oquake_use_health", OQ_UseHealth_f);
        Cmd_AddCommand("oquake_use_armor", OQ_UseArmor_f);
        g_star_console_registered = 1;
        /* Default: I key opens OASIS inventory if not already bound */
        {
            int kn = Key_StringToKeynum("i");
            if (kn >= 0 && kn < MAX_KEYS && (!keybindings[kn] || !keybindings[kn][0]))
                Key_SetBinding(kn, "oasis_inventory_toggle");
        }
        /* Q = quest popup (like ODOOM) */
        Cmd_AddCommand("oquake_quest_toggle", OQ_QuestToggle_f);
        {
            int kq = Key_StringToKeynum("q");
            if (kq >= 0 && kq < MAX_KEYS && (!keybindings[kq] || !keybindings[kq][0]))
                Key_SetBinding(kq, "oquake_quest_toggle");
        }
        /* C = use health, F = use armor (like ODOOM) */
        {
            int kc = Key_StringToKeynum("c");
            int kf = Key_StringToKeynum("f");
            if (kc >= 0 && kc < MAX_KEYS && (!keybindings[kc] || !keybindings[kc][0]))
                Key_SetBinding(kc, "oquake_use_health");
            if (kf >= 0 && kf < MAX_KEYS && (!keybindings[kf] || !keybindings[kf][0]))
                Key_SetBinding(kf, "oquake_use_armor");
        }
    }

    /* Try to auto-load config from config.cfg or oasisstar.json */
    /* Default is to use oasisstar.json to avoid Quake's exec overwriting values */
    {
        int config_loaded = 0;
        char found_cfg_path[512] = {0};
        char found_json_path[512] = {0};
        
        /* Check which config file type to use (default: json) */
        const char *config_type = oquake_star_config_file.string;
        int use_json = 1; /* Default to JSON */
        if (config_type && config_type[0] && q_strcasecmp(config_type, "cfg") == 0) {
            use_json = 0;
        }
        
        /* Find both files */
        int found_cfg = OQ_FindConfigFile("config.cfg", found_cfg_path, sizeof(found_cfg_path));
        int found_json = OQ_FindConfigFile("oasisstar.json", found_json_path, sizeof(found_json_path));
        
        /* Show config preference */
        Con_Printf("OQuake: Config preference: %s\n", use_json ? "oasisstar.json" : "config.cfg");
        
        /* If JSON not found but config.cfg exists, load config.cfg first to get values, then create JSON */
        if (!found_json && found_cfg && use_json) {
            /* Load from config.cfg first to populate CVARs */
            FILE *f = fopen(found_cfg_path, "r");
            if (f) {
                char line[256];
                while (fgets(line, sizeof(line), f)) {
                    char *p = line;
                    while (*p && (*p == ' ' || *p == '\t')) p++;
                    if (*p == '\n' || *p == '\r' || *p == 0) continue;
                    if (*p == '/' && p[1] == '/') continue;
                    if (*p == '#') continue;
                    
                    if (strncmp(p, "set ", 4) == 0) {
                        p += 4;
                        while (*p && (*p == ' ' || *p == '\t')) p++;
                        
                        char cvar_name[64] = {0};
                        char cvar_value[256] = {0};
                        int n = 0;
                        
                        while (*p && *p != ' ' && *p != '\t' && *p != '\n' && *p != '\r' && n < sizeof(cvar_name) - 1) {
                            cvar_name[n++] = *p++;
                        }
                        
                        if (n > 0) {
                            cvar_name[n] = 0;
                            while (*p && (*p == ' ' || *p == '\t')) p++;
                            
                            if (*p == '"') {
                                p++;
                                n = 0;
                                while (*p && *p != '"' && *p != '\n' && *p != '\r' && n < sizeof(cvar_value) - 1) {
                                    cvar_value[n++] = *p++;
                                }
                            } else {
                                n = 0;
                                while (*p && *p != ' ' && *p != '\t' && *p != '\n' && *p != '\r' && n < sizeof(cvar_value) - 1) {
                                    cvar_value[n++] = *p++;
                                }
                            }
                            
                            if (n > 0) {
                                cvar_value[n] = 0;
                                if (strcmp(cvar_name, "oquake_star_config_file") == 0) {
                                    Cvar_Set("oquake_star_config_file", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_api_url") == 0) {
                                    Cvar_Set("oquake_star_api_url", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_oasis_api_url") == 0) {
                                    Cvar_Set("oquake_oasis_api_url", cvar_value);
                                } else if (strcmp(cvar_name, "oasis_star_beam_face") == 0) {
                                    Cvar_SetValueQuick(&oasis_star_beam_face, atoi(cvar_value));
                                } else if (strcmp(cvar_name, "oquake_star_stack_armor") == 0) {
                                    Cvar_Set("oquake_star_stack_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_weapons") == 0) {
                                    Cvar_Set("oquake_star_stack_weapons", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_powerups") == 0) {
                                    Cvar_Set("oquake_star_stack_powerups", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_keys") == 0) {
                                    Cvar_Set("oquake_star_stack_keys", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_sigils") == 0) {
                                    Cvar_Set("oquake_star_stack_sigils", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_weapons") == 0) {
                                    Cvar_Set("oquake_star_mint_weapons", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_armor") == 0) {
                                    Cvar_Set("oquake_star_mint_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_powerups") == 0) {
                                    Cvar_Set("oquake_star_mint_powerups", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_keys") == 0) {
                                    Cvar_Set("oquake_star_mint_keys", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_max_health") == 0) {
                                    Cvar_Set("oquake_star_max_health", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_max_armor") == 0) {
                                    Cvar_Set("oquake_star_max_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_always_allow_pickup_if_max") == 0) {
                                    Cvar_Set("oquake_star_always_allow_pickup_if_max", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_always_add_items_to_inventory") == 0) {
                                    Cvar_Set("oquake_star_always_add_items_to_inventory", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_health_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_health_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_armor_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_armor_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_powerup_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_powerup_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_nft_provider") == 0) {
                                    Cvar_Set("oquake_star_nft_provider", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_send_to_address_after_minting") == 0) {
                                    Cvar_Set("oquake_star_send_to_address_after_minting", cvar_value);
                                }
                            }
                        }
                    }
                }
                fclose(f);
                config_loaded = 1;
                Con_Printf("OQuake: Loaded config from: %s\n", found_cfg_path);
                const char* star_url = oquake_star_api_url.string;
                const char* oasis_url = oquake_oasis_api_url.string;
                if (star_url && star_url[0]) {
                    Con_Printf("OQuake: STAR API URL: %s\n", star_url);
                }
                if (oasis_url && oasis_url[0]) {
                    Con_Printf("OQuake: OASIS API URL: %s\n", oasis_url);
                }
                
                /* Now create JSON file in same directory */
                q_strlcpy(found_json_path, found_cfg_path, sizeof(found_json_path));
                char *slash = strrchr(found_json_path, '\\');
                if (!slash) slash = strrchr(found_json_path, '/');
                if (slash) {
                    q_strlcpy(slash + 1, "oasisstar.json", sizeof(found_json_path) - (slash + 1 - found_json_path));
                } else {
                    q_strlcpy(found_json_path, "oasisstar.json", sizeof(found_json_path));
                }
                if (OQ_SaveJsonConfig(found_json_path)) {
                    found_json = 1; /* Mark as found so we use it next time */
                    Con_Printf("OQuake: Created JSON config: %s\n", found_json_path);
                }
            }
        }
        
        /* Load based on preference and availability */
        if (use_json && found_json) {
            /* Prefer JSON - load it */
            if (OQ_LoadJsonConfig(found_json_path)) {
                config_loaded = 1;
                Con_Printf("OQuake: Loaded config from: %s\n", found_json_path);
                const char* star_url = oquake_star_api_url.string;
                const char* oasis_url = oquake_oasis_api_url.string;
                if (star_url && star_url[0]) {
                    Con_Printf("OQuake: STAR API URL: %s\n", star_url);
                }
                if (oasis_url && oasis_url[0]) {
                    Con_Printf("OQuake: OASIS API URL: %s\n", oasis_url);
                }
                /* Sync to config.cfg if it exists */
                if (found_cfg) {
                    OQ_SyncConfigFiles(found_cfg_path, found_json_path);
                }
            }
        } else if (!use_json && found_cfg) {
            /* Prefer config.cfg - load it */
            FILE *f = fopen(found_cfg_path, "r");
            if (f) {
                char line[256];
                while (fgets(line, sizeof(line), f)) {
                    char *p = line;
                    while (*p && (*p == ' ' || *p == '\t')) p++;
                    if (*p == '\n' || *p == '\r' || *p == 0) continue;
                    if (*p == '/' && p[1] == '/') continue;
                    if (*p == '#') continue;
                    
                    if (strncmp(p, "set ", 4) == 0) {
                        p += 4;
                        while (*p && (*p == ' ' || *p == '\t')) p++;
                        
                        char cvar_name[64] = {0};
                        char cvar_value[256] = {0};
                        int n = 0;
                        
                        while (*p && *p != ' ' && *p != '\t' && *p != '\n' && *p != '\r' && n < sizeof(cvar_name) - 1) {
                            cvar_name[n++] = *p++;
                        }
                        
                        if (n > 0) {
                            cvar_name[n] = 0;
                            while (*p && (*p == ' ' || *p == '\t')) p++;
                            
                            if (*p == '"') {
                                p++;
                                n = 0;
                                while (*p && *p != '"' && *p != '\n' && *p != '\r' && n < sizeof(cvar_value) - 1) {
                                    cvar_value[n++] = *p++;
                                }
                            } else {
                                n = 0;
                                while (*p && *p != ' ' && *p != '\t' && *p != '\n' && *p != '\r' && n < sizeof(cvar_value) - 1) {
                                    cvar_value[n++] = *p++;
                                }
                            }
                            
                            if (n > 0) {
                                cvar_value[n] = 0;
                                if (strcmp(cvar_name, "oquake_star_config_file") == 0) {
                                    Cvar_Set("oquake_star_config_file", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_api_url") == 0) {
                                    Cvar_Set("oquake_star_api_url", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_oasis_api_url") == 0) {
                                    Cvar_Set("oquake_oasis_api_url", cvar_value);
                                } else if (strcmp(cvar_name, "oasis_star_beam_face") == 0) {
                                    Cvar_SetValueQuick(&oasis_star_beam_face, atoi(cvar_value));
                                } else if (strcmp(cvar_name, "oquake_star_stack_armor") == 0) {
                                    Cvar_Set("oquake_star_stack_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_weapons") == 0) {
                                    Cvar_Set("oquake_star_stack_weapons", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_powerups") == 0) {
                                    Cvar_Set("oquake_star_stack_powerups", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_keys") == 0) {
                                    Cvar_Set("oquake_star_stack_keys", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_sigils") == 0) {
                                    Cvar_Set("oquake_star_stack_sigils", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_weapons") == 0) {
                                    Cvar_Set("oquake_star_mint_weapons", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_armor") == 0) {
                                    Cvar_Set("oquake_star_mint_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_powerups") == 0) {
                                    Cvar_Set("oquake_star_mint_powerups", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_keys") == 0) {
                                    Cvar_Set("oquake_star_mint_keys", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_max_health") == 0) {
                                    Cvar_Set("oquake_star_max_health", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_max_armor") == 0) {
                                    Cvar_Set("oquake_star_max_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_always_allow_pickup_if_max") == 0) {
                                    Cvar_Set("oquake_star_always_allow_pickup_if_max", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_always_add_items_to_inventory") == 0) {
                                    Cvar_Set("oquake_star_always_add_items_to_inventory", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_health_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_health_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_armor_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_armor_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_powerup_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_powerup_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_nft_provider") == 0) {
                                    Cvar_Set("oquake_star_nft_provider", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_send_to_address_after_minting") == 0) {
                                    Cvar_Set("oquake_star_send_to_address_after_minting", cvar_value);
                                }
                            }
                        }
                    }
                }
                fclose(f);
                config_loaded = 1;
                Con_Printf("OQuake: Loaded config from: %s\n", found_cfg_path);
                const char* star_url = oquake_star_api_url.string;
                const char* oasis_url = oquake_oasis_api_url.string;
                if (star_url && star_url[0]) {
                    Con_Printf("OQuake: STAR API URL: %s\n", star_url);
                }
                if (oasis_url && oasis_url[0]) {
                    Con_Printf("OQuake: OASIS API URL: %s\n", oasis_url);
                }
                /* Sync to JSON if it exists */
                if (found_json) {
                    OQ_SyncConfigFiles(found_cfg_path, found_json_path);
                }
            }
        } else if (found_json) {
            /* Fallback: use JSON if available */
            if (OQ_LoadJsonConfig(found_json_path)) {
                config_loaded = 1;
                Con_Printf("OQuake: Loaded config from (fallback): %s\n", found_json_path);
                const char* star_url = oquake_star_api_url.string;
                const char* oasis_url = oquake_oasis_api_url.string;
                if (star_url && star_url[0]) {
                    Con_Printf("OQuake: STAR API URL: %s\n", star_url);
                }
                if (oasis_url && oasis_url[0]) {
                    Con_Printf("OQuake: OASIS API URL: %s\n", oasis_url);
                }
            }
        } else if (found_cfg) {
            /* Fallback: use config.cfg if available */
            FILE *f = fopen(found_cfg_path, "r");
            if (f) {
                char line[256];
                while (fgets(line, sizeof(line), f)) {
                    char *p = line;
                    while (*p && (*p == ' ' || *p == '\t')) p++;
                    if (*p == '\n' || *p == '\r' || *p == 0) continue;
                    if (*p == '/' && p[1] == '/') continue;
                    if (*p == '#') continue;
                    
                    if (strncmp(p, "set ", 4) == 0) {
                        p += 4;
                        while (*p && (*p == ' ' || *p == '\t')) p++;
                        
                        char cvar_name[64] = {0};
                        char cvar_value[256] = {0};
                        int n = 0;
                        
                        while (*p && *p != ' ' && *p != '\t' && *p != '\n' && *p != '\r' && n < sizeof(cvar_name) - 1) {
                            cvar_name[n++] = *p++;
                        }
                        
                        if (n > 0) {
                            cvar_name[n] = 0;
                            while (*p && (*p == ' ' || *p == '\t')) p++;
                            
                            if (*p == '"') {
                                p++;
                                n = 0;
                                while (*p && *p != '"' && *p != '\n' && *p != '\r' && n < sizeof(cvar_value) - 1) {
                                    cvar_value[n++] = *p++;
                                }
                            } else {
                                n = 0;
                                while (*p && *p != ' ' && *p != '\t' && *p != '\n' && *p != '\r' && n < sizeof(cvar_value) - 1) {
                                    cvar_value[n++] = *p++;
                                }
                            }
                            
                            if (n > 0) {
                                cvar_value[n] = 0;
                                if (strcmp(cvar_name, "oquake_star_config_file") == 0) {
                                    Cvar_Set("oquake_star_config_file", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_api_url") == 0) {
                                    Cvar_Set("oquake_star_api_url", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_oasis_api_url") == 0) {
                                    Cvar_Set("oquake_oasis_api_url", cvar_value);
                                } else if (strcmp(cvar_name, "oasis_star_beam_face") == 0) {
                                    Cvar_SetValueQuick(&oasis_star_beam_face, atoi(cvar_value));
                                } else if (strcmp(cvar_name, "oquake_star_stack_armor") == 0) {
                                    Cvar_Set("oquake_star_stack_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_weapons") == 0) {
                                    Cvar_Set("oquake_star_stack_weapons", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_powerups") == 0) {
                                    Cvar_Set("oquake_star_stack_powerups", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_keys") == 0) {
                                    Cvar_Set("oquake_star_stack_keys", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_stack_sigils") == 0) {
                                    Cvar_Set("oquake_star_stack_sigils", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_weapons") == 0) {
                                    Cvar_Set("oquake_star_mint_weapons", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_armor") == 0) {
                                    Cvar_Set("oquake_star_mint_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_powerups") == 0) {
                                    Cvar_Set("oquake_star_mint_powerups", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_mint_keys") == 0) {
                                    Cvar_Set("oquake_star_mint_keys", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_max_health") == 0) {
                                    Cvar_Set("oquake_star_max_health", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_max_armor") == 0) {
                                    Cvar_Set("oquake_star_max_armor", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_always_allow_pickup_if_max") == 0) {
                                    Cvar_Set("oquake_star_always_allow_pickup_if_max", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_always_add_items_to_inventory") == 0) {
                                    Cvar_Set("oquake_star_always_add_items_to_inventory", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_health_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_health_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_armor_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_armor_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_use_powerup_on_pickup") == 0) {
                                    Cvar_Set("oquake_star_use_powerup_on_pickup", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_nft_provider") == 0) {
                                    Cvar_Set("oquake_star_nft_provider", cvar_value);
                                } else if (strcmp(cvar_name, "oquake_star_send_to_address_after_minting") == 0) {
                                    Cvar_Set("oquake_star_send_to_address_after_minting", cvar_value);
                                }
                            }
                        }
                    }
                }
                fclose(f);
                config_loaded = 1;
                Con_Printf("OQuake: Loaded config from (fallback): %s\n", found_cfg_path);
                const char* star_url = oquake_star_api_url.string;
                const char* oasis_url = oquake_oasis_api_url.string;
                if (star_url && star_url[0]) {
                    Con_Printf("OQuake: STAR API URL: %s\n", star_url);
                }
                if (oasis_url && oasis_url[0]) {
                    Con_Printf("OQuake: OASIS API URL: %s\n", oasis_url);
                }
                /* Create JSON file from config.cfg if JSON doesn't exist */
                if (!found_json && found_json_path[0]) {
                    if (OQ_SaveJsonConfig(found_json_path)) {
                        Con_Printf("OQuake: Created JSON config: %s\n", found_json_path);
                    }
                }
            }
        }
        
        if (!config_loaded) {
            Con_Printf("OQuake: Config file not found in any standard location\n");
            Con_Printf("OQuake: Tried: config.cfg and oasisstar.json\n");
            Con_Printf("OQuake: Set oquake_star_config_file to \"json\" or \"cfg\" to choose format\n");
            /* Create default JSON file if neither exists */
            char default_json[512];
#ifdef _WIN32
            char exe_path[MAX_PATH] = {0};
            char exe_dir[MAX_PATH] = {0};
            if (GetModuleFileNameA(NULL, exe_path, sizeof(exe_path))) {
                char *last_slash = strrchr(exe_path, '\\');
                if (last_slash) {
                    int dir_len = last_slash - exe_path;
                    if (dir_len < sizeof(exe_dir)) {
                        memcpy(exe_dir, exe_path, dir_len);
                        exe_dir[dir_len] = 0;
                        q_snprintf(default_json, sizeof(default_json), "%s\\oasisstar.json", exe_dir);
                    } else {
                        q_strlcpy(default_json, "oasisstar.json", sizeof(default_json));
                    }
                } else {
                    q_strlcpy(default_json, "oasisstar.json", sizeof(default_json));
                }
            } else {
                q_strlcpy(default_json, "oasisstar.json", sizeof(default_json));
            }
#else
            q_strlcpy(default_json, "oasisstar.json", sizeof(default_json));
#endif
            if (OQ_SaveJsonConfig(default_json)) {
                Con_Printf("OQuake: Created default JSON config: %s\n", default_json);
                if (OQ_LoadJsonConfig(default_json)) {
                    config_loaded = 1;
                    Con_Printf("OQuake: Loaded default config from: %s\n", default_json);
                }
            }
        }
        
        /* Store JSON path for delayed reload (after Quake's exec config.cfg runs) */
        if (found_json && found_json_path[0]) {
            q_strlcpy(g_json_config_path, found_json_path, sizeof(g_json_config_path));
            g_oq_reapply_json_frames = 70;  /* Re-apply JSON after ~2s so mint etc. override config.cfg */
        } else if (!found_json && found_json_path[0]) {
            /* JSON will be created, store the path */
            q_strlcpy(g_json_config_path, found_json_path, sizeof(g_json_config_path));
            g_oq_reapply_json_frames = 70;
        }
        
        /* Queue delayed reload of JSON after Quake's exec config.cfg completes */
        /* This ensures our values aren't overwritten by Quake's config */
        if (use_json && g_json_config_path[0]) {
            extern void Cbuf_AddText(const char *text);
            Cbuf_AddText("wait 0.5; oasis_reload_config\n");
        }
    }

    /* Load config: CVAR first, then env var, then default */
    const char* config_url = oquake_star_api_url.string;
    if (!config_url || !config_url[0]) {
        const char* env_url = getenv("STAR_API_URL");
        if (env_url && env_url[0]) {
            config_url = env_url;
        } else {
            config_url = "https://star-api.oasisplatform.world/api";
        }
    }
    g_star_config.base_url = config_url;
    
    /* API key: CVAR -> env var */
    const char* config_api_key = oquake_star_api_key.string;
    if (!config_api_key || !config_api_key[0]) {
        config_api_key = getenv("STAR_API_KEY");
    }
    g_star_config.api_key = config_api_key;
    
    /* Avatar ID: CVAR -> env var */
    const char* config_avatar_id = oquake_star_avatar_id.string;
    if (!config_avatar_id || !config_avatar_id[0]) {
        config_avatar_id = getenv("STAR_AVATAR_ID");
    }
    g_star_config.avatar_id = config_avatar_id;
    
    g_star_config.timeout_seconds = 30;

    result = star_api_init(&g_star_config);
    if (result != STAR_API_SUCCESS) {
        printf("OQuake STAR API: Failed to initialize: %s\n", star_api_get_last_error());
    } else {
        /* NFT minting and avatar auth use WEB4 OASIS API; set from oquake_oasis_api_url so mint goes to WEB4 not WEB5. */
        if (oquake_oasis_api_url.string && oquake_oasis_api_url.string[0]) {
            star_api_set_oasis_base_url(oquake_oasis_api_url.string);
        }
        /* Username: CVAR -> env var */
        username = oquake_star_username.string;
        if (!username || !username[0]) {
            username = getenv("STAR_USERNAME");
        }
        /* Password: CVAR -> env var */
        password = oquake_star_password.string;
        if (!password || !password[0]) {
            password = getenv("STAR_PASSWORD");
        }
        if (username && password) {
            result = star_api_authenticate(username, password);
            if (result == STAR_API_SUCCESS) {
                g_star_initialized = 1;
                printf("OQuake STAR API: Authenticated. Cross-game assets enabled.\n");
            } else {
                printf("OQuake STAR API: SSO failed: %s\n", star_api_get_last_error());
            }
        } else if (g_star_config.api_key && g_star_config.avatar_id) {
            g_star_initialized = 1;
            printf("OQuake STAR API: Using API key. Cross-game assets enabled.\n");
        } else {
            printf("OQuake STAR API: Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID for cross-game keys.\n");
        }
    }
    /* OASIS / OQuake loading splash - same professional style as ODOOM */
    Con_Printf("\n");
    Con_Printf("  ================================================\n");
    Con_Printf("            O A S I S   O Q U A K E  " OQUAKE_VERSION " (Build " OQUAKE_BUILD ")\n");
    Con_Printf("               By NextGen World Ltd\n");
    Con_Printf("  ================================================\n");
    Con_Printf("\n");
    Con_Printf("  " OQUAKE_VERSION_STR "\n");
    Con_Printf("  STAR API - Enabling full interoperable games across the OASIS Omniverse!\n");
    Con_Printf("  Type 'star' in console for STAR commands.\n");
    Con_Printf("\n");
    Con_Printf("  Welcome to OQuake!\n");
    Con_Printf("\n");
}

void OQuake_STAR_Cleanup(void) {
    OQ_SaveStarConfigToFiles(); /* persist any STAR option changes on exit */
    star_sync_cleanup();
    if (g_star_initialized) {
        star_api_cleanup();
        g_star_initialized = 0;
        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
        printf("OQuake STAR API: Cleaned up.\n");
    }
}

void OQuake_STAR_OnKeyPickup(const char* key_name) {
    if (!key_name || !g_star_initialized)
        return;
    const char* desc = get_key_description(key_name);
    if (OQ_StackKeys()) {
        const char* event_name = !strcmp(key_name, OQUAKE_ITEM_SILVER_KEY) ? "Silver Key" : "Gold Key";
        if (OQ_AddInventoryEvent(event_name, desc, "KeyItem")) {
            printf("OQuake STAR API: Queued %s for sync.\n", key_name);
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Collected: %s", key_name);
        }
    } else {
        const char* api_name = !strcmp(key_name, OQUAKE_ITEM_SILVER_KEY) ? "Silver Key" : "Gold Key";
        if (OQ_AddInventoryUnlockIfMissing(api_name, desc, "KeyItem")) {
            printf("OQuake STAR API: Queued %s for sync.\n", key_name);
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Collected: %s", key_name);
        }
    }
    /* Auto-complete matching quest objective (WEB5 STAR Quest API). */
    {
        static const char OQUAKE_DEFAULT_QUEST_ID[] = "cross_dimensional_keycard_hunt";
        if (strcmp(key_name, OQUAKE_ITEM_SILVER_KEY) == 0)
            star_api_complete_quest_objective(OQUAKE_DEFAULT_QUEST_ID, "quake_silver_key", "Quake");
        else if (strcmp(key_name, OQUAKE_ITEM_GOLD_KEY) == 0)
            star_api_complete_quest_objective(OQUAKE_DEFAULT_QUEST_ID, "quake_gold_key", "Quake");
    }
    OQ_StartInventorySyncIfNeeded();
}

static const oquake_monster_entry_t* OQ_FindMonsterByEngineName(const char* engine_name) {
    int i;
    if (!engine_name || !engine_name[0]) return NULL;
    for (i = 0; i < OQ_MONSTER_COUNT; i++) {
        if (OQUAKE_MONSTERS[i].engine_name && q_strcasecmp(OQUAKE_MONSTERS[i].engine_name, engine_name) == 0)
            return &OQUAKE_MONSTERS[i];
    }
    return NULL;
}

static int OQ_ShouldMintMonster(int monster_index) {
    if (monster_index < 0 || monster_index >= OQ_MONSTER_FLAGS_MAX) return 1;
    return g_oq_mint_monster_flags[monster_index] != 0;
}

void OQuake_STAR_OnMonsterKilled(const char* monster_name) {
    const oquake_monster_entry_t* e;
    int do_mint;
    const char* prov;
    int idx;
    char star_log_buf[256];
    if (!monster_name || !monster_name[0]) {
        Con_Printf("OQuake STAR: OnMonsterKilled called with empty name (hook may be mis-installed)\n");
        return;
    }
    if (!g_star_initialized) {
        Con_Printf("OQuake STAR: monster \"%s\" killed but not beamed in (no XP/mint)\n", monster_name);
        snprintf(star_log_buf, sizeof(star_log_buf), "OQUAKE: monster \"%s\" killed but not beamed in (no XP/mint)", monster_name);
        star_api_log_to_file(star_log_buf);
        return;
    }
    e = OQ_FindMonsterByEngineName(monster_name);
    if (!e) {
        Con_Printf("OQuake STAR: unknown monster \"%s\" (no XP/mint)\n", monster_name);
        snprintf(star_log_buf, sizeof(star_log_buf), "OQUAKE: unknown monster \"%s\" (no XP/mint)", monster_name);
        star_api_log_to_file(star_log_buf);
        return;
    }
    idx = (int)(e - OQUAKE_MONSTERS);
    do_mint = OQ_ShouldMintMonster(idx) ? 1 : 0;
    prov = oquake_star_nft_provider.string && oquake_star_nft_provider.string[0] ? oquake_star_nft_provider.string : "SolanaOASIS";
    Con_Printf("OQuake STAR: monster kill queued: %s (%d XP, mint=%d)\n", e->display_name, e->xp, do_mint);
    snprintf(star_log_buf, sizeof(star_log_buf), "OQUAKE: monster kill queued: %s (%d XP, mint=%d)", e->display_name, e->xp, do_mint);
    star_api_log_to_file(star_log_buf);
    star_api_queue_monster_kill(e->engine_name, e->display_name, e->xp, e->is_boss, do_mint, prov, "OQUAKE");
}

/* Hook: called from PF_Remove/PF_sv_makestatic (pr_cmds.c) as fallback. Primary path is SVC_KILLEDMONSTER in PF_sv_WriteByte. Dedupe same entity same frame. */
void OQuake_STAR_OnEntityFreed(void* ed) {
    const char* ed_classname;
    static void* s_last_counted_ed;
    static int s_last_counted_frame = -1;

    if (!ed)
        return;
    ed_classname = PR_GetString(((edict_t*)ed)->v.classname);
    if (!ed_classname || strncmp(ed_classname, "monster_", 8) != 0)
        return;

    if (cls.demoplayback)
        return;
    /* Dedupe: same entity can be seen from both PF_Remove and PF_sv_makestatic in same frame. */
    if (ed == s_last_counted_ed && host_framecount == s_last_counted_frame)
        return;
    s_last_counted_ed = ed;
    s_last_counted_frame = host_framecount;

    OQuake_STAR_OnMonsterKilled(ed_classname);
}

void OQuake_STAR_OnBossKilled(const char* boss_name) {
    /* Use same path as any monster: XP + optional mint + add to inventory (all async). */
    OQuake_STAR_OnMonsterKilled(boss_name);
}

void OQuake_STAR_OnItemsChangedEx(unsigned int old_items, unsigned int new_items, int in_real_game)
{
    unsigned int gained = new_items & ~old_items;
    int added = 0;

    if (!in_real_game)
        return;
    if (gained == 0)
        return;
    /* Only mint/add after user has beamed in and started a level; avoid minting shells/shotgun at startup. */
    if (!g_star_beamed_in)
        return;
    {
        extern server_t sv;
        extern client_static_t cls;
        if (!sv.active || cls.demoplayback)
            return;
    }

    if (gained & IT_SHOTGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Shotgun", "Shotgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Shotgun", "Shotgun discovered", "Weapon");
    if (gained & IT_SUPER_SHOTGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Super Shotgun", "Super Shotgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Super Shotgun", "Super Shotgun discovered", "Weapon");
    if (gained & IT_NAILGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Nailgun", "Nailgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Nailgun", "Nailgun discovered", "Weapon");
    if (gained & IT_SUPER_NAILGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Super Nailgun", "Super Nailgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Super Nailgun", "Super Nailgun discovered", "Weapon");
    if (gained & IT_GRENADE_LAUNCHER) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Grenade Launcher", "Grenade Launcher discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Grenade Launcher", "Grenade Launcher discovered", "Weapon");
    if (gained & IT_ROCKET_LAUNCHER) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Rocket Launcher", "Rocket Launcher discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Rocket Launcher", "Rocket Launcher discovered", "Weapon");
    if (gained & IT_LIGHTNING) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Lightning Gun", "Lightning Gun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Lightning Gun", "Lightning Gun discovered", "Weapon");
    if (gained & IT_SUPER_LIGHTNING) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Super Lightning", "Super Lightning discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Super Lightning", "Super Lightning discovered", "Weapon");

    /* Armor quantity is recorded from OnStatsChangedEx ("Armor pickup +100"); do not also add +1 here or we double-count. */
    if (gained & IT_ARMOR1) added += OQ_StackArmor() ? 0 : OQ_AddInventoryUnlockIfMissing("Green Armor", "Green Armor", "Armor");
    if (gained & IT_ARMOR2) added += OQ_StackArmor() ? 0 : OQ_AddInventoryUnlockIfMissing("Yellow Armor", "Yellow Armor", "Armor");
    if (gained & IT_ARMOR3) added += OQ_StackArmor() ? 0 : OQ_AddInventoryUnlockIfMissing("Red Armor", "Red Armor", "Armor");

    if (gained & IT_SUPERHEALTH) added += OQ_StackPowerups() ? OQ_AddInventoryEvent("Megahealth", "Megahealth pickup", "Powerup") : OQ_AddInventoryUnlockIfMissing("Megahealth", "Megahealth", "Powerup");
    if (gained & IT_INVISIBILITY) added += OQ_StackPowerups() ? OQ_AddInventoryEvent("Ring of Shadows", "Ring of Shadows pickup", "Powerup") : OQ_AddInventoryUnlockIfMissing("Ring of Shadows", "Ring of Shadows", "Powerup");
    if (gained & IT_INVULNERABILITY) added += OQ_StackPowerups() ? OQ_AddInventoryEvent("Pentagram of Protection", "Pentagram of Protection pickup", "Powerup") : OQ_AddInventoryUnlockIfMissing("Pentagram of Protection", "Pentagram of Protection", "Powerup");
    if (gained & IT_SUIT) added += OQ_StackPowerups() ? OQ_AddInventoryEvent("Biosuit", "Biosuit pickup", "Powerup") : OQ_AddInventoryUnlockIfMissing("Biosuit", "Biosuit", "Powerup");
    if (gained & IT_QUAD) added += OQ_StackPowerups() ? OQ_AddInventoryEvent("Quad Damage", "Quad Damage pickup", "Powerup") : OQ_AddInventoryUnlockIfMissing("Quad Damage", "Quad Damage", "Powerup");

    if (gained & IT_SIGIL1) added += OQ_StackSigils() ? OQ_AddInventoryEvent("Sigil Piece 1", "Sigil Piece 1 acquired", "Artifact") : OQ_AddInventoryUnlockIfMissing("Sigil Piece 1", "Sigil Piece 1 acquired", "Artifact");
    if (gained & IT_SIGIL2) added += OQ_StackSigils() ? OQ_AddInventoryEvent("Sigil Piece 2", "Sigil Piece 2 acquired", "Artifact") : OQ_AddInventoryUnlockIfMissing("Sigil Piece 2", "Sigil Piece 2 acquired", "Artifact");
    if (gained & IT_SIGIL3) added += OQ_StackSigils() ? OQ_AddInventoryEvent("Sigil Piece 3", "Sigil Piece 3 acquired", "Artifact") : OQ_AddInventoryUnlockIfMissing("Sigil Piece 3", "Sigil Piece 3 acquired", "Artifact");
    if (gained & IT_SIGIL4) added += OQ_StackSigils() ? OQ_AddInventoryEvent("Sigil Piece 4", "Sigil Piece 4 acquired", "Artifact") : OQ_AddInventoryUnlockIfMissing("Sigil Piece 4", "Sigil Piece 4 acquired", "Artifact");

    if (gained & IT_KEY1) OQuake_STAR_OnKeyPickup(OQUAKE_ITEM_SILVER_KEY);
    if (gained & IT_KEY2) OQuake_STAR_OnKeyPickup(OQUAKE_ITEM_GOLD_KEY);

    if (added > 0) {
        if (g_star_initialized)
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR updated: %d new pickup(s)", added);
        if (g_inventory_open)
            OQ_RefreshOverlayFromClient();
    }
}

void OQuake_STAR_OnItemsChanged(unsigned int old_items, unsigned int new_items) {
    OQuake_STAR_OnItemsChangedEx(old_items, new_items, 1);
}

void OQuake_STAR_OnStatsChangedEx(
    int old_shells, int new_shells, int old_nails, int new_nails,
    int old_rockets, int new_rockets, int old_cells, int new_cells,
    int old_health, int new_health, int old_armor, int new_armor, int in_real_game)
{
    int added = 0;
    char desc[96];

    if (!in_real_game)
        return;
    if (!g_star_beamed_in)
        return;
    {
        extern server_t sv;
        extern client_static_t cls;
        if (!sv.active || cls.demoplayback)
            return;
    }
    /* vkQuake (and many Quake engines) do NOT call the touch intercept for health/armor/ammo - they only update
     * client stats. So the only path that sees these pickups is this one: stats increase after CL_ReadFromServer.
     * Always add here when stats go up (restores original "used to work" behaviour; touch path often not called). */

    /* Ammo: stats path is the one that fires in vkQuake (no touch for ammo boxes). */
    if (new_shells > old_shells) {
        q_snprintf(desc, sizeof(desc), "Shells pickup +%d", new_shells - old_shells);
        added += OQ_AddInventoryEvent("Shells", desc, "Ammo");
        OQ_PickupLog("Stats: Shells +%d -> STAR", new_shells - old_shells);
    }
    if (new_nails > old_nails) {
        q_snprintf(desc, sizeof(desc), "Nails pickup +%d", new_nails - old_nails);
        added += OQ_AddInventoryEvent("Nails", desc, "Ammo");
        OQ_PickupLog("Stats: Nails +%d -> STAR", new_nails - old_nails);
    }
    if (new_rockets > old_rockets) {
        q_snprintf(desc, sizeof(desc), "Rockets pickup +%d", new_rockets - old_rockets);
        added += OQ_AddInventoryEvent("Rockets", desc, "Ammo");
        OQ_PickupLog("Stats: Rockets +%d -> STAR", new_rockets - old_rockets);
    }
    if (new_cells > old_cells) {
        q_snprintf(desc, sizeof(desc), "Cells pickup +%d", new_cells - old_cells);
        added += OQ_AddInventoryEvent("Cells", desc, "Ammo");
        OQ_PickupLog("Stats: Cells +%d -> STAR", new_cells - old_cells);
    }
    /* Armor: add 1 qty with description e.g. "Green Armor (+100)" so use-item applies correct amount. Skip if we just applied armor from overlay (would re-add and qty bounces). */
    if (new_armor > old_armor) {
        extern double realtime;
        if (realtime - g_oq_armor_applied_from_overlay_time >= 1.0) {
            int delta = new_armor - old_armor;
            const char* armor_name = (delta <= 100) ? "Green Armor" : (delta < 200) ? "Yellow Armor" : "Red Armor";
            q_snprintf(desc, sizeof(desc), "%s (+%d)", armor_name, delta);
            star_api_queue_add_item(armor_name, desc, "Quake", "Armor", NULL, 1, 1);
            added++;
            OQ_PickupLog("Stats: Armor +%d (%s) -> STAR", delta, armor_name);
        } else {
            OQ_PickupLog("Stats: Armor +%d skip (applied from overlay recently)", new_armor - old_armor);
        }
    }
    /* Health: add 1 qty with description e.g. "Health (+25)" or "Megahealth (+100)". Skip if we just applied health from overlay (would re-add and qty bounces). */
    if (new_health > old_health) {
        extern double realtime;
        if (realtime - g_oq_health_applied_from_overlay_time >= 1.0) {
            int delta = new_health - old_health;
            if (delta >= 100) {
                q_snprintf(desc, sizeof(desc), "Megahealth (+%d)", delta);
                star_api_queue_add_item("Megahealth", desc, "Quake", "Powerup", NULL, 1, 1);
                OQ_PickupLog("Stats: Megahealth +%d -> STAR", delta);
            } else {
                q_snprintf(desc, sizeof(desc), "Health (+%d)", delta);
                star_api_queue_add_item("Health", desc, "Quake", "Health", NULL, 1, 1);
                OQ_PickupLog("Stats: Health +%d -> STAR", delta);
            }
            added++;
        } else {
            OQ_PickupLog("Stats: Health +%d skip (applied from overlay recently)", new_health - old_health);
        }
    }

    if (added > 0) {
        if (g_star_initialized)
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR updated: %d pickup(s) queued", added);
        if (g_inventory_open)
            OQ_RefreshOverlayFromClient();
    }
}

void OQuake_STAR_OnStatsChanged(
    int old_shells, int new_shells,
    int old_nails, int new_nails,
    int old_rockets, int new_rockets,
    int old_cells, int new_cells,
    int old_health, int new_health,
    int old_armor, int new_armor)
{
    OQuake_STAR_OnStatsChangedEx(old_shells, new_shells, old_nails, new_nails,
        old_rockets, new_rockets, old_cells, new_cells,
        old_health, new_health, old_armor, new_armor, 1);
}

/** Case-insensitive string equality (for classnames; some engines/mods use different casing). */
static int OQ_StrEqNoCase(const char* a, const char* b) {
    if (!a || !b) return (a == b);
    while (*a && *b) {
        if (tolower((unsigned char)*a) != tolower((unsigned char)*b)) return 0;
        a++; b++;
    }
    return (*a == *b);
}

/** Case-insensitive prefix check (e.g. "item_", "weapon_"). */
static int OQ_StrStartsWithNoCase(const char* str, const char* prefix) {
    if (!str || !prefix) return 0;
    while (*prefix) {
        if (tolower((unsigned char)*str) != tolower((unsigned char)*prefix)) return 0;
        str++; prefix++;
    }
    return 1;
}

/** Log pickup intercept to console and STAR log file when star debug is on. */
static void OQ_PickupLog(const char* fmt, ...) {
    char buf[384];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    if (g_star_debug_logging) {
        Con_Printf("[OQuake pickup] %s\n", buf);
        star_api_log_to_file(buf);
    }
}

/** Log to console and star_api.log only when star debug is on. Use for use-item, C/F keys, config, and general STAR flow tracking. */
static void OQ_StarDebugLog(const char* fmt, ...) {
    char buf[512];
    va_list ap;
    if (!g_star_debug_logging) return;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    Con_Printf("[STAR debug] %s\n", buf);
    star_api_log_to_file(buf);
}

/**
 * Call from engine before invoking the touch function for (item_ent, player_edict).
 * Same logic as original working OQuake: exact strcmp on classname (id1 progs use item_health, item_armor1, item_armor2, item_armorInv).
 * - always_add_items_to_inventory=1: always add health/armor to STAR and return 0 (engine runs too; player gets both).
 * - When player at max: always_allow_pickup_if_max=1 → add to STAR and return 1. always_allow_pickup_if_max=0 → return 0 (item stays on floor).
 * - When not at max: return 0 (engine applies, do not add to STAR).
 */
int OQuake_STAR_InterceptTouchPickupAtMax(void* item_edict, void* player_edict) {
    const char* classname;
    int player_health, player_armor;
    int max_h = 100, max_a = 100;
    int always_add = 0;
    int allow_pickup_if_max = 1;
    edict_t* e1 = (edict_t*)item_edict;
    edict_t* e2 = (edict_t*)player_edict;
    edict_t* item;
    edict_t* player;
    int first_edict_is_item;  /* 1 if e1 is the pickup (caller will ED_Free(e1)); 0 if e1 is player - never return 1. */
    const char* e1_cn;
    char log_buf[384];

    /* Engine calls us for both touches: (e2,e1) when item's touch runs, (e1,e2) when player's. First arg may be player. */
    if (!e1 || !e2) {
        OQ_PickupLog("InterceptTouch: e1=%p e2=%p (null check)", (void*)e1, (void*)e2);
        return 0;
    }
    /* Always log when touch involves health pickup so we can see if intercept is called at 100% health. */
    {
        const char* c1 = PR_GetString(e1->v.classname);
        const char* c2 = PR_GetString(e2->v.classname);
        if ((c1 && (OQ_StrEqNoCase(c1, "item_health") || OQ_StrEqNoCase(c1, "item_health_mega") || OQ_StrEqNoCase(c1, "item_health_super"))) ||
            (c2 && (OQ_StrEqNoCase(c2, "item_health") || OQ_StrEqNoCase(c2, "item_health_mega") || OQ_StrEqNoCase(c2, "item_health_super")))) {
            Con_Printf("[OQuake STAR] InterceptTouch HEALTH touch: e1=%s e2=%s\n", c1 ? c1 : "?", c2 ? c2 : "?");
        }
    }
    if (!g_star_initialized || !g_star_beamed_in) {
        OQ_PickupLog("InterceptTouch: skip (init=%d beamed=%d)", g_star_initialized, g_star_beamed_in);
        return 0;
    }
    {
        extern server_t sv;
        extern client_static_t cls;
        if (!sv.active || cls.demoplayback) {
            OQ_PickupLog("InterceptTouch: skip (sv.active=%d demoplayback=%d)", sv.active ? 1 : 0, cls.demoplayback ? 1 : 0);
            return 0;
        }
    }
    e1_cn = PR_GetString(e1->v.classname);
    classname = PR_GetString(e2->v.classname);
    if (e1_cn && strcmp(e1_cn, "player") == 0) {
        item = e2;
        player = e1;
        first_edict_is_item = 0;  /* e1=player: never return 1 or engine would ED_Free(player). */
        classname = PR_GetString(item->v.classname);
        OQ_PickupLog("InterceptTouch: e1=player e2=%s first_edict_is_item=0", classname ? classname : "(null)");
    } else {
        item = e1;
        player = e2;
        first_edict_is_item = 1;
        classname = PR_GetString(item->v.classname);
        OQ_PickupLog("InterceptTouch: e1=%s e2=other first_edict_is_item=1", e1_cn ? e1_cn : "(null)");
    }
    if (!classname || !classname[0]) {
        OQ_PickupLog("InterceptTouch: item classname empty");
        return 0;
    }
    /* Always log when we see a health pickup touch so we can confirm intercept is called at 100% health. */
    if (OQ_StrEqNoCase(classname, "item_health")) {
        Con_Printf("[OQuake STAR] Touch: item_health, player_health=%d max_h=%d\n", (int)player->v.health, max_h);
    }
    if (oquake_star_always_add_items_to_inventory.string && atoi(oquake_star_always_add_items_to_inventory.string))
        always_add = 1;
    if (oquake_star_always_allow_pickup_if_max.string && atoi(oquake_star_always_allow_pickup_if_max.string))
        allow_pickup_if_max = 1;
    else
        allow_pickup_if_max = 0;
    if (oquake_star_max_health.string && atoi(oquake_star_max_health.string) > 0)
        max_h = atoi(oquake_star_max_health.string);
    if (oquake_star_max_armor.string && atoi(oquake_star_max_armor.string) > 0)
        max_a = atoi(oquake_star_max_armor.string);
    player_health = (int)player->v.health;
    player_armor = (int)player->v.armorvalue;
    /* Process both call orders: when first_edict_is_item==0 the engine called (player, item) and will ED_Free(e2)=item on return 1; when 1, (item, player) and ED_Free(e1)=item. So we can add and return 1 in both cases. */
    /* Return 1 = free e1 (item), 2 = free e2 (item when e1=player). So when first_edict_is_item we return 1; when e1=player we return 2 so engine frees e2. */
    #define OQ_INTERCEPT_RET(first_edict_is_item) ((first_edict_is_item) ? 1 : 2)
    q_snprintf(log_buf, sizeof(log_buf), "InterceptTouch: class=%s health=%d max_h=%d armor=%d max_a=%d always_add=%d allow_ifmax=%d first_item=%d",
               classname, player_health, max_h, player_armor, max_a, always_add, allow_pickup_if_max, first_edict_is_item);
    OQ_PickupLog("%s", log_buf);
    /* Run intercept logic for both orderings. When e1=player (first_edict_is_item=0) return 2 so engine frees e2=item and does not run item touch - detection "earlier" so at-max health is handled even when engine only calls (player,item). */
    /* Health: item_health (25), item_health_mega / item_health_super (100). use_health_on_pickup: 0=below max->inventory only; 1=standard. */
    if (OQ_StrEqNoCase(classname, "item_health")) {
        int use_health = (oquake_star_use_health_on_pickup.string && atoi(oquake_star_use_health_on_pickup.string)) ? 1 : 0;
        OQ_StarDebugLog("InterceptTouch: item_health use_health=%d", use_health);
        if (always_add) {
            OQuake_STAR_OnPickupLeftOnFloor("Health", "Health", 1, "Health (+25)");
            if (player_health >= max_h) { OQ_StarDebugLog("InterceptTouch: item_health always_add at_max -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item)); return OQ_INTERCEPT_RET(first_edict_is_item); }
            OQ_StarDebugLog("InterceptTouch: item_health always_add below_max use_health=%d -> ret=%d", use_health, use_health ? 0 : OQ_INTERCEPT_RET(first_edict_is_item));
            return use_health ? 0 : OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (player_health >= max_h) {
            if (!allow_pickup_if_max) { OQ_StarDebugLog("InterceptTouch: item_health at_max !allow -> ret=0"); return 0; }
            OQuake_STAR_OnPickupLeftOnFloor("Health", "Health", 1, "Health (+25)");
            OQ_StarDebugLog("InterceptTouch: item_health at_max allow -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item));
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        /* Below max: use_health 0 -> inventory only (intercept); 1 -> let engine use */
        if (!use_health) {
            OQuake_STAR_OnPickupLeftOnFloor("Health", "Health", 1, "Health (+25)");
            OQ_StarDebugLog("InterceptTouch: item_health below_max !use_health -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item));
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        OQ_StarDebugLog("InterceptTouch: item_health below_max use_health -> ret=0");
        return 0;
    }
    if (OQ_StrEqNoCase(classname, "item_health_mega") || OQ_StrEqNoCase(classname, "item_health_super")) {
        int use_powerup = (oquake_star_use_powerup_on_pickup.string && atoi(oquake_star_use_powerup_on_pickup.string)) ? 1 : 0;
        OQ_StarDebugLog("InterceptTouch: megahealth use_powerup=%d", use_powerup);
        if (always_add) {
            OQuake_STAR_OnPickupLeftOnFloor("Megahealth", "Powerup", 1, "Megahealth (+100)");
            if (player_health >= max_h) { OQ_StarDebugLog("InterceptTouch: megahealth always_add at_max -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item)); return OQ_INTERCEPT_RET(first_edict_is_item); }
            OQ_StarDebugLog("InterceptTouch: megahealth always_add below_max -> ret=%d", use_powerup ? 0 : OQ_INTERCEPT_RET(first_edict_is_item));
            return use_powerup ? 0 : OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (player_health >= max_h) {
            if (!allow_pickup_if_max) { OQ_StarDebugLog("InterceptTouch: megahealth at_max !allow -> ret=0"); return 0; }
            OQuake_STAR_OnPickupLeftOnFloor("Megahealth", "Powerup", 1, "Megahealth (+100)");
            OQ_StarDebugLog("InterceptTouch: megahealth at_max allow -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item));
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (!use_powerup) {
            OQuake_STAR_OnPickupLeftOnFloor("Megahealth", "Powerup", 1, "Megahealth (+100)");
            OQ_StarDebugLog("InterceptTouch: megahealth below_max !use_powerup -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item));
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        OQ_StarDebugLog("InterceptTouch: megahealth below_max use_powerup -> ret=0");
        return 0;
    }
    /* Armor: item_armor1 (green +100), item_armor2 (yellow +150), item_armorInv (red +200). use_armor_on_pickup=0 -> inventory only, do not let engine apply. */
    if (OQ_StrEqNoCase(classname, "item_armor1")) {
        int use_armor = (oquake_star_use_armor_on_pickup.string && atoi(oquake_star_use_armor_on_pickup.string)) ? 1 : 0;
        OQ_StarDebugLog("InterceptTouch: item_armor1 use_armor=%d", use_armor);
        if (always_add) {
            OQuake_STAR_OnPickupLeftOnFloor("Green Armor", "Armor", 1, "Green Armor (+100)");
            if (player_armor >= max_a) { OQ_StarDebugLog("InterceptTouch: item_armor1 always_add at_max -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item)); return OQ_INTERCEPT_RET(first_edict_is_item); }
            OQ_StarDebugLog("InterceptTouch: item_armor1 always_add below_max -> ret=%d", use_armor ? 0 : OQ_INTERCEPT_RET(first_edict_is_item));
            return use_armor ? 0 : OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (player_armor >= max_a) {
            if (!allow_pickup_if_max) { OQ_StarDebugLog("InterceptTouch: item_armor1 at_max !allow -> ret=0"); return 0; }
            OQuake_STAR_OnPickupLeftOnFloor("Green Armor", "Armor", 1, "Green Armor (+100)");
            OQ_StarDebugLog("InterceptTouch: item_armor1 at_max allow -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item));
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (!use_armor) {
            OQuake_STAR_OnPickupLeftOnFloor("Green Armor", "Armor", 1, "Green Armor (+100)");
            OQ_StarDebugLog("InterceptTouch: item_armor1 below_max !use_armor -> ret=%d (inventory only)", OQ_INTERCEPT_RET(first_edict_is_item));
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        OQ_StarDebugLog("InterceptTouch: item_armor1 below_max use_armor -> ret=0");
        return 0;
    }
    if (OQ_StrEqNoCase(classname, "item_armor2")) {
        int use_armor = (oquake_star_use_armor_on_pickup.string && atoi(oquake_star_use_armor_on_pickup.string)) ? 1 : 0;
        OQ_StarDebugLog("InterceptTouch: item_armor2 use_armor=%d", use_armor);
        if (always_add) {
            OQuake_STAR_OnPickupLeftOnFloor("Yellow Armor", "Armor", 1, "Yellow Armor (+150)");
            if (player_armor >= max_a) { OQ_StarDebugLog("InterceptTouch: item_armor2 always_add at_max -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item)); return OQ_INTERCEPT_RET(first_edict_is_item); }
            return use_armor ? 0 : OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (player_armor >= max_a) {
            if (!allow_pickup_if_max) { OQ_StarDebugLog("InterceptTouch: item_armor2 at_max !allow -> ret=0"); return 0; }
            OQuake_STAR_OnPickupLeftOnFloor("Yellow Armor", "Armor", 1, "Yellow Armor (+150)");
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (!use_armor) {
            OQuake_STAR_OnPickupLeftOnFloor("Yellow Armor", "Armor", 1, "Yellow Armor (+150)");
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        return 0;
    }
    if (OQ_StrEqNoCase(classname, "item_armorInv") || OQ_StrEqNoCase(classname, "item_armor_inv")) {
        int use_armor = (oquake_star_use_armor_on_pickup.string && atoi(oquake_star_use_armor_on_pickup.string)) ? 1 : 0;
        OQ_StarDebugLog("InterceptTouch: item_armorInv use_armor=%d", use_armor);
        if (always_add) {
            OQuake_STAR_OnPickupLeftOnFloor("Red Armor", "Armor", 1, "Red Armor (+200)");
            if (player_armor >= max_a) { OQ_StarDebugLog("InterceptTouch: item_armorInv always_add at_max -> ret=%d", OQ_INTERCEPT_RET(first_edict_is_item)); return OQ_INTERCEPT_RET(first_edict_is_item); }
            return use_armor ? 0 : OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (player_armor >= max_a) {
            if (!allow_pickup_if_max) { OQ_StarDebugLog("InterceptTouch: item_armorInv at_max !allow -> ret=0"); return 0; }
            OQuake_STAR_OnPickupLeftOnFloor("Red Armor", "Armor", 1, "Red Armor (+200)");
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        if (!use_armor) {
            OQuake_STAR_OnPickupLeftOnFloor("Red Armor", "Armor", 1, "Red Armor (+200)");
            return OQ_INTERCEPT_RET(first_edict_is_item);
        }
        return 0;
    }
    /* Ammo: when always_add=1 add to STAR (engine also gives ammo to player). id1 classnames: item_shells, item_spikes, item_rockets, item_cells. */
    if (always_add) {
        if (OQ_StrEqNoCase(classname, "item_shells")) {
            OQuake_STAR_OnPickupLeftOnFloor("Shells", "Ammo", 1, NULL);
            return 0;
        }
        if (OQ_StrEqNoCase(classname, "item_spikes")) {
            OQuake_STAR_OnPickupLeftOnFloor("Nails", "Ammo", 1, NULL);
            return 0;
        }
        if (OQ_StrEqNoCase(classname, "item_rockets")) {
            OQuake_STAR_OnPickupLeftOnFloor("Rockets", "Ammo", 1, NULL);
            return 0;
        }
        if (OQ_StrEqNoCase(classname, "item_cells")) {
            OQuake_STAR_OnPickupLeftOnFloor("Cells", "Ammo", 1, NULL);
            return 0;
        }
        /* Fallback: any other item_* or weapon_* from mods gets added when always_add=1 */
        if (OQ_StrStartsWithNoCase(classname, "item_")) {
            OQuake_STAR_OnPickupLeftOnFloor(classname, "Item", 1, NULL);
            return 0;
        }
        if (OQ_StrStartsWithNoCase(classname, "weapon_")) {
            OQuake_STAR_OnPickupLeftOnFloor(classname, "Weapon", 1, NULL);
            return 0;
        }
    }
    #undef OQ_INTERCEPT_RET
    OQ_PickupLog("InterceptTouch: no match for classname=%s -> ret=0", classname);
    return 0;
}

/** Same as ODOOM: add to STAR only when the engine would leave the item on the floor. optional_description: if non-NULL, stored as item description (e.g. "Health (+25)"); else default "Pickup (engine left on floor) +N". */
void OQuake_STAR_OnPickupLeftOnFloor(const char* item_name, const char* item_type, int quantity, const char* optional_description) {
    char desc[96];
    char log_msg[256];
    int qty = (quantity > 0) ? quantity : 1;
    if (!item_name || !item_name[0] || !g_star_initialized || !g_star_beamed_in)
        return;
    {
        extern server_t sv;
        extern client_static_t cls;
        if (!sv.active || cls.demoplayback)
            return;
    }
    q_snprintf(log_msg, sizeof(log_msg), "OQUAKE: OnPickupLeftOnFloor called: name=%s type=%s qty=%d", item_name ? item_name : "(null)", item_type ? item_type : "(null)", qty);
    OQ_StarDebugLog("%s", log_msg);
    if (optional_description && optional_description[0])
        q_strlcpy(desc, optional_description, sizeof(desc));
    else
        q_snprintf(desc, sizeof(desc), "Pickup (engine left on floor) +%d", qty);
    if (OQ_DoMintForItemType(item_type ? item_type : "Item"))
        star_api_queue_pickup_with_mint(item_name, desc, "Quake", item_type ? item_type : "Item", 1, oquake_star_nft_provider.string, oquake_star_send_to_address_after_minting.string, qty);
    else
        star_api_queue_add_item(item_name, desc, "Quake", item_type ? item_type : "Item", NULL, qty, 1);
    q_strlcpy(g_star_last_pickup_name, item_name, sizeof(g_star_last_pickup_name));
    q_strlcpy(g_star_last_pickup_desc, desc, sizeof(g_star_last_pickup_desc));
    q_strlcpy(g_star_last_pickup_type, item_type ? item_type : "Item", sizeof(g_star_last_pickup_type));
    g_star_has_last_pickup = true;
    if (g_inventory_open)
        OQ_RefreshOverlayFromClient();
}

/* Frame-based item/stats poll so pickups are reported even when sbar isn't drawn. Call from Host_Frame. */
void OQuake_STAR_PollItems(void) {
    extern client_state_t cl;
    extern client_static_t cls;
    extern server_t sv;
    static unsigned int poll_prev_items = 0;
    static int poll_prev_shells = -1, poll_prev_nails = -1, poll_prev_rockets = -1, poll_prev_cells = -1;
    static int poll_prev_health = -1, poll_prev_armor = -1;
    static int poll_prev_valid = 0;

    /* Run async completions (auth, inventory, use_item) every frame so e.g. "star beamin" finishes even when console is open. */
    star_sync_pump();

    /* Show mint result in console when background pickup-with-mint completes (NFT ID + Hash). */
    {
        char item_buf[256] = {0}, nft_buf[128] = {0}, hash_buf[256] = {0};
        if (star_api_consume_last_mint_result(item_buf, sizeof(item_buf), nft_buf, sizeof(nft_buf), hash_buf, sizeof(hash_buf)))
            Con_Printf("NFT minted: %s | ID: %s | Hash: %s\n", item_buf, nft_buf, hash_buf[0] ? hash_buf : "(none)");
    }
    /* Show any background errors (mint/add_item failure or pickup not queued) in console. */
    {
        char err_buf[512] = {0};
        if (star_api_consume_last_background_error(err_buf, sizeof(err_buf)))
            Con_Printf("%s\n", err_buf);
    }
    /* Show STAR log messages in console when star debug is on (quests, XP refresh, monster kill, etc.). */
    {
        char log_buf[1024] = {0};
        if (g_star_debug_logging) {
            int i;
            for (i = 0; i < 25; i++) {
                if (!star_api_consume_console_log(log_buf, sizeof(log_buf)))
                    break;
                Con_Printf("[STAR] %s\n", log_buf);
            }
        } else {
            while (star_api_consume_console_log(log_buf, sizeof(log_buf))) {}
        }
    }

    /* Re-apply oasisstar.json after a short delay so mint/stack from JSON override any config.cfg load. */
    if (g_oq_reapply_json_frames == 0) {
        g_oq_reapply_json_frames = -1;
        if (g_json_config_path[0] && OQ_LoadJsonConfig(g_json_config_path)) {
            const char* config_url = oquake_star_api_url.string;
            if (config_url && config_url[0])
                g_star_config.base_url = config_url;
        }
    } else if (g_oq_reapply_json_frames > 0) {
        g_oq_reapply_json_frames--;
    }

    if (g_oq_toast_frames > 0)
        g_oq_toast_frames--;

    /* XP refresh is done once in auth-done or API-key path; no delayed second call. */

    if (!sv.active || cls.demoplayback) {
        poll_prev_items = (unsigned int)cl.items;
        poll_prev_shells = cl.stats[STAT_SHELLS];
        poll_prev_nails = cl.stats[STAT_NAILS];
        poll_prev_rockets = cl.stats[STAT_ROCKETS];
        poll_prev_cells = cl.stats[STAT_CELLS];
        poll_prev_health = cl.stats[STAT_HEALTH];
        poll_prev_armor = cl.stats[STAT_ARMOR];
        poll_prev_valid = 1;
        return;
    }
    OQuake_STAR_OnItemsChangedEx(poll_prev_items, (unsigned int)cl.items, 1);
    poll_prev_items = (unsigned int)cl.items;
    if (poll_prev_valid) {
        OQuake_STAR_OnStatsChangedEx(
            poll_prev_shells, cl.stats[STAT_SHELLS],
            poll_prev_nails, cl.stats[STAT_NAILS],
            poll_prev_rockets, cl.stats[STAT_ROCKETS],
            poll_prev_cells, cl.stats[STAT_CELLS],
            poll_prev_health, cl.stats[STAT_HEALTH],
            poll_prev_armor, cl.stats[STAT_ARMOR], 1);
    }
    poll_prev_shells = cl.stats[STAT_SHELLS];
    poll_prev_nails = cl.stats[STAT_NAILS];
    poll_prev_rockets = cl.stats[STAT_ROCKETS];
    poll_prev_cells = cl.stats[STAT_CELLS];
    poll_prev_health = cl.stats[STAT_HEALTH];
    poll_prev_armor = cl.stats[STAT_ARMOR];
    poll_prev_valid = 1;
}

/** Called from main thread by star_sync_pump() when use-item (door key) completes. */
static void OQ_OnUseItemDone(void* user_data) {
    int success = 0;
    char err_buf[384] = {0};
    (void)user_data;
    if (!star_sync_use_item_get_result(&success, err_buf, sizeof(err_buf)))
        return;
    if (success)
        OQ_RefreshOverlayFromClient();
}

/** 1 if item name (lowercase) contains substring. */
static int OQ_ItemNameContains(const char* item_name, const char* sub) {
    char lower[256];
    size_t i, len;
    if (!item_name || !sub || !sub[0]) return 0;
    len = strlen(item_name);
    if (len >= sizeof(lower)) len = sizeof(lower) - 1;
    for (i = 0; i < len; i++)
        lower[i] = (char)tolower((unsigned char)item_name[i]);
    lower[len] = '\0';
    return strstr(lower, sub) ? 1 : 0;
}

/** Silver doors: only silver_key. Gold doors: only gold_key. No Doom keycards. */
static int OQ_FindKeyForDoor(const char* required_key_name, char* out_name, size_t out_size) {
    star_item_list_t* list = NULL;
    size_t i;
    if (!required_key_name || !out_name || out_size < 2) return 0;
    out_name[0] = '\0';
    if (star_api_get_inventory(&list) != STAR_API_SUCCESS || !list || !list->items)
        return 0;
    for (i = 0; i < list->count; i++) {
        const char* n = list->items[i].name;
        if (!n || !n[0]) continue;
        if (OQ_ItemNameContains(required_key_name, "silver") && q_strcasecmp(n, OQUAKE_ITEM_SILVER_KEY) == 0) {
            q_strlcpy(out_name, n, out_size);
            star_api_free_item_list(list);
            return 1;
        }
        if (OQ_ItemNameContains(required_key_name, "gold") && q_strcasecmp(n, OQUAKE_ITEM_GOLD_KEY) == 0) {
            q_strlcpy(out_name, n, out_size);
            star_api_free_item_list(list);
            return 1;
        }
    }
    star_api_free_item_list(list);
    return 0;
}

/** Door access: only open and consume when we have a key that matches this door. Uses actual item name from inventory for consumption (like ODOOM). QuakeC should call only when player presses use on the door, not on touch. */
int OQuake_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name) {
    char actual_name[256];
    if (!g_star_initialized || !required_key_name)
        return 0;
    if (!OQ_FindKeyForDoor(required_key_name, actual_name, sizeof(actual_name)))
        return 0;
    star_sync_use_item_start(actual_name, door_targetname && door_targetname[0] ? door_targetname : "quake_door", OQ_OnUseItemDone, NULL);
    return 1;
}

/*-----------------------------------------------------------------------------
 * Debug mode toggle command (debugmode on/off)
 *-----------------------------------------------------------------------------*/
/*
static void OQ_DebugMode_f(void) {
    extern cvar_t developer;
    int argc = Cmd_Argc();
    
    if (argc < 2) {
        Con_Printf("Debug mode is currently %s\n", developer.value > 0.5f ? "ON" : "OFF");
        Con_Printf("Usage: debugmode <on|off>\n");
        return;
    }
    
    const char* arg = Cmd_Argv(1);
    if (strcmp(arg, "on") == 0 || strcmp(arg, "1") == 0) {
        Cvar_SetValueQuick(&developer, 1);
        Con_Printf("Debug mode enabled (developer messages will be shown)\n");
    } else if (strcmp(arg, "off") == 0 || strcmp(arg, "0") == 0) {
        Cvar_SetValueQuick(&developer, 0);
        Con_Printf("Debug mode disabled (developer messages hidden)\n");
    } else {
        Con_Printf("Usage: debugmode <on|off>\n");
    }
}
*/

/*-----------------------------------------------------------------------------
 * STAR console command (star <subcmd> [args...]) - same style as ODOOM
 *-----------------------------------------------------------------------------*/
void OQuake_STAR_Console_f(void) {
    int argc = Cmd_Argc();
    if (argc < 2) {
        Con_Printf("\n");
        Con_Printf("STAR API console commands (OQuake):\n");
        Con_Printf("\n");
        Con_Printf("  star version        - Show integration and API status\n");
        Con_Printf("  star status         - Show init state and last error\n");
        Con_Printf("  star inventory      - List items in STAR inventory\n");
        Con_Printf("  star lastpickup     - Show most recent synced pickup\n");
        Con_Printf("  star has <item>     - Check if you have an item (e.g. silver_key)\n");
        Con_Printf("  star add <item> [desc] [type] - Add item (dellams/anorak only)\n");
        Con_Printf("  star use <item> [context]     - Use item\n");
        Con_Printf("  star quest start|objective|complete ... - Quest progress\n");
        Con_Printf("  star bossnft <name> [desc]    - Create boss NFT (dellams/anorak only)\n");
        Con_Printf("  star deploynft <nft_id> <game> [loc] - Deploy boss NFT\n");
        Con_Printf("  star pickup ifmax <0|1> - At max: 1=pick up into STAR, 0=original Quake (leave on floor)\n");
        Con_Printf("  star pickup all <0|1> - 1=always add to STAR even when engine uses it, 0=only when at max\n");
        Con_Printf("  star pickup keycard <silver|gold> - Add key to STAR inventory (admin only)\n");
        Con_Printf("  star debug on|off|status - Toggle STAR debug logging\n");
        Con_Printf("  star send_avatar <user> <item_class> - Send item to avatar\n");
        Con_Printf("  star send_clan <clan> <item_class>   - Send item to clan\n");
        Con_Printf("  star beamin <username> <password> - Log in inside Quake\n");
        Con_Printf("  star beamed in <username> <password> - Alias for beamin\n");
        Con_Printf("  star beamin   - Log in using STAR_USERNAME/STAR_PASSWORD or API key\n");
        Con_Printf("  star beamout  - Log out / disconnect from STAR\n");
        Con_Printf("  star face on|off|status - Toggle beam-in face switch\n");
        Con_Printf("  star config        - Show current config (URLs, stack, mint options)\n");
        Con_Printf("  starconfig / star_config - Same as 'star config' (use if 'star config' is unrecognised)\n");
        Con_Printf("  star config save   - Write config to files now (also saved on exit)\n");
        Con_Printf("  star stack <armor|weapons|powerups|keys|sigils> <0|1> - Stack (1) or unlock (0)\n");
        Con_Printf("  star mint <armor|weapons|powerups|keys> <0|1> - Mint NFT when collecting (1=on, 0=off)\n");
        Con_Printf("  star mint monster <name> <0|1> - Mint NFT when killing monster (e.g. oquake_ogre)\n");
        Con_Printf("  star nftprovider <name> - Set NFT mint provider (e.g. SolanaOASIS)\n");
        Con_Printf("  star seturl <url>       - Set STAR API URL (saved to config)\n");
        Con_Printf("  star setoasisurl <url>  - Set OASIS API URL (saved to config)\n");
        Con_Printf("  star configfile json|cfg - Prefer oasisstar.json or config.cfg\n");
        Con_Printf("  star reloadconfig  - Reload from oasisstar.json\n");
        Con_Printf("\n");
        return;
    }
    const char* sub = Cmd_Argv(1);
    if (!sub) {
        Con_Printf("Error: No subcommand provided.\n");
        return;
    }
    if (strcmp(sub, "pickup") == 0) {
        if (argc >= 4 && strcmp(Cmd_Argv(2), "ifmax") == 0) {
            int on = (Cmd_Argv(3)[0] == '1' && Cmd_Argv(3)[1] == '\0') ? 1 : 0;
            Cvar_Set("oquake_star_always_allow_pickup_if_max", on ? "1" : "0");
            OQ_SaveStarConfigToFiles();
            Con_Printf("Pick up when at max (always_allow_pickup_if_max) set to %s. Config saved.\n", on ? "1" : "0");
            return;
        }
        if (argc >= 4 && strcmp(Cmd_Argv(2), "all") == 0) {
            int on = (Cmd_Argv(3)[0] == '1' && Cmd_Argv(3)[1] == '\0') ? 1 : 0;
            Cvar_Set("oquake_star_always_add_items_to_inventory", on ? "1" : "0");
            OQ_SaveStarConfigToFiles();
            Con_Printf("Always add to STAR (always_add_items_to_inventory) set to %s. Config saved.\n", on ? "1" : "0");
            return;
        }
        if (argc < 4 || strcmp(Cmd_Argv(2), "keycard") != 0) {
            Con_Printf("Usage: star pickup ifmax <0|1> - At max: 1=pick up into STAR, 0=original Quake (leave on floor)\n");
            Con_Printf("       star pickup all <0|1> - 1=always add to STAR even when engine uses it, 0=only when at max\n");
            Con_Printf("       star pickup keycard <silver|gold> - Add key to STAR inventory (admin only)\n");
            return;
        }
        if (!OQ_AllowPrivilegedCommands()) { Con_Printf("Only dellams or anorak can use star pickup keycard.\n"); return; }
        const char* color = Cmd_Argv(3);
        const char* name = NULL;
        const char* desc = NULL;
        if (strcmp(color, "silver") == 0) { name = OQUAKE_ITEM_SILVER_KEY; desc = get_key_description(name); }
        else if (strcmp(color, "gold") == 0) { name = OQUAKE_ITEM_GOLD_KEY; desc = get_key_description(name); }
        else { Con_Printf("Unknown keycard: %s. Use silver|gold.\n", color); return; }
        star_api_queue_pickup_with_mint(name, desc, "Quake", "KeyItem", 1, NULL, NULL, 1);
        star_api_result_t r = star_api_flush_add_item_jobs();
        if (r == STAR_API_SUCCESS) {
            Con_Printf("Added %s to STAR inventory.\n", name);
            q_strlcpy(g_star_last_pickup_name, name, sizeof(g_star_last_pickup_name));
            q_strlcpy(g_star_last_pickup_desc, desc ? desc : "", sizeof(g_star_last_pickup_desc));
            q_strlcpy(g_star_last_pickup_type, "KeyItem", sizeof(g_star_last_pickup_type));
            g_star_has_last_pickup = true;
        } else Con_Printf("Failed: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "version") == 0) {
        Con_Printf("STAR API integration 1.0 (OQuake)\n");
        Con_Printf("  Initialized: %s\n", star_initialized() ? "yes" : "no");
        if (!star_initialized()) Con_Printf("  Last error: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "status") == 0) {
        Con_Printf("STAR API initialized: %s\n", star_initialized() ? "yes" : "no");
        Con_Printf("Last error: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "inventory") == 0) {
        if (!star_initialized()) { Con_Printf("STAR API not initialized. %s\n", star_api_get_last_error()); return; }
        if (star_sync_inventory_in_progress()) {
            Con_Printf("Inventory sync in progress. Run 'star inventory' again in a moment.\n");
            return;
        }
        /* Refresh from client cache (one get_inventory); then print. */
        OQ_RefreshOverlayFromClient();
        if (g_inventory_count > 0) {
            size_t i;
            Con_Printf("STAR inventory (%d items):\n", g_inventory_count);
            for (i = 0; i < (size_t)g_inventory_count; i++) {
                const char *type = g_inventory_entries[i].item_type[0] ? g_inventory_entries[i].item_type : "n/a";
                int qty = g_inventory_entries[i].quantity > 0 ? g_inventory_entries[i].quantity : 1;
                Con_Printf("  %s - %s (type=%s, qty=%d)\n",
                    g_inventory_entries[i].name,
                    g_inventory_entries[i].description,
                    type,
                    qty);
            }
        } else {
            Con_Printf("STAR inventory is empty.\n");
        }
        return;
    }
    if (strcmp(sub, "has") == 0) {
        if (argc < 3) { Con_Printf("Usage: star has <item_name>\n"); return; }
        int has = star_api_has_item(Cmd_Argv(2));
        Con_Printf("Has '%s': %s\n", Cmd_Argv(2), has ? "yes" : "no");
        return;
    }
    if (strcmp(sub, "add") == 0) {
        if (!OQ_AllowPrivilegedCommands()) { Con_Printf("Only dellams or anorak can use star add.\n"); return; }
        if (argc < 3) { Con_Printf("Usage: star add <item_name> [description] [item_type]\n"); return; }
        const char* name = Cmd_Argv(2);
        const char* desc = argc > 3 ? Cmd_Argv(3) : "Added from console";
        const char* type = argc > 4 ? Cmd_Argv(4) : "Miscellaneous";
        star_api_queue_pickup_with_mint(name, desc, "Quake", type, 1, NULL, NULL, 1);
        star_api_result_t r = star_api_flush_add_item_jobs();
        if (r == STAR_API_SUCCESS) {
            Con_Printf("Added '%s' to STAR inventory.\n", name);
            q_strlcpy(g_star_last_pickup_name, name, sizeof(g_star_last_pickup_name));
            q_strlcpy(g_star_last_pickup_desc, desc ? desc : "", sizeof(g_star_last_pickup_desc));
            q_strlcpy(g_star_last_pickup_type, type ? type : "Miscellaneous", sizeof(g_star_last_pickup_type));
            g_star_has_last_pickup = true;
        } else Con_Printf("Failed to add '%s': %s\n", name, star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "use") == 0) {
        if (argc < 3) { Con_Printf("Usage: star use <item_name> [context]\n"); return; }
        const char* ctx = argc > 3 ? Cmd_Argv(3) : "console";
        star_api_queue_use_item(Cmd_Argv(2), ctx);
        int r = star_api_flush_use_item_jobs();
        int ok = (r == STAR_API_SUCCESS);
        Con_Printf("Use '%s' (context %s): %s\n", Cmd_Argv(2), ctx, ok ? "ok" : "failed");
        if (!ok) Con_Printf("  %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "lastpickup") == 0) {
        if (!g_star_has_last_pickup) {
            Con_Printf("No pickup has been synced to STAR yet in this session.\n");
            return;
        }
        Con_Printf("Last STAR-synced pickup:\n  name: %s\n  type: %s\n  desc: %s\n", g_star_last_pickup_name, g_star_last_pickup_type, g_star_last_pickup_desc);
        return;
    }
    if (strcmp(sub, "quest") == 0) {
        if (argc < 3) { Con_Printf("Usage: star quest start|objective|complete ...\n"); return; }
        const char* qsub = Cmd_Argv(2);
        if (strcmp(qsub, "start") == 0) {
            if (argc < 4) { Con_Printf("Usage: star quest start <quest_id>\n"); return; }
            star_api_result_t r = star_api_start_quest(Cmd_Argv(3));
            Con_Printf(r == STAR_API_SUCCESS ? "Quest started.\n" : "Failed: %s\n", star_api_get_last_error());
            return;
        }
        if (strcmp(qsub, "objective") == 0) {
            if (argc < 5) { Con_Printf("Usage: star quest objective <quest_id> <objective_id>\n"); return; }
            star_api_result_t r = star_api_complete_quest_objective(Cmd_Argv(3), Cmd_Argv(4), "Quake");
            Con_Printf(r == STAR_API_SUCCESS ? "Objective completed.\n" : "Failed: %s\n", star_api_get_last_error());
            return;
        }
        if (strcmp(qsub, "complete") == 0) {
            if (argc < 4) { Con_Printf("Usage: star quest complete <quest_id>\n"); return; }
            star_api_result_t r = star_api_complete_quest(Cmd_Argv(3));
            Con_Printf(r == STAR_API_SUCCESS ? "Quest completed.\n" : "Failed: %s\n", star_api_get_last_error());
            return;
        }
        Con_Printf("Unknown: star quest %s. Use start|objective|complete.\n", qsub);
        return;
    }
    if (strcmp(sub, "bossnft") == 0) {
        if (!OQ_AllowPrivilegedCommands()) { Con_Printf("Only dellams or anorak can use star bossnft.\n"); return; }
        if (argc < 3) { Con_Printf("Usage: star bossnft <boss_name> [description]\n"); return; }
        const char* name = Cmd_Argv(2);
        const char* desc = argc > 3 ? Cmd_Argv(3) : "Boss from OQuake";
        char nft_id[64] = {0};
        const char* prov = oquake_star_nft_provider.string && oquake_star_nft_provider.string[0] ? oquake_star_nft_provider.string : NULL;
        star_api_result_t r = star_api_create_monster_nft(name, desc, "Quake", "{}", prov, nft_id);
        if (r == STAR_API_SUCCESS) Con_Printf("Boss NFT created. ID: %s\n", nft_id[0] ? nft_id : "(none)");
        else Con_Printf("Failed: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "deploynft") == 0) {
        if (argc < 4) { Con_Printf("Usage: star deploynft <nft_id> <target_game> [location]\n"); return; }
        const char* loc = argc > 4 ? Cmd_Argv(4) : "";
        star_api_result_t r = star_api_deploy_boss_nft(Cmd_Argv(2), Cmd_Argv(3), loc);
        Con_Printf(r == STAR_API_SUCCESS ? "NFT deploy requested.\n" : "Failed: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "debug") == 0) {
        if (argc < 3 || !Cmd_Argv(2) || strcmp(Cmd_Argv(2), "status") == 0) {
            Con_Printf("STAR debug logging is %s\n", g_star_debug_logging ? "on" : "off");
            Con_Printf("Usage: star debug on|off|status\n");
            return;
        }
        if (strcmp(Cmd_Argv(2), "on") == 0) {
            g_star_debug_logging = true;
            star_api_set_debug(1);
            Con_Printf("STAR debug logging enabled. Check console and star_api.log (in id1 or exe dir).\n");
            OQ_StarDebugLog("STAR debug ON | max_health=%s max_armor=%s always_add=%s allow_pickup_if_max=%s use_health_on_pickup=%s use_armor_on_pickup=%s use_powerup_on_pickup=%s",
                oquake_star_max_health.string, oquake_star_max_armor.string,
                oquake_star_always_add_items_to_inventory.string, oquake_star_always_allow_pickup_if_max.string,
                oquake_star_use_health_on_pickup.string, oquake_star_use_armor_on_pickup.string, oquake_star_use_powerup_on_pickup.string);
            return;
        }
        if (strcmp(Cmd_Argv(2), "off") == 0) { g_star_debug_logging = false; star_api_set_debug(0); Con_Printf("STAR debug logging disabled.\n"); return; }
        Con_Printf("Unknown debug option: %s. Use on|off|status.\n", Cmd_Argv(2));
        return;
    }
    if (strcmp(sub, "send_avatar") == 0) {
        if (argc < 4) { Con_Printf("Usage: star send_avatar <username> <item_class>\n"); return; }
        Con_Printf("Send to avatar: \"%s\" item \"%s\" (STAR send API not yet implemented).\n", Cmd_Argv(2), Cmd_Argv(3));
        return;
    }
    if (strcmp(sub, "send_clan") == 0) {
        if (argc < 4) { Con_Printf("Usage: star send_clan <clan_name> <item_class>\n"); return; }
        Con_Printf("Send to clan: \"%s\" item \"%s\" (STAR send API not yet implemented).\n", Cmd_Argv(2), Cmd_Argv(3));
        return;
    }
    if (strcmp(sub, "beamin") == 0 || (strcmp(sub, "beamed") == 0 && argc >= 3 && strcmp(Cmd_Argv(2), "in") == 0)) {
        const char* runtime_user = NULL;
        const char* runtime_pass = NULL;
        int arg_shift = (strcmp(sub, "beamed") == 0) ? 1 : 0;
        if (argc >= (4 + arg_shift) && strcmp(Cmd_Argv(2 + arg_shift), "jwt") != 0) {
            runtime_user = Cmd_Argv(2 + arg_shift);
            runtime_pass = Cmd_Argv(3 + arg_shift);
        }

        if (star_initialized() && !runtime_user) { Con_Printf("Already logged in. Use 'star beamout' first.\n"); return; }
        if (star_initialized() && runtime_user) {
            star_api_cleanup();
            g_star_initialized = 0;
            g_star_beamed_in = 0;
        }

        if (runtime_user && runtime_pass && OQ_IsMockAnorakCredentials(runtime_user, runtime_pass)) {
            g_star_initialized = 1;
            q_strlcpy(g_star_username, runtime_user, sizeof(g_star_username));
            /* Save to CVARs */
            Cvar_Set("oquake_star_username", runtime_user);
            Cvar_Set("oquake_star_password", runtime_pass);
            OQ_ApplyBeamFacePreference();
            Con_Printf("Beam-in successful (mock). Welcome, %s.\n", runtime_user);
            return;
        }

        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
        
        /* Load API URL: CVAR -> env -> default */
        const char* api_url = oquake_star_api_url.string;
        if (!api_url || !api_url[0]) {
            api_url = getenv("STAR_API_URL");
            if (!api_url || !api_url[0]) {
                api_url = "https://star-api.oasisplatform.world/api";
            }
        }
        g_star_config.base_url = api_url;
        
        /* Load API key: runtime -> CVAR -> env */
        const char* api_key = NULL;
        if (runtime_user && runtime_pass) {
            /* If username/password provided, use CVAR or env for API key */
            api_key = oquake_star_api_key.string;
            if (!api_key || !api_key[0]) {
                api_key = getenv("STAR_API_KEY");
            }
        } else {
            api_key = oquake_star_api_key.string;
            if (!api_key || !api_key[0]) {
                api_key = getenv("STAR_API_KEY");
            }
        }
        g_star_config.api_key = api_key;
        
        /* Load Avatar ID: CVAR -> env */
        const char* avatar_id = oquake_star_avatar_id.string;
        if (!avatar_id || !avatar_id[0]) {
            avatar_id = getenv("STAR_AVATAR_ID");
        }
        g_star_config.avatar_id = avatar_id;
        g_star_config.timeout_seconds = 30;
        star_api_result_t r = star_api_init(&g_star_config);
        if (r != STAR_API_SUCCESS) {
            Con_Printf("Beamin failed - init: %s\n", star_api_get_last_error());
            return;
        }
        /* NFT minting and avatar auth use WEB4 OASIS API; set from oquake_oasis_api_url so mint goes to WEB4 not WEB5. */
        if (oquake_oasis_api_url.string && oquake_oasis_api_url.string[0]) {
            star_api_set_oasis_base_url(oquake_oasis_api_url.string);
        }
        /* Load username: runtime -> CVAR -> env */
        const char* username = runtime_user;
        if (!username || !username[0]) {
            username = oquake_star_username.string;
            if (!username || !username[0]) {
                username = getenv("STAR_USERNAME");
            }
        }
        
        /* Load password: runtime -> CVAR -> env */
        const char* password = runtime_pass;
        if (!password || !password[0]) {
            password = oquake_star_password.string;
            if (!password || !password[0]) {
                password = getenv("STAR_PASSWORD");
            }
        }
        
        if (username && password) {
            if (star_sync_auth_in_progress()) {
                Con_Printf("Authentication already in progress. Please wait...\n");
                return;
            }
            star_sync_auth_start(username, password, OQ_OnAuthDone, NULL);
            Con_Printf("Authenticating... Please wait...\n");
            if (runtime_user) Cvar_Set("oquake_star_username", runtime_user);
            if (runtime_pass) Cvar_Set("oquake_star_password", runtime_pass);
            return;
        }
        if (g_star_config.api_key && g_star_config.avatar_id) {
            g_star_initialized = 1;
            if (!g_star_refresh_xp_called_this_session) {
                g_star_refresh_xp_called_this_session = 1;
                star_api_refresh_avatar_xp();
            }
            g_star_beamed_in = 1;
            // Try to get username from avatar_id or use a default
            if (g_star_config.avatar_id) {
                q_strlcpy(g_star_username, "API User", sizeof(g_star_username));
            }
            /* Save API key and avatar ID to CVARs if they came from env */
            if (api_key && !oquake_star_api_key.string[0]) {
                Cvar_Set("oquake_star_api_key", api_key);
            }
            if (avatar_id && !oquake_star_avatar_id.string[0]) {
                Cvar_Set("oquake_star_avatar_id", avatar_id);
            }
            OQ_ApplyBeamFacePreference();
            Con_Printf("Logged in with API key. Cross-game assets enabled.\n");
            return;
        }
        Con_Printf("Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID and try again.\n");
        return;
    }
    if (strcmp(sub, "beamout") == 0) {
        if (!star_initialized()) { Con_Printf("Not logged in. Use 'star beamin' to log in.\n"); return; }
        star_api_cleanup();
        g_star_initialized = 0;
        g_star_beamed_in = 0;
        g_star_refresh_xp_called_this_session = 0;  /* Next beam-in will call refresh once. */
        g_star_username[0] = 0;
        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
        Con_Printf("Logged out (beamout). Use 'star beamin' to log in again.\n");
        return;
    }
    if (strcmp(sub, "face") == 0) {
        Con_Printf("\n");
        if (argc < 3 || !Cmd_Argv(2) || strcmp(Cmd_Argv(2), "status") == 0) {
            Con_Printf("Beam-in face switch is %s\n", oasis_star_beam_face.value > 0.5f ? "on" : "off");
            Con_Printf("Usage: star face on|off|status\n");
            Con_Printf("\n");
            return;
        }
        if (Cmd_Argv(2) && strcmp(Cmd_Argv(2), "on") == 0) {
            Cvar_SetValueQuick(&oasis_star_beam_face, 1);
            OQ_ApplyBeamFacePreference();
            OQ_SaveStarConfigToFiles();
            Con_Printf("Beam-in face switch enabled.\n");
            Con_Printf("\n");
            return;
        }
        if (Cmd_Argv(2) && strcmp(Cmd_Argv(2), "off") == 0) {
            Cvar_SetValueQuick(&oasis_star_beam_face, 0);
            Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
            OQ_SaveStarConfigToFiles();
            Con_Printf("Beam-in face switch disabled.\n");
            Con_Printf("\n");
            return;
        }
        Con_Printf("Unknown face option: %s. Use on|off|status.\n", Cmd_Argv(2) ? Cmd_Argv(2) : "(none)");
        Con_Printf("\n");
        return;
    }
    if (sub && q_strcasecmp(sub, "config") == 0) {
        const char* save_arg = (argc >= 3) ? Cmd_Argv(2) : NULL;
        if (save_arg && strcmp(save_arg, "save") == 0) {
            OQ_SaveStarConfigToFiles();
            Con_Printf("Config saved to oasisstar.json and config.cfg (if paths found).\n");
            return;
        }
        OQ_StarConfig_f();
        return;
    }
    if (strcmp(sub, "stack") == 0) {
        if (argc < 4) {
            Con_Printf("Usage: star stack <armor|weapons|powerups|keys|sigils> <0|1>\n");
            Con_Printf("  1 = stack (each pickup adds quantity), 0 = unlock (one per type). Ammo always stacks.\n");
            return;
        }
        const char* cat = Cmd_Argv(2);
        const char* val = Cmd_Argv(3);
        int on = (val[0] == '1' && val[1] == '\0') ? 1 : 0;
        const char* cvar = NULL;
        if (strcmp(cat, "armor") == 0) cvar = "oquake_star_stack_armor";
        else if (strcmp(cat, "weapons") == 0) cvar = "oquake_star_stack_weapons";
        else if (strcmp(cat, "powerups") == 0) cvar = "oquake_star_stack_powerups";
        else if (strcmp(cat, "keys") == 0) cvar = "oquake_star_stack_keys";
        else if (strcmp(cat, "sigils") == 0) cvar = "oquake_star_stack_sigils";
        if (!cvar) {
            Con_Printf("Unknown category: %s. Use armor|weapons|powerups|keys|sigils\n", cat);
            return;
        }
        Cvar_Set(cvar, on ? "1" : "0");
        OQ_SaveStarConfigToFiles();
        Con_Printf("%s set to %s (%s). Config files updated.\n", cvar, on ? "1" : "0", on ? "stack" : "unlock");
        return;
    }
    if (strcmp(sub, "mint") == 0) {
        if (argc >= 5 && strcmp(Cmd_Argv(2), "monster") == 0) {
            const char* name_arg = Cmd_Argv(3);
            const char* val = Cmd_Argv(4);
            int on = (val[0] == '1' && val[1] == '\0') ? 1 : 0;
            int i;
            const oquake_monster_entry_t* chosen = NULL;
            int chosen_idx = -1;
            for (i = 0; i < OQ_MONSTER_COUNT; i++) {
                if (q_strcasecmp(OQUAKE_MONSTERS[i].config_key, name_arg) == 0) { chosen = &OQUAKE_MONSTERS[i]; chosen_idx = i; break; }
                if (q_strcasecmp(OQUAKE_MONSTERS[i].display_name, name_arg) == 0) { chosen = &OQUAKE_MONSTERS[i]; chosen_idx = i; break; }
                if (OQUAKE_MONSTERS[i].engine_name && q_strcasecmp(OQUAKE_MONSTERS[i].engine_name, name_arg) == 0) { chosen = &OQUAKE_MONSTERS[i]; chosen_idx = i; break; }
            }
            if (!chosen || chosen_idx < 0 || chosen_idx >= OQ_MONSTER_FLAGS_MAX) {
                Con_Printf("Unknown monster: %s. Use star config to see list (e.g. oquake_ogre, Shambler).\n", name_arg);
                return;
            }
            for (i = 0; i < OQ_MONSTER_COUNT && i < OQ_MONSTER_FLAGS_MAX; i++)
                if (strcmp(OQUAKE_MONSTERS[i].config_key, chosen->config_key) == 0)
                    g_oq_mint_monster_flags[i] = on ? 1 : 0;
            OQ_SaveStarConfigToFiles();
            Con_Printf("Mint NFT for %s (mint_monster_%s) set to %s. Config saved.\n", chosen->display_name, chosen->config_key, on ? "on" : "off");
            return;
        }
        if (argc < 4) {
            Con_Printf("Usage: star mint <armor|weapons|powerups|keys> <0|1>\n");
            Con_Printf("       star mint monster <name> <0|1>\n");
            Con_Printf("  1 = mint NFT when collecting/killing that category, 0 = off.\n");
            return;
        }
        const char* cat = Cmd_Argv(2);
        const char* val = Cmd_Argv(3);
        int on = (val[0] == '1' && val[1] == '\0') ? 1 : 0;
        const char* cvar = NULL;
        if (strcmp(cat, "armor") == 0) cvar = "oquake_star_mint_armor";
        else if (strcmp(cat, "weapons") == 0) cvar = "oquake_star_mint_weapons";
        else if (strcmp(cat, "powerups") == 0) cvar = "oquake_star_mint_powerups";
        else if (strcmp(cat, "keys") == 0) cvar = "oquake_star_mint_keys";
        if (!cvar) {
            Con_Printf("Unknown category: %s. Use armor|weapons|powerups|keys or star mint monster <name> <0|1>.\n", cat);
            return;
        }
        Cvar_Set(cvar, on ? "1" : "0");
        OQ_SaveStarConfigToFiles();
        Con_Printf("%s set to %s. Config files updated.\n", cvar, on ? "1" : "0");
        return;
    }
    if (strcmp(sub, "nftprovider") == 0) {
        if (argc < 3) {
            Con_Printf("Usage: star nftprovider <name>\n");
            Con_Printf("  e.g. SolanaOASIS (default). Used when minting inventory item NFTs.\n");
            return;
        }
        Cvar_Set("oquake_star_nft_provider", Cmd_Argv(2));
        OQ_SaveStarConfigToFiles();
        Con_Printf("NFT provider set to: %s. Config files updated.\n", Cmd_Argv(2));
        return;
    }
    if (strcmp(sub, "seturl") == 0) {
        if (argc < 3) {
            Con_Printf("Usage: star seturl <star_api_url>\n");
            return;
        }
        Cvar_Set("oquake_star_api_url", Cmd_Argv(2));
        OQ_SaveStarConfigToFiles();
        Con_Printf("STAR API URL set to: %s. Config files updated.\n", Cmd_Argv(2));
        return;
    }
    if (strcmp(sub, "setoasisurl") == 0) {
        if (argc < 3) {
            Con_Printf("Usage: star setoasisurl <oasis_api_url>\n");
            return;
        }
        Cvar_Set("oquake_oasis_api_url", Cmd_Argv(2));
        if (g_star_initialized)
            star_api_set_oasis_base_url(Cmd_Argv(2));
        OQ_SaveStarConfigToFiles();
        Con_Printf("OASIS API URL set to: %s. Config files updated.\n", Cmd_Argv(2));
        return;
    }
    if (strcmp(sub, "configfile") == 0) {
        if (argc < 3) {
            Con_Printf("Usage: star configfile json|cfg\n");
            Con_Printf("  json - prefer oasisstar.json (default)\n");
            Con_Printf("  cfg  - prefer config.cfg\n");
            return;
        }
        const char* val = Cmd_Argv(2);
        if (q_strcasecmp(val, "json") == 0) {
            Cvar_Set("oquake_star_config_file", "json");
            OQ_SaveStarConfigToFiles();
            Con_Printf("Config file preference set to json. Config files updated.\n");
            return;
        }
        if (q_strcasecmp(val, "cfg") == 0) {
            Cvar_Set("oquake_star_config_file", "cfg");
            OQ_SaveStarConfigToFiles();
            Con_Printf("Config file preference set to cfg. Config files updated.\n");
            return;
        }
        Con_Printf("Unknown value: %s. Use json or cfg.\n", val);
        return;
    }
    if (strcmp(sub, "reloadconfig") == 0) {
        if (g_json_config_path[0] && OQ_LoadJsonConfig(g_json_config_path)) {
            Con_Printf("Reloaded config from: %s\n", g_json_config_path);
            return;
        }
        char path[512];
        if (OQ_FindConfigFile("oasisstar.json", path, sizeof(path)) && OQ_LoadJsonConfig(path)) {
            q_strlcpy(g_json_config_path, path, sizeof(g_json_config_path));
            Con_Printf("Reloaded config from: %s\n", path);
            return;
        }
        Con_Printf("Could not find or load oasisstar.json. Try exec config.cfg for config.cfg.\n");
        return;
    }
    Con_Printf("Unknown STAR subcommand: '%s'. Type 'star' for list.\n", sub ? sub : "(null)");
}

void OQuake_STAR_DrawInventoryOverlay(cb_context_t* cbx) {
    if (!cbx)
        return;
    int panel_w;
    int panel_h;
    int panel_x;
    int panel_y;
    int tab_y;
    int tab_slot_w;
    int tab;
    int draw_y;
    int row;
    int rep_indices[OQ_MAX_INVENTORY_ITEMS];
    char grouped_labels[OQ_MAX_INVENTORY_ITEMS][OQ_GROUP_LABEL_MAX];
    int grouped_modes[OQ_MAX_INVENTORY_ITEMS];
    int grouped_values[OQ_MAX_INVENTORY_ITEMS];
    qboolean grouped_pending[OQ_MAX_INVENTORY_ITEMS];
    int grouped_count;
    int visible_end;
    char line[512];

    if (!g_star_initialized || !cbx)
        return;
    /* Draw toast first every frame (so it shows when C/F or E at max even if overlay is closed) */
    OQuake_STAR_DrawToast(cbx);
    /* Check if background operations completed */
    OQ_CheckAuthenticationComplete();
    OQ_CheckInventoryRefreshComplete();

    /* Q key: edge-triggered toggle for quest popup (same pattern as I for inventory - poll in draw path so it works regardless of binding) */
    {
        static int s_q_key = -1;
        if (s_q_key < 0)
            s_q_key = Key_StringToKeynum("q");
        if (s_q_key >= 0 && s_q_key < MAX_KEYS && key_dest != key_message && key_dest != key_console && key_dest != key_menu) {
            if (keydown[s_q_key] && !g_quest_key_was_down) {
                g_quest_popup_open = !g_quest_popup_open;
                if (g_quest_popup_open)
                    star_api_invalidate_quest_cache();
                else
                    g_quest_status_message[0] = '\0', g_quest_status_frames = 0;
                g_quest_key_was_down = true;
            }
            if (!keydown[s_q_key])
                g_quest_key_was_down = false;
        }
    }

    /* C = use health, F = use armor (like ODOOM): poll in draw path so they work regardless of key bindings. */
    if (key_dest != key_message && key_dest != key_console && key_dest != key_menu && g_star_initialized) {
        static int s_c_key = -1, s_f_key = -1;
        if (s_c_key < 0) s_c_key = Key_StringToKeynum("c");
        if (s_f_key < 0) s_f_key = Key_StringToKeynum("f");
        if (s_c_key >= 0 && s_c_key < MAX_KEYS && keydown[s_c_key] && !g_c_key_was_down) {
            g_c_key_was_down = true;
            OQ_UseHealth_f();
        }
        if (!(s_c_key >= 0 && s_c_key < MAX_KEYS && keydown[s_c_key]))
            g_c_key_was_down = false;
        if (s_f_key >= 0 && s_f_key < MAX_KEYS && keydown[s_f_key] && !g_f_key_was_down) {
            g_f_key_was_down = true;
            OQ_UseArmor_f();
        }
        if (!(s_f_key >= 0 && s_f_key < MAX_KEYS && keydown[s_f_key]))
            g_f_key_was_down = false;
    }

    if (g_inventory_open)
        OQ_PollInventoryHotkeys();

    if (!g_inventory_open && !g_quest_popup_open)
        return;

    if (g_inventory_open) {
    /* Refresh list from client every frame while overlay is open (merge is in-memory, so pickups show immediately). */
    OQ_RefreshOverlayFromClient();

    panel_w = q_min(glwidth - 48, 900);
    panel_h = q_min(glheight - 96, 480);  /* Increased height to prevent text overlap */
    if (panel_w < 480) panel_w = 480;
    if (panel_h < 160) panel_h = 160;
    panel_x = (glwidth - panel_w) / 2;
    panel_y = (glheight - panel_h) / 2;
    if (panel_x < 0) panel_x = 0;
    if (panel_y < 0) panel_y = 0;

    Draw_Fill(cbx, panel_x, panel_y, panel_w, panel_h, 0, 0.70f);
    {
        const char* header = "OASIS INVENTORY ";
        int header_len = strlen(header);
        int header_x = panel_x + (panel_w - (header_len * 8)) / 2;
        if (header_x < panel_x + 6) header_x = panel_x + 6;
        Draw_String(cbx, header_x, panel_y + 6, header);
    }
    tab_y = panel_y + 34;
    tab_slot_w = (panel_w - 24) / OQ_TAB_COUNT;
    for (tab = 0; tab < OQ_TAB_COUNT; tab++) {
        int slot_x = panel_x + 12 + tab * tab_slot_w;
        const char* tab_name = OQ_TabShortName(tab);
        int tab_name_w = (int)strlen(tab_name) * 8;
        int tab_name_x = slot_x + (tab_slot_w - tab_name_w) / 2;
        if (tab == g_inventory_active_tab)
            Draw_Fill(cbx, slot_x + 1, tab_y - 1, tab_slot_w - 2, 10, 224, 0.60f);
        Draw_String(cbx, tab_name_x, tab_y, tab_name);
    }
    Draw_String(cbx, panel_x + 6, panel_y + panel_h - 16, "Arrows=Select  E=Use  C=Health  F=Armor  Z/X=Send  I=Toggle  O/P=Tabs");

    draw_y = panel_y + 54;
    grouped_count = OQ_BuildGroupedRows(
        rep_indices, grouped_labels, grouped_modes, grouped_values, grouped_pending, OQ_MAX_INVENTORY_ITEMS);
    OQ_ClampSelection(grouped_count);
    visible_end = q_min(grouped_count, g_inventory_scroll_row + OQ_MAX_OVERLAY_ROWS);
    for (row = g_inventory_scroll_row; row < visible_end; row++) {
        if (row == g_inventory_selected_row)
            Draw_Fill(cbx, panel_x + 5, draw_y - 1, panel_w - 10, 10, 224, 0.50f);
        if (grouped_modes[row] == OQ_GROUP_MODE_SUM)
            q_snprintf(line, sizeof(line), "%s +%d", grouped_labels[row], grouped_values[row]);
        else
            q_snprintf(line, sizeof(line), "%s x%d", grouped_labels[row], grouped_values[row]);
        if (grouped_pending[row] && strlen(line) < sizeof(line) - 8)
            q_strlcat(line, " [LOCAL]", sizeof(line));
        Draw_String(cbx, panel_x + 8, draw_y, line);
        draw_y += 8;
    }

    /* Draw status message in bottom right corner */
    if (g_inventory_status[0] && strcmp(g_inventory_status, "STAR inventory unavailable.") != 0) {
        int status_len = (int)strlen(g_inventory_status);
        int status_x = panel_x + panel_w - (status_len * 8) - 6;
        int status_y = panel_y + panel_h - 16;
        Draw_String(cbx, status_x, status_y, g_inventory_status);
    }
    
    if (grouped_count == 0)
        Draw_String(cbx, panel_x + 6, draw_y, "No items");

    if (g_inventory_send_popup != OQ_SEND_POPUP_NONE) {
        int popup_w = q_min(panel_w - 80, 420);
        int popup_h = 108;
        int popup_x = panel_x + (panel_w - popup_w) / 2;
        int popup_y = panel_y + (panel_h - popup_h) / 2;
        const char* title = g_inventory_send_popup == OQ_SEND_POPUP_CLAN ? "SEND TO CLAN" : "SEND TO AVATAR";
        const char* label = g_inventory_send_popup == OQ_SEND_POPUP_CLAN ? "Clan" : "Username";
        int mode = OQ_GROUP_MODE_COUNT;
        int available = 1;
        char send_item_label[OQ_GROUP_LABEL_MAX];

        Draw_Fill(cbx, popup_x, popup_y, popup_w, popup_h, 0, 0.9f);
        Draw_String(cbx, popup_x + 8, popup_y + 8, title);
        /* Show which item we are sending above the name box */
        send_item_label[0] = '\0';
        if (OQ_GetSelectedItem()) {
            OQ_GetGroupedDisplayInfo(OQ_GetSelectedItem(), send_item_label, sizeof(send_item_label), &mode, &available);
            if (mode != OQ_GROUP_MODE_COUNT && available > 1)
                q_snprintf(line, sizeof(line), "Sending: %s x%d", send_item_label, g_inventory_send_quantity);
            else
                q_snprintf(line, sizeof(line), "Sending: %s", send_item_label);
            Draw_String(cbx, popup_x + 8, popup_y + 18, line);
        }
        q_snprintf(line, sizeof(line), "%s: %s%s", label, g_inventory_send_target, ((int)(realtime * 2) & 1) ? "_" : "");
        Draw_String(cbx, popup_x + 8, popup_y + 30, line);
        OQ_GetSelectedGroupInfo(NULL, &mode, &available, NULL, 0);
        if (mode != OQ_GROUP_MODE_COUNT)
            available = 1;
        if (available < 1)
            available = 1;
        if (g_inventory_send_quantity < 1)
            g_inventory_send_quantity = 1;
        if (g_inventory_send_quantity > available)
            g_inventory_send_quantity = available;
        q_snprintf(line, sizeof(line), "Quantity: %d / %d (Up/Down)", g_inventory_send_quantity, available);
        Draw_String(cbx, popup_x + 8, popup_y + 42, line);
        Draw_String(cbx, popup_x + 8, popup_y + 54, "Left=Send  Right=Cancel");

        if (g_inventory_send_button == 0)
            Draw_Fill(cbx, popup_x + 8, popup_y + 78, 64, 10, 224, 0.65f);
        Draw_String(cbx, popup_x + 16, popup_y + 79, "SEND");

        if (g_inventory_send_button == 1)
            Draw_Fill(cbx, popup_x + 84, popup_y + 78, 72, 10, 224, 0.65f);
        Draw_String(cbx, popup_x + 92, popup_y + 79, "CANCEL");
    }
    } /* end if (g_inventory_open) */

    /* Quest popup (Q key): filter by status (Home/End/PgUp), selection (Up/Down), Enter = Start (Not Started) or Set tracker (In Progress). */
    if (g_quest_popup_open) {
        static char quest_buf[16384];
        static char q_id[OQ_QUEST_MAX][64];
        static char q_name[OQ_QUEST_MAX][128];
        static char q_status[OQ_QUEST_MAX][24];
        static char q_pct[OQ_QUEST_MAX][8];
        static int q_count;
        static int q_filtered_indices[OQ_QUEST_MAX];
        static int q_filtered_count;

        int n = star_api_get_quests_string(quest_buf, sizeof(quest_buf));
        if (n < 0) n = 0;
        if (n >= (int)sizeof(quest_buf)) n = (int)sizeof(quest_buf) - 1;
        quest_buf[n] = '\0';

        q_count = 0;
        if (n > 0 && quest_buf[0] && (n < 9 || memcmp(quest_buf, "Loading...", 9) != 0) && (n < 6 || memcmp(quest_buf, "Error:", 6) != 0)) {
            /* Parse line by line: every line starting with Q\t is a quest (id, name, desc, status, pct) */
            const char* p = quest_buf;
            const char* end = quest_buf + n;
            /* Skip UTF-8 BOM and leading newlines */
            if (p + 3 <= end && (unsigned char)p[0] == 0xEF && (unsigned char)p[1] == 0xBB && (unsigned char)p[2] == 0xBF)
                p += 3;
            while (p < end && (*p == '\n' || *p == '\r')) p++;
            while (p < end && q_count < OQ_QUEST_MAX) {
                const char* eol = (const char*)memchr(p, '\n', (size_t)(end - p));
                if (!eol) eol = end;
                /* Line may start with "Q\t" or "---Q\t" (legacy: separator and next quest on same line) */
                const char* qstart = p;
                if (eol - p >= 5 && p[0] == '-' && p[1] == '-' && p[2] == '-') {
                    qstart = p + 3;
                    while (qstart < eol && (*qstart == ' ' || *qstart == '\t')) qstart++;
                }
                if (eol - qstart >= 2 && qstart[0] == 'Q' && qstart[1] == '\t') {
                    const char* f = qstart + 2;
                    const char* fe = eol;
                    const char* t;
                    t = (const char*)memchr(f, '\t', (size_t)(fe - f));
                    if (t && t - f < 63) {
                        int id_len = (int)(t - f);
                        memcpy(q_id[q_count], f, (size_t)id_len);
                        q_id[q_count][id_len] = '\0';
                        f = t + 1;
                    } else { q_id[q_count][0] = '\0'; f = fe; }
                    t = f < fe ? (const char*)memchr(f, '\t', (size_t)(fe - f)) : NULL;
                    if (t && t - f < 127) {
                        int name_len = (int)(t - f);
                        memcpy(q_name[q_count], f, (size_t)name_len);
                        q_name[q_count][name_len] = '\0';
                        f = t + 1;
                    } else { q_name[q_count][0] = '\0'; if (f < fe) f = fe; }
                    t = f < fe ? (const char*)memchr(f, '\t', (size_t)(fe - f)) : NULL;
                    if (t) f = t + 1; else f = fe;
                    t = f < fe ? (const char*)memchr(f, '\t', (size_t)(fe - f)) : NULL;
                    if (t && t - f < 23) {
                        int st_len = (int)(t - f);
                        memcpy(q_status[q_count], f, (size_t)st_len);
                        q_status[q_count][st_len] = '\0';
                        /* Strip \r and normalize: remove spaces so "Not Started" matches "NotStarted" */
                        { int j, k; for (j = 0, k = 0; q_status[q_count][j]; j++) { if (q_status[q_count][j] == '\r') break; if (q_status[q_count][j] != ' ') q_status[q_count][k++] = q_status[q_count][j]; } q_status[q_count][k] = '\0'; }
                        f = t + 1;
                    } else { q_status[q_count][0] = '\0'; if (f < fe) f = fe; }
                    if (fe - f < 7)
                        memcpy(q_pct[q_count], f, (size_t)(fe - f)), q_pct[q_count][(int)(fe - f)] = '\0';
                    else
                        q_pct[q_count][0] = '\0';
                    q_count++;
                }
                p = eol + (eol < end && *eol == '\n' ? 1 : 0);
            }
        }

        /* Build filtered list (accept status "0"/"1"/"2" or "NotStarted"/"InProgress"/"Completed") */
        q_filtered_count = 0;
        for (int i = 0; i < q_count && q_filtered_count < OQ_QUEST_MAX; i++) {
            qboolean show = false;
            if (q_status[i][0]) {
                if ((strcmp(q_status[i], "NotStarted") == 0 || strcmp(q_status[i], "0") == 0) && g_quest_filter_not_started) show = true;
                else if ((strcmp(q_status[i], "InProgress") == 0 || strcmp(q_status[i], "1") == 0) && g_quest_filter_in_progress) show = true;
                else if ((strcmp(q_status[i], "Completed") == 0 || strcmp(q_status[i], "2") == 0) && g_quest_filter_completed) show = true;
            }
            if (show)
                q_filtered_indices[q_filtered_count++] = i;
        }
        /* No fallback: when filters hide all quests, list stays empty so toggles actually filter the list. */

        /* Debug: log what we received and parsed (once per popup open, when we have data) */
        {
            static int s_last_log_n = -1;
            static int s_last_log_count = -1;
            if (n != s_last_log_n || q_count != s_last_log_count) {
                s_last_log_n = n;
                s_last_log_count = q_count;
                OQ_StarDebugLog("Quest popup: bytes_from_api=%d q_parsed=%d q_filtered=%d", n, q_count, q_filtered_count);
                if (q_count > 0) {
                    int log_max = q_count > 24 ? 24 : q_count;
                    int ii;
                    for (ii = 0; ii < log_max; ii++)
                        OQ_StarDebugLog("  [%d] id=%s name=%.60s status=%s", ii, q_id[ii], q_name[ii] ? q_name[ii] : "(null)", q_status[ii] ? q_status[ii] : "(null)");
                } else if (n > 20) {
                    char prev[420];
                    int pi = 0;
                    int ni = 0;
                    prev[0] = '\0';
                    for (ni = 0; ni < n && pi < 400; ni++) {
                        char c = quest_buf[ni];
                        if (c == '\n') { if (pi + 2 < (int)sizeof(prev)) { prev[pi++] = '\\'; prev[pi++] = 'n'; } }
                        else if (c == '\r') { if (pi + 2 < (int)sizeof(prev)) { prev[pi++] = '\\'; prev[pi++] = 'r'; } }
                        else if (c == '\t') { if (pi + 1 < (int)sizeof(prev)) prev[pi++] = '|'; }
                        else if (c >= 32 && c < 127) { if (pi + 1 < (int)sizeof(prev)) prev[pi++] = c; }
                        else { if (pi + 4 < (int)sizeof(prev)) pi += q_snprintf(prev + pi, (int)sizeof(prev) - pi, "\\x%02x", (unsigned char)c); }
                    }
                    prev[pi] = '\0';
                    OQ_StarDebugLog("Quest buffer preview (q_count=0): %s", prev);
                }
            }
        }

        /* Key handling: use Key_StringToKeynum so Enter works regardless of engine key numbering */
        {
            static int s_enter_key = -2, s_kp_enter_key = -2;
            if (s_enter_key == -2) s_enter_key = Key_StringToKeynum("enter");
            if (s_kp_enter_key == -2) s_kp_enter_key = Key_StringToKeynum("kp_enter");
            if (s_enter_key < 0) s_enter_key = K_ENTER;
            if (s_kp_enter_key < 0) s_kp_enter_key = K_KP_ENTER;
            if (q_filtered_count > 0) {
                if (OQ_KeyPressed(K_UPARROW) || OQ_KeyPressed(K_MWHEELUP)) {
                    g_quest_selected_index--;
                    if (g_quest_selected_index < 0) g_quest_selected_index = 0;
                }
                if (OQ_KeyPressed(K_DOWNARROW) || OQ_KeyPressed(K_MWHEELDOWN)) {
                    g_quest_selected_index++;
                    if (g_quest_selected_index >= q_filtered_count) g_quest_selected_index = q_filtered_count - 1;
                }
                if (OQ_KeyPressed(s_enter_key) || OQ_KeyPressed(s_kp_enter_key)) {
                    int idx = q_filtered_indices[g_quest_selected_index];
                    if (idx >= 0 && idx < q_count && q_id[idx][0]) {
                        if (strcmp(q_status[idx], "NotStarted") == 0 || strcmp(q_status[idx], "0") == 0) {
                            q_strlcpy(g_quest_status_message, "Starting quest...", sizeof(g_quest_status_message));
                            g_quest_status_frames = 105;  /* ~3 sec at 35 fps */
                            star_api_start_quest(q_id[idx]);
                        } else if (strcmp(q_status[idx], "InProgress") == 0 || strcmp(q_status[idx], "1") == 0) {
                            q_strlcpy(g_quest_tracker_id, q_id[idx], sizeof(g_quest_tracker_id));
                            q_strlcpy(g_quest_tracker_name, q_name[idx] ? q_name[idx] : "", sizeof(g_quest_tracker_name));
                        }
                    }
                }
            }
        }
        if (OQ_KeyPressed(K_HOME))   g_quest_filter_not_started = !g_quest_filter_not_started;
        if (OQ_KeyPressed(K_END))   g_quest_filter_completed = !g_quest_filter_completed;
        if (OQ_KeyPressed(K_PGUP))  g_quest_filter_in_progress = !g_quest_filter_in_progress;

        if (g_quest_selected_index >= q_filtered_count && q_filtered_count > 0)
            g_quest_selected_index = q_filtered_count - 1;
        if (g_quest_selected_index < 0) g_quest_selected_index = 0;

        /* Draw: double width (640px), space below toggles */
        int qw = q_min(glwidth - 48, 640);
        int qh = q_min(glheight - 96, 480);
        if (qw < 400) qw = 400;
        if (qh < 160) qh = 160;
        int qx = (glwidth - qw) / 2;
        int qy = (glheight - qh) / 2;
        if (qx < 0) qx = 0;
        if (qy < 0) qy = 0;
        Draw_Fill(cbx, qx, qy, qw, qh, 0, 0.75f);
        Draw_String(cbx, qx + (qw - 6*8) / 2, qy + 6, "QUESTS");
        /* Space beneath Quests heading */

        /* Toggles always visible, centre-aligned */
        {
            char cb[128];
            int cb_len;
            q_snprintf(cb, sizeof(cb), "%s Not Started  %s In Progress  %s Completed",
                g_quest_filter_not_started ? "[X]" : "[ ]",
                g_quest_filter_in_progress ? "[X]" : "[ ]",
                g_quest_filter_completed ? "[X]" : "[ ]");
            cb_len = (int)strlen(cb);
            Draw_String(cbx, qx + (qw - cb_len * 8) / 2, qy + 24, cb);
        }

        if (n >= 9 && memcmp(quest_buf, "Loading...", 9) == 0) {
            Draw_String(cbx, qx + 10, qy + 48, "Loading quests...");
        } else if (n >= 6 && memcmp(quest_buf, "Error:", 6) == 0) {
            Draw_String(cbx, qx + 10, qy + 48, "Error loading quests. Check console or star_api.log for details.");
        } else if (q_filtered_count > 0) {
            /* Table: Name | % | Status (wider columns for 640px popup) */
            char name_buf[64];
            char status_display[20];
            const char* status_str;
            int i, dy, max_rows, row_h;
            int idx;
            qboolean sel;
            int col1_x, col2_x, col3_x;
            int col1_chars = 54;  /* name column ~50px wider again (54*8 = 432px) */
            int col2_chars = 6;
            dy = qy + 48;  /* space below toggles */
            row_h = 12;
            max_rows = (qh - 84) / row_h;  /* heading + space + toggles + space + header + footer */
            if (max_rows < 6) max_rows = 6;
            if (max_rows > OQ_QUEST_MAX) max_rows = OQ_QUEST_MAX;
            col1_x = qx + 10;
            col2_x = qx + 10 + col1_chars * 8;
            col3_x = qx + 10 + (col1_chars + col2_chars) * 8;

            /* Column headers */
            Draw_String(cbx, col1_x, dy, "Name");
            Draw_String(cbx, col2_x, dy, "%");
            Draw_String(cbx, col3_x, dy, "Status");
            dy += row_h;

            /* Scroll so selection is visible */
            if (g_quest_selected_index < g_quest_scroll) g_quest_scroll = g_quest_selected_index;
            if (g_quest_selected_index >= g_quest_scroll + max_rows) g_quest_scroll = g_quest_selected_index - max_rows + 1;
            if (g_quest_scroll < 0) g_quest_scroll = 0;

            for (i = 0; i < max_rows && g_quest_scroll + i < q_filtered_count; i++) {
                idx = q_filtered_indices[g_quest_scroll + i];
                sel = (g_quest_scroll + i == g_quest_selected_index);
                if (sel)
                    Draw_Fill(cbx, qx + 6, dy - 1, qw - 12, row_h, 224, 0.50f);
                status_str = q_status[idx];
                if (strcmp(status_str, "NotStarted") == 0 || strcmp(status_str, "0") == 0)
                    status_str = "Not Started";
                else if (strcmp(status_str, "InProgress") == 0 || strcmp(status_str, "1") == 0)
                    status_str = "In Progress";
                else if (strcmp(status_str, "Completed") == 0 || strcmp(status_str, "2") == 0)
                    status_str = "Completed";
                else
                    status_str = status_str[0] ? status_str : "-";
                q_strlcpy(status_display, status_str, sizeof(status_display));
                q_strlcpy(name_buf, q_name[idx] ? q_name[idx] : "-", sizeof(name_buf));
                if ((int)strlen(name_buf) > col1_chars - 2) {
                    name_buf[col1_chars - 3] = '.';
                    name_buf[col1_chars - 2] = '.';
                    name_buf[col1_chars - 1] = '\0';
                }
                Draw_String(cbx, col1_x, dy, name_buf);
                q_snprintf(name_buf, sizeof(name_buf), "%s%%", q_pct[idx] ? q_pct[idx] : "0");
                Draw_String(cbx, col2_x, dy, name_buf);
                Draw_String(cbx, col3_x, dy, status_display);
                dy += row_h;
            }
        } else {
            Draw_String(cbx, qx + 10, qy + 48, "No Quests Found");
        }

        /* Bottom info text centre-aligned */
        {
            const char* footer = "Home/End/PgUp=Filter  Arrows=Select  Enter=Start or Set tracker  Q=Close";
            int footer_len = (int)strlen(footer);
            Draw_String(cbx, qx + (qw - footer_len * 8) / 2, qy + qh - 20, footer);
        }
        /* Status message in bottom-right (e.g. "Starting quest..."), 10px higher than before */
        if (g_quest_status_frames > 0 && g_quest_status_message[0]) {
            int status_len = (int)strlen(g_quest_status_message);
            int status_x = qx + qw - (status_len * 8) - 8;
            int status_y = qy + qh - 36;  /* 10px higher than previous (qh - 26) */
            if (status_x < qx + 8) status_x = qx + 8;
            Draw_String(cbx, status_x, status_y, g_quest_status_message);
            g_quest_status_frames--;
            if (g_quest_status_frames <= 0) g_quest_status_message[0] = '\0';
        }
    }
}

/** Draw current quest tracker on HUD at top-left when user has set a tracked quest (Enter on In Progress in popup). Call from same HUD path as OQuake_STAR_DrawBeamedInStatus. */
void OQuake_STAR_DrawQuestTracker(cb_context_t* cbx) {
    if (!g_star_initialized || !cbx)
        return;
    if (!g_quest_tracker_id[0])
        return;

    {
        char buf[160];
        if (g_quest_tracker_name[0])
            q_snprintf(buf, sizeof(buf), "Quest: %.120s", g_quest_tracker_name);
        else
            q_strlcpy(buf, "Quest (tracked)", sizeof(buf));
        Draw_String(cbx, 8, 8, buf);  /* top-left of screen */
    }
}

void OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx) {
    extern int glheight;

    if (!g_star_initialized)
        return;
    /* Poll for async beam-in completion every frame so login state and "Beamed In" update
     * even when the inventory overlay is never opened. Face is correct because
     * OQuake_STAR_ShouldUseAnorakFace() returns the live value. */
    OQ_CheckAuthenticationComplete();

    if (!cbx)
        return;

    if (glheight <= 0)
        return;

    /* Quest tracker (when set from quest popup) above "Beamed In" */
    OQuake_STAR_DrawQuestTracker(cbx);

    const char* username = OQuake_STAR_GetUsername();
    char status[128];
    if (username && username[0]) {
        q_snprintf(status, sizeof(status), "Beamed In: %s", username);
    } else {
        q_strlcpy(status, "Beamed In: None", sizeof(status));
    }

    Draw_String(cbx, 8, glheight - 24, status);
}

void OQuake_STAR_DrawVersionStatus(cb_context_t* cbx) {
    extern int glwidth, glheight;
    const char* text = "OQUAKE " OQUAKE_VERSION " (BUILD " OQUAKE_BUILD ")";
    int text_w;
    int x;
    int y;

    if (!cbx || glwidth <= 0 || glheight <= 0)
        return;

    text_w = (int)strlen(text) * 8;
    x = glwidth - text_w - 8;
    y = glheight - 24;
    if (x < 8)
        x = 8;
    if (y < 8)
        y = 8;
    Draw_String(cbx, x, y, text);
}

void OQuake_STAR_DrawXpStatus(cb_context_t* cbx) {
    extern int glwidth, glheight;
    int xp = 0;
    char buf[64];
    int x, y;

    if (!cbx || glwidth <= 0 || glheight <= 0)
        return;
    if (!g_star_initialized || !g_star_beamed_in)
        return;
    if (!star_api_get_avatar_xp(&xp))
        return;
    q_snprintf(buf, sizeof(buf), "XP: %d", xp);
    /* Top right: same horizontal alignment as version, a bit below top edge */
    x = glwidth - (int)strlen(buf) * 8 - 8;
    y = 12;
    if (x < 8) x = 8;
    Draw_String(cbx, x, y, buf);
}

void OQuake_STAR_DrawToast(cb_context_t* cbx) {
    extern int glwidth, glheight;
    int len, x, y;

    if (!cbx || glwidth <= 0 || glheight <= 0 || g_oq_toast_frames <= 0 || !g_oq_toast_message[0])
        return;
    len = (int)strlen(g_oq_toast_message);
    if (len <= 0) return;
    x = (glwidth - len * 8) / 2;
    if (x < 8) x = 8;
    y = 12;
    Draw_String(cbx, x, y, g_oq_toast_message);
}

int OQuake_STAR_ShouldUseAnorakFace(void) {
    /* Return live result so the engine always sees current state (e.g. after async beam-in) */
    return g_star_initialized && OQ_ShouldUseAnorakFace();
}

const char* OQuake_STAR_GetUsername(void) {
    if (g_star_initialized && g_star_username[0])
        return g_star_username;
    return NULL;
}
