# Starknet Account Deployment Guide

## Overview

Starknet addresses are **contract addresses** that must be deployed to the network before they can be used. This guide explains how to deploy accounts programmatically in your cross-chain wallet.

## Quick Start

### Option 1: Using Starkli CLI (Recommended for Testing)

```bash
# 1. Install starkli
cargo install starkli

# 2. Initialize signer (stores private key)
starkli signer keystore new ./starknet-keys/my-account.json

# 3. Initialize account (creates account config)
starkli account oz init ./starknet-accounts/my-account.json \
  --signer ./starknet-keys/my-account.json \
  --network sepolia

# 4. Fund the precomputed address
# (Send ETH to the address shown in step 3)

# 5. Deploy the account
starkli account deploy ./starknet-accounts/my-account.json \
  --network sepolia
```

### Option 2: Using Python Script (Programmatic)

See `scripts/deploy-starknet-account.py` for a complete Python implementation.

### Option 3: Using C# / StarkSharp (Future Implementation)

The `StarknetAccountDeploymentHelper` class provides the foundation for programmatic deployment.

## Integration into Wallet Creation Flow

### Current Flow

1. **Generate Keys** → `KeyManager` generates private/public key pair
2. **Derive Address** → `AddressDerivationHelper.DeriveStarknetAddress()` computes address
3. **Store Wallet** → Wallet saved to database with address
4. **❌ Missing:** Account deployment

### Recommended Flow

1. **Generate Keys** → Generate private/public key pair
2. **Derive Address** → Compute Starknet address
3. **Check Deployment** → `StarknetAccountDeploymentHelper.IsAccountDeployedAsync()`
4. **Deploy if Needed** → `StarknetAccountDeploymentHelper.DeployAccountAsync()`
5. **Store Wallet** → Save deployed wallet to database

## Implementation Steps

### Step 1: Add Deployment Check to Wallet Creation

Update your wallet creation logic:

```csharp
// In your wallet creation service
public async Task<Wallet> CreateStarknetWalletAsync(string avatarId, string network = "testnet")
{
    // 1. Generate keys
    var keyPair = GenerateKeyPair();
    
    // 2. Derive address
    string address = AddressDerivationHelper.DeriveAddress(
        keyPair.PublicKey, 
        ProviderType.StarknetOASIS, 
        network
    );
    
    // 3. Check if already deployed
    bool isDeployed = await StarknetAccountDeploymentHelper.IsAccountDeployedAsync(
        address, 
        network
    );
    
    // 4. Deploy if not deployed
    if (!isDeployed)
    {
        // Option A: Auto-deploy (requires funding)
        // string txHash = await StarknetAccountDeploymentHelper.DeployAccountAsync(
        //     keyPair.PrivateKey,
        //     keyPair.PublicKey,
        //     network
        // );
        
        // Option B: Mark for manual deployment
        // Set a flag that account needs deployment
    }
    
    // 5. Create wallet object
    var wallet = new Wallet
    {
        Address = address,
        PublicKey = keyPair.PublicKey,
        PrivateKey = keyPair.PrivateKey, // Encrypted
        ProviderType = ProviderType.StarknetOASIS,
        Network = network,
        IsDeployed = isDeployed,
        NeedsDeployment = !isDeployed
    };
    
    return wallet;
}
```

### Step 2: Add Deployment Endpoint

Create an API endpoint for deploying accounts:

```csharp
[HttpPost("api/v1/starknet/wallets/{walletId}/deploy")]
public async Task<IActionResult> DeployStarknetAccount(string walletId, [FromBody] DeployAccountRequest request)
{
    var wallet = await GetWalletAsync(walletId);
    
    if (wallet.ProviderType != ProviderType.StarknetOASIS)
    {
        return BadRequest("Wallet is not a Starknet wallet");
    }
    
    // Check if already deployed
    bool isDeployed = await StarknetAccountDeploymentHelper.IsAccountDeployedAsync(
        wallet.Address,
        request.Network
    );
    
    if (isDeployed)
    {
        return Ok(new { message = "Account already deployed", address = wallet.Address });
    }
    
    // Deploy account
    try
    {
        string txHash = await StarknetAccountDeploymentHelper.DeployAccountAsync(
            wallet.PrivateKey, // Decrypt first
            wallet.PublicKey,
            request.Network,
            request.RpcUrl
        );
        
        // Update wallet status
        wallet.IsDeployed = true;
        wallet.DeploymentTxHash = txHash;
        await UpdateWalletAsync(wallet);
        
        return Ok(new { 
            transactionHash = txHash,
            address = wallet.Address,
            message = "Account deployment initiated"
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}
```

### Step 3: Add Deployment Status Tracking

Add fields to your Wallet model:

```csharp
public class Wallet
{
    // ... existing fields ...
    
    public bool IsDeployed { get; set; } = false;
    public bool NeedsDeployment { get; set; } = true;
    public string DeploymentTxHash { get; set; }
    public DateTime? DeployedAt { get; set; }
    public string DeploymentNetwork { get; set; }
}
```

## Deployment Methods

### Method 1: CLI-Based Deployment (Current)

**Pros:**
- ✅ Well-tested and reliable
- ✅ Good for manual operations
- ✅ Clear error messages

**Cons:**
- ❌ Requires external tool installation
- ❌ Not fully automated
- ❌ Hard to integrate into API

**Use Case:** Testing, manual deployments, one-off operations

### Method 2: Python Script (Recommended for Now)

**Pros:**
- ✅ Easy to integrate (call from C#)
- ✅ Full programmatic control
- ✅ Can be automated
- ✅ Good error handling

**Cons:**
- ❌ Requires Python environment
- ❌ Additional dependency

**Use Case:** Automated deployments, API integration

### Method 3: Direct RPC Calls (Future)

**Pros:**
- ✅ Pure C# implementation
- ✅ No external dependencies
- ✅ Full control

**Cons:**
- ❌ More complex to implement
- ❌ Need to handle all edge cases

**Use Case:** Production deployments, full automation

## Funding Accounts

Before deployment, accounts must be funded with ETH to cover gas fees.

### Funding Flow

1. **Compute Address** → Derive address from public key
2. **Display Funding Address** → Show user the address to fund
3. **Wait for Funding** → Poll balance or wait for user confirmation
4. **Deploy** → Once funded, deploy the account

### Funding Options

**Option A: Manual Funding**
- User sends ETH from another wallet
- System waits for funding confirmation
- Then deploys

**Option B: Automated Funding**
- System has a funding wallet
- Automatically funds new accounts
- Deploys immediately
- User reimburses later (or it's a service)

**Option C: Pre-funded Pool**
- Pre-deploy accounts with funding
- Assign to users on demand
- Faster user experience

## Testing

### Test Deployment on Sepolia Testnet

```bash
# 1. Get testnet ETH from faucet
# https://starknet-faucet.vercel.app/

# 2. Deploy test account
starkli account deploy ./starknet-accounts/test-account.json --network sepolia

# 3. Verify deployment
starkli account fetch ./starknet-accounts/test-account.json --network sepolia
```

### Verify Account is Deployed

```csharp
bool isDeployed = await StarknetAccountDeploymentHelper.IsAccountDeployedAsync(
    "0x...", // address
    "testnet"
);
```

## Cost Considerations

- **Deployment Fee:** ~0.001-0.01 ETH (varies by network)
- **Gas Price:** Dynamic, check current rates
- **Network:** Testnet is free, mainnet costs real ETH

## Security Considerations

1. **Private Keys:** Never store unencrypted private keys
2. **Deployment Keys:** Use separate keys for deployment operations
3. **Funding:** Don't leave large amounts in deployment wallets
4. **Rate Limiting:** Limit deployment requests to prevent abuse

## Next Steps

1. ✅ **Address Generation** - Complete (using Pedersen hash)
2. ⏳ **Deployment Helper** - Created (needs full implementation)
3. ⏳ **Integration** - Add to wallet creation flow
4. ⏳ **Testing** - Test on Sepolia testnet
5. ⏳ **Production** - Deploy to mainnet

## Resources

- **Starkli Documentation:** https://book.starkli.rs/
- **Starknet.py:** https://github.com/starknet-py/starknet.py
- **StarkSharp SDK:** https://github.com/project3fusion/StarkSharp
- **Starknet RPC Docs:** https://docs.starknet.io/documentation/architecture_and_concepts/Network_Architecture/rpc/

