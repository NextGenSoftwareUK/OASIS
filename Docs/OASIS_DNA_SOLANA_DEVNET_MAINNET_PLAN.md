# OASIS DNA: Solana devnet vs mainnet

## Goal

- **Default:** Solana stays on **devnet** (minting, balances, etc.) so you don’t spend mainnet SOL in development.
- **When needed:** Use **mainnet** for **read-only token metadata** (e.g. Solscan memecoin → NFT in Telegram bot) without switching the whole provider to mainnet.

## DNA layout (OASIS_DNA.json)

Under `StorageProviders.SolanaOASIS`:

| Key | Purpose | Example |
|-----|---------|--------|
| **ConnectionString** | Active RPC for minting, sends, balances (usually **devnet**). | `https://devnet.helius-rpc.com/?api-key=...` |
| **MainnetConnectionString** | Optional. When set, **token metadata lookup only** (metadata-by-mint, Telegram memecoin import) uses this mainnet RPC. | `https://mainnet.helius-rpc.com/?api-key=...` or leave `""` |

- Keep **ConnectionString** as devnet for normal use.
- Set **MainnetConnectionString** to a mainnet RPC when you need Solscan/memecoin metadata (e.g. Telegram “paste Solscan link”). Leave it empty to use devnet for everything.

JSON doesn’t support comments, so keep a snippet elsewhere for quick copy/paste.

### Copy-paste: mainnet RPC for metadata only

When you need Solscan/memecoin metadata (e.g. Telegram “paste Solscan link”), set **MainnetConnectionString** in `OASIS_DNA.json` under `StorageProviders.SolanaOASIS`:

```json
"MainnetConnectionString": "https://mainnet.helius-rpc.com/?api-key=YOUR_MAINNET_KEY"
```

Leave it as `""` if you don’t need mainnet token metadata.

### Example snippet (mainnet – for reference only)

When you want to switch to mainnet for everything (e.g. production), you could use:

```json
"ConnectionString": "https://mainnet.helius-rpc.com/?api-key=YOUR_MAINNET_KEY"
```

For **metadata-only mainnet** (recommended for dev):

```json
"ConnectionString": "https://devnet.helius-rpc.com/?api-key=YOUR_DEVNET_KEY",
"MainnetConnectionString": "https://mainnet.helius-rpc.com/?api-key=YOUR_MAINNET_KEY"
```

## Code behaviour

1. **OASISDNA** – `SolanaOASISSettings` has optional **MainnetConnectionString**.
2. **Solana provider** – Still built with **ConnectionString** only (devnet or mainnet). New method **LoadOnChainNFTDataWithRpcAsync(mint, rpcUrl)** uses the given RPC for a single metadata read.
3. **TokenMetadataByMintService** – If **MainnetConnectionString** is set, it uses that RPC for metadata-by-mint (and thus Telegram memecoin import). Otherwise it uses the default provider (devnet).
4. **Minting / sends** – Always use **ConnectionString** (unchanged). So you can keep devnet for minting and only add mainnet for metadata.

## When to set MainnetConnectionString

- You use the Telegram bot “paste Solscan link” (memecoin → NFT) and the token is on **mainnet**.
- You call **GET /api/nft/metadata-by-mint** for mainnet tokens (e.g. from MCP).

## When to leave it empty

- Everything is on devnet (no Solscan/mainnet token metadata needed).
- You’ve set **ConnectionString** to mainnet (metadata lookup will use the same RPC).

## Summary

| Scenario | ConnectionString | MainnetConnectionString |
|----------|------------------|--------------------------|
| Dev only (default) | devnet RPC | `""` |
| Dev + Solscan/memecoin metadata | devnet RPC | mainnet RPC |
| Full mainnet | mainnet RPC | `""` or same mainnet RPC |

This gives you a commented-out style “mainnet RPC available to switch to when required” via **MainnetConnectionString**, without changing the default devnet behaviour.
