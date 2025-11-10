# x402 Revenue Distribution – Setup Guide

This README explains how to run the full SC-Gen → x402 → Solana pipeline so that every Smart Contract Generator payment is routed to NFT holders in real time.

---

## 1. What You’ll Need

- **Node.js 18+** (NVM recommended)
- **.NET 9 SDK** (for SC-Gen API)
- **Solana CLI** pointing at devnet
- **OASIS API** (`dotnet run` from `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`)
- **Devnet keypair** with SOL for gas (`solana-devnet.json`)

Environment variables used below:

```bash
export X402_USE_MOCK_DATA=false
export X402_SIGNER_SECRET=45BiYK1XjngQTu6asQorrpXsk5EUyhkrKWzdc66pMShnRFeTUqLEbUUirfC2ixfrjBtnufJrZ8qX7KtyaMhiEmDa
```

`X402_SIGNER_SECRET` is the base58 form of the devnet signer that holds the funds we distribute (see `solana-devnet.json`).

---

## 2. Service Topology

```
[OASIS API] --mints--> [NFT on Solana]

[SC-Gen UI] -> [SC-Gen API] -> /api/x402/webhook -> [x402 Service] -> Solana transfers
                                                              |
                                                              v
                                                     Treasury & NFT holders
```

---

## 3. Step-by-Step Setup

### 3.1 Ensure OASIS API is Running

```bash
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
# API listens on https://localhost:5004
```

### 3.2 Start x402 Distribution Service

```bash
cd x402/backend-service
npm install
X402_USE_MOCK_DATA=false \
X402_SIGNER_SECRET=45BiYK1XjngQTu6asQorrpXsk5EUyhkrKWzdc66pMShnRFeTUqLEbUUirfC2ixfrjBtnufJrZ8qX7KtyaMhiEmDa \
npm start
# Service listens on http://localhost:4000
```

You should see:

```
✅ X402PaymentDistributor initialized
   Solana RPC: https://api.devnet.solana.com
   Platform Fee %: 2.5
   Signer Loaded: 6rF4zzvuBgM5RgftahPQHuPfp9WmVLYkGn44CkbRijfv
```

### 3.3 Start Smart Contract Generator API

```bash
cd SmartContractGenerator/src/SmartContractGen/ScGen.API
dotnet run
# API listens on http://localhost:5000
```

`appsettings.json` already points `X402:DistributionWebhookUrl` to `http://localhost:4000/api/x402/webhook`.

### 3.4 Mint an x402-Enabled NFT (OASIS API)

Authenticate and mint via HTTPS (example):

```bash
curl -k -s -X POST https://localhost:5004/api/nft/mint-nft \
  -H "Authorization: Bearer <jwt>" \
  -H "Content-Type: application/json" \
  -d '{
        "Title": "MetaBrick x402 Devnet #2",
        "Symbol": "MBRICKX402",
        "OnChainProvider": "SolanaOASIS",
        "SendToAddressAfterMinting": "3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X",
        "MetaData": {
          "x402": {
            "enabled": true,
            "distributionPercentage": 90,
            "treasuryAddress": "3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X",
            "webhookEndpoint": "http://localhost:4000/api/x402/webhook"
          }
        }
      }'
```

Note the returned mint address (e.g. `6poPnTtvVSBeCb4SNydu6r8y8jye76nAQFqe9EtWpH69`).

### 3.5 Register the Mint with x402

```bash
curl -s -X POST http://localhost:4000/api/x402/register \
  -H "Content-Type: application/json" \
  -d '{
        "nftMintAddress":"6poPnTtvVSBeCb4SNydu6r8y8jye76nAQFqe9EtWpH69",
        "paymentEndpoint":"http://localhost:4000/api/x402/webhook",
        "revenueModel":"equal_split",
        "treasuryWallet":"3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X"
      }'
```

### 3.6 Trigger Revenue via SC-Gen

Supply the Solana transaction signature and the mint address:

```bash
curl -s -X POST http://localhost:5000/api/v1/payments/verify \
  -H "Content-Type: application/json" \
  -d '{
        "signature":"4D6NPhNn5aWstgM5sm7CsdB99pKh9Vyj2CPBPGgeH7NxkzxpXfDrNV18s1iEYVT8pAzJahfjM5H4wYbj8iX8i27o",
        "operation":"generate",
        "blockchain":"rust",
        "amount":0.02,
        "nftMintAddress":"6poPnTtvVSBeCb4SNydu6r8y8jye76nAQFqe9EtWpH69"
      }'
```

The API replies with a payment token and the x402 service sends a real Solana transfer.

### 3.7 Confirm the Distribution

```bash
curl -s http://localhost:4000/api/x402/history/6poPnTtvVSBeCb4SNydu6r8y8jye76nAQFqe9EtWpH69
```

Look for the latest entry and verify the `txSignature` on Solscan (devnet). Example distribution:

```
3NgZ5xJb9zxpVJZdSYKH6KHiyVbCtnZUXFgxwXMkFrWYu6oMzjH51JMNqJRjE6u5yNW4XiDRBZA42pSsgcfDCssn
```

---

## 4. Viewing & Testing Extras

- **Manual payout:** `POST http://localhost:4000/api/x402/distribute` with `{ "nftMintAddress": "...", "amount": 0.05 }`
- **MetaBricks stats:** `GET http://localhost:4000/api/metabricks/stats`
- **Front-end dashboard:** `http://localhost:3002/x402-dashboard` (if you spin up `nft-mint-frontend`)
- **Developer narrative:** `x402/X402_SCGEN_REVENUE_TO_NFT_YIELD.md`

---

## 5. Folder Reference

```
x402/
├── README.md (this file)
├── X402_SCGEN_REVENUE_TO_NFT_YIELD.md     # Narrative + economics
├── X402_SCGEN_END_TO_END.md               # Technical deep dive
├── backend-service/                       # Node.js distributor
├── docs/                                  # Slides, context, quick refs
└── metabricks-integration/                # Legacy MetaBricks materials
```

For deeper context see:
- `docs/X402_README.md`
- `docs/X402_ONE_PAGER.md`
- `metabricks-integration/PRICING_ECONOMICS.md`

---

With these steps you can go from a clean workspace to live, on-chain revenue sharing in under 10 minutes. Let me know if you need Docker/production variants or integration with other OASIS providers.   

