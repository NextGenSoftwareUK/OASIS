# Cross-Chain Liquidity Pool: Architecture Map

**Purpose:** Map how to build unified cross-chain liquidity pools on top of the existing OASIS core.  
**Audience:** Engineering and product.  
**Status:** Design document — implementation roadmap.

---

## 1. Goal

**One logical pool per pair** (e.g. USDC/ETH): LPs add liquidity once, and that pool serves swaps on every supported chain. TVL is shared; fees are aggregated; whales see one deep pool.

---

## 2. What We Reuse From Core

| Component | Role in cross-chain pool |
|-----------|---------------------------|
| **HolonManager + Holons** | Canonical pool state: reserves by chain, LP shares, fee accounting. One “Pool State” holon (or small holon graph) per pool. Saved/loaded via current providers; multi-provider persistence gives redundancy and read paths. |
| **HyperDrive** | Today it routes **data** (SaveHolon, LoadHolon, SaveAvatar, LoadAvatar). We use it in two ways: (1) Persist/load **Pool State Holons** with failover and load balancing. (2) Optionally add a **LiquidityOperationRequest** and route it to a new Liquidity Coordinator — same “route by request type” pattern. |
| **CrossChainBridgeManager + IOASISBridge** | Move value across chains (Withdraw source → Deposit dest, rollback on failure). Used for: rebalancing pool reserves, settling cross-chain swaps, and (in one design) moving user funds when they add/remove liquidity on a non-canonical chain. |
| **Web4 / Web3 tokens** | Pool is defined over **Web4 token pairs** (e.g. “pUSD” vs “pEUR”), so the same logical asset on many chains is one pool. Metadata and chain mapping already live in Web4; we add “pool id” and “reserve role” in pool logic. |
| **ProviderManager / providers** | Each chain has a provider (Solana, Ethereum, etc.). Bridge uses them for Withdraw/Deposit. Pool Coordinator uses them to read balances, send tx, and to know “which chain” when updating pool state. |
| **Exchange rate service** | Already used by the bridge. Reuse for pool pricing, fee conversion, and display (e.g. TVL in USD). |

---

## 3. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        CROSS-CHAIN LIQUIDITY POOL LAYER                           │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                   │
│  ┌──────────────────────┐     ┌──────────────────────┐     ┌─────────────────┐ │
│  │ LiquidityCoordinator  │────►│   Pool State Holon    │◄────│  HolonManager   │ │
│  │ (new service)         │     │   (canonical state)   │     │  + HyperDrive   │ │
│  └──────────┬────────────┘     └──────────────────────┘     └─────────────────┘ │
│             │                                                                     │
│             │  • reserves_by_chain, lp_supply, lp_shares, fees_pending            │
│             │  • Save/Load via existing HolonManager; replicate via HyperDrive    │
│             │                                                                     │
│             ▼                                                                     │
│  ┌──────────────────────┐     ┌──────────────────────┐                          │
│  │ CrossChainBridge      │     │ Per-chain pool       │                          │
│  │ Manager               │     │ contracts or vaults  │                          │
│  │ (existing)            │     │ (new, one per chain)  │                          │
│  └──────────┬────────────┘     └──────────┬───────────┘                          │
│             │                              │                                       │
│             │  Withdraw/Deposit            │  Lock/Release, Swap (optional)       │
│             │  for rebalance &             │  or just “custody” per chain         │
│             │  cross-chain settlement      │                                       │
│             ▼                              ▼                                       │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                    IOASISBridge per chain (existing)                          │ │
│  │  Solana • Ethereum • Polygon • Arbitrum • Base • …                            │ │
│  └─────────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Canonical State: Pool State Holon

**Idea:** One pool = one “Pool State” holon (or a parent + child holons). All chain-agnostic truth lives here.

**Suggested shape (e.g. in `MetaData` or a dedicated holon type):**

```json
{
  "poolId": "uuid",
  "token0": "Web4TokenId_or_symbol",
  "token1": "Web4TokenId_or_symbol",
  "totalLpSupply": "123456789",
  "reservesByChain": {
    "ethereum": { "token0": "1000000", "token1": "800" },
    "solana":    { "token0": "500000",  "token1": "400" },
    "polygon":  { "token0": "200000",  "token1": "160" }
  },
  "lpSharesByAvatar": {
    "avatarId1": "100000",
    "avatarId2": "50000"
  },
  "feesPendingByChain": {
    "ethereum": { "token0": "100", "token1": "0.08" },
    "solana":   { "token0": "50",  "token1": "0.04" }
  },
  "lastRebalanceAt": "iso8601",
  "version": 1
}
```

**Persistence:** Use **HolonManager.SaveHolon / LoadHolon** as today. No change to HolonManager. Use **HyperDrive** (or direct ProviderManager) so pool-state reads/writes can fail over and load-balance across MongoDB, Solana, IPFS, etc. Pool state is “data”; HyperDrive already routes data.

**Identity:** Add `HolonType.LiquidityPool` (or use `HolonType.Holon` + `MetaData["type"] = "LiquidityPool"`) and `ProviderUniqueStorageKey` per provider so the same pool can be stored and replicated like any other holon.

---

## 5. Liquidity Coordinator (New Backend Service)

**Role:** Single place that (a) owns the rules of the pool (add/remove/swap, fee math), (b) updates the Pool State Holon, (c) calls the bridge and/or per-chain contracts when value must move.

**Location (conceptual):** New project or folder, e.g. `OASIS Architecture/.../Managers/Liquidity/` or `ONODE/.../Services/LiquidityCoordinatorService.cs`, with REST (or internal API) used by ONODE.

**Inputs:**

- “Add liquidity” (avatarId, chainId, token0Amount, token1Amount)
- “Remove liquidity” (avatarId, lpAmount, preferredChain)
- “Swap” (avatarId, chainId, tokenIn, amountIn, tokenOut, minAmountOut)

**Core flow (add liquidity on chain A):**

1. Load Pool State Holon (via HolonManager).
2. Check balances on chain A (via provider or bridge).
3. User signs lock/deposit on chain A (vault or bridge technical account).
4. Coordinator updates Pool State Holon: `reservesByChain[A] += amounts`, `totalLpSupply += newLp`, `lpSharesByAvatar[avatarId] += newLp`.
5. Save Pool State Holon (HolonManager.SaveHolon → HyperDrive/data layer).
6. Return LP receipt / event.

**Core flow (swap on chain B):**

1. Load Pool State Holon.
2. Compute in-pool swap (e.g. constant-product or oracle-bounded like FXPool) using `reservesByChain[B]` or “global” reserves, depending on design.
3. User signs swap on chain B (vault releases tokenOut to user, takes tokenIn).
4. Coordinator updates Pool State Holon: reserves for chain B (or global), and `feesPendingByChain[B]` if applicable.
5. Save Pool State Holon.
6. Return tx id / event.

**Core flow (cross-chain swap, e.g. user on Solana wants token that “lives” on Ethereum):**

1. Load Pool State Holon.
2. Decide settlement: e.g. “use Solana reserve for tokenIn, need tokenOut on Ethereum.”
3. Use **CrossChainBridgeManager**: e.g. Withdraw tokenOut-equivalent from Ethereum pool reserve → Deposit to user on Solana (or user’s preferred chain), after exchange-rate and fee logic.
4. Update Pool State Holon (reserves, fees).
5. Save.
6. Return bridge order id + tx ids.

The Coordinator never holds custody itself; it updates the ledger (holon) and triggers movement via bridge and/or on-chain vaults.

---

## 6. Where Value Lives: Two Design Options

### Option A: Pool reserves in bridge “technical” accounts per chain

- Each chain’s **IOASISBridge** already has a notion of “technical” or escrow account (e.g. for Withdraw/Deposit).
- Reserve for chain A = balance of that technical account on chain A (for token0/token1).
- **Add liquidity on A:** User sends tokens to that account (or a wrapper contract that forwards to it); Coordinator updates Pool State Holon.
- **Swap on A:** Coordinator instructs bridge/provider to move from technical account to user (and user to technical account for tokenIn), then updates state.
- **Pros:** Reuses existing bridge accounts; minimal new on-chain contracts.  
- **Cons:** Bridge and pool semantics are tied; need clear separation of “bridge escrow” vs “pool reserve” if both use the same accounts, or dedicated “pool technical accounts” per chain.

### Option B: Dedicated pool vault contracts per chain

- Each chain has a **PoolVault(chainId, poolId)** (or one contract per pool per chain).
- PoolVault holds token0/token1 for that chain; exposes Lock, Release, Swap (or only Lock/Release; swap is Coordinator + bridge).
- **Add liquidity on A:** User calls `PoolVault(A).Lock(token0, token1)`; backend listens, then Coordinator updates Pool State Holon.
- **Swap on B:** User calls `PoolVault(B).swap(...)` or Coordinator triggers Release after verifying user pay-in on another chain via bridge.
- **Pros:** Clear separation; pool logic is on-chain per chain; easier to audit.  
- **Cons:** Requires new contracts and deployments per chain; more work.

**Recommendation:** Start with **Option A** (reuse bridge accounts or add “pool reserve” technical accounts per chain in the bridge layer) to ship faster; introduce Option B when you need more on-chain guarantees or composability.

---

## 7. HyperDrive’s Role, Extended

**Today:** HyperDrive routes `IRequest` by type → `SaveHolonRequest`, `LoadHolonRequest`, `SaveAvatarRequest`, `LoadAvatarRequest` → HolonManager/AvatarManager.

**For pools we have two straightforward options:**

1. **Use existing Holon load/save only**  
   Liquidity Coordinator uses **HolonManager** directly (or via whatever already goes through HyperDrive for holons). Pool State is “just a holon.” No change to HyperDrive request types. Coordinator is a separate service that calls HolonManager + Bridge.

2. **Add a liquidity request type**  
   Define `LiquidityOperationRequest : IRequest` (e.g. `Operation = "AddLiquidity" | "RemoveLiquidity" | "Swap"`, plus parameters). In `RouteToProviderAsync`, add:
   - `LiquidityOperationRequest op => await RouteLiquidityOperationAsync<T>(op)`  
   and implement `RouteLiquidityOperationAsync` by calling the Liquidity Coordinator (in-process or out-of-process). HyperDrive then “routes” liquidity ops to the coordinator in the same way it routes save/load to storage. Provider list for “liquidity” could be a single logical provider (the coordinator) or a no-op for selection.

Option 1 is simpler and uses the core “as is.” Option 2 makes liquidity a first-class request type and sets you up for things like “route this swap to the least-loaded coordinator instance” later.

---

## 8. End-to-End Flows (Summary)

| Flow | Coordinator | Holon / state | Bridge / chains |
|------|-------------|----------------|-----------------|
| **Add liquidity (chain A)** | Validate amounts, compute LP; update state | Load pool holon → apply `reservesByChain[A] += _, lpShares += _` → Save | User (or frontend) locks tokens into pool reserve on A (vault or bridge account) |
| **Remove liquidity** | Compute redeem amounts; update state; choose chain for payout | Decrease `lpShares`, `reservesByChain[chain]`; Save | Release from pool reserve on chosen chain to user (or bridge to user’s chain) |
| **Swap same chain** | Pricing + fee; update reserves for that chain | Load → adjust `reservesByChain[chain]`, `feesPendingByChain` → Save | Vault/contract on that chain does transfer user ↔ reserve (or Coordinator triggers via provider/bridge) |
| **Swap cross-chain** | Pricing + fee; decide source/dest chain | Load → adjust reserves for both chains (or global), fees → Save | Bridge.Withdraw(srcChain) → Bridge.Deposit(destChain, user); rollback if deposit fails |
| **Rebalance** | Decide movements to keep reserves “balanced” or above thresholds | Load → update `reservesByChain` after each move → Save | Bridge transfers between chains (e.g. from chain with surplus to chain with demand) |
| **Fee distribution** | Periodically apply `feesPendingByChain` to LP rewards or NAV | Load → update lpShares or fee accumulator, clear pending → Save | Optionally move fee tokens via bridge to a “fee distribution” flow (e.g. x402 on Solana) |

---

## 9. Web4 and “One Pool, All Chains”

- **Pool definition:** `token0` / `token1` are Web4 token ids (or symbols resolved to Web4). That implies “same logical asset” on many chains.
- **Reserves:** `reservesByChain` keys = chain ids (or provider symbols). Values = amounts of token0/token1 on that chain that belong to this pool.
- **LP shares:** Global. `lpSharesByAvatar` is chain-agnostic; when user removes liquidity, they can nominate a chain for payout, and Coordinator + bridge handle movement if needed.

So “one pool” = one Pool State Holon + one logical Web4 pair. “All chains” = many keys in `reservesByChain` and many bridges/vaults, all updated by the same Coordinator and state.

---

## 10. New vs Existing Code (Checklist)

| Piece | New or existing | Where |
|-------|------------------|-------|
| Pool State Holon schema (e.g. MetaData shape or HolonType) | New schema, existing Holon type system | HolonType.LiquidityPool (add enum value) or Holon + MetaData; doc in Core or DNA |
| LiquidityCoordinator (add/remove/swap, fee logic, state updates) | **New** | e.g. `Managers/Liquidity/` or ONODE `Services/LiquidityCoordinatorService.cs` |
| API surface for liquidity (REST or internal) | **New** | e.g. ONODE `LiquidityController.cs` or equivalent, calling Coordinator |
| Pool vaults or “pool reserve” accounts per chain | **New** (if Option B) or **extend** bridge (if Option A) | New contracts per chain, or config + usage of existing bridge technical accounts |
| HolonManager / HyperDrive usage for pool state | **Existing** | Use SaveHolon/LoadHolon (and optionally HyperDrive) from Coordinator; no change to Manager/HyperDrive internals |
| CrossChainBridgeManager usage | **Existing** | Coordinator calls CreateBridgeOrderAsync (or equivalent) for cross-chain settlement and rebalance |
| Exchange rate service | **Existing** | Coordinator uses same service as bridge for pricing and fees |
| Web4/Web3 token resolution | **Existing** | Use existing Web4/Web3 types to resolve token0/token1 and chain mappings |

---

## 11. Phased Build

**Phase 1 — Canonical state + one chain**

- Introduce Pool State Holon (e.g. `HolonType.LiquidityPool` + schema).
- Implement Liquidity Coordinator for **one** chain only: add/remove liquidity, swap on that chain, fees.
- Pool “reserves” live in one place (one chain’s vault or bridge account). No cross-chain yet.
- Delivers: “One pool, one chain,” with state and logic ready for multi-chain.

**Phase 2 — Multi-chain reserves, one pool**

- Add `reservesByChain` and wire Coordinator to bridge + providers for 2–3 chains.
- Add liquidity on chain A or B; remove on A or B (payout chosen by user); rebalance via bridge when needed.
- Same LP share ledger; multiple reserve buckets.

**Phase 3 — Cross-chain swap + routing**

- User on chain A wants token that’s mostly on chain B. Coordinator uses bridge to move value and updates both reserves.
- Optionally: “best chain to execute this swap” (cost/latency) — could be a small HyperDrive-style selector over chains (e.g. `LiquidityOperationRequest` with chain preference or “auto”).

**Phase 4 — Fee aggregation and distribution**

- Aggregate `feesPendingByChain` into one accounting; distribute to LPs (e.g. increase LP NAV or run a fee-distribution job).
- Optional: plug into x402 or existing treasury flows for payouts on specific chains.

---

## 12. Diagram: How It Fits in OASIS

```
                    ┌─────────────────────────────────────────┐
                    │           ONODE / API layer              │
                    │  LiquidityController (new)               │
                    └─────────────────┬───────────────────────┘
                                      │
                    ┌─────────────────▼───────────────────────┐
                    │     LiquidityCoordinator (new)           │
                    │  • Add/Remove/Swap logic                 │
                    │  • Fee and LP math                       │
                    │  • Load/Save Pool State Holon            │
                    │  • Call Bridge + providers               │
                    └──────┬──────────────────────┬───────────┘
                           │                      │
         ┌─────────────────▼──────┐    ┌─────────▼─────────────────┐
         │  HolonManager          │    │  CrossChainBridgeManager   │
         │  (+ HyperDrive for     │    │  (existing)                │
         │   save/load holons)    │    │  Withdraw/Deposit          │
         └─────────────────┬──────┘    └─────────┬─────────────────┘
                           │                      │
         ┌─────────────────▼──────┐    ┌─────────▼─────────────────┐
         │  Pool State Holon     │    │  IOASISBridge per chain     │
         │  (canonical state)    │    │  Solana, Ethereum, Polygon… │
         │  reservesByChain,     │    │  + optional PoolVaults     │
         │  lpShares, fees       │    │  per chain                 │
         └───────────────────────┘    └───────────────────────────┘
```

---

## 13. References in Repo

| Topic | Location |
|-------|----------|
| Holon type and persistence | `Core/Enums/HolonType.cs`, `Core/Managers/HolonManager/`, `Core/Holons/HolonBase.cs` |
| HyperDrive request routing | `Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs` (RouteToProviderAsync, request switch) |
| Bridge atomic swap | `Core/Managers/Bridge/CrossChainBridgeManager.cs` (ExecuteAtomicSwapAsync, Withdraw/Deposit) |
| IOASISBridge | `Core/Managers/Bridge/Interfaces/IOASISBridge.cs` |
| Single-chain AMM reference | `pUSD/contracts/FXPool.sol` (add/remove liquidity, swap, LP tokens, oracle) |
| Web4 token model | `Core/Objects/Wallet/Web4Token.cs`, `Core/Interfaces/NFT/IWeb4Token.cs` |
| Capability gap (today vs design) | `Docs/CROSS_CHAIN_LIQUIDITY_CAPABILITY_ASSESSMENT.md` |

---

*This document maps existing OASIS core to a concrete cross-chain liquidity pool design. Implementation details (e.g. exact holon schema, API contracts, chain ids) can be refined in follow-on specs.*
