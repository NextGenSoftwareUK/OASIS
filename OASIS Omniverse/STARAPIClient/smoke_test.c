#include "star_api.h"
#include <stdio.h>

static void on_star_callback(star_api_result_t result, void* user_data)
{
    (void)user_data;
    printf("[callback] result=%d\n", (int)result);
}

int main(void)
{
    printf("STAR API smoke test starting...\n");

    star_api_config_t config;
    config.base_url = "http://127.0.0.1:65535/api";
    config.api_key = "SMOKE_TEST_KEY";
    config.avatar_id = "00000000-0000-0000-0000-000000000000";
    config.timeout_seconds = 2;

    star_api_set_callback(on_star_callback, NULL);

    star_api_result_t init_result = star_api_init(&config);
    printf("star_api_init => %d\n", (int)init_result);
    if (init_result != STAR_API_SUCCESS)
    {
        const char* err = star_api_get_last_error();
        printf("Init failed: %s\n", err ? err : "(null)");
        return 1;
    }

    int has_item = star_api_has_item("SmokeTestItem");
    printf("star_api_has_item => %d\n", has_item);

    star_item_list_t* inventory = NULL;
    star_api_result_t inv_result = star_api_get_inventory(&inventory);
    printf("star_api_get_inventory => %d\n", (int)inv_result);
    if (inventory != NULL)
    {
        printf("inventory.count=%llu\n", (unsigned long long)inventory->count);
        star_api_free_item_list(inventory);
    }
    else
    {
        const char* err = star_api_get_last_error();
        printf("Inventory error (expected with test endpoint): %s\n", err ? err : "(null)");
    }

    star_api_cleanup();
    printf("STAR API smoke test completed.\n");
    return 0;
}

