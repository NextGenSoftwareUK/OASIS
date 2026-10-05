# Stablecoin Smart Contracts

## Overview

This directory contains the smart contracts for the Zcash-backed stablecoin system.

## Contract Structure

```
contracts/
├── aztec/
│   ├── src/
│   │   └── main.nr          # Main stablecoin contract (Noir)
│   ├── Nargo.toml          # Contract configuration
│   ├── README.md           # Contract documentation
│   └── DEPLOY.md           # Deployment guide
└── README.md               # This file
```

## Aztec Contract

The main stablecoin contract is written in **Noir** (Aztec's ZK language) and provides:

### Features
- ✅ Private minting with Zcash collateral
- ✅ Private redemption
- ✅ Automatic liquidation
- ✅ Yield generation
- ✅ Position management
- ✅ Oracle price updates

### Functions

**Private Functions:**
- `mint()` - Mint stablecoin
- `burn()` - Burn stablecoin
- `liquidate()` - Liquidate position
- `generate_yield()` - Generate yield

**Public Functions:**
- `update_price()` - Update oracle price
- `get_position_*()` - Get position data
- `get_balance()` - Get user balance
- `get_total_supply()` - Get total supply

## Integration

The contract integrates with:
- **OASIS Backend**: Managers call contract functions
- **Zcash Provider**: Locks/releases ZEC
- **Aztec Provider**: Calls contract functions
- **Oracle Service**: Updates prices

## Development

### Compile
```bash
cd contracts/aztec
aztec-nargo compile
```

### Deploy
```bash
aztec-wallet deploy --node-url $NODE_URL --from $ACCOUNT target/stablecoin_contract-StablecoinContract.json
```

### Test
```bash
aztec-nargo test
```

## Security

- All operations are private (ZK proofs)
- Viewing keys stored as hashes
- Oracle access control (to be implemented)
- Decentralized liquidation

## Status

- ✅ Contract code complete
- ⏳ Compilation pending
- ⏳ Deployment pending
- ⏳ Testing pending

---

**Next**: Compile and deploy contract

