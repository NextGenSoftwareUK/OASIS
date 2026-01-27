# Web4 Liquidity Pool

**Unified cross-chain liquidity pools for Solana and Base.**

One logical pool per pair; LPs add liquidity once and earn from both chains. Built on OASIS HolonManager, CrossChainBridgeManager, and providers.

---

## Target Chains

| Chain   | Symbol | Role                          |
|---------|--------|-------------------------------|
| **Solana** | SOL   | Reserve bucket, swap venue    |
| **Base**   | BASE  | Reserve bucket, swap venue    |

Later: Ethereum, Polygon, Arbitrum as additional reserve buckets.

---

## What This Module Contains

- **Schema** — Pool State Holon shape and chain ids for Solana + Base.
- **Docs** — Architecture for Solana/Base, Phase 1 plan, integration with OASIS core.
- **Src** — Liquidity Coordinator service (and any shared DTOs) that plug into ONODE/OASIS.

---

## Architecture

- **Canonical state:** One Pool State Holon per pool (reserves by chain, LP shares, fees). Stored via HolonManager + HyperDrive.
- **Value movement:** CrossChainBridgeManager (Solana ↔ Base) for rebalance and cross-chain settlement.
- **Coordinator:** New service that applies add/remove/swap and fee logic, updates the holon, and calls the bridge.

Full design: [../Docs/CROSS_CHAIN_LIQUIDITY_POOL_ARCHITECTURE.md](../Docs/CROSS_CHAIN_LIQUIDITY_POOL_ARCHITECTURE.md).

Solana + Base specifics: [docs/ARCHITECTURE_SOLANA_BASE.md](docs/ARCHITECTURE_SOLANA_BASE.md).

---

## Quick Start (Phase 1)

1. **Pool State Holon** — Use `schema/PoolStateHolon.schema.json` and register `HolonType.LiquidityPool` (or equivalent) in core.
2. **Liquidity Coordinator** — Implement add/remove/swap for **one** chain first (e.g. Solana), then add Base and cross-chain.
3. **ONODE API** — Add endpoints that call the Coordinator; see `docs/PHASE1_PLAN.md`.

---

## Repository Layout

```
Web4 Liquidity Pool/
├── README.md                 (this file)
├── docs/
│   ├── ARCHITECTURE_SOLANA_BASE.md
│   └── PHASE1_PLAN.md
├── schema/
│   └── PoolStateHolon.schema.json
└── src/
    └── (Liquidity Coordinator and DTOs — see PHASE1_PLAN)
```

---

## References

- [Cross-Chain Liquidity Pool Architecture](../Docs/CROSS_CHAIN_LIQUIDITY_POOL_ARCHITECTURE.md)
- [Cross-Chain Liquidity Capability Assessment](../Docs/CROSS_CHAIN_LIQUIDITY_CAPABILITY_ASSESSMENT.md)
- OASIS Bridge: `OASIS Architecture/.../Managers/Bridge/CrossChainBridgeManager.cs`
- HolonManager: `OASIS Architecture/.../Managers/HolonManager/`
