# Contract Generator + x402 Quick Start

This checklist gets every service needed for the demo up and running from a clean terminal session.

---

## 1. Prerequisites

- **Dotnet SDK 9** (project currently builds with .NET 9 preview tooling)
- **Node.js 20.11.1** (use `nvm use 20.11.1`)
- **pnpm/npm** (repo uses npm lockfiles)
- **Solana CLI** (optional, but useful for checking balances)
- A funded devnet wallet JSON for the x402 signer (`x402/backend-service/devnet-signer.json`)
- A private devnet RPC endpoint (QuickNode/Helius/Alchemy/etc.) – strongly recommended to avoid public rate limits

Create a `.env.local` for the UI and export environment variables for the API/x402 service as described below before starting each service.

---

## 2. Start the x402 Distribution Service

```bash
# 1. Terminal A – x402 backend service
source ~/.nvm/nvm.sh
nvm use 20.11.1
cd /Volumes/Storage/OASIS_CLEAN/x402/backend-service

# ENV: point to private Solana RPC + signer key
export SOLANA_RPC_URL="https://your-private-devnet-rpc/"
export X402_SIGNER_KEYPAIR_PATH="/Volumes/Storage/OASIS_CLEAN/x402/backend-service/devnet-signer.json"
export X402_USE_MOCK_DATA=false
export X402_WEBHOOK_SECRET="demo_webhook_secret_123"

npm install
npm run dev
```

The service exposes:
- Health: `http://localhost:4000/health`
- x402 API: `http://localhost:4000/api/x402`
- MetaBricks routes: `http://localhost:4000/api/metabricks`

Leave this terminal running.

---

## 3. Start the .NET API (SmartContractGen)

```bash
# 2. Terminal B – API backend
dotnet --list-sdks  # verify .NET 9.x is available
cd /Volumes/Storage/OASIS_CLEAN/contract-generator/api/src/SmartContractGen/ScGen.API

# Optional: override Solana RPC if not already set
export SOLANA__RPCURL="https://your-private-devnet-rpc/"

# Ensure OpenAI key & x402 settings are in appsettings.Development.json or exported as env vars
# e.g.
# export OpenAI__ApiKey="sk-..."
# export X402__DistributionWebhookUrl="http://localhost:4000/api/x402/webhook"
# export X402__DefaultNftMintAddress="8b7jJB3QsyR1z7odturFSXR33g7FcWpyCTKjpXcfbNTb"

# Restore & run
dotnet restore
DOTNET_ENVIRONMENT=Development dotnet run
```

API runs on `http://localhost:5000` by default. Leave this terminal running.

---

## 4. Start the Next.js Frontend

```bash
# 3. Terminal C – Next.js UI
source ~/.nvm/nvm.sh
nvm use 20.11.1
cd /Volumes/Storage/OASIS_CLEAN/contract-generator/ui
npm install

cat <<'EOF' > .env.local
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_X402_API_URL=http://localhost:4000
NEXT_PUBLIC_SOLANA_RPC_URL=https://your-private-devnet-rpc/
NEXT_PUBLIC_X402_RECIPIENT=3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X
EOF

npm run dev -- --port 3001
```

Frontend will be available at `http://localhost:3001`. The AI generator and template pages both talk to the API & x402 service.

---

## 5. Phantom Wallet Setup

1. Open Phantom and make sure you are on **Devnet**.
2. Fund the wallet with devnet SOL (1–2 SOL is enough) using the Solana faucet.
3. This wallet will:
   - Pay for contract operations (generate/compile/deploy)
   - Purchase credit packs (revenue flows through x402)

For demo consistency, avoid using the same wallet as the treasury (treasury is `3BTE...VZco5X`).

---

## 6. Test Flow Checklist

1. Visit `http://localhost:3001/generate/template`.
2. Connect Phantom → Generate contract.
3. Payment modal triggers → approve on Phantom.
4. Observe MiniConsole logs → look for distribution confirmation.
5. Open `http://localhost:3001/x402-dashboard` to watch the yNFT treasury update.

If distributions show as "mock-distribution" in the logs, it means the RPC was rate limited. Switching to a private RPC (see step 2) ensures real on-chain payouts.

---

## 7. Troubleshooting

- **429 / rate limit**: You’re still on the public devnet RPC. Set `SOLANA_RPC_URL` + `NEXT_PUBLIC_SOLANA_RPC_URL` to a private endpoint and restart services.
- **x402 service not receiving webhooks**: Confirm `X402__DistributionWebhookUrl` in the API points to `http://localhost:4000/api/x402/webhook` and that the x402 service logs show incoming webhook calls.
- **AI generation fails (404)**: API is not running or `NEXT_PUBLIC_API_URL` is incorrect.
- **Credits badge missing**: Connect Phantom first; the header only renders the badge after a wallet address is available.

