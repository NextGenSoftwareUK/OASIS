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


## Guarded native header contract qualified

The actual Core8a5ab45 NativeConnectionGuard handler, loaded from its frozen .NET10 DLL in existing PowerShell7, returned HTTP200for genesis, three explicit historical block hashes and their two-hash subset. No SDK install, build, sourceOwner or mutation. Observed genesis identifies Mainnet/764824073; securityparam is the decimal string2160.

The catalogue comes from operator fixture commit2e8ad77681a29a9a3aca94617acacebbb85d8d59. Full response order differs from requested order, and subset order differs again. Complete unique key sets match exactly. Repeated stable header fields agree; num_confirmations is a per-response observation and must not be treated as immutable or consensus verification.

- Heights6946669/6946670/6946673; observed confirmations7198318/7198317/7198314. Integer tokens checked exactly without float conversion; fee/output fields remain optional decimal-integer strings, not rounded decimals or invented zeroes.
- Operator fixture download9392bytes, SHA256f305b74241c0773e285448b7510c825cf72030c8f1c8a7fb0f42a2897ecc9e10. Native genesis7606bytes; three2234bytes; two1489bytes. HTTP elapsed559/116/320ms in this observation, not a latency guarantee.
- Compact TestResults/CardanoPublicSourceCandidateCurrent/native-header-validation.json SHA2566dbfba1c91fec4b2c8aa0ddd5f5536badbb9607328facd79d6b7a285a6fb08af. Raw response hashes and selected public hashes are in that report; original404preserved.

Next implement the bounded read-only adapter/tests: fixed Mainnet endpoint and genesis identity, exact hash and integer domains, complete-set rejection for missing/duplicate/extra rows, deterministic requested-key ordering after full validation, explicit historical confirmation policy, nullable fee/output preservation, cancellation/result limits and no partial output. Then actual manager/CLI/API/local federation before hosting. CQL implementation remains absent; counts38implemented/36hosted do not increase yet.

Primary contracts: [block endpoints](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/paths/block.yaml), [response fields](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/schemas/block.yaml), [batch request body](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/request-bodies.yaml). These describe an indexer API, not independently verified chain storage.
