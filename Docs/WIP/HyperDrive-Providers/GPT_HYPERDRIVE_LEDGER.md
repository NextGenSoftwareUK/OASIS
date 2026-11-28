# gpt-hyperdrive ledger

## 2026-11-27 — current-Development native rechecks

- Claimed Firebase and Turso separately on `claims`; used own work/gpt branches without switching the shared dirty checkout.
- Source: Development d91bf074fa16a7e6f15f78fd87ad7314c90d72c7. Firebase 46099124837 and Turso 46099193518 passed 26 sync + 26 async checks each on isolated official/native backends in CI. Focused existing regression checks also passed, cleanup confirmed.
- Required rechecks: 2/3 complete; PocketBase is owned by another session. Programme: about115/234 carried forward, no double-counting requalification.
- OPORTAL: preserve Claude’s recent fixes, correct verified MCP server source links in separate OPORTAL-JS batch. No new owner action.
- No shared Atlas tests, protected queue changes, local build/env/download or extra worktree. Temporary indexes removed in finally.

## 2026-11-27 — delegated storage identities (in progress)

- Claimed integration of existing `6c95573c` rather than duplicate its implementation. Adapted Eclipse to the newer `_solana` composition.
- EOSIO invariant: contract record IDs are canonical storage keys. Save/read/delete by provider key must use the same repository and composing-provider type. Avatar/detail snapshots persist keys before commit; caller identity is acknowledged after a successful repository write.
- Added focused native key round trips for Eclipse and Telos to their existing real Agave/Antelope suites, sync + async. CI pending; no acceptance or Development deployment claimed. SEEDS outer identity requires separate follow-up; existing SEEDS native suite remains part of Antelope regression.
- No local builds/downloads/envs or shared Atlas calls; private indexes cleaned in finally.
