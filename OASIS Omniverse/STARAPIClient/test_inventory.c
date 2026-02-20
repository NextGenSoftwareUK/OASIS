#include "star_api.h"
#include <stdio.h>
#include <string.h>

static void on_star_callback(star_api_result_t result, void* user_data)
{
    (void)user_data;
    printf("[callback] result=%d\n", (int)result);
}

int main(int argc, char** argv)
{
    printf("STAR API Inventory Test\n");
    printf("======================\n\n");

    if (argc < 4)
    {
        printf("Usage: %s <base_url> <username> <password> [api_key] [avatar_id]\n", argv[0]);
        printf("Example: %s http://127.0.0.1:65535/api testuser testpass\n", argv[0]);
        return 1;
    }

    const char* base_url = argv[1];
    const char* username = argv[2];
    const char* password = argv[3];
    const char* api_key = argc > 4 ? argv[4] : NULL;
    const char* avatar_id = argc > 5 ? argv[5] : NULL;

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
    if (avatar_id_result == STAR_API_SUCCESS)
    {
        printf("   ✓ Avatar ID: %s\n\n", avatar_id_buf);
    }
    else
    {
        const char* err = star_api_get_last_error();
        printf("   WARNING: Could not get avatar ID: %s\n\n", err ? err : "(null)");
    }

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
        printf("\n   Items:\n");
        for (size_t i = 0; i < inventory->count; i++)
        {
            printf("   [%llu] %s - %s (%s)\n", 
                (unsigned long long)i + 1,
                inventory->items[i].name,
                inventory->items[i].description,
                inventory->items[i].item_type);
        }
    }
    else
    {
        printf("   (Inventory is empty)\n");
    }

    star_api_free_item_list(inventory);

    printf("\n5. Testing HasItem...\n");
    int has_test = star_api_has_item("TestItem");
    printf("   star_api_has_item(\"TestItem\") => %d\n", has_test);
    
    /* Test has_item with items from inventory */
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

    printf("\n6. Testing AddItem (starting weapons/items)...\n");
    
    /* Test adding starting weapons that might already exist */
    const char* test_items[][3] = {
        {"quake_weapon_shotgun", "Shotgun discovered", "Weapon"},
        {"quake_pickup_shells_000001", "Shells pickup +25", "Ammo"},
        {"quake_weapon_super_shotgun", "Super Shotgun discovered", "Weapon"},
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
            star_api_result_t add_result = star_api_add_item(item_name, item_desc, "Quake", item_type);
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
                    printf("     ✓ Found: %s\n", item_name);
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
            "Weapon");
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

    star_api_cleanup();
    printf("\n✓ Test completed successfully!\n");
    return 0;
}

