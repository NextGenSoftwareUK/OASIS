# Aztec Stablecoin Contract

## Overview

This is the Aztec smart contract for the Zcash-backed stablecoin. It implements:
- Private minting with Zcash collateral
- Private redemption
- Automatic liquidation
- Yield generation
- Position management

## Contract Structure

### Main Functions

#### Private Functions
- `mint()` - Mint stablecoin with Zcash collateral
- `burn()` - Burn stablecoin and release collateral
- `liquidate()` - Liquidate undercollateralized positions
- `generate_yield()` - Generate yield for positions

#### Public Functions
- `update_price()` - Update ZEC price from oracle
- `get_position_collateral()` - Get position collateral
- `get_position_debt()` - Get position debt
- `get_balance()` - Get user balance
- `get_total_supply()` - Get total supply
- `get_price()` - Get current price

## Storage

### System State
- `total_supply` - Total stablecoin minted
- `total_collateral` - Total ZEC locked
- `collateral_ratio` - Required collateral ratio (150% = 150)
- `liquidation_threshold` - Liquidation threshold (120% = 120)
- `oracle_price` - Current ZEC price (scaled by 1e6)

### Position Storage
- `positions_collateral` - Map of position_id -> collateral_amount
- `positions_debt` - Map of position_id -> debt_amount
- `positions_owner` - Map of position_id -> owner_address
- `positions_zcash_tx` - Map of position_id -> zcash_tx_hash
- `positions_viewing_key` - Map of position_id -> viewing_key_hash
- `positions_yield` - Map of position_id -> yield_earned
- `positions_liquidated` - Map of position_id -> is_liquidated

### User Storage
- `balances` - Map of user_address -> balance

## Compilation

```bash
cd contracts/aztec
aztec-nargo compile
```

## Deployment

```bash
aztec-wallet deploy \
    --node-url $AZTEC_NODE_URL \
    --from accounts:your_account \
    --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
    --alias stablecoin \
    target/stablecoin_contract-StablecoinContract.json
```

## Configuration

After deployment, update the contract address in:
- `StablecoinManager.cs` - Set `STABLECOIN_CONTRACT_ADDRESS`
- `AztecService.cs` - Use contract address for calls

## Security Notes

1. **Oracle Access Control**: The `update_price()` function should be restricted to authorized oracles only. Currently, it's public for testing.

2. **Liquidation Access**: The `liquidate()` function can be called by anyone when a position is undercollateralized. This is intentional for decentralized liquidation.

3. **Viewing Keys**: Viewing keys are stored as hashes for privacy. The actual keys are stored in OASIS holons.

4. **Price Precision**: Prices are scaled by 1e6 to maintain precision in integer arithmetic.

5. **Amount Scaling**: 
   - ZEC amounts are scaled by 1e8 (satoshi-like precision)
   - Stablecoin amounts are scaled by 1e18 (wei-like precision)

## Testing

```bash
# Run tests (when test framework is set up)
aztec-nargo test
```

## Integration

The contract integrates with:
- **Zcash Provider**: For locking/releasing ZEC
- **Aztec Provider**: For calling contract functions
- **Oracle Service**: For price updates
- **OASIS Holons**: For storing position data and viewing keys

---

**Status**: Contract Code Complete ✅  
**Next**: Compilation & Deployment ⏳

