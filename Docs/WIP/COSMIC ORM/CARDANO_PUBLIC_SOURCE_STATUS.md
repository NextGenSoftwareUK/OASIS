# Cardano public-source qualification

Updated 1 November 2026. Read-only research only; no CQL implementation or hosted count grant.

- The [operator's introduction](https://www.koios.rest/guide/introduction.html) lists its Mainnet endpoint and points to the [operator's API artifacts](https://github.com/cardano-community/koios-artifacts) for specifications and SQL contracts.
- One anonymous Node24 GET to https://api.koios.rest/api/v1/genesis returned HTTP200, one record with networkid=Mainnet and networkmagic=764824073. This is an indexer-reported identity observation, not independent consensus proof.
- A proposed OpenAPI description request at /api/v1/ returned404. That path guess was wrong; it does not show a service-wide failure. No retry/fallback endpoint used.
- No wallet, account, key, installation, database, sourceOwner, native .NET guard qualification or mutation. No block catalogue or three/two-record query has been qualified.

Evidence in TestResults/CardanoPublicSourceCandidateCurrent:

| File | Bytes | SHA256 |
|---|---:|---|
| genesis.response.json | 7606 | 418ed31e2dfc84c6e7e47286b99158a204e73eeed9b4e902b426c0b2e032dee8 |
| openapi.response.json | 87 | 20bd41b63d2a9c4f9a1fbb3abf9378e95faa3c9711c2e9ac834d5e93e2477cc9 |

Next: read the documented block-selection/schema contract from the operator's specs, then qualify bounded historical header selections and native numeric/identity/status domains through the actual guarded .NET transport. No implementation/hosting admission until that succeeds. Preserve these original observations; do not install a node or replace endpoint based on the404.
