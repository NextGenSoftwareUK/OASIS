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

/** Operation type for star_api_set_operation_callback. Game can run "profile loaded" only when type is STAR_API_OP_PROFILE_LOADED. Other values (1-27) identify get_avatar_id, has_item, get_inventory, etc.; see StarApiClient.cs StarApiOp* constants. */
#define STAR_API_OP_PROFILE_LOADED 0
#define STAR_API_OP_GET_INVENTORY 3
#define STAR_API_OP_QUESTS_CACHE_REFRESHED 28
typedef void (*star_api_operation_callback_t)(star_api_result_t result, int operation_type, void* user_data);

star_api_result_t star_api_init(const star_api_config_t* config);
void star_api_set_quest_progress_cache_refresh(int mode);
star_api_result_t star_api_authenticate(const char* username, const char* password);
/** Same as star_api_authenticate but on success writes JWT to jwt_buf (for oasisstar.json). jwt_buf can be NULL. */
star_api_result_t star_api_authenticate_with_jwt_out(const char* username, const char* password, char* jwt_buf, size_t jwt_size);
/** Set JWT from persisted session (e.g. oasisstar.json). Call star_api_restore_session to validate and load profile. */
star_api_result_t star_api_set_saved_session(const char* jwt);
/** Start async session restore (GET avatar/current). Callback is invoked on success/failure. Does not block. */
star_api_result_t star_api_restore_session(void);
/** Write current username to buf for saving to oasisstar.json. Returns bytes written or 0. */
int star_api_get_current_username(char* buf, size_t buf_size);
/** Write current JWT to buf for saving to oasisstar.json. Returns bytes written or 0. Caller should not log. */
int star_api_get_current_jwt(char* buf, size_t buf_size);
/** Set refresh token from persisted session (e.g. oasisstar.json). Call after star_api_set_saved_session when restoring so 401 can trigger token refresh. */
void star_api_set_refresh_token(const char* refresh_token);
/** Write current refresh token to buf for saving to oasisstar.json. Returns bytes written or 0. Caller should not log. */
int star_api_get_current_refresh_token(char* buf, size_t buf_size);
/** Returns 1 if session was cleared due to expired JWT and refresh failed (or no refresh token). When saving oasisstar.json, clear jwt_token/refresh_token so the next launch does not try to restore a dead session. */
int star_api_is_session_expired(void);
/* Set WEB4 OASIS API base URI (used for avatar auth + NFT mint endpoints). */
star_api_result_t star_api_set_oasis_base_url(const char* oasis_base_url);
void star_api_cleanup(void);
bool star_api_has_item(const char* item_name);
star_api_result_t star_api_get_inventory(star_item_list_t** item_list);
/** Request inventory fetch in background. When done, operation_callback is invoked with STAR_API_OP_GET_INVENTORY. Non-blocking. Call star_api_get_inventory after callback to read cache. */
void star_api_request_inventory_in_background(void);
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
/** Queue quest progress only (no inventory add). Use when the engine consumed health/armor on pickup so add_item is skipped but objectives must still advance. */
#define STAR_API_HAS_QUEUE_QUEST_PROGRESS_FROM_PICKUP 1
void star_api_queue_quest_progress_from_pickup(const char* game_source, const char* item_type, const char* item_name);
star_api_result_t star_api_flush_add_item_jobs(void);
void star_api_queue_use_item(const char* item_name, const char* context);
star_api_result_t star_api_flush_use_item_jobs(void);
star_api_result_t star_api_start_quest(const char* quest_id);
star_api_result_t star_api_complete_quest_objective(const char* quest_id, const char* objective_id, const char* game_source);
star_api_result_t star_api_complete_quest(const char* quest_id);
/** Write serialized quest list (all quests for avatar) to buf for game UI. Returns bytes written, or negative star_api_result_t on error. Format: "Q\tid\tname\tdesc\tstatus\tpct\n" per quest, "O\tid\tdesc\tdone\n" per objective, "---\n" between quests. Filter by status (Not Started, In Progress, Completed) in UI with checkboxes. Uses cache; never blocks. */
int star_api_get_quests_string(char* buf, size_t buf_size);
/** Write serialized top-level quests only (no sub-quests) to buf for left list. Same format as star_api_get_quests_string. Use for main quest list so sub-quests do not appear in the left panel. */
int star_api_get_top_level_quests_string(char* buf, size_t buf_size);
/** Write display name of current tracked quest (from cache) to buf, null-terminated. Returns bytes written or 0 if not available. Use so HUD shows quest name as soon as quest list loads after beam-in. */
int star_api_get_tracker_quest_name(char* buf, size_t buf_size);
/** Write serialized sub-quests (child quests with parent_quest_id) to buf for right panel. Same format as star_api_get_quests_string. parent_quest_id must be non-NULL. */
int star_api_get_quest_sub_quests_string(const char* parent_quest_id, char* buf, size_t buf_size);
/** Write serialized objectives from the quest's Objectives collection for parent_quest_id to buf for right panel. Same format as star_api_get_quests_string. parent_quest_id must be non-NULL. */
int star_api_get_quest_objectives_string(const char* parent_quest_id, char* buf, size_t buf_size);
/** Incremented when on-demand objective fetch merges into cache; games may re-fetch objectives when this changes. */
#define STAR_API_HAS_QUEST_OBJECTIVES_CACHE_VERSION 1
int star_api_get_quest_objectives_cache_version(void);
/** Write serialized prerequisite quests (id, name, desc) for the given quest_id to buf for right panel. Same format as star_api_get_quests_string. quest_id must be non-NULL. */
int star_api_get_quest_prereqs_string(const char* quest_id, char* buf, size_t buf_size);
/** Write requirement/progress lines for quest (and optional objective_id) to buf. One line per requirement e.g. "Killed 3/10 monsters in ODOOM". objective_id may be NULL for quest-level only. */
int star_api_get_quest_objective_requirements_string(const char* quest_id, const char* objective_id, char* buf, size_t buf_size);
/** Write one progress line per objective for the tracker (newline-separated). For HUD cycle 1,2,3,All. quest_id must be non-NULL. */
int star_api_get_quest_tracker_objectives_string(const char* quest_id, char* buf, size_t buf_size);
/** Return 0-based index of first incomplete objective for the tracked quest, or 0 if all complete. */
int star_api_get_quest_tracker_active_objective_index(const char* quest_id);
/** Clear quest cache so next star_api_get_quests_string triggers a fresh fetch. Call when opening the quest popup so data is up to date. */
void star_api_invalidate_quest_cache(void);
/** Start a background refresh of the quest cache without clearing it. Show existing cache in the UI immediately; list updates when the callback returns. */
void star_api_refresh_quest_cache_in_background(void);
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
/** Report current level elapsed time (seconds) for quest time-limit objectives (e.g. Quake cl.time / scoreboard). Throttle in game ~10s. game_source e.g. Quake, ODOOM. */
void star_api_queue_quest_level_time(const char* game_source, int level_elapsed_seconds);
/** Get last known avatar XP (from get-current-avatar or after add-xp). Returns 0 if not loaded. Write to *xp_out; pass NULL to skip. Returns 1 if value is valid, 0 otherwise. */
int star_api_get_avatar_xp(int* xp_out);
/* REDUNDANT: removed. Use star_api_refresh_avatar_profile() only. */
/* void star_api_refresh_avatar_xp(void); */
/** Kick off avatar profile refresh (XP + quest/objective) in background; callback when done. Call on beam-in. */
void star_api_refresh_avatar_profile(void);
/** Get last active quest ID from avatar detail (restored after beam-in). Writes GUID string to buf, null-terminated. Returns 1 if had value, 0 otherwise. */
int star_api_get_active_quest_id(char* buf, size_t buf_size);
/** Get last active objective ID from avatar detail (restored after beam-in). Writes GUID string to buf, null-terminated. Returns 1 if had value, 0 otherwise. */
int star_api_get_active_objective_id(char* buf, size_t buf_size);
/** Persist active quest and objective on avatar detail. quest_id and objective_id can be NULL/empty to clear. Call when user sets tracker in game. */
star_api_result_t star_api_set_active_quest(const char* quest_id, const char* objective_id);
const char* star_api_get_last_error(void);
/** Consume last mint result from background pickup-with-mint. Writes item name, NFT ID, and hash to buffers (null-terminated). Returns 1 if a result was available, 0 otherwise. Call from game pump/frame to show mint results in console. */
#define STAR_API_HAS_CONSUME_LAST_MINT 1
int star_api_consume_last_mint_result(char* item_name_out, size_t item_name_size, char* nft_id_out, size_t nft_id_size, char* hash_out, size_t hash_size);
/** Consume last background error (mint/add_item failure or pickup not queued). Writes message to buf (null-terminated). Returns 1 if an error was available, 0 otherwise. Call from game pump to show in console. */
#define STAR_API_HAS_CONSUME_LAST_BACKGROUND_ERROR 1
int star_api_consume_last_background_error(char* buf, size_t size);
/** Consume one STAR log message for the game console. Returns 1 if a message was copied to buf, 0 otherwise. Call from game pump each frame. */
int star_api_consume_console_log(char* buf, size_t size);
/** Append a line to star_api.log (same file as C# StarApiLog). Use from game code so door-check and other STAR debug messages appear in the log for pasting. message can be NULL (no-op). */
void star_api_log_to_file(const char* message);
/** Enable (1) or disable (0) STAR API debug logging in the client. When on, quest and other API requests log URI and response to star_api.log and console. Call when user toggles "star debug on|off". */
void star_api_set_debug(int enabled);
void star_api_set_callback(star_api_callback_t callback, void* user_data);
/** Optional: set callback with operation_type so game only reacts to profile-loaded. If set, profile refresh uses this; else uses star_api_set_callback. */
void star_api_set_operation_callback(star_api_operation_callback_t callback, void* user_data);

#ifdef __cplusplus
}
#endif

#endif

