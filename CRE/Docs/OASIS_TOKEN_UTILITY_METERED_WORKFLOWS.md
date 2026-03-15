# OASIS Token Utility: Metered Workflow Execution

**Date:** February 2026  
**Author:** Max Gershfield / OASIS  
**Purpose:** Advisor, investor, and partner-ready explanation of the OASIS token utility model

---

## The Problem With Most Crypto Tokens

Most tokens exist for one of two reasons:

- **Speculation** — buy and hold, hope the price goes up
- **Governance** — hold tokens to vote on proposals

Neither creates *ongoing demand*. People buy once and sit. Price goes up until it doesn't. When the hype fades, so does the token.

A **utility token** is different. It creates demand every time the product is *used*. The more the platform runs, the more tokens get consumed. Usage = demand. That's a business model, not a bet.

OASIS has that model — built into the architecture.

---

## How Metered Workflow Execution Works

When a developer defines a workflow on OASIS CRE — for example:

```
Quest Complete → Award Karma → Mint NFT → Write Proof → Distribute Revenue
```

Each step in that workflow executes against real OASIS infrastructure: HolonManager, QuestController, NftController, X402, HyperDrive. Each step has a cost, proportional to the compute and storage it uses.

Before each step executes, the WorkflowEngine deducts a small amount of OASIS platform tokens from the executing avatar's balance:

| Step | Action | Token Cost |
|------|--------|-----------|
| 1 | Award karma | 0.1 |
| 2 | Mint NFT (hits Solana) | 0.5 |
| 3 | Write proof holon | 0.1 |
| 4 | Distribute revenue (x402 drip) | 0.2 |
| 5 | Notify avatar | 0.05 |
| **Total** | | **0.95 tokens per execution** |

Every time that workflow runs — for any avatar, in any game, triggered by any event — tokens are consumed. If the workflow runs 1,000 times a day, 950 tokens are consumed that day. From that workflow alone.

---

## Why This Creates Sustained Demand

### Usage scales with integrations

Every developer or product that builds on OASIS runs workflows. Every workflow execution consumes tokens. The more OASIS is integrated into other products, the more workflows run, the more tokens are consumed — *automatically*, without any marketing effort.

- **DOOM / Quake / IsoCity** running quest completion workflows = tokens consumed per player session
- **SAINTS Telegram bot** minting NFTs = tokens consumed per mint
- **Mitchell's PrivacyMage agents** running BRAID reasoning workflows = tokens consumed per reasoning step
- **Pangea / Launchboard** tokenising cap table entries = tokens consumed per tokenisation
- **Covia orchestrating OASIS holons** = tokens consumed per workflow execution

Each integration is a new stream of continuous token consumption.

### It's proportional to value delivered

Heavier steps (minting an NFT on Solana, running a BRAID reasoning graph with an LLM) cost more than lightweight steps (writing a holon to MongoDB, sending a notification). Developers pay in proportion to what they use. This is fair, predictable, and easy to reason about.

### It doesn't require belief in the token

Developers don't need to speculate on the token price to use the platform. They buy tokens to run workflows, the same way they buy AWS credits to run Lambda functions. As long as the platform is useful, there is demand.

---

## The Closest Analogies

**AWS Lambda** charges per function invocation and per 100ms of compute. The more your app runs, the more AWS earns. OASIS metered workflows are exactly this — except developers pay in OASIS tokens instead of dollars, and the tokens are consumed (reducing supply) rather than going to a single company.

**Chainlink LINK** is consumed every time a smart contract requests a price feed from a Chainlink oracle. Every DeFi protocol using Chainlink = continuous LINK consumption. This is why LINK has sustained demand across multiple market cycles — it is tied to platform usage, not speculation. OASIS tokens are the equivalent for the Web2/Web3/AI layer.

**Ethereum gas** is consumed with every transaction on Ethereum. No one speculates on gas — they buy ETH to do things. The more activity on Ethereum, the more ETH is consumed as gas. Same model, applied to OASIS workflows.

---

## Tiered Access vs Pay-Per-Step

These two models are complementary, not competing.

| Model | How it works | Best for |
|-------|-------------|---------|
| **Tiered balance gate** | Hold X tokens to access the platform at a given tier (already designed in `MEMECOIN_NFT_MINTER_BUSINESS_PLAN.md`) | Developers, community members, long-term holders |
| **Pay-per-step metering** | Tokens deducted per workflow step at execution time | Production workloads, enterprise integrations, high-volume workflows |

A developer onboards via the tiered model (hold tokens → unlock access tier). Once building, their production workflows consume tokens per execution. Both mechanisms drive demand from different angles — access and usage.

---

## What Already Exists in the Codebase

This is not a new idea that needs new infrastructure. The pieces are already built:

- **`SubscriptionMiddleware`** — already gates API calls by subscription or credits balance
- **`WalletController.GetTokenBalance`** — already checks avatar's token balance for any contract on any chain
- **`TokenGateMiddleware` pattern** — already designed, documented, and ready to implement
- **`HolonManager`** — every workflow step already writes/reads holons; the cost deduction is one additional call before each step
- **`WorkflowEngine`** (Phase 1 of OASIS CRE) — the executor that calls `DeductWorkflowStepCost(avatarId, stepType)` before running each step

The WorkflowEngine calls a single method before each step:

```csharp
var costResult = await TokenMeterService.DeductStepCostAsync(
    avatarId,
    stepType,         // determines cost tier
    providerType      // e.g. SolanaOASIS costs more than MongoDBOASIS
);

if (costResult.IsError || !costResult.Result.Sufficient)
    return InsufficientTokenBalance(stepId);

// proceed with step execution
```

That is the entire new code required to make every workflow step metered. The rest is configuration: a cost table mapping step types to token amounts.

---

## The Revenue Flow

Token consumption can be structured in multiple ways. The simplest:

```
Developer buys OASIS tokens
    ↓
Tokens deducted per workflow step (burned or to treasury)
    ↓
Reduced supply → upward price pressure over time
    ↓
More demand from new developers → more consumption
```

Or with a revenue share built in:

```
Tokens deducted per workflow step
    ↓
70% burned (deflationary)
    20% to treasury (funds development)
    10% to workflow creator (if using a community workflow)
```

The community workflow revenue share (10% to whoever published the workflow) is a direct incentive to build reusable workflows and publish them to the OASIS Workflow Library. This creates the marketplace network effect: more published workflows → more integrations → more consumption → more creator rewards → more incentive to publish.

---

## The One-Sentence Version

> "Every time a developer runs a workflow on OASIS — whether it's a game minting an NFT, an AI agent completing a task, or a DAO distributing revenue — it consumes platform tokens proportional to the compute used. Usage creates demand. The more developers build on OASIS, the more tokens get consumed."

---

## What This Unlocks Strategically

**For investors:** Token demand is tied to a measurable, auditable metric — workflow executions per day. Not sentiment. Not speculation. A number that grows as integrations grow.

**For developers:** Pay for what you use. No upfront commitment. Works exactly like cloud compute billing.

**For token holders:** Long-term value accrual tied to platform growth. The more OASIS is used, the more tokens are consumed, the scarcer the supply.

**For partners (Covia, Mitchell, Pangea, OpenSERV):** Their integrations automatically contribute to token demand. They're not just using OASIS — they're participating in its economy.

---

## Supporting References

- [OASIS CRE Plan](OASIS_CRE_PLAN.md) — the workflow runtime this model sits on top of
- [Memecoin NFT Minter Business Plan](MEMECOIN_NFT_MINTER_BUSINESS_PLAN.md) — existing token gate middleware design
- [OASIS Business Model Executive Summary](BUSINESS_MODEL_EXECUTIVE_SUMMARY.md) — broader commercial context
- [SAINT Token OASIS Features and Utility](../../Docs/LFG/SAINT_Token_OASIS_Features_And_Utility.md) — token utility patterns already live

---

*Document v1.0 · February 2026 · OASIS / NextGen Software*
