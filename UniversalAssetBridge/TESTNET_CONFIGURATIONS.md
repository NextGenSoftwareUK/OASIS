# Testnet Configurations for All Chains

Date: November 4, 2025  
Purpose: Safe testing configurations for all 10 bridge chains

---

## Mainnet vs Testnet Status

### Current Bridge Services (Default to Mainnet):

All bridge services I created use mainnet RPC endpoints by default. For SAFE TESTING, you should use testnets!

---

## Testnet Configuration for Each Chain

### 1. Solana
**Mainnet:** https://api.mainnet-beta.solana.com  
**Testnet:** https://api.devnet.solana.com  
**Network:** Devnet  
**Faucet:** https://faucet.solana.com

### 2. Ethereum
**Mainnet:** https://mainnet.infura.io/v3/YOUR_KEY (Chain ID: 1)  
**Testnet:** https://sepolia.infura.io/v3/YOUR_KEY (Chain ID: 11155111)  
**Network:** Sepolia (recommended over Goerli)  
**Faucet:** https://sepoliafaucet.com

### 3. Polygon
**Mainnet:** https://polygon-rpc.com (Chain ID: 137)  
**Testnet:** https://rpc-mumbai.maticvigil.com (Chain ID: 80001)  
**Network:** Mumbai  
**Faucet:** https://faucet.polygon.technology

### 4. Base
**Mainnet:** https://mainnet.base.org (Chain ID: 8453)  
**Testnet:** https://sepolia.base.org (Chain ID: 84532)  
**Network:** Base Sepolia  
**Faucet:** https://www.coinbase.com/faucets/base-ethereum-goerli-faucet

### 5. Arbitrum
**Mainnet:** https://arb1.arbitrum.io/rpc (Chain ID: 42161)  
**Testnet:** https://sepolia-rollup.arbitrum.io/rpc (Chain ID: 421614)  
**Network:** Arbitrum Sepolia  
**Faucet:** https://faucet.quicknode.com/arbitrum/sepolia

### 6. Optimism
**Mainnet:** https://mainnet.optimism.io (Chain ID: 10)  
**Testnet:** https://sepolia.optimism.io (Chain ID: 11155420)  
**Network:** Optimism Sepolia  
**Faucet:** https://app.optimism.io/faucet

### 7. BNB Chain
**Mainnet:** https://bsc-dataseed.binance.org (Chain ID: 56)  
**Testnet:** https://data-seed-prebsc-1-s1.binance.org:8545 (Chain ID: 97)  
**Network:** BSC Testnet  
**Faucet:** https://testnet.bnbchain.org/faucet-smart

### 8. Avalanche
**Mainnet:** https://api.avax.network/ext/bc/C/rpc (Chain ID: 43114)  
**Testnet:** https://api.avax-test.network/ext/bc/C/rpc (Chain ID: 43113)  
**Network:** Fuji Testnet  
**Faucet:** https://faucet.avax.network

### 9. Fantom
**Mainnet:** https://rpcapi.fantom.network (Chain ID: 250)  
**Testnet:** https://rpc.testnet.fantom.network (Chain ID: 4002)  
**Network:** Fantom Testnet  
**Faucet:** https://faucet.fantom.network

### 10. Radix
**Mainnet:** https://mainnet.radixdlt.com  
**Testnet:** https://stokenet.radixdlt.com  
**Network:** Stokenet  
**Faucet:** https://stokenet-console.radixdlt.com

---

## How to Configure for Testing

### Option 1: Update Bridge Service Constructor (Recommended)

Modify each bridge service to accept a network parameter:

```csharp
public enum NetworkType
{
    Mainnet,
    Testnet
}

public EthereumBridgeService(
    Web3 web3, 
    Account technicalAccount, 
    NetworkType network = NetworkType.Testnet) // Default to testnet for safety
{
    _web3 = web3;
    _technicalAccount = technicalAccount;
    _hostUri = network == NetworkType.Mainnet 
        ? "https://mainnet.infura.io/v3/" 
        : "https://sepolia.infura.io/v3/";
}
```

### Option 2: Environment Variable

Set in OASIS_DNA.json or environment:

```json
{
  "Ethereum": {
    "Network": "Sepolia",
    "RpcUrl": "https://sepolia.infura.io/v3/YOUR_KEY",
    "ChainId": 11155111
  }
}
```

### Option 3: Configuration at Initialization

Pass testnet URL when creating provider:

```csharp
var ethereumProvider = new EthereumOASIS(
    hostUri: "https://sepolia.infura.io/v3/YOUR_KEY", // Testnet
    chainPrivateKey: "YOUR_KEY",
    chainId: 11155111, // Sepolia chain ID
    contractAddress: "YOUR_CONTRACT"
);
```

---

## Recommended: Add Network Parameter to Bridge Services

Let me create updated versions that support both mainnet and testnet:

### Updated Constructor Pattern:

```csharp
public class EthereumBridgeService : IEthereumBridgeService
{
    private readonly bool _useTestnet;
    
    public EthereumBridgeService(
        Web3 web3, 
        Account technicalAccount, 
        bool useTestnet = true) // Default to testnet for safety
    {
        _web3 = web3;
        _technicalAccount = technicalAccount;
        _useTestnet = useTestnet;
        
        // Set RPC based on network
        _hostUri = useTestnet 
            ? "https://sepolia.infura.io/v3/YOUR_KEY"
            : "https://mainnet.infura.io/v3/YOUR_KEY";
    }
}
```

---

## Current Status (What Needs Updating)

### Bridge Services Created (Use Mainnet by Default):
❌ EthereumBridgeService - Mainnet only  
❌ PolygonBridgeService - Mainnet only  
❌ BaseBridgeService - Mainnet only  
❌ OptimismBridgeService - Mainnet only  
❌ BNBChainBridgeService - Mainnet only  
❌ AvalancheBridgeService - Mainnet only  
❌ FantomBridgeService - Mainnet only  

### Pre-existing (May Have Testnet):
✓ SolanaOASIS - Has devnet support  
? ArbitrumOASIS - Check if testnet configured  
? RadixOASIS - Uses Stokenet (testnet)

---

## Quick Fix: Add Testnet Support Now

### Approach 1: Update All Bridge Services (1-2 hours)

Add testnet parameter to each bridge service constructor.

### Approach 2: Use Provider-Level Configuration (30 min)

Pass testnet RPC when initializing providers:

```csharp
// For Ethereum testnet
var ethProvider = new EthereumOASIS(
    hostUri: "https://sepolia.infura.io/v3/YOUR_KEY",
    chainPrivateKey: "YOUR_KEY",
    chainId: 11155111, // Sepolia
    contractAddress: "YOUR_CONTRACT"
);

// Bridge service will use testnet automatically
var bridge = ethProvider.BridgeService;
```

### Approach 3: Frontend Configuration (Easiest)

Add network selector in frontend:

```typescript
export const networkConfig = {
  ETH: {
    mainnet: { rpc: "https://mainnet.infura...", chainId: 1 },
    testnet: { rpc: "https://sepolia.infura...", chainId: 11155111 }
  },
  MATIC: {
    mainnet: { rpc: "https://polygon-rpc.com", chainId: 137 },
    testnet: { rpc: "https://rpc-mumbai...", chainId: 80001 }
  },
  // etc.
};
```

---

## Recommendation

For NOW (quick testing):
- Use approach 2: Pass testnet RPC when initializing providers
- Each provider accepts `hostUri` parameter
- Just use testnet URLs

For PRODUCTION (proper solution):
- Add network parameter to all bridge services
- Support both mainnet and testnet
- Configurable via OASIS_DNA.json

---

## What Do You Want?

1. **Keep it simple** - Just pass testnet URLs when testing (works now)
2. **Add testnet parameter** - I'll update all 7 bridge services (1-2 hours)
3. **Full configuration system** - Environment-based network selection (3-4 hours)

Option 1 works immediately. Options 2-3 are more robust.

What's your preference?

