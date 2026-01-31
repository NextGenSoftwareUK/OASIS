# Meme Coin → NFT Service Brief

**Audience:** Meme coin creator (no Cursor).  
**Goals:** Fast, simple, customizable way to “convert” meme coins to NFTs. Optional connection to pump.fun.

---

## 1. What “convert meme coins to NFTs” means

- **Primary:** Create **NFT collectibles** (image + metadata on Solana) that are **tied to or themed around** their meme coin (same name, symbol, branding, optional link to token).
- **Optional:** Pre-fill or suggest NFT metadata from their **pump.fun token** (name, symbol, image) so one click gets them 80% of the way there.

So the service = **simple web flow: image + name/symbol/description → mint NFT**, with an optional “Import from pump.fun” step.

---

## 2. How it could work (no Cursor)

### Option A: Dedicated mini-site (recommended)

- **URL:** e.g. `nft.oasisweb4.one/meme` or `memecointonft.oasisweb4.one`
- **Flow:**
  1. **Sign in** – OASIS avatar (email/password or wallet connect). One account, all future mints tied to it.
  2. **Image**
     - **Upload** – drag & drop or paste.
     - **Or AI** – one text prompt (Glif), then “Use this image.”
  3. **Details** – Title, Symbol, Description (all editable; symbol default e.g. from pump.fun if used).
  4. **Optional: Connect pump.fun**
     - Input: **pump.fun token mint address** (or bond curve address).
     - We fetch name, symbol, image (if available) and **pre-fill** the form; user can change anything.
  5. **Mint** – One button → OASIS `mint-nft` (Solana). Show tx link + NFT link when done.

- **Why fast & simple:** 3–5 steps, no dev tools, no Cursor. Customizable because every field is editable and we support both upload and AI image.

### Option B: Embedded in existing OASIS Portal

- Add a **“Meme coin → NFT”** section or tab in the current portal (e.g. next to NFT Mint Studio).
- Same flow as above: (optional pump.fun import) → image → details → mint.
- Reuse existing auth and NFT Mint Studio logic; add only “Import from pump.fun” and a simplified layout/copy for meme creators.

### Option C: API + no-code frontend

- You run a **small backend** (e.g. Next.js API or Cloudflare Worker) that:
  - Authenticates user (OASIS or API key).
  - Optionally calls pump.fun (or Moralis) to resolve token → name, symbol, image URL.
  - Calls OASIS `/api/nft/mint-nft` (and optionally Glif for “prompt → image”).
- Partner or you build a **simple no-code frontend** (e.g. Typeform + Zapier, or a single landing page with form) that POSTs to your API. Still no Cursor for the meme coin creator.

---

## 3. Customizable

- **Image:** Upload **or** AI prompt (Glif); user picks.
- **Metadata:** Title, symbol, description fully editable; optional attributes/traits later.
- **Pump.fun:** Import is a convenience; user can replace name, symbol, and image.
- **Chain:** Start with Solana only (pump.fun native); later add “mint on Ethereum/Polygon” if needed.
- **Quantity:** “Number to mint” (e.g. 1 for single collectible, or small batch).

---

## 4. Pump.fun connection (practical)

- **What we need:** For a given **token mint address** (or bonding curve), get: **name**, **symbol**, **image URL** (if any).
- **Ways to get it:**
  - **Moralis** – Solana token metadata by mint (name, symbol, logo, etc.). [Docs](https://docs.moralis.com/web3-data-api/solana/tutorials/get-pump-fun-token-metadata).
  - **pump.fun–style APIs** – e.g. `GET https://api.pumpfunapis.com/api/bonding-curve/{mint}` for bonding curve state; some providers expose token metadata (name, symbol, image) for the same mint.
  - **Metaplex / on-chain metadata** – if the pump token has Metaplex metadata, we can read name, symbol, URI (image) from chain or a Solana RPC + Metaplex SDK.
- **Product flow:**
  - User pastes **pump.fun token address** (mint or link from pump.fun).
  - We resolve → name, symbol, image URL.
  - We pre-fill the NFT form; user edits and mints. Optionally store “linked token” in NFT metadata (e.g. `memeCoinMint`) for provenance/UX later.

---

## 5. Tech already in OASIS

- **Auth:** Avatar login (username/password or future wallet connect).
- **Mint:** `POST /api/nft/mint-nft` (Solana SPL, ImageUrl, Title, Symbol, Description, etc.) – used by NFT Mint Studio and others.
- **Image:** Glif integration (prompt → image URL) in MCP; same backend can be called from a web app.
- **Portal:** NFT Mint Studio and AI NFT assistant show we already have “form → mint” and “prompt → image → mint” in the codebase; we can reuse or simplify for meme creators.

**To add:**

- Small **pump.fun token resolver** (mint → name, symbol, image) using one of the options above.
- **“Meme coin → NFT”** UI path (mini-site or portal section) with optional “Import from pump.fun” step.
- Copy and defaults tuned for “meme coin creator” (e.g. “Paste your pump.fun token”, “Your NFT will link to this token”).

---

## 6. Summary

- **Service:** Web-based “meme coin → NFT” flow: optional pump.fun import → image (upload or AI) → edit details → mint on Solana. No Cursor required.
- **Fast & simple:** One URL, 3–5 steps, one “Mint” button.
- **Customizable:** Full control over image and metadata; pump.fun only pre-fills.
- **Pump.fun:** Add a resolver (mint → name, symbol, image) and an “Import from pump.fun” step so it’s one-paste and edit, then mint.

If you want, next step can be a one-page **UX flow** (screens/wireframes) or a **technical checklist** (endpoints, env vars, pump.fun resolver API choice).
