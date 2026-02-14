# NFT Mint API: Steps and Solana/Metaplex Alignment

This doc describes the steps the OASIS NFT API actually follows when minting, and how they align with Solana/Metaplex requirements so image and description show on Solscan and in wallets.

---

## 1. Steps the API actually follows

### REST entry (NftController mint-nft)

1. **Request validation** – Parse `OnChainProvider`, `OffChainProvider`, `NFTOffChainMetaType`, `NFTStandardType`, destination (SendToAddress / SendToAvatar), etc.
2. **Defaults** – If `NFTOffChainMetaType` is not provided → **defaults to `OASIS`**. If `JSONMetaDataURL` is provided but type is OASIS, it will be **overwritten** later by NFTManager.
3. **Build MintWeb4NFTRequest** – Title, Description, ImageUrl, Image (bytes), Symbol, JSONMetaDataURL, NFTOffChainMetaType, etc.
4. **Call** `NFTManager.MintNftAsync(mintRequest)`.

### NFTManager (MintNftAsync → MintNFTInternalAsync)

5. **Validate** request and resolve provider (e.g. SolanaOASIS).
6. **Image (only if `mergedRequest.Image` is not null)**  
   - If `NFTOffChainMetaType` is Pinata/IPFS/OASIS: upload image bytes to that provider and set `mergedRequest.ImageUrl`.  
   - If `NFTOffChainMetaType` is **ExternalJSONURL**: no image upload; caller must have set `ImageUrl` and `JSONMetaDataURL` already.
7. **Metadata JSON and URI**  
   - If `mergedRequest.ImageUrl` is set **or** `NFTOffChainMetaType == ExternalJSONURL`:  
     - JSON = `mergedRequest.JSONMetaData` if set, else `CreateMetaDataJson(mergedRequest, ...)` (e.g. `CreateMetaplexJson` for Solana).  
     - **Switch on NFTOffChainMetaType:**  
       - **Pinata** – Upload JSON to Pinata, set `mergedRequest.JSONMetaDataURL` = Pinata file URL.  
       - **IPFS** – Save JSON to IPFS, set `JSONMetaDataURL` = IPFS URL.  
       - **OASIS** – Save JSON to OASIS (e.g. MongoDB), set `JSONMetaDataURL` = **OASIS API URL** (e.g. `.../data/load-file/{id}`).  
       - **ExternalJSONURL** – **Do not overwrite** `JSONMetaDataURL`; only require it to be non-empty.
8. **Call provider** – `nftProviderResult.Result.MintNFTAsync(web3Request)` with `web3Request.JSONMetaDataURL` and `web3Request.ImageUrl` (and Title, Symbol, etc.).

### Solana provider (SolanaService.MintNftAsync)

9. **Validate** – RPC client, OASIS account, request not null, **JSONMetaDataURL required**.
10. **Metaplex metadata** – Build `Metadata`:  
    - `name` = request.Title (truncated to 32 chars).  
    - `symbol` = request.Symbol (truncated to 10 chars).  
    - `uri` = **request.JSONMetaDataURL** (the metadata JSON URL).  
    - `sellerFeeBasisPoints`, `creators` (provider config).
11. **Create NFT** – `metadataClient.CreateNFT(..., tokenMetadata, isMasterEdition: true, isMutable: true)` (mint + metadata account + master edition).
12. **Return** mint result (tx hash, NFT address, etc.).

So yes: the **actual NFT API** does follow the required steps (validate → resolve image/metadata URLs → pass URI to Solana → create mint + metadata + master edition). The critical part is **which URI** ends up in `JSONMetaDataURL` when it reaches Solana.

---

## 2. Why image/description didn’t show before (Telegram + OASIS type)

- We were sending **NFTOffChainMetaType.OASIS** and a pre-built **Pinata** metadata URL from the Telegram flow.
- In step 7, NFTManager **ignored** our URL: for OASIS it built its own JSON via `CreateMetaDataJson`/`CreateMetaplexJson`, saved it to OASIS, and **overwrote** `JSONMetaDataURL` with the OASIS API URL.
- Solana then stored **that** OASIS URL as the token’s metadata URI. Explorers (Solscan) and wallets fetch that URI; if it’s an internal OASIS endpoint (or different format), image/description may not show.

So the API was “following” the steps, but the **metadata URI** used on-chain was the OASIS one, not our Pinata Metaplex JSON.

---

## 3. What we need for Solana/Metaplex (Solscan + wallets)

- **On-chain**: Metaplex metadata with `name`, `symbol`, `uri` (and usually `seller_fee_basis_points`, `creators`). ✅ Solana provider does this.
- **At `uri`**: A **public** URL that returns a **single JSON object** in a Metaplex-style format, e.g.:  
  `name`, `symbol`, `description`, `image` (URL to the image), and optionally `properties` (e.g. `category`, `files`) for full compatibility.

So the “steps required” are:

1. Have a public, Metaplex-style JSON (with image + description) hosted at a URL.
2. Pass that URL as **JSONMetaDataURL** and **do not** let the API overwrite it.
3. Pass that same URL through to Solana as the metadata `uri`.

That is exactly what **NFTOffChainMetaType.ExternalJSONURL** is for: use our own metadata URL (e.g. Pinata) and skip NFTManager’s OASIS/Pinata/IPFS metadata upload and overwrite.

---

## 4. Aligning the Telegram bot with the API

- **Upload image** to Pinata → get image URL.  
- **Build Metaplex-style JSON** (name, symbol, description, image, properties with category/files).  
- **Upload that JSON** to Pinata → get metadata URL.  
- Call the mint API with:  
  - **NFTOffChainMetaType = ExternalJSONURL**  
  - **JSONMetaDataURL** = that Pinata metadata URL  
  - **ImageUrl** = image URL (for consistency; Solana uses the JSON at `uri` for display.)  
- NFTManager then leaves **JSONMetaDataURL** unchanged (step 7, ExternalJSONURL branch) and passes it to the Solana provider.  
- Solana stores it as the token’s `uri` → Solscan/wallets fetch our JSON → image and description show.

So we **are** following all the steps required in the actual NFT API; we just had to use **ExternalJSONURL** so the API uses our pre-built metadata URL instead of overwriting it with an OASIS URL.

---

## 5. REST API callers (e.g. Postman / custom apps)

- **Default** `NFTOffChainMetaType` in NftController is **OASIS** (see step 2). So if they pass `JSONMetaDataURL` but omit `NFTOffChainMetaType`, the API will **overwrite** `JSONMetaDataURL` with the OASIS metadata URL.
- For **Solana + custom/public metadata** (e.g. your own Pinata or IPFS JSON):  
  - Set **NFTOffChainMetaType = ExternalJSONURL**  
  - Set **JSONMetaDataURL** to the public Metaplex JSON URL  
  - Optionally set **ImageUrl** to the image URL (for consistency; display comes from the JSON at `uri`).

---

## 6. Summary

| Step | Who | What |
|------|-----|------|
| Validate + defaults | NftController | NFTOffChainMetaType default OASIS; can overwrite caller’s JSONMetaDataURL |
| Image URL | NFTManager | Only if `Image` bytes provided; else use caller’s ImageUrl |
| Metadata URL | NFTManager | OASIS/Pinata/IPFS → build JSON and **overwrite** JSONMetaDataURL; **ExternalJSONURL** → keep caller’s JSONMetaDataURL |
| On-chain metadata | Solana provider | name, symbol, **uri** = JSONMetaDataURL, creators, etc. |
| Display | Solscan/wallets | Fetch **uri**; expect Metaplex JSON with **image**, **description** |

So yes: we are following all the steps required in the actual NFT API; for Solana to show image and description, the token’s `uri` must point at a **public Metaplex JSON** and we must use **ExternalJSONURL** so that URI is the one we provide (e.g. Pinata), not the OASIS API URL.

---

## 7. Local build and mainnet setup (devnet + mainnet in one API)

The same API supports **devnet** and **mainnet** via cluster selection. No second deployment.

### Build and run locally

```bash
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build -f net8.0
dotnet run
```

- API: **https://localhost:5004** and **http://localhost:5003** (see `launchSettings.json`).
- Mint endpoint: `POST /api/nft/mint-nft`.

### Mainnet: new wallet and RPC

1. **Create a new Solana mainnet wallet** (keypair). For example with Node/`@solana/web3.js` or Solana CLI:
   - `solana-keygen new` (save the keypair file and note the pubkey), or generate keypair in code.
   - **Fund the wallet** with SOL on mainnet (needed for mint fees).

2. **RPC (mainnet)**  
   In `OASIS_DNA.json` → `OASIS.StorageProviders.SolanaOASIS`:
   - **MainnetConnectionString** – set to your mainnet RPC URL (e.g. Helius: `https://mainnet.helius-rpc.com/?api-key=YOUR_KEY`). Already present in repo; replace API key if needed.

3. **Wallet keys in DNA**  
   In the same `SolanaOASIS` section set:
   - **MainnetPrivateKey** – Base58 **private** key of the new mainnet wallet.
   - **MainnetPublicKey** – Base58 **public** key (wallet address).

   Devnet continues to use **PrivateKey**, **PublicKey**, and **ConnectionString** (devnet RPC).

### Choosing cluster when calling the API

- **Devnet (default):** Omit cluster or set `cluster=devnet` / `X-Solana-Cluster: devnet` / `"Cluster": "devnet"`.
- **Mainnet:** Set `cluster=mainnet` (or `mainnet-beta`) in query, header **X-Solana-Cluster**, or body **Cluster**.

If mainnet is requested but any of `MainnetConnectionString`, `MainnetPrivateKey`, or `MainnetPublicKey` are missing, the API returns an error and does not mint.

### Quick local test

- **Devnet:** `POST /api/nft/mint-nft` with your usual body (e.g. OnChainProvider = SolanaOASIS, JSONMetaDataURL, etc.). No cluster or `cluster=devnet` → uses devnet.
- **Mainnet:** Same request with `X-Solana-Cluster: mainnet` (or `?cluster=mainnet` or `"Cluster": "mainnet"`). Requires mainnet keys and RPC configured as above.
