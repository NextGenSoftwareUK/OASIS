# Task 20: Configuration Example

## appsettings.json Configuration

```json
{
  "Blockchain": {
    "FundingRate": {
      "PrimaryProvider": "Solana",
      "EnabledProviders": ["Solana"],
      "PublishToAllChains": false,
      "PublishIntervalMinutes": 60,
      "TrackedSymbols": ["AAPL", "MSFT", "GOOGL", "TSLA", "NVDA"]
    },
    "Solana": {
      "RpcUrl": "https://api.devnet.solana.com",
      "PrivateKey": "[BASE64_ENCODED_OR_HEX_PRIVATE_KEY]",
      "PublicKey": "[PUBLIC_KEY]",
      "FundingRateProgramId": "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS",
      "Network": "devnet"
    }
  },
  "SolanaTechnicalAccountBridgeOptions": {
    "HostUri": "https://api.devnet.solana.com",
    "PrivateKey": "[ALTERNATE_CONFIG_LOCATION]",
    "PublicKey": "[ALTERNATE_CONFIG_LOCATION]"
  }
}
```

## Configuration Notes

- **PrivateKey**: Can be base64 encoded or hex string
- **PublicKey**: Standard Solana public key string
- **RpcUrl**: Solana RPC endpoint (devnet/mainnet)
- **FundingRateProgramId**: Program ID after deployment (default is placeholder)
- **PublishIntervalMinutes**: How often to publish rates (default: 60 minutes = hourly)
- **TrackedSymbols**: List of symbols to publish funding rates for

## Deployment Checklist

1. ✅ Deploy Solana program to devnet/mainnet
2. ✅ Update `FundingRateProgramId` with deployed program ID
3. ✅ Configure wallet keys (private/public)
4. ✅ Set RPC URL for target network
5. ✅ Configure tracked symbols
6. ✅ Start application - worker will begin publishing

## Testing on Devnet

```bash
# 1. Build Anchor program
cd /Volumes/Storage/OASIS_CLEAN/RWA/oracle/programs/rwa-oracle
anchor build

# 2. Deploy to devnet
anchor deploy --provider.cluster devnet

# 3. Get program ID from deployment output
# 4. Update appsettings.json with program ID

# 5. Fund devnet wallet
solana airdrop 2 [WALLET_ADDRESS] --url devnet

# 6. Test initialization
# Use Solana CLI or create test script

# 7. Monitor logs for publishing
```

## Mainnet Configuration

For mainnet:
- Use mainnet RPC URL
- Use mainnet program ID
- Ensure wallet has SOL for transaction fees
- Monitor transaction costs (can add up with hourly updates)

