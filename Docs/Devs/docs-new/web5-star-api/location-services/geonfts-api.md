# GeoNFTs API

## Overview

The GeoNFTs API provides location-based NFT management for the STAR ecosystem: place, mint-and-place, discover nearby, and search GeoNFTs for AR/VR experiences. **GeoNFT operations are implemented today on the WEB4 NFT API** at `http://api.oasisweb4.com/api/nft`. Use these endpoints for live GeoNFT features.

**Live base path:** `/api/nft` (WEB4)  
**Intended STAR path (when separate):** `/api/geonfts` or `/api/geonft` — confirm in Swagger.

**Authentication:** Required (Bearer token). Always check **isError** in responses.

**Rate limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Live GeoNFT endpoints (WEB4 NFT API)

Use the following on **`http://api.oasisweb4.com/api/nft`**:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `load-all-geo-nfts-for-avatar/{avatarId}` | All GeoNFTs for an avatar |
| GET | `load-all-geo-nfts-for-avatar/{avatarId}/{providerType}/{setGlobally}` | With provider selection |
| GET | `load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}` | GeoNFTs by mint wallet |
| GET | `load-all-geo-nfts` | All GeoNFTs (and provider variant) |
| POST | `place-geo-nft` | Place GeoNFT (body: PlaceGeoSpatialNFTRequest) |
| POST | `mint-and-place-geo-nft` | Mint and place GeoNFT (body: MintAndPlaceGeoSpatialNFTRequest) |
| GET | `search-web4-geo-nfts/{searchTerm}/{avatarId}` | Search GeoNFTs (query: filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType) |

---

## Place GeoNFT (live)

**Endpoint:** `POST http://api.oasisweb4.com/api/nft/place-geo-nft`

**Authentication:** Required

**Body (PlaceGeoSpatialNFTRequest):** NFT ID, location (lat/long/elevation), provider types, etc. See Swagger or [NFT API](../../web4-oasis-api/blockchain-wallets/nft-api.md) for full schema.

**Response:** `result` is the placed `IWeb4GeoSpatialNFT`; check **isError** and **message**.

---

## Mint and place GeoNFT (live)

**Endpoint:** `POST http://api.oasisweb4.com/api/nft/mint-and-place-geo-nft`

**Body (MintAndPlaceGeoSpatialNFTRequest):** Title, description, image URL, location, on-chain/off-chain providers, optional SendToAvatarAfterMintingId, etc. See Swagger for full schema.

**Response:** `result` is the minted and placed GeoNFT.

---

## Intended standalone GeoNFT endpoints (reference)

When a dedicated GeoNFTs API is deployed, it may expose:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/geonfts` or `/api/geonft` | List GeoNFTs |
| GET | `/api/geonft/{geoNftId}` | Get by ID |
| POST | `/api/geonft` | Create/place |
| PUT | `/api/geonft/{geoNftId}` | Update |
| DELETE | `/api/geonft/{geoNftId}` | Delete |
| POST | `/api/geonft/{geoNftId}/mint` | Mint |
| POST | `/api/geonft/{geoNftId}/collect` | Collect |
| GET | `/api/geonft/nearby` | Nearby GeoNFTs |
| GET | `/api/geonft/{geoNftId}/location` | Get location |

---

## Related documentation

- [WEB5 STAR API Overview](../overview.md)
- [NFT API (WEB4)](../../web4-oasis-api/blockchain-wallets/nft-api.md) – GeoNFT section and all NFT endpoints
- [WEB5 STAR Overview](../overview.md) – Map nearby/visit at `/api/map` (live)

---

*Last Updated: January 24, 2026*
