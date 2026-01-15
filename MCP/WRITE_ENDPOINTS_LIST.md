# All Write/Create MCP Endpoints

**Last Updated:** 2026-01-11  
**Status:** ✅ **COMPLETE LIST**

---

## Overview

This document lists **ALL** write/create/modify endpoints available in the OASIS MCP server. These endpoints modify data, so use with caution in production.

**Total Write Endpoints:** ~25+

---

## Authentication Endpoints

### `oasis_authenticate_avatar`
- **Type:** Authentication (required for most write operations)
- **Description:** Authenticate and get JWT token
- **Parameters:** `username`, `password`
- **Test Status:** ✅ Tested

### `oasis_register_avatar`
- **Type:** Create
- **Description:** Register/Create a new avatar
- **Parameters:** `username`, `email`, `password`, `confirmPassword`, `acceptTerms`, `avatarType` (optional), `firstName`, `lastName`, `title`
- **Test Status:** ⚠️ Not tested (creates new avatar)

---

## Wallet Creation Endpoints

### `oasis_create_wallet`
- **Type:** Create
- **Description:** Create a basic wallet for an avatar
- **Parameters:** `avatarId`, `walletType` (optional)
- **Test Status:** ⚠️ Not tested

### `oasis_create_wallet_full`
- **Type:** Create
- **Description:** Create wallet with full options
- **Parameters:** `avatarId`, `WalletProviderType`, `Name` (optional), `Description` (optional), `GenerateKeyPair` (default: true), `IsDefaultWallet` (default: false)
- **Test Status:** ⚠️ Not tested

### `oasis_create_solana_wallet` ⭐ NEW
- **Type:** Create
- **Description:** Create Solana wallet (follows correct order: public key first, then private key)
- **Parameters:** `avatarId`, `setAsDefault` (default: true), `ensureProviderActivated` (default: true)
- **Test Status:** ✅ Ready to test

### `oasis_import_wallet_private_key`
- **Type:** Import
- **Description:** Import wallet using private key
- **Parameters:** `avatarId`, `privateKey`, `providerType`
- **Test Status:** ⚠️ Not tested

### `oasis_import_wallet_public_key`
- **Type:** Import
- **Description:** Import wallet using public key
- **Parameters:** `avatarId`, `publicKey`, `providerType`
- **Test Status:** ⚠️ Not tested

### `oasis_set_default_wallet`
- **Type:** Update
- **Description:** Set default wallet for an avatar
- **Parameters:** `avatarId`, `walletId`, `providerType`
- **Test Status:** ⚠️ Not tested

---

## NFT Creation Endpoints

### `oasis_mint_nft`
- **Type:** Create
- **Description:** Mint a new NFT
- **Parameters:** `JSONMetaDataURL` (required), `Symbol` (required), `Title`, `Description`, `ImageUrl`, `ThumbnailUrl`, `Price`, `NumberToMint`, `OnChainProvider` (default: SolanaOASIS), `OffChainProvider`, `NFTOffChainMetaType`, `NFTStandardType`, `SendToAddressAfterMinting`, `SendToAvatarAfterMintingId`
- **Test Status:** ⚠️ Not tested (creates actual NFT)

### `oasis_send_nft`
- **Type:** Transfer
- **Description:** Send NFT between wallets
- **Parameters:** `FromWalletAddress`, `ToWalletAddress`, `FromProvider`, `ToProvider`, `Amount` (optional), `MemoText` (optional)
- **Test Status:** ⚠️ Not tested

---

## Holon/Data Operations

### `oasis_save_holon`
- **Type:** Create/Update
- **Description:** Save or create a holon (data object)
- **Parameters:** `holon` (object with name, description, holonType, etc.)
- **Test Status:** ✅ Ready to test

### `oasis_update_holon`
- **Type:** Update
- **Description:** Update an existing holon
- **Parameters:** `holonId`, `holon` (updated holon object)
- **Test Status:** ✅ Ready to test

### `oasis_delete_holon`
- **Type:** Delete
- **Description:** Delete a holon
- **Parameters:** `holonId`
- **Test Status:** ⚠️ Not tested (destructive operation)

---

## Avatar Update Operations

### `oasis_update_avatar`
- **Type:** Update
- **Description:** Update avatar information
- **Parameters:** `avatarId`, `updates` (object with fields to update)
- **Test Status:** ✅ Ready to test

---

## Karma Operations

### `oasis_add_karma`
- **Type:** Create
- **Description:** Add positive karma to an avatar
- **Parameters:** `avatarId`, `KarmaType` (e.g., "HelpOtherPerson"), `karmaSourceType` (e.g., "App"), `KaramSourceTitle` (optional), `KarmaSourceDesc` (optional)
- **Test Status:** ⚠️ Not tested (modifies karma)

### `oasis_remove_karma`
- **Type:** Create
- **Description:** Remove karma (negative karma) from an avatar
- **Parameters:** `avatarId`, `KarmaType` (e.g., "DropLitter"), `karmaSourceType`, `KaramSourceTitle` (optional), `KarmaSourceDesc` (optional)
- **Test Status:** ⚠️ Not tested (modifies karma)

### `oasis_vote_positive_karma_weighting`
- **Type:** Vote
- **Description:** Vote for positive karma weighting
- **Parameters:** `karmaType` (e.g., "HelpOtherPerson"), `weighting` (number)
- **Test Status:** ⚠️ Not tested

### `oasis_vote_negative_karma_weighting`
- **Type:** Vote
- **Description:** Vote for negative karma weighting
- **Parameters:** `karmaType` (e.g., "DropLitter"), `weighting` (number)
- **Test Status:** ⚠️ Not tested

---

## Transaction Operations

### `oasis_send_transaction`
- **Type:** Transfer
- **Description:** Send tokens between wallets
- **Parameters:** `amount` (required), `fromAvatarId` (optional), `fromWalletAddress` (optional), `toAvatarId` (optional), `toAddress` (optional), `toWalletAddress` (optional), `fromProvider` (default: 3 for SolanaOASIS), `toProvider` (default: 3), `memoText` (optional)
- **Test Status:** ⚠️ Not tested (sends actual tokens)

---

## A2A Agent Operations

### `oasis_register_agent_capabilities`
- **Type:** Register
- **Description:** Register agent capabilities via A2A Protocol
- **Parameters:** `services` (array, required), `skills` (array, optional), `pricing` (object, optional), `status` (string/number, optional), `max_concurrent_tasks` (number, optional), `description` (string, optional)
- **Test Status:** ⚠️ Not tested (requires Agent avatar type)

### `oasis_register_agent_as_serv_service`
- **Type:** Register
- **Description:** Register A2A agent as SERV service
- **Parameters:** None (uses authenticated avatar)
- **Test Status:** ⚠️ Not tested (requires Agent avatar type)

### `oasis_send_a2a_jsonrpc_request`
- **Type:** Send
- **Description:** Send A2A JSON-RPC 2.0 request
- **Parameters:** `method` (required, e.g., "ping", "service_request"), `params` (object, optional), `id` (string, optional)
- **Test Status:** ⚠️ Not tested

### `oasis_mark_a2a_message_processed`
- **Type:** Update
- **Description:** Mark A2A message as processed
- **Parameters:** `messageId` (required)
- **Test Status:** ⚠️ Not tested

### `oasis_register_openserv_agent`
- **Type:** Register
- **Description:** Register OpenSERV AI agent as A2A agent and SERV service
- **Parameters:** `openServAgentId` (required), `openServEndpoint` (required), `capabilities` (array, required), `apiKey` (optional), `username` (optional), `email` (optional), `password` (optional)
- **Test Status:** ⚠️ Not tested

### `oasis_execute_ai_workflow`
- **Type:** Execute
- **Description:** Execute AI workflow via A2A Protocol
- **Parameters:** `toAgentId` (required), `workflowRequest` (required), `workflowParameters` (object, optional)
- **Test Status:** ⚠️ Not tested

---

## Summary by Category

| Category | Count | Endpoints |
|----------|-------|-----------|
| **Authentication** | 2 | authenticate, register |
| **Wallet Creation** | 6 | create_wallet, create_wallet_full, create_solana_wallet, import_private_key, import_public_key, set_default |
| **NFT Operations** | 2 | mint_nft, send_nft |
| **Holon/Data** | 3 | save_holon, update_holon, delete_holon |
| **Avatar** | 1 | update_avatar |
| **Karma** | 4 | add_karma, remove_karma, vote_positive_weighting, vote_negative_weighting |
| **Transactions** | 1 | send_transaction |
| **A2A** | 6 | register_capabilities, register_serv_service, send_jsonrpc, mark_message_processed, register_openserv, execute_workflow |
| **Total** | **25+** | |

---

## Testing Write Endpoints

### Safe to Test (Non-Destructive)
- ✅ `oasis_authenticate_avatar` - Authentication
- ✅ `oasis_create_solana_wallet` - Creates wallet (safe)
- ✅ `oasis_save_holon` - Creates test holon (safe)
- ✅ `oasis_update_holon` - Updates holon (safe if you created it)
- ✅ `oasis_update_avatar` - Updates avatar info (safe)

### Use with Caution (Creates Real Data)
- ⚠️ `oasis_register_avatar` - Creates new avatar
- ⚠️ `oasis_mint_nft` - Creates actual NFT
- ⚠️ `oasis_add_karma` - Modifies karma score
- ⚠️ `oasis_send_transaction` - Sends real tokens

### Destructive Operations
- 🚨 `oasis_delete_holon` - Permanently deletes holon
- 🚨 `oasis_remove_karma` - Reduces karma score

---

## Test Scripts

### 1. Test All Write Endpoints
```bash
# Requires authentication
export TEST_AVATAR_ID="your-avatar-id"
export OASIS_PASSWORD="your-password"
npx tsx test-all-write-endpoints.ts
```

### 2. Comprehensive Test (includes write operations)
```bash
# Includes write operations if authenticated
export TEST_AVATAR_ID="your-avatar-id"
export OASIS_PASSWORD="your-password"
npx tsx test-mcp-endpoints-comprehensive.ts
```

---

## Authentication Requirements

Most write endpoints require authentication. Set up authentication:

1. **Set credentials in environment:**
   ```bash
   export OASIS_USERNAME="OASIS_ADMIN"
   export OASIS_PASSWORD="your-password"
   ```

2. **Or authenticate first:**
   ```bash
   # Use oasis_authenticate_avatar endpoint
   # JWT token will be stored for subsequent requests
   ```

---

## Related Documentation

- `ENDPOINT_INVENTORY.md` - Complete endpoint inventory
- `TEST_COVERAGE.md` - Test coverage information
- `test-all-write-endpoints.ts` - Script to test all write operations
