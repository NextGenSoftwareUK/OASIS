# Cross-Chain Liquidity: Capability Assessment

**Purpose:** Honest audit of what we **have** vs. what we **describe** for cross-chain liquidity pools.  
**Status:** As of January 2026, based on code and docs in-repo.

---

## Executive summary

We have **building blocks** for cross-chain value movement and **single-chain** liquidity pools. We do **not** yet have implemented **unified cross-chain liquidity pools** (one pool, shared TVL, serving all chains). The “unified liquidity” / “HyperDrive liquidity” narrative in the Universal Asset Bridge UI and in `CROSS_CHAIN_LIQUIDITY_FAQ.md` is a **target architecture**, not current capability.

---

## 1. What we have today

### 1.1 Cross-chain value movement (bridge)

**Location:**  
`OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Bridge/CrossChainBridgeManager.cs`  
`Managers/Bridge/Interfaces/IOASISBridge.cs`

**Capability:**

- **Atomic cross-chain swap**: Withdraw on source chain (user → technical/escrow account), Deposit on destination chain (technical → user). Rollback (Deposit back to source) if the second leg fails.
- **Pairs:** SOL↔XRD, SOL↔ETH, ETH↔SOL, ZEC↔SOL (and private pairs ZEC↔Aztec, ZEC↔Miden, ZEC↔Starknet where applicable).
- **Exchange rates:** CoinGecko (or equivalent) for conversion.
- **API:** CreateBridgeOrderAsync, CheckOrderBalanceAsync, etc.; BridgeController exposes REST.

So we **can** move value across chains in an atomic-swap style. That is **cross-chain transfer**, not “one pool shared across chains.”

### 1.2 Single-chain liquidity pool (AMM)

**Location:**  
`pUSD/contracts/FXPool.sol`  
`pUSD/scripts/Deploy.s.sol`, `pUSD/test/FXPool.t.sol`

**Capability:**

- **One chain:** pUSD/pEUR AMM on a single chain (Ethereum in the pUSD context).
- **Functions:** `addLiquidity`, `removeLiquidity`, `swap`, LP tokens, fees, oracle-bounded pricing.
- **Deploy/initial liquidity:** Scripts and tests exist.

So we **do** have a real, on-chain **single-chain** liquidity pool. It is **not** cross-chain and does **not** aggregate TVL across chains.

### 1.3 HyperDrive

**Location:**  
`OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs`  
`Docs/OASIS_HYPERDRIVE_WHITEPAPER.md`

**Capability:**

- **Data/storage routing:** Routes **IRequest** (avatar, holon, and other data operations) to **storage providers** (MongoDB, Solana, IPFS, SQLite, etc.).
- **Features:** Auto-failover, auto–load balancing, cost-based routing, performance-based selection.
- **Scope:** “Which provider do I read/write **data** from?” — **not** “which chain do I route **trades** or **liquidity** through?”

So HyperDrive is a **universal data routing engine**, not a liquidity or trading router. The liquidity UI’s line that “HyperDrive creates a single logical pool that exists simultaneously across all chains” is **conceptual/forward-looking**; current HyperDrive code does not implement that.

### 1.4 Web4 tokens

**Location:**  
`OASIS Architecture/NextGenSoftware.OASIS.API.Core/Objects/Wallet/Web4Token.cs`  
`Interfaces/NFT/IWeb4Token.cs`

**Capability:**

- **Data model:** One Web4 token wraps many Web3 tokens (e.g. multiple chain representations of “pUSD”).
- **Use:** Multi-chain **representation** and metadata (NAV, reserves, etc.), and foundation for cross-chain product design.
- **Gap:** No logic that says “all Web3 instances of this Web4 token share one liquidity pool” or “swaps on any chain draw from the same TVL.”

So we have the **abstraction** for “one asset, many chains,” but not the **unified pool** mechanics.

---

## 2. What we do not have yet

### 2.1 Unified cross-chain liquidity pool

**Missing:**

- **One logical pool** whose TVL is shared across chains (e.g. one pool for USDC/ETH used on Ethereum, Solana, Polygon, etc.).
- **Liquidity routing** so that “swap on chain X” is executed against that shared pool (e.g. via lock-mint/burn-release, or a shared custodian/coordinator).
- **Fee aggregation** “from all chains” into one pool and one set of LP claims.
- **On-chain or coordinator logic** that implements “provide liquidity once → earn from all chains.”

The Universal Asset Bridge **liquidity UI** (`UniversalAssetBridge/frontend/src/app/liquidity/liquidity-content.tsx`) uses **mock data** and **copy** that describes this model. There is no backend, smart contract, or bridge component that currently implements it.

### 2.2 HyperDrive as “liquidity router”

**Reality:**

- HyperDrive does **not** route **trades** or **liquidity**.
- It routes **storage/data** requests. Using it as the “engine” that “creates a single logical pool across all chains” would require a **new layer** (liquidity routing, pool state, settlement) that does **not** exist in the current HyperDrive code.

### 2.3 Cross-chain AMM logic in core/bridge

**Reality:**

- The bridge does **Withdraw** (source) + **Deposit** (destination). It does **not**:
  - Maintain a “unified pool” balance,
  - Route swaps to a shared pool,
  - Allocate LP fees from multiple chains to one pool.

So we have **cross-chain transfer**, not **cross-chain AMM**.

---

## 3. Summary table

| Capability | Have it? | Where |
|------------|----------|--------|
| Cross-chain value move (atomic swap style) | ✅ Yes | CrossChainBridgeManager, IOASISBridge (Withdraw/Deposit) |
| Single-chain AMM (add/remove liquidity, swap, LP tokens) | ✅ Yes | pUSD FXPool.sol |
| HyperDrive data/storage routing | ✅ Yes | OASISHyperDrive, ProviderManager |
| Unified pool (one TVL, all chains) | ❌ No | — |
| Liquidity routing (“swap on any chain → same pool”) | ❌ No | — |
| Fee aggregation across chains into one pool | ❌ No | — |
| HyperDrive as liquidity/trade router | ❌ No | HyperDrive is data-only |

---

## 4. What would be needed for “unified cross-chain liquidity”

To match the **design** in the FAQ and liquidity UI, we’d need to add (conceptually):

1. **Unified pool model**
   - A single notion of “pool state” (reserves, LP shares) that is **consistent across chains** (e.g. via lock-mint, or a coordinator that holds canonical state).

2. **Liquidity routing layer**
   - For “swap on chain X”: either
     - Execute against a **local** pool that is **synchronized** with the canonical pool (e.g. liquidity rebalancing, or wrapped assets that map to one backing pool), or
     - Route the swap through a **coordinator** that pulls from the canonical pool and settles on chain X.

3. **Bridge + pool integration**
   - Use the **existing** bridge to move value between chains (e.g. rebalancing liquidity, or settling after a cross-chain swap).
   - Add **new** logic so that bridge flows update **pool state** and **LP entitlements** in a unified way.

4. **Fee aggregation**
   - Track fees per chain and attribute them to the **one** pool and to **LP shares** in that pool (on-chain or via a signed off-chain accounting that settles periodically).

5. **Clarified role of HyperDrive**
   - If “HyperDrive” is to be part of unified liquidity, it would need a **dedicated** use: e.g. “which chain to execute this swap on” (cost/latency) or “which replica of pool state to read/write.” That would be **new** liquidity-routing logic, not the current HyperDrive storage routing.

---

## 5. How to talk about it externally

- **Accurate today:**  
  “We have cross-chain atomic swaps (e.g. SOL↔ETH, SOL↔XRD) and a single-chain AMM (pUSD/pEUR). We’re designing unified cross-chain liquidity pools where one pool serves all chains, and we’re building toward that.”

- **Avoid implying:**  
  “We already run unified cross-chain liquidity pools” or “HyperDrive today routes liquidity/trades across chains.”

- **FAQ / Liquidity UI:**  
  Treat `CROSS_CHAIN_LIQUIDITY_FAQ.md` and the Universal Asset Bridge liquidity copy as **target architecture** and **product direction**. When we add the missing pieces above, we can align the FAQ to “current capability” and call out what’s live vs. planned.

---

## 6. References in code/docs

| Topic | Files |
|-------|--------|
| Bridge / atomic swap | `CrossChainBridgeManager.cs`, `IOASISBridge.cs`, `BridgeController.cs` |
| Single-chain AMM | `pUSD/contracts/FXPool.sol`, `pUSD/test/FXPool.t.sol` |
| HyperDrive (data only) | `OASISHyperDrive.cs`, `OASIS_HYPERDRIVE_WHITEPAPER.md` |
| Unified liquidity (design only) | `UniversalAssetBridge/frontend/.../liquidity-content.tsx`, `CROSS_CHAIN_LIQUIDITY_FAQ.md` |
| Web4 token model | `Web4Token.cs`, `IWeb4Token.cs` |

---

*Assessment is based on the codebase and docs as of the date above. Implementation may change; re-check when prioritising or resourcing cross-chain liquidity work.*
