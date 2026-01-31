# Solana memecoin → NFT: setup and config

This doc explains how to **convert a Solscan token/memecoin into an NFT** (image + metadata) and how **OASIS_DNA** Solana config (devnet vs mainnet) affects it.

---

## What you can do

- **From a Solscan token link** (e.g. `https://solscan.io/token/8Jx8AAHj86wbQgUTjGuj6GTTL5Ps3cqxKRTvpaJApump`), resolve the token’s **name, symbol, image, description** and mint an **NFT** with that data.
- **In Telegram:** paste the Solscan URL (or mint address) when the bot asks for an image; it loads the token and asks for your wallet, then mints the NFT.
- **Via API/MCP:** `GET /api/nft/metadata-by-mint?mint=...` and then call mint-nft with the returned fields.

Most Solscan token links are **mainnet**. If your OASIS is on **devnet**, you must set **MainnetConnectionString** in DNA so metadata lookup uses mainnet (see below).

---

## 1. OASIS_DNA Solana config

In **OASIS_DNA.json** (the one your API loads at runtime), under **`StorageProviders.SolanaOASIS`**:

| Key | Purpose |
|-----|--------|
| **ConnectionString** | RPC used for **minting, sends, balances**. Keep as **devnet** for development. |
| **MainnetConnectionString** | Optional. When set, **only token metadata lookup** (Solscan/memecoin → NFT) uses this **mainnet** RPC. |

### Recommended for dev (devnet minting + mainnet metadata)

```json
"SolanaOASIS": {
  "WalletMnemonicWords": "",
  "PrivateKey": "...",
  "PublicKey": "...",
  "ConnectionString": "https://devnet.helius-rpc.com/?api-key=YOUR_DEVNET_KEY",
  "MainnetConnectionString": "https://mainnet.helius-rpc.com/?api-key=YOUR_MAINNET_KEY"
}
```

- **ConnectionString** = devnet → minting stays on devnet.
- **MainnetConnectionString** = mainnet → Solscan/memecoin metadata works (Telegram “paste Solscan link”, `GET /api/nft/metadata-by-mint`).

### When to set MainnetConnectionString

- You use **Telegram “paste Solscan link”** and the token is on mainnet.
- You call **GET /api/nft/metadata-by-mint** or **MCP** `oasis_get_token_metadata_by_mint` for mainnet tokens.

### When to leave it empty

- Everything is on devnet and you don’t need mainnet token metadata.
- You already use **ConnectionString** = mainnet (metadata lookup uses the same RPC).

### Quick reference

| Scenario | ConnectionString | MainnetConnectionString |
|----------|------------------|--------------------------|
| Dev only | devnet RPC | `""` |
| Dev + Solscan/memecoin metadata | devnet RPC | mainnet RPC |
| Full mainnet | mainnet RPC | `""` or same mainnet RPC |

---

## 2. Telegram bot (PinkMinter)

1. Send **`/mint`** (or `/start mint`).
2. Log in with OASIS or type **`skip`** to mint as the bot.
3. When asked for an image, **paste a Solscan token URL** or the **mint address** (32–44 chars).
4. Bot loads the token (name, symbol, image) and asks for your **Solana wallet address**.
5. Confirm with **YES** to mint. The NFT uses the token’s image and metadata.

**Requirement:** If the token is on **mainnet**, set **MainnetConnectionString** in OASIS_DNA (see §1). Restart the ONODE WebAPI after changing DNA.

---

## 3. API and MCP

### Metadata lookup (no auth)

```http
GET /api/nft/metadata-by-mint?mint=8Jx8AAHj86wbQgUTjGuj6GTTL5Ps3cqxKRTvpaJApump
```

**MCP:** `oasis_get_token_metadata_by_mint` with `mint` = token address.

**Response:** `name`, `symbol`, `uri`, `image`, `description`.

### Mint the NFT

Use the **mint-nft** API or **oasis_mint_nft** with:

- **Title** = `name`
- **Symbol** = `symbol`
- **ImageUrl** = `image`
- **Description** = `description`
- **JSONMetaDataURL** = `uri`

(Minting requires auth; MCP: use `oasis_authenticate_avatar` first.)

---

## 4. Troubleshooting

| Issue | What to check |
|-------|----------------|
| “Could not load token metadata” in Telegram | 1) Token has **Metaplex metadata** (e.g. pump.fun tokens). 2) If token is **mainnet**, set **MainnetConnectionString** in OASIS_DNA and restart the API. |
| Metadata works in browser/MCP but not in Telegram | Restart the ONODE WebAPI so it loads the latest code and DNA. |
| Want to switch everything to mainnet | Set **ConnectionString** to a mainnet RPC. **MainnetConnectionString** can stay `""` or match. |

---

## 5. Related docs

- **OASIS_DNA_SOLANA_DEVNET_MAINNET_PLAN.md** – Detailed plan for devnet vs mainnet and code behaviour.
- **MEMECOIN_TO_NFT_FROM_SOLSCAN.md** – Step-by-step flow for API/MCP and Telegram.
