# Max's Branch Features: Completeness and Remaining Work

As of 2026-10-19. Live, editable version: https://claude.ai/code/artifact/666b7f06-9224-401d-913f-99d8b9b2bf30

After a full code read, only one of the nine features found on Max's old branches (`main`, `max-build4`) was shippable: SPL token creation, now in `Development` (e3bd11918). The other eight are stubbed, hollow or unsafe and need a proper build (OASIS policy: real live data only).

| Feature | Source | Size (lines) | Completeness | Verdict |
| --- | --- | ---: | --- | --- |
| SPL token endpoints | `max-build4` | ~120 + provider methods | Mint/burn/send already in Development (stronger); token creation was new | Done: token creation ported |
| A2A / SERV / OpenSERV / MNEE agents | `max-build4` | ~6,700 | Public agent card leaks OpenSERV API keys; SERV/MNEE payments call provider methods that exist nowhere; messages only in memory | Rebuild |
| Web4 liquidity pool | `max-build4` | ~870 | No tokens move on add/remove (custody stubbed), LP shares credited for free; no concurrency control; double maths | Rebuild |
| Contract-generator controller | `main` | ~290 | Calls a ContractGeneratorService that never existed; real API is ScGen.API in SmartContractGenerator repo | Not ported |
| RWA ownership oracle | `main` | ~3,300 | Storage TODO, mock attestations, mock encumbrances | Rebuild |
| Zcash / Aztec stablecoin | `main` | ~3,900 | Placeholder token contracts and collateral address | Rebuild |
| Address-derivation helper | `main` | ~1,600 | Placeholder addresses on failure; Starknet path invalid | Rebuild |
| Starknet account deployment | `main` | ~210 | Deploy, exists check, fee estimate all TODO | Rebuild |
| Holonic Book Review | `max-build4` | ~100 | Demo only | Leave archived |

## Remaining work per examined feature

**SPL tokens.** Development already mints, burns and sends SPL tokens through `IOASISBlockchainStorageProvider` with avatar-key signing and post-transaction verification. New: token creation via an opt-in `IOASISTokenIssuerProvider` capability (Solana first) and `POST api/wallet/create-token`. Not ported: `set-mint-authority`, which took the authority's private key in the request body.

**A2A agents.** Remove API keys from agent-card metadata; persist messages/tasks as holons; pay via WalletManager token send (MNEE is ERC-20) instead of reflection to non-existent methods; drop SERV payments unless a real integration exists; replace Type.GetType/GetMethod optional paths; real karma query; NFT trading to ONODE.Core; tests.

**Liquidity pool.** Real custody (lock tokens before crediting LP shares, release on remove); optimistic concurrency on the pool holon; exact base-unit maths; tests.

**Contract generator.** Nothing to port; if wanted, build an ONODE client for ScGen.API (generate/compile/deploy) with its URL in OASIS DNA.

## Remaining work per stubbed feature

- **RWA oracle:** real ownership-history storage, chain observers for encumbrances, signed oracle attestations.
- **Stablecoin:** deploy zUSD and lock address; avatar search; JWT avatar claims; on-chain confirmation.
- **Address derivation:** real chain SDKs; `OASISResult` errors, not placeholder addresses; dedupe with providers.
- **Starknet deployment:** deploy/exists/fee via StarkSharp or RPC; integrate with StarknetOASIS.

## Preservation

`OASIS-main-archive` repo (full `main` at `305ef3711`), `archive/*` tags in OASIS, and the project repos created on 2026-10-11 and 2026-10-19.
