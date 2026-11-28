# gpt-hyperdrive ledger

## 2026-11-27 — current-Development native rechecks

- Claimed Firebase and Turso separately on `claims`; used own work/gpt branches without switching the shared dirty checkout.
- Source: Development d91bf074fa16a7e6f15f78fd87ad7314c90d72c7. Firebase 46099124837 and Turso 46099193518 passed 26 sync + 26 async checks each on isolated official/native backends in CI. Focused existing regression checks also passed, cleanup confirmed.
- Required rechecks: 2/3 complete; PocketBase is owned by another session. Programme: about115/234 carried forward, no double-counting requalification.
- OPORTAL: preserve Claude’s recent fixes, correct verified MCP server source links in separate OPORTAL-JS batch. No new owner action.
- No shared Atlas tests, protected queue changes, local build/env/download or extra worktree. Temporary indexes removed in finally.

## 2026-11-27 — Antelope composing-provider keys

- Integrated the EOSIO/Telos portion of existing6c95573c, then fixed the canonical EOSIO record repository/key mismatch. Avatar/detail saves persist keys in a snapshot; source identity is acknowledged after the repository write.
- SEEDS uses an immutable Telos storage delegate configured for SEEDSOASIS, preserving the injected Telos instance. Provider-key entry points share the canonical repository implementation.
- Telos CI46101946563 and SEEDS CI46101275004 pass26sync+26async each plus native provider-key/update round trips; independent tables empty, nodes/wallets/fixtures removed. Earlier constructor/type-check failures are superseded, not counted as acceptance.
- Eligible verified slice only: Eclipse/Solana source stays on the feature branch pending Eclipse CI46101901966. Solana native26+26 passed within46100237006, which failed later compiling the then-old Eclipse test helper; no green whole-run claim.
- Programme remains about115/234; existing-provider identity fixes do not add providers. Rechecks2/3, PocketBase retained by its owner. Hosted/global v2 gates stay open.
- OPORTAL: corrected private-source public-link exposure, runtime label mismatch, bounded transient artifact retention; removed9138068830bytes of superseded failed reports. GitHub quota recount pending; no claim that laptop disk grew by this amount.
- No local runtime/env/download/build/database/output, no shared Atlas/protected delivery operations. Private Git indexes removed in finally; compact receipts retained.

## 2026-11-27 — v2 source-gate audit (in progress)

Claimed before inspection. `HYPERDRIVE_V2_GATE_AUDIT_2026-11-27.md` records a split authoritative state between the public ProviderSwitcher helper/ProviderRegistry and shipping ProviderManager, enum-only switching, publication before possible failure, and fabricated last-switch time. Existing native main-router acceptance is preserved and not relabelled as switcher evidence. No global backend tests run before provider acceptance; no owner action or local runtime/output. Next is the canonical state fix and focused real-provider verification when the provider gate permits it.

- OPORTAL owner custody checkpoint: GitHub repository Actions secret-name inventory is empty; recorded OPORTAL-CI1 in the single owner-action list, referencing the already supplied account from Claude’s handoff rather than asking for credentials again. No signed-in request or shared-cluster test dispatched.
