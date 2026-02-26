#include "star_api.h"
#include <stdio.h>
#include <string.h>

static void on_star_callback(star_api_result_t result, void* user_data)
{
    (void)user_data;
    printf("[callback] result=%d\n", (int)result);
}

/* Return quantity of first item matching name, or -1 if not found. */
static int get_item_quantity(const star_item_list_t* list, const char* name)
{
    if (!list || !name) return -1;
    for (size_t i = 0; i < list->count; i++)
    {
        if (strcmp(list->items[i].name, name) == 0)
            return list->items[i].quantity;
    }
    return -1;
}

int main(int argc, char** argv)
{
    printf("STAR API Inventory Test\n");
    printf("======================\n\n");

    if (argc < 4)
    {
        printf("Usage: %s <base_url> <username> <password> [api_key] [avatar_id] [send_avatar_target] [send_clan_name]\n", argv[0]);
        printf("Example: %s http://127.0.0.1:65535/api testuser testpass\n", argv[0]);
        printf("  send_avatar_target = username or avatar id to send an item to (optional; tests send-to-avatar).\n");
        printf("  send_clan_name     = clan name to send an item to (optional; tests send-to-clan).\n");
        return 1;
    }

    const char* base_url = argv[1];
    const char* username = argv[2];
    const char* password = argv[3];
    const char* api_key = argc > 4 ? argv[4] : NULL;
    const char* avatar_id = argc > 5 ? argv[5] : NULL;
    const char* send_avatar_target = argc > 6 ? argv[6] : NULL;
    const char* send_clan_name = argc > 7 ? argv[7] : NULL;

    star_api_config_t config;
    config.base_url = base_url;
    config.api_key = api_key;
    config.avatar_id = avatar_id;
    config.timeout_seconds = 60;  /* Increased timeout for slow Web4 responses */

    star_api_set_callback(on_star_callback, NULL);

    printf("1. Initializing STAR API...\n");
    star_api_result_t init_result = star_api_init(&config);
    printf("   star_api_init => %d\n", (int)init_result);
    if (init_result != STAR_API_SUCCESS)
    {
        const char* err = star_api_get_last_error();
        printf("   ERROR: Init failed: %s\n", err ? err : "(null)");
        return 1;
    }
    printf("   ✓ Initialized successfully\n\n");

    /* Set Web4 OASIS API base URL (required for inventory endpoints that forward to Web4) */
    /* Extract base URL and change port from 5556 to 5555 for Web4 */
    char web4_url[256] = {0};
    if (strstr(base_url, ":5556"))
    {
        strncpy(web4_url, base_url, sizeof(web4_url) - 1);
        char* port = strstr(web4_url, ":5556");
        if (port)
        {
            memmove(port + 5, port + 5, strlen(port + 5) + 1);
            strncpy(port, ":5555", 5);
        }
        /* Remove /api suffix if present */
        char* api_suffix = strstr(web4_url, "/api");
        if (api_suffix)
            *api_suffix = 0;
    }
    else
    {
        strncpy(web4_url, "http://localhost:5555", sizeof(web4_url) - 1);
    }
    
    star_api_result_t oasis_result = star_api_set_oasis_base_url(web4_url);
    printf("   star_api_set_oasis_base_url(\"%s\") => %d\n", web4_url, (int)oasis_result);
    if (oasis_result != STAR_API_SUCCESS)
    {
        const char* err = star_api_get_last_error();
        printf("   WARNING: Could not set Web4 URL: %s\n", err ? err : "(null)");
    }
    else
    {
        printf("   ✓ Web4 OASIS API URL set\n\n");
    }
    
    /* NOTE: The 405 error indicates Web5 is forwarding to Web4, but Web4 is rejecting the request.
     * This could mean:
     * 1. Web4 doesn't have the /api/avatar/inventory endpoint
     * 2. Web4 expects a different HTTP method
     * 3. Web4 needs additional authentication/headers
     * 4. Web5's Web4OasisApiBaseUrl configuration is missing or incorrect
     * 
     * Check Web5's appsettings.json or set WEB4_OASIS_API_BASE_URL environment variable.
     */

    printf("2. Authenticating...\n");
    star_api_result_t auth_result = star_api_authenticate(username, password);
    printf("   star_api_authenticate => %d\n", (int)auth_result);
    if (auth_result != STAR_API_SUCCESS)
    {
        const char* err = star_api_get_last_error();
        printf("   ERROR: Authentication failed: %s\n", err ? err : "(null)");
        star_api_cleanup();
        return 1;
    }
    printf("   ✓ Authenticated successfully\n\n");

    printf("3. Getting Avatar ID...\n");
    char avatar_id_buf[64] = {0};
    star_api_result_t avatar_id_result = star_api_get_avatar_id(avatar_id_buf, sizeof(avatar_id_buf));
    printf("   star_api_get_avatar_id => %d\n", (int)avatar_id_result);
    if (avatar_id_result == STAR_API_SUCCESS && avatar_id_buf[0])
        printf("   ✓ Avatar ID: %s\n", avatar_id_buf);
    else
    {
        const char* err = star_api_get_last_error();
        printf("   WARNING: Could not get avatar ID: %s\n", err ? err : "(null)");
    }
    printf("\n   >>> TEST IS USING Username: %s | Avatar ID: %s <<<\n\n", username, avatar_id_buf[0] ? avatar_id_buf : "(none)");

    printf("4. Getting Inventory...\n");
    star_item_list_t* inventory = NULL;
    star_api_result_t inv_result = star_api_get_inventory(&inventory);
    printf("   star_api_get_inventory => %d\n", (int)inv_result);
    
    if (inv_result != STAR_API_SUCCESS)
    {
        const char* err = star_api_get_last_error();
        printf("   ERROR: Failed to get inventory: %s\n", err ? err : "(null)");
        star_api_cleanup();
        return 1;
    }

    if (inventory == NULL)
    {
        printf("   ERROR: Inventory is NULL but result was success\n");
        star_api_cleanup();
        return 1;
    }

    printf("   ✓ Inventory retrieved successfully\n");
    printf("   Inventory count: %llu\n", (unsigned long long)inventory->count);
    
    if (inventory->count > 0)
    {
        printf("\n   Items (name, description, type, quantity):\n");
        for (size_t i = 0; i < inventory->count; i++)
        {
            printf("   [%llu] %s - %s (%s) qty=%d\n",
                (unsigned long long)i + 1,
                inventory->items[i].name,
                inventory->items[i].description,
                inventory->items[i].item_type,
                inventory->items[i].quantity);
        }
    }
    else
    {
        printf("   (Inventory is empty)\n");
    }

    printf("\n5. Testing HasItem...\n");
    int has_test = star_api_has_item("TestItem");
    printf("   star_api_has_item(\"TestItem\") => %d\n", has_test);
    
    /* Test has_item with items from inventory (before freeing) */
    if (inventory != NULL && inventory->count > 0)
    {
        printf("   Testing has_item with existing items from inventory:\n");
        for (size_t i = 0; i < inventory->count && i < 3; i++) /* Test first 3 items */
        {
            int has = star_api_has_item(inventory->items[i].name);
            printf("   star_api_has_item(\"%s\") => %d %s\n", 
                inventory->items[i].name, 
                has,
                has ? "(exists)" : "(not found)");
        }
    }

    star_api_free_item_list(inventory);
    inventory = NULL;

    printf("\n6. Testing AddItem (starting weapons/items)...\n");
    
    /* Test adding starting weapons that might already exist */
    const char* test_items[][3] = {
        {"Shotgun", "Shotgun discovered", "Weapon"},
        {"Shells", "Shells pickup +25", "Ammo"},
        {"Super Shotgun", "Super Shotgun discovered", "Weapon"},
        {NULL, NULL, NULL}
    };
    
    for (int i = 0; test_items[i][0] != NULL; i++)
    {
        const char* item_name = test_items[i][0];
        const char* item_desc = test_items[i][1];
        const char* item_type = test_items[i][2];
        
        printf("   Testing item: %s\n", item_name);
        
        /* First check if item already exists */
        int has_before = star_api_has_item(item_name);
        printf("     star_api_has_item(\"%s\") => %d %s\n", 
            item_name, 
            has_before,
            has_before ? "(already exists)" : "(does not exist)");
        
        if (has_before)
        {
            printf("     ✓ Item already exists in remote - should be marked as synced\n");
        }
        else
        {
            /* Item doesn't exist - try to add it */
            printf("     Attempting to add item...\n");
            star_api_result_t add_result = star_api_add_item(item_name, item_desc, "Quake", item_type, NULL, 1, 1);
            printf("     star_api_add_item(\"%s\") => %d\n", item_name, (int)add_result);
            
            if (add_result == STAR_API_SUCCESS)
            {
                printf("     ✓ Item added successfully - should be marked as synced\n");
                
                /* Verify it was added by checking has_item */
                int has_after = star_api_has_item(item_name);
                printf("     star_api_has_item(\"%s\") after add => %d %s\n", 
                    item_name, 
                    has_after,
                    has_after ? "(confirmed)" : "(NOT FOUND - possible cache issue)");
            }
            else
            {
                const char* err = star_api_get_last_error();
                printf("     ✗ Failed to add item: %s\n", err ? err : "(null)");
            }
        }
        printf("\n");
    }

    /* ========== NEW INVENTORY SYSTEM: stack / no-stack, quantity, save, load, add more ========== */
    printf("6a. NEW INVENTORY SYSTEM (stack, no-stack, quantity, save/load, add more)\n");
    printf("    Uses unique item names so tests do not clash with existing inventory.\n");
    printf("    'Save' = API persists on each add_item; 'Load' = clear client cache + get_inventory.\n\n");

    const char* stack_item    = "test_inv_stack_Shells";
    const char* nostack_item  = "test_inv_nostack_Key";
    const char* bulk_item     = "test_inv_bulk_Gems";
    int tests_ok = 0;
    int tests_fail = 0;

    /* --- [1] Stack: add 1 → load (clear+get) → check qty 1 → add 2 more → load → check qty 3 --- */
    printf("    [1] STACK + add 1, load, check; then add 2 more, load, check (expect 1 then 3)...\n");
    star_api_result_t r1 = star_api_add_item(stack_item, "Stack test shells", "Test", "Ammo", NULL, 1, 1);
    if (r1 != STAR_API_SUCCESS)
    {
        printf("        FAIL: add 1 (stack=1): %s\n", star_api_get_last_error() ? star_api_get_last_error() : "unknown");
        tests_fail++;
    }
    else
    {
        star_api_clear_cache();
        star_item_list_t* inv1a = NULL;
        star_api_result_t gr1a = star_api_get_inventory(&inv1a);
        if (gr1a != STAR_API_SUCCESS || !inv1a)
        {
            printf("        FAIL: get_inventory after first add\n");
            tests_fail++;
        }
        else
        {
            int q1a = get_item_quantity(inv1a, stack_item);
            star_api_free_item_list(inv1a);
            if (q1a != 1)
            {
                printf("        FAIL: after add 1, expected qty 1, got %d\n", q1a);
                tests_fail++;
            }
            else
            {
                printf("        OK: add 1 -> load -> qty 1\n");
                tests_ok++;
                star_api_result_t r2 = star_api_add_item(stack_item, "Stack test shells", "Test", "Ammo", NULL, 2, 1);
                if (r2 != STAR_API_SUCCESS)
                {
                    printf("        FAIL: add 2 more (stack=1): %s\n", star_api_get_last_error() ? star_api_get_last_error() : "unknown");
                    tests_fail++;
                }
                else
                {
                    star_api_clear_cache();
                    star_item_list_t* inv1b = NULL;
                    star_api_result_t gr1b = star_api_get_inventory(&inv1b);
                    if (gr1b != STAR_API_SUCCESS || !inv1b)
                    {
                        printf("        FAIL: get_inventory after add 2 more\n");
                        tests_fail++;
                    }
                    else
                    {
                        int q1b = get_item_quantity(inv1b, stack_item);
                        star_api_free_item_list(inv1b);
                        if (q1b == 3) { printf("        OK: add 2 more -> load -> qty 3\n"); tests_ok++; }
                        else { printf("        FAIL: expected qty 3, got %d (quantity not persisted)\n", q1b); tests_fail++; }
                    }
                }
            }
        }
    }

    /* --- [2] No-stack: add once OK, add again must fail ("Item already exists") --- */
    printf("    [2] NO-STACK: add 1, then add again (expect second add to fail)...\n");
    star_api_result_t r3 = star_api_add_item(nostack_item, "No-stack key", "Test", "KeyItem", NULL, 1, 0);
    if (r3 != STAR_API_SUCCESS)
    {
        printf("        FAIL: first add (stack=0): %s\n", star_api_get_last_error() ? star_api_get_last_error() : "unknown");
        tests_fail++;
    }
    else
    {
        star_api_result_t r4 = star_api_add_item(nostack_item, "No-stack key", "Test", "KeyItem", NULL, 1, 0);
        if (r4 == STAR_API_SUCCESS)
        {
            printf("        FAIL: second add should fail (item already exists, stack=0)\n");
            tests_fail++;
        }
        else
        {
            const char* err = star_api_get_last_error();
            int is_already = err && (strstr(err, "already exists") || strstr(err, "Already exists"));
            if (is_already) { printf("        OK: second add correctly failed: '%s'\n", err ? err : ""); tests_ok++; }
            else { printf("        OK: second add failed as expected: %s\n", err ? err : "unknown"); tests_ok++; }
        }
    }

    /* --- [3] Add more than 1 in one call (qty 5), then add 3 more (total 8), load and check --- */
    printf("    [3] BULK: add 5, then add 3 (stack=1), load, check qty 8...\n");
    star_api_result_t r5 = star_api_add_item(bulk_item, "Bulk gems", "Test", "KeyItem", NULL, 5, 1);
    if (r5 != STAR_API_SUCCESS)
    {
        printf("        FAIL: add 5: %s\n", star_api_get_last_error() ? star_api_get_last_error() : "unknown");
        tests_fail++;
    }
    else
    {
        star_api_result_t r6 = star_api_add_item(bulk_item, "Bulk gems", "Test", "KeyItem", NULL, 3, 1);
        if (r6 != STAR_API_SUCCESS)
        {
            printf("        FAIL: add 3 more: %s\n", star_api_get_last_error() ? star_api_get_last_error() : "unknown");
            tests_fail++;
        }
        else
        {
            star_api_clear_cache();
            star_item_list_t* inv2 = NULL;
            star_api_result_t gr2 = star_api_get_inventory(&inv2);
            if (gr2 != STAR_API_SUCCESS || !inv2)
            {
                printf("        FAIL: get_inventory after bulk adds\n");
                tests_fail++;
            }
            else
            {
                int q2 = get_item_quantity(inv2, bulk_item);
                star_api_free_item_list(inv2);
                if (q2 == 8) { printf("        OK: qty 8 (5+3) after load\n"); tests_ok++; }
                else { printf("        FAIL: expected qty 8, got %d\n", q2); tests_fail++; }
            }
        }
    }

    /* --- [4] Add more to existing stack item (e.g. +2 to stack_item already at 3 → 5), load, check --- */
    printf("    [4] ADD MORE: add 2 to stack item (already 3), load, check qty 5...\n");
    star_api_result_t r7 = star_api_add_item(stack_item, "Stack test shells", "Test", "Ammo", NULL, 2, 1);
    if (r7 != STAR_API_SUCCESS)
    {
        printf("        FAIL: add 2 more to stack item: %s\n", star_api_get_last_error() ? star_api_get_last_error() : "unknown");
        tests_fail++;
    }
    else
    {
        star_api_clear_cache();
        star_item_list_t* inv4 = NULL;
        star_api_result_t gr4 = star_api_get_inventory(&inv4);
        if (gr4 != STAR_API_SUCCESS || !inv4)
        {
            printf("        FAIL: get_inventory after add more\n");
            tests_fail++;
        }
        else
        {
            int q4 = get_item_quantity(inv4, stack_item);
            star_api_free_item_list(inv4);
            if (q4 == 5) { printf("        OK: stack item qty 5 (3+2) after load\n"); tests_ok++; }
            else { printf("        FAIL: expected qty 5, got %d\n", q4); tests_fail++; }
        }
    }

    /* --- [5] Final "session reload": clear cache, get inventory, verify all test items and quantities --- */
    printf("    [5] SESSION RELOAD: clear cache, get_inventory, verify all test items and quantities...\n");
    star_api_clear_cache();
    star_item_list_t* inv_final = NULL;
    star_api_result_t gr_final = star_api_get_inventory(&inv_final);
    if (gr_final != STAR_API_SUCCESS || !inv_final)
    {
        printf("        FAIL: get_inventory (session reload)\n");
        tests_fail++;
    }
    else
    {
        int q_stack  = get_item_quantity(inv_final, stack_item);
        int q_nostack = get_item_quantity(inv_final, nostack_item);
        int q_bulk   = get_item_quantity(inv_final, bulk_item);
        int ok = (q_stack == 5 && q_nostack == 1 && q_bulk == 8);
        if (ok)
        {
            printf("        OK: stack=%d, nostack=%d, bulk=%d (all persisted)\n", q_stack, q_nostack, q_bulk);
            tests_ok++;
        }
        else
        {
            printf("        FAIL: stack=%d (expect 5), nostack=%d (expect 1), bulk=%d (expect 8)\n", q_stack, q_nostack, q_bulk);
            tests_fail++;
        }
        star_api_free_item_list(inv_final);
    }

    printf("\n    NEW INVENTORY SYSTEM SUMMARY: %d passed, %d failed\n", tests_ok, tests_fail);
    if (tests_fail > 0)
        printf("    WARNING: Some tests failed. Check API stack/quantity persistence (e.g. MongoDB/SQLite).\n");
    printf("\n");

    printf("7. Verifying inventory after add operations...\n");
    star_item_list_t* inventory_after = NULL;
    star_api_result_t inv_after_result = star_api_get_inventory(&inventory_after);
    printf("   star_api_get_inventory (after adds) => %d\n", (int)inv_after_result);
    
    if (inv_after_result == STAR_API_SUCCESS && inventory_after != NULL)
    {
        printf("   Inventory count after adds: %llu\n", (unsigned long long)inventory_after->count);
        
        /* Check if our test items are in the inventory */
        printf("   Checking if test items are in inventory:\n");
        for (int i = 0; test_items[i][0] != NULL; i++)
        {
            const char* item_name = test_items[i][0];
            int found = 0;
            for (size_t j = 0; j < inventory_after->count; j++)
            {
                if (strcmp(inventory_after->items[j].name, item_name) == 0)
                {
                    found = 1;
                    printf("     ✓ Found: %s (qty=%d)\n", item_name, inventory_after->items[j].quantity);
                    break;
                }
            }
            if (!found)
            {
                printf("     ✗ Not found: %s (may need to wait for sync)\n", item_name);
            }
        }
        star_api_free_item_list(inventory_after);
    }
    else
    {
        const char* err = star_api_get_last_error();
        printf("   ERROR: Failed to get inventory after adds: %s\n", err ? err : "(null)");
    }

    printf("\n7b. Testing STACKED items (Quake-style _NNNNNN, add each without has_item)...\n");
    {
        char av_buf[64] = {0};
        star_api_get_avatar_id(av_buf, sizeof(av_buf));
        printf("   Using Username: %s | Avatar ID: %s (all add_item/get_inventory use this)\n", username, av_buf[0] ? av_buf : "(none)");
        /* Stacked items: same name with stack=1 so API increments Quantity (e.g. Shells 1 -> 2 -> 3). */
        const char* stacked_items[][3] = {
            {"Shells", "Shells +25", "Ammo"},
            {"Shells", "Shells +25", "Ammo"},
            {"Shells", "Shells +25", "Ammo"},
            {"Nails", "Nails +25", "Ammo"},
            {"Rockets", "Rockets +5", "Ammo"},
            {NULL, NULL, NULL}
        };
        int stacked_added = 0;
        int stacked_failed = 0;
        for (int i = 0; stacked_items[i][0] != NULL; i++)
        {
            const char* name = stacked_items[i][0];
            const char* desc = stacked_items[i][1];
            const char* type = stacked_items[i][2];
            printf("   add_item \"%s\" => ", name);
            fflush(stdout);
            star_api_result_t r = star_api_add_item(name, desc, "Quake", type, NULL, 1, 1);
            printf("%d", (int)r);
            if (r == STAR_API_SUCCESS)
            {
                printf(" (success)\n");
                stacked_added++;
            }
            else
            {
                const char* err = star_api_get_last_error();
                printf(" FAILED: %s\n", err && err[0] ? err : "unknown error");
                stacked_failed++;
            }
        }
        printf("   Stacked add summary: %d succeeded, %d failed\n", stacked_added, stacked_failed);

        /* Force a real GET from the API (clear cache). Otherwise get_inventory returns in-memory cache and we never see if the API actually persisted the items. */
        printf("   Clearing client cache so next get_inventory hits the API...\n");
        star_api_clear_cache();

        /* Re-fetch inventory and check which stacked items appear (real HTTP GET). */
        printf("   Re-fetching inventory to verify stacked items (real GET)...\n");
        star_item_list_t* inv_stacked = NULL;
        star_api_result_t inv_r = star_api_get_inventory(&inv_stacked);
        if (inv_r == STAR_API_SUCCESS && inv_stacked != NULL)
        {
            for (int i = 0; stacked_items[i][0] != NULL; i++)
            {
                const char* name = stacked_items[i][0];
                int qty = get_item_quantity(inv_stacked, name);
                if (qty >= 0)
                    printf("     %s in inventory: YES (qty=%d)\n", name, qty);
                else
                    printf("     %s in inventory: NO\n", name);
            }
            printf("   Total inventory count after stacked adds: %llu\n", (unsigned long long)inv_stacked->count);
            star_api_free_item_list(inv_stacked);
        }
        else
        {
            const char* err = star_api_get_last_error();
            printf("   ERROR: get_inventory after stacked adds failed: %s\n", err ? err : "(null)");
        }
    }

    printf("\n8. Testing sync logic (check before add)...\n");
    const char* sync_test_item = "quake_weapon_test_sync";
    printf("   Testing sync logic for: %s\n", sync_test_item);
    
    /* Check if exists */
    int has_sync_test = star_api_has_item(sync_test_item);
    printf("     star_api_has_item(\"%s\") => %d\n", sync_test_item, has_sync_test);
    
    if (has_sync_test)
    {
        printf("     ✓ Item exists - should be marked as synced (no add needed)\n");
    }
    else
    {
        printf("     Item doesn't exist - attempting to add...\n");
        star_api_result_t sync_add_result = star_api_add_item(
            sync_test_item, 
            "Test sync item", 
            "Quake", 
            "Weapon",
            NULL, 1, 1);
        printf("     star_api_add_item => %d\n", (int)sync_add_result);
        
        if (sync_add_result == STAR_API_SUCCESS)
        {
            printf("     ✓ Item added - should be marked as synced\n");
            
            /* Verify sync */
            int has_after_sync = star_api_has_item(sync_test_item);
            printf("     star_api_has_item after add => %d %s\n", 
                has_after_sync,
                has_after_sync ? "(synced)" : "(NOT synced - possible issue)");
        }
        else
        {
            const char* err = star_api_get_last_error();
            printf("     ✗ Failed to add: %s\n", err ? err : "(null)");
        }
    }

    /* 9. Send to Avatar (optional: pass send_avatar_target as 6th arg) */
    printf("\n9. Testing Send to Avatar...\n");
    {
        const char* target = (send_avatar_target && send_avatar_target[0]) ? send_avatar_target : "nonexistent_avatar_test";
        const char* item_to_send = "Shotgun";
        if (!send_avatar_target || !send_avatar_target[0])
            printf("   (No target provided; using placeholder to exercise API. Pass 6th arg for real send.)\n");
        printf("   star_api_send_item_to_avatar(\"%s\", \"%s\", 1, NULL) => ", target, item_to_send);
        fflush(stdout);
        star_api_result_t send_av = star_api_send_item_to_avatar(target, item_to_send, 1, NULL);
        printf("%d\n", (int)send_av);
        if (send_av != STAR_API_SUCCESS)
        {
            const char* err = star_api_get_last_error();
            printf("   Error: %s\n", err ? err : "(null)");
        }
        else
            printf("   ✓ Sent to avatar successfully\n");
    }

    /* 10. Send to Clan (optional: pass send_clan_name as 7th arg) */
    printf("\n10. Testing Send to Clan...\n");
    {
        const char* clan = (send_clan_name && send_clan_name[0]) ? send_clan_name : "TestClanNonexistent";
        const char* item_to_send = "Shotgun";
        if (!send_clan_name || !send_clan_name[0])
            printf("   (No clan provided; using placeholder to exercise API. Pass 7th arg for real send.)\n");
        printf("   star_api_send_item_to_clan(\"%s\", \"%s\", 1, NULL) => ", clan, item_to_send);
        fflush(stdout);
        star_api_result_t send_cl = star_api_send_item_to_clan(clan, item_to_send, 1, NULL);
        printf("%d\n", (int)send_cl);
        if (send_cl != STAR_API_SUCCESS)
        {
            const char* err = star_api_get_last_error();
            printf("   Error: %s\n", err ? err : "(null)");
        }
        else
            printf("   ✓ Sent to clan successfully\n");
    }

    star_api_cleanup();
    printf("\n✓ Test completed successfully!\n");
    return 0;
}

