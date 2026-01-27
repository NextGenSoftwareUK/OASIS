# Web4 Liquidity Pool: Solana + Base Architecture

**Target chains:** Solana, Base.  
**Purpose:** How the unified pool uses OASIS bridge and providers for Solana and Base.

---

## 1. Chain Identifiers

Use these keys in `reservesByChain`, `feesPendingByChain`, and bridge/coordinator logic:

| Chain   | Key       | Provider (OASIS)   | Bridge symbol | Chain ID (EVM) |
|---------|-----------|--------------------|---------------|----------------|
| Solana  | `solana`  | SolanaOASIS        | SOL           | —              |
| Base    | `base`    | BaseOASIS          | BASE          | 8453           |

---

## 2. Data Flow: Solana and Base

```
                    ┌─────────────────────────────────────────┐
                    │     Liquidity Coordinator                │
                    │     (load/save Pool State Holon)         │
                    └───────────────┬───────────────────────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
          ▼                         ▼                         ▼
   ┌──────────────┐          ┌──────────────┐          ┌──────────────┐
   │ Pool State   │          │ CrossChain   │          │ SolanaOASIS  │
   │ Holon        │          │ BridgeManager│          │ BaseOASIS    │
   │              │          │              │          │ (providers)  │
   │ reservesBy   │          │ SOL ↔ BASE   │          │              │
   │ Chain:       │          │ (when we     │          │ balances,    │
   │  solana      │          │  add it)     │          │ transfer     │
   │  base        │          │              │          │              │
   └──────────────┘          └──────────────┘          └──────────────┘
```

- **Pool State Holon** holds `reservesByChain["solana"]` and `reservesByChain["base"]`.
- **CrossChainBridgeManager** is used for SOL ↔ BASE (or SOL ↔ ETH, BASE ↔ ETH) once those routes exist in the bridge; Web4 Liquidity Pool calls the bridge for rebalance and cross-chain settlement.
- **SolanaOASIS / BaseOASIS** are used for balances and per-chain transfers (e.g. lock/release for pool reserves).

---

## 3. Phase 1: One Chain (Solana)

- **Pool State Holon:** Single key `reservesByChain["solana"]` and global `totalLpSupply`, `lpSharesByAvatar`.
- **Add/Remove/Swap:** Implement only on Solana (user locks to pool reserve on Solana; coordinator updates holon).
- **No cross-chain yet.** Delivers “one pool, one chain” and validates schema + coordinator.

---

## 4. Phase 2: Add Base

- **Pool State Holon:** Add `reservesByChain["base"]` and `feesPendingByChain["solana"]`, `feesPendingByChain["base"]`.
- **Add liquidity on Base:** User locks to pool reserve on Base; coordinator updates `reservesByChain["base"]` and `lpSharesByAvatar`.
- **Remove liquidity:** User chooses payout chain (Solana or Base); coordinator releases from that chain’s reserve (or uses bridge if we need to “move” value to the other chain first).
- **Swap:** Same-chain swap on Solana or Base using that chain’s reserves. Cross-chain swap (user on Solana wants token that lives on Base) uses CrossChainBridgeManager in a later phase.

---

## 5. Bridge Routes for Solana + Base

Current bridge pairs (from CrossChainBridgeManager) include SOL ↔ ETH, etc. For **SOL ↔ BASE** we need either:

- A **SOL ↔ BASE** route implemented in the bridge (two legs: SOL → ETH or intermediary, then ETH/BASE; or direct SOL ↔ BASE if supported), or
- **SOL ↔ ETH** and **BASE ↔ ETH** and coordinate via Ethereum as hub.

**Web4 Liquidity Pool** does not implement new bridge routes; it calls the existing CrossChainBridgeManager. Bridge expansion (e.g. Base bridge service) is done in the existing Bridge provider layer.

---

## 6. Provider Usage (Solana + Base)

- **SolanaOASIS:** Wallet/balance checks, lock/release of pool reserves on Solana (e.g. technical account or future PoolVault on Solana).
- **BaseOASIS:** Same on Base (EVM). Use existing Base provider for balance and transfers; pool reserve = designated address (or future PoolVault on Base).

Contract addresses and “pool reserve” accounts for Solana and Base are defined in config or in the Pool State Holon (e.g. `MetaData["reserveAddressByChain"]`).

---

## 7. Token Pairs (Examples)

For Solana + Base, initial pairs can be:

- **USDC / USDC** (same asset, different chains) — canonical “stable” pair; reserve buckets on Solana and Base.
- **SOL / wrapped-SOL-on-Base** (or ETH) — once bridge supports SOL ↔ BASE or SOL ↔ ETH, BASE ↔ ETH.
- **Web4-wrapped pair** — token0 and token1 are Web4 token ids; their Web3 instances on Solana and Base are used for reserves.

Schema and coordinator are chain-agnostic; chain ids `solana` and `base` are the only Solana/Base-specific pieces in the pool state.

---

## 8. References

- [Main architecture](../../Docs/CROSS_CHAIN_LIQUIDITY_POOL_ARCHITECTURE.md)
- [Phase 1 plan](PHASE1_PLAN.md)
- [Pool State Holon schema](../schema/PoolStateHolon.schema.json)
- Bridge: `OASIS Architecture/.../Managers/Bridge/CrossChainBridgeManager.cs`
- Base provider: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/`
- Solana provider: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/`
