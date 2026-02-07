/**
 * OASIS STAR API - C/C++ Wrapper for Game Integration
 * 
 * This header provides a C interface for integrating games (Doom, Quake, etc.)
 * with the OASIS STAR API for cross-game item sharing and quest systems.
 * 
 * Usage:
 *   1. Call star_api_init() at game startup
 *   2. Use star_api_has_item() to check for items
 *   3. Use star_api_add_item() when items are collected
 *   4. Use star_api_use_item() when items are used
 *   5. Call star_api_cleanup() at game shutdown
 */

#ifndef STAR_API_H
#define STAR_API_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdbool.h>
#include <stdint.h>

// Configuration structure
typedef struct {
    const char* base_url;
    const char* api_key;
    const char* avatar_id;
    int timeout_seconds;
} star_api_config_t;

// Item structure
typedef struct {
    char id[64];
    char name[256];
    char description[512];
    char game_source[64];
    char item_type[64];
} star_item_t;

// Item list structure
typedef struct {
    star_item_t* items;
    size_t count;
    size_t capacity;
} star_item_list_t;

// Result codes
typedef enum {
    STAR_API_SUCCESS = 0,
    STAR_API_ERROR_INIT_FAILED = -1,
    STAR_API_ERROR_NOT_INITIALIZED = -2,
    STAR_API_ERROR_NETWORK = -3,
    STAR_API_ERROR_INVALID_PARAM = -4,
    STAR_API_ERROR_API_ERROR = -5
} star_api_result_t;

/**
 * Initialize the STAR API client with API key
 * Must be called before any other API functions
 * 
 * @param config Configuration structure with API URL, key, and avatar ID
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_init(const star_api_config_t* config);

/**
 * Authenticate using avatar credentials (SSO)
 * 
 * @param username Username or email
 * @param password Password
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_authenticate(const char* username, const char* password);

/**
 * Cleanup and shutdown the STAR API client
 * Should be called at game shutdown
 */
void star_api_cleanup(void);

/**
 * Check if the player has a specific item in their inventory
 * 
 * @param item_name Name of the item to check (case-insensitive)
 * @return true if player has the item, false otherwise
 */
bool star_api_has_item(const char* item_name);

/**
 * Get all items in the player's inventory
 * 
 * @param item_list Pointer to item list structure (will be allocated)
 * @return STAR_API_SUCCESS on success, error code on failure
 * 
 * Note: Caller must free the item list using star_api_free_item_list()
 */
star_api_result_t star_api_get_inventory(star_item_list_t** item_list);

/**
 * Free an item list allocated by star_api_get_inventory()
 */
void star_api_free_item_list(star_item_list_t* item_list);

/**
 * Add an item to the player's inventory
 * Called when player collects an item in-game
 * 
 * @param item_name Name of the item
 * @param description Description of the item
 * @param game_source Source game (e.g., "Doom", "Quake")
 * @param item_type Type of item (e.g., "KeyItem", "Weapon", "PowerUp")
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_add_item(
    const char* item_name,
    const char* description,
    const char* game_source,
    const char* item_type
);

/**
 * Use an item from the player's inventory
 * Called when player uses an item (e.g., uses keycard to open door)
 * 
 * @param item_name Name of the item to use
 * @param context Optional context string (e.g., "door_123", "chest_456")
 * @return true if item was successfully used, false otherwise
 */
bool star_api_use_item(const char* item_name, const char* context);

/**
 * Start a quest
 * 
 * @param quest_id Quest identifier
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_start_quest(const char* quest_id);

/**
 * Complete a quest objective
 * 
 * @param quest_id Quest identifier
 * @param objective_id Objective identifier
 * @param game_source Source game (e.g., "Doom", "Quake")
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_complete_quest_objective(const char* quest_id, const char* objective_id, const char* game_source);

/**
 * Complete a quest and claim rewards
 * 
 * @param quest_id Quest identifier
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_complete_quest(const char* quest_id);

/**
 * Create an NFT for a defeated boss
 * 
 * @param boss_name Name of the boss
 * @param description Description of the boss
 * @param game_source Source game
 * @param boss_stats JSON string with boss stats
 * @param nft_id_out Output buffer for NFT ID (must be at least 64 bytes)
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_create_boss_nft(
    const char* boss_name,
    const char* description,
    const char* game_source,
    const char* boss_stats,
    char* nft_id_out
);

/**
 * Deploy a boss NFT in a game
 * 
 * @param nft_id NFT identifier
 * @param target_game Target game (e.g., "Quake")
 * @param location Location identifier
 * @return STAR_API_SUCCESS on success, error code on failure
 */
star_api_result_t star_api_deploy_boss_nft(const char* nft_id, const char* target_game, const char* location);

/**
 * Get the last error message
 * 
 * @return Error message string (valid until next API call)
 */
const char* star_api_get_last_error(void);

/**
 * Set callback function for async operations
 * 
 * @param callback Function to call when async operation completes
 */
typedef void (*star_api_callback_t)(star_api_result_t result, void* user_data);
void star_api_set_callback(star_api_callback_t callback, void* user_data);

#ifdef __cplusplus
}
#endif

#endif // STAR_API_H

