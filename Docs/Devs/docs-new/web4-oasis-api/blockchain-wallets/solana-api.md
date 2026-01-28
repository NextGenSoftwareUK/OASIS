# Solana API

## Overview

The Solana API provides Solana-specific operations for the OASIS ecosystem. It supports minting NFTs and sending transactions on Solana. All endpoints require authentication.

**Base URL:** `/api/solana`

**Authentication:** Required (Bearer token). Unauthenticated requests often return **HTTP 200** with `isError: true`—always check the response body.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `Mint` | Mint NFT on Solana (body: MintWeb3NFTRequest) |
| POST | `Send` | Send transaction (body: SendTransactionRequest – FromAccount, ToAccount, Lampposts size) |

---

## Mint NFT

**Endpoint:** `POST /api/solana/Mint`

**Body (MintWeb3NFTRequest):** Mint public key account, mint decimals, and other NFT mint parameters (see Swagger).

**Response:** `result` contains `MintNftResult` (e.g. transaction hash).

---

## Send Transaction

**Endpoint:** `POST /api/solana/Send`

**Body (SendTransactionRequest):** FromAccount (public key), ToAccount (public key), and transaction parameters (e.g. Lampposts size).

**Response:** `result` contains `SendTransactionResult` (e.g. transaction hash).

---

## Related Documentation

- [NFT API](nft-api.md) – Cross-chain NFT management (includes Solana)
- [Wallet API](wallet-api.md) – Multi-chain wallets

---

*Last Updated: January 24, 2026*
