# Agent Handoff Context – November 11, 2025

## Project Snapshot

- **Repository:** `/Volumes/Storage/OASIS_CLEAN`
- **Active Focus:** Contract Generator UI/API with x402 Solana revenue distribution.
- **Primary Branch:** Assume `main` (no explicit branch changes noted).

## Running Services (see `QUICKSTART_SERVICES.md` for commands)

1. **x402 backend service** (`x402/backend-service`)
   - Requires Node 20.11.1, funded devnet signer (`devnet-signer.json`).
   - Use private Solana RPC (`SOLANA_RPC_URL`) to avoid rate limits.
   - Provides webhook + stats/history endpoints used by the UI.

2. **SmartContractGen API** (`contract-generator/api`)
   - .NET 9 project (`DOTNET_ENVIRONMENT=Development dotnet run`).
   - Needs OpenAI key and x402 webhook URL configured.

3. **Next.js Frontend** (`contract-generator/ui`)
   - Node 20.11.1, `npm run dev -- --port 3001`.
   - `.env.local` should set `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_X402_API_URL`, `NEXT_PUBLIC_SOLANA_RPC_URL`, `NEXT_PUBLIC_X402_RECIPIENT`.

## Recent Changes (today)

- Dashboard header styling and yNFT imagery updates.
- Treasury feed now reads distribution history/stats from the x402 API and handles RPC rate limits gracefully.
- x402 distributor now records a `mock-distribution` fallback when on-chain transfers fail (typically due to RPC throttling) so the UI still shows activity.
- Created `QUICKSTART_SERVICES.md` for startup steps and this handoff context.

## Current State / Known Issues

- **Rate limiting:** Public devnet RPC hits 429 quickly. Use a private endpoint (QuickNode/Helius/etc.) via `SOLANA_RPC_URL` + `NEXT_PUBLIC_SOLANA_RPC_URL`.
- **Treasury/payer wallet overlap:** Demo currently uses the same Phantom account for payments and treasury (`3BTE...VZco5X`), so SOL balance appears to climb instead of drop. For realistic demos use separate payer vs treasury wallets.
- **Distributions:** With private RPC, the fallback path won’t trigger and transactions will settle on-chain. Without it, entries are marked `mock-distribution` but the UI still updates.
- **Credits badge:** Only visible once Phantom is connected (expected behavior, but easy to forget).

## Open Questions / Follow-ups

- Do we need automated retries for on-chain transfers once a private RPC is configured? (Currently fallback records the event but does not retry.)
- Should we persist aggregated stats server-side to remove the client balance fetch entirely?
- Any outstanding git cleanup? Repo has tons of pending/untracked files; no commit performed yet.

## Next Steps Suggestion

1. Configure private Solana RPC across services if on-chain transfers are required.
2. Verify end-to-end demo: AI generate → payment → distribution history updates without rate-limit warnings.
3. Capture a clean git diff for the relevant files once all demos are recorded.

