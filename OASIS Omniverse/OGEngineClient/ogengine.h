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
    /* Optional: which game binary is running (e.g. "ODOOM", "OQUAKE") for cross-game quest tracker rows. NULL = use quest/objective metadata + last progress only. */
    const char* client_game_source;
    /* 0 = remote (HTTP WEB5/WEB4). 1 = native in-process OASIS (requires a ogengine build that embeds HyperDrive; default library returns OGENGINE_ERROR_INIT_FAILED). */
    int32_t transport;
    /* Optional: UTF-8 path to OASIS_DNA.json for native transport (for future native host). */
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

/** Operation type for ogengine_set_operation_callback. Game can run "profile loaded" only when type is OGENGINE_OP_PROFILE_LOADED. Other values (1-27) identify get_avatar_id, has_item, get_inventory, etc.; see OGEngineClient.cs StarApiOp* constants. */
#define OGENGINE_OP_PROFILE_LOADED 0
#define OGENGINE_OP_GET_INVENTORY 3
#define OGENGINE_OP_QUESTS_CACHE_REFRESHED 28
typedef void (*ogengine_operation_callback_t)(ogengine_result_t result, int operation_type, void* user_data);

ogengine_result_t ogengine_init(const ogengine_config_t* config);
void ogengine_set_quest_progress_cache_refresh(int mode);
ogengine_result_t ogengine_authenticate(const char* username, const char* password);
/** Same as ogengine_authenticate but on success writes JWT to jwt_buf (for oasisstar.json). jwt_buf can be NULL. */
ogengine_result_t ogengine_authenticate_with_jwt_out(const char* username, const char* password, char* jwt_buf, size_t jwt_size);
/** Set JWT from persisted session (e.g. oasisstar.json). Call ogengine_restore_session to validate and load profile. */
ogengine_result_t ogengine_set_saved_session(const char* jwt);
/** Start async session restore (GET avatar/current). Callback is invoked on success/failure. Does not block. */
ogengine_result_t ogengine_restore_session(void);
/** Write current username to buf for saving to oasisstar.json. Returns bytes written or 0. */
int ogengine_get_current_username(char* buf, size_t buf_size);
/** Write current JWT to buf for saving to oasisstar.json. Returns bytes written or 0. Caller should not log. */
int ogengine_get_current_jwt(char* buf, size_t buf_size);
/** Set refresh token from persisted session (e.g. oasisstar.json). Call after ogengine_set_saved_session when restoring so 401 can trigger token refresh. */
void ogengine_set_refresh_token(const char* refresh_token);
/** Write current refresh token to buf for saving to oasisstar.json. Returns bytes written or 0. Caller should not log. */
int ogengine_get_current_refresh_token(char* buf, size_t buf_size);
/** Returns 1 if session was cleared due to expired JWT and refresh failed (or no refresh token). When saving oasisstar.json, clear jwt_token/refresh_token so the next launch does not try to restore a dead session. */
int ogengine_is_session_expired(void);
/* Set WEB4 OASIS API base URI (used for avatar auth + NFT mint endpoints). */
ogengine_result_t ogengine_set_oasis_base_url(const char* oasis_base_url);
void ogengine_cleanup(void);
bool ogengine_has_item(const char* item_name);
ogengine_result_t ogengine_get_inventory(ogengine_item_list_t** item_list);
/** Request inventory fetch in background. When done, operation_callback is invoked with OGENGINE_OP_GET_INVENTORY. Non-blocking. Call ogengine_get_inventory after callback to read cache. */
void ogengine_request_inventory_in_background(void);
/** Clear client inventory cache. Next ogengine_get_inventory will do a real HTTP GET. Use to verify API actually returns items (e.g. after add_item). */
void ogengine_invalidate_inventory_cache(void);
/** Clear all client caches (e.g. inventory). Same effect as ogengine_invalidate_inventory_cache. */
void ogengine_clear_cache(void);
void ogengine_free_item_list(ogengine_item_list_t* item_list);
/** quantity: amount to add (or initial if new). stack: 1 = if item exists increment quantity; 0 = if exists return error "item already exists". */
ogengine_result_t ogengine_add_item(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* nft_id, int quantity, int stack);
/** Mint an NFT for an inventory item via WEB4 OASIS API (NFTHolon). Returns NFT ID; pass to ogengine_add_item as nft_id. provider may be NULL (default SolanaOASIS). nft_id_out must be at least 128 bytes. hash_out optional (128 bytes) for tx hash/signature; pass NULL to omit. send_to_address_after_minting optional wallet address to send the minted NFT to (from oasisstar.json); pass NULL to omit. */
ogengine_result_t ogengine_mint_inventory_nft(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* provider, char* nft_id_out, char* hash_out, const char* send_to_address_after_minting);
bool ogengine_use_item(const char* item_name, const char* context);
/** Queue one add-item job (batching). nft_id may be NULL. quantity and stack: same as ogengine_add_item (default 1, 1). */
void ogengine_queue_add_item(const char* item_name, const char* description, const char* game_source, const char* item_type, const char* nft_id, int quantity, int stack);
/** Queue pickup with optional mint; C# client does mint (if do_mint) then add_item in background. Same pattern as queue_add_item. */
#define OGENGINE_HAS_QUEUE_PICKUP_WITH_MINT 1
void ogengine_queue_pickup_with_mint(const char* item_name, const char* description, const char* game_source, const char* item_type, int do_mint, const char* provider, const char* send_to_address_after_minting, int quantity);
/** Queue quest progress only (no inventory add). Use when the engine consumed health/armor on pickup so add_item is skipped but objectives must still advance. */
#define OGENGINE_HAS_QUEUE_QUEST_PROGRESS_FROM_PICKUP 1
void ogengine_queue_quest_progress_from_pickup(const char* game_source, const char* item_type, const char* item_name);
ogengine_result_t ogengine_flush_add_item_jobs(void);
void ogengine_queue_use_item(const char* item_name, const char* context);
ogengine_result_t ogengine_flush_use_item_jobs(void);
ogengine_result_t ogengine_start_quest(const char* quest_id);
/** Queue start quest; when that succeeds, persist active quest + objective on avatar (same as ogengine_set_active_quest). Use from game when user picks an objective on a Not Started quest so set-active runs after start completes (avoids racing the async start). objective_id required. */
#define OGENGINE_HAS_START_QUEST_THEN_SET_ACTIVE_OBJECTIVE 1
ogengine_result_t ogengine_start_quest_then_set_active_objective(const char* quest_id, const char* objective_id);
ogengine_result_t ogengine_complete_quest_objective(const char* quest_id, const char* objective_id, const char* game_source);
ogengine_result_t ogengine_complete_quest(const char* quest_id);
/** Write serialized quest list (all quests for avatar) to buf for game UI. Returns bytes written, or negative ogengine_result_t on error. Format: "Q\tid\tname\tdesc\tstatus\tpct\n" per quest, "O\tid\tdesc\tdone\n" per objective, "---\n" between quests. Filter by status (Not Started, In Progress, Completed) in UI with checkboxes. Uses cache; never blocks. */
int ogengine_get_quests_string(char* buf, size_t buf_size);
/** Write serialized top-level quests only (no sub-quests) to buf for left list. Same format as ogengine_get_quests_string. Use for main quest list so sub-quests do not appear in the left panel. */
int ogengine_get_top_level_quests_string(char* buf, size_t buf_size);
/** Write display name of current tracked quest (from cache) to buf, null-terminated. Returns bytes written or 0 if not available. Use so HUD shows quest name as soon as quest list loads after beam-in. */
int ogengine_get_tracker_quest_name(char* buf, size_t buf_size);
/** Write serialized sub-quests (child quests with parent_quest_id) to buf for right panel. Same format as ogengine_get_quests_string. parent_quest_id must be non-NULL. */
int ogengine_get_quest_sub_quests_string(const char* parent_quest_id, char* buf, size_t buf_size);
/** Write serialized objectives from the quest's Objectives collection for parent_quest_id to buf for right panel. Same format as ogengine_get_quests_string. parent_quest_id must be non-NULL. */
int ogengine_get_quest_objectives_string(const char* parent_quest_id, char* buf, size_t buf_size);
/** Incremented when on-demand objective fetch merges into cache; games may re-fetch objectives when this changes. */
#define OGENGINE_HAS_QUEST_OBJECTIVES_CACHE_VERSION 1
int ogengine_get_quest_objectives_cache_version(void);
/** Write serialized prerequisite quests (id, name, desc) for the given quest_id to buf for right panel. Same format as ogengine_get_quests_string. quest_id must be non-NULL. */
int ogengine_get_quest_prereqs_string(const char* quest_id, char* buf, size_t buf_size);
/** Write requirement/progress lines for quest (and optional objective_id) to buf. One line per requirement e.g. "Killed 3/10 monsters in ODOOM". objective_id may be NULL for quest-level only. */
int ogengine_get_quest_objective_requirements_string(const char* quest_id, const char* objective_id, char* buf, size_t buf_size);
/** Write one progress line per objective for the tracker (newline-separated). For HUD cycle 1,2,3,All. quest_id must be non-NULL. */
int ogengine_get_quest_tracker_objectives_string(const char* quest_id, char* buf, size_t buf_size);
/** Return 0-based index of first incomplete objective for the tracked quest, or 0 if all complete. */
int ogengine_get_quest_tracker_active_objective_index(const char* quest_id);
/** Clear quest cache so next ogengine_get_quests_string triggers a fresh fetch. Call when opening the quest popup so data is up to date. */
void ogengine_invalidate_quest_cache(void);
/** Start a background refresh of the quest cache without clearing it. Show existing cache in the UI immediately; list updates when the callback returns. */
void ogengine_refresh_quest_cache_in_background(void);
/** 1 = in-game quest list popup is open, 0 = closed. While open, quest progress (POST/merge) and applying GET all-for-avatar into the cache are skipped. List uses the same in-memory cache updated by progress merge during gameplay. */
void ogengine_set_quest_popup_open(int is_open);
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
/** Queue add XP for the beamed-in avatar (e.g. on monster kill). Flushed with add-item jobs or on next API sync. amount must be > 0. */
void ogengine_queue_add_xp(int amount);
/** Queue monster kill (XP + optional mint + add to inventory). All work runs on background thread; never blocks. provider/game_source may be NULL (game_source NULL/empty = ODOOM). */
void ogengine_queue_monster_kill(const char* engine_name, const char* display_name, int xp, int is_boss, int do_mint, const char* provider, const char* game_source);
/** Report current level elapsed time (seconds) for quest time-limit objectives (e.g. Quake cl.time / scoreboard). Throttle in game ~10s. game_source e.g. Quake, ODOOM. */
void ogengine_queue_quest_level_time(const char* game_source, int level_elapsed_seconds);
/** Get last known avatar XP (from get-current-avatar or after add-xp). Returns 0 if not loaded. Write to *xp_out; pass NULL to skip. Returns 1 if value is valid, 0 otherwise. */
int ogengine_get_avatar_xp(int* xp_out);
/** Get last known avatar karma score (from get-current-avatar). Writes to *karma_out; pass NULL to skip. Returns 1 if valid, 0 otherwise. */
int ogengine_get_avatar_karma(long* karma_out);
/* REDUNDANT: removed. Use ogengine_refresh_avatar_profile() only. */
/* void ogengine_refresh_avatar_xp(void); */
/** Kick off avatar profile refresh (XP + quest/objective) in background; callback when done. Call on beam-in. */
void ogengine_refresh_avatar_profile(void);
/** Get last active quest ID from avatar detail (restored after beam-in). Writes GUID string to buf, null-terminated. Returns 1 if had value, 0 otherwise. */
int ogengine_get_active_quest_id(char* buf, size_t buf_size);
/** Get last active objective ID from avatar detail (restored after beam-in). Writes GUID string to buf, null-terminated. Returns 1 if had value, 0 otherwise. */
int ogengine_get_active_objective_id(char* buf, size_t buf_size);
/** Persist active quest and objective on avatar detail. quest_id and objective_id can be NULL/empty to clear. Call when user sets tracker in game. */
ogengine_result_t ogengine_set_active_quest(const char* quest_id, const char* objective_id);
const char* ogengine_get_last_error(void);
/** Consume last mint result from background pickup-with-mint. Writes item name, NFT ID, and hash to buffers (null-terminated). Returns 1 if a result was available, 0 otherwise. Call from game pump/frame to show mint results in console. */
#define OGENGINE_HAS_CONSUME_LAST_MINT 1
int ogengine_consume_last_mint_result(char* item_name_out, size_t item_name_size, char* nft_id_out, size_t nft_id_size, char* hash_out, size_t hash_size);
/** Consume last background error (mint/add_item failure or pickup not queued). Writes message to buf (null-terminated). Returns 1 if an error was available, 0 otherwise. Call from game pump to show in console. */
#define OGENGINE_HAS_CONSUME_LAST_BACKGROUND_ERROR 1
int ogengine_consume_last_background_error(char* buf, size_t size);
/** Consume one STAR log message for the game console. Returns 1 if a message was copied to buf, 0 otherwise. Call from game pump each frame. */
int ogengine_consume_console_log(char* buf, size_t size);
/** Append a line to ogengine.log (same file as C# StarApiLog). Use from game code so door-check and other STAR debug messages appear in the log for pasting. message can be NULL (no-op). */
void ogengine_log_to_file(const char* message);
/** Enable (1) or disable (0) STAR API debug logging in the client. When on, quest and other API requests log URI and response to ogengine.log and console. Call when user toggles "star debug on|off". */
void ogengine_set_debug(int enabled);
void ogengine_set_callback(ogengine_callback_t callback, void* user_data);
/** Optional: set callback with operation_type so game only reacts to profile-loaded. If set, profile refresh uses this; else uses ogengine_set_callback. */
void ogengine_set_operation_callback(ogengine_operation_callback_t callback, void* user_data);

/* ---- Cross-game portal / teleport API ----------------------------------------
 * These functions implement the OASIS Portal system so players can warp between
 * OGames.  Call ogengine_poll_teleport_request each frame (or from the same tick
 * that calls ogengine_sync_pump / ogengine_sync_tick).  On receipt, perform the
 * game-native warp then call ogengine_confirm_teleport_arrival so the STAR relay
 * knows the client has arrived and clears the pending request.
 * ogengine_request_teleport initiates an outgoing warp to another OGame.
 * ----------------------------------------------------------------------------- */

/** Poll for a pending incoming teleport request from another OGame.
 *  Returns 1 if a request is waiting; out_map, out_x/y/z are filled in.
 *  out_map receives the target map name (null-terminated, up to map_len bytes).
 *  Returns 0 when no request is pending (out args are unchanged). */
int ogengine_poll_teleport_request(char* out_map, size_t map_len, float* out_x, float* out_y, float* out_z);

/** Confirm that the local player has been warped to the requested position.
 *  Call immediately after performing the game-native teleport move so the STAR
 *  relay clears the pending request and notifies the source OGame. */
void ogengine_confirm_teleport_arrival(void);

/** Initiate an outgoing OASIS Portal teleport to another OGame.
 *  target_game: canonical game ID (e.g. "ODOOM", "OQUAKE").
 *  target_map:  map/level name in that game.
 *  x/y/z:       spawn position in target-game coordinates (0 = use default). */
void ogengine_request_teleport(const char* target_game, const char* target_map, float x, float y, float z);

/* ---- Cross-game spawn-event API ----------------------------------------------
 * The STAR relay can instruct a game to spawn an entity (e.g. a monster or
 * object imported from another OGame).  Poll each frame after sync_pump/tick.
 * On receipt, spawn the entity in game-native code, then confirm so the relay
 * removes the event from the queue.
 * ----------------------------------------------------------------------------- */

/** Poll for a pending cross-game spawn event.
 *  Returns 1 if an event is waiting; out_entity_id, out_x/y/z are filled in.
 *  out_entity_id receives the OASIS entity ID (null-terminated, up to id_len bytes).
 *  Returns 0 when no event is pending (out args are unchanged). */
int ogengine_poll_spawn_event(char* out_entity_id, size_t id_len, float* out_x, float* out_y, float* out_z);

/** Confirm that the spawn event has been handled.
 *  Call after the entity has been spawned in the game world so the relay removes
 *  the event from the queue and notifies the originating OGame. */
void ogengine_confirm_spawn(const char* entity_id);

/* ---- Cross-game event polling -----------------------------------------------
 * Poll for queued cross-game events (ShowNarration, PlayAudio, PlayVideo,
 * OpenWebsite, UnlockPortal) from quest progress responses.  Call each frame.
 * Returns 1 and writes event JSON to out_json if an event is queued; 0 otherwise. */
int ogengine_poll_cross_game_event(char* out_json, size_t json_len);

/* ---- Inventory grant polling ------------------------------------------------
 * Poll for a pending inventory grant (item GUID) from the STAR relay.
 * Returns 1 and writes GUID to out_guid (null-terminated) if found; 0 otherwise. */
int ogengine_poll_inventory_grant(char* out_guid, size_t guid_len);

/* ---- Portal unlock notification ---------------------------------------------
 * Notify OGEditor/OmniverseKernel that the named OASIS portal has been unlocked.
 * Writes oasis_portal_unlock_{portalId}.json to %TEMP% for OGEditor pickup. */
void ogengine_notify_portal_unlock(const char* portal_id);

#ifdef __cplusplus
}
#endif

#endif

