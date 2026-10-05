# OASIS Provider Status Reference

**Last Updated:** December 2025  
**Source of Truth:** `ProviderType` enum in `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/ProviderType.cs`  
**Verification:** Cross-referenced with actual provider implementations in `Providers/` directory

---

## Provider Count Summary

- **Total Provider Types Defined:** 55 (excluding `None`, `All`, `Default`)
- **Provider Implementations Found:** 40+ (based on directory structure)
- **Active Status:** Depends on `OASIS_DNA.json` configuration

**Note:** A provider being defined in the enum does not mean it's implemented. Check the `Providers/` directory for actual implementations.

---

## All Provider Types (Defined in Enum)

### Blockchain Providers (40)

| Provider | Enum Name | Implementation Status | Notes |
|----------|-----------|----------------------|-------|
| Solana | `SolanaOASIS` | ✅ Implemented | Directory exists: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/` |
| Radix | `RadixOASIS` | ✅ Implemented | Directory exists |
| Arbitrum | `ArbitrumOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| Avalanche | `AvalancheOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| Base | `BaseOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| Monad | `MonadOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| Ethereum | `EthereumOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| Polygon | `PolygonOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| EOSIO | `EOSIOOASIS` | ✅ Implemented | Directory exists |
| Telos | `TelosOASIS` | ✅ Implemented | Directory exists, EOSIO-based |
| SEEDS | `SEEDSOASIS` | ✅ Implemented | Directory exists |
| Loom | `LoomOASIS` | ⚠️ Defined | No directory found |
| TON | `TONOASIS` | ✅ Implemented | Directory exists |
| Stellar | `StellarOASIS` | ⚠️ Defined | No directory found |
| BlockStack | `BlockStackOASIS` | ✅ Implemented | Directory exists |
| Hashgraph | `HashgraphOASIS` | ✅ Implemented | Directory exists (Hedera) |
| Elrond | `ElrondOASIS` | ✅ Implemented | Directory exists (MultiversX) |
| TRON | `TRONOASIS` | ✅ Implemented | Directory exists |
| Cosmos | `CosmosBlockChainOASIS` | ✅ Implemented | Directory exists |
| Rootstock | `RootstockOASIS` | ✅ Implemented | Directory exists, Bitcoin L2 |
| ChainLink | `ChainLinkOASIS` | ✅ Implemented | Directory exists (Oracle) |
| Cardano | `CardanoOASIS` | ⚠️ Defined | No directory found |
| Polkadot | `PolkadotOASIS` | ⚠️ Defined | No directory found |
| Bitcoin | `BitcoinOASIS` | ✅ Implemented | Directory exists |
| NEAR | `NEAROASIS` | ⚠️ Defined | No directory found |
| Sui | `SuiOASIS` | ⚠️ Defined | No directory found (only .csproj) |
| Starknet | `StarknetOASIS` | ✅ Implemented | Directory exists |
| Aptos | `AptosOASIS` | ✅ Implemented | Directory exists |
| Aztec | `AztecOASIS` | ✅ Implemented | Directory exists |
| Zcash | `ZcashOASIS` | ✅ Implemented | Directory exists |
| Miden | `MidenOASIS` | ✅ Implemented | Directory exists |
| Optimism | `OptimismOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| BNB Chain | `BNBChainOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| Fantom | `FantomOASIS` | ✅ Implemented | Directory exists, EVM-compatible |
| Moralis | `MoralisOASIS` | ✅ Implemented | Directory exists (Web3 API) |
| Web3Core | `Web3CoreOASIS` | ✅ Implemented | Directory exists (Universal Web3) |

### Storage/Network Providers (12)

| Provider | Enum Name | Implementation Status | Notes |
|----------|-----------|----------------------|-------|
| IPFS | `IPFSOASIS` | ✅ Implemented | Directory exists: `Providers/Storage/` or `Providers/Network/` |
| Pinata | `PinataOASIS` | ✅ Implemented | IPFS gateway, directory exists |
| Holochain | `HoloOASIS` | ✅ Implemented | Directory exists |
| MongoDB | `MongoDBOASIS` | ✅ Implemented | Directory exists |
| Neo4j | `Neo4jOASIS` | ✅ Implemented | Directory exists (Graph database) |
| SQLite | `SQLLiteDBOASIS` | ✅ Implemented | Directory exists |
| SQL Server | `SQLServerDBOASIS` | ⚠️ Defined | No directory found |
| Oracle DB | `OracleDBOASIS` | ⚠️ Defined | No directory found |
| Google Cloud | `GoogleCloudOASIS` | ✅ Implemented | Directory exists |
| Azure Storage | `AzureStorageOASIS` | ⚠️ Defined | No directory found |
| Azure Cosmos DB | `AzureCosmosDBOASIS` | ✅ Implemented | Directory exists |
| AWS | `AWSOASIS` | ✅ Implemented | Directory exists |

### Other/Network Providers (8)

| Provider | Enum Name | Implementation Status | Notes |
|----------|-----------|----------------------|-------|
| Urbit | `UrbitOASIS` | ⚠️ Defined | No directory found |
| ThreeFold | `ThreeFoldOASIS` | ✅ Implemented | Directory exists |
| PLAN | `PLANOASIS` | ✅ Implemented | Directory exists |
| HoloWeb | `HoloWebOASIS` | ⚠️ Defined | No directory found |
| SOLID | `SOLIDOASIS` | ✅ Implemented | Directory exists (Tim Berners-Lee's protocol) |
| ActivityPub | `ActivityPubOASIS` | ✅ Implemented | Directory exists (Mastodon protocol) |
| Scuttlebutt | `ScuttlebuttOASIS` | ✅ Implemented | Directory exists |
| Local File | `LocalFileOASIS` | ✅ Implemented | Directory exists |

### Map Providers (Not in Enum, but exist)

| Provider | Implementation Status | Notes |
|----------|----------------------|-------|
| Mapbox | ✅ Implemented | Directory: `Providers/Maps/NextGenSoftware.OASIS.API.Providers.MapboxOASIS/` |
| WRLD3D | ✅ Implemented | Directory: `Providers/Maps/NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS/` |
| GO Map | ✅ Implemented | Directory: `Providers/Maps/NextGenSoftware.OASIS.API.Providers.GOMapOASIS/` |

### Other Specialized Providers (Not in Enum, but exist)

| Provider | Implementation Status | Notes |
|----------|----------------------|-------|
| Cargo | ✅ Implemented | NFT marketplace protocol, Directory: `Providers/Other/NextGenSoftware.OASIS.API.Providers.CargoOASIS/` |
| ONION Protocol | ✅ Implemented | Directory: `Providers/Other/NextGenSoftware.OASIS.API.Providers.ONION-Protocol/` |
| Orion Protocol | ✅ Implemented | Directory: `Providers/Other/NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS/` |

---

## Provider Activation Status

**Important:** Provider activation depends on `OASIS_DNA.json` configuration. The following providers are configured in the default `OASIS_DNA.json`:

### Currently Configured in OASIS_DNA.json

Based on `OASIS_DNA.json` analysis:

1. **MongoDBOASIS** ✅
   - Configured with connection string
   - Primary database provider

2. **ArbitrumOASIS** ✅
   - Configured with Sepolia testnet
   - Chain ID: 421614
   - Contract address configured

3. **EthereumOASIS** ✅
   - Configured with Sepolia testnet
   - Chain ID: 11155111
   - Connection string: Infura

4. **SolanaOASIS** ✅
   - Configured with devnet
   - Wallet credentials configured

5. **PolygonOASIS** ✅
   - Configured with Amoy testnet
   - Connection string configured

6. **RootstockOASIS** ✅
   - Configured with testnet
   - Connection string configured

7. **TelosOASIS** ✅
   - Connection string configured

8. **SEEDSOASIS** ✅
   - Connection string configured

9. **PinataOASIS** ✅
   - API keys and JWT configured

10. **SQLLiteDBOASIS** ✅
    - Connection string configured

11. **Neo4jOASIS** ⚠️
    - Username configured but password empty

12. **IPFSOASIS** ⚠️
    - Configured as `null` (needs setup)

13. **HoloOASIS** ⚠️
    - Configured for localhost only

14. **AzureCosmosDBOASIS** ⚠️
    - Connection strings empty (needs credentials)

---

## HyperDrive Configuration

Based on `OASIS_DNA.json`:

```json
"AutoReplicationEnabled": false
"AutoFailOverEnabled": true
"AutoLoadBalanceEnabled": true
"AutoReplicationProviders": "MongoDBOASIS"
"AutoLoadBalanceProviders": "MongoDBOASIS"
"AutoFailOverProviders": "MongoDBOASIS, ArbitrumOASIS, EthereumOASIS"
```

**Current Configuration:**
- **Auto-Replication:** Disabled (only MongoDB configured)
- **Auto-Failover:** Enabled (MongoDB → Arbitrum → Ethereum)
- **Auto-Load Balancing:** Enabled (MongoDB only)

---

## Implementation vs Configuration

**Key Distinction:**

1. **Provider Defined:** Listed in `ProviderType` enum (60 providers)
2. **Provider Implemented:** Has implementation code in `Providers/` directory (40+ providers)
3. **Provider Configured:** Has configuration in `OASIS_DNA.json` (~12 providers)
4. **Provider Active:** Successfully registered and activated at runtime (varies)

**To check if a provider is actually active:**
- Check runtime logs during OASIS boot
- Query `/api/provider/get-all-providers` endpoint
- Check `ProviderManager.Instance.GetAllRegisteredProviders()`

---

## Provider Categories

Providers can be categorized by their `ProviderCategory`:

1. **Blockchain** - Cryptocurrency and smart contract platforms
2. **Storage** - Databases and file storage systems
3. **Network** - Distributed networks and protocols
4. **Cloud** - Cloud service providers
5. **Other** - Specialized services

---

## Notes

- This status is based on code inspection as of December 2025
- Actual runtime status depends on:
  - OASIS_DNA.json configuration
  - Provider registration during boot
  - Successful activation
  - Network connectivity
  - Credentials validity

- **For accurate runtime status:** Query the API endpoint `/api/provider/get-all-providers` or check boot logs

---

**Verification Method:** Code inspection of:
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/ProviderType.cs`
- `Providers/` directory structure
- `OASIS_DNA.json` configuration

