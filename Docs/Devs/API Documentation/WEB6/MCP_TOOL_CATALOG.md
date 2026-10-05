# OASIS MCP Tool Catalog

This catalog is generated from the `[McpServerTool]` registrations in the OASIS MCP Server. It is the authoritative inventory for the 512 callable commands shipped across WEB4–WEB10.

| Layer | Tools | Scope |
|---|---:|---|
| WEB4 | 361 | Identity, avatars, karma, holons, search, chat, wallets, keys, NFTs, data, providers and HyperDrive |
| WEB5 | 95 | STAR, OAPPs, quests, missions, inventory, NFTs, zomes, templates, libraries, runtimes and games |
| WEB6 | 39 | AI providers, completion, embeddings, FAHRN agents, BRAID memory, orchestration, DID/VC and external memory |
| WEB7 | 7 | Consent-based symbiosis sessions, bio-signals and collective-consciousness spaces |
| WEB8 | 8 | Galactic mesh nodes, routing, relay and protocol translation |
| WEB9 | 1 | Singularity state and convergence |
| WEB10 | 1 | Source connection and universal state |
| **Total** | **512** | |

All commands are exposed over MCP stdio. Most return a JSON-serialized `OASISResult<T>` envelope: successful calls set `isError` to `false` and place data in `result`; failures set `isError` to `true` and explain the cause in `message`.

## WEB4 commands (361)

| Command | Description |
|---|---|
| `web4_avatar_add_karma` | WEB4: adds positive karma to an avatar via AvatarManager. karmaType is KarmaTypePositive (e.g. 'JoinOASIS'), sourceType is KarmaSourceType (e.g. 'API'). |
| `web4_avatar_add_karma_to_avatar` | WEB4: adds positive karma to an avatar. karmaType is a KarmaTypePositive value (e.g. 'JoinOASIS'). karmaSourceType is a KarmaSourceType value (e.g. 'API'). karmaSourceTitle and karmaSourceDesc are free-text labels. |
| `web4_avatar_add_xp` | WEB4: adds experience points (XP) to an avatar. avatarId is the GUID of the avatar. amount must be >= 0 (0 returns current XP without changing it). |
| `web4_avatar_authenticate` | WEB4: authenticates an avatar by username/password and returns the avatar (including its JWT token) on success. |
| `web4_avatar_delete` | WEB4: deletes (soft-deletes by default) an avatar by id. |
| `web4_avatar_delete_by_email` | WEB4: deletes (soft-deletes) an avatar by email address. |
| `web4_avatar_delete_by_username` | WEB4: deletes (soft-deletes) an avatar by username. |
| `web4_avatar_forgot_password` | WEB4: triggers the forgot-password email flow for the given avatar email address. |
| `web4_avatar_get_portrait_by_email` | WEB4: retrieves an avatar's portrait image by email address. |
| `web4_avatar_get_portrait_by_id` | WEB4: retrieves an avatar's portrait image by GUID id. |
| `web4_avatar_get_portrait_by_username` | WEB4: retrieves an avatar's portrait image by username. |
| `web4_avatar_get_terms` | WEB4: retrieves the OASIS Terms and Conditions text. |
| `web4_avatar_get_uma_json_by_email` | WEB4: gets the 3D model UMA JSON for an avatar by email address. |
| `web4_avatar_get_uma_json_by_id` | WEB4: gets the 3D model UMA JSON for an avatar by its GUID id. |
| `web4_avatar_get_uma_json_by_username` | WEB4: gets the 3D model UMA JSON for an avatar by username. |
| `web4_avatar_inventory_add_item` | WEB4: adds an item to an avatar's inventory. inventoryItemJson is a JSON object matching InventoryItem (fields: Name, Description, HolonSubType, Quantity, etc.). |
| `web4_avatar_inventory_get` | WEB4: gets all inventory items for an avatar by GUID id. |
| `web4_avatar_inventory_get_item` | WEB4: gets a single inventory item from an avatar's inventory by item GUID id. |
| `web4_avatar_inventory_has_item` | WEB4: checks whether an avatar has a specific inventory item by item GUID id. |
| `web4_avatar_inventory_has_item_by_name` | WEB4: checks whether an avatar has a specific inventory item by item name. |
| `web4_avatar_inventory_remove_item` | WEB4: removes a quantity of an item from an avatar's inventory. quantity defaults to 1. |
| `web4_avatar_inventory_search` | WEB4: searches an avatar's inventory by a free-text search term. |
| `web4_avatar_inventory_send_to_avatar` | WEB4: sends an inventory item from one avatar to another. fromAvatarId is the sender GUID. target is the recipient avatar's username or id. itemName identifies the item. quantity defaults to 1. itemId (optional GUID) can pin to a specific item record. |
| `web4_avatar_inventory_send_to_clan` | WEB4: sends an inventory item from an avatar to a clan. fromAvatarId is the sender GUID. target is the clan name. itemName identifies the item. quantity defaults to 1. itemId (optional GUID) pins to a specific record. |
| `web4_avatar_inventory_update_item` | WEB4: updates an existing inventory item for an avatar. inventoryItemJson is a JSON object matching InventoryItem. |
| `web4_avatar_load_all` | WEB4: loads every avatar registered in the OASIS network (admin/server-side use). |
| `web4_avatar_load_all_details` | WEB4: loads the full detailed profile for every avatar in the OASIS network (admin/server-side use). |
| `web4_avatar_load_all_names` | WEB4: loads a flat list of avatar display names. includeUsernames and includeIds both default to true. |
| `web4_avatar_load_all_names_grouped` | WEB4: loads avatar display names grouped by name. Returns a dictionary mapping each name to a list of variants (e.g. with id, username). includeUsernames and includeIds both default to true. |
| `web4_avatar_load_by_email` | WEB4: loads an avatar by its email address. |
| `web4_avatar_load_by_id` | WEB4: loads an avatar by its GUID id. |
| `web4_avatar_load_by_username` | WEB4: loads an avatar by its username. |
| `web4_avatar_load_detail_by_email` | WEB4: loads the detailed avatar profile by email address. |
| `web4_avatar_load_detail_by_id` | WEB4: loads the detailed avatar profile (AvatarDetail) by GUID id. |
| `web4_avatar_load_detail_by_username` | WEB4: loads the detailed avatar profile by username. |
| `web4_avatar_register` | WEB4: registers a new OASIS avatar. |
| `web4_avatar_remove_karma` | WEB4: removes karma from an avatar. karmaType is KarmaTypeNegative (e.g. 'Hacking'), sourceType is KarmaSourceType. |
| `web4_avatar_remove_karma_from_avatar` | WEB4: removes karma from an avatar. karmaType is a KarmaTypeNegative value (e.g. 'Hacking'). karmaSourceType is a KarmaSourceType value. |
| `web4_avatar_reset_password` | WEB4: resets an avatar's password using a valid token issued by web4_avatar_forgot_password. |
| `web4_avatar_save` | WEB4: saves (creates or updates) an avatar. avatarJson should be a JSON object with fields: Id (omit for create), Username, Email, FirstName, LastName, Password, AvatarType, etc. |
| `web4_avatar_search` | WEB4: searches avatars using OASIS search parameters. searchParamsJson is a JSON object matching SearchParams (fields: SearchQuery, SearchAvatars, SearchHolons, SearchAll, etc.). |
| `web4_avatar_session_create` | WEB4: creates a new session for the given avatar. requestJson is a JSON object matching CreateSessionRequest (DeviceId, DeviceName, IpAddress, etc.). |
| `web4_avatar_session_get` | WEB4: retrieves all active sessions for the given avatar ID. |
| `web4_avatar_session_logout` | WEB4: logs the avatar out of the specified session IDs. sessionIdsJson is a JSON array of session ID strings. |
| `web4_avatar_session_logout_all` | WEB4: logs the avatar out of all active sessions. |
| `web4_avatar_session_stats` | WEB4: retrieves session statistics (counts, last-login etc.) for the given avatar ID. |
| `web4_avatar_session_update` | WEB4: updates an existing avatar session. requestJson is a JSON object matching UpdateSessionRequest. |
| `web4_avatar_session_validate_token` | WEB4: validates an OASIS JWT account token and returns the validation result. |
| `web4_avatar_set_active_quest` | WEB4: sets the active quest and objective for an avatar. avatarId is the GUID. activeQuestId and activeObjectiveId are nullable GUIDs (pass null or empty string to clear). |
| `web4_avatar_update_by_email` | WEB4: updates an avatar's profile fields by email address. updateJson is the same shape as web4_avatar_update_by_id. |
| `web4_avatar_update_by_id` | WEB4: updates an avatar's profile fields by GUID id. updateJson should be a JSON object with any subset of: Title, FirstName, LastName, Username, Email, Password, Description, DID, DIDPublicKey, AcceptTerms, IsActive, AvatarType, MetaData. |
| `web4_avatar_update_by_username` | WEB4: updates an avatar's profile fields by username. updateJson is the same shape as web4_avatar_update_by_id. |
| `web4_avatar_update_detail_by_email` | WEB4: updates the full AvatarDetail record for an avatar by email address. avatarDetailJson is a JSON object matching AvatarDetail. |
| `web4_avatar_update_detail_by_id` | WEB4: updates the full AvatarDetail record for an avatar by GUID id. avatarDetailJson is a JSON object matching AvatarDetail. |
| `web4_avatar_update_detail_by_username` | WEB4: updates the full AvatarDetail record for an avatar by username. avatarDetailJson is a JSON object matching AvatarDetail. |
| `web4_avatar_upload_portrait` | WEB4: uploads or replaces an avatar portrait. Provide at least one of avatarId, username, or email. imageBase64 is the base-64-encoded image bytes. |
| `web4_avatar_validate_reset_token` | WEB4: validates a password-reset token (checks it has not expired or been used). |
| `web4_avatar_verify_email` | WEB4: verifies an avatar's email address using the token sent after registration. |
| `web4_biometric_status` | WEB4: gets the authenticated avatar's biometric enrollment status (biometricEnrolled, voiceEnrolled, requireForLogin, requireForSensitiveOps, voiceServiceAvailable). |
| `web4_biometric_voice_delete` | WEB4: deletes the authenticated avatar's enrolled voice profile from Azure and clears the VoiceprintId. |
| `web4_biometric_voice_enroll` | WEB4: enrolls the authenticated avatar's voice (Azure Speaker Recognition). audioBase64 is a Base64-encoded WAV/OGG/MP3 recording of at least 20 seconds. |
| `web4_biometric_voice_verify` | WEB4: verifies the authenticated avatar's voice against their enrolled profile. audioBase64 is a Base64-encoded recording of at least 5 seconds. Returns accepted, score, message. |
| `web4_biometric_voice_verify_by_avatar_id` | WEB4 admin: verifies a specific avatar's voice. Caller must be that avatar or hold HerzID clearance >= 8. audioBase64 is a Base64-encoded recording of at least 5 seconds. |
| `web4_bridge_check_order_balance` | WEB4 Bridge: checks the balance and status of an existing bridge order by its GUID order id. |
| `web4_bridge_create_order` | WEB4 Bridge: creates a new cross-chain bridge order (token swap). orderRequestJson fields: FromToken, ToToken, Amount, FromAddress, ToAddress. |
| `web4_bridge_create_private_order` | WEB4 Bridge: creates a private cross-chain bridge order with viewing-key audit and zero-knowledge proof verification enabled. orderRequestJson fields: FromToken, ToToken, Amount, FromAddress, ToAddress. |
| `web4_bridge_get_exchange_rate` | WEB4 Bridge: gets the current exchange rate between two tokens. fromToken and toToken are token symbols (e.g. 'SOL', 'XRD', 'ETH'). |
| `web4_bridge_get_supported_networks` | WEB4 Bridge: returns the list of blockchain networks supported by the Universal Asset Bridge. |
| `web4_bridge_record_viewing_key` | WEB4 Bridge: records a viewing key for auditability/compliance. auditEntryJson fields: OrderId, ViewingKey, Timestamp. |
| `web4_bridge_verify_proof` | WEB4 Bridge: verifies a zero-knowledge proof payload. proofPayload is the raw proof bytes/string. proofType is the proof type string (e.g. 'Groth16', 'PLONK'). |
| `web4_chat_end_session` | WEB4: ends and closes a chat session. |
| `web4_chat_get_active_sessions` | WEB4: lists all active chat sessions for an avatar. |
| `web4_chat_get_history` | WEB4: retrieves paginated message history for a chat session. |
| `web4_chat_send_message` | WEB4: sends a message into an existing chat session. sessionId is the string returned by web4_chat_start_session. |
| `web4_chat_start_session` | WEB4: creates a new chat session. participantIdsJson is a JSON array of GUID strings. Returns the new session id. |
| `web4_clan_create` | WEB4: creates a new clan. The ownerAvatarId becomes the owner and first member. Provide name and optional description. |
| `web4_clan_delete` | WEB4: deletes (soft-deletes by default) a clan by its GUID id. |
| `web4_clan_load_all` | WEB4: loads all clans, optionally filtered by ownerAvatarId (pass empty string or omit for all). |
| `web4_clan_load_by_id` | WEB4: loads a clan by its GUID id. |
| `web4_clan_load_inventory` | WEB4: loads the inventory (treasury) of items for a given clan. |
| `web4_clan_load_members` | WEB4: loads the member avatar IDs for a given clan. |
| `web4_clan_update` | WEB4: updates an existing clan's name and/or description. Provide clanId, new name, and optional description. |
| `web4_competition_leaderboard` | WEB4: gets the leaderboard for a competition type and season. competitionType: Karma, Experience, EggCollection, QuestCompletion, etc. seasonType: Daily, Weekly, Monthly, Yearly, etc. |
| `web4_competition_leagues` | WEB4: gets available leagues for a competition type and season (Bronze, Silver, Gold, etc.). |
| `web4_competition_rank` | WEB4: gets a specific avatar's rank in a competition. Returns their leaderboard entry with rank, score and stats. |
| `web4_competition_stats` | WEB4: gets competition statistics for an avatar — their rank entry plus their league for a given competition and season. |
| `web4_competition_tournaments` | WEB4: gets active tournaments for a competition type. competitionType defaults to Karma. |
| `web4_data_get_provider_key` | WEB4: loads a holon by its provider-specific storage key (e.g. a Holochain DNA hash). Returns the holon or an error. |
| `web4_data_load` | WEB4: loads a custom value by key from the current avatar's data store. avatarId must match the authenticated avatar. |
| `web4_data_load_all_holons` | WEB4 COSMIC ORM: loads every holon of a given type from the OASIS network. holonType defaults to 'All'. loadChildren and recursive control child loading. |
| `web4_data_load_by_metadata` | WEB4: loads holons whose metadata contains the given key/value pair. holonType defaults to 'All'. Returns a list of matching holons. |
| `web4_data_load_file` | WEB4: loads a file previously saved via web4_data_save_file. Returns file bytes as a Base64-encoded string in the result. Provide the holon GUID returned at save time. |
| `web4_data_load_holons_for_parent` | WEB4 COSMIC ORM: loads all child holons of a given parent by its GUID id. holonType filters to a specific subtype. |
| `web4_data_save` | WEB4: saves a custom key/value string to the current avatar's data store. avatarId must match the authenticated avatar. |
| `web4_data_save_file` | WEB4: saves binary file data to OASIS storage and returns the holon GUID that identifies it. Provide the file bytes as a Base64-encoded string. |
| `web4_data_save_holons_bulk` | WEB4: saves a collection of holons in a single bulk call. holonsJson should be a JSON array of Holon objects. |
| `web4_data_set_provider_key` | WEB4: saves a holon identified by an existing GUID and associates it with the given provider key. holonJson should be a JSON Holon object including its Id. |
| `web4_egg_discover` | WEB4: discovers a new egg for an avatar. avatarId is the GUID. eggType is an EggType enum value. name is the egg name. locationId is the GUID of the location. locationName is the display name of the location. discoveryMethod is an EggDiscoveryMethod value (default: 'Exploration'). |
| `web4_egg_get_all` | WEB4: gets all eggs hidden in the OASIS. avatarId is the GUID of the requesting avatar. |
| `web4_egg_get_current_quest_leaderboard` | WEB4: gets the leaderboard for the current egg quest(s) for an avatar by GUID id. |
| `web4_egg_get_current_quests` | WEB4: gets the currently active egg quests for an avatar by GUID id. |
| `web4_egg_get_my_eggs` | WEB4: gets all eggs owned by an avatar by GUID id. |
| `web4_egg_hatch` | WEB4: hatches an egg for an avatar. avatarId is the GUID of the avatar. eggId is the GUID of the egg to hatch. |
| `web4_eosio_get_account` | WEB4 EOSIO: gets the EOSIO account details for a given account name. |
| `web4_eosio_get_account_for_avatar` | WEB4 EOSIO: gets the EOSIO account details for a given OASIS Avatar by GUID id. |
| `web4_eosio_get_account_names` | WEB4 EOSIO: gets the EOSIO account name(s) for a given OASIS Avatar by GUID id. |
| `web4_eosio_get_avatar_for_account_name` | WEB4 EOSIO: loads the full OASIS Avatar for a given EOSIO account name. |
| `web4_eosio_get_avatar_id_for_account_name` | WEB4 EOSIO: looks up the OASIS Avatar GUID for a given EOSIO account name. |
| `web4_eosio_get_balance_for_account` | WEB4 EOSIO: gets the EOSIO token balance for a given account name. code is the token contract (e.g. 'eosio.token'), symbol is the token symbol (e.g. 'EOS'). |
| `web4_eosio_get_balance_for_avatar` | WEB4 EOSIO: gets the EOSIO token balance for a given OASIS Avatar by GUID id. code is the token contract, symbol is the token symbol. |
| `web4_eosio_get_private_key` | WEB4 EOSIO: gets the EOSIO account private key for a given OASIS Avatar by GUID id. |
| `web4_file_delete` | WEB4: deletes a file by its GUID id for a given avatar. |
| `web4_file_download` | WEB4: downloads a file by its GUID id for a given avatar. |
| `web4_file_get_all` | WEB4: gets all files stored for an avatar by GUID id. |
| `web4_file_get_metadata` | WEB4: gets file metadata (without downloading the content) for a file by GUID id. |
| `web4_file_update_metadata` | WEB4: updates metadata key-value pairs for a file. metadataJson is a JSON object with the new/updated metadata fields. |
| `web4_file_upload` | WEB4: uploads a file for an avatar. avatarId is the GUID. fileName is the file name. fileDataBase64 is the file content as a Base64-encoded string. contentType is the MIME type (e.g. 'image/png'). metadataJson is an optional JSON object. |
| `web4_geo_nft_delete` | WEB4: deletes a Web4 Geo-Spatial NFT (soft-delete by default). Set burnChildWeb3NFTs=true to also burn its on-chain tokens. |
| `web4_geo_nft_load` | WEB4: loads a Web4 Geo-Spatial NFT by id. |
| `web4_geo_nft_load_all` | WEB4 NFT: loads all Geo-Spatial NFTs across the OASIS network (admin/server-side use). |
| `web4_geo_nft_load_all_for_avatar` | WEB4: loads every Geo-Spatial NFT owned by/minted for an avatar. |
| `web4_geo_nft_load_all_for_mint_address` | WEB4 NFT: loads all Geo-Spatial NFTs associated with a given on-chain mint wallet address. |
| `web4_geo_nft_load_near_location` | WEB4: finds Geo-Spatial NFTs within a radius of a coordinate. latLocation and longLocation are integer degrees × 1e6 (micro-degrees). radiusMetres is the search radius. |
| `web4_geo_nft_mint_and_place` | WEB4: mints a Geo-Spatial NFT and pins it to a geographic coordinate in one step. mintAndPlaceRequestJson fields: Title, Description, Lat, Long, OnChainProvider, OffChainProvider, NFTStandardType. |
| `web4_gift_get_history` | WEB4: gets gift transaction history for an avatar. limit defaults to 50, offset defaults to 0. |
| `web4_gift_get_my_gifts` | WEB4: gets all gifts for an avatar by GUID id. |
| `web4_gift_get_stats` | WEB4: gets gift statistics (sent, received, opened counts, etc.) for an avatar by GUID id. |
| `web4_gift_open` | WEB4: opens (activates) a received gift for an avatar. avatarId is the GUID of the recipient. giftId is the GUID of the gift. |
| `web4_gift_receive` | WEB4: marks a gift as received for an avatar. avatarId is the recipient GUID. giftId is the GUID of the gift. |
| `web4_gift_send` | WEB4: sends a gift from one avatar to another. fromAvatarId and toAvatarId are GUIDs. giftType is a GiftType enum value. message and metadataJson are optional. |
| `web4_herzid_ghost_check` | WEB4 HerzID: admin ghost-account detection check for a HerzID. The caller must hold clearance level 8+. |
| `web4_herzid_profile` | WEB4 HerzID: returns the HerzID profile (rank, clearance, QEA tier, vouches remaining) for the authenticated avatar. |
| `web4_herzid_register` | WEB4 HerzID: assigns a HerzID to the authenticated avatar. The avatar must already have been vouched for by voucherHerzId. qeaProfile is the QEA profile string; voiceprintId is optional. |
| `web4_herzid_set_clearance` | WEB4 HerzID: updates the clearance level (1-9) of an avatar. The caller must hold clearance level 8 (Flame Keeper) or 9. |
| `web4_herzid_verify` | WEB4 HerzID: verifies a HerzID by recomputing its QEA seal. Public — no authentication required. herzId is the HerzID string (e.g. '052·0·000·000·001·✦'). |
| `web4_herzid_vouch` | WEB4 HerzID: gifts one of the authenticated avatar's vouches to a new member by their OASIS Avatar ID. |
| `web4_herzid_vouch_chain` | WEB4 HerzID: returns the vouching chain (ancestry) for a given HerzID — walks upward from the member to the founding member. Public endpoint. herzId is the HerzID string. |
| `web4_holochain_get_agent_ids` | WEB4 Holochain: gets the Holochain agent ID(s) (public keys) for a given avatar by GUID id. |
| `web4_holochain_get_avatar_for_agent_id` | WEB4 Holochain: loads the full OASIS Avatar for a given Holochain agent ID. |
| `web4_holochain_get_avatar_id_for_agent_id` | WEB4 Holochain: looks up the OASIS Avatar GUID for a given Holochain agent ID. |
| `web4_holochain_get_private_keys` | WEB4 Holochain: gets the Holochain agent private key(s) for a given avatar by GUID id. |
| `web4_holochain_link_agent_id` | WEB4 Holochain: links a Holochain agent ID (public key) to an OASIS Avatar by GUID id. walletId is the GUID of the wallet to link to. |
| `web4_holon_delete` | WEB4 COSMIC ORM: deletes (soft-deletes by default) a holon. |
| `web4_holon_load` | WEB4 COSMIC ORM: loads a holon by id. |
| `web4_holon_save` | WEB4 COSMIC ORM: saves (creates or updates) a holon. holonJson should be a JSON object matching the Holon shape (Id, Name, Description, HolonType, MetaData, etc). |
| `web4_holon_search` | WEB4 COSMIC ORM: searches holons by a free-text search term. holonType defaults to 'All'. |
| `web4_karma_activity_feed` | WEB4: gets a paged karma activity feed for an avatar, newest first. Defaults: limit=50, offset=0. |
| `web4_karma_add` | WEB4: adds karma to an avatar. sourceType is a KarmaSourceType enum value (e.g. 'Action', 'Event', 'API'). |
| `web4_karma_akashic_records` | WEB4: gets the karma akashic records for an avatar — the full history of karma earned and lost. |
| `web4_karma_deduct` | WEB4: deducts karma from an avatar. |
| `web4_karma_get` | WEB4: gets an avatar's current karma total. |
| `web4_karma_get_history` | WEB4: gets an avatar's karma transaction history. |
| `web4_karma_get_stats` | WEB4: gets aggregate karma statistics for an avatar. |
| `web4_karma_get_weightings` | WEB4: gets the configured weighting for one positive or negative karma type through the authenticated WEB4 API. |
| `web4_key_base58_check_decode` | WEB4 Keys: decodes a Base58Check-encoded string to raw bytes. |
| `web4_key_clear_cache` | WEB4 Keys: clears the KeyManager's internal cache of resolved keys. |
| `web4_key_create` | WEB4 Keys: creates a new key record (holon) for an avatar. avatarId is the owning avatar's GUID. keyName is the label. keyType is an arbitrary type string. |
| `web4_key_decode_private_wif` | WEB4 Keys: decodes a private key from WIF (Wallet Import Format) encoding. data is the WIF-encoded string. |
| `web4_key_delete` | WEB4 Keys: soft-deletes a key record holon by its GUID id. |
| `web4_key_encode_signature` | WEB4 Keys: encodes raw signature bytes into the OASIS signature string format. sourceBase64 is the Base64-encoded source bytes. |
| `web4_key_generate_and_link_by_email` | WEB4: generates a key pair with a wallet address and links it to an avatar by email. |
| `web4_key_generate_and_link_by_id` | WEB4: generates a key pair with a wallet address and links the keys to an avatar by GUID id. |
| `web4_key_generate_and_link_by_username` | WEB4: generates a key pair with a wallet address and links it to an avatar by username. |
| `web4_key_generate_keypair_with_wallet` | WEB4: generates a standalone key pair plus a wallet address for a given provider type (does not link to any avatar). |
| `web4_key_get_all_private_keys_by_id` | WEB4 Keys: gets private keys across all providers for an avatar by GUID id. |
| `web4_key_get_all_private_keys_by_username` | WEB4 Keys: gets private keys across all providers for an avatar by username. |
| `web4_key_get_all_public_keys_by_email` | WEB4 Keys: gets public keys across all providers for an avatar by email address. |
| `web4_key_get_all_public_keys_by_id` | WEB4: gets public keys across all providers for an avatar by GUID id. |
| `web4_key_get_all_public_keys_by_username` | WEB4 Keys: gets public keys across all providers for an avatar by username. |
| `web4_key_get_all_unique_storage_keys_by_email` | WEB4: gets unique storage keys across all providers for an avatar by email. |
| `web4_key_get_all_unique_storage_keys_by_id` | WEB4: gets unique storage keys across all providers for an avatar by GUID id. |
| `web4_key_get_all_unique_storage_keys_by_username` | WEB4: gets unique storage keys across all providers for an avatar by username. |
| `web4_key_get_avatar_email_for_public_key` | WEB4: looks up the avatar email from a provider public key. |
| `web4_key_get_avatar_email_for_storage_key` | WEB4 Keys: looks up the avatar email from a provider unique storage key. |
| `web4_key_get_avatar_for_public_key` | WEB4: loads the full avatar object for a given provider public key. |
| `web4_key_get_avatar_for_storage_key` | WEB4: loads the full avatar object for a given provider unique storage key. |
| `web4_key_get_avatar_id_for_public_key` | WEB4: looks up the avatar GUID id from a provider public key. |
| `web4_key_get_avatar_id_for_storage_key` | WEB4: looks up the avatar GUID id from a provider unique storage key. |
| `web4_key_get_avatar_username_for_public_key` | WEB4: looks up the avatar username from a provider public key. |
| `web4_key_get_avatar_username_for_storage_key` | WEB4 Keys: looks up the avatar username from a provider unique storage key. |
| `web4_key_get_private_keys_by_email` | WEB4 Keys: gets an avatar's private keys for a provider (e.g. SolanaOASIS, EthereumOASIS) by the avatar's email address. |
| `web4_key_get_private_keys_by_id` | WEB4: gets provider private keys for an avatar by GUID id. |
| `web4_key_get_private_keys_by_username` | WEB4: gets provider private keys for an avatar by username. |
| `web4_key_get_private_wif` | WEB4 Keys: derives the private WIF string from raw key bytes. sourceBase64 is the Base64-encoded source bytes. |
| `web4_key_get_public_keys_by_email` | WEB4: gets provider public keys for an avatar by email. |
| `web4_key_get_public_keys_by_id` | WEB4: gets provider public keys for an avatar by GUID id. |
| `web4_key_get_public_keys_by_username` | WEB4: gets provider public keys for an avatar by username. |
| `web4_key_get_public_wif` | WEB4 Keys: derives the public WIF string from a public key and prefix. publicKeyBase64 is the Base64-encoded public key bytes; prefix is the WIF version prefix byte. |
| `web4_key_get_unique_storage_key_by_email` | WEB4: gets the provider-specific unique storage key for an avatar by email. |
| `web4_key_get_unique_storage_key_by_id` | WEB4: gets the provider-specific unique storage key for an avatar by GUID id. |
| `web4_key_get_unique_storage_key_by_username` | WEB4: gets the provider-specific unique storage key for an avatar by username. |
| `web4_key_link_private_key_by_id` | WEB4: links a provider private key to an avatar by GUID id. Set showPrivateKey=true to return the key in the response. |
| `web4_key_link_private_key_by_username` | WEB4: links a provider private key to an avatar by username. |
| `web4_key_link_public_key_by_email` | WEB4: links a provider public key to an avatar by email address. |
| `web4_key_link_public_key_by_id` | WEB4: links a provider public key to an avatar by GUID id. providerType is a ProviderType enum (e.g. 'Ethereum', 'Solana'). |
| `web4_key_link_public_key_by_username` | WEB4: links a provider public key to an avatar by username. |
| `web4_key_link_wallet_address_by_email` | WEB4 Keys: links a provider wallet address to an avatar by email address. |
| `web4_key_link_wallet_address_by_id` | WEB4 Keys: links a provider wallet address to an avatar by GUID id. providerType is a ProviderType enum (e.g. 'Ethereum', 'Solana'). |
| `web4_key_link_wallet_address_by_username` | WEB4 Keys: links a provider wallet address to an avatar by username. |
| `web4_key_list_all` | WEB4 Keys: lists all key record holons for an avatar by GUID id. Returns id, name, type, createdAt, isActive for each key. |
| `web4_key_update` | WEB4 Keys: updates an existing key record holon. keyId is the GUID of the key holon. keyName and keyType are the new values. |
| `web4_level_calculate` | WEB4: calculates the OASIS level for a given karma value. |
| `web4_level_lookup` | WEB4: returns the full karma-to-level lookup table: { level → minimumKarmaRequired }. |
| `web4_map_draw_2d_sprite_hud` | WEB4/Map: draws a 2D sprite on the Our World HUD at position (x, y). |
| `web4_map_draw_2d_sprite_map` | WEB4/Map: draws a 2D sprite on the Our World map at position (x, y). |
| `web4_map_draw_3d_object` | WEB4/Map: draws a 3D object on the Our World map at position (x, y). |
| `web4_map_draw_route_between_holons` | WEB4/Map: creates and draws a route on the Our World map between two holons, identified by GUID. |
| `web4_map_draw_route_between_points` | WEB4/Map: creates and draws a route between map points. pointsJson is a MapPoints JSON object. |
| `web4_map_get_nearby` | WEB4/Map: gets locations near a latitude/longitude within radiusKm (default 10) for an avatar. |
| `web4_map_get_stats` | WEB4/Map: retrieves map statistics for an avatar (total visits, unique locations, etc.). |
| `web4_map_get_visit_history` | WEB4/Map: retrieves an avatar's map visit history. limit defaults to 50, offset to 0. |
| `web4_map_pan_down` | WEB4/Map: pans the Our World map downward by value units. |
| `web4_map_pan_left` | WEB4/Map: pans the Our World map leftward by value units. |
| `web4_map_pan_right` | WEB4/Map: pans the Our World map rightward by value units. |
| `web4_map_pan_up` | WEB4/Map: pans the Our World map upward by value units. |
| `web4_map_search_locations` | WEB4/Map: searches map locations by text query with optional filters. type is a LocationType name (e.g. Building, Quest). Returns matching MapLocation objects. |
| `web4_map_visit_location` | WEB4/Map: records a location visit for an avatar and updates its map competition scores. purpose is optional. |
| `web4_map_zoom_in` | WEB4/Map: zooms the Our World map in by value units. |
| `web4_map_zoom_out` | WEB4/Map: zooms the Our World map out by value units. |
| `web4_map_zoom_to_holon` | WEB4/Map: zooms and centres the map on a holon, identified by GUID. |
| `web4_map_zoom_to_quest` | WEB4/Map: zooms and centres the map on a quest, identified by GUID. |
| `web4_message_get` | WEB4: retrieves messages for an avatar, newest first (paginated). |
| `web4_message_get_conversation` | WEB4: retrieves the conversation thread between two avatars, newest first (paginated). |
| `web4_message_mark_read` | WEB4: marks a set of messages as read. messageIdsJson is a JSON array of GUID strings. |
| `web4_message_send` | WEB4: sends a direct message from one avatar to another. messageType is a MessagingType value (e.g. 'Direct', 'Broadcast'). |
| `web4_nft_collect` | WEB4 NFT: marks an NFT as collected by an avatar. collectRequestJson fields: NFTId, CollectedByAvatarId, and optional collection metadata. |
| `web4_nft_collect_geo` | WEB4 NFT: collects a Geo-Spatial NFT placement for an avatar. collectRequestJson fields: NFTId, CollectedByAvatarId, Lat, Long. |
| `web4_nft_collection_create` | WEB4 NFT: creates a new Web4 NFT collection. collectionJson fields: Name, Description, OnChainProvider, OffChainProvider, and optional metadata. |
| `web4_nft_collection_delete` | WEB4 NFT: deletes a Web4 NFT collection (soft-delete by default). Set softDelete=false for a hard delete. Set deleteChildNFTs=true to also delete contained NFTs. |
| `web4_nft_collection_load` | WEB4: loads a Web4 NFT collection by id, optionally pre-loading child NFT records. |
| `web4_nft_collection_load_all_for_avatar` | WEB4: loads every Web4 NFT collection owned by an avatar. |
| `web4_nft_collection_update` | WEB4 NFT: updates an existing Web4 NFT collection. updateJson fields: Id (GUID), Name, Description, and mutable metadata. |
| `web4_nft_delete` | WEB4: deletes a Web4 NFT (soft-delete by default). Set burnChildWeb3NFTs=true to also burn its on-chain tokens. |
| `web4_nft_geo_collection_status` | WEB4 NFT: returns per-avatar visibility/respawn state for up to 500 Geo-NFT placements. idsJson is a JSON array of Geo-NFT GUID strings. |
| `web4_nft_geo_update` | WEB4 NFT: updates a Geo-Spatial NFT placement's authored metadata/rules (restricted to its creator). updateRequestJson fields: Id, Title, Description, Lat, Long, and other mutable fields. |
| `web4_nft_load` | WEB4: loads a Web4 NFT by its GUID id. |
| `web4_nft_load_all` | WEB4 NFT: loads all Web4 NFTs across the OASIS network (admin/server-side use). |
| `web4_nft_load_all_for_avatar` | WEB4: loads every Web4 NFT minted by/for an avatar. |
| `web4_nft_load_all_for_mint_address` | WEB4 NFT: loads all Web4 NFTs associated with a given on-chain mint wallet address. |
| `web4_nft_load_by_hash` | WEB4 NFT: loads a Web4 NFT by its on-chain hash (mint address / transaction hash). |
| `web4_nft_mint` | WEB4: mints a new Web4 NFT. mintRequestJson fields: Title, Description, Price, NumberToMint, OnChainProvider (e.g. 'Solana'), OffChainProvider (e.g. 'IPFSOASIS', 'PinataOASIS', 'ArweaveOASIS'), NFTStandardType (e.g. 'Metaplex'), StoreNFTMetaDataOnChain. |
| `web4_nft_send` | WEB4: sends (transfers) a Web4 NFT to another wallet. sendRequestJson fields: NFTId (GUID), ToWalletAddress or ToAvatarId, optionally Message. |
| `web4_notification_get` | WEB4: retrieves notifications for an avatar, newest first (paginated). |
| `web4_notification_mark_read` | WEB4: marks a set of notifications as read. notificationIdsJson is a JSON array of GUID strings. |
| `web4_oidc_authorize` | WEB4 OIDC: runs the authorization-code step for an already-authenticated avatar and returns the redirect location carrying the short-lived code (and state). responseType must be 'code'. |
| `web4_oidc_discovery` | WEB4 OIDC: returns the OpenID Connect discovery document (/.well-known/openid-configuration). Requires OIDC to be enabled in OASISDNA. |
| `web4_oidc_jwks` | WEB4 OIDC: returns the JSON Web Key Set (JWKS) used to verify OASIS-issued tokens. |
| `web4_oidc_token` | WEB4 OIDC: exchanges an authorization code or refresh token for a JWT access token. grantType is 'authorization_code' (needs code, clientId, redirectUri) or 'refresh_token' (needs refreshToken). |
| `web4_oidc_userinfo` | WEB4 OIDC: returns the UserInfo claims (sub, name, email, id, did, herzid, herzid_clearance) for the avatar the bearer token belongs to. |
| `web4_oland_delete` | WEB4/OLand: deletes an OLand parcel by olandId on behalf of avatarId. |
| `web4_oland_get_price` | WEB4/OLand: gets the price for purchasing a given count of OLand parcels. couponCode is optional. |
| `web4_oland_load` | WEB4/OLand: loads a single OLand parcel by its GUID olandId. |
| `web4_oland_load_all` | WEB4/OLand: loads all OLand parcels in the OASIS. |
| `web4_oland_purchase` | WEB4/OLand: purchases OLand parcel(s). requestJson is a PurchaseOlandRequest JSON (AvatarId, Count, Tiles, etc.). |
| `web4_oland_save` | WEB4/OLand: creates or saves an OLand parcel. olandJson is an Oland JSON object. |
| `web4_oland_update` | WEB4/OLand: updates an existing OLand parcel. olandJson is an Oland JSON object including Id. |
| `web4_onet_broadcast_message` | WEB4/ONET: broadcasts a message to all connected P2P nodes. |
| `web4_onet_connect_node` | WEB4/ONET: connects to a specific P2P node by nodeId and nodeAddress (host:port). |
| `web4_onet_disconnect_node` | WEB4/ONET: disconnects from a specific P2P node. |
| `web4_onet_get_network_nodes` | WEB4/ONET: lists nodes currently connected to the P2P network. |
| `web4_onet_get_network_stats` | WEB4/ONET: retrieves P2P network statistics (bandwidth, message counts, etc.). |
| `web4_onet_get_network_status` | WEB4/ONET: gets the current P2P network status (online/offline, peer count). |
| `web4_onet_get_network_topology` | WEB4/ONET: retrieves the P2P network topology graph. |
| `web4_onet_get_config` | WEB4/ONET: retrieves this node's ONET configuration (Wizard only). NodePrivateKey and ONETApiKey are always returned empty. |
| `web4_onet_register_node` | WEB4/ONET: registers a community ONODE's public key with the bootstrap server. nodeAddress is optional. |
| `web4_onet_start_network` | WEB4/ONET: starts the P2P network. |
| `web4_onet_stop_network` | WEB4/ONET: stops the P2P network. |
| `web4_onet_update_config` | WEB4/ONET: updates operator-editable ONET settings (Wizard only). onetConfigJson is an ONETConfig JSON object; node identity keys cannot be changed and ONETApiKey is only replaced when non-empty. |
| `web4_onode_disable_provider` | WEB4/ONODE: disables a storage provider by providerType string. |
| `web4_onode_enable_provider` | WEB4/ONODE: enables a storage provider by providerType string via the ONODEService supervisor. |
| `web4_onode_get_active_nodes` | WEB4/ONODE: lists all ONODE instances that have reported state within the last 5 minutes. |
| `web4_onode_get_audit_log` | WEB4/ONODE: retrieves the ONODE audit log. Optional nodeId filter and limit (default 200). |
| `web4_onode_get_config` | WEB4/ONODE: retrieves the current ONODE configuration dictionary. |
| `web4_onode_get_info` | WEB4/ONODE: gets detailed ONODE information. |
| `web4_onode_get_logs` | WEB4/ONODE: retrieves recent ONODE log lines. lines defaults to 100. |
| `web4_onode_get_metrics` | WEB4/ONODE: gets performance metrics for this ONODE (CPU, memory, throughput). |
| `web4_onode_get_peers` | WEB4/ONODE: lists all peers connected to this ONODE. |
| `web4_onode_get_providers` | WEB4/ONODE: lists all configured OASIS storage providers and their enabled state from OASISDNA.json. |
| `web4_onode_get_stats` | WEB4/ONODE: retrieves ONODE statistics (uptime, request counts, etc.). |
| `web4_onode_get_status` | WEB4/ONODE: gets the current ONODE status (online/offline, version). |
| `web4_onode_restart` | WEB4/ONODE: restarts the ONODE. |
| `web4_onode_start` | WEB4/ONODE: starts the ONODE. |
| `web4_onode_stop` | WEB4/ONODE: stops the ONODE. |
| `web4_onode_update_config` | WEB4/ONODE: updates the ONODE configuration. configJson is a JSON object of key/value pairs. |
| `web4_provider_activate` | WEB4: activates a previously registered provider so it can be used for storage/network operations. |
| `web4_provider_deactivate` | WEB4: deactivates an active provider without unregistering it. |
| `web4_provider_get_all_registered` | WEB4: lists all registered OASIS providers. |
| `web4_provider_get_all_registered_types` | WEB4: lists all registered provider type enum values. |
| `web4_provider_get_auto_failover` | WEB4: lists all providers in the auto-failover list. |
| `web4_provider_get_auto_load_balance` | WEB4: lists all providers in the auto-load-balance list. |
| `web4_provider_get_auto_replicating` | WEB4: lists all providers currently configured for auto-replication. |
| `web4_provider_get_current` | WEB4: returns the current active OASIS storage provider. |
| `web4_provider_get_current_type` | WEB4: returns the current active OASIS storage provider type enum value. |
| `web4_provider_get_for_category` | WEB4: lists registered providers of a given ProviderCategory (e.g. Storage, Network, Renderer). |
| `web4_provider_get_network_providers` | WEB4: lists all registered network providers. |
| `web4_provider_get_storage_providers` | WEB4: lists all registered storage providers. |
| `web4_provider_is_registered` | WEB4: returns true if the given providerType (e.g. MongoDBOASIS) is already registered. |
| `web4_provider_register_type` | WEB4: registers a provider by type name (e.g. MongoDBOASIS). Uses the OASIS boot-loader to instantiate and register the provider. |
| `web4_provider_set_active` | WEB4: sets and activates a provider as the current storage provider. setGlobally=true makes this permanent; false applies only to the next request. |
| `web4_provider_set_auto_failover_all` | WEB4: enables or disables auto-failover for ALL registered providers. |
| `web4_provider_set_auto_failover_list` | WEB4: enables or disables auto-failover for a comma-separated list of provider type names. |
| `web4_provider_set_auto_load_balance_all` | WEB4: enables or disables auto-load-balancing across ALL registered providers. |
| `web4_provider_set_auto_load_balance_list` | WEB4: enables or disables auto-load-balancing for a comma-separated list of provider type names. |
| `web4_provider_set_auto_replicate_all` | WEB4: enables or disables auto-replication across ALL registered providers. |
| `web4_provider_set_auto_replicate_list` | WEB4: enables or disables auto-replication for a comma-separated list of provider type names (e.g. MongoDBOASIS,IPFSOASIS). |
| `web4_provider_unregister_type` | WEB4: unregisters a provider by type name, removing it from the active provider list. |
| `web4_search` | WEB4 COSMIC ORM: full cross-entity OASIS search. searchParamsJson is a JSON object matching SearchParams (fields include SearchQuery, SearchAvatars, SearchHolons, SearchAll, etc.). |
| `web4_seeds_get_transactions` | WEB4: retrieves all SEEDS transactions for the specified avatar GUID. |
| `web4_seeds_save_transaction` | WEB4: saves a SEEDS token transaction for an avatar. avatarId (GUID) and avatarUserName identify the avatar; amount is the SEEDS amount; memo is optional. |
| `web4_settings_get_all` | WEB4: gets all OASIS settings (HyperDrive, notifications, privacy, subscription, system) for an avatar by GUID id. |
| `web4_settings_get_hyperdrive` | WEB4: gets HyperDrive settings for an avatar by GUID id. |
| `web4_settings_get_notifications` | WEB4: gets notification preferences for an avatar by GUID id. |
| `web4_settings_get_privacy` | WEB4: gets privacy settings for an avatar by GUID id. |
| `web4_settings_get_subscription` | WEB4: gets subscription/plan settings for an avatar by GUID id. |
| `web4_settings_get_system` | WEB4: gets system configuration settings for an avatar by GUID id. |
| `web4_settings_get_system_config` | WEB4: returns a system configuration object including OASIS version, environment, and feature flags. |
| `web4_settings_get_version` | WEB4: returns the current OASIS API version string. |
| `web4_settings_update_hyperdrive` | WEB4: updates HyperDrive settings for an avatar. avatarId is the GUID. settingsJson is a JSON object with the HyperDrive key-value pairs to update. |
| `web4_settings_update_notifications` | WEB4: updates notification preferences for an avatar. preferencesJson is a JSON object with notification preference key-value pairs. |
| `web4_settings_update_privacy` | WEB4: updates privacy settings for an avatar. privacySettingsJson is a JSON object with privacy key-value pairs. |
| `web4_settings_update_subscription` | WEB4: updates subscription settings for an avatar. settingsJson is a JSON object with subscription key-value pairs. |
| `web4_settings_update_system` | WEB4: updates system settings for an avatar. settingsJson is a JSON object with the system key-value pairs to update. |
| `web4_share_holon` | WEB4: shares a holon with one or more avatars by recording the avatar IDs in the holon's SHARED_AVATAR_IDS metadata key. avatarIds is a comma-separated list of avatar GUIDs. |
| `web4_social_get_feed` | WEB4: retrieves the aggregated social-media feed from all registered providers for an avatar. |
| `web4_social_get_registered_providers` | WEB4: lists all social-media providers registered for an avatar. |
| `web4_social_register_provider` | WEB4: registers a social-media provider (e.g. 'Twitter', 'Facebook') for an avatar with an access token. settingsJson is an optional JSON object with extra provider settings. |
| `web4_social_share_holon` | WEB4: shares a holon to social media. providerIdsJson is an optional JSON array of provider id strings to share to; omit to share to all registered providers. |
| `web4_stats_achievement_stats` | WEB4 Stats: gets achievement statistics for an avatar by GUID id. |
| `web4_stats_get_avatar` | WEB4: gets comprehensive statistics for an avatar (karma, achievements, NFTs, gifts, etc.). |
| `web4_stats_get_chat` | WEB4: gets chat activity statistics for an avatar. |
| `web4_stats_get_gift` | WEB4: gets gift (item-transfer) statistics for an avatar. |
| `web4_stats_get_karma` | WEB4: gets karma statistics for an avatar. |
| `web4_stats_get_karma_history` | WEB4: gets karma transaction history for an avatar. limit defaults to 50. |
| `web4_stats_get_key` | WEB4: gets cryptographic-key statistics for an avatar. |
| `web4_stats_get_leaderboard` | WEB4: gets leaderboard statistics for an avatar. |
| `web4_stats_get_system` | WEB4: gets system-wide OASIS network statistics (total avatars, holons, NFTs, etc.). |
| `web4_video_end_call` | WEB4 Video: ends and closes a video call session. callId is the session identifier. avatarId is the avatar ending the call. |
| `web4_video_join_call` | WEB4 Video: joins an existing video call session. callId is the session identifier returned by web4_video_start_call. avatarId is the joining avatar's GUID. |
| `web4_video_start_call` | WEB4 Video: starts a new group video call. callerAvatarId is the initiating avatar's GUID. participantIdsJson is a JSON array of participant avatar GUID strings. callName is an optional session label. |
| `web4_wallet_create` | WEB4: creates a new provider wallet for an avatar. walletProviderType is a ProviderType enum value (e.g. Ethereum, Solana, EOS, Holochain). |
| `web4_wallet_get_analytics` | WEB4: gets analytics for a specific wallet. Returns sample data when UseTestDataWhenLiveDataNotAvailable is enabled on the server. |
| `web4_wallet_get_default` | WEB4: gets the default wallet for an avatar. Provide avatarId (GUID) and providerType name, e.g. 'EthereumOASIS'. |
| `web4_wallet_get_portfolio_value` | WEB4: gets the total portfolio value across all wallets for an avatar. Calls the WEB4 REST API (returns sample data when UseTestDataWhenLiveDataNotAvailable is enabled on the server). |
| `web4_wallet_get_tokens` | WEB4: gets tokens held in a specific wallet. Returns sample data when UseTestDataWhenLiveDataNotAvailable is enabled on the server. |
| `web4_wallet_get_total_balance` | WEB4: gets an avatar's total balance summed across every provider wallet. |
| `web4_wallet_import_key` | WEB4: imports a wallet using a private key for an avatar. Provide avatarId, privateKey, and the providerType to import to, e.g. 'EthereumOASIS'. |
| `web4_wallet_load_by_email` | WEB4: loads all provider wallets for an avatar by email address. |
| `web4_wallet_load_by_username` | WEB4: loads all provider wallets for an avatar by username. |
| `web4_wallet_load_provider_wallets` | WEB4: loads every provider wallet for an avatar, grouped by provider type. Set showOnlyDefault=true to return only the default wallet per provider. |
| `web4_wallet_set_default` | WEB4: sets the default wallet for an avatar by avatarId and walletId. Provide providerType name, e.g. 'EthereumOASIS'. |
| `web4_wallet_token_burn` | WEB4: burns (destroys) a token on the specified blockchain provider. requestJson must match BurnWeb4TokenRequest: { AvatarId, TokenId, Amount, ProviderType, etc. }. |
| `web4_wallet_token_import_by_email` | WEB4: imports a wallet for an avatar (by email) using a secret phrase. providerType defaults to 'Default'. |
| `web4_wallet_token_import_by_id` | WEB4: imports a wallet for an avatar (by avatar ID) using a secret phrase. providerType defaults to 'Default'. |
| `web4_wallet_token_import_by_username` | WEB4: imports a wallet for an avatar (by username) using a secret phrase. providerType defaults to 'Default'. |
| `web4_wallet_token_lock` | WEB4: locks a token on the specified blockchain provider so it cannot be transferred. requestJson must match LockWeb4TokenRequest. |
| `web4_wallet_token_unlock` | WEB4: unlocks a previously locked token on the specified blockchain provider. requestJson must match UnlockWeb4TokenRequest. |

## WEB5 commands (95)

| Command | Description |
|---|---|
| `web5_celestial_body_download` | WEB5 STARNET: downloads a celestial body package from STARNET to a local path. |
| `web5_celestial_body_load` | WEB5 STARNET: loads a single celestial body (planet, moon, star, galaxy, dimension, etc.) by its GUID id. |
| `web5_celestial_body_load_all_for_avatar` | WEB5 STARNET: loads every celestial body published by or visible to an avatar. |
| `web5_celestial_body_search` | WEB5 STARNET: searches celestial bodies by a free-text search term. |
| `web5_celestial_space_download` | WEB5 STARNET: downloads a celestial space package from STARNET to a local path. |
| `web5_celestial_space_load` | WEB5 STARNET: loads a single celestial space (universe, multiverse, dimension, etc.) by its GUID id. |
| `web5_celestial_space_load_all_for_avatar` | WEB5 STARNET: loads every celestial space published by or visible to an avatar. |
| `web5_celestial_space_search` | WEB5 STARNET: searches celestial spaces by a free-text search term. |
| `web5_chapter_download` | WEB5 STARNET: downloads a chapter from STARNET to a local path. |
| `web5_chapter_load` | WEB5 STARNET: loads a single quest chapter by its GUID id. |
| `web5_chapter_load_all_for_avatar` | WEB5 STARNET: loads every chapter published by or accessible to an avatar. |
| `web5_chapter_search` | WEB5 STARNET: searches chapters on STARNET by a free-text search term. |
| `web5_game_get_avatar_karma` | WEB5 STARNET: gets the total karma accumulated by an avatar across all games. |
| `web5_game_get_cross_game_quests` | WEB5 STARNET: gets quests that span multiple games for an avatar. |
| `web5_game_get_shared_assets` | WEB5 STARNET: gets inventory items shared across all games for an avatar. |
| `web5_game_load` | WEB5 STARNET: loads a single game by its GUID id. |
| `web5_game_load_all_for_avatar` | WEB5 STARNET: loads every game published by or accessible to an avatar. |
| `web5_game_search` | WEB5 STARNET: searches games on STARNET by a free-text search term. |
| `web5_geo_hotspot_download` | WEB5 STARNET: downloads a geo hotspot from STARNET to a local path. |
| `web5_geo_hotspot_load` | WEB5 STARNET: loads a single geo hotspot by its GUID id. |
| `web5_geo_hotspot_load_all_for_avatar` | WEB5 STARNET: loads every geo hotspot published by or accessible to an avatar. |
| `web5_geo_hotspot_search` | WEB5 STARNET: searches geo hotspots on STARNET by a free-text search term. |
| `web5_holon_download` | WEB5 STARNET: downloads a STARNET holon by id and version to a local path. |
| `web5_holon_download_and_install` | WEB5 STARNET: downloads and installs a STARNET holon in one step. fullInstallPath is the local installation directory. |
| `web5_holon_load` | WEB5 STARNET: loads a single STARNET holon by its GUID id. |
| `web5_holon_load_all_for_avatar` | WEB5 STARNET: loads every STARNET holon published by or accessible to an avatar. |
| `web5_holon_search` | WEB5 STARNET: searches STARNET holons by a free-text search term. |
| `web5_inventory_item_download` | WEB5 STARNET: downloads an inventory item definition from STARNET to a local path. |
| `web5_inventory_item_load` | WEB5 STARNET: loads a single inventory item by its GUID id. |
| `web5_inventory_item_load_all_for_avatar` | WEB5 STARNET: loads every inventory item owned by or accessible to an avatar. |
| `web5_inventory_item_search` | WEB5 STARNET: searches inventory items on STARNET by a free-text search term. |
| `web5_library_download` | WEB5 STARNET: downloads a library from STARNET to a local path. |
| `web5_library_download_and_install` | WEB5 STARNET: downloads and installs a library from STARNET in one step. |
| `web5_library_load` | WEB5 STARNET: loads a single OASIS library by its GUID id. |
| `web5_library_load_all_for_avatar` | WEB5 STARNET: loads every library published by or accessible to an avatar. |
| `web5_library_search` | WEB5 STARNET: searches libraries on STARNET by a free-text search term. |
| `web5_mission_complete` | WEB5: marks a mission complete for an avatar. |
| `web5_mission_get_leaderboard` | WEB5: gets the leaderboard for a mission. |
| `web5_mission_get_rewards` | WEB5: gets the reward list for a mission. |
| `web5_mission_get_stats` | WEB5: gets aggregate mission statistics for an avatar. |
| `web5_mission_load` | WEB5: loads a single mission by its GUID id. |
| `web5_mission_load_all_for_avatar` | WEB5: loads every mission for an avatar. |
| `web5_mission_search` | WEB5: searches missions on STARNET by a free-text search term. |
| `web5_oapp_activate` | WEB5 STARNET: activates an installed OAPP for an avatar (makes it the live running version). |
| `web5_oapp_deactivate` | WEB5 STARNET: deactivates a running OAPP for an avatar. |
| `web5_oapp_download` | WEB5 STARNET: downloads an OAPP from STARNET to a local path. Leave fullDownloadPath empty for the default download directory. |
| `web5_oapp_download_and_install` | WEB5 STARNET: downloads and installs an OAPP from STARNET in one step. fullInstallPath is the local installation directory. |
| `web5_oapp_is_installed` | WEB5 STARNET: checks whether a specific version of an OAPP is installed for an avatar. |
| `web5_oapp_list_installed` | WEB5 STARNET: lists every OAPP currently installed for an avatar. |
| `web5_oapp_load` | WEB5 STARNET: loads a single OAPP by its GUID id. |
| `web5_oapp_load_all_for_avatar` | WEB5 STARNET: loads every OAPP published by or accessible to an avatar. |
| `web5_oapp_search` | WEB5 STARNET: searches OAPPs on STARNET by a free-text search term. |
| `web5_oapp_template_download` | WEB5 STARNET: downloads an OAPP template from STARNET to a local path. |
| `web5_oapp_template_load` | WEB5 STARNET: loads a single OAPP template by its GUID id. |
| `web5_oapp_template_load_all_for_avatar` | WEB5 STARNET: loads every OAPP template published by or accessible to an avatar. |
| `web5_oapp_template_search` | WEB5 STARNET: searches OAPP templates on STARNET by a free-text search term. |
| `web5_plugin_activate` | WEB5 STARNET: activates an installed plugin for an avatar. |
| `web5_plugin_deactivate` | WEB5 STARNET: deactivates a running plugin for an avatar. |
| `web5_plugin_download_and_install` | WEB5 STARNET: downloads and installs a plugin from STARNET in one step. |
| `web5_plugin_load` | WEB5 STARNET: loads a single OASIS plugin by its GUID id. |
| `web5_plugin_load_all_for_avatar` | WEB5 STARNET: loads every plugin published by or accessible to an avatar. |
| `web5_plugin_search` | WEB5 STARNET: searches plugins on STARNET by a free-text search term. |
| `web5_quest_complete` | WEB5: marks an entire quest complete for an avatar. |
| `web5_quest_complete_objective` | WEB5: marks a single objective of a quest complete for an avatar. |
| `web5_quest_load` | WEB5: loads a single quest by its GUID id. |
| `web5_quest_load_all_for_avatar` | WEB5: loads every quest available to/started by an avatar. |
| `web5_quest_search` | WEB5: searches quests on STARNET by a free-text search term. |
| `web5_quest_start` | WEB5: starts a quest for an avatar. |
| `web5_runtime_download` | WEB5 STARNET: downloads a runtime from STARNET to a local path. |
| `web5_runtime_download_and_install` | WEB5 STARNET: downloads and installs a runtime from STARNET in one step. |
| `web5_runtime_load` | WEB5 STARNET: loads a single OASIS runtime by its GUID id. |
| `web5_runtime_load_all_for_avatar` | WEB5 STARNET: loads every runtime published by or accessible to an avatar. |
| `web5_runtime_search` | WEB5 STARNET: searches runtimes on STARNET by a free-text search term. |
| `web5_star_geo_nft_collection_download` | WEB5 STARNET: downloads a STAR geo-spatial NFT collection from STARNET to a local path. |
| `web5_star_geo_nft_collection_load` | WEB5 STARNET: loads a single STAR geo-spatial NFT collection by its GUID id. |
| `web5_star_geo_nft_collection_load_all_for_avatar` | WEB5 STARNET: loads every STAR geo-spatial NFT collection owned by an avatar. |
| `web5_star_geo_nft_collection_search` | WEB5 STARNET: searches STAR geo-spatial NFT collections on STARNET by a free-text search term. |
| `web5_star_geo_nft_delete` | WEB5 STARNET: soft-deletes a STAR geo-spatial NFT by id (set softDelete=false to hard delete). |
| `web5_star_geo_nft_download` | WEB5 STARNET: downloads a STAR geo-spatial NFT from STARNET to a local path. |
| `web5_star_geo_nft_load` | WEB5 STARNET: loads a single STAR geo-spatial NFT by its GUID id. |
| `web5_star_geo_nft_load_all_for_avatar` | WEB5 STARNET: loads every STAR geo-spatial NFT published by or owned by an avatar. |
| `web5_star_geo_nft_search` | WEB5 STARNET: searches STAR geo-spatial NFTs on STARNET by a free-text search term. |
| `web5_star_nft_collection_download` | WEB5 STARNET: downloads a STAR NFT collection from STARNET to a local path. |
| `web5_star_nft_collection_load` | WEB5 STARNET: loads a single STAR NFT collection by its GUID id. |
| `web5_star_nft_collection_load_all_for_avatar` | WEB5 STARNET: loads every STAR NFT collection owned by an avatar. |
| `web5_star_nft_collection_search` | WEB5 STARNET: searches STAR NFT collections on STARNET by a free-text search term. |
| `web5_star_nft_delete` | WEB5 STARNET: soft-deletes a STAR NFT by id (set softDelete=false to hard delete). |
| `web5_star_nft_download` | WEB5 STARNET: downloads a STAR NFT from STARNET to a local path. |
| `web5_star_nft_load` | WEB5 STARNET: loads a single STAR NFT by its GUID id. |
| `web5_star_nft_load_all_for_avatar` | WEB5 STARNET: loads every STAR NFT published by or owned by an avatar. |
| `web5_star_nft_search` | WEB5 STARNET: searches STAR NFTs on STARNET by a free-text search term. |
| `web5_star_zome_download` | WEB5 STARNET: downloads a STAR Zome from STARNET to a local path. |
| `web5_star_zome_load` | WEB5 STARNET: loads a single STAR Zome by its GUID id. Zomes are the callable module containers inside Holochain OAPPs. |
| `web5_star_zome_load_all_for_avatar` | WEB5 STARNET: loads every STAR Zome published by or accessible to an avatar. |
| `web5_star_zome_search` | WEB5 STARNET: searches STAR Zomes on STARNET by a free-text search term. |

## WEB6 commands (39)

| Command | Description |
|---|---|
| `web6_braid_find_graph` | WEB6 Holonic BRAID: looks up the shared reasoning graph already generated for a task type, if any (lookup-or-create pattern - zero generation cost on a hit). |
| `web6_braid_record_outcome` | WEB6 Holonic BRAID: feeds a real solver outcome back into a graph's quality metadata via EMA (updates avg_solver_accuracy). |
| `web6_braid_save_graph` | WEB6 Holonic BRAID: stores a newly generated Mermaid reasoning graph in the shared library for a task type (the Generator step of the two-stage BRAID protocol). |
| `web6_complete` | WEB6: routes a unified chat completion request to whichever AI provider/model best fits (100 providers: OpenAI, Anthropic, Gemini, Groq, Mistral, XAI, Ollama, Cohere, AzureOpenAI, HuggingFace, AWSBedrock, Cerebras, TogetherAI, Perplexity, SambaNova, OpenRouter, DeepSeek, ElevenLabs, RunwayML, Black Forest Labs, Bittensor, GaiaNet, Venice AI, Alibaba Qwen, Doubao, MiniMax, Zhipu AI, Baidu ERNIE, Naver HyperCLOVA X, and 70+ more, or 'auto'). 512 MCP tools total across WEB4–WEB10. |
| `web6_embed` | WEB6: generates embeddings for one or more texts via the configured provider (OpenAI, Cohere, or HuggingFace). Returns float arrays suitable for semantic search, RAG pipelines, or cosine-similarity comparisons. |
| `web6_estimate_cost` | WEB6: estimates the USD cost of a completion call before executing it, based on expected token counts and the provider's current pricing. Use this before long or expensive calls. |
| `web6_fahrn_dispatch` | WEB6 FAHRN: dispatches a problem to the reasoning network. The controller agent scores eligible agents, picks Serial/Parallel/Decomposed execution, runs loop detection, assembles the final Mermaid plan and updates every involved agent's score via EMA. |
| `web6_fahrn_evolve_agent_skill` | WEB6 SkillOpt: triggers one SkillOpt epoch for a FAHRN agent and task category — proposes bounded textual edits to the skill document, validates on held-out problems, and accepts only improvements. Returns the updated skill document. |
| `web6_fahrn_get_agent_skill` | WEB6 SkillOpt: returns the current best_skill.md document for a FAHRN agent and task category — the evolved natural-language procedure that guides the agent's reasoning, produced by the SkillOpt self-improvement loop (Microsoft Research arXiv:2605.23904, +23.5% avg improvement). |
| `web6_fahrn_get_agents` | WEB6 FAHRN: lists every reasoning agent currently registered, with live composite scoring metadata. |
| `web6_fahrn_register_agent` | WEB6 FAHRN: registers a new reasoning agent with the Fractal Adaptive Holonic Reasoning Network. |
| `web6_fahrn_seed_openserv_agents` | WEB6 FAHRN: seeds FAHRN with one reasoning agent per model in the OpenServ SERV catalog (skips any AgentName already registered), so the network can immediately score/route/braid across every OpenServ-reachable model (OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek) behind a single SERV_API_KEY. Safe to call repeatedly. |
| `web6_fahrn_solve` | WEB6 FAHRN hero endpoint: takes a natural-language problem and runs the full pipeline — auto-classify task type, inject avatar context (Web4+Web5), look up Holonic BRAID graph, dispatch to the reasoning network (Serial/Parallel/Decomposed/Debate/Voting), EMA-update agent scores, record session memory — returning the answer, reasoning trace, Mermaid plan, and full telemetry in one call. |
| `web6_generate_image` | WEB6: generates an image via StabilityAI or OpenAI (gpt-image-1). |
| `web6_get_avatar_context` | WEB6: assembles and returns a rich context block for an OASIS avatar — karma, karma level, active quests, world memberships — assembled from Web4 and Web5 in parallel. Use this to ground AI prompts in the avatar's real OASIS state. |
| `web6_get_model` | WEB6: returns full detail for a single model by its ID (e.g. 'gpt-4o', 'claude-sonnet-5', 'llama3.3'). |
| `web6_get_usage` | WEB6: returns the calling avatar's usage summary — daily calls used, effective daily limit (plan × karma multiplier), monthly token spend, and remaining quota today. |
| `web6_health` | WEB6: returns API status, version and UTC timestamp. No authentication required. Use to verify the WEB6 API is reachable before making authenticated calls. |
| `web6_list_models` | WEB6: returns the full WEB6 model catalogue — all available models with provider, tier, context window, pricing and capabilities. Optionally filter by plan (Free/Bronze/Silver/Gold). |
| `web6_list_openserv_models` | WEB6: lists every model reachable through the OpenServ provider (provider: "openserv") - the full SERV catalog spanning OpenAI, Anthropic, Google, xAI, Qwen and DeepSeek behind a single SERV_API_KEY. |
| `web6_list_providers` | WEB6: returns all 100 AI providers registered in WEB6 with their minimum plan requirement, supported endpoint types, and operational status. |
| `web6_memory_external_add` | WEB6 External Memory: adds a memory to the specified external memory provider (Mem0, Zep, Letta, LangMem, Graphiti), scoped to the avatar. |
| `web6_memory_external_list_providers` | WEB6 External Memory: lists the names of all external memory providers currently registered (auto-detected from environment variables on startup). |
| `web6_memory_external_search` | WEB6 External Memory: searches one or more configured external memory providers (Mem0, Zep, Letta, LangMem, Graphiti) for memories relevant to the given query, scoped to the avatar. Returns merged, score-ranked results. |
| `web6_memory_get_earth_holon` | WEB6 Holonic BRAID memory hierarchy: gets the single planetary Earth holon, creating it if this is the very first call anywhere. |
| `web6_memory_get_or_create_holon` | WEB6 Holonic BRAID memory hierarchy: finds or creates a holon at the given level (Session, Agent, User, Group, Neighbourhood, District, City, County, Country, Continent) under the given parent. |
| `web6_memory_propagate` | WEB6 Holonic BRAID memory hierarchy: propagates whatever the child holon's membrane rule permits up to its parent holon (a single hop). |
| `web6_memory_propagate_up` | WEB6 Holonic BRAID memory hierarchy: propagates permitted memory items up the fractal hierarchy for up to N hops (pass int.MaxValue to reach Earth). Priority 16a — multi-hop upward propagation. |
| `web6_memory_record` | WEB6 Holonic BRAID memory hierarchy: records a new memory item at the given holon. |
| `web6_memory_search` | WEB6 Holonic BRAID memory hierarchy: semantic search over all memory items in a holon. Returns the top-K items most similar to the query using cosine similarity over stored embedding vectors (falls back to keyword overlap when no embeddings are stored). Priority 16b — semantic search. |
| `web6_memory_set_membrane_rule` | WEB6 Holonic BRAID memory hierarchy: sets the membrane rule governing what a holon is allowed to propagate upward to its parent (per-field, consent-governed - default is private). |
| `web6_ml_classify_task` | WEB6 ML.NET: classifies a problem string into a FAHRN task category in-process (zero latency, no API call) using the trained ML.NET model or heuristic fallback. Returns: code/reasoning/writing/mathematics/legal/architecture/real-time/general. |
| `web6_ml_sentiment` | WEB6 ML.NET: analyses the sentiment of text in-process (no API call). Returns Positive, Neutral, or Negative. |
| `web6_orchestrate_autogen` | WEB6 Orchestrators: runs an AutoGen conversation initiation (initialMessage plus optional systemMessage) through WEB6 completions and returns the assistant reply in AutoGen's message shape. |
| `web6_orchestrate_crewai` | WEB6 Orchestrators: runs a CrewAI-style task list as WEB6 completions. Each task has Id, Agent, Description, ExpectedOutput. process 'sequential' feeds each task the earlier outputs; 'hierarchical' adds a final manager synthesis. |
| `web6_orchestrate_langgraph` | WEB6 Orchestrators: executes one LangGraph node's LLM step. Supply NodeName, optional Instruction, the current State and Messages; returns the updated state (State["<node>_output"]), NextNode and messages. |
| `web6_orchestrator_invoke` | WEB6: invokes a registered orchestrator adapter with a normalised request, translating to/from its native protocol wire format (MCP, A2A, ACP, ANP, LangGraph, OpenAI Agents SDK, Nostr NIP-90, LangChain, AutoGen, CrewAI, SemanticKernel, BeeAgent, Temporal, Dapr, NATSJetStream, gRPC, GraphQL, Kafka, AMQP, MQTT, Webhook — 22 protocols) under the hood. |
| `web6_orchestrator_list` | WEB6: lists every registered orchestrator adapter. |
| `web6_orchestrator_register` | WEB6: registers an external agent/orchestrator endpoint (MCP server, A2A agent, LangChain/AutoGen/CrewAI/Semantic Kernel deployment, or generic webhook). |

## WEB7 commands (7)

| Command | Description |
|---|---|
| `web7_create_space` | WEB7: creates a collective consciousness space - a shared intention field where multiple consenting sessions co-create. |
| `web7_end_session` | WEB7: ends a session instantly - with Ephemeral retention (the default), all signal-derived data is wiped immediately, leaving no trace. |
| `web7_get_aggregate_field` | WEB7: recomputes and returns the aggregate (mean) intention field across every participating session in a collective consciousness space - never any individual's raw signal. |
| `web7_get_session` | WEB7: gets a symbiosis session's current state, including its last computed intention state. |
| `web7_join_space` | WEB7: joins a consenting symbiosis session to a collective consciousness space. |
| `web7_start_session` | WEB7: starts a new symbiosis session. consentGranted must be explicitly true - the connection is always voluntary. |
| `web7_submit_signals` | WEB7: submits a batch of raw bio-signal samples (EEG/HRV/GSR/EyeTracking/VocalHarmonics) for an active, consenting session and returns the freshly computed intention state (focus, arousal, emotional valence, cognitive load) via real FFT/HRV/GSR DSP. Rejects any channel name implying an invasive/implanted source (Borg-Free pledge). |

## WEB8 commands (8)

| Command | Description |
|---|---|
| `web8_add_link` | WEB8: declares a bidirectional weighted link (mesh edge) between two nodes, carrying its latency for shortest-path routing. |
| `web8_compute_route` | WEB8: computes the shortest (lowest cumulative latency) path between two nodes via Dijkstra's algorithm, excluding any node outside the liveness window (self-healing). |
| `web8_get_nodes` | WEB8: lists every node currently registered in the mesh. |
| `web8_heartbeat` | WEB8: records a heartbeat for a node, keeping it inside the liveness window so routing continues to consider it healthy. |
| `web8_register_node` | WEB8: registers a node in the mesh - any externally-reachable system that can accept a relayed message at an HTTP endpoint. |
| `web8_send_message` | WEB8: routes and relays a message hop-by-hop to its destination via real HTTP forwarding, self-healing around any failed/stale node by excluding it and recomputing the route. |
| `web8_translate_inbound` | WEB8 protocol bridge: translates an external system's raw payload (Json, FormUrlEncoded or PlainText) into the unified MeshMessage envelope. |
| `web8_translate_outbound` | WEB8 protocol bridge: translates a MeshMessage's payload back into a target external wire format (Json, FormUrlEncoded or PlainText). |

## WEB9 commands (1)

| Command | Description |
|---|---|
| `web9_get_unified_status` | WEB9: probes WEB4-WEB8 in parallel and returns one unified status report - "the network observing itself" implemented as real cross-service health aggregation and live metric collection. |

## WEB10 commands (1)

| Command | Description |
|---|---|
| `web10_get_source` | WEB10: returns the foundational OASIS runtime/version identity (the Alpha) together with WEB9's live unified status across WEB4-WEB8 (the Omega) - "WEB10 = WEB0" as one real, queryable endpoint. |
