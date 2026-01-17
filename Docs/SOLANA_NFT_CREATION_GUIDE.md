# Solana NFT Creation Guide

This guide explains how to create (mint) Solana NFTs using the OASIS API.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Minting NFTs](#minting-nfts)
- [Automatic Wallet Sending](#automatic-wallet-sending)
- [API Endpoint](#api-endpoint)
- [Request Parameters](#request-parameters)
- [Response Format](#response-format)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### 1. OASIS Account Setup
- The OASIS Solana account must be configured in `OASIS_DNA.json`
- The account must have sufficient SOL balance (recommended: at least 0.1 SOL on devnet, 0.5 SOL on mainnet)
- The account is used to pay for:
  - Transaction fees
  - Rent for mint account creation
  - Rent for metadata account creation
  - Rent for master edition account creation

### 2. SolanaOASIS Provider Activation
The SolanaOASIS provider must be registered and activated:

```bash
# Authenticate
TOKEN=$(curl -L -k -s -X POST "http://localhost:5003/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "YOUR_PASSWORD"}' | \
  python3 -c "import sys, json; d=json.load(sys.stdin); print(d.get('result', {}).get('result', {}).get('jwtToken', ''))")

# Activate provider
curl -k -s -X POST "https://localhost:5004/api/provider/activate-provider/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

### 3. Avatar Wallet Linking
For automatic NFT sending, your avatar should have a Solana wallet linked. The system will automatically:
- Find your avatar's Solana wallet address
- Send the minted NFT to that wallet

## Configuration

### OASIS_DNA.json Setup

The SolanaOASIS provider configuration in `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "StorageProviders": {
      "SolanaOASIS": {
        "PrivateKey": "YOUR_PRIVATE_KEY_BASE58_OR_BASE64",
        "PublicKey": "YOUR_PUBLIC_KEY_BASE58",
        "ConnectionString": "https://api.devnet.solana.com"
      }
    }
  }
}
```

**Key Format Support:**
- **Base58 format**: Direct Base58 encoded key (preferred)
- **Base64 format**: Base64 encoded key (automatically detected and converted)

The system automatically detects Base64 keys (by checking for `+`, `/`, or `=`) and converts them to Base58 using `Cryptography.ECDSA.Base58.Encode`, matching the test harness approach.

## Minting NFTs

### API Endpoint

```
POST https://localhost:5004/api/nft/mint-nft
```

### Authentication

Include the JWT token in the Authorization header:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

### Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Title` | string | Yes | The title/name of the NFT |
| `JSONMetaDataURL` | string | Yes | URL to the JSON metadata file |
| `Symbol` | string | Yes | Symbol/ticker for the NFT collection |
| `OnChainProvider` | string | Yes | Must be `"SolanaOASIS"` |
| `OffChainProvider` | string | No | Default: `"MongoDBOASIS"` |
| `NFTOffChainMetaType` | string | No | Default: `"OASIS"` |
| `NFTStandardType` | string | Yes | Must be `"SPL"` for Solana |
| `NumberToMint` | number | No | Default: `1` |
| `WaitTillNFTMinted` | boolean | No | Default: `false` |
| `SendToAvatarAfterMintingId` | string | No | Avatar ID to send NFT to (auto-detected if not provided) |
| `MetaData` | object | No | Additional metadata object |

### Request Body Example

```json
{
  "Title": "My First NFT",
  "JSONMetaDataURL": "https://example.com/metadata.json",
  "Symbol": "MYNFT",
  "OnChainProvider": "SolanaOASIS",
  "OffChainProvider": "MongoDBOASIS",
  "NFTOffChainMetaType": "OASIS",
  "NFTStandardType": "SPL",
  "NumberToMint": 1,
  "WaitTillNFTMinted": false,
  "MetaData": {
    "description": "My first Solana NFT",
    "image": "https://example.com/image.png",
    "attributes": [
      {"trait_type": "Color", "value": "Blue"},
      {"trait_type": "Rarity", "value": "Common"}
    ]
  }
}
```

## Automatic Wallet Sending

The OASIS API automatically sends minted NFTs to your avatar's linked Solana wallet:

1. **If `SendToAvatarAfterMintingId` is provided**: Sends to that avatar's Solana wallet
2. **If not provided**: Automatically sends to the authenticated avatar's Solana wallet
3. **Wallet lookup**: The system finds the avatar's Solana wallet from `ProviderWallets[SolanaOASIS]`

### How It Works

1. NFT is minted using the OASIS Solana account (`7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q`)
2. System looks up the destination avatar's Solana wallet address
3. NFT is automatically transferred to that wallet address
4. Transaction hash is returned in the response

## Response Format

### Success Response

```json
{
  "isError": false,
  "message": "Successfully minted the OASIS NFT containing 1 Web NFT(s)|Successfully minted the Web3 NFT on the SolanaOASIS provider with hash TRANSACTION_HASH...",
  "result": {
    "web3NFTs": [
      {
        "nftTokenAddress": "BpCkBoj1BubUVEZHvaQuSXnQmzBZN3rbZ6gTk53RB3mq",
        "mintTransactionHash": "5uGsW5Wswcke612441uzmaUoqD6w3UzVhEzCCEjWXtAvwk6GoTQJXK11R6jxhipLcbZ4jMWyFn8Vwec2JyVAHAWd",
        "sendNFTTransactionHash": "27HWQEXwba9u9T6cRTAUwPt6Suaj3ggsNsG3C9TeRoDTAUBVcw3ZyM5BDPGEUmnRaspUZbMq8Uym41y5LsyquMzv",
        "sendToAddressAfterMinting": "8bFhmkao9SJ6axVNcNRoeo85aNG45HP94oMtnQKGSUuz",
        "oasisMintWalletAddress": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q"
      }
    ]
  }
}
```

### Response Fields

- `nftTokenAddress`: The Solana token address (mint address) of the NFT
- `mintTransactionHash`: Transaction hash for the minting operation
- `sendNFTTransactionHash`: Transaction hash for sending NFT to destination wallet
- `sendToAddressAfterMinting`: The wallet address the NFT was sent to
- `oasisMintWalletAddress`: The OASIS account used for minting

## Examples

### Example 1: Basic NFT Minting

```bash
TOKEN=$(curl -L -k -s -X POST "http://localhost:5003/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "YOUR_PASSWORD"}' | \
  python3 -c "import sys, json; d=json.load(sys.stdin); print(d.get('result', {}).get('result', {}).get('jwtToken', ''))")

curl -k -s -X POST "https://localhost:5004/api/nft/mint-nft" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Title": "My First NFT",
    "JSONMetaDataURL": "https://jsonplaceholder.typicode.com/posts/1",
    "Symbol": "MYNFT",
    "OnChainProvider": "SolanaOASIS",
    "NFTStandardType": "SPL"
  }'
```

### Example 2: NFT with Custom Metadata

```bash
curl -k -s -X POST "https://localhost:5004/api/nft/mint-nft" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Title": "Rare Artwork #1",
    "JSONMetaDataURL": "https://example.com/metadata/artwork1.json",
    "Symbol": "ART",
    "OnChainProvider": "SolanaOASIS",
    "NFTStandardType": "SPL",
    "NumberToMint": 1,
    "MetaData": {
      "description": "A rare digital artwork",
      "image": "https://example.com/images/artwork1.png",
      "attributes": [
        {"trait_type": "Artist", "value": "John Doe"},
        {"trait_type": "Year", "value": "2024"},
        {"trait_type": "Rarity", "value": "Legendary"}
      ]
    }
  }'
```

### Example 3: Send to Specific Avatar

```bash
curl -k -s -X POST "https://localhost:5004/api/nft/mint-nft" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Title": "Gift NFT",
    "JSONMetaDataURL": "https://example.com/metadata/gift.json",
    "Symbol": "GIFT",
    "OnChainProvider": "SolanaOASIS",
    "NFTStandardType": "SPL",
    "SendToAvatarAfterMintingId": "0df19747-fa32-4c2f-a6b8-b55ed76d04af"
  }'
```

## How It Works Internally

### 1. Provider Initialization
- SolanaOASIS provider reads `PrivateKey` and `PublicKey` from `OASIS_DNA.json`
- Detects if key is Base64 or Base58 format
- Converts Base64 → bytes → Base58 (if needed) using `Cryptography.ECDSA.Base58.Encode`
- Creates `PrivateKey` and `PublicKey` objects
- Creates `Account` using `PrivateKey.Key` and `PublicKey.Key`

### 2. NFT Minting Process
1. **Create Mint Account**: A new `Account()` is created for each NFT (unique mint address)
2. **Create Metadata**: Metadata object is created with title, symbol, URI, and creators
3. **Call CreateNFT**: `MetadataClient.CreateNFT()` is called with:
   - `ownerAccount`: The OASIS Solana account (pays fees and owns NFT)
   - `mintAccount`: New account for this specific NFT
   - `TokenStandard.NonFungible`
   - `isMasterEdition: true`
   - `isMutable: true`
4. **CreateNFT handles**:
   - Creating the mint account on-chain
   - Creating metadata account
   - Creating master edition account
   - Minting the NFT token

### 3. Automatic Sending
- After successful minting, the system looks up the destination avatar's Solana wallet
- Creates associated token account if needed
- Transfers the NFT to the destination wallet
- Returns both mint and send transaction hashes

## Troubleshooting

### Error: "Provider passed in is null!"
**Cause**: SolanaOASIS provider failed to initialize (usually key format issue)

**Solution**:
- Check that `PrivateKey` and `PublicKey` in `OASIS_DNA.json` are valid
- Ensure Base64 keys are properly formatted (if using Base64)
- Check provider activation logs for specific errors

### Error: "Transaction simulation failed: Attempt to debit an account but found no record of a prior credit"
**Cause**: OASIS Solana account has insufficient SOL balance (0 or too low)

**Solution**:
- Fund the OASIS account with SOL (devnet faucet or transfer)
- Check balance: `curl -s "https://api.devnet.solana.com" -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","id":1,"method":"getBalance","params":["YOUR_PUBLIC_KEY"]}'`
- Recommended minimum: 0.1 SOL on devnet, 0.5 SOL on mainnet

### Error: "Invalid base58 data"
**Cause**: PrivateKey format issue (should be resolved with Base64 detection)

**Solution**:
- Ensure key is either valid Base58 or Base64
- Base64 keys are automatically detected and converted
- Check constructor logs for key processing details

### Error: "Transaction failed to sanitize accounts offsets correctly"
**Cause**: Using same account for both owner and mint (should be resolved)

**Solution**:
- This is fixed - each NFT now uses a new `Account()` for the mint
- Ensure you're using the latest code

### Error: "No wallet was found for avatar"
**Cause**: Avatar doesn't have a Solana wallet linked

**Solution**:
- Link a Solana wallet to the avatar using the Wallet API or Key API
- The wallet must be of type `SolanaOASIS`

## Best Practices

1. **Always check OASIS account balance** before minting multiple NFTs
2. **Use meaningful metadata URLs** that are publicly accessible
3. **Link Solana wallets to avatars** for automatic NFT delivery
4. **Monitor transaction hashes** to verify on-chain minting
5. **Use devnet for testing** before moving to mainnet

## Network Configuration

### Devnet (Default)
```
ConnectionString: "https://api.devnet.solana.com"
```
- Free SOL from faucets
- For testing only
- Faster transaction confirmation

### Mainnet
```
ConnectionString: "https://api.mainnet-beta.solana.com"
```
- Requires real SOL
- Permanent transactions
- Slower confirmation times

### Custom RPC (Helius)
```
ConnectionString: "https://devnet.helius-rpc.com/?api-key=YOUR_API_KEY"
```
- Better rate limits
- Enhanced features
- Requires API key

## Related Documentation

- [Solana Wallet Creation Guide](./SOLANA_WALLET_CREATION_GUIDE.md)
- [NFT Minting Workflow](./NFT_MINTING_WORKFLOW.md)
- [Agent NFT Linking Guide](./NFT_AGENT_LINKING_COMPLETE_GUIDE.md)

## Support

For issues or questions:
1. Check provider activation logs
2. Verify OASIS account balance
3. Ensure avatar has linked Solana wallet
4. Review transaction hashes on Solana explorer

---

**Last Updated**: January 17, 2026  
**Version**: 1.0
