# NEAR CQL MVP

Updated 29 October 2026. **Adapter controls verified; native and paired demo pending.**

## Implemented scope

`NEAROASIS` now implements `ICosmicQueryStorageProvider`. Captured ID equality,
membership, AND/OR and constants compile to at most 100 native record-key reads,
intersected with a trusted server-owned authorization catalogue. Paging follows
deterministic ID order. Unsupported metadata/order/graph expressions fail admission.

The adapter uses the existing SDK encrypted snapshot reader and canonical stored
holon decoder. Load/save/query share the same key builder without changing the
stored key format. Each returned identity and read access is validated, deleted
records hidden, and response/result bounds checked before publishing a page.
Errors/cancellation publish no partial results. Reads can observe different blocks.

Caller cancellation now reaches the existing SDK worker process owner. It is not
implemented by abandoning a live background task. Existing reflection-based test
callers explicitly supply the cancellation argument for the changed internal SDK
signature; native tests compile but have not been re-executed.

## Evidence

- CQL controls: **8/8 pass, zero skips**, including an actual controlled Node
  worker started, cancelled during its read and verified absent afterward.
- Existing encrypted snapshot controls: **10/10 pass, zero skips**. They cover
  retained ciphertext, authentication/context rejection and missing-key admission.
- `TestResults/NearCqlCurrent/near-cql-controls.trx`, SHA256
  `B0C7F8AB0AE59B761DF7609FBE0FC799D2E49291260D27EE2D3AF9C4633761BD`.
- `TestResults/NearCqlCurrent/near-encryption-controls.trx`, SHA256
  `7D266D47F8AE888E269173C4C143809E7F2AFE1A0FDFE23948E95E825B587A50`.
- Worker marker removed only after exact process-exit verification.

These are controlled transport proofs, not native-chain or demo acceptance.

## Native readiness and exact next step

The retained NEAR sandbox2.6.2 runtime, Node22.22.2 and original ledger exist.
No active `near-sandbox` process was found. The worktree bridge dependencies are
absent, as are the old retained root's direct SDK packages. The encryption variable
is not present in this session. No keys were printed, replaced or guessed.

Existing provider reports record an interrupted native sequence with unresolved
ordinary writes. Preserve its original evidence and ledger: do not reset/replay it
or treat historical successful tests as current acceptance. See
[historical gates](../HyperDrive-Providers/NEAR_PUBLIC_CONDITIONAL_SAVE.md).

Next: restore the pinned worktree SDK from retained caches, inspect original
read-only state/receipt evidence and encryption-key availability, then establish
whether a retained three-record source can be queried without disturbing unresolved
writes. Any new seed requires its own signed-original journal and a reviewed safe
mutation boundary. Then add Mongo-paired C#/HTTP/browser five/four-result presets.

No public-network, full-parity or universal HyperDrive claim is made.
Return to [current status](CURRENT_STATUS.md); resources are tracked in
[the cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
