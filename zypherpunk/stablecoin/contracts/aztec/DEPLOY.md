# Stablecoin Contract Deployment Guide

## Prerequisites

1. Aztec CLI installed and configured
2. Aztec account created and funded
3. Node URL configured (testnet or mainnet)

## Step 1: Compile Contract

```bash
cd /Volumes/Storage/OASIS_CLEAN/zypherpunk/stablecoin/contracts/aztec

# Compile the contract
aztec-nargo compile
```

If compilation fails due to dependency issues:

```bash
# Update Nargo.toml with correct Aztec version
# Check latest version at: https://github.com/AztecProtocol/aztec-packages
```

## Step 2: Deploy Contract

```bash
# Set environment variables
export AZTEC_NODE_URL=https://aztec-testnet-fullnode.zkv.xyz
export AZTEC_ACCOUNT=accounts:your_account_name

# Deploy
aztec-wallet deploy \
    --node-url $AZTEC_NODE_URL \
    --from $AZTEC_ACCOUNT \
    --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
    --alias stablecoin \
    target/stablecoin_contract-StablecoinContract.json
```

## Step 3: Save Contract Address

After deployment, save the contract address:

```bash
# The deployment will output a contract address
# Save it to a file or environment variable
echo "STABLECOIN_CONTRACT_ADDRESS=0x..." > .env.contract
```

## Step 4: Update OASIS Configuration

Update the contract address in the codebase:

1. **AztecService.cs**:
```csharp
private const string STABLECOIN_CONTRACT_ADDRESS = "0x..."; // Your deployed address
```

2. **StablecoinManager.cs**:
```csharp
// Update any contract address references
```

3. **Environment Variables**:
```bash
export STABLECOIN_CONTRACT_ADDRESS=0x...
```

## Step 5: Initialize Contract

After deployment, initialize the contract:

1. Set initial oracle price
2. Configure collateral ratios (if needed)
3. Set up oracle access control

## Step 6: Test Deployment

Test the contract with a simple call:

```bash
# Get total supply (should be 0 initially)
aztec-wallet call \
    --node-url $AZTEC_NODE_URL \
    --from $AZTEC_ACCOUNT \
    --contract stablecoin \
    --function get_total_supply
```

## Troubleshooting

### Compilation Errors

If you get dependency errors:
1. Check Aztec version compatibility
2. Update `Nargo.toml` with correct version
3. Clear nargo cache: `rm -rf ~/.nargo`

### Deployment Errors

If deployment fails:
1. Check account has funds
2. Verify node URL is correct
3. Check network connectivity
4. Verify contract compiled successfully

### Contract Call Errors

If contract calls fail:
1. Verify contract address is correct
2. Check function signatures match
3. Verify account has permissions
4. Check contract is deployed and active

## Production Deployment

For mainnet deployment:

1. **Security Audit**: Get contract audited before mainnet
2. **Access Control**: Implement proper oracle access control
3. **Upgrade Path**: Consider upgradeable contract pattern
4. **Monitoring**: Set up monitoring and alerts
5. **Backup**: Save all deployment artifacts

## Next Steps

After successful deployment:
1. Update OASIS service to use contract address
2. Test mint/redeem operations
3. Set up oracle price updates
4. Monitor contract activity
5. Test liquidation flow

---

**Status**: Ready for Deployment ⏳  
**Note**: Test on testnet first before mainnet deployment

