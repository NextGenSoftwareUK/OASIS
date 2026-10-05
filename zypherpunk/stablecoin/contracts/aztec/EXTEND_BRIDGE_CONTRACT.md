# Extending the Deployed Bridge Contract

## Current Situation

✅ **Bridge Contract Deployed**: `0x2d9ca3ac6f973e10cb3ac009849605c690116d23f120ca317d82da131b51ee77`

**Current Functions:**
- `deposit(owner, amount)` - Private
- `withdraw(destination, amount)` - Private
- `get_deposits(user)` - Public
- `get_withdrawals(user)` - Public

## Options for Adding Stablecoin Functionality

### Option 1: Extend Bridge Contract (Recommended)

Add stablecoin functions to the existing bridge contract and redeploy as a new version:

**Advantages:**
- Single contract for both bridge and stablecoin
- Can reuse existing deposit/withdraw logic
- Simpler integration

**Steps:**
1. Add stablecoin functions to `~/aztec-bridge-contract/src/main.nr`
2. Compile the extended contract
3. Deploy as new contract (or upgrade if supported)
4. Update contract address in OASIS config

### Option 2: Create Separate Stablecoin Contract

Create a new contract that uses the bridge contract:

**Advantages:**
- Doesn't affect existing bridge functionality
- Can be deployed independently
- Easier to maintain separately

**Steps:**
1. Create new stablecoin contract
2. Import/call bridge contract functions when needed
3. Deploy new contract
4. Both contracts work together

### Option 3: Use Bridge Contract As-Is

Integrate stablecoin logic in OASIS backend, using bridge contract for deposits/withdrawals:

**Advantages:**
- No new contract deployment needed
- Faster to implement
- Uses existing infrastructure

**Limitations:**
- Stablecoin logic in backend, not on-chain
- Less decentralized

## Recommended Approach: Option 1

Extend the bridge contract with stablecoin functions. This gives us:
- On-chain stablecoin logic
- Integration with existing bridge
- Single contract to manage

---

**Next**: Let's extend the bridge contract with stablecoin functions

