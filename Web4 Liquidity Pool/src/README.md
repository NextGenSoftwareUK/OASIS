# Web4 Liquidity Pool — Source and Integration

This folder holds **contracts and integration points** for the Liquidity Coordinator. The actual implementation can live in OASIS Core or ONODE; these files define the API and DTOs so both Solana and Base flows use the same shape.

---

## Contents

- **ILiquidityCoordinator.cs** — Interface for add/remove/swap and pool state. Implement in `OASIS Architecture/.../Managers/Liquidity/` or `ONODE/.../Services/LiquidityCoordinatorService.cs`.
- **DTOs/** — Request/response and pool-state DTOs. Copy or reference from ONODE or Core when building the Coordinator and the LiquidityController.

---

## Where to Implement

| Piece | Suggested location |
|-------|--------------------|
| Coordinator logic | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Liquidity/LiquidityCoordinator.cs` (new), or `ONODE/.../Services/LiquidityCoordinatorService.cs` |
| DTOs / requests | Next to Coordinator, or `ONODE/.../Models/Liquidity/` |
| ONODE API | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/LiquidityController.cs` |
| Pool State Holon type | `OASIS Architecture/.../Holons/PoolStateHolon.cs` (optional), or use `Holon` + `MetaData` shaped by `schema/PoolStateHolon.schema.json` |

---

## Target Chains

- **Solana** — Phase 1a (one chain only).
- **Base** — Phase 1b (second reserve bucket; add/remove/swap on Base).

See [docs/PHASE1_PLAN.md](../docs/PHASE1_PLAN.md) and [docs/ARCHITECTURE_SOLANA_BASE.md](../docs/ARCHITECTURE_SOLANA_BASE.md).
