# Web4 Liquidity Pool — OASIS/ONODE Integration Checklist

**Target:** Solana, Base. Use this list when wiring the Web4 Liquidity Pool into the main OASIS/ONODE solution.

---

## 1. Core (OASIS Architecture)

- [x] **HolonType** — In `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/HolonType.cs`, add:
  ```csharp
  LiquidityPool,
  ```
- [x] **Pool State Holon** — Use existing `Holon` with `HolonType.LiquidityPool`; pool state stored in `MetaData["PoolState"]` as JSON (shape from `schema/PoolStateHolon.schema.json`). Implemented in `LiquidityManager.PoolStateToHolon` / `PoolStateFromHolon`.
- [x] **Liquidity Coordinator** — `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Liquidity/LiquidityManager.cs` implements add/remove/swap and CreatePool; loads/saves Pool State Holon via HolonManager. Phase 1a: no Solana/Base provider calls yet.
- [x] **DTOs** — AddLiquidityRequest, RemoveLiquidityRequest, SwapRequest, CreatePoolRequest, PoolStateResult, ReserveAmounts, etc. live in `LiquidityManager.cs` (same namespace).

---

## 2. ONODE

- [x] **LiquidityController** — `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/LiquidityController.cs` uses `LiquidityManager.Instance` (no separate service). Endpoints:
  - `POST /api/v1/liquidity/pools` → CreatePoolAsync
  - `GET /api/v1/liquidity/pools/{poolId}` → GetPoolStateAsync
  - `POST /api/v1/liquidity/pools/{poolId}/add` → AddLiquidityAsync
  - `POST /api/v1/liquidity/pools/{poolId}/remove` → RemoveLiquidityAsync
  - `POST /api/v1/liquidity/pools/{poolId}/swap` → SwapAsync
- [x] **Startup/DI** — No registration needed; controller uses `LiquidityManager.Instance` like BridgeController.

---

## 3. Providers and Bridge

- [ ] **Solana** — Ensure SolanaOASIS is registered and usable for balance/transfer. Pool reserve on Solana = config or `reserveAddressByChain["solana"]` in pool holon.
- [ ] **Base** — Ensure BaseOASIS is registered. Pool reserve on Base = config or `reserveAddressByChain["base"]`.
- [ ] **Bridge** — For Phase 2 (cross-chain), ensure CrossChainBridgeManager has or will have a route that can move value between Solana and Base (or via an intermediary chain). Phase 1a/1b only need same-chain operations.

---

## 4. Web4 Liquidity Pool Folder (this repo)

- [ ] **Schema** — `Web4 Liquidity Pool/schema/PoolStateHolon.schema.json` is the source of truth for pool state shape.
- [ ] **Docs** — `docs/ARCHITECTURE_SOLANA_BASE.md`, `docs/PHASE1_PLAN.md` describe Solana + Base and phase order.
- [ ] **Contract** — `src/ILiquidityCoordinator.cs` defines the coordinator API and DTOs; implement in Core/ONODE as above.

---

## 5. Phase Order

1. **Phase 1a:** Solana only — one pool, add/remove/swap on Solana, state in Pool State Holon.
2. **Phase 1b:** Add Base — second reserve bucket, add/remove/swap on Base, shared LP ledger.
3. **Phase 2:** Cross-chain — use bridge for cross-chain swap and rebalance.

See [docs/PHASE1_PLAN.md](docs/PHASE1_PLAN.md) for detailed tasks and acceptance criteria.
