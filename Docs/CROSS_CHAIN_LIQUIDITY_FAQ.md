# Cross-Chain Liquidity: How It Works

**Purpose:** Answer prospect/investor questions about multichain liquidity and whale-grade depth.  
**Context:** “If or when you go multichain—how do you make sure every chain has enough liquidity? Whales need thick liquidity. How will your cross-chain liquidity pools work?”

---

## Short answer

**Note:** This FAQ describes our **target architecture** for unified cross-chain liquidity. Current capability vs. planned: see [CROSS_CHAIN_LIQUIDITY_CAPABILITY_ASSESSMENT.md](./CROSS_CHAIN_LIQUIDITY_CAPABILITY_ASSESSMENT.md).

We don’t try to fill each chain with its own pool. We use **unified liquidity pools** powered by **HyperDrive**: one logical pool, one capital base, serving all chains. That gives every chain access to the *same* deep pool, so whales get thick liquidity without fragmenting TVL across 10+ chains.

---

## 1. How do you make sure every chain has enough liquidity?

**We don’t “fill each chain” separately.** Traditional multichain DeFi does:

- Chain A: separate pool, e.g. $2M TVL  
- Chain B: separate pool, e.g. $1.5M TVL  
- Chain C: separate pool, e.g. $1M TVL  
- …

So liquidity is split, and many chains stay thin.

**Our model: unified liquidity**

- **One logical pool** exists across all chains (HyperDrive).
- LPs add liquidity **once** (e.g. on Ethereum or Solana). That capital is part of the unified pool.
- Swaps on **any** chain (Ethereum, Solana, Polygon, Base, etc.) draw from that **same** pool.
- So “enough liquidity on every chain” means: the *entire* TVL is available to every chain. We don’t need to seed each chain individually; we need one deep pool that all chains use.

**In practice**

- HyperDrive routes and settles across 50+ providers.
- Auto-failover and load-balancing mean users can trade on the chain that’s fastest/cheapest, while still using the shared liquidity.
- From a liquidity perspective, “every chain” is served by the combined pool.

---

## 2. Whales need thick liquidity—how does that work?

**Thick liquidity** = large size traded with low price impact.

**Why unified pools help whales**

1. **One deep pool, not many shallow ones**  
   If total TVL is $20M in one unified pool, a $1M swap moves price less than the same $1M split across ten $2M pools. Deeper pool → less slippage → better execution for size.

2. **No per-chain fragmentation**  
   Today, a whale often has to split orders across chains or accept bad fills on thin chains. Here, the whale’s order is effectively hitting the **aggregate** depth of the unified pool, regardless of which chain they use. So “whale-ready” means making that single pool large, not filling 10 small pools.

3. **Lower impermanent loss for LPs**  
   Deeper pools have less price impact per trade, so less IL. That makes it more attractive for large LPs to add size, which in turn makes the pool thicker for whales.

4. **Price sync across chains**  
   HyperDrive keeps prices aligned across chains. When they briefly diverge, arbitrage corrects it—and those arb flows also pay fees into the same unified pool, so liquidity earns from cross-chain activity.

**What we’re building toward**

- **Unified Liquidity Pools** (see Universal Asset Bridge / HyperDrive Liquidity): “Provide liquidity once, earn from all chains.” Same idea as above: one pool, all chains.
- **Web4 pairs** (e.g. wrapped multi-chain representations) so pairs are tradeable across chains while still backing the same pool.
- **Thick liquidity** comes from onboarding large LPs into these unified pools, and from routing all volume through them so TVL compounds in one place instead of fragmenting.

---

## 3. How will your cross-chain liquidity pools work? (concrete picture)

**Logical model**

- **One pool per pair** (e.g. USDC/ETH, or pUSD/pEUR), but that pool is **global**.
- LPs deposit into that pool once (e.g. on one “home” chain or via a canonical entry point). They receive a share of the pool (e.g. LP tokens or a proportional claim).
- **HyperDrive**:
  - Exposes that pool to every supported chain.
  - Routes user swaps to the best chain (cost, speed).
  - Tracks all activity (all chains) and attributes fees to the single pool.
- So “cross-chain liquidity pool” = **one shared liquidity pool + HyperDrive making it available and coherent across chains**.

**For LPs**

- Add liquidity once → earn fees from activity on **every** chain that uses that pool.
- No need to manage 10 separate positions on 10 chains.
- Deeper total TVL → better execution for everyone, including whales.

**For traders (including whales)**

- Interact with the pool from any supported chain.
- Effective depth = full TVL of the pool, not per-chain TVL.
- Large orders are executed against that full depth, with routing and execution handled by the system.

**Technical building blocks**

- **CrossChainBridgeManager** – atomic swaps, lock/mint/burn/release, so value moves across chains safely.
- **HyperDrive** – routing, failover, load balancing, so liquidity is “one pool, many fronts.”
- **Web4 tokens** – multi-chain wrappers (e.g. one pUSD across many chains) so the same asset is tradeable everywhere and backs the same pools.

---

## 4. One-sentence summary for conversations

**“We use unified liquidity pools: one pool per pair, shared across all chains via HyperDrive, so LPs deploy once and every chain has access to the same deep liquidity—which is exactly what whales need.”**

---

## 5. Where this is documented in our stack

| Topic | Location |
|-------|----------|
| Unified pools, “earn from all chains,” whale-friendly depth | `UniversalAssetBridge/frontend/src/app/liquidity/liquidity-content.tsx` |
| HyperDrive, fragmentation, multi-chain TVL | `pangea-repo/docs/OASIS_PUSD_INTEGRATION_ANALYSIS.md` (§2) |
| Cross-chain bridge, atomic swaps | `UniversalAssetBridge/BRIDGE_ARCHITECTURE_EXPLAINED.md`, `pUSD/docs/CROSS_CHAIN_INTEGRATION.md` |
| Web4 tokens, multi-chain assets | `pangea-repo/docs/OASIS_PUSD_INTEGRATION_ANALYSIS.md` (§1), Core `Web4Token.cs` |

---

*This FAQ reflects our architecture and product direction. Implementation details (e.g. exact liquidity deployment flow, chain-by-chain rollout) may evolve as we ship.*
