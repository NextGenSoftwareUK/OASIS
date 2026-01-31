# Memecoin Lookup → Image & Metadata → NFT

**Yes.** You can lookup a memecoin by its **Solana mint address**, read its image and metadata, and use those to mint an NFT.

---

## 1. How memecoins (and pump.fun) store metadata

- **Pump.fun** and most Solana memecoins are **SPL tokens** with **Metaplex Token Metadata**.
- Each **mint address** has an associated **Metadata Account** (PDA) that holds:
  - `name` – token name  
  - `symbol` – token symbol  
  - `uri` – URL to an **off-chain JSON** (often IPFS or Arweave)
- That **URI** points to a JSON file that usually contains:
  - `image` – URL of the token/logo image  
  - `name`, `symbol`, `description` – can duplicate or extend on-chain  
  - `attributes`, `properties`, etc. (optional)

So: **mint address → Metaplex metadata (name, symbol, uri) → fetch uri (JSON) → get image + description.**

---

## 2. Lookup: get name, symbol, and metadata URI

### Option A: Use existing OASIS Solana provider (on-chain)

The **SOLANAOASIS** provider already has a path that reads Metaplex metadata by mint address:

- **`LoadNftAsync(string address)`** in `SolanaService.cs`  
  - Uses `MetadataAccount.GetAccount(rpcClient, mintPublicKey)`  
  - Returns a result that includes **Name**, **Symbol**, **Url** (the metadata URI).

So for any **mint address** (memecoin or NFT), you get:

- `Name`  
- `Symbol`  
- `Url` = metadata JSON URI  

If this is exposed via ONODE (e.g. “get NFT/metadata by mint”), the same API can be used for **memecoin lookup**: pass the memecoin’s mint address and you get name, symbol, and URI. If not, you’d add a thin API that calls the same Solana provider method.

### Option B: External API (Moralis, etc.)

- **Moralis** (and similar) can return token metadata by Solana mint, including:
  - name, symbol, logo, metadata URI  
- Useful if you don’t want to hit Solana RPC directly or want a single “token info” response that already includes logo URL.

Either way, the **minimum you need** from the lookup step is:

- **name** (for NFT title)  
- **symbol** (for NFT symbol)  
- **metadata URI** (so you can fetch the JSON and get **image** and **description**)

---

## 3. Resolve image (and description) from metadata URI

- **GET** the **URI** (e.g. `https://ipfs.io/ipfs/...` or `https://arweave.net/...`).
- Parse the response as JSON.
- Read:
  - **`image`** (or `image_url`, etc.) → this is the **image URL** to use for the NFT.  
  - **`description`** (optional) → use as NFT description.

If the JSON doesn’t have `image`, some APIs (e.g. Moralis) may return a **logo** URL directly from the token metadata response; you can use that as the NFT image instead.

---

## 4. Convert to NFT (OASIS mint)

OASIS **mint-nft** already accepts:

- **Title** ← memecoin name (from step 2)  
- **Symbol** ← memecoin symbol (from step 2)  
- **ImageUrl** ← from JSON `image` (step 3) or logo from API  
- **Description** ← from JSON `description` (step 3) or empty  
- **JSONMetaDataURL** ← either:
  - the **same URI** (reuse the memecoin’s metadata JSON), or  
  - a **placeholder** (e.g. `https://jsonplaceholder.typicode.com/posts/1`) if you don’t need to host new JSON; the important part for display is **ImageUrl** and the on-chain name/symbol.

So the **conversion** step is a single call to your existing **mint-nft** (or equivalent) with the resolved fields.

---

## 5. End-to-end flow (summary)

| Step | Action | Output |
|------|--------|--------|
| 1 | **Lookup** memecoin by **mint address** (Solana provider or Moralis) | `name`, `symbol`, `uri` |
| 2 | **GET** `uri` → parse JSON | `image` URL, `description` (optional) |
| 3 | **Mint NFT** via OASIS (Title, Symbol, ImageUrl, Description, JSONMetaDataURL) | New NFT with memecoin image and metadata |

So: **yes, there is a way:** lookup by mint → take image and metadata from the URI JSON (and/or API) → pass them into your existing mint flow to “convert” that memecoin into an NFT.

---

## 6. Where to implement

- **Backend/API:**  
  - Add an endpoint, e.g. **`GET /api/nft/metadata-by-mint?mint={address}`** (or use existing if one exists), that returns name, symbol, uri (and optionally fetches the JSON and returns image + description).  
  - Add **`POST /api/nft/mint-from-memecoin`** (or similar) that: accepts mint address + optional overrides → runs steps 1–3 → returns mint result.  
- **MCP / scripts:**  
  - A tool or script that: input = mint address; steps 1–3; output = mint tx / NFT link.  
- **Frontend (meme coin → NFT):**  
  - User pastes mint or pump.fun link; frontend calls the new “lookup” and “mint-from-memecoin” APIs so the user gets an NFT with the memecoin’s image and metadata without using Cursor.

If you want, next step can be a concrete **API spec** (request/response) for “lookup by mint” and “mint-from-memecoin”, or a small **reference implementation** (e.g. Node/TS or C#) that does steps 1–3 using the existing Solana provider and mint-nft.
