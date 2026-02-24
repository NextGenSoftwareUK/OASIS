/**
 * Example: Cross-Game Keycard Integration
 * 
 * This example demonstrates how to implement cross-game keycard sharing
 * between Doom and Quake using the STAR API.
 */

#include "../NativeWrapper/star_api.h"
#include <stdio.h>
#include <stdlib.h>

// Example: Doom keycard pickup
void Doom_Example_KeycardPickup(int keycard_type) {
    const char* keycard_names[] = {
        NULL,
        "red_keycard",
        "blue_keycard",
        "yellow_keycard",
        "skull_key"
    };
    
    if (keycard_type < 1 || keycard_type > 4) {
        return;
    }
    
    const char* keycard_name = keycard_names[keycard_type];
    const char* descriptions[] = {
        NULL,
        "Red Keycard - Opens red doors",
        "Blue Keycard - Opens blue doors",
        "Yellow Keycard - Opens yellow doors",
        "Skull Key - Opens skull-marked doors"
    };
    
    // Add keycard to STAR API inventory
    star_api_result_t result = star_api_add_item(
        keycard_name,
        descriptions[keycard_type],
        "Doom",
        "KeyItem"
    );
    
    if (result == STAR_API_SUCCESS) {
        printf("Doom: Added %s to cross-game inventory!\n", keycard_name);
    } else {
        printf("Doom: Failed to add keycard: %s\n", star_api_get_last_error());
    }
}

// Example: Quake door access check
bool Quake_Example_CheckDoor(const char* door_name, const char* required_key) {
    // First check local Quake inventory
    // (This would be your normal Quake key checking logic)
    bool has_local_key = false;  // Placeholder
    
    if (has_local_key) {
        return true;  // Use local key
    }
    
    // Check cross-game inventory via STAR API
    if (star_api_has_item(required_key)) {
        printf("Quake: Door '%s' opened using cross-game key: %s\n", door_name, required_key);
        
        // Use the item
        star_api_use_item(required_key, door_name);
        return true;
    }
    
    // Also check for Doom keycard equivalents
    // Map Quake keys to Doom keycards
    if (strcmp(required_key, "silver_key") == 0) {
        if (star_api_has_item("red_keycard")) {
            printf("Quake: Using Doom red keycard to open door!\n");
            star_api_use_item("red_keycard", door_name);
            return true;
        }
    } else if (strcmp(required_key, "gold_key") == 0) {
        if (star_api_has_item("blue_keycard")) {
            printf("Quake: Using Doom blue keycard to open door!\n");
            star_api_use_item("blue_keycard", door_name);
            return true;
        }
    }
    
    return false;  // No key available
}

// Example: Initialize and use STAR API
int main(void) {
    // Initialize STAR API
    star_api_config_t config = {
        .base_url = "https://star-api.oasisweb4.com/api",
        .api_key = getenv("STAR_API_KEY"),
        .avatar_id = getenv("STAR_AVATAR_ID"),
        .timeout_seconds = 10
    };
    
    if (!config.api_key || !config.avatar_id) {
        printf("Error: STAR_API_KEY and STAR_AVATAR_ID must be set\n");
        return 1;
    }
    
    star_api_result_t result = star_api_init(&config);
    if (result != STAR_API_SUCCESS) {
        printf("Failed to initialize STAR API: %s\n", star_api_get_last_error());
        return 1;
    }
    
    printf("STAR API initialized successfully!\n");
    
    // Example: Player picks up red keycard in Doom
    printf("\n=== Doom: Player picks up red keycard ===\n");
    Doom_Example_KeycardPickup(1);
    
    // Example: Player tries to open door in Quake
    printf("\n=== Quake: Player tries to open door requiring silver key ===\n");
    if (Quake_Example_CheckDoor("door_123", "silver_key")) {
        printf("Door opened successfully!\n");
    } else {
        printf("Door remains locked.\n");
    }
    
    // Check if player has any keycards
    printf("\n=== Checking inventory ===\n");
    star_item_list_t* inventory = NULL;
    result = star_api_get_inventory(&inventory);
    if (result == STAR_API_SUCCESS && inventory) {
        printf("Player has %zu items in inventory:\n", inventory->count);
        for (size_t i = 0; i < inventory->count; i++) {
            printf("  - %s: %s\n", inventory->items[i].name, inventory->items[i].description);
        }
        star_api_free_item_list(inventory);
    }
    
    // Cleanup
    star_api_cleanup();
    printf("\nSTAR API cleaned up.\n");
    
    return 0;
}



