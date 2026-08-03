/**
 * OASIS WEB5 STAR API - C/C++ Wrapper for Game Integration
 * Native ABI compatible header for OGEngineClient NativeAOT exports.
 */

#ifndef OGENGINE_H
#define OGENGINE_H

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
    const char* client_game_source;
    int32_t transport;
    const char* oasis_dna_path;
} ogengine_config_t;

typedef struct {
    char id[64];
    char name[256];
    char description[512];
    char game_source[64];
    char item_type[64];
    char nft_id[128];  /* NFTId from MetaData when item is linked to NFTHolon; empty when not an NFT item */
    int quantity;      /* Stack size. API increments if item exists and stack=1; otherwise new item gets this. */
} ogengine_item_t;

typedef struct {
    ogengine_item_t* items;
    size_t count;
    size_t capacity;
} ogengine_item_list_t;

typedef enum {
    OGENGINE_SUCCESS = 0,
    OGENGINE_ERROR_INIT_FAILED = -1,
    OGENGINE_ERROR_NOT_INITIALIZED = -2,
    OGENGINE_ERROR_NETWORK = -3,
    OGENGINE_ERROR_INVALID_PARAM = -4,
    OGENGINE_ERROR_API_ERROR = -5
} ogengine_result_t;

typedef void (*ogengine_callback_t)(ogengine_result_t result, void* user_data);

ogengine_result_t ogengine_init(const ogengine_config_t* config);
ogengine_result_t ogengine_authenticate(const char* username, const char* password);
/* Set WEB4 OASIS API base URI (used for avatar auth + NFT mint endpoints). */
ogengine_result_t ogengine_set_oasis_base_url(const char* oasis_base_url);
void ogengine_cleanup(void);
bool ogengine_has_item(const char* item_name);
ogengine_result_t ogengine_get_inventory(ogengine_item_list_t** item_list);
/** Clear client inventory cache. Next ogengine_get_inventory will do a real HTTP GET. Use to verify API actually returns items (e.g. after add_item). */
void ogengine_invalidate_inventory_cache(void);
/** Clear all client caches (e.g. inventory). Same effect as ogengine_invalidate_inventory_cache. */
void ogengine_clear_cache(void);
void ogengine_free_item_list(ogengine_item_list_t* item_list);
/** quantity: amount to add (or initial if new). stack: 1 = if item exists increment quantity; 0 = if exists return error "item already exists". */
ogengine_result_t ogengine_add_item(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* nft_id, int quantity, int stack);
/** Mint an NFT for an inventory item (WEB4 NFTHolon). Returns NFT ID; pass to ogengine_add_item as nft_id. provider may be NULL (default SolanaOASIS). nft_id_out must be at least 128 bytes. hash_out optional (128 bytes) for tx hash/signature; pass NULL to omit. */
ogengine_result_t ogengine_mint_inventory_nft(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* provider, char* nft_id_out, char* hash_out, const char* send_to_address_after_minting);
bool ogengine_use_item(const char* item_name, const char* context);
/** Queue one add-item job (batching). nft_id may be NULL. quantity and stack: same as ogengine_add_item (default 1, 1). */
void ogengine_queue_add_item(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* nft_id, int quantity, int stack);
/** Queue pickup with optional mint; C# client does mint (if do_mint) then add_item in background. Same pattern as queue_add_item. */
#define OGENGINE_HAS_QUEUE_PICKUP_WITH_MINT 1
void ogengine_queue_pickup_with_mint(const char* item_name, const char* description, const char* game_source, const char* item_type, int do_mint, const char* provider, const char* send_to_address_after_minting, int quantity);
ogengine_result_t ogengine_flush_add_item_jobs(void);
void ogengine_queue_use_item(const char* item_name, const char* context);
ogengine_result_t ogengine_flush_use_item_jobs(void);
ogengine_result_t ogengine_start_quest(const char* quest_id);
ogengine_result_t ogengine_start_quest_then_set_active_objective(const char* quest_id, const char* objective_id);
ogengine_result_t ogengine_complete_quest_objective(const char* quest_id, const char* objective_id, const char* game_source);
ogengine_result_t ogengine_complete_quest(const char* quest_id);
/** Write serialized quest list to buf (format: "Q\t<id>\t<name>\t<desc>\t<status>\t<pct>\n" per quest).
 *  Returns bytes written (excl. NUL). Cache miss returns "Loading...\n" and starts background refresh. */
int ogengine_get_quests_string(char* buf, size_t buf_size);
/** Invalidate quest cache so next ogengine_get_quests_string fetches fresh data from STAR API. */
void ogengine_invalidate_quest_cache(void);
/** provider: NFT provider (e.g. SolanaOASIS); NULL/empty = use default. Same as nft_provider in oasisstar.json. */
ogengine_result_t ogengine_create_monster_nft(const char* monster_name, const char* description, const char* game_source, const char* monster_stats, const char* provider, char* nft_id_out);
ogengine_result_t ogengine_deploy_boss_nft(const char* nft_id, const char* target_game, const char* location);
ogengine_result_t ogengine_get_avatar_id(char* avatar_id_out, size_t avatar_id_size);
/** Set avatar ID on the client (e.g. after SSO from C++ auth result). Does not change JWT. */
ogengine_result_t ogengine_set_avatar_id(const char* avatar_id);
/** Send item from current avatar's inventory to another avatar. Target = username or avatar Id. item_id optional (NULL or empty = match by name). */
ogengine_result_t ogengine_send_item_to_avatar(const char* target_username_or_avatar_id, const char* item_name, int quantity, const char* item_id);
/** Send item from current avatar's inventory to a clan. Target = clan name (or username). item_id optional (NULL or empty = match by name). */
ogengine_result_t ogengine_send_item_to_clan(const char* clan_name_or_target, const char* item_name, int quantity, const char* item_id);
const char* ogengine_get_last_error(void);
/** Consume last mint result from background pickup-with-mint. Writes item name, NFT ID, and hash to buffers (null-terminated). Returns 1 if a result was available, 0 otherwise. */
int ogengine_consume_last_mint_result(char* item_name_out, size_t item_name_size, char* nft_id_out, size_t nft_id_size, char* hash_out, size_t hash_size);
/** Consume last background error (mint/add_item failure or pickup not queued). buf null-terminated. Returns 1 if error was available. */
int ogengine_consume_last_background_error(char* buf, size_t size);
void ogengine_set_callback(ogengine_callback_t callback, void* user_data);

/* ── Cross-game teleportation ─────────────────────────────────────────── */

/** Request teleport to another game+map. Called by game when player steps on oasis_portal entity.
 *  Writes %TEMP%\oasis_teleport_<avatarId>.json for OmniverseKernel to pick up. */
void ogengine_request_teleport(const char* target_game, const char* target_map,
                                float x, float y, float z);

/** Poll: did OmniverseKernel request a teleport INTO this game?
 *  Returns 1 if a pending request exists and fills out_map/out_x/y/z; 0 otherwise.
 *  Reads %TEMP%\oasis_teleport_arrive_<avatarId>.json and deletes it after reading. */
int  ogengine_poll_teleport_request(char* out_map,  size_t map_len,
                                     float* out_x, float* out_y, float* out_z);

/** Game calls this after it has loaded the target map at the requested position. */
void ogengine_confirm_teleport_arrival(void);

/* ── Cross-game entity spawning ───────────────────────────────────────── */

/** Poll for a pending cross-game spawn event pushed by the STAR API.
 *  Returns 1 if an event exists; fills entity_id (e.g. "oasset_quake_shambler") and position. */
int  ogengine_poll_spawn_event(char* out_entity_id, size_t id_len,
                                float* out_x, float* out_y, float* out_z);

/** Game calls this after successfully spawning the entity. */
void ogengine_confirm_spawn(const char* entity_id);

/* ── Map entity list ──────────────────────────────────────────────────── */

/** Fetch the cross-game entity list for a given map (from oasis_{mapname}.json sidecar via STAR API).
 *  out_json receives a JSON array: [{"entityId":"oasset_quake_shambler","x":100,"y":0,"z":64}, ...]
 *  Returns OGENGINE_SUCCESS or error code. */
ogengine_result_t ogengine_get_map_entities(const char* game_id, const char* map_name,
                                             char* out_json, size_t buf_len);

#ifdef __cplusplus
}
#endif

#endif

