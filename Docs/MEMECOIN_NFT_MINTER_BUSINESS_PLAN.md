# Memecoin NFT Minter – Business plan & token mechanics

**Summary:** A business plan around the **memecoin → NFT** technology: Pump.fun hackathon angle, OpenServ partnership, token launch (Base or Solana), use cases (e.g. private memecoin chat rooms), and **how your token could work**—including balance-gated access so the Avatar API only allows use when a user holds a minimum token balance (no staking required, but staking can be an option).

---

## 1. Positioning

### 1.1 What you have

- **Memecoin → NFT in one flow:** From a Solscan link (or mint address), resolve token image + metadata and mint an NFT. Live today via Telegram (PinkMinter), API, and MCP.
- **OASIS Avatar API:** Identity, wallets, NFT ownership, subscriptions/credits. Existing middleware already gates by subscription or credits.
- **OpenServ partnership:** A2A agents, workflows; OpenServ tokens on **Base** or **Solana**.
- **Pump.fun relevance:** Memecoins, Solana, “launch and let the market decide”—your tool fits the same ecosystem.

### 1.2 One-line pitch

**“Turn any memecoin into an NFT in one click—then use that NFT to open private rooms, gate chats, and reward holders.”**

---

## 2. Pump.fun Build in Public Hackathon

**Source:** [Pump.fun Build in Public Hackathon](https://hackathon.pump.fun/#header)

- **$3M total**, 12 winners, **$250k per project**.
- **Three stages:** Idea (0→1), MVP (1→10), Product (10→100).
- **Expectations:** Launch a token, build in public, let **market participation** determine success; share updates (social, streams, pump.fun); rapid iteration.

**Fit for the hackathon:**

| Hackathon theme | Your angle |
|-----------------|------------|
| Launch a coin | Launch **your** token with OpenServ (Base or Solana); token gates the NFT minter and future utilities. |
| Build in public | Ship memecoin→NFT + “NFT = room key” in public; stream demos, post on X/Telegram. |
| Market decides | Degens adopt the minter for private memecoin rooms; usage and TVL/token demand = validation. |
| Memecoin ecosystem | Tool is **for** Pump.fun / Solscan memecoins: paste link → NFT → use NFT as access key. |

**Recommendation:** Position as **MVP stage** (working product: minter + one clear utility, e.g. “mint NFT → open private room”). Apply with a short video + link to live Telegram bot and/or API; emphasize OpenServ partnership and token design (balance-gated access).

---

## 3. OpenServ partnership & token (Base vs Solana)

- **OpenServ:** You’re launching a token **with** them; their tokens are on **Base** or **Solana**.
- **Implication:** Your “platform token” can be on **Base** (EVM, broad wallet support) or **Solana** (same chain as most memecoins and your current minter). Both are viable.

| Chain | Pros | Cons |
|-------|------|------|
| **Solana** | Same chain as PinkMinter, Pump.fun, Solscan memecoins; one wallet for token + NFT minting. | Need Solana token balance check in API (see §5). |
| **Base** | Lower gas, strong OpenServ presence; existing OASIS `GetTokenBalance` for Ethereum/Base. | User might hold Base token + Solana wallet for NFTs (two chains). |

**Practical choice:** If OpenServ’s standard is Base, launch on Base and use **balance check on Base** to gate API access. If they support Solana, Solana token keeps everything on one chain for degens. You can also support **both** (e.g. “hold X on Base **or** Y on Solana”) with two balance checks.

---

## 4. Use cases

### 4.1 Primary: Private memecoin chat rooms (degens)

- **Flow:** User pastes Solscan link → mints NFT (image + metadata of the memecoin) → that NFT acts as **room key** for a private chat (Telegram, OpenServ, or your own room product).
- **Value:** Only holders of the “room key” NFT (or the underlying memecoin) get in; creates exclusive, token-gated spaces per memecoin.
- **Monetisation:** Access to the **minting tool** or to **room creation** is gated by **your platform token** (balance or staking—see §5).

### 4.2 Other utility (to expand over time)

- **Airdrops at market cap:** Mint milestone NFT and airdrop to top holders (see [MEMECOIN_NFT_AIRDROP_AT_MARKET_CAP](MEMECOIN_NFT_AIRDROP_AT_MARKET_CAP.md)).
- **NFT-gated perks:** Discord/Telegram roles, alpha channels, or rewards for holding the memecoin NFT.
- **Merch / proof-of-community:** Memecoin NFT as proof of early support; link to merch or IRL events.
- **OpenServ agents:** “Mint memecoin NFT” and “check room access” as agent actions; token-gated so only your token holders can use the agent.

---

## 5. How your token could work

Two main patterns: **balance-gated access** (hold X tokens) and **staking** (lock tokens for higher access or rewards). You can offer both.

### 5.1 Option A: Balance-gated access (no staking)

- **Rule:** User must hold **≥ N** platform tokens (in a linked wallet) to use the memecoin NFT minter (and optionally other features).
- **Check:** Before allowing mint/room creation, the **Avatar API** (or a dedicated middleware) checks the user’s **token balance** for the platform token contract on the chosen chain (Base or Solana).
- **User experience:** Connect wallet → API checks balance once per session or per action → if balance &lt; threshold, return a clear error (“Hold at least X tokens to use this feature”) and optional link to buy.

**Pros:** Simple, no lock-up, easy to explain. **Cons:** No direct “lock” of supply; demand depends on utility and speculation.

### 5.2 Option B: Staking for access or tiers

- **Rule:** User **stakes** a minimum amount of platform tokens to unlock the minter (or higher tiers, e.g. more mints per month, or room creation).
- **Check:** API checks **staked balance** (from your staking contract or indexer) instead of (or in addition to) liquid balance.
- **User experience:** Stake on your app → tier unlocks → use minter/rooms.

**Pros:** Reduces circulating supply; aligns long-term holders with the product. **Cons:** More dev (staking contract, UI), and some users prefer not to lock.

### 5.3 Option C: Hybrid

- **Tier 1 – Free / low:** Hold X tokens (balance check only); limited mints or read-only.
- **Tier 2 – Full:** Hold more **or** stake Y tokens; full minter + create rooms.
- **Tier 3 – Pro / partner:** Stake Z; higher limits, airdrop tools, or revenue share.

You can start with **balance-only** (Option A) and add staking (Option B/C) once the token and staking contract are live.

### 5.4 Tiered balance tiers (1B token supply)

Below is the **tiered option** for balance-gated access. **Token supply: 1,000,000,000 (1B).** Balances are in **platform tokens** (liquid hold in wallet).

| Tier | Min balance (hold) | Name | What you get |
|------|-------------------|------|----------------|
| **0** | 0 | **Free / Read-only** | Metadata lookup only (e.g. `GET /api/nft/metadata-by-mint`). No minting, no room creation. |
| **1** | **10,000** | **Community** | Mint memecoin → NFT (e.g. 1–3 mints per day). Join NFT-gated rooms. Use Telegram minter with limit. |
| **2** | **100,000** | **Builder** | Higher mint limit (e.g. 10 mints/day). Create 1 private room (NFT = room key). API/MCP access. |
| **3** | **500,000** | **Pro** | Higher limits (e.g. 30 mints/day), multiple rooms. Priority support. Future: airdrop-at-mcap tools. |
| **4** | **2,500,000** | **Partner** | Highest limits or unlimited mints; room creation caps raised. Revenue share or alpha features. |

**Balance thresholds (1B supply):**

- **Tier 0:** `0` (no token required for public metadata).
- **Tier 1 (Community):** `10,000` – low barrier, “try the product” (~0.001% of supply).
- **Tier 2 (Builder):** `100,000` – serious users, room creators (~0.01% of supply).
- **Tier 3 (Pro):** `500,000` – power users, future airdrop tools (~0.05% of supply).
- **Tier 4 (Partner):** `2,500,000` – partners, revenue share (~0.25% of supply).

**Optional – staking equivalents (same tier, lock instead of hold):**

| Tier | Min **staked** (alternative) | Note |
|------|------------------------------|------|
| 1 | 5,000 staked | Slightly lower threshold for locking. |
| 2 | 50,000 staked | |
| 3 | 250,000 staked | |
| 4 | 1,250,000 staked | |

**Config shape (for TokenGateMiddleware):**

```json
"TokenGate": {
  "Enabled": true,
  "ContractAddress": "0x...",
  "Chain": "Base",
  "TotalSupply": 1000000000,
  "Tiers": [
    { "MinBalance": 0,       "TierId": "free",     "MintsPerDay": 0,   "CanCreateRoom": false },
    { "MinBalance": 10000,   "TierId": "community", "MintsPerDay": 3,   "CanCreateRoom": false },
    { "MinBalance": 100000,  "TierId": "builder", "MintsPerDay": 10,  "CanCreateRoom": true, "MaxRooms": 1 },
    { "MinBalance": 500000,  "TierId": "pro",     "MintsPerDay": 30,  "CanCreateRoom": true, "MaxRooms": 5 },
    { "MinBalance": 2500000, "TierId": "partner", "MintsPerDay": -1,  "CanCreateRoom": true, "MaxRooms": -1 }
  ]
}
```

Use the user’s balance to resolve their tier (highest tier where `balance >= MinBalance`), then enforce `MintsPerDay`, `CanCreateRoom`, and `MaxRooms` per request.

---

## 6. Can the Avatar API check token balance before allowing use?

**Yes.** The codebase already supports:

1. **Subscription / credits middleware**  
   `SubscriptionMiddleware` runs after JWT; it has access to the **avatar** (from `context.Items["Avatar"]`). It currently checks subscription or **credits balance** and blocks or allows the request. The same pattern can be used for **token balance**.

2. **Wallet + token balance API**  
   `WalletController` exposes **`GET /api/wallet/token/balance`** with `avatarId`, `tokenContractAddress`, and `providerType` (e.g. `EthereumOASIS`, `BaseOASIS`). So the backend can already resolve an avatar’s wallets and query token balance for a given contract on a given chain.

**Implementation outline:**

- **New middleware (e.g. `TokenGateMiddleware`)**  
  - Runs on selected routes (e.g. `/api/nft/mint`, `/api/nft/metadata-by-mint`, or a new “create room” endpoint).  
  - After JWT, reads `avatar` from `context.Items["Avatar"]`.  
  - Loads the avatar’s wallet(s) for the chain where your platform token lives (Base or Solana).  
  - Calls the same logic as `GetTokenBalance` (or a new `ITokenBalanceService`) for your **platform token contract** and **minimum required amount**.  
  - If `balance >= minRequired`, call `_next(context)`. Otherwise return `403` with a message like “Insufficient platform token balance. Hold at least X tokens to use this feature.”

- **Config:**  
  - `TokenGate:Enabled`, `TokenGate:ContractAddress`, `TokenGate:Chain` (Base vs Solana), `TokenGate:MinimumBalance`, and optionally `TokenGate:SkipPaths` (e.g. health, public metadata).

- **Solana:**  
  - If your token is on Solana, you need a Solana **token balance** call (SPL token) for the avatar’s Solana wallet. The existing `WalletController` token balance is ERC-20–oriented; add a Solana path (e.g. use Helius/RPC to get SPL balance by mint address) and expose it to the middleware.

So: **yes, the Avatar API can check token balance and allow or deny use** without staking. Staking can be added later by checking a “staked balance” source instead of (or in addition to) liquid balance.

---

## 7. Token mechanics – summary table

| Mechanism | How it works | Use when |
|-----------|----------------|----------|
| **Balance gate** | Hold ≥ N tokens in wallet; API checks before mint/room creation. | First launch; simple UX; no lock-up. |
| **Tiered balance** | Multiple tiers (e.g. 1k / 10k / 50k / 250k); each tier gets different limits (mints/day, rooms). See **§5.4**. | You want clear upgrade path and limits per tier. |
| **Staking** | Lock tokens in a staking contract; API checks staked balance for tier. | You want reduced circulating supply and stronger holder alignment. |
| **Credits + token** | Keep existing credits/subscription; add “or hold X tokens” as alternative. | You want to keep current paying users and add token holders as a second tier. |

**Recommended first step:**  
- **Tiered balance-gated access** (see §5.4, **1B supply**): Free (metadata only) → Community (10k) → Builder (100k) → Pro (500k) → Partner (2.5M).  
- **Config:** Platform token contract + chain (Base or Solana) + `TokenGate.Tiers` (min balance and limits per tier).  
- **Implementation:** New middleware that resolves avatar → wallet(s) → token balance → tier; enforce `MintsPerDay`, `CanCreateRoom`, `MaxRooms` per tier. Add Solana SPL balance if token is on Solana.

---

## 8. OpenServ + token alignment

- **OpenServ tokens (Base or Solana):** If your platform token **is** the OpenServ token or is launched with them, then “hold our token to use the minter” also drives demand for their ecosystem.
- **Agent workflows:** Register an OpenServ agent that “mints memecoin NFT” or “checks room access”; gate that agent so only avatars that pass the **token balance check** can call it. Same middleware idea: before executing the workflow, resolve avatar → wallet → token balance → allow or deny.
- **Distribution:** OpenServ’s distribution channels can help with token reach; your utility (minter + rooms) gives the token a clear use case for Pump.fun / degen users.

---

## 9. Plan summary

| Item | Action |
|------|--------|
| **Pump.fun hackathon** | Apply (MVP stage); lead with “memecoin → NFT → private room key”; show Telegram bot + token plan; build in public. |
| **Token chain** | Decide with OpenServ: Base vs Solana (or both); align with their launch. |
| **Access model** | Start with **balance-gated** access (hold X tokens to use minter/rooms); add staking later if desired. |
| **Avatar API** | Add **token-gate middleware** that checks platform token balance (and chain) before allowing protected routes; implement Solana SPL balance if token is on Solana. |
| **Use cases** | Ship “NFT = room key” first; document airdrop-at-mcap and other utilities as roadmap. |

This gives you a single document to return to for positioning, hackathon, OpenServ, token mechanics, and the technical answer: **yes, the Avatar API can check token balance and allow use only when it’s at a certain level**, with or without staking depending on how you design tiers.

---

## 10. References

- [Pump.fun Build in Public Hackathon](https://hackathon.pump.fun/#header)
- [SOLANA_MEMECOIN_TO_NFT_SETUP](SOLANA_MEMECOIN_TO_NFT_SETUP.md) – Minter setup, DNA, Telegram, API/MCP
- [MEMECOIN_NFT_AIRDROP_AT_MARKET_CAP](MEMECOIN_NFT_AIRDROP_AT_MARKET_CAP.md) – Airdrop-at-mcap plan
- [NFT_UTILITY_IMPLEMENTATION_GUIDE](NFT_UTILITY_IMPLEMENTATION_GUIDE.md) – NFT gating patterns
- ONODE `SubscriptionMiddleware` – Credits/subscription gating pattern
- ONODE `WalletController.GetTokenBalance` – Avatar + token contract + provider (Base/Ethereum); extend for Solana SPL for balance-gate
- A2A/OpenServ: `A2A/docs/OPENSERV_BRIDGE_DESIGN.md`, `A2A/docs/A2A_OPENSERV_INTEGRATION.md`
