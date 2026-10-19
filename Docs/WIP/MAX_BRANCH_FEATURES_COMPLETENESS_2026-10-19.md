# Max's Branch Features: Completeness and Remaining Work

As of 2026-10-19. Live, editable version: https://claude.ai/code/artifact/666b7f06-9224-401d-913f-99d8b9b2bf30

Four of the nine features found on Max's old branches (`main`, `max-build4`) are real code and are being ported into `Development`; five are stubbed and need a proper build first (OASIS policy: real live data only).

| Feature | Source | Size (lines) | Completeness | Verdict |
| --- | --- | ---: | --- | --- |
| SPL token endpoints | `max-build4` | ~120 + provider methods | Mint/burn/send already in Development (stronger); only token creation is new | Port token creation |
| A2A / SERV / OpenSERV / MNEE agents | `max-build4` | ~6,700 | Mostly real; placeholder karma lookup, NFT part blocked by a dependency loop | Port, fix 2 gaps |
| Web4 liquidity pool | `max-build4` | ~870 | Real code; reserves/LP shares only in OASIS holons, not on-chain | Port as Phase 1a |
| Contract-generator controller | `main` | ~290 | Real; proxies to SmartContractGenerator service | Port |
| RWA ownership oracle | `main` | ~3,300 | Storage TODO, mock attestations, mock encumbrances | Rebuild |
| Zcash / Aztec stablecoin | `main` | ~3,900 | Placeholder token contracts and collateral address | Rebuild |
| Address-derivation helper | `main` | ~1,600 | Placeholder addresses on failure; Starknet path invalid | Rebuild |
| Starknet account deployment | `main` | ~210 | Deploy, exists check, fee estimate all TODO | Rebuild |
| Holonic Book Review | `max-build4` | ~100 | Demo only | Leave archived |

## Remaining work per ported feature

**SPL tokens.** Development already mints, burns and sends SPL tokens through `IOASISBlockchainStorageProvider` with avatar-key signing and post-transaction verification. New: token creation via an opt-in `IOASISTokenIssuerProvider` capability (Solana first) and `POST api/wallet/create-token`. Not ported: `set-mint-authority`, which took the authority's private key in the request body.

**A2A agents.** Replace the placeholder karma lookup; move NFT trading to ONODE.Core to break the API.Core → ONODE.Core cycle; re-target to current APIs; SERV/OpenSERV/MNEE endpoints and keys in OASIS DNA; tests.

**Liquidity pool.** Phase 1b: on-chain contracts for reserves and LP shares; tests for swap/share maths; re-target to current APIs.

**Contract generator.** Service URL in OASIS DNA; `OASISResult` errors when unreachable; integration test.

## Remaining work per stubbed feature

- **RWA oracle:** real ownership-history storage, chain observers for encumbrances, signed oracle attestations.
- **Stablecoin:** deploy zUSD and lock address; avatar search; JWT avatar claims; on-chain confirmation.
- **Address derivation:** real chain SDKs; `OASISResult` errors, not placeholder addresses; dedupe with providers.
- **Starknet deployment:** deploy/exists/fee via StarkSharp or RPC; integrate with StarknetOASIS.

## Preservation

`OASIS-main-archive` repo (full `main` at `305ef3711`), `archive/*` tags in OASIS, and the project repos created on 2026-10-11 and 2026-10-19.
