# NFT API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Core Endpoints](#core-endpoints)
- [GeoNFT Endpoints](#geonft-endpoints)
- [Collections & Search](#collections--search)
- [Provider Utilities](#provider-utilities)

## Overview

The NFT API provides comprehensive NFT management services for the OASIS ecosystem. It handles NFT creation, minting, trading, and analytics with support for multiple standards, real-time updates, and advanced security features.
The WEB4 NFT API is backed by `NftController` and `NFTManager`.  
It works with **Web3 NFTs**, **Web4 NFTs**, and **GeoSpatial NFTs**, and supports both on-chain and off-chain metadata.

All endpoints live under:

```http
Base: /api/nft
```

All responses are wrapped in `OASISResult<T>`.

---

## Core Endpoints

- **Load Web4 NFT by ID**
  - `GET /api/nft/load-nft-by-id/{id}`
  - `GET /api/nft/load-nft-by-id/{id}/{providerType}/{setGlobally}`

- **Load Web4 NFT by On-Chain Hash**
  - `GET /api/nft/load-nft-by-hash/{hash}`
  - `GET /api/nft/load-nft-by-hash/{hash}/{providerType}/{setGlobally}`

- **Load All Web4 NFTs For Avatar**
  - `GET /api/nft/load-all-nfts-for_avatar/{avatarId}`
  - `GET /api/nft/load-all-nfts-for_avatar/{avatarId}/{providerType}/{setGlobally}`

- **Load All Web4 NFTs For Mint Wallet Address**
  - `GET /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}`
  - `GET /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}`

- **Load All Web4 NFTs (Admin)**
  - `GET /api/nft/load-all-nfts`
  - `GET /api/nft/load-all-nfts/{providerType}/{setGlobally}` (requires `AvatarType.Wizard`)

- **Send NFT**
  - `POST /api/nft/send-nft`
  - Body: `NFTWalletTransactionRequest`
  - Calls `NFTManager.SendNFTAsync(avatarId, ISendWeb4NFTRequest)`

- **Mint Web4 NFT**
  - `POST /api/nft/mint-nft`
  - Body: `MintNFTTransactionRequest`
  - Calls `NFTManager.MintNftAsync(IMintWeb4NFTRequest, ...)`

- **Remint NFT**
  - `POST /api/nft/remint-nft`
  - Body: `RemintWeb4NFTRequest`
  - Calls `NFTManager.RemintNftAsync(IRemintWeb4NFTRequest, ...)`

- **Import Web3 NFT → Web4**
  - `POST /api/nft/import-web3-nft`
  - Body: `IImportWeb3NFTRequest`
  - Calls `NFTManager.ImportWeb3NFTAsync`

- **Import Web4 NFT from JSON file**
  - `POST /api/nft/import-web4-nft-from-file/{importedByAvatarId}/{fullPathToOASISNFTJsonFile}`
  - Optional `providerType` route param

- **Import Web4 NFT object**
  - `POST /api/nft/import-web4-nft/{importedByAvatarId}`
  - Body: `IWeb4NFT`

- **Export Web4 NFT to JSON file**
  - `POST /api/nft/export-web4-nft-to-file/{oasisNFTId}/{fullPathToExportTo}`

- **Export Web4 NFT object**
  - `POST /api/nft/export-web4-nft`
  - Body: `IWeb4NFT` + `fullPathToExportTo`

- **Load Web3 NFT by ID**
  - `GET /api/nft/load-web3-nft-by-id/{id}`
  - Optional `providerType` route param

- **Load Web3 NFT by On-Chain Hash**
  - `GET /api/nft/load-web3-nft-by-hash/{onChainNftHash}`
  - Optional `providerType` route param

- **Load All Web3 NFTs For Avatar**
  - `GET /api/nft/load-all-web3-nfts-for-avatar/{avatarId}`
  - Optional: `parentWeb4NFTId`, `providerType`

- **Load All Web3 NFTs For Mint Address**
  - `GET /api/nft/load-all-web3-nfts-for-mint-address/{mintWalletAddress}`
  - Optional: `parentWeb4NFTId`, `providerType`

- **Load All Web3 NFTs (Admin)**
  - `GET /api/nft/load-all-web3-nfts`

---

## GeoNFT Endpoints

- **Load All GeoNFTs For Avatar**
  - `GET /api/nft/load-all-geo-nfts-for-avatar/{avatarId}`
  - `GET /api/nft/load-all-geo-nfts-for-avatar/{avatarId}/{providerType}/{setGlobally}`

- **Load All GeoNFTs For Mint Wallet Address**
  - `GET /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}`
  - `GET /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}`

- **Load All GeoNFTs (Admin)**
  - `GET /api/nft/load-all-geo-nfts`
  - `GET /api/nft/load-all-geo-nfts/{providerType}/{setGlobally}`

- **Place an existing Web4 NFT as GeoNFT**
  - `POST /api/nft/place-geo-nft`
  - Body: `PlaceGeoSpatialNFTRequest`

- **Mint & Place GeoNFT in one step**
  - `POST /api/nft/mint-and-place-geo-nft`
  - Body: `MintAndPlaceGeoSpatialNFTRequest`

---

## Collections & Search

- **Create Web4 NFT Collection**
  - `POST /api/nft/create-web4-nft-collection`
  - Body: `ICreateWeb4NFTCollectionRequest`

- **Search Web4 NFTs**
  - `GET /api/nft/search-web4-nfts/{searchTerm}/{avatarId}`
  - Optional: `searchOnlyForCurrentAvatar`, `providerType`

- **Search GeoNFTs**
  - `GET /api/nft/search-web4-geo-nfts/{searchTerm}/{avatarId}`

- **Search Web4 NFT Collections**
  - `GET /api/nft/search-web4-nft-collections/{searchTerm}/{avatarId}`

---

## Provider Utilities

- **Get NFT Provider from ProviderType**
  - `GET /api/nft/get-nft-provider-from-provider-type/{providerType}`
  - Returns `IOASISNFTProvider` for the given `ProviderType`.

> For full DTO definitions, see `NextGenSoftware.OASIS.API.Core.Interfaces.NFT.*` and  
> `NextGenSoftware.OASIS.API.Core.Objects.NFT.*`.


