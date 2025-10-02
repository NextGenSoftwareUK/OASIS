# NFT Mint Studio – Multi-Chain Expansion Plan

## Goals
- Preserve the current Solana-first wizard for users who need the existing flow.
- Deliver a unified “Multi-Chain Studio” that lets creators switch between Solana, Arbitrum, Polygon, Base, and Rootstock inside one wizard.
- Keep the payload, provider registration, and mint logic aligned with the OASIS API contract (see http://oasisweb4.one/swagger/index.html).
- Minimise disruptive UI/UX changes by reusing the current wizard shell and state machinery.

## Workstream Overview

1. **Snapshot Solana Wizard**
   - Duplicate `page-content.tsx` into a dedicated route (e.g., `/(routes)/solana-studio/page-content.tsx`).
   - Update routing (`app/solana-studio/page.tsx`) so the legacy flow stays accessible without regression risk.

2. **Multi-Chain Entry Point**
   - Convert the root `page-content.tsx` into a multi-chain wizard.
   - Step 1 becomes a “Select Your Chain” grid (Solana, Arbitrum, Polygon, Base, Rootstock) showing logo, protocol, wallet hint.
   - Replace the Solana configuration preset cards with these chain cards; selection drives downstream state.

3. **Chain Metadata Layer**
   - Expand `src/types/chains.ts` from a single `SOLANA_CHAIN` constant to an array of `ChainOption`s.
   - For each chain capture:
     - `providerMapping` (on-chain/off-chain provider enums, metadata storage preference, NFT standard type).
     - Recommended wallet or connection text.
     - Optional defaults (e.g., whether on-chain metadata is required, symbol prefixes).
   - Align enum values with `ProviderType` definitions in `NextGenSoftware.OASIS.API.Core` (SolanaOASIS, ArbitrumOASIS, BaseOASIS, PolygonOASIS, RootstockOASIS, MongoDBOASIS).

4. **Provider Toggles by Chain**
   - Refactor `ProviderTogglePanel` to accept a chain’s provider list (instead of hard-coding Solana/MongoDB).
   - Auto-render toggles for each provider the chain requires; generate the register/activate endpoints by provider name.
   - Persist state per chain so switching chains doesn’t lose progress (store in keyed object or reset intentionally).

5. **Payload Composition**
   - In `MintReviewPanel`, read the active chain metadata to set:
     - `OnChainProvider`
     - `OffChainProvider`
     - `NFTOffChainMetaType`
     - `NFTStandardType`
   - Adjust validation/summary text dynamically (e.g., “Recipient Wallet” label, metadata expectations).
   - Allow chain-specific overrides (e.g., Rootstock might use the same off-chain provider but require different defaults).

6. **UX Enhancements**
   - Hero copy and session summary reflect the chosen chain (logo, colour accents).
   - Inline hints for wallet connection (Phantom vs MetaMask).
   - Optional: display gas/fee notes per chain, or warn if compression/editions only apply to Solana.

7. **Testing & Guardrails**
   - Regression test `solana-studio` route end-to-end.
   - For multi-chain wizard, manually validate at least Solana + one EVM chain hitting the devnet API (even if the mint fails due to backend limitations, ensure error handling is clear).
   - Add logging to surface provider/chain mismatches early.

## Future Enhancements
- Wallet adapters per chain (Phantom, MetaMask/WalletConnect) integrated into the final mint step.
- Chain-specific metadata templates (e.g., EVM vs SPL metadata differences).
- Persisted drafts per chain so users can prep multiple mints before switching.

This plan keeps today’s Solana workflow untouched while laying out the incremental steps to turn the studio into a cross-chain wizard once the current deployment is stable.

