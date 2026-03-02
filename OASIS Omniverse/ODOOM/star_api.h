/**
 * OASIS WEB5 STAR API - C/C++ Wrapper for Game Integration
 * Native ABI compatible header for STARAPIClient NativeAOT exports.
 */

#ifndef STAR_API_H
#define STAR_API_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdbool.h>
#include <stdint.h>
#include <stddef.h>

typedef struct {
    /* WEB5 STAR API base URI (maps to C# Web5StarApiBaseUrl) */
    const char* base_url;
    const char* api_key;
    const char* avatar_id;
    int timeout_seconds;
} star_api_config_t;

typedef struct {
    char id[64];
    char name[256];
    char description[512];
    char game_source[64];
    char item_type[64];
    char nft_id[128];  /* NFTId from MetaData when item is linked to NFTHolon; empty when not an NFT item */
    int quantity;      /* Stack size. API increments if item exists and stack=1; otherwise new item gets this. */
} star_item_t;

typedef struct {
    star_item_t* items;
    size_t count;
    size_t capacity;
} star_item_list_t;

typedef enum {
    STAR_API_SUCCESS = 0,
    STAR_API_ERROR_INIT_FAILED = -1,
    STAR_API_ERROR_NOT_INITIALIZED = -2,
    STAR_API_ERROR_NETWORK = -3,
    STAR_API_ERROR_INVALID_PARAM = -4,
    STAR_API_ERROR_API_ERROR = -5
} star_api_result_t;

typedef void (*star_api_callback_t)(star_api_result_t result, void* user_data);

star_api_result_t star_api_init(const star_api_config_t* config);
star_api_result_t star_api_authenticate(const char* username, const char* password);
/* Set WEB4 OASIS API base URI (used for avatar auth + NFT mint endpoints). */
star_api_result_t star_api_set_oasis_base_url(const char* oasis_base_url);
void star_api_cleanup(void);
bool star_api_has_item(const char* item_name);
star_api_result_t star_api_get_inventory(star_item_list_t** item_list);
/** Clear client inventory cache. Next star_api_get_inventory will do a real HTTP GET. Use to verify API actually returns items (e.g. after add_item). */
void star_api_invalidate_inventory_cache(void);
/** Clear all client caches (e.g. inventory). Same effect as star_api_invalidate_inventory_cache. */
void star_api_clear_cache(void);
void star_api_free_item_list(star_item_list_t* item_list);
/** quantity: amount to add (or initial if new). stack: 1 = if item exists increment quantity; 0 = if exists return error "item already exists". */
star_api_result_t star_api_add_item(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* nft_id, int quantity, int stack);
/** Mint an NFT for an inventory item via WEB4 OASIS API (NFTHolon). Returns NFT ID; pass to star_api_add_item as nft_id. provider may be NULL (default SolanaOASIS). nft_id_out must be at least 128 bytes. hash_out optional (128 bytes) for tx hash/signature; pass NULL to omit. send_to_address_after_minting optional wallet address to send the minted NFT to (from oasisstar.json); pass NULL to omit. */
star_api_result_t star_api_mint_inventory_nft(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* provider, char* nft_id_out, char* hash_out, const char* send_to_address_after_minting);
bool star_api_use_item(const char* item_name, const char* context);
/** Queue one add-item job (batching). nft_id may be NULL. quantity and stack: same as star_api_add_item (default 1, 1). */
void star_api_queue_add_item(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* nft_id, int quantity, int stack);
/** Queue pickup with optional mint; C# client does mint (if do_mint) then add_item in background. Same pattern as queue_add_item. */
#define STAR_API_HAS_QUEUE_PICKUP_WITH_MINT 1
void star_api_queue_pickup_with_mint(const char* item_name, const char* description, const char* game_source, const char* item_type, int do_mint, const char* provider, const char* send_to_address_after_minting, int quantity);
star_api_result_t star_api_flush_add_item_jobs(void);
void star_api_queue_use_item(const char* item_name, const char* context);
star_api_result_t star_api_flush_use_item_jobs(void);
star_api_result_t star_api_start_quest(const char* quest_id);
star_api_result_t star_api_complete_quest_objective(const char* quest_id, const char* objective_id, const char* game_source);
star_api_result_t star_api_complete_quest(const char* quest_id);
/** provider: NFT provider (e.g. SolanaOASIS); NULL/empty = use default. Same as nft_provider in oasisstar.json. */
star_api_result_t star_api_create_monster_nft(const char* monster_name, const char* description, const char* game_source, const char* monster_stats, const char* provider, char* nft_id_out);
star_api_result_t star_api_deploy_boss_nft(const char* nft_id, const char* target_game, const char* location);
star_api_result_t star_api_get_avatar_id(char* avatar_id_out, size_t avatar_id_size);
/** Set avatar ID on the client (e.g. after SSO from C++ auth result). Does not change JWT. */
star_api_result_t star_api_set_avatar_id(const char* avatar_id);
/** Send item from current avatar's inventory to another avatar. Target = username or avatar Id. item_id optional (NULL or empty = match by name). */
star_api_result_t star_api_send_item_to_avatar(const char* target_username_or_avatar_id, const char* item_name, int quantity, const char* item_id);
/** Send item from current avatar's inventory to a clan. Target = clan name (or username). item_id optional (NULL or empty = match by name). */
star_api_result_t star_api_send_item_to_clan(const char* clan_name_or_target, const char* item_name, int quantity, const char* item_id);
/** Queue add XP for the beamed-in avatar (e.g. on monster kill). Flushed with add-item jobs or on next API sync. amount must be > 0. */
void star_api_queue_add_xp(int amount);
/** Queue monster kill (XP + optional mint + add to inventory). All work runs on background thread; never blocks. provider/game_source may be NULL (game_source NULL/empty = ODOOM). */
void star_api_queue_monster_kill(const char* engine_name, const char* display_name, int xp, int is_boss, int do_mint, const char* provider, const char* game_source);
/** Get last known avatar XP (from get-current-avatar or after add-xp). Returns 0 if not loaded. Write to *xp_out; pass NULL to skip. Returns 1 if value is valid, 0 otherwise. */
int star_api_get_avatar_xp(int* xp_out);
/** Refresh avatar XP from API (GET /api/avatar/current; server returns avatar with XP). Call once after beam-in. */
void star_api_refresh_avatar_xp(void);
/** Block until avatar XP is loaded from API. Call in auth-done callback before setting "beamed in" so HUD shows correct XP immediately. */
void star_api_refresh_avatar_xp_blocking(void);
const char* star_api_get_last_error(void);
/** Consume last mint result from background pickup-with-mint. Writes item name, NFT ID, and hash to buffers (null-terminated). Returns 1 if a result was available, 0 otherwise. Call from game pump/frame to show mint results in console. */
#define STAR_API_HAS_CONSUME_LAST_MINT 1
int star_api_consume_last_mint_result(char* item_name_out, size_t item_name_size, char* nft_id_out, size_t nft_id_size, char* hash_out, size_t hash_size);
/** Consume last background error (mint/add_item failure or pickup not queued). Writes message to buf (null-terminated). Returns 1 if an error was available, 0 otherwise. Call from game pump to show in console. */
#define STAR_API_HAS_CONSUME_LAST_BACKGROUND_ERROR 1
int star_api_consume_last_background_error(char* buf, size_t size);
/** Consume one STAR log message for the game console. Returns 1 if a message was copied to buf, 0 otherwise. Call from game pump each frame. */
int star_api_consume_console_log(char* buf, size_t size);
void star_api_set_callback(star_api_callback_t callback, void* user_data);

#ifdef __cplusplus
}
#endif

#endif

