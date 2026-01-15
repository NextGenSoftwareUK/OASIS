# Solana Wallet MCP Endpoints

**Last Updated:** 2026-01-09  
**Status:** ✅ **READY TO USE**

---

## Overview

This document describes the MCP (Model Context Protocol) endpoint for creating Solana wallets for avatars. This endpoint works for both regular avatars and agent avatars, as the wallet creation process is identical for both. It follows the correct order specified in `SOLANA_WALLET_CREATION_GUIDE.md` to ensure reliable wallet creation.

---

## Available Endpoint

### `oasis_create_solana_wallet`

Creates a Solana wallet for an avatar. Works for both regular avatars and agent avatars.

**Parameters:**
- `avatarId` (string, required) - The avatar ID (UUID) to create the wallet for. Works for both regular avatars and agent avatars.
- `setAsDefault` (boolean, optional) - Whether to set this wallet as the default wallet (default: `true`)
- `ensureProviderActivated` (boolean, optional) - Whether to ensure Solana provider is registered and activated (default: `true`)

**Returns:**
```json
{
  "walletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
  "walletAddress": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
  "publicKey": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
  "providerType": "SolanaOASIS",
  "avatarId": "d42b8448-52a9-4579-a6b1-b7c624616459",
  "isDefaultWallet": true
}
```

**Example Usage:**
```typescript
// Via MCP - for regular avatar
{
  "tool": "oasis_create_solana_wallet",
  "arguments": {
    "avatarId": "d42b8448-52a9-4579-a6b1-b7c624616459",
    "setAsDefault": true
  }
}

// Via MCP - for agent avatar (same endpoint!)
{
  "tool": "oasis_create_solana_wallet",
  "arguments": {
    "avatarId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "setAsDefault": true
  }
}
```

---

## Implementation Details

### Correct Order (CRITICAL)

The implementation follows the correct order for Solana wallet creation:

1. **Ensure Provider Activated** (if `ensureProviderActivated` is true)
   - Registers Solana provider (if not already registered)
   - Activates Solana provider (if not already activated)

2. **Generate Keypair**
   - Calls `/api/keys/generate_keypair_with_wallet_address_for_provider/SolanaOASIS`
   - Returns base64-encoded private key and base58 public key/address

3. **Link Public Key FIRST** (creates wallet)
   - Calls `/api/keys/link_provider_public_key_to_avatar_by_id`
   - Creates the wallet and returns wallet ID
   - **This must happen before linking the private key**

4. **Link Private Key SECOND** (completes wallet)
   - Calls `/api/keys/link_provider_private_key_to_avatar_by_id`
   - Uses the wallet ID from step 3
   - Completes the wallet setup

5. **Set as Default** (if `setAsDefault` is true)
   - Calls `/api/wallet/avatar/{avatarId}/default-wallet/{walletId}`

### Why This Order Matters

For Solana and other non-Bitcoin providers, the private key linking method cannot derive the wallet address from the private key. By linking the public key first, we create the wallet with the correct address from keypair generation, then link the private key to that existing wallet.

---

## Integration

### TypeScript/JavaScript MCP Server

```typescript
import { SolanaWalletMCPTools, solanaWalletMCPToolDefinitions } from './solana-wallet-tools';
import { Server } from '@modelcontextprotocol/sdk/server/index.js';

// Initialize the tools
const solanaTools = new SolanaWalletMCPTools(
  process.env.OASIS_API_URL || 'https://api.oasisweb4.com',
  async () => {
    // Your token retrieval logic
    // This should return a valid JWT token for OASIS API
    return await getAuthToken();
  }
);

// Register tools with MCP server
const server = new Server({
  name: 'oasis-solana-wallet',
  version: '1.0.0',
}, {
  capabilities: {
    tools: {},
  },
});

// List available tools
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: solanaWalletMCPToolDefinitions,
  };
});

// Handle tool calls
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

    try {
      switch (name) {
        case 'oasis_create_solana_wallet':
          const result = await solanaTools.createSolanaWallet({
            avatarId: args.avatarId,
            setAsDefault: args.setAsDefault ?? true,
            ensureProviderActivated: args.ensureProviderActivated ?? true,
          });
          return {
            content: [
              {
                type: 'text',
                text: JSON.stringify(result, null, 2),
              },
            ],
          };

        default:
          throw new Error(`Unknown tool: ${name}`);
      }
  } catch (error: any) {
    return {
      content: [
        {
          type: 'text',
          text: `Error: ${error.message}`,
        },
      ],
      isError: true,
    };
  }
});
```

---

## Error Handling

The endpoints handle common errors gracefully:

- **Provider already registered/activated**: Silently continues (no error)
- **Invalid avatar ID**: Returns error with descriptive message
- **Network errors**: Returns error with details
- **API errors**: Returns error with OASIS API error message

---

## Authentication

The endpoints require authentication via Bearer token. The token should be retrieved using the `getAuthToken` function passed to the `SolanaWalletMCPTools` constructor.

**Example token retrieval:**
```typescript
async function getAuthToken(): Promise<string> {
  // Option 1: From environment variable
  if (process.env.OASIS_JWT_TOKEN) {
    return process.env.OASIS_JWT_TOKEN;
  }

  // Option 2: From cache/storage
  const cachedToken = await getCachedToken();
  if (cachedToken && !isTokenExpired(cachedToken)) {
    return cachedToken;
  }

  // Option 3: Authenticate and get new token
  const response = await axios.post('https://api.oasisweb4.com/api/avatar/authenticate', {
    username: process.env.OASIS_USERNAME,
    password: process.env.OASIS_PASSWORD,
  });
  
  const token = response.data.result?.token || response.data.token;
  await cacheToken(token);
  return token;
}
```

---

## Testing

### Test with Cursor/Claude Desktop

1. Ensure your MCP server is configured in `~/.cursor/mcp.json` or `%APPDATA%\Cursor\mcp.json`
2. Restart Cursor/Claude Desktop
3. Use the tools in conversation:

```
Create a Solana wallet for avatar d42b8448-52a9-4579-a6b1-b7c624616459
```

### Manual Testing

```bash
# Test via curl (requires authentication)
curl -X POST "https://api.oasisweb4.com/api/keys/generate_keypair_with_wallet_address_for_provider/SolanaOASIS" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Then link keys following the guide...
```

---

## Related Documentation

- `SOLANA_WALLET_CREATION_GUIDE.md` - Complete guide on Solana wallet creation workflow
- `solana-wallet-tools.ts` - Implementation source code
- OASIS API Documentation - For underlying API endpoints

---

## Summary

✅ **One unified MCP endpoint:**
- `oasis_create_solana_wallet` - Works for both regular avatars and agent avatars

✅ **Follows correct order:**
- Public key linked first (creates wallet)
- Private key linked second (completes wallet)

✅ **Automatic provider management:**
- Registers and activates Solana provider if needed

✅ **Easy to integrate:**
- Simple TypeScript implementation
- Clear error handling
- Flexible authentication
