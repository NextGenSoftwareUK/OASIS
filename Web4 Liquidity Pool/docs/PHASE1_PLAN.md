# Web4 Liquidity Pool: Phase 1 Plan (Solana, then Base)

**Goal:** Ship “one pool, one chain” on Solana first, then add Base and cross-chain.  
**Target chains:** Solana (Phase 1a), Base (Phase 1b), then cross-chain (Phase 2).

---

## Phase 1a: Solana Only

### 1. Pool State Holon

- [ ] Add `HolonType.LiquidityPool` to `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/HolonType.cs` (or use `HolonType.Holon` + `MetaData["type"] = "LiquidityPool"`).
- [ ] Use schema from `schema/PoolStateHolon.schema.json`. Store in holon `MetaData` or in a dedicated `PoolStateHolon` class that implements `IHolon`.
- [ ] `reservesByChain` has one key: `"solana"`. `lpSharesByAvatar` and `totalLpSupply` are global.

### 2. Liquidity Coordinator (backend)

- [ ] Create service that:
  - Loads/saves Pool State Holon via `HolonManager`.
  - Implements add liquidity, remove liquidity, swap (constant-product or oracle-bounded, reusing logic from pUSD FXPool where useful).
  - For Phase 1a, all operations are **on Solana only** (user locks to pool reserve on Solana; coordinator updates holon).
- [ ] **Location options:**
  - **A:** New project under `Web4 Liquidity Pool/src/` that references OASIS Core and is used by ONODE.
  - **B:** New manager under `OASIS Architecture/.../Managers/Liquidity/` (e.g. `LiquidityCoordinator.cs` or `LiquidityManager.cs`).
  - **C:** New service under `ONODE/.../Services/LiquidityCoordinatorService.cs` that wraps (A) or (B).

Recommendation: start with (B) or (C) so ONODE can call it without a separate deployment. Use `Web4 Liquidity Pool/src/` for DTOs, interfaces, and any shared schema that live outside core.

### 3. Pool “reserve” on Solana

- [ ] **Option A (fast):** Use existing Solana bridge/technical account or a designated Solana wallet as the pool reserve for Phase 1a. Coordinator records this address in Pool State Holon (e.g. `reserveAddressByChain["solana"]`).
- [ ] **Option B (clearer):** Deploy a minimal “PoolVault” contract/program on Solana that holds token0/token1 and exposes Lock/Release (or inline lock/release via simple transfer to a known address). Coordinator triggers releases via SolanaOASIS.

### 4. ONODE API

- [ ] Add endpoints, e.g.:
  - `POST /api/liquidity/pools` — create pool (admin).
  - `GET /api/liquidity/pools/{poolId}` — get pool state (reserves, TVL, LP supply).
  - `POST /api/liquidity/pools/{poolId}/add` — add liquidity (body: avatarId, chainId, token0Amount, token1Amount; Phase 1a chainId = solana).
  - `POST /api/liquidity/pools/{poolId}/remove` — remove liquidity (body: avatarId, lpAmount, preferredChainId).
  - `POST /api/liquidity/pools/{poolId}/swap` — swap (body: avatarId, chainId, tokenIn, amountIn, tokenOut, minAmountOut).
- [ ] All mutate operations call the Liquidity Coordinator; coordinator updates Pool State Holon and (when applicable) triggers Solana moves via provider/bridge.

### 5. Acceptance (Phase 1a)

- [ ] Create one pool (e.g. USDC/USDC or a test pair) with `reservesByChain["solana"]`.
- [ ] Add liquidity on Solana → `totalLpSupply` and `lpSharesByAvatar` update; reserves on Solana increase.
- [ ] Remove liquidity on Solana → reserves decrease; user receives tokens on Solana.
- [ ] Same-chain swap on Solana → reserves update; user receives output token on Solana.
- [ ] Pool state persisted and loadable via HolonManager (and optionally HyperDrive).

---

## Phase 1b: Add Base

### 6. Base reserve bucket

- [ ] Add `reservesByChain["base"]` and (if used) `reserveAddressByChain["base"]`.
- [ ] Use BaseOASIS for balance checks and transfers. Reserve = designated EVM address or future PoolVault on Base.

### 7. Coordinator: add/remove on Base

- [ ] Add liquidity on Base: user locks to Base reserve; coordinator updates `reservesByChain["base"]` and `lpSharesByAvatar`.
- [ ] Remove liquidity: user chooses payout chain (Solana or Base); coordinator releases from that chain’s reserve. If we need to move value from one chain to the other for payout, that’s Phase 2 (bridge).

### 8. Same-chain swap on Base

- [ ] Swap on Base using `reservesByChain["base"]`; coordinator updates state and triggers Base transfer via BaseOASIS.

### 9. Acceptance (Phase 1b)

- [ ] Add/remove liquidity on Base; LP shares are shared (one pool, two chains).
- [ ] Swap on Solana and swap on Base both work; reserves per chain update correctly.
- [ ] Pool state holon shows both `solana` and `base` in `reservesByChain` and `feesPendingByChain`.

---

## Phase 2: Cross-Chain (after 1b)

- [ ] Cross-chain swap: user on Solana wants token that lives on Base (or vice versa). Coordinator uses CrossChainBridgeManager to move value between chains, then updates both reserve buckets.
- [ ] Rebalance: coordinator (or keeper) triggers bridge moves when one chain’s reserves are too low or too high.
- [ ] Fee aggregation: aggregate `feesPendingByChain` and distribute to LPs (e.g. increase NAV or run distribution job).

---

## Files and Locations (Summary)

| What | Where |
|------|--------|
| Pool State Holon schema | `Web4 Liquidity Pool/schema/PoolStateHolon.schema.json` |
| HolonType.LiquidityPool | `OASIS Architecture/.../Enums/HolonType.cs` (add enum value) |
| Liquidity Coordinator logic | `OASIS Architecture/.../Managers/Liquidity/` or `ONODE/.../Services/LiquidityCoordinatorService.cs` |
| DTOs / requests | `Web4 Liquidity Pool/src/` or next to Coordinator in core/ONODE |
| ONODE API | `ONODE/.../Controllers/LiquidityController.cs` (new) |
| Solana reserve | Config or `reserveAddressByChain["solana"]` in pool holon; optional PoolVault later |
| Base reserve | Config or `reserveAddressByChain["base"]` in pool holon; optional PoolVault later |

---

## Dependency Order

1. Schema + HolonType (or MetaData convention).
2. Coordinator that only reads/writes Pool State Holon and talks to SolanaOASIS (Phase 1a).
3. ONODE endpoints that call Coordinator.
4. Base reserve + Coordinator support for Base (Phase 1b).
5. Bridge integration for cross-chain (Phase 2).

---

## References

- [Architecture (Solana + Base)](ARCHITECTURE_SOLANA_BASE.md)
- [Main architecture](../../Docs/CROSS_CHAIN_LIQUIDITY_POOL_ARCHITECTURE.md)
- [Pool State Holon schema](../schema/PoolStateHolon.schema.json)
