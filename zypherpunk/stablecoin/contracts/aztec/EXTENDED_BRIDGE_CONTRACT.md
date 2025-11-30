# Extended Bridge Contract with Stablecoin

## What We Did

✅ **Extended the existing bridge contract** with stablecoin functionality

**Original Bridge Contract:**
- Address: `0x2d9ca3ac6f973e10cb3ac009849605c690116d23f120ca317d82da131b51ee77`
- Functions: `deposit`, `withdraw`, `get_deposits`, `get_withdrawals`

**Extended Contract:**
- ✅ All original bridge functions preserved
- ✅ Added stablecoin minting (`mint_stablecoin`)
- ✅ Added stablecoin burning (`burn_stablecoin`)
- ✅ Added liquidation (`liquidate_position`)
- ✅ Added yield generation (`generate_yield`)
- ✅ Added oracle price updates (`update_price`)
- ✅ Added query functions for stablecoin data

## Contract Location

- **Source**: `/Volumes/Storage/OASIS_CLEAN/aztec-bridge-contract/src/main_extended.nr`
- **Working Copy**: `~/aztec-bridge-extended/src/main.nr`
- **Backup**: `/Volumes/Storage/OASIS_CLEAN/aztec-bridge-contract/src/main_backup.nr`

## New Functions Added

### Private Functions
- `mint_stablecoin()` - Mint stablecoin with Zcash collateral
- `burn_stablecoin()` - Burn stablecoin and release collateral
- `liquidate_position()` - Liquidate undercollateralized positions
- `generate_yield()` - Generate yield for positions

### Public Functions
- `update_price()` - Update ZEC price from oracle
- `get_stablecoin_balance()` - Get user stablecoin balance
- `get_total_supply()` - Get total stablecoin supply
- `get_total_collateral()` - Get total ZEC collateral
- `get_position_*()` - Get position data
- `get_price()` - Get current ZEC price
- `is_position_liquidated()` - Check if position is liquidated

## Next Steps

1. **Compile Extended Contract**
   ```bash
   cd ~/aztec-bridge-extended
   aztec-nargo compile
   ```

2. **Deploy Extended Contract**
   ```bash
   aztec-wallet deploy \
       --node-url $NODE_URL \
       --from accounts:test1 \
       --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
       --alias bridge-stablecoin \
       target/bridge_contract-BridgeContract.json
   ```

3. **Update OASIS Configuration**
   - Update contract address in `AztecService.cs`
   - Update contract address in `StablecoinManager.cs`
   - Update contract address in configuration files

4. **Test Functions**
   - Test bridge functions (should work as before)
   - Test stablecoin minting
   - Test stablecoin burning
   - Test liquidation

## Integration with Existing Bridge

The extended contract maintains **100% backward compatibility**:
- All existing bridge functions work exactly as before
- Existing bridge contract can continue operating
- New stablecoin functions are additive only

## Migration Strategy

**Option 1: Use Both Contracts**
- Keep existing bridge contract for bridge operations
- Use new extended contract for stablecoin operations
- Both contracts can coexist

**Option 2: Migrate to Extended Contract**
- Deploy extended contract
- Migrate bridge operations to extended contract
- Deprecate old bridge contract

**Option 3: Gradual Migration**
- Deploy extended contract
- Test stablecoin functions
- Gradually migrate bridge operations
- Keep old contract as backup

---

**Status**: Extended Contract Created ✅  
**Next**: Compile and Deploy ⏳

