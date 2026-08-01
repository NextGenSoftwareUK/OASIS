#include "ogengine.h"
#include <stdio.h>

static void on_star_callback(ogengine_result_t result, void* user_data)
{
    (void)user_data;
    printf("[callback] result=%d\n", (int)result);
}

int main(void)
{
    printf("WEB5 STAR API smoke test starting...\n");

    ogengine_config_t config;
    config.base_url = "http://127.0.0.1:65535/api";
    config.api_key = "SMOKE_TEST_KEY";
    config.avatar_id = "00000000-0000-0000-0000-000000000000";
    config.timeout_seconds = 2;
    config.client_game_source = NULL;
    /* Optional WEB4 OASIS API URI for avatar auth + NFT mint routes. */
    (void)ogengine_set_oasis_base_url("http://127.0.0.1:65535");

    ogengine_set_callback(on_star_callback, NULL);

    ogengine_result_t init_result = ogengine_init(&config);
    printf("ogengine_init => %d\n", (int)init_result);
    if (init_result != OGENGINE_SUCCESS)
    {
        const char* err = ogengine_get_last_error();
        printf("Init failed: %s\n", err ? err : "(null)");
        return 1;
    }

    int has_item = ogengine_has_item("SmokeTestItem");
    printf("ogengine_has_item => %d\n", has_item);

    ogengine_item_list_t* inventory = NULL;
    ogengine_result_t inv_result = ogengine_get_inventory(&inventory);
    printf("ogengine_get_inventory => %d\n", (int)inv_result);
    if (inventory != NULL)
    {
        printf("inventory.count=%llu\n", (unsigned long long)inventory->count);
        ogengine_free_item_list(inventory);
    }
    else
    {
        const char* err = ogengine_get_last_error();
        printf("Inventory error (expected with test endpoint): %s\n", err ? err : "(null)");
    }

    ogengine_cleanup();
    printf("WEB5 STAR API smoke test completed.\n");
    return 0;
}

