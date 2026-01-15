# MCP Endpoint Testing Guide

**Last Updated:** 2026-01-11  
**Status:** ✅ **READY FOR TESTING**

---

## Overview

This document describes how to test the MCP endpoints available in the `max-build3` branch. The MCP server provides access to OASIS API functionality through Model Context Protocol tools.

---

## Available MCP Endpoints

Based on the `max-build3` branch commit `052e864fb`, the following MCP endpoints are available:

### Avatar Endpoints
- `oasis_get_avatar` - Get avatar by ID, username, or email
- `oasis_get_avatar_detail` - Get detailed avatar information
- `oasis_get_all_avatars` - Get all avatars (Wizard/Admin only)
- `oasis_get_all_avatar_details` - Get all avatar details (Wizard/Admin only)
- `oasis_get_all_avatar_names` - Get all avatar names
- `oasis_get_avatar_portrait` - Get avatar portrait
- `oasis_register_avatar` - Register a new avatar
- `oasis_authenticate_avatar` - Authenticate and get JWT token
- `oasis_update_avatar` - Update avatar information
- `oasis_search_avatars` - Search avatars

### Karma Endpoints
- `oasis_get_karma` - Get karma score for an avatar
- `oasis_get_karma_stats` - Get karma statistics
- `oasis_get_karma_history` - Get karma history
- `oasis_add_karma` - Add positive karma
- `oasis_remove_karma` - Remove karma (negative karma)
- `oasis_get_karma_akashic_records` - Get karma akashic records

### NFT Endpoints
- `oasis_get_nfts` - Get all NFTs for an avatar
- `oasis_get_nft` - Get NFT details by ID
- `oasis_get_nft_by_hash` - Get NFT details by hash
- `oasis_get_geo_nfts` - Get all GeoNFTs for an avatar
- `oasis_get_nfts_for_mint_address` - Get NFTs for mint address
- `oasis_get_geo_nfts_for_mint_address` - Get GeoNFTs for mint address
- `oasis_get_all_nfts` - Get all NFTs (Wizard/Admin only)
- `oasis_get_all_geo_nfts` - Get all GeoNFTs (Wizard/Admin only)
- `oasis_mint_nft` - Mint a new NFT
- `oasis_send_nft` - Send NFT between wallets
- `oasis_search_nfts` - Search NFTs

### Wallet Endpoints
- `oasis_get_wallet` - Get wallet information for an avatar
- `oasis_get_provider_wallets` - Get provider wallets for an avatar
- `oasis_get_provider_wallets_by_username` - Get wallets by username
- `oasis_get_provider_wallets_by_email` - Get wallets by email
- `oasis_get_default_wallet` - Get default wallet for an avatar
- `oasis_set_default_wallet` - Set default wallet
- `oasis_get_wallets_by_chain` - Get wallets by chain
- `oasis_get_wallet_analytics` - Get wallet analytics
- `oasis_get_wallet_tokens` - Get tokens in a wallet
- `oasis_get_portfolio_value` - Get total portfolio value
- `oasis_get_supported_chains` - Get list of supported chains
- `oasis_get_transaction` - Get transaction details by hash
- `oasis_create_wallet` - Create a basic wallet
- `oasis_create_wallet_full` - Create wallet with full options
- `oasis_import_wallet_private_key` - Import wallet using private key
- `oasis_import_wallet_public_key` - Import wallet using public key
- `oasis_send_transaction` - Send tokens between wallets

### Holon/Data Endpoints
- `oasis_get_holon` - Get holon by ID
- `oasis_save_holon` - Save/create a holon
- `oasis_update_holon` - Update a holon
- `oasis_delete_holon` - Delete a holon
- `oasis_search_holons` - Search holons
- `oasis_load_holons_for_parent` - Load holons for a parent
- `oasis_load_all_holons` - Load all holons (Wizard/Admin only)

### Utility Endpoints
- `oasis_health_check` - Check OASIS API health status
- `oasis_basic_search` - Basic search across OASIS
- `oasis_advanced_search` - Advanced search with filters
- `oasis_search_files` - Search files

---

## Testing Methods

### Method 1: Using the Test Script

A test script is provided at `MCP/test-mcp-endpoints.ts`:

```bash
# Set environment variables
export OASIS_API_URL="https://api.oasisweb4.com"
export TEST_AVATAR_ID="your-avatar-id-here"

# Run tests
cd MCP
npx tsx test-mcp-endpoints.ts
```

### Method 2: Using MCP Directly in Cursor

If you have the MCP server configured in Cursor, you can test endpoints directly:

1. **Health Check:**
   ```
   Check OASIS API health status
   ```

2. **Get Avatar:**
   ```
   Get avatar information for ID d42b8448-52a9-4579-a6b1-b7c624616459
   ```

3. **Get Karma:**
   ```
   Get karma score for avatar d42b8448-52a9-4579-a6b1-b7c624616459
   ```

### Method 3: Manual Testing via MCP Protocol

You can test endpoints by sending MCP protocol messages directly. See the MCP documentation for protocol details.

---

## Test Cases

### Basic Tests (No Authentication Required)

1. **Health Check**
   - Endpoint: `oasis_health_check`
   - Expected: Returns API status

2. **Get Supported Chains**
   - Endpoint: `oasis_get_supported_chains`
   - Expected: Returns list of supported blockchain chains

### Avatar Tests (May Require Authentication)

1. **Get Avatar**
   - Endpoint: `oasis_get_avatar`
   - Args: `{ avatarId: "..." }`
   - Expected: Returns avatar information

2. **Search Avatars**
   - Endpoint: `oasis_search_avatars`
   - Args: `{ searchQuery: "test", limit: 10 }`
   - Expected: Returns matching avatars

### Wallet Tests (Requires Authentication)

1. **Get Wallet**
   - Endpoint: `oasis_get_wallet`
   - Args: `{ avatarId: "..." }`
   - Expected: Returns wallet information

2. **Get Provider Wallets**
   - Endpoint: `oasis_get_provider_wallets`
   - Args: `{ avatarId: "...", providerType: "SolanaOASIS" }`
   - Expected: Returns provider-specific wallets

3. **Get Portfolio Value**
   - Endpoint: `oasis_get_portfolio_value`
   - Args: `{ avatarId: "..." }`
   - Expected: Returns total portfolio value

### NFT Tests (Requires Authentication)

1. **Get NFTs**
   - Endpoint: `oasis_get_nfts`
   - Args: `{ avatarId: "..." }`
   - Expected: Returns all NFTs for avatar

2. **Search NFTs**
   - Endpoint: `oasis_search_nfts`
   - Args: `{ searchQuery: "test", limit: 10 }`
   - Expected: Returns matching NFTs

---

## Environment Variables

Set these environment variables for testing:

```bash
# OASIS API Configuration
export OASIS_API_URL="https://api.oasisweb4.com"
# or for local development:
export OASIS_API_URL="https://127.0.0.1:5004"

# Test Data
export TEST_AVATAR_ID="your-avatar-id-here"
export TEST_USERNAME="your-username"
export TEST_PASSWORD="your-password"

# Authentication (optional - will authenticate if needed)
export OASIS_USERNAME="your-username"
export OASIS_PASSWORD="your-password"
```

---

## Expected Results

### Successful Test
```
🧪 Testing: oasis_health_check
   Description: Check OASIS API health status
   Args: {}
   ✅ Success (234ms)
   Result: { "status": "healthy", ... }
```

### Failed Test
```
🧪 Testing: oasis_get_avatar
   Description: Get avatar by ID
   Args: { "avatarId": "invalid-id" }
   ❌ Failed (123ms)
   Error: Avatar not found
```

---

## Troubleshooting

### Common Issues

1. **Authentication Required**
   - Some endpoints require authentication
   - Use `oasis_authenticate_avatar` first to get a JWT token

2. **Invalid Avatar ID**
   - Make sure you're using a valid UUID format
   - Avatar must exist in the system

3. **Network Errors**
   - Check `OASIS_API_URL` is correct
   - Verify API is running and accessible
   - For local development, SSL verification is disabled

4. **Provider Not Activated**
   - Some wallet operations require provider activation
   - Use provider registration/activation endpoints first

---

## Next Steps

1. **Run Basic Tests:** Start with health check and supported chains
2. **Test Avatar Operations:** Get avatar, search avatars
3. **Test Wallet Operations:** Get wallets, portfolio value
4. **Test NFT Operations:** Get NFTs, search NFTs
5. **Test Write Operations:** Create wallet, mint NFT (requires auth)

---

## Related Documentation

- `ENDPOINT_INVENTORY.md` - Complete list of all endpoints
- `ENDPOINT_TEST_RESULTS.md` - Previous test results
- `HOW_TO_USE_MCP.md` - How to use MCP with Cursor
- `QUICK_START.md` - Quick start guide
