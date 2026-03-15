# SAINT Token × OASIS: Features & Utility

**Context:** [Saint (SAINT) on pump.fun](https://pump.fun/coin/9VTxmdpCD9dKsJZaBccHsBwYcebGV6iCZtKA8et5pump) is the community token for the SAINTS project. OASIS already integrates it; this doc maps **existing** utility and **additional** features OASIS could bring to the token, based on the [OASIS codebase and DeepWiki](https://deepwiki.com/NextGenSoftwareUK/OASIS).

---

## What Already Exists (Current $SAINT Utility in OASIS)

| Feature | How it works |
|--------|----------------|
| **Mint gating** | Telegram `/mint`: wallet must hold ≥ `SaintTokenRequiredBalance` of $SAINT to mint a Saint NFT. Config: `SaintTokenMint`, `SaintTokenRequiredBalance`. |
| **Ordain / Baptise** | `/ordain` blesses **top holders** of a token (e.g. $SAINT); `/baptise` can target **newest investors** or top holders. With `OrdainRequireSaintHolder: true`, only wallets that **also hold $SAINT** receive the ordained NFT. |
| **Secret group** | After minting a Saint NFT, `/join_saints` (in DM) returns the SAINTS secret group link. Gating = “has minted” (via `ISaintMintRecordService`). |
| **x402 / Drip** | Revenue and “blessings” go to **Saint NFT holders who also hold $SAINT**. `GET /api/x402/saint/eligible-recipients` returns that list; drip/pool sends SOL (or future token) to them. So **holding $SAINT is required to receive payouts**. |
| **Buy link** | Insufficient-balance message includes pump.fun link to buy $SAINT. |

So today: **$SAINT = access to mint, eligibility for ordain/baptise, and eligibility for drip/revenue.**

---

## Additional Features / Utility OASIS Could Bring to $SAINT

Using OASIS’s existing systems (avatars, karma, quests, NFTs, holons, SERV/A2A), these are concrete directions.

### 1. **OASIS Avatar ↔ $SAINT (Identity & Reputation)**

- **Link wallet to avatar:** OASIS already has `ProviderWallets` per avatar (e.g. Solana). If a user links the wallet that holds $SAINT to their OASIS avatar, we can:
  - Show “$SAINT balance” and “Saint NFT count” on their **OASIS profile** (e.g. in OPORTAL or any OASIS app).
  - Use **$SAINT balance tiers** for **avatar-level benefits** (e.g. “Saint Holder” badge, or karma multiplier for SAINTS-related actions).
- **Utility:** $SAINT becomes part of **on-OASIS identity**, not only Telegram.

### 2. **Karma ↔ $SAINT (Reputation Rewards)**

- **Karma for SAINTS actions:** Already have `AddKarmaToAvatarAsync`, karma types (e.g. `LevelUp`, `HelpOtherPerson`), and karma-weighted token rewards in the tokenomics model. We could:
  - Award **karma** for: minting a Saint NFT, being ordained, holding $SAINT over time (e.g. “Saint Steward”).
  - Use **karma × $SAINT** (e.g. tiered by balance) to weight **builder/community rewards** if SAINTS has an OASIS-side reward pool.
- **Utility:** Holding and participating with $SAINT improves **reputation** inside OASIS (visible to other apps/games on OASIS).

### 3. **Quests & Missions (Proof of Participation)**

- **Quest system:** OASIS has `QuestController`, `CompleteObjective`, karma/XP on completion, and `QuestProofService`. We could:
  - Define **SAINTS quests** (e.g. “Mint a Saint NFT”, “Hold 1M $SAINT for 30 days”, “Get ordained”).
  - On completion: **karma + XP**, and optionally **proof NFT** or **badge** stored in OASIS.
- **Utility:** $SAINT and Saint NFTs become **objectives** in a broader OASIS quest layer; progress is portable across OASIS apps.

### 4. **Saint NFT as Access Control (Beyond Telegram)**

- **NFT gating in OASIS:** Per [NFT Utility Implementation Guide](https://github.com/NextGenSoftwareUK/OASIS/blob/master/Docs/NFT_UTILITY_IMPLEMENTATION_GUIDE.md), we can gate features by **NFT ownership** (by symbol, mint, or trait). We could:
  - Gate **OASIS apps/features** (e.g. OPORTAL “Saint-only” areas, or SERV agent access) to avatars whose linked wallet holds a **Saint NFT** (and optionally minimum $SAINT).
  - Use **traits** on Saint NFTs (e.g. “Message from SAINT”, “Ordained”) for tiered access.
- **Utility:** Saint NFT + $SAINT become **universal OASIS access keys**, not only Telegram.

### 5. **Memecoin → NFT “State Switch” (SAINTS Project Brief)**

- The [SAINTS project brief](https://github.com/NextGenSoftwareUK/OASIS/blob/master/SAINTS/PROJECT_BRIEF_SUMMARY.md) describes **converting memecoin into NFTs** (lock/burn token → mint NFT that records amount, timestamp, wallet). OASIS could:
  - Provide **API + flows** (e.g. “Convert $SAINT → Saint NFT” or “Convert X amount → Legend NFT”) with **holon storage** for conversion history and **avatar linkage**.
  - **Utility:** $SAINT gains a **canonical “exit with dignity”** path and provable participation history on OASIS.

### 6. **SERV / A2A (Agents & Services)**

- **Agent reputation:** A2A has agent karma and reputation NFTs. We could:
  - Allow **$SAINT holders** (or Saint NFT holders) to **pay for or tip** SERV agents in $SAINT (if OASIS supports SPL payments).
  - **Saint-gated agents:** Only respond to or prioritize requests from avatars that hold Saint NFT / $SAINT.
- **Utility:** $SAINT becomes a **payment/reputation layer** for AI agents in the OASIS ecosystem.

### 7. **Holon & Data (Saint as a Holon)**

- **Saint / SAINTS as holons:** OASIS holons are hierarchical data objects. We could:
  - Model **SAINTS community** or **Ascension Pool** as a holon; **$SAINT balance** or **Saint NFT count** could be stored or derived as holon metadata for **governance or rewards**.
- **Utility:** $SAINT and Saint NFTs become **first-class data** in OASIS for analytics, governance, and cross-app logic.

### 8. **Drip / Pool Enhancements**

- **Already designed:** [SAINTS_X402_POOL_AND_DRIP](https://github.com/NextGenSoftwareUK/OASIS/blob/master/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Docs/SAINTS_X402_POOL_AND_DRIP.md): pool pays **Saint NFT holders who hold $SAINT**. Possible additions:
  - **Weight by $SAINT balance** (already in config: `X402RevenueModel` e.g. weighted).
  - **Minimum $SAINT for drip:** `SaintDripMinBalance` so only “serious” holders get blessings.
  - **Dashboard:** Use OASIS avatar + wallet linkage to show “Your drip eligibility” and “Next distribution” in an OASIS app.
- **Utility:** Clear, transparent **revenue share** tied to both NFT and token.

---

## Summary Table

| Category | Existing | Possible addition |
|----------|----------|-------------------|
| **Access** | Mint gate, ordain/baptise filter, /join_saints | OASIS-wide NFT/balance gating; avatar badges |
| **Revenue** | x402 drip to Saint NFT + $SAINT holders | Weight by $SAINT; dashboard; pool governance |
| **Identity** | Wallet in Telegram flow | Avatar ↔ wallet; $SAINT on profile; karma |
| **Reputation** | — | Karma for SAINTS actions; quest completion; proof NFTs |
| **Interop** | — | Memecoin→NFT conversion API; SERV payments / Saint-gated agents |
| **Data** | Mint/ordain/baptise records | SAINTS as holon; eligibility and history in OASIS |

---

## Suggested Next Steps

1. **Define “Saint holder” in OASIS:** By wallet only (current) vs by **avatar** (after linking wallet to avatar). Enables profile, karma, and cross-app gating.
2. **Add 1–2 SAINTS quests** (e.g. mint, hold) and expose completion + karma in avatar detail.
3. **Expose eligible-recipients (or a “my eligibility”)** in an OASIS frontend so holders see drip status.
4. **Document** the SAINT token mint and config (`SaintTokenMint`, etc.) in a single “SAINT in OASIS” runbook for operators and partners.

This keeps $SAINT’s utility **grounded in existing OASIS capabilities** (avatars, karma, quests, NFTs, x402, holons, SERV) while extending it from “Telegram + drip” to **identity, reputation, and interoperability** across the platform.
