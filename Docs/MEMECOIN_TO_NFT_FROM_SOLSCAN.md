# Convert a Memecoin to an NFT (from Solscan)

You can take any Solana token/memecoin (e.g. from a [Solscan](https://solscan.io) token page) and mint it as an NFT using its **image and metadata**.

---

## 1. Get the mint address

From a Solscan token URL, the last path segment is the **mint address**:

- **URL:** `https://solscan.io/token/8Jx8AAHj86wbQgUTjGuj6GTTL5Ps3cqxKRTvpaJApump`
- **Mint:** `8Jx8AAHj86wbQgUTjGuj6GTTL5Ps3cqxKRTvpaJApump`

---

## 2. Lookup metadata (name, symbol, image, description)

**API (no auth):**

```http
GET /api/nft/metadata-by-mint?mint=8Jx8AAHj86wbQgUTjGuj6GTTL5Ps3cqxKRTvpaJApump
```

**MCP:** Call `oasis_get_token_metadata_by_mint` with `mint` = the token address.

**Response:**  
`name`, `symbol`, `uri` (metadata JSON URL), `image` (resolved from that JSON), `description`.

- The endpoint uses the Solana provider to read **Metaplex metadata** for the mint (name, symbol, uri).
- It then **fetches the URI** (JSON) and returns the `image` and `description` from it.
- If the token has no Metaplex metadata (e.g. some raw SPL tokens), the call returns an error.

---

## 3. Mint the NFT

Use the existing **mint-nft** flow with the metadata from step 2:

| Mint request field   | Use from metadata-by-mint |
|----------------------|---------------------------|
| **Title**            | `name`                    |
| **Symbol**           | `symbol`                  |
| **ImageUrl**         | `image`                   |
| **Description**      | `description`             |
| **JSONMetaDataURL**  | `uri` (token’s existing metadata JSON) |

- **Option A – Reuse token URI:**  
  Pass `JSONMetaDataURL` = `uri`. The new NFT will point at the same metadata JSON (and image) as the memecoin. Solscan/wallets will show the image and description.

- **Option B – Your own metadata:**  
  Upload your own Metaplex-style JSON (e.g. to Pinata) with the same `image` and set `JSONMetaDataURL` to that URL.

**MCP:** After `oasis_get_token_metadata_by_mint`, call `oasis_mint_nft` with:

- `Title` = response.name  
- `Symbol` = response.symbol  
- `ImageUrl` = response.image  
- `Description` = response.description  
- `JSONMetaDataURL` = response.uri  

(Minting still requires auth: use `oasis_authenticate_avatar` first.)

---

## 4. End-to-end (summary)

| Step | Action |
|------|--------|
| 1 | Take mint from Solscan URL (e.g. `8Jx8AAHj86wbQgUTjGuj6GTTL5Ps3cqxKRTvpaJApump`). |
| 2 | `GET /api/nft/metadata-by-mint?mint=...` or MCP `oasis_get_token_metadata_by_mint`. |
| 3 | Mint NFT with `Title`/`Symbol`/`ImageUrl`/`Description`/`JSONMetaDataURL` from step 2. |

So: **yes, you can** get the image and metadata from a Solscan token link and create an NFT that includes them.

---

## 5. Via Telegram bot (PinkMinter)

You can do the same flow **inside Telegram**:

1. Send **`/mint`** to your bot (or `/start mint`).
2. Optionally log in with OASIS (or type `skip` to mint as the bot).
3. When asked for an image, **paste a Solscan token URL** (e.g. `https://solscan.io/token/8Jx8AAHj86wbQgUTjGuj6GTTL5Ps3cqxKRTvpaJApump`) or the **mint address**.
4. The bot looks up the token’s metadata and image, then asks for your **Solana wallet address**.
5. Confirm with **YES** to mint. The NFT uses the token’s image and metadata (same as the memecoin).

**Config:** If the webhook is behind a proxy/ngrok, set `TelegramNftMint:OasisApiBaseUrl` (e.g. `https://your-domain.com`) in appsettings so the bot can call the metadata-by-mint API. Otherwise it uses the request host.
