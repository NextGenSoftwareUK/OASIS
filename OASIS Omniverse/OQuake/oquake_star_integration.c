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
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <ctype.h>

static star_api_config_t g_star_config;
static int g_star_initialized = 0;
static int g_star_console_registered = 0;
static char g_star_username[64] = {0};

/* key binding helpers from keys.c */
extern char *keybindings[MAX_KEYS];
extern qboolean keydown[MAX_KEYS];
extern int Key_StringToKeynum(const char *str);
extern void Key_SetBinding(int keynum, const char *binding);

cvar_t oasis_star_anorak_face = {"oasis_star_anorak_face", "0", CVAR_ARCHIVE};

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
} oquake_inventory_entry_t;

static oquake_inventory_entry_t g_inventory_entries[OQ_MAX_INVENTORY_ITEMS];
static int g_inventory_count = 0;
static oquake_inventory_entry_t g_local_inventory_entries[OQ_MAX_INVENTORY_ITEMS];
static int g_local_inventory_count = 0;
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
static int star_initialized(void);
static int OQ_ItemMatchesTab(const oquake_inventory_entry_t* item, int tab);
static void OQ_RefreshInventoryCache(void);
static void OQ_ClampSelection(int filtered_count);

enum {
    OQ_GROUP_MODE_COUNT = 0,
    OQ_GROUP_MODE_SUM = 1
};

static int OQ_AddInventoryUnlockIfMissing(const char* item_name, const char* description, const char* item_type)
{
    int i;
    int local_added = 0;
    int remote_added = 0;
    star_api_result_t result;

    if (!item_name || !item_name[0])
        return 0;

    for (i = 0; i < g_local_inventory_count; i++) {
        if (!strcmp(g_local_inventory_entries[i].name, item_name))
            break;
    }
    if (i == g_local_inventory_count && g_local_inventory_count < OQ_MAX_INVENTORY_ITEMS) {
        oquake_inventory_entry_t* dst = &g_local_inventory_entries[g_local_inventory_count++];
        q_strlcpy(dst->name, item_name, sizeof(dst->name));
        q_strlcpy(dst->description, description ? description : "", sizeof(dst->description));
        q_strlcpy(dst->item_type, item_type ? item_type : "Item", sizeof(dst->item_type));
        local_added = 1;
    }

    if (g_star_initialized && !star_api_has_item(item_name)) {
        result = star_api_add_item(item_name, description, "Quake", item_type);
        if (result == STAR_API_SUCCESS)
            remote_added = 1;
    }

    return local_added || remote_added;
}

static int OQ_AddInventoryEvent(const char* item_prefix, const char* description, const char* item_type)
{
    char item_name[128];
    int local_added = 0;
    int remote_added = 0;
    star_api_result_t result;
    if (!item_prefix || !item_prefix[0])
        return 0;
    g_inventory_event_seq++;
    q_snprintf(item_name, sizeof(item_name), "%s_%06u", item_prefix, g_inventory_event_seq);

    if (g_local_inventory_count < OQ_MAX_INVENTORY_ITEMS) {
        oquake_inventory_entry_t* dst = &g_local_inventory_entries[g_local_inventory_count++];
        q_strlcpy(dst->name, item_name, sizeof(dst->name));
        q_strlcpy(dst->description, description ? description : "", sizeof(dst->description));
        q_strlcpy(dst->item_type, item_type ? item_type : "Item", sizeof(dst->item_type));
        local_added = 1;
    }

    if (g_star_initialized) {
        result = star_api_add_item(item_name, description, "Quake", item_type);
        if (result == STAR_API_SUCCESS)
            remote_added = 1;
    }

    return local_added || remote_added;
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

static void OQ_GetGroupedDisplayInfo(const oquake_inventory_entry_t* item, char* label, size_t label_size, int* mode, int* value)
{
    const char* name = item ? item->name : "";
    const char* desc = item ? item->description : "";
    *mode = OQ_GROUP_MODE_COUNT;
    *value = 1;

    if (!strncmp(name, "quake_pickup_shells_", 20)) {
        q_strlcpy(label, "Shells", label_size);
        *mode = OQ_GROUP_MODE_SUM;
        *value = OQ_ParsePickupDelta(desc);
        return;
    }
    if (!strncmp(name, "quake_pickup_nails_", 19)) {
        q_strlcpy(label, "Nails", label_size);
        *mode = OQ_GROUP_MODE_SUM;
        *value = OQ_ParsePickupDelta(desc);
        return;
    }
    if (!strncmp(name, "quake_pickup_rockets_", 21)) {
        q_strlcpy(label, "Rockets", label_size);
        *mode = OQ_GROUP_MODE_SUM;
        *value = OQ_ParsePickupDelta(desc);
        return;
    }
    if (!strncmp(name, "quake_pickup_cells_", 19)) {
        q_strlcpy(label, "Cells", label_size);
        *mode = OQ_GROUP_MODE_SUM;
        *value = OQ_ParsePickupDelta(desc);
        return;
    }
    if (!strncmp(name, "quake_pickup_armor_", 19)) {
        q_strlcpy(label, "Armor", label_size);
        *mode = OQ_GROUP_MODE_SUM;
        *value = OQ_ParsePickupDelta(desc);
        return;
    }
    if (!strncmp(name, "quake_pickup_health_", 20)) {
        q_strlcpy(label, "Health", label_size);
        *mode = OQ_GROUP_MODE_SUM;
        *value = OQ_ParsePickupDelta(desc);
        return;
    }
    if (!strncmp(name, "quake_pickup_quad_", 18)) {
        q_strlcpy(label, "Quad Damage", label_size);
        return;
    }
    if (!strncmp(name, "quake_pickup_suit_", 18)) {
        q_strlcpy(label, "Biosuit", label_size);
        return;
    }
    if (!strncmp(name, "quake_pickup_invisibility_", 26)) {
        q_strlcpy(label, "Ring of Shadows", label_size);
        return;
    }
    if (!strncmp(name, "quake_pickup_invulnerability_", 28)) {
        q_strlcpy(label, "Pentagram of Protection", label_size);
        return;
    }
    if (!strncmp(name, "quake_pickup_megahealth_", 24)) {
        q_strlcpy(label, "Megahealth", label_size);
        return;
    }

    if (!strcmp(name, OQUAKE_ITEM_SILVER_KEY)) {
        q_strlcpy(label, "Silver Key", label_size);
        return;
    }
    if (!strcmp(name, OQUAKE_ITEM_GOLD_KEY)) {
        q_strlcpy(label, "Gold Key", label_size);
        return;
    }

    q_strlcpy(label, name, label_size);
}

static int OQ_BuildGroupedRows(
    int* out_rep_indices, char out_labels[][OQ_GROUP_LABEL_MAX], int* out_modes, int* out_values, int max_rows)
{
    int filtered_indices[OQ_MAX_INVENTORY_ITEMS];
    int filtered_count;
    int group_count = 0;
    int i;

    filtered_count = OQ_BuildFilteredIndices(filtered_indices, OQ_MAX_INVENTORY_ITEMS);
    for (i = 0; i < filtered_count; i++) {
        int item_idx = filtered_indices[i];
        int row;
        char label[OQ_GROUP_LABEL_MAX];
        int mode;
        int value;
        OQ_GetGroupedDisplayInfo(&g_inventory_entries[item_idx], label, sizeof(label), &mode, &value);
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
            q_strlcpy(out_labels[group_count], label, OQ_GROUP_LABEL_MAX);
            group_count++;
        }
        out_values[row] += value;
    }

    return group_count;
}

static int OQ_GetSelectedGroupInfo(int* out_rep_index, int* out_mode, int* out_value, char* out_label, size_t out_label_size)
{
    int rep_indices[OQ_MAX_INVENTORY_ITEMS];
    char labels[OQ_MAX_INVENTORY_ITEMS][OQ_GROUP_LABEL_MAX];
    int modes[OQ_MAX_INVENTORY_ITEMS];
    int values[OQ_MAX_INVENTORY_ITEMS];
    int grouped_count = OQ_BuildGroupedRows(rep_indices, labels, modes, values, OQ_MAX_INVENTORY_ITEMS);

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

    q_snprintf(
        g_inventory_status,
        sizeof(g_inventory_status),
        "Mock sent %d x '%s' to %s '%s'.",
        qty,
        item->name,
        send_kind,
        g_inventory_send_target);
    g_inventory_send_popup = OQ_SEND_POPUP_NONE;
}

static void OQ_UseSelectedItem(void)
{
    oquake_inventory_entry_t* item = OQ_GetSelectedItem();
    if (!item) {
        q_strlcpy(g_inventory_status, "No item selected.", sizeof(g_inventory_status));
        return;
    }

    if (star_api_use_item(item->name, "inventory_overlay")) {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Used item: %s", item->name);
        OQ_RefreshInventoryCache();
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
    int is_key = OQ_ContainsNoCase(type, "key") || OQ_ContainsNoCase(name, "key");
    int is_powerup = OQ_ContainsNoCase(type, "powerup");
    int is_weapon = OQ_ContainsNoCase(type, "weapon");
    int is_ammo = OQ_ContainsNoCase(type, "ammo");
    int is_armor = OQ_ContainsNoCase(type, "armor");

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

static void OQ_RefreshInventoryCache(void) {
    star_item_list_t* list = NULL;
    star_api_result_t result;
    size_t i;
    int remote_ok = 0;

    g_inventory_count = 0;
    q_strlcpy(g_inventory_status, "STAR inventory unavailable.", sizeof(g_inventory_status));

    if (star_initialized()) {
        result = star_api_get_inventory(&list);
        if (result == STAR_API_SUCCESS)
            remote_ok = 1;
    }

    if (remote_ok && list && list->count > 0) {
        for (i = 0; i < list->count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS; i++) {
            oquake_inventory_entry_t* dst = &g_inventory_entries[g_inventory_count];
            q_strlcpy(dst->name, list->items[i].name, sizeof(dst->name));
            q_strlcpy(dst->description, list->items[i].description, sizeof(dst->description));
            q_strlcpy(dst->item_type, list->items[i].item_type, sizeof(dst->item_type));
            g_inventory_count++;
        }
        star_api_free_item_list(list);
        list = NULL;
    }

    for (i = 0; i < (size_t)g_local_inventory_count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS; i++) {
        int j;
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
            g_inventory_count++;
        }
    }

    if (g_inventory_count == 0) {
        q_strlcpy(g_inventory_status, "Inventory is empty.", sizeof(g_inventory_status));
    } else if (remote_ok) {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR inventory synced (%d items)", g_inventory_count);
    } else if (star_initialized()) {
        q_snprintf(
            g_inventory_status, sizeof(g_inventory_status), "STAR API unavailable; showing local inventory (%d items)", g_inventory_count);
    } else {
        q_snprintf(
            g_inventory_status, sizeof(g_inventory_status), "Offline local inventory (%d items). Use: star beamin", g_inventory_count);
    }

    if (list)
        star_api_free_item_list(list);
    g_inventory_last_refresh = realtime;
    {
        int rep_indices[OQ_MAX_INVENTORY_ITEMS];
        char labels[OQ_MAX_INVENTORY_ITEMS][OQ_GROUP_LABEL_MAX];
        int modes[OQ_MAX_INVENTORY_ITEMS];
        int values[OQ_MAX_INVENTORY_ITEMS];
        OQ_ClampSelection(OQ_BuildGroupedRows(rep_indices, labels, modes, values, OQ_MAX_INVENTORY_ITEMS));
    }
}

static void OQ_InventoryToggle_f(void) {
    g_inventory_open = !g_inventory_open;
    if (g_inventory_open) {
        g_inventory_selected_row = 0;
        g_inventory_scroll_row = 0;
        g_inventory_send_popup = OQ_SEND_POPUP_NONE;
        OQ_RefreshInventoryCache();
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
    if (key_dest == key_menu || key_dest == key_message || key_dest == key_console)
        return;

    {
        int rep_indices[OQ_MAX_INVENTORY_ITEMS];
        char labels[OQ_MAX_INVENTORY_ITEMS][OQ_GROUP_LABEL_MAX];
        int modes[OQ_MAX_INVENTORY_ITEMS];
        int values[OQ_MAX_INVENTORY_ITEMS];
        grouped_count = OQ_BuildGroupedRows(rep_indices, labels, modes, values, OQ_MAX_INVENTORY_ITEMS);
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
            return;
        }
        if (OQ_KeyPressed(K_LEFTARROW) || OQ_KeyPressed(K_RIGHTARROW))
            g_inventory_send_button = 1 - g_inventory_send_button;
        if (OQ_KeyPressed(K_UPARROW) && g_inventory_send_quantity < available)
            g_inventory_send_quantity++;
        if (OQ_KeyPressed(K_DOWNARROW) && g_inventory_send_quantity > 1)
            g_inventory_send_quantity--;

        if (OQ_KeyPressed(K_ENTER) || OQ_KeyPressed(K_KP_ENTER)) {
            if (g_inventory_send_button == 0)
                OQ_SendSelectedItem();
            else
                g_inventory_send_popup = OQ_SEND_POPUP_NONE;
        }
        return;
    }

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

/* Forward declaration */
// static void OQ_DebugMode_f(void); // Temporarily disabled

void OQuake_STAR_Init(void) {
    star_api_result_t result;
    const char* username;
    const char* password;

    Cvar_RegisterVariable(&oasis_star_anorak_face);
    Cvar_SetValueQuick(&oasis_star_anorak_face, 0);

    if (!g_star_console_registered) {
        Cmd_AddCommand("star", OQuake_STAR_Console_f);
        Cmd_AddCommand("oasis_inventory_toggle", OQ_InventoryToggle_f);
        Cmd_AddCommand("oasis_inventory_prevtab", OQ_InventoryPrevTab_f);
        Cmd_AddCommand("oasis_inventory_nexttab", OQ_InventoryNextTab_f);
        g_star_console_registered = 1;
    }


    g_star_config.base_url = "https://star-api.oasisplatform.world/api";
    g_star_config.api_key = getenv("STAR_API_KEY");
    g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
    g_star_config.timeout_seconds = 10;

    result = star_api_init(&g_star_config);
    if (result != STAR_API_SUCCESS) {
        printf("OQuake STAR API: Failed to initialize: %s\n", star_api_get_last_error());
    } else {
        username = getenv("STAR_USERNAME");
        password = getenv("STAR_PASSWORD");
        if (username && password) {
            result = star_api_authenticate(username, password);
            if (result == STAR_API_SUCCESS) {
                g_star_initialized = 1;
                printf("OQuake STAR API: Authenticated. Cross-game keys enabled.\n");
            } else {
                printf("OQuake STAR API: SSO failed: %s\n", star_api_get_last_error());
            }
        } else if (g_star_config.api_key && g_star_config.avatar_id) {
            g_star_initialized = 1;
            printf("OQuake STAR API: Using API key. Cross-game keys enabled.\n");
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
    if (g_star_initialized) {
        star_api_cleanup();
        g_star_initialized = 0;
        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
        printf("OQuake STAR API: Cleaned up.\n");
    }
}

void OQuake_STAR_OnKeyPickup(const char* key_name) {
    if (!key_name)
        return;
    const char* desc = get_key_description(key_name);
    if (OQ_AddInventoryUnlockIfMissing(key_name, desc, "KeyItem")) {
        printf("OQuake STAR API: Added %s to cross-game inventory.\n", key_name);
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Collected: %s", key_name);
        OQ_RefreshInventoryCache();
    } else {
        printf("OQuake STAR API: Failed to add %s: %s\n", key_name, star_api_get_last_error());
    }
}

void OQuake_STAR_OnItemsChanged(unsigned int old_items, unsigned int new_items)
{
    unsigned int gained = new_items & ~old_items;
    int added = 0;

    if (gained == 0)
        return;

    if (gained & IT_SHOTGUN) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_shotgun", "Shotgun discovered", "Weapon");
    if (gained & IT_SUPER_SHOTGUN) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_super_shotgun", "Super Shotgun discovered", "Weapon");
    if (gained & IT_NAILGUN) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_nailgun", "Nailgun discovered", "Weapon");
    if (gained & IT_SUPER_NAILGUN) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_super_nailgun", "Super Nailgun discovered", "Weapon");
    if (gained & IT_GRENADE_LAUNCHER) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_grenade_launcher", "Grenade Launcher discovered", "Weapon");
    if (gained & IT_ROCKET_LAUNCHER) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_rocket_launcher", "Rocket Launcher discovered", "Weapon");
    if (gained & IT_LIGHTNING) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_lightning", "Lightning Gun discovered", "Weapon");
    if (gained & IT_SUPER_LIGHTNING) added += OQ_AddInventoryUnlockIfMissing("quake_weapon_super_lightning", "Super Lightning discovered", "Weapon");

    if (gained & IT_ARMOR1) added += OQ_AddInventoryUnlockIfMissing("quake_armor_green", "Green Armor acquired", "Armor");
    if (gained & IT_ARMOR2) added += OQ_AddInventoryUnlockIfMissing("quake_armor_yellow", "Yellow Armor acquired", "Armor");
    if (gained & IT_ARMOR3) added += OQ_AddInventoryUnlockIfMissing("quake_armor_red", "Red Armor acquired", "Armor");

    if (gained & IT_SUPERHEALTH) added += OQ_AddInventoryEvent("quake_pickup_megahealth", "Megahealth pickup", "Powerup");
    if (gained & IT_INVISIBILITY) added += OQ_AddInventoryEvent("quake_pickup_invisibility", "Ring of Shadows pickup", "Powerup");
    if (gained & IT_INVULNERABILITY) added += OQ_AddInventoryEvent("quake_pickup_invulnerability", "Pentagram of Protection pickup", "Powerup");
    if (gained & IT_SUIT) added += OQ_AddInventoryEvent("quake_pickup_suit", "Biosuit pickup", "Powerup");
    if (gained & IT_QUAD) added += OQ_AddInventoryEvent("quake_pickup_quad", "Quad Damage pickup", "Powerup");

    if (gained & IT_SIGIL1) added += OQ_AddInventoryUnlockIfMissing("quake_sigil_1", "Sigil Piece 1 acquired", "Artifact");
    if (gained & IT_SIGIL2) added += OQ_AddInventoryUnlockIfMissing("quake_sigil_2", "Sigil Piece 2 acquired", "Artifact");
    if (gained & IT_SIGIL3) added += OQ_AddInventoryUnlockIfMissing("quake_sigil_3", "Sigil Piece 3 acquired", "Artifact");
    if (gained & IT_SIGIL4) added += OQ_AddInventoryUnlockIfMissing("quake_sigil_4", "Sigil Piece 4 acquired", "Artifact");

    if (gained & IT_KEY1) OQuake_STAR_OnKeyPickup(OQUAKE_ITEM_SILVER_KEY);
    if (gained & IT_KEY2) OQuake_STAR_OnKeyPickup(OQUAKE_ITEM_GOLD_KEY);

    if (added > 0) {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR updated: %d new pickup(s)", added);
        OQ_RefreshInventoryCache();
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
    int added = 0;
    char desc[96];
    if (new_shells > old_shells) {
        q_snprintf(desc, sizeof(desc), "Shells pickup +%d", new_shells - old_shells);
        added += OQ_AddInventoryEvent("quake_pickup_shells", desc, "Ammo");
    }
    if (new_nails > old_nails) {
        q_snprintf(desc, sizeof(desc), "Nails pickup +%d", new_nails - old_nails);
        added += OQ_AddInventoryEvent("quake_pickup_nails", desc, "Ammo");
    }
    if (new_rockets > old_rockets) {
        q_snprintf(desc, sizeof(desc), "Rockets pickup +%d", new_rockets - old_rockets);
        added += OQ_AddInventoryEvent("quake_pickup_rockets", desc, "Ammo");
    }
    if (new_cells > old_cells) {
        q_snprintf(desc, sizeof(desc), "Cells pickup +%d", new_cells - old_cells);
        added += OQ_AddInventoryEvent("quake_pickup_cells", desc, "Ammo");
    }
    if (new_health > old_health) {
        q_snprintf(desc, sizeof(desc), "Health pickup +%d", new_health - old_health);
        added += OQ_AddInventoryEvent("quake_pickup_health", desc, "Consumable");
    }
    if (new_armor > old_armor) {
        q_snprintf(desc, sizeof(desc), "Armor pickup +%d", new_armor - old_armor);
        added += OQ_AddInventoryEvent("quake_pickup_armor", desc, "Armor");
    }

    if (added > 0) {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR updated: %d pickup event(s)", added);
        OQ_RefreshInventoryCache();
    }
}

int OQuake_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name) {
    if (!g_star_initialized || !required_key_name)
        return 0;
    if (star_api_has_item(required_key_name)) {
        printf("OQuake STAR API: Door opened with cross-game key: %s\n", required_key_name);
        star_api_use_item(required_key_name, door_targetname ? door_targetname : "quake_door");
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
        Con_Printf("  star has <item>     - Check if you have an item (e.g. silver_key)\n");
        Con_Printf("  star add <item> [desc] [type] - Add item\n");
        Con_Printf("  star use <item> [context]     - Use item\n");
        Con_Printf("  star pickup keycard <silver|gold> - Add OQuake key (convenience)\n");
        Con_Printf("  star beamin <username> <password> - Log in inside Quake\n");
        Con_Printf("  star beamed in <username> <password> - Alias for beamin\n");
        Con_Printf("  star beamin   - Log in using STAR_USERNAME/STAR_PASSWORD or API key\n");
        Con_Printf("  star beamout  - Log out / disconnect from STAR\n");
        Con_Printf("\n");
        return;
    }
    const char* sub = Cmd_Argv(1);
    if (strcmp(sub, "pickup") == 0) {
        if (argc < 4 || strcmp(Cmd_Argv(2), "keycard") != 0) {
            Con_Printf("Usage: star pickup keycard <silver|gold>\n");
            return;
        }
        const char* color = Cmd_Argv(3);
        const char* name = NULL;
        const char* desc = NULL;
        if (strcmp(color, "silver") == 0) { name = OQUAKE_ITEM_SILVER_KEY; desc = get_key_description(name); }
        else if (strcmp(color, "gold") == 0) { name = OQUAKE_ITEM_GOLD_KEY; desc = get_key_description(name); }
        else { Con_Printf("Unknown keycard: %s. Use silver|gold.\n", color); return; }
        star_api_result_t r = star_api_add_item(name, desc, "Quake", "KeyItem");
        if (r == STAR_API_SUCCESS) Con_Printf("Added %s to STAR inventory.\n", name);
        else Con_Printf("Failed: %s\n", star_api_get_last_error());
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
        star_item_list_t* list = NULL;
        star_api_result_t r = star_api_get_inventory(&list);
        if (r != STAR_API_SUCCESS) {
            Con_Printf("Failed to get inventory: %s\n", star_api_get_last_error());
            return;
        }
        if (!list || list->count == 0) { Con_Printf("Inventory is empty.\n"); if (list) star_api_free_item_list(list); return; }
        Con_Printf("STAR inventory (%u items):\n", (unsigned)list->count);
        for (size_t i = 0; i < list->count; i++) {
            Con_Printf("  %s - %s (%s, %s)\n", list->items[i].name, list->items[i].description, list->items[i].game_source, list->items[i].item_type);
        }
        star_api_free_item_list(list);
        return;
    }
    if (strcmp(sub, "has") == 0) {
        if (argc < 3) { Con_Printf("Usage: star has <item_name>\n"); return; }
        int has = star_api_has_item(Cmd_Argv(2));
        Con_Printf("Has '%s': %s\n", Cmd_Argv(2), has ? "yes" : "no");
        return;
    }
    if (strcmp(sub, "add") == 0) {
        if (argc < 3) { Con_Printf("Usage: star add <item_name> [description] [item_type]\n"); return; }
        const char* name = Cmd_Argv(2);
        const char* desc = argc > 3 ? Cmd_Argv(3) : "Added from console";
        const char* type = argc > 4 ? Cmd_Argv(4) : "Miscellaneous";
        star_api_result_t r = star_api_add_item(name, desc, "Quake", type);
        if (r == STAR_API_SUCCESS) Con_Printf("Added '%s' to STAR inventory.\n", name);
        else Con_Printf("Failed to add '%s': %s\n", name, star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "use") == 0) {
        if (argc < 3) { Con_Printf("Usage: star use <item_name> [context]\n"); return; }
        const char* ctx = argc > 3 ? Cmd_Argv(3) : "console";
        int ok = star_api_use_item(Cmd_Argv(2), ctx);
        Con_Printf("Use '%s' (context %s): %s\n", Cmd_Argv(2), ctx, ok ? "ok" : "failed");
        if (!ok) Con_Printf("  %s\n", star_api_get_last_error());
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
            Cvar_SetValueQuick(&oasis_star_anorak_face, 1);
            Con_Printf("Beam-in successful (mock). Welcome, %s.\n", runtime_user);
            return;
        }

        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
        g_star_config.base_url = "https://star-api.oasisplatform.world/api";
        g_star_config.api_key = getenv("STAR_API_KEY");
        g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
        g_star_config.timeout_seconds = 10;
        star_api_result_t r = star_api_init(&g_star_config);
        if (r != STAR_API_SUCCESS) {
            Con_Printf("Beamin failed - init: %s\n", star_api_get_last_error());
            return;
        }
        const char* username = runtime_user ? runtime_user : getenv("STAR_USERNAME");
        const char* password = runtime_pass ? runtime_pass : getenv("STAR_PASSWORD");
        if (username && password) {
            r = star_api_authenticate(username, password);
            if (r == STAR_API_SUCCESS) {
                g_star_initialized = 1;
                q_strlcpy(g_star_username, username, sizeof(g_star_username));
                Con_Printf("Logged in (beamin). Cross-game keys enabled.\n");
                return;
            }
            Con_Printf("Beamin (SSO) failed: %s\n", star_api_get_last_error());
            return;
        }
        if (g_star_config.api_key && g_star_config.avatar_id) {
            g_star_initialized = 1;
            // Try to get username from avatar_id or use a default
            if (g_star_config.avatar_id) {
                q_strlcpy(g_star_username, "API User", sizeof(g_star_username));
            }
            Con_Printf("Logged in with API key. Cross-game keys enabled.\n");
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
    Con_Printf("Unknown STAR subcommand: %s. Type 'star' for list.\n", sub);
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
    int grouped_count;
    int visible_end;
    char line[512];

    OQ_PollInventoryHotkeys();

    if (!g_inventory_open || !cbx)
        return;

    if (realtime - g_inventory_last_refresh > 2.0)
        OQ_RefreshInventoryCache();

    panel_w = q_min(glwidth - 48, 900);
    panel_h = q_min(glheight - 96, 420);
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
            Draw_Fill(cbx, slot_x + 1, tab_y - 1, tab_slot_w - 2, 10, 96, 0.35f);
        Draw_String(cbx, tab_name_x, tab_y, tab_name);
    }
    Draw_String(cbx, panel_x + 6, panel_y + panel_h - 16, "Arrows=Select  E=Use  Z=Send Avatar  X=Send Clan");
    Draw_String(cbx, panel_x + 6, panel_y + panel_h - 8, "I=Toggle  O/P=Switch Tabs");

    draw_y = panel_y + 54;
    grouped_count = OQ_BuildGroupedRows(rep_indices, grouped_labels, grouped_modes, grouped_values, OQ_MAX_INVENTORY_ITEMS);
    OQ_ClampSelection(grouped_count);
    visible_end = q_min(grouped_count, g_inventory_scroll_row + OQ_MAX_OVERLAY_ROWS);
    for (row = g_inventory_scroll_row; row < visible_end; row++) {
        if (row == g_inventory_selected_row)
            Draw_Fill(cbx, panel_x + 5, draw_y - 1, panel_w - 10, 10, 96, 0.25f);
        if (grouped_modes[row] == OQ_GROUP_MODE_SUM)
            q_snprintf(line, sizeof(line), "%s +%d", grouped_labels[row], grouped_values[row]);
        else
            q_snprintf(line, sizeof(line), "%s x%d", grouped_labels[row], grouped_values[row]);
        Draw_String(cbx, panel_x + 8, draw_y, line);
        draw_y += 8;
    }

    if (grouped_count == 0)
        Draw_String(cbx, panel_x + 6, draw_y, g_inventory_status);

    if (g_inventory_send_popup != OQ_SEND_POPUP_NONE) {
        int popup_w = q_min(panel_w - 80, 420);
        int popup_h = 96;
        int popup_x = panel_x + (panel_w - popup_w) / 2;
        int popup_y = panel_y + (panel_h - popup_h) / 2;
        const char* title = g_inventory_send_popup == OQ_SEND_POPUP_CLAN ? "SEND TO CLAN" : "SEND TO AVATAR";
        const char* label = g_inventory_send_popup == OQ_SEND_POPUP_CLAN ? "Clan" : "Username";
        int mode = OQ_GROUP_MODE_COUNT;
        int available = 1;

        Draw_Fill(cbx, popup_x, popup_y, popup_w, popup_h, 0, 0.9f);
        Draw_String(cbx, popup_x + 8, popup_y + 8, title);
        q_snprintf(line, sizeof(line), "%s: %s%s", label, g_inventory_send_target, ((int)(realtime * 2) & 1) ? "_" : "");
        Draw_String(cbx, popup_x + 8, popup_y + 24, line);
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
        Draw_String(cbx, popup_x + 8, popup_y + 36, line);

        if (g_inventory_send_button == 0)
            Draw_Fill(cbx, popup_x + 8, popup_y + 60, 64, 10, 96, 0.35f);
        Draw_String(cbx, popup_x + 16, popup_y + 61, "SEND");

        if (g_inventory_send_button == 1)
            Draw_Fill(cbx, popup_x + 84, popup_y + 60, 72, 10, 96, 0.35f);
        Draw_String(cbx, popup_x + 92, popup_y + 61, "CANCEL");
    }
}

void OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx) {
    extern int glheight;
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

int OQuake_STAR_ShouldUseAnorakFace(void) {
    return oasis_star_anorak_face.value > 0.5f;
}

const char* OQuake_STAR_GetUsername(void) {
    if (g_star_initialized && g_star_username[0])
        return g_star_username;
    return NULL;
}
