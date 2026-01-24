# x402 Drip / Vesting: Set Amount from a Designated Fund Over Time

## The Ask

Higher-tier NFTs (e.g. Founder Pass, Pioneer Pass) should have **x402 integrated so they have value**. In addition to revenue from **usage payments**, we want: **a designated fund pays a set amount to the NFT (and thus to holders) over time** — i.e. scheduled drips or vesting.

---

## How x402 Works Today (Reactive Only)

Current x402 is **event‑driven on payment received**:

1. **User pays** (e.g. SOL) to the NFT’s `treasuryWallet` (or a known treasury) when using a feature.
2. **OASIS (or your service) detects** that payment.
3. **Webhook** is sent to the NFT’s `paymentEndpoint` with `{ signature, amount, nftTokenAddress, nftSymbol, treasury, revenueModel, ... }`.
4. **The endpoint** (or OASIS): gets NFT holders → splits by `revenueModel` (equal / weighted / creator-split) → sends SOL to each holder.

There is **no built‑in notion** of:

- A **designated fund** that is separate from “payments for using the NFT”
- **Scheduled** transfers (e.g. monthly) from that fund to holders
- **Vesting** (cap total over time, or stop after N periods)

So: **out of the box, x402 does not “program” a set amount from a designated fund over time.** It can be **added** in a way that reuses x402’s distribution logic.

---

## Yes, It’s Possible: Two Parts

To support “NFT receives a set amount from a designated fund over time” you need:

1. **Drip / vesting terms**  
   - Which fund (wallet), how much per period, how often, and optional cap/duration.

2. **A Drip Service (or OASIS extension)**  
   - Runs on a schedule, reads those terms, and performs **the same kind of distribution as x402** (holders + `revenueModel`), but **sourcing SOL from the designated fund** instead of from a user payment.

x402’s **`paymentEndpoint`** and **`revenueModel`** can stay as‑is for **usage‑based** revenue. Drip reuses the **distribution rules** (who gets what share); only the **trigger** (schedule) and **source of funds** (designated fund) change.

---

## Design Options

### Option A: `dripConfig` in NFT `x402Config` (Recommended)

Store drip terms in the same place as x402, so one NFT can have:

- **Usage‑based x402:** `paymentEndpoint`, `revenueModel`, `treasuryWallet` — when users pay to use the NFT.
- **Drip:** `dripConfig` — when the Drip Service pays from a designated fund on a schedule.

Example `x402Config`:

```json
{
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://api.oasisweb4.com/api/x402/revenue/foundernft",
    "revenueModel": "equal",
    "treasuryWallet": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
    "dripConfig": {
      "enabled": true,
      "sourceFundWallet": "DESIGNATED_FUND_SOLANA_WALLET",
      "amountPerPeriod": 0.5,
      "period": "monthly",
      "currency": "SOL",
      "startDate": "2025-02-01",
      "capTotal": 6,
      "capPeriods": 12
    },
    "metadata": {
      "distributionFrequency": "realtime",
      "revenueSharePercentage": 100,
      "creatorSplitPercentage": 10
    }
  }
}
```

**Fields:**

| Field | Meaning |
|-------|---------|
| `dripConfig.enabled` | If `true`, Drip Service includes this NFT in scheduled runs. |
| `sourceFundWallet` | Solana wallet of the **designated fund**. SOL is sent **from** here to holders. |
| `amountPerPeriod` | SOL to distribute in each drip (before split by `revenueModel`). |
| `period` | `"weekly"` \| `"monthly"` \| `"quarterly"`. Drips run at period boundaries. |
| `startDate` | First drip on/after this date (ISO). |
| `capTotal` | (Optional) Stop after this many SOL total. |
| `capPeriods` | (Optional) Stop after this many periods. |

`revenueModel` and `creatorSplitPercentage` (if any) from the parent `x402Config` apply to **both** usage payments and drip.

**Mint / MCP:**  
- Today: `X402Enabled`, `X402PaymentEndpoint`, `X402RevenueModel`, `X402TreasuryWallet`.  
- Add: `X402DripEnabled`, `X402DripSourceFundWallet`, `X402DripAmountPerPeriod`, `X402DripPeriod`, `X402DripStartDate`, `X402DripCapTotal`, `X402DripCapPeriods` (or a single `X402DripConfig` JSON).  
- These are written into `MetaData.x402Config.dripConfig` when minting.

---

### Option B: Drip Registry (Outside NFT Metadata)

Drip terms live in a **registry** (DB, holon, or config) keyed by `nftId` / `nftTokenAddress` / `nftSymbol`:

```json
{
  "nftSymbol": "FOUNDERPASS",
  "nftTokenAddress": "...",
  "sourceFundWallet": "...",
  "amountPerPeriod": 0.5,
  "period": "monthly",
  "startDate": "2025-02-01",
  "capTotal": 6,
  "capPeriods": 12,
  "x402PaymentEndpoint": "https://...",
  "revenueModel": "equal"
}
```

**Pros:**  
- No change to NFT metadata or mint API.  
- Admin can add/remove/edit drips without re‑minting.

**Cons:**  
- Drip and NFT are decoupled; need to keep `revenueModel` etc. in sync with the NFT’s `x402Config` if you want them to match.

---

## Drip Service: What It Does

The **Drip Service** is a scheduled job (cron, Azure Functions, etc.) that:

1. **Discovery**  
   - **Option A:** Query NFTs (e.g. from OASIS) where `MetaData.x402Config.dripConfig.enabled === true`.  
   - **Option B:** Read Drip Registry.

2. **Eligibility**  
   For each NFT in this run’s window (e.g. “this month” for `period: "monthly"`):

   - `now >= startDate`
   - `(capPeriods == null || periodsRun < capPeriods)` and `(capTotal == null || totalDistributed < capTotal)`
   - `amountThisPeriod = min(amountPerPeriod, capTotal - totalDistributed)` if `capTotal` is set.

3. **Fund check**  
   - Get SOL balance of `sourceFundWallet` (Solana RPC).  
   - If `balance < amountThisPeriod`, skip (and optionally alert). Log and continue to next NFT.

4. **Holders and splits**  
   - Resolve NFT’s mint/token (from metadata or registry).  
   - Get list of holder wallets (same logic as x402: on‑chain or OASIS `load-all-nfts-for_avatar`–style indexing by mint).  
   - Compute each holder’s share from `revenueModel` (equal / weighted / creator-split), reusing the same math as the x402 `paymentEndpoint` implementation.

5. **Execute transfers**  
   - Build Solana tx(s): from `sourceFundWallet` → each holder for their share.  
   - Sign with the key that controls `sourceFundWallet` (or via OASIS/your custody).  
   - Submit; on success, persist `lastDripAt`, `periodsRun`, `totalDistributed` (in DB or in `dripConfig` / registry).

6. **Optional: notify x402 `paymentEndpoint`**  
   - `POST` to `paymentEndpoint` with a **drip payload**, e.g.:

     ```json
     {
       "source": "drip",
       "amount": 0.5,
       "nftSymbol": "FOUNDERPASS",
       "nftTokenAddress": "...",
       "revenueModel": "equal",
       "sourceFundWallet": "...",
       "periodId": "2025-02",
       "recipients": 10,
       "amountPerRecipient": 0.05,
       "timestamp": 1738368000
     }
     ```

   - The existing `paymentEndpoint` can:  
     - Ignore `source: "drip"` (distribution already done by Drip Service), or  
     - Use it only for **logging / audit / “revenue history”** so both usage payments and drips appear in one place.

7. **Idempotency**  
   - Key runs by `(nftId, periodId)`. If the job runs twice for the same period, `periodsRun` / `lastDripAt` should prevent double‑distribution.

---

## Designated Fund

- **Wallet:** A Solana wallet used only as the **Beta Rewards Fund** or **Founder Pool**.  
- **Funding:**  
  - Manual: admin/multisig tops it up.  
  - Or: TreasuryManager / your own logic moves SOL from product revenue or another treasury into this wallet.  
- **Control:** The Drip Service (or OASIS) needs the ability to **send SOL from** this wallet. That implies:  
  - Private key or HSM in a secure env, or  
  - OASIS `send_sol` / equivalent with a “system” or “drip” avatar that holds that wallet.  
- **Per‑NFT funds (optional):** You can use one shared fund for all drips, or separate wallets per NFT/collection. The design above uses one `sourceFundWallet` per NFT/dripConfig; a shared fund is just one wallet reused in many configs.

---

## Reusing x402 Distribution Logic

The **distribution step** (holders + `revenueModel`) should be the same for:

- **User payment → `paymentEndpoint`:** treasury receives SOL; endpoint gets holders, splits, and sends from that treasury (or from a pot that received the payment).  
- **Drip:** Drip Service gets holders, applies the same `revenueModel`, but sends **from** `sourceFundWallet`.

So:

- **If** the `paymentEndpoint` is implemented as a library or service (e.g. `X402DistributionService.Distribute(nft, amount, sourceWallet)`), the Drip Service can call that with `sourceWallet = sourceFundWallet` and `amount = amountThisPeriod`.  
- **If** the endpoint is only a webhook handler, the Drip Service implements (or shares) the same “get holders + compute splits + send SOL” logic. The `paymentEndpoint` can stay focused on “payment received” events; drip is a separate code path that reuses the math.

`revenueModel` and `creatorSplitPercentage` can be read from:

- `x402Config` (Option A), or  
- The Drip Registry (Option B), ideally mirroring the NFT’s `x402Config`.

---

## Summary: What You Need to Build

| Piece | Purpose |
|-------|---------|
| **`dripConfig` (or registry)** | Store: `sourceFundWallet`, `amountPerPeriod`, `period`, `startDate`, optional `capTotal` / `capPeriods`. |
| **Mint / MCP** | Optional: `X402Drip*` params so `dripConfig` is written into NFT metadata at mint. |
| **Drip Service** | Cron: discover eligible NFTs, check fund balance, get holders, compute splits, send SOL from `sourceFundWallet`, update `lastDripAt` / `periodsRun` / `totalDistributed`, optionally POST drip payload to `paymentEndpoint` for audit. |
| **Designated fund** | Solana wallet; funded by admin or another treasury; Drip Service has authority to send from it. |
| **`paymentEndpoint` (optional)** | Understand `source: "drip"` and treat it as audit-only, or ignore. |

---

## Example: Founder Pass

- **Usage x402:** Users pay 0.1 SOL to use a “Founder-only” feature → SOL goes to `treasuryWallet` → `paymentEndpoint` distributes to holders (e.g. `equal`).  
- **Drip:**  
  - `dripConfig`: `sourceFundWallet = "BetaRewardsFund..."`, `amountPerPeriod = 0.5` SOL, `period = "monthly"`, `startDate = "2025-02-01"`, `capTotal = 6` (over 12 months).  
  - Each month, Drip Service sends 0.5 SOL from that fund to Founder Pass holders (same `revenueModel`). After 6 SOL total, drip stops.

So: **yes, you can program x402‑style payments so the NFT receives a set amount from a designated fund over time**, by adding **drip/vesting config** and a **Drip Service** that reuses x402’s distribution rules and uses the designated fund as the source of funds.
