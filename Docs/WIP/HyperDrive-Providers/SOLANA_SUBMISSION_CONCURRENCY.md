# Solana SDK submission concurrency — staged verification

2026-10-19. Source not yet integrated: native acceptance is outstanding. This is not full provider or HyperDrive V2 certification.

## Invariant and fix

Within one FinalizedSolanaRpc SDK adapter, a later submission must not bypass an unresolved original submission. The old pre-call failure check allowed concurrent calls to both pass before the first recorded its unknown outcome. Two held-response tests (byte-array and base64 callers) reproduced the race before the fix:2/2failed, report SHA256 `642CE3D06352A161D4DF3CBC6408AE7E11F46AA51DE26FBD5D70D76140C67CD9`.

The adapter now serializes its submissions with one asynchronous semaphore, checks sticky original failure after entering it, and releases it in finally. Waiting byte-array inputs are copied before waiting; encoded strings are immutable. The original submission failure is retained rather than overwritten. Finalized-signature readers receive a locked detached snapshot, not a mutable live list. Both actual submission/finality and transaction construction retain their existing owners; no new broadcast retry, finality loop, native fallback or global provider lock was introduced.

New controlled tests prove an unknown first response causes both concurrent calls to retain the same reconciliation exception with exactly one RPC broadcast; a successfully finalized predecessor allows the queued successor and preserves its original bytes despite caller mutation while queued. This is per-adapter ordering, not cross-instance, cross-process or native exactly-once semantics. No queue bound/cancellation/deadline guarantee beyond the existing submission owner is claimed.

## Current evidence and native limitation

Current producer0errors/2warnings. Full rebuilt controlled suite **539/539passed**, zero skipped,5seconds; fixed `TestResults/SolanaCurrent/solana-unit-current.trx` SHA256 `9234ED48E2D32D7C5155EE43A7C77F29F03FC3EB3E5BC3EB7BF0DC46B073C928`. These tests use controlled RPC results, not actual chain execution.

Existing reuse-only runner `Run-ReusableSolanaTests.ps1 -InspectRecent -ObservationSeconds 2` completed terminal0 on the SAME retained Agave4.3.0/ledger. Genesis `HXfgJ8CoRQ7RcgETc178DX2FwbSQQK6nCCTNc5YwGRi2`; processed/confirmed/finalized505, observed finalized roots[505], root still505 after observation, lastVote504/vote root472/stake999999997717120, no recent finalized fixture signatures. This diagnostic proves the native gate remains blocked, not that mint/storage/fees work. No new transaction, key read, funding, ledger reset/warp/tower deletion, runtime install/download or duplicate environment. Runner owns/stops only its resumed retained process.

Missing test project assets regenerated from the existing NuGet cache only. Fixed reports retained; reusable build outputs retained for remaining acceptance. Observed free C:82,601,730,048bytes. No broad cleanup/reclaim attribution. Firestore pending checkpoint/source remains separate and untouched.

Remaining: resolve retained validator consensus without discarding protected state, then actual native SDK mint/collection/account-state/fee/error/concurrent acceptance; console logging/global state and all other provider interface/options/history/hosted/V2 gates remain separate. Do not merge this staged adapter change as native-proven.

## Retained consensus diagnosis — 2026-10-19

Read-only inspection of the last startup log establishes more than a generic stalled RPC:

- Bank snapshot loaded from `ledger/snapshots/505`; snapshot rebuilding completes.
- `load_frozen_forks`: bank-forks root505, latest blockstore root557/max_root557. Ledger replay ends with only one bank505.
- Saved tower root557, last vote588, restored lockouts558–588. Agave explicitly reports reprocessing already voted/rooted slots and suspends voting until588; corresponding bank data for restored vote588 is missing.
- New local forks506 onward have zero switch-proof stake against required379999999132505.6 of total999999997717120. This explains the voting refusal; it is not an OASIS request-readback delay or a provider retry fix.

This evidence proves a mismatch between restored bank/replay state and saved consensus state. It does **not** prove why later bank data was unavailable (purging, prior interruption, snapshot selection or another cause must still be distinguished). Do not assert disk corruption or repair by deleting the tower. Retained snapshot archives404/505 are about397KB/399KB; tower file2319bytes exists. Pinned runtime contains test-validator, solana and solana-keygen only; no ledger-tool available for offline bounds/replay inspection. No new download/install has been performed.

Official pinned [Agave4.3 test-validator source](https://github.com/anza-xyz/agave/blob/v4.3.0/test-validator/src/lib.rs) confirms snapshot setup and default test-validator configuration with optional tower storage and warp support. These are separate mechanisms; a warp/reset is not evidence-preserving replay recovery. Installed test-validator CLI exposes warp but no snapshot-selection/replay control in its help. No recovery mutation has been authorized or executed here. Validator was confirmed stopped before read-only log inspection; no additional startup was needed.

Next native recovery prerequisite is to establish retained blockstore bounds/replayability with appropriate existing/offline tooling, then choose a reviewed state-preserving recovery. Do not repeatedly resume the same stalled ledger or substitute a fresh chain to claim original-state acceptance. Independent provider implementation work can continue while this native gate is unresolved.
