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
#ifdef _WIN32
#include <windows.h>
#else
#include <sys/stat.h>
#endif

#ifndef STAR_API_HAS_SEND_ITEM
/* Forward declare send-item API when using an older star_api.h. Link with updated star_api.lib. */
star_api_result_t star_api_send_item_to_avatar(const char* target_username_or_avatar_id, const char* item_name, int quantity, const char* item_id);
star_api_result_t star_api_send_item_to_clan(const char* clan_name_or_target, const char* item_name, int quantity, const char* item_id);
#endif

/* Forward declare callbacks so they can be used before their definitions. */
static void OQ_OnSendItemDone(void* user_data);
static void OQ_OnInventoryDone(void* user_data);

static qboolean g_star_debug_logging = false;

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
static int g_star_console_registered = 0;
static char g_star_username[64] = {0};
static char g_json_config_path[512] = {0};
/* Last pickup synced to STAR (for star lastpickup). */
static char g_star_last_pickup_name[256] = {0};
static char g_star_last_pickup_desc[512] = {0};
static char g_star_last_pickup_type[64] = {0};
static qboolean g_star_has_last_pickup = false;

/* key binding helpers from keys.c */
extern char *keybindings[MAX_KEYS];
extern qboolean keydown[MAX_KEYS];
extern int Key_StringToKeynum(const char *str);
extern void Key_SetBinding(int keynum, const char *binding);

cvar_t oasis_star_anorak_face = {"oasis_star_anorak_face", "0", 0}; /* Runtime state - not archived */
cvar_t oasis_star_beam_face = {"oasis_star_beam_face", "1", CVAR_ARCHIVE};
cvar_t oquake_star_config_file = {"oquake_star_config_file", "json", CVAR_ARCHIVE}; /* "json" or "cfg" - which config file to use */
cvar_t oquake_star_api_url = {"oquake_star_api_url", "https://star-api.oasisplatform.world/api", CVAR_ARCHIVE};
cvar_t oquake_oasis_api_url = {"oquake_oasis_api_url", "https://api.oasisplatform.world", CVAR_ARCHIVE};
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

enum {
    OQ_TAB_KEYS = 0,
    OQ_TAB_POWERUPS = 1,
    OQ_TAB_WEAPONS = 2,
    OQ_TAB_AMMO = 3,
    OQ_TAB_ARMOR = 4,
    OQ_TAB_ITEMS = 5,
    OQ_TAB_COUNT = 6
};

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
    int quantity;  /* from API (stack size); use for display so reload shows correct total */
} oquake_inventory_entry_t;

static oquake_inventory_entry_t g_inventory_entries[OQ_MAX_INVENTORY_ITEMS];
static int g_inventory_count = 0;
static oquake_inventory_entry_t g_local_inventory_entries[OQ_MAX_INVENTORY_ITEMS];
static qboolean g_local_inventory_synced[OQ_MAX_INVENTORY_ITEMS];
static int g_local_inventory_count = 0;
/* Mirror of local entries for star_sync layer (sync layer updates .synced) */
static star_sync_local_item_t g_star_sync_local_items[OQ_MAX_INVENTORY_ITEMS];
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
static unsigned int g_inventory_event_seq = 0;
static qboolean g_inventory_popup_input_captured = false;
static qboolean g_inventory_send_bindings_captured = false;
static char g_inventory_saved_up_bind[128];
static char g_inventory_saved_down_bind[128];
static char g_inventory_saved_left_bind[128];
static char g_inventory_saved_right_bind[128];
static char g_inventory_saved_all_binds[MAX_KEYS][128];
static int star_initialized(void);
static int OQ_ItemMatchesTab(const oquake_inventory_entry_t* item, int tab);
static void OQ_RefreshInventoryCache(void);
static void OQ_StartInventorySyncIfNeeded(void);
static void OQ_RefreshOverlayFromClient(void);
static void OQ_AppendLocalToDisplay(void);
static void OQ_ClampSelection(int filtered_count);
static void OQ_ApplyBeamFacePreference(void);
static void OQ_CheckAndSyncLocalItem(int index);

enum {
    OQ_GROUP_MODE_COUNT = 0,
    OQ_GROUP_MODE_SUM = 1
};

/* Queue only: add to local list for display; sync starts in background straight away (OQ_StartInventorySyncIfNeeded) or when overlay opens. Client does has_item/add_item; minimal API - same pattern as ODOOM. */
static int OQ_AddInventoryUnlockIfMissing(const char* item_name, const char* description, const char* item_type)
{
    int i;
    int local_added = 0;

    if (!item_name || !item_name[0])
        return 0;

    for (i = 0; i < g_local_inventory_count; i++) {
        if (!strcmp(g_local_inventory_entries[i].name, item_name))
            return 0; /* Already in local list */
    }
    if (g_local_inventory_count < OQ_MAX_INVENTORY_ITEMS) {
        oquake_inventory_entry_t* dst = &g_local_inventory_entries[g_local_inventory_count++];
        q_strlcpy(dst->name, item_name, sizeof(dst->name));
        q_strlcpy(dst->description, description ? description : "", sizeof(dst->description));
        q_strlcpy(dst->item_type, item_type ? item_type : "Item", sizeof(dst->item_type));
        dst->id[0] = '\0';
        q_strlcpy(dst->game_source, "Quake", sizeof(dst->game_source));
        dst->quantity = 1;
        g_local_inventory_synced[g_local_inventory_count - 1] = false;
        local_added = 1;
    }

    return local_added;
}

/* Queue only: add to local list; sync starts in background. Sync sends base name (e.g. Shells) with quantity=1, stack=true so the API increments Quantity on the existing item or creates a new one. */
static int OQ_AddInventoryEvent(const char* item_prefix, const char* description, const char* item_type)
{
    char item_name[128];
    int local_added = 0;
    if (!item_prefix || !item_prefix[0])
        return 0;
    /* Seed so each run gets a different range (avoids reusing _000001 from a previous session). time + clock + rand so same second still differs. */
    if (g_inventory_event_seq == 0) {
        srand((unsigned int)time(NULL));
        g_inventory_event_seq = (unsigned int)(((unsigned long long)time(NULL) * 1000ULL + (unsigned long long)(clock() % 1000) + (unsigned long long)(rand() % 1000)) % 1000000ULL);
        if (g_inventory_event_seq == 0)
            g_inventory_event_seq = 1;
    }
    g_inventory_event_seq++;
    q_snprintf(item_name, sizeof(item_name), "%s_%06u", item_prefix, g_inventory_event_seq);

    if (g_local_inventory_count < OQ_MAX_INVENTORY_ITEMS) {
        oquake_inventory_entry_t* dst = &g_local_inventory_entries[g_local_inventory_count];
        q_strlcpy(dst->name, item_name, sizeof(dst->name));
        q_strlcpy(dst->description, description ? description : "", sizeof(dst->description));
        q_strlcpy(dst->item_type, item_type ? item_type : "Item", sizeof(dst->item_type));
        dst->id[0] = '\0';
        q_strlcpy(dst->game_source, "Quake", sizeof(dst->game_source));
        dst->quantity = 1;
        g_local_inventory_synced[g_local_inventory_count] = false;
        g_local_inventory_count++;
        local_added = 1;
    }

    return local_added;
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

static int OQ_ParsePickupDelta(const char* description)
{
    const char* plus;
    if (!description)
        return 1;
    plus = strrchr(description, '+');
    if (!plus || !plus[1] || !isdigit((unsigned char)plus[1]))
        return 1;
    return atoi(plus + 1);
}

static void OQ_AppendGameSourceTag(const oquake_inventory_entry_t* item, char* label, size_t label_size)
{
    const char* gs;
    if (!item || !label || label_size < 2)
        return;
    gs = item->game_source;
    if (!gs || !gs[0])
        return;
    if (strstr(gs, "Doom") || strstr(gs, "ODOOM") || strstr(gs, "doom"))
        q_strlcat(label, " (ODOOM)", label_size);
    else if (strstr(gs, "Quake") || strstr(gs, "OQUAKE") || strstr(gs, "quake"))
        q_strlcat(label, " (OQUAKE)", label_size);
}

static void OQ_GetGroupedDisplayInfo(const oquake_inventory_entry_t* item, char* label, size_t label_size, int* mode, int* value)
{
    const char* name = item ? item->name : "";
    const char* desc = item ? item->description : "";
    size_t len = strlen(name);
    *mode = OQ_GROUP_MODE_COUNT;
    *value = 1;

    /* Item names are short (e.g. "Shells", "Silver Key"); stack events add _NNNNNN locally. Strip suffix for display. */
    if (len >= 8 && name[len - 7] == '_') {
        int j;
        for (j = 0; j < 6; j++)
            if (name[len - 6 + j] < '0' || name[len - 6 + j] > '9') break;
        if (j == 6) {
            size_t base_len = len - 7;
            if (base_len >= label_size) base_len = label_size - 1;
            memcpy(label, name, base_len);
            label[base_len] = '\0';
        } else {
            q_strlcpy(label, name, label_size);
        }
    } else {
        q_strlcpy(label, name, label_size);
    }

    /* Stackable types: group by label and sum. Prefer parsed delta for ammo so "Shells pickup +25" shows 25; use quantity for API/armor. */
    if (!strcmp(label, "Shells") || !strcmp(label, "Nails") || !strcmp(label, "Rockets") || !strcmp(label, "Cells")) {
        *mode = OQ_GROUP_MODE_SUM;
        { int parsed = OQ_ParsePickupDelta(desc); *value = (parsed > 0) ? parsed : ((item && item->quantity > 0) ? item->quantity : 1); }
    } else if (!strcmp(label, "Green Armor") || !strcmp(label, "Yellow Armor") || !strcmp(label, "Red Armor") || !strcmp(label, "Health")) {
        *mode = OQ_GROUP_MODE_SUM;
        *value = (item && item->quantity > 0) ? item->quantity : 1;
    } else if (!strcmp(label, "Silver Key") || !strcmp(label, "Gold Key")) {
        *mode = OQ_GROUP_MODE_SUM;
        *value = (item && item->quantity > 0) ? item->quantity : 1;
    }

    OQ_AppendGameSourceTag(item, label, label_size);
}

static int OQ_BuildGroupedRows(
    int* out_rep_indices, char out_labels[][OQ_GROUP_LABEL_MAX], int* out_modes, int* out_values, qboolean* out_pending, int max_rows)
{
    int filtered_indices[OQ_MAX_INVENTORY_ITEMS];
    int filtered_count;
    int group_count = 0;
    int i;
    int row_api_sum[OQ_MAX_INVENTORY_ITEMS];
    int row_local_sum[OQ_MAX_INVENTORY_ITEMS];

    filtered_count = OQ_BuildFilteredIndices(filtered_indices, OQ_MAX_INVENTORY_ITEMS);
    for (i = 0; i < max_rows; i++) {
        row_api_sum[i] = 0;
        row_local_sum[i] = 0;
    }
    for (i = 0; i < filtered_count; i++) {
        int item_idx = filtered_indices[i];
        int row;
        char label[OQ_GROUP_LABEL_MAX];
        int mode;
        int value;
        const oquake_inventory_entry_t* ent = &g_inventory_entries[item_idx];
        OQ_GetGroupedDisplayInfo(ent, label, sizeof(label), &mode, &value);
        for (row = 0; row < group_count; row++) {
            if (out_modes[row] == mode && !strcmp(out_labels[row], label))
                break;
        }
        if (row == group_count) {
            if (group_count >= max_rows)
                break;
            out_rep_indices[group_count] = item_idx;
            out_modes[group_count] = mode;
            out_values[group_count] = 0;
            out_pending[group_count] = false;
            q_strlcpy(out_labels[group_count], label, OQ_GROUP_LABEL_MAX);
            group_count++;
        }
        /* Track API vs local so we show max (avoid double-count when both API and local have same label). */
        if (ent->id[0] != '\0')
            row_api_sum[row] += value;
        else
            row_local_sum[row] += value;
        out_values[row] = (row_api_sum[row] > row_local_sum[row]) ? row_api_sum[row] : row_local_sum[row];
        if (out_values[row] < 1)
            out_values[row] = 1;
        if (out_pending[row] == false) {
            int j;
            for (j = 0; j < g_local_inventory_count; j++) {
                if (!strcmp(g_local_inventory_entries[j].name, g_inventory_entries[item_idx].name) && !g_local_inventory_synced[j]) {
                    out_pending[row] = true;
                    break;
                }
            }
        }
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

static void OQ_UseSelectedItem(void)
{
    oquake_inventory_entry_t* item = OQ_GetSelectedItem();
    if (!item) {
        q_strlcpy(g_inventory_status, "No item selected.", sizeof(g_inventory_status));
        return;
    }

    star_api_queue_use_item(item->name, "inventory_overlay");
    if (star_api_flush_use_item_jobs() == STAR_API_SUCCESS) {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Used item: %s", item->name);
        OQ_RefreshOverlayFromClient();
    } else {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Use failed: %s", star_api_get_last_error());
    }
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
        default: return "Items";
    }
}

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

static int OQ_ItemMatchesTab(const oquake_inventory_entry_t* item, int tab) {
    const char* type = item ? item->item_type : NULL;
    const char* name = item ? item->name : NULL;
    /* API often returns "KeyItem" or "Miscellaneous"; derive category from name so items show in correct tab. */
    int is_key = OQ_ContainsNoCase(type, "key") || (name && (strstr(name, "Key") != NULL || strstr(name, "key") != NULL));
    int is_powerup = OQ_ContainsNoCase(type, "powerup") || (name && (strstr(name, "Megahealth") != NULL || strstr(name, "Ring") != NULL || strstr(name, "Pentagram") != NULL || strstr(name, "Biosuit") != NULL || strstr(name, "Quad") != NULL));
    int is_weapon = OQ_ContainsNoCase(type, "weapon") || (name && (strstr(name, "Shotgun") != NULL || strstr(name, "Nailgun") != NULL || strstr(name, "Launcher") != NULL || strstr(name, "Lightning") != NULL));
    int is_ammo = OQ_ContainsNoCase(type, "ammo") || (name && (strstr(name, "Shells") != NULL || strstr(name, "Nails") != NULL || strstr(name, "Rockets") != NULL || strstr(name, "Cells") != NULL));
    int is_armor = OQ_ContainsNoCase(type, "armor") || (name && (strstr(name, "Armor") != NULL || strstr(name, "armor") != NULL));

    switch (tab) {
        case OQ_TAB_KEYS: return is_key;
        case OQ_TAB_POWERUPS: return is_powerup;
        case OQ_TAB_WEAPONS: return is_weapon;
        case OQ_TAB_AMMO: return is_ammo;
        case OQ_TAB_ARMOR: return is_armor;
        default: return !is_key && !is_powerup && !is_weapon && !is_ammo && !is_armor;
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
        Con_Printf("Logged in (beamin). Cross-game assets enabled.\n");
        g_inventory_last_refresh = 0.0;
        /* Start inventory fetch in background so popup has data when opened. */
        if (!star_sync_inventory_in_progress()) {
            q_strlcpy(g_inventory_status, "Syncing...", sizeof(g_inventory_status));
            int l;
            for (l = 0; l < g_local_inventory_count; l++) {
                star_sync_local_item_t* d = &g_star_sync_local_items[l];
                oquake_inventory_entry_t* s = &g_local_inventory_entries[l];
                q_strlcpy(d->name, s->name, sizeof(d->name));
                q_strlcpy(d->description, s->description, sizeof(d->description));
                q_strlcpy(d->game_source, "Quake", sizeof(d->game_source));
                q_strlcpy(d->item_type, s->item_type, sizeof(d->item_type));
                d->nft_id[0] = '\0';
                { int pq = OQ_ParsePickupDelta(s->description); d->quantity = (pq > 0) ? pq : 1; }
                d->synced = g_local_inventory_synced[l] ? 1 : 0;
            }
            star_sync_inventory_start(g_star_sync_local_items, g_local_inventory_count, "Quake", OQ_OnInventoryDone, NULL);
        }
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

static void OQ_ProcessInventoryResult(star_item_list_t* list, star_api_result_t result, const char* api_error);

/** Called from main thread by star_sync_pump() when inventory refresh completes. */
static void OQ_OnInventoryDone(void* user_data) {
    star_item_list_t* list = NULL;
    star_api_result_t result = STAR_API_ERROR_NOT_INITIALIZED;
    char api_error_buf[256] = {0};
    (void)user_data;
    if (!star_sync_inventory_get_result(&list, &result, api_error_buf, sizeof(api_error_buf))) {
        Con_Printf("OQuake: inventory sync callback ran but no result available (get_result=0).\n");
        return;
    }
    {
        int add_calls = star_sync_inventory_get_last_add_item_calls();
        if (result == STAR_API_SUCCESS)
            Con_Printf("OQuake: inventory sync succeeded (add_item_calls=%d).\n", add_calls);
        else
            Con_Printf("OQuake: inventory sync failed: %s\n", api_error_buf[0] ? api_error_buf : "unknown error");
        if (add_calls == 0 && result == STAR_API_SUCCESS)
            Con_Printf("OQuake: add_item_calls=0 means rebuild with updated star_sync.c (sync add_item path) so pickups reach the API.\n");
    }
    OQ_ProcessInventoryResult(list, result, api_error_buf[0] ? api_error_buf : NULL);
    if (list)
        star_api_free_item_list(list);
    star_sync_inventory_clear_result();
}

static void OQ_CheckAuthenticationComplete(void) {
    star_sync_pump();
}

static void OQ_ProcessInventoryResult(star_item_list_t* list, star_api_result_t result, const char* api_error) {
    size_t i;
    int remote_ok = (result == STAR_API_SUCCESS && list != NULL);
    int pending_local = 0;
    char remote_item_names[OQ_MAX_INVENTORY_ITEMS][256];
    int remote_item_count = 0;

    /* Copy back synced flags from sync layer */
    {
        int l;
        for (l = 0; l < g_local_inventory_count; l++)
            g_local_inventory_synced[l] = (qboolean)g_star_sync_local_items[l].synced;
    }
    g_inventory_count = 0;

    if (remote_ok && list && list->count > 0) {
        for (i = 0; i < list->count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS && remote_item_count < OQ_MAX_INVENTORY_ITEMS; i++) {
            oquake_inventory_entry_t* dst = &g_inventory_entries[g_inventory_count];
            q_strlcpy(dst->name, list->items[i].name, sizeof(dst->name));
            q_strlcpy(dst->description, list->items[i].description, sizeof(dst->description));
            q_strlcpy(dst->item_type, list->items[i].item_type, sizeof(dst->item_type));
            q_strlcpy(dst->id, list->items[i].id, sizeof(dst->id));
            q_strlcpy(dst->game_source, list->items[i].game_source, sizeof(dst->game_source));
            dst->quantity = list->items[i].quantity > 0 ? list->items[i].quantity : 1;
            g_inventory_count++;
            q_strlcpy(remote_item_names[remote_item_count], list->items[i].name, sizeof(remote_item_names[remote_item_count]));
            remote_item_count++;
            {
                int l;
                for (l = 0; l < g_local_inventory_count; l++) {
                    if (!strcmp(g_local_inventory_entries[l].name, list->items[i].name)) {
                        g_local_inventory_synced[l] = true;
                        break;
                    }
                }
            }
        }
    }

    {
        int l;
        for (l = 0; l < g_local_inventory_count; l++) {
            if (!g_local_inventory_synced[l]) {
                int found_in_remote = 0;
                if (remote_ok) {
                    int r;
                    for (r = 0; r < remote_item_count; r++) {
                        if (!strcmp(g_local_inventory_entries[l].name, remote_item_names[r])) {
                            found_in_remote = 1;
                            break;
                        }
                    }
                }
                if (found_in_remote)
                    g_local_inventory_synced[l] = true;
                /* Sync layer already did has_item/add_item; no extra star_api_has_item here. */
            }
        }
    }

    for (i = 0; i < (size_t)g_local_inventory_count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS; i++) {
        int j, exists = 0;
        for (j = 0; j < g_inventory_count; j++) {
            if (!strcmp(g_inventory_entries[j].name, g_local_inventory_entries[i].name)) {
                exists = 1;
                break;
            }
        }
        if (exists)
            continue;
        if (g_local_inventory_synced[i]) {
            int in_remote = 0;
            if (remote_ok) {
                int r;
                for (r = 0; r < remote_item_count; r++) {
                    if (!strcmp(g_local_inventory_entries[i].name, remote_item_names[r])) {
                        in_remote = 1;
                        break;
                    }
                }
            }
            if (in_remote)
                continue;
        }
        {
            oquake_inventory_entry_t* dst = &g_inventory_entries[g_inventory_count];
            q_strlcpy(dst->name, g_local_inventory_entries[i].name, sizeof(dst->name));
            q_strlcpy(dst->description, g_local_inventory_entries[i].description, sizeof(dst->description));
            q_strlcpy(dst->item_type, g_local_inventory_entries[i].item_type, sizeof(dst->item_type));
            dst->id[0] = '\0';
            q_strlcpy(dst->game_source, "Quake", sizeof(dst->game_source));
            dst->quantity = 1;
            g_inventory_count++;
        }
    }

    {
        int l, write_idx = 0;
        for (l = 0; l < g_local_inventory_count; l++) {
            if (!g_local_inventory_synced[l]) {
                if (write_idx != l) {
                    g_local_inventory_entries[write_idx] = g_local_inventory_entries[l];
                    g_local_inventory_synced[write_idx] = g_local_inventory_synced[l];
                }
                write_idx++;
            }
        }
        g_local_inventory_count = write_idx;
    }
    pending_local = g_local_inventory_count;

    if (g_inventory_count == 0) {
        if (remote_ok) {
            q_strlcpy(g_inventory_status, "STAR inventory is empty.", sizeof(g_inventory_status));
        } else if (api_error) {
            char error_msg[128];
            q_strlcpy(error_msg, api_error, sizeof(error_msg));
            if (strlen(error_msg) > 80) {
                error_msg[77] = '.'; error_msg[78] = '.'; error_msg[79] = '.'; error_msg[80] = 0;
            }
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR API error: %s", error_msg);
        } else {
            q_strlcpy(g_inventory_status, "Inventory is empty.", sizeof(g_inventory_status));
        }
    } else if (remote_ok) {
        if (pending_local > 0)
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Synced (%d items), %d pending", g_inventory_count, pending_local);
        else
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Synced (%d items)", g_inventory_count);
    } else if (star_initialized()) {
        if (api_error) {
            char error_msg[128];
            q_strlcpy(error_msg, api_error, sizeof(error_msg));
            if (strlen(error_msg) > 80) {
                error_msg[77] = '.'; error_msg[78] = '.'; error_msg[79] = '.'; error_msg[80] = 0;
            }
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR API error: %s (showing local: %d items)", error_msg, g_inventory_count);
        } else {
            q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR API unavailable; showing local inventory (%d items)", g_inventory_count);
        }
    } else {
        q_strlcpy(g_inventory_status, "Offline - use STAR BEAMIN", sizeof(g_inventory_status));
    }
    if (api_error && api_error[0] && result != STAR_API_SUCCESS)
        Con_Printf("OQuake: Failed to load STAR inventory: %s\n", api_error);
    g_inventory_last_refresh = realtime;
    {
        int rep_indices[OQ_MAX_INVENTORY_ITEMS];
        char labels[OQ_MAX_INVENTORY_ITEMS][OQ_GROUP_LABEL_MAX];
        int modes[OQ_MAX_INVENTORY_ITEMS];
        int values[OQ_MAX_INVENTORY_ITEMS];
        qboolean pending[OQ_MAX_INVENTORY_ITEMS];
        OQ_ClampSelection(OQ_BuildGroupedRows(rep_indices, labels, modes, values, pending, OQ_MAX_INVENTORY_ITEMS));
    }
}

static void OQ_CheckInventoryRefreshComplete(void) {
    star_sync_pump();
}

/** Refresh overlay from client cache only (one get_inventory; client returns cache). Minimal API - same pattern as ODOOM. Call after send/use so overlay stays in sync. */
static void OQ_RefreshOverlayFromClient(void) {
    star_item_list_t* list = NULL;
    if (star_api_get_inventory(&list) != STAR_API_SUCCESS || !list) {
        if (!star_initialized())
            q_strlcpy(g_inventory_status, "Offline - use STAR BEAMIN", sizeof(g_inventory_status));
        return;
    }
    g_inventory_count = 0;
    size_t i;
    for (i = 0; i < list->count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS; i++) {
        oquake_inventory_entry_t* dst = &g_inventory_entries[g_inventory_count];
        q_strlcpy(dst->name, list->items[i].name, sizeof(dst->name));
        q_strlcpy(dst->description, list->items[i].description, sizeof(dst->description));
        q_strlcpy(dst->item_type, list->items[i].item_type, sizeof(dst->item_type));
        q_strlcpy(dst->id, list->items[i].id, sizeof(dst->id));
        q_strlcpy(dst->game_source, list->items[i].game_source, sizeof(dst->game_source));
        dst->quantity = list->items[i].quantity > 0 ? list->items[i].quantity : 1;
        g_inventory_count++;
        /* Mark matching local items as synced */
        {
            int l;
            for (l = 0; l < g_local_inventory_count; l++) {
                if (!strcmp(g_local_inventory_entries[l].name, list->items[i].name))
                    g_local_inventory_synced[l] = true;
            }
        }
    }
    star_api_free_item_list(list);
    OQ_AppendLocalToDisplay();
    g_inventory_last_refresh = realtime;
    if (g_inventory_count == 0)
        q_strlcpy(g_inventory_status, "STAR inventory is empty.", sizeof(g_inventory_status));
    else
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Synced (%d items)", g_inventory_count);
}

/** Append any local items not already in g_inventory_entries so pickups show immediately. */
static void OQ_AppendLocalToDisplay(void) {
    int i, j;
    for (i = 0; i < g_local_inventory_count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS; i++) {
        int exists = 0;
        for (j = 0; j < g_inventory_count; j++) {
            if (!strcmp(g_inventory_entries[j].name, g_local_inventory_entries[i].name)) {
                exists = 1;
                break;
            }
        }
        if (!exists) {
            oquake_inventory_entry_t* dst = &g_inventory_entries[g_inventory_count];
            q_strlcpy(dst->name, g_local_inventory_entries[i].name, sizeof(dst->name));
            q_strlcpy(dst->description, g_local_inventory_entries[i].description, sizeof(dst->description));
            q_strlcpy(dst->item_type, g_local_inventory_entries[i].item_type, sizeof(dst->item_type));
            dst->id[0] = '\0';
            q_strlcpy(dst->game_source, g_local_inventory_entries[i].game_source[0] ? g_local_inventory_entries[i].game_source : "Quake", sizeof(dst->game_source));
            dst->quantity = 1;
            g_inventory_count++;
        }
    }
}

/** If there are pending (unsynced) local items and no sync in progress, start inventory sync on a background thread. Call after pickups and from OQ_RefreshInventoryCache. */
static void OQ_StartInventorySyncIfNeeded(void) {
    if (star_sync_inventory_in_progress() || star_sync_auth_in_progress() || !star_initialized()) {
        /* Only log when we have local items we might want to sync, to avoid spam */
        if (g_local_inventory_count > 0) {
            int pending = 0;
            int l;
            for (l = 0; l < g_local_inventory_count; l++) { if (!g_local_inventory_synced[l]) { pending = 1; break; } }
            if (pending)
                Con_Printf("OQuake: sync skipped (in_progress=%d auth=%d initialized=%d)\n",
                    star_sync_inventory_in_progress(), star_sync_auth_in_progress(), star_initialized());
        }
        return;
    }
    {
        int pending = 0;
        int l;
        for (l = 0; l < g_local_inventory_count; l++) {
            if (!g_local_inventory_synced[l]) { pending = 1; break; }
        }
        if (!pending || g_local_inventory_count <= 0) {
            if (g_local_inventory_count > 0)
                Con_Printf("OQuake: sync skipped (no pending local items, count=%d)\n", g_local_inventory_count);
            return;
        }
        Con_Printf("OQuake: starting inventory sync (%d local items to push).\n", g_local_inventory_count);
        q_strlcpy(g_inventory_status, "Syncing...", sizeof(g_inventory_status));
        for (l = 0; l < g_local_inventory_count; l++) {
            star_sync_local_item_t* d = &g_star_sync_local_items[l];
            oquake_inventory_entry_t* s = &g_local_inventory_entries[l];
            q_strlcpy(d->name, s->name, sizeof(d->name));
            q_strlcpy(d->description, s->description, sizeof(d->description));
            q_strlcpy(d->game_source, "Quake", sizeof(d->game_source));
            q_strlcpy(d->item_type, s->item_type, sizeof(d->item_type));
            d->nft_id[0] = '\0';
            { int pq = OQ_ParsePickupDelta(s->description); d->quantity = (pq > 0) ? pq : 1; }
            d->synced = g_local_inventory_synced[l] ? 1 : 0;
        }
        star_sync_inventory_start(g_star_sync_local_items, g_local_inventory_count, "Quake", OQ_OnInventoryDone, NULL);
    }
}

/** Start background inventory sync only when we have pending (unsynced) local items; otherwise refresh from client cache. Same pattern as ODOOM - minimal API calls. */
static void OQ_RefreshInventoryCache(void) {
    if (star_sync_inventory_in_progress())
        return;
    if (star_sync_auth_in_progress()) {
        q_strlcpy(g_inventory_status, "Authenticating...", sizeof(g_inventory_status));
        return;
    }
    if (!star_initialized()) {
        g_inventory_count = 0;
        q_strlcpy(g_inventory_status, "Offline - use STAR BEAMIN", sizeof(g_inventory_status));
        return;
    }
    /* Always refresh overlay first (server cache + local pickups) so opening the popup shows current qty including just-picked items. */
    OQ_RefreshOverlayFromClient();
    /* Then start sync if we have unsynced local items. */
    OQ_StartInventorySyncIfNeeded();
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
    
    fprintf(f, "{\n");
    fprintf(f, "  \"config_file\": \"%s\",\n", config_file && config_file[0] ? config_file : "json");
    fprintf(f, "  \"star_api_url\": \"%s\",\n", star_url ? star_url : "");
    fprintf(f, "  \"oasis_api_url\": \"%s\",\n", oasis_url ? oasis_url : "");
    fprintf(f, "  \"beam_face\": %d,\n", beam_face);
    fprintf(f, "  \"stack_armor\": %s,\n", (s_armor && atoi(s_armor)) ? "1" : "0");
    fprintf(f, "  \"stack_weapons\": %s,\n", (s_weapons && atoi(s_weapons)) ? "1" : "0");
    fprintf(f, "  \"stack_powerups\": %s,\n", (s_powerups && atoi(s_powerups)) ? "1" : "0");
    fprintf(f, "  \"stack_keys\": %s,\n", (s_keys && atoi(s_keys)) ? "1" : "0");
    fprintf(f, "  \"stack_sigils\": %s\n", (s_sigils && atoi(s_sigils)) ? "1" : "0");
    fprintf(f, "}\n");
    
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

    if (!g_star_console_registered) {
        Cmd_AddCommand("star", OQuake_STAR_Console_f);
        Cmd_AddCommand("oasis_inventory_toggle", OQ_InventoryToggle_f);
        Cmd_AddCommand("oasis_inventory_prevtab", OQ_InventoryPrevTab_f);
        Cmd_AddCommand("oasis_inventory_nexttab", OQ_InventoryNextTab_f);
        Cmd_AddCommand("oasis_reload_config", OQ_ReloadConfig_f);
        g_star_console_registered = 1;
        /* Default: I key opens OASIS inventory if not already bound */
        {
            int kn = Key_StringToKeynum("i");
            if (kn >= 0 && (!keybindings[kn] || !keybindings[kn][0]))
                Key_SetBinding(kn, "oasis_inventory_toggle");
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
        } else if (!found_json && found_json_path[0]) {
            /* JSON will be created, store the path */
            q_strlcpy(g_json_config_path, found_json_path, sizeof(g_json_config_path));
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
    OQ_StartInventorySyncIfNeeded();
}

void OQuake_STAR_OnItemsChangedEx(unsigned int old_items, unsigned int new_items, int in_real_game)
{
    unsigned int gained = new_items & ~old_items;
    int added = 0;

    if (!in_real_game || !g_star_initialized)
        return;
    if (gained == 0)
        return;

    if (gained & IT_SHOTGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Shotgun", "Shotgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Shotgun", "Shotgun discovered", "Weapon");
    if (gained & IT_SUPER_SHOTGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Super Shotgun", "Super Shotgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Super Shotgun", "Super Shotgun discovered", "Weapon");
    if (gained & IT_NAILGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Nailgun", "Nailgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Nailgun", "Nailgun discovered", "Weapon");
    if (gained & IT_SUPER_NAILGUN) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Super Nailgun", "Super Nailgun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Super Nailgun", "Super Nailgun discovered", "Weapon");
    if (gained & IT_GRENADE_LAUNCHER) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Grenade Launcher", "Grenade Launcher discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Grenade Launcher", "Grenade Launcher discovered", "Weapon");
    if (gained & IT_ROCKET_LAUNCHER) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Rocket Launcher", "Rocket Launcher discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Rocket Launcher", "Rocket Launcher discovered", "Weapon");
    if (gained & IT_LIGHTNING) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Lightning Gun", "Lightning Gun discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Lightning Gun", "Lightning Gun discovered", "Weapon");
    if (gained & IT_SUPER_LIGHTNING) added += OQ_StackWeapons() ? OQ_AddInventoryEvent("Super Lightning", "Super Lightning discovered", "Weapon") : OQ_AddInventoryUnlockIfMissing("Super Lightning", "Super Lightning discovered", "Weapon");

    if (gained & IT_ARMOR1) added += OQ_StackArmor() ? OQ_AddInventoryEvent("Green Armor", "Green Armor +1", "Armor") : OQ_AddInventoryUnlockIfMissing("Green Armor", "Green Armor", "Armor");
    if (gained & IT_ARMOR2) added += OQ_StackArmor() ? OQ_AddInventoryEvent("Yellow Armor", "Yellow Armor +1", "Armor") : OQ_AddInventoryUnlockIfMissing("Yellow Armor", "Yellow Armor", "Armor");
    if (gained & IT_ARMOR3) added += OQ_StackArmor() ? OQ_AddInventoryEvent("Red Armor", "Red Armor +1", "Armor") : OQ_AddInventoryUnlockIfMissing("Red Armor", "Red Armor", "Armor");

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
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR updated: %d new pickup(s)", added);
        OQ_AppendLocalToDisplay();
        OQ_StartInventorySyncIfNeeded();
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
    (void)old_health;
    (void)new_health;
    (void)old_armor;
    (void)new_armor;
    if (!in_real_game || !g_star_initialized)
        return;
    if (new_shells > old_shells) {
        q_snprintf(desc, sizeof(desc), "Shells pickup +%d", new_shells - old_shells);
        added += OQ_AddInventoryEvent("Shells", desc, "Ammo");
    }
    if (new_nails > old_nails) {
        q_snprintf(desc, sizeof(desc), "Nails pickup +%d", new_nails - old_nails);
        added += OQ_AddInventoryEvent("Nails", desc, "Ammo");
    }
    if (new_rockets > old_rockets) {
        q_snprintf(desc, sizeof(desc), "Rockets pickup +%d", new_rockets - old_rockets);
        added += OQ_AddInventoryEvent("Rockets", desc, "Ammo");
    }
    if (new_cells > old_cells) {
        q_snprintf(desc, sizeof(desc), "Cells pickup +%d", new_cells - old_cells);
        added += OQ_AddInventoryEvent("Cells", desc, "Ammo");
    }
    if (added > 0) {
        Con_Printf("OQuake: %d pickup event(s) recorded (shells/nails/rockets/cells), starting sync.\n", added);
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR updated: %d pickup event(s)", added);
        OQ_AppendLocalToDisplay();
        /* Display already has new items from AppendLocalToDisplay; overlay reads g_inventory_entries each frame so it will update. Do not call get_inventory here or it can overwrite with stale cache before sync completes. */
        OQ_StartInventorySyncIfNeeded();
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

int OQuake_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name) {
    if (!g_star_initialized || !required_key_name)
        return 0;
    /* star_api_has_item uses client cache first, then API if needed. */
    if (star_api_has_item(required_key_name)) {
        printf("OQuake STAR API: Door opened with cross-game key: %s\n", required_key_name);
        star_sync_use_item_start(required_key_name, door_targetname ? door_targetname : "quake_door", OQ_OnUseItemDone, NULL);
        return 1;
    }
    return 0;
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
        Con_Printf("  star pickup keycard <silver|gold> - Add OQuake key (dellams/anorak only)\n");
        Con_Printf("  star debug on|off|status - Toggle STAR debug logging\n");
        Con_Printf("  star send_avatar <user> <item_class> - Send item to avatar\n");
        Con_Printf("  star send_clan <clan> <item_class>   - Send item to clan\n");
        Con_Printf("  star beamin <username> <password> - Log in inside Quake\n");
        Con_Printf("  star beamed in <username> <password> - Alias for beamin\n");
        Con_Printf("  star beamin   - Log in using STAR_USERNAME/STAR_PASSWORD or API key\n");
        Con_Printf("  star beamout  - Log out / disconnect from STAR\n");
        Con_Printf("  star face on|off|status - Toggle beam-in face switch\n");
        Con_Printf("  star config        - Show current config (URLs, stack options)\n");
        Con_Printf("  star config save   - Write config to files now (also saved on exit)\n");
        Con_Printf("  star stack <armor|weapons|powerups|keys|sigils> <0|1> - Stack (1) or unlock (0)\n");
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
        if (argc < 4 || strcmp(Cmd_Argv(2), "keycard") != 0) {
            Con_Printf("Usage: star pickup keycard <silver|gold>\n");
            return;
        }
        if (!OQ_AllowPrivilegedCommands()) { Con_Printf("Only dellams or anorak can use star pickup keycard.\n"); return; }
        const char* color = Cmd_Argv(3);
        const char* name = NULL;
        const char* desc = NULL;
        if (strcmp(color, "silver") == 0) { name = OQUAKE_ITEM_SILVER_KEY; desc = get_key_description(name); }
        else if (strcmp(color, "gold") == 0) { name = OQUAKE_ITEM_GOLD_KEY; desc = get_key_description(name); }
        else { Con_Printf("Unknown keycard: %s. Use silver|gold.\n", color); return; }
        star_api_queue_add_item(name, desc, "Quake", "KeyItem", NULL, 1, 1);
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
                Con_Printf("  %s - %s (%s, %s)\n", g_inventory_entries[i].name, g_inventory_entries[i].description, g_inventory_entries[i].game_source, g_inventory_entries[i].item_type);
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
        star_api_queue_add_item(name, desc, "Quake", type, NULL, 1, 1);
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
        star_api_result_t r = star_api_create_boss_nft(name, desc, "Quake", "{}", nft_id);
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
        if (strcmp(Cmd_Argv(2), "on") == 0) { g_star_debug_logging = true; Con_Printf("STAR debug logging enabled.\n"); return; }
        if (strcmp(Cmd_Argv(2), "off") == 0) { g_star_debug_logging = false; Con_Printf("STAR debug logging disabled.\n"); return; }
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
    if (strcmp(sub, "config") == 0) {
        const char* save_arg = (argc >= 3) ? Cmd_Argv(2) : NULL;
        if (save_arg && strcmp(save_arg, "save") == 0) {
            OQ_SaveStarConfigToFiles();
            Con_Printf("Config saved to oasisstar.json and config.cfg (if paths found).\n");
            return;
        }
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
        Con_Printf("  STAR API URL: %s\n", star_url && star_url[0] ? star_url : "(default: https://star-api.oasisplatform.world/api)");
        Con_Printf("  OASIS API URL: %s\n", oasis_url && oasis_url[0] ? oasis_url : "(default: https://api.oasisplatform.world)");
        Con_Printf("  Username: %s\n", oquake_star_username.string && oquake_star_username.string[0] ? oquake_star_username.string : "(not set)");
        Con_Printf("  Password: %s\n", oquake_star_password.string && oquake_star_password.string[0] ? "***" : "(not set)");
        Con_Printf("  API Key: %s\n", oquake_star_api_key.string && oquake_star_api_key.string[0] ? "***" : "(not set)");
        Con_Printf("  Avatar ID: %s\n", oquake_star_avatar_id.string && oquake_star_avatar_id.string[0] ? oquake_star_avatar_id.string : "(not set)");
        Con_Printf("  Beam face: %s\n", oasis_star_beam_face.value > 0.5f ? "on" : "off");
        Con_Printf("  Stack (1) / Unlock (0) - ammo always stacks:\n");
        Con_Printf("    stack_armor:    %s\n", atoi(oquake_star_stack_armor.string) ? "1 (stack)" : "0 (unlock)");
        Con_Printf("    stack_weapons:  %s\n", atoi(oquake_star_stack_weapons.string) ? "1 (stack)" : "0 (unlock)");
        Con_Printf("    stack_powerups: %s\n", atoi(oquake_star_stack_powerups.string) ? "1 (stack)" : "0 (unlock)");
        Con_Printf("    stack_keys:     %s\n", atoi(oquake_star_stack_keys.string) ? "1 (stack)" : "0 (unlock)");
        Con_Printf("    stack_sigils:   %s (OQuake only)\n", atoi(oquake_star_stack_sigils.string) ? "1 (stack)" : "0 (unlock)");
        Con_Printf("\n");
        Con_Printf("To set: star stack <armor|weapons|powerups|keys|sigils> <0|1> (sigils = OQuake only)\n");
        Con_Printf("URLs: star seturl <url>   star setoasisurl <url>\n");
        Con_Printf("Config file: star configfile json|cfg\n");
        Con_Printf("To save now: star config save (also saved on exit)\n");
        Con_Printf("Auth: set oquake_star_username \"...\" or star beamin <user> <pass>\n");
        Con_Printf("\n");
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

    /* Check if background operations completed */
    OQ_CheckAuthenticationComplete();
    OQ_CheckInventoryRefreshComplete();
    
    OQ_PollInventoryHotkeys();

    if (!g_inventory_open || !cbx)
        return;

    /* Inventory only loads after beam-in and refreshes once when opened; no periodic 2s refresh. */

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
    Draw_String(cbx, panel_x + 6, panel_y + panel_h - 16, "Arrows=Select  E=Use  Z=Send Avatar  X=Send Clan  I=Toggle  O/P=Switch Tabs");

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
}

void OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx) {
    extern int glheight;

    /* Poll for async beam-in completion every frame so login state and "Beamed In" update
     * even when the inventory overlay is never opened. Face is correct because
     * OQuake_STAR_ShouldUseAnorakFace() returns the live value. */
    OQ_CheckAuthenticationComplete();

    if (!cbx)
        return;

    if (glheight <= 0)
        return;

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

int OQuake_STAR_ShouldUseAnorakFace(void) {
    /* Return live result so the engine always sees current state (e.g. after async beam-in) */
    return g_star_initialized && OQ_ShouldUseAnorakFace();
}

const char* OQuake_STAR_GetUsername(void) {
    if (g_star_initialized && g_star_username[0])
        return g_star_username;
    return NULL;
}
