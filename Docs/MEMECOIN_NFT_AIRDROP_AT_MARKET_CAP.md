# Memecoin NFT airdrop at market-cap milestone

**Goal:** When a memecoin’s market cap reaches a target level, **mint an NFT** (using that token’s image and metadata) and **send one to each of the top N token holders**.

This doc is a **planning and implementation reference** you can return to. It builds on the existing [Solana memecoin → NFT flow](SOLANA_MEMECOIN_TO_NFT_SETUP.md).

---

## 1. Overview

| Step | What happens |
|------|----------------|
| **Trigger** | Detect when the memecoin’s market cap crosses a threshold (e.g. $1M). |
| **Snapshot** | Get the list of top token holders at (or near) that time. |
| **Mint** | Create one NFT per holder (or one “edition” NFT and send copies). |
| **Send** | Transfer each NFT to the corresponding holder’s wallet. |

**Prerequisites:**

- Memecoin **mint address** (Solana SPL token).
- **Target market cap** (e.g. USD) and **holder count** (e.g. top 100).
- Existing OASIS flow for **memecoin → NFT** (metadata + image from token; see [SOLANA_MEMECOIN_TO_NFT_SETUP](SOLANA_MEMECOIN_TO_NFT_SETUP.md)).
- Solana RPC (mainnet if token is mainnet) with enough capacity for batch mint/send.

---

## 2. Trigger: “Market cap hit X”

**Options:**

| Approach | Pros | Cons |
|----------|------|------|
| **Scheduled job (cron)** | Simple; reuses existing APIs. | Polling delay; need price + supply source. |
| **Webhook / event** | Near real-time. | Requires external service (e.g. Birdeye, Helius webhook) that emits on price/mcap. |
| **Manual / admin** | Full control; good for testing. | Not automatic. |

**Data needed:**

- **Price:** From an oracle or API (e.g. Birdeye, Jupiter, DexScreener) for the token mint.
- **Supply:** Circulating or total supply from chain or API.
- **Market cap** = price × supply (or use “market cap” directly if the API provides it).

**Implementation sketch:**

- **Option A – Cron:** Every N minutes, call price API for the mint; compute mcap; if ≥ threshold and not already executed, run the airdrop pipeline.
- **Option B – Webhook:** Register a webhook (e.g. Helius “token price change” or Birdeye alert) that hits your API; validate mcap and run pipeline once (idempotent).
- **Option C – Admin:** POST `/api/admin/airdrop-memecoin-nft` with `mint`, `marketCapTarget`, optional `holderCount`; service checks current mcap and, if satisfied, runs pipeline.

Store **“airdrop done”** per (mint, target) so you never double-send (e.g. DB or config).

---

## 3. Snapshot: Top token holders

**Goal:** Ordered list of wallet addresses and (optionally) balances for the memecoin mint at trigger time.

**Options:**

| Source | Notes |
|--------|--------|
| **Helius** | [Token holders by mint](https://docs.helius.dev/solana-apis/token-api/get-token-holders); paginated; need mainnet RPC if token is mainnet. |
| **Solana RPC** | `getProgramAccounts` for the token program filtered by mint; heavy and rate-limited; use only for small sets or dev. |
| **Birdeye / DexScreener** | Some APIs expose “top holders”; check their docs. |

**Implementation:**

- Call holder API for the **memecoin mint**.
- Sort by balance descending; take top **N** (e.g. 100).
- Optional: filter out burn/mint authority, CEX wallets, or wallets below a min balance.
- Store snapshot (addresses + balances) for idempotency and support (e.g. “who received the airdrop”).

---

## 4. Mint + send (batch)

**Mint:**

- Reuse existing **memecoin → NFT** path:
  - Resolve metadata (name, symbol, image, description) via **TokenMetadataByMintService** or `GET /api/nft/metadata-by-mint?mint=...`.
  - Use the same metadata (and image URI) for every recipient so the NFT is visually/thematically the “memecoin milestone” NFT.
- **Mint strategy:**
  - **Option 1 – One NFT per holder:** Mint N separate NFTs (same metadata, different owners). Simple; each holder has a unique token ID.
  - **Option 2 – Metaplex Certified Collection / print:** One “master” NFT; “print” copies to each holder. Fewer mint txs; depends on your Metaplex/collection setup.

**Send:**

- For each holder in the snapshot, transfer the corresponding NFT to their **wallet address** (the same address that holds the memecoin).
- Use existing OASIS/ONODE NFT transfer flow; batch in chunks to respect RPC rate limits and avoid timeouts.
- **Gas / rent:** Ensure the minting wallet has enough SOL for all mints and transfers.

**Idempotency:**

- Persist “airdrop run” with mint + market-cap target + snapshot (e.g. list of addresses). Before minting, check that this (mint, target) hasn’t already been executed.
- Optional: store per-recipient status (pending / sent / failed) for retries and support.

---

## 5. Components to add (high level)

| Component | Purpose |
|-----------|--------|
| **Market-cap check** | Service or API that, given a mint and target, returns “above threshold” or not (using price + supply or mcap API). |
| **Holder snapshot** | Service that, given a mint and N, returns top N holder addresses (and optionally balances). |
| **Airdrop pipeline** | Orchestrator: trigger → snapshot → mint (using TokenMetadataByMintService + existing mint) → batch send; plus idempotency and logging. |
| **Trigger** | Cron job, webhook handler, or admin endpoint that invokes the pipeline when conditions are met. |
| **Config** | Per-memecoin or global: mint, market-cap target, holder count, optional filters (min balance, exclude list). |

---

## 6. Configuration sketch

Example structure (env or config DB):

- **MemecoinMint:** Solana address of the SPL token.
- **MarketCapTargetUsd:** e.g. `1_000_000`.
- **TopHolderCount:** e.g. `100`.
- **AlreadyExecuted:** flag or table keyed by (mint, target) to prevent duplicate runs.
- **Price/Mcap source:** API key and base URL (e.g. Birdeye, Helius).
- **RPC:** Mainnet RPC for holder fetch and for mint/send if token is mainnet (see [SOLANA_MEMECOIN_TO_NFT_SETUP](SOLANA_MEMECOIN_TO_NFT_SETUP.md) re mainnet vs devnet).

---

## 7. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| **Double airdrop** | Idempotency key (mint + market-cap target); persist “executed” before sending. |
| **Wrong holders** | Snapshot at trigger time; optional delay (e.g. “1 hour after mcap hit”) to reduce front-running. |
| **RPC rate limits** | Batch mints/sends in chunks; backoff and retries. |
| **Price manipulation** | Use TWAP or multiple oracles; or “manual confirm” for first campaigns. |
| **SOL / rent** | Ensure minting wallet funded; estimate tx count × fee and monitor. |

---

## 8. References

- [Solana memecoin → NFT setup](SOLANA_MEMECOIN_TO_NFT_SETUP.md) – metadata lookup, DNA mainnet/devnet, Telegram bot, API/MCP.
- [MEMECOIN_TO_NFT_FROM_SOLSCAN](MEMECOIN_TO_NFT_FROM_SOLSCAN.md) – API and flow for converting a Solscan token to NFT.
- OASIS **TokenMetadataByMintService** – in-process metadata by mint (used by NftController and Telegram flow).
- Helius: [Token API](https://docs.helius.dev/solana-apis/token-api) (holders); [Webhooks](https://docs.helius.dev/webhooks-and-websockets) if using event-based trigger.
- Birdeye / DexScreener – price and possibly “top holders” for Solana tokens.

---

*Document you can return to for implementing “mint memecoin NFT and airdrop to top holders at market-cap milestone”.*
