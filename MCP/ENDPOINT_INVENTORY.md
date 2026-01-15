# OASIS API Endpoint Inventory

This document catalogs all OASIS API endpoints and their MCP tool status.

**Last Updated:** 2026-01-07  
**Latest Addition:** Added 25+ new MCP tools covering Avatar names/portraits, NFT operations (mint address, all NFTs), Wallet operations (default wallet, import, analytics, tokens, chains), Data/Holon operations (load all, update), Karma operations (akashic records, weighting, voting), and Search operations (basic, advanced, files).  
**Test Results:** See [ENDPOINT_TEST_RESULTS.md](./ENDPOINT_TEST_RESULTS.md) for detailed test findings.

## Status Legend
- ✅ = Available in MCP
- ❌ = Missing from MCP
- 🔄 = Needs implementation

## Avatar Controller

### Registration & Authentication
- ✅ `POST /api/avatar/register` → `oasis_register_avatar`
- ❌ `POST /api/avatar/register/{providerType}/{setGlobally}`
- ❌ `GET /api/avatar/verify-email`
- ❌ `POST /api/avatar/verify-email`
- ✅ `POST /api/avatar/authenticate` → `oasis_authenticate_avatar`
- ❌ `POST /api/avatar/authenticate/{providerType}/{setGlobally}`
- ❌ `POST /api/avatar/authenticate-token/{JWTToken}`
- ❌ `POST /api/avatar/refresh-token`
- ❌ `POST /api/avatar/revoke-token`

### Password Management
- ❌ `POST /api/avatar/forgot-password`
- ❌ `POST /api/avatar/validate-reset-token`
- ❌ `POST /api/avatar/reset-password`

### Avatar CRUD
- ✅ `GET /api/avatar/{id}` → `oasis_get_avatar` (by ID)
- ✅ `GET /api/avatar/username/{username}` → `oasis_get_avatar` (by username)
- ✅ `GET /api/avatar/email/{email}` → `oasis_get_avatar` (by email)
- ✅ `GET /api/avatar/get-all-avatars` → `oasis_get_all_avatars`
- ✅ `GET /api/avatar/get-avatar-detail-by-id/{id}` → `oasis_get_avatar_detail`
- ✅ `GET /api/avatar/get-avatar-detail-by-email/{email}` → `oasis_get_avatar_detail`
- ✅ `GET /api/avatar/get-avatar-detail-by-username/{username}` → `oasis_get_avatar_detail`
- ✅ `GET /api/avatar/get-all-avatar-details` → `oasis_get_all_avatar_details`
- ✅ `GET /api/avatar/get-all-avatar-names/{includeUsernames}/{includeIds}` → `oasis_get_all_avatar_names`
- ✅ `PUT /api/avatar/{id}` → `oasis_update_avatar`

### Avatar Portraits
- ✅ `GET /api/avatar/get-avatar-portrait/{id}` → `oasis_get_avatar_portrait`
- ✅ `GET /api/avatar/get-avatar-portrait-by-username/{username}` → `oasis_get_avatar_portrait`
- ✅ `GET /api/avatar/get-avatar-portrait-by-email/{email}` → `oasis_get_avatar_portrait`
- ❌ `POST /api/avatar/upload-avatar-portrait`

## NFT Controller

### Read Operations
- ✅ `GET /api/nft/load-nft-by-id/{id}` → `oasis_get_nft`
- ✅ `GET /api/nft/load-nft-by-hash/{hash}` → `oasis_get_nft_by_hash`
- ✅ `GET /api/nft/load-all-nfts-for_avatar/{avatarId}` → `oasis_get_nfts`
- ✅ `GET /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}` → `oasis_get_nfts_for_mint_address`
- ✅ `GET /api/nft/load-all-geo-nfts-for-avatar/{avatarId}` → `oasis_get_geo_nfts`
- ✅ `GET /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}` → `oasis_get_geo_nfts_for_mint_address`
- ✅ `GET /api/nft/load-all-nfts` → `oasis_get_all_nfts` (Wizard only)
- ✅ `GET /api/nft/load-all-geo-nfts` → `oasis_get_all_geo_nfts` (Wizard only)

### Write Operations
- ✅ `POST /api/nft/mint-nft` → `oasis_mint_nft`
- ✅ `POST /api/nft/send-nft` → `oasis_send_nft`
- ❌ `POST /api/nft/place-geo-nft`
- ❌ `POST /api/nft/mint-and-place-geo-nft`

## Wallet Controller

### Read Operations
- ✅ `GET /api/wallet/{avatarId}` → `oasis_get_wallet` (basic)
- ✅ `GET /api/wallet/avatar/{id}/wallets` → `oasis_get_provider_wallets` (provider wallets)
- ✅ `GET /api/wallet/avatar/username/{username}/wallets` → `oasis_get_provider_wallets_by_username`
- ✅ `GET /api/wallet/avatar/email/{email}/wallets` → `oasis_get_provider_wallets_by_email`
- ✅ `GET /api/wallet/avatar/{id}/default-wallet` → `oasis_get_default_wallet`
- ✅ `GET /api/wallet/avatar/{avatarId}/portfolio/value` → `oasis_get_portfolio_value`
- ✅ `GET /api/wallet/avatar/{avatarId}/wallets/chain/{chain}` → `oasis_get_wallets_by_chain`
- ✅ `GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/analytics` → `oasis_get_wallet_analytics`
- ✅ `GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/tokens` → `oasis_get_wallet_tokens`
- ✅ `GET /api/wallet/transaction/{transactionHash}` → `oasis_get_transaction`
- ✅ `GET /api/wallet/supported-chains` → `oasis_get_supported_chains`

### Write Operations
- ✅ `POST /api/wallet/{avatarId}` → `oasis_create_wallet` (basic)
- ✅ `POST /api/wallet/avatar/{avatarId}/create-wallet` → `oasis_create_wallet_full` (full)
- ❌ `POST /api/wallet/avatar/{id}/wallets` (save provider wallets)
- ✅ `POST /api/wallet/avatar/{id}/default-wallet/{walletId}` → `oasis_set_default_wallet`
- ✅ `POST /api/wallet/avatar/{avatarId}/import/private-key` → `oasis_import_wallet_private_key`
- ✅ `POST /api/wallet/avatar/{avatarId}/import/public-key` → `oasis_import_wallet_public_key`
- ✅ `POST /api/wallet/send-token` → `oasis_send_transaction` (basic)
- ❌ `POST /api/wallet/send_token` (full)
- ❌ `POST /api/wallet/transfer`

## Data/Holon Controller

### Read Operations
- ✅ `GET /api/data/load-holon/{id}` → `oasis_get_holon`
- ❌ `POST /api/data/load-holon` (with options)
- ✅ `GET /api/data/load-holons-for-parent/{parentId}` → `oasis_load_holons_for_parent`
- ✅ `GET /api/data/load-all-holons` → `oasis_load_all_holons`
- ⚠️ `POST /api/data/search-holons` → `oasis_search_holons` ⚠️ **ISSUE:** Returns 404 - endpoint may not be implemented
- ❌ `GET /api/data/load-holon-by-meta-data`

### Write Operations
- ✅ `POST /api/data/save-holon` → `oasis_save_holon`
- ✅ `PUT /api/data/update-holon/{id}` → `oasis_update_holon`
- ✅ `DELETE /api/data/delete-holon/{id}` → `oasis_delete_holon`
- ❌ `POST /api/data/save-holon-with-options`

## Karma Controller

### Read Operations
- ✅ `GET /api/karma/{avatarId}` → `oasis_get_karma` (basic)
- ❌ `GET /api/karma/get-karma-for-avatar/{avatarId}`
- ✅ `GET /api/karma/get-karma-akashic-records-for-avatar/{avatarId}` → `oasis_get_karma_akashic_records`
- ✅ `GET /api/karma/get-karma-stats/{avatarId}` → `oasis_get_karma_stats`
- ✅ `GET /api/karma/get-karma-history/{avatarId}` → `oasis_get_karma_history`
- ⚠️ `GET /api/karma/get-positive-karma-weighting/{karmaType}` → `oasis_get_positive_karma_weighting` ⚠️ **ISSUE:** Need to document valid karma type enum values
- ⚠️ `GET /api/karma/get-negative-karma-weighting/{karmaType}` → `oasis_get_negative_karma_weighting` ⚠️ **ISSUE:** Need to document valid karma type enum values

### Write Operations
- ✅ `POST /api/karma/add-karma-to-avatar/{avatarId}` → `oasis_add_karma`
- ✅ `POST /api/karma/remove-karma-from-avatar/{avatarId}` → `oasis_remove_karma`
- ✅ `POST /api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}` → `oasis_vote_positive_karma_weighting`
- ✅ `POST /api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}` → `oasis_vote_negative_karma_weighting`
- ❌ `POST /api/karma/set-positive-karma-weighting/{karmaType}/{weighting}` (Wizard)
- ❌ `POST /api/karma/set-negative-karma-weighting/{karmaType}/{weighting}` (Wizard)

## Search Controller
- ❌ `GET /api/search` → `oasis_basic_search` ⚠️ **ISSUE:** Route mismatch - controller expects `{searchParams}` route param, client uses query string
- ❌ `GET /api/search/advanced` → `oasis_advanced_search` ⚠️ **ISSUE:** Requires request body but client sends query params
- ❌ `POST /api/search/search-holons` → `oasis_search_holons` ⚠️ **NOT IMPLEMENTED:** Endpoint doesn't exist in SearchController (use `/api/data/search-holons` instead)
- ❌ `POST /api/search/search-avatars` → `oasis_search_avatars` ⚠️ **NOT IMPLEMENTED:** Endpoint doesn't exist in SearchController
- ❌ `POST /api/search/search-nfts` → `oasis_search_nfts` ⚠️ **NOT IMPLEMENTED:** Endpoint doesn't exist in SearchController
- ❌ `POST /api/search/search-files` → `oasis_search_files` ⚠️ **NOT IMPLEMENTED:** Endpoint doesn't exist in SearchController

## Other Controllers (Not Yet Implemented)
- OLand Controller
- Files Controller
- Chat Controller
- Messaging Controller
- Social Controller
- Share Controller
- Settings Controller
- Seeds Controller
- Stats Controller
- Video Controller
- Solana Controller
- Telos Controller
- Holochain Controller
- ONODE Controller
- ONET Controller
- Map Controller
- OAPP Controller
- Cargo Controller
- Gifts Controller
- Eggs Controller
- Missions Controller
- Competition Controller
- Provider Controller
- Keys Controller
- HyperDrive Controller
- Subscription Controller
- EOSIO Controller
- ✅ **A2A Controller** - See [A2A/SERV Tools](#a2a-controller) below
- AI Controller
- Health Controller

## A2A Controller

### Agent Cards & Discovery
- ✅ `GET /api/a2a/agent-card/{agentId}` → `oasis_get_agent_card`
- ✅ `GET /api/a2a/agents` → `oasis_get_all_agents`
- ✅ `GET /api/a2a/agents/by-service/{service}` → `oasis_get_agents_by_service`

### Agent Capabilities & Registration
- ✅ `POST /api/a2a/agent/capabilities` → `oasis_register_agent_capabilities`
- ✅ `POST /api/a2a/agent/register-service` → `oasis_register_agent_as_serv_service`

### SERV Infrastructure Integration
- ✅ `GET /api/a2a/agents/discover-serv` → `oasis_discover_agents_via_serv`
- ✅ `GET /api/a2a/agents/discover-serv?service={name}` → `oasis_discover_agents_via_serv` (with serviceName param)

### A2A Protocol Communication
- ✅ `POST /api/a2a/jsonrpc` → `oasis_send_a2a_jsonrpc_request`
- ✅ `GET /api/a2a/messages` → `oasis_get_pending_a2a_messages`
- ✅ `POST /api/a2a/messages/{messageId}/process` → `oasis_mark_a2a_message_processed`

### OpenSERV Integration
- ✅ `POST /api/a2a/openserv/register` → `oasis_register_openserv_agent`
- ✅ `POST /api/a2a/workflow/execute` → `oasis_execute_ai_workflow`

## Smart Contract Generator (MCP Tools)

### Generation & Compilation
- ✅ `POST /api/v1/contracts/generate` → `scgen_generate_contract` ✅ **TESTED**
- ✅ `POST /api/v1/contracts/compile` → `scgen_compile_contract` ⚠️ **TESTED** (requires compiler)
- ✅ `POST /api/v1/contracts/generate` + compile → `scgen_generate_and_compile` ⚠️ **TESTED** (requires compiler)
- ✅ `GET /api/v1/contracts/cache-stats` → `scgen_get_cache_stats` ✅ **TESTED**

### Deployment
- ✅ `POST /api/v1/contracts/deploy` → `scgen_deploy_contract` ✅ **UPDATED & TESTED** (uses @solana/web3.js for Solana)

**Test Status:** See [SMART_CONTRACT_ENDPOINT_TEST_RESULTS.md](./SMART_CONTRACT_ENDPOINT_TEST_RESULTS.md) for detailed test results.

**Note:** These are MCP tools that call the SmartContractGenerator API. The API supports:
- **Ethereum** (Solidity) - Returns plain source code
- **Solana** (Rust) - Returns ZIP with full project structure
- **Radix** (Scrypto) - Not yet tested

## Summary

**Total Endpoints Cataloged:** ~200+
**Currently Available in MCP:** ~75 (including 12 A2A/SERV tools + 5 Smart Contract tools)
**Missing from MCP:** ~135+

## Priority Order for Implementation

1. **High Priority** (Core functionality) - **IN PROGRESS**:
   - ✅ Complete Avatar operations (details ✅, portraits ❌, names ❌)
   - ✅ Complete NFT operations (GeoNFTs ✅, send NFT ✅)
   - 🔄 Complete Wallet operations (provider wallets ✅, transactions ✅, analytics ❌)
   - ✅ Complete Data/Holon operations (search ✅, load by parent ✅, delete ✅)
   - ✅ Complete Karma operations (add ✅, remove ✅, stats ✅, history ✅)
   - ✅ Search operations (avatars ✅, NFTs ✅, holons ✅, files ❌)

2. **Medium Priority** (Important features):
   - OLand operations
   - Files operations
   - Chat/Messaging operations
   - Social operations
   - Avatar portraits (upload/get)
   - Password management (forgot/reset)
   - More wallet operations (import, export, analytics)

3. **Low Priority** (Specialized features):
   - Other controllers as needed

