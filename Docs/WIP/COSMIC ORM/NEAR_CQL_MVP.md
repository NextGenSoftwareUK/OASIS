# NEAR CQL MVP

Updated 29 October 2026. **Controls and native query verified; paired demo pending.**

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

The controls are controlled-transport proofs. Native acceptance additionally passes
**1/1, zero skips, 4 seconds** on the retained chain: three existing encrypted
`TestHolon` records return three IDs, captured membership returns two, and canonical
contents/revisions remain unchanged. No signing key, chain mutation, reseeding,
contract deployment or historical replay was involved.

- Native report: `TestResults/NearCqlCurrent/near-cql-native.trx`, SHA256
  `8E6A9E22654CBF4D588C1D66D482033AAF03DF6F82ED0E54FF061CFDA8AC0A3F`.
- Reusable catalogue: `LocalState/COSMIC/NearCql/native-source.json`, 196 bytes,
  SHA256 `DC09EFE6C8D9482A67B83D6500D98F35AE27F7C14CBD0A46C0A980004A71E559`.
- Original protected storage key found and reused from the existing provider
  evidence location. It remains outside disposable build/test output.
- Original code SHA256:
  `86e566d1e51bdac93aa0f47a511bb581ab33f29f939cb4b6ac5a7fd0595945dd`.
- Owned native PID10881 exited0 after SIGINT; ports3030/24567 closed.

## Native readiness and exact next step

The retained NEAR sandbox2.6.2 runtime, Node22.22.2 and original ledger exist.
The pinned worktree SDK is restored: 68 packages, 10,807,176 bytes. Offline restore
first identified a missing cached tarball; prefer-offline restore fetched the
locked missing packages from the official registry. Lockfile SHA256:
`24C481A7740F5A5090C4D9228AB1691BB3A5141246A4BBB90D5BDA17F8B02075`.

Existing provider reports record an interrupted native sequence with unresolved
ordinary writes. Preserve its original evidence and ledger: do not reset/replay it
or treat historical successful tests as current acceptance. See
[historical gates](../HyperDrive-Providers/NEAR_PUBLIC_CONDITIONAL_SAVE.md).

Read-only diagnostic discovery, separate from CQL execution, inventoried 1,159
retained cells and 377 opaque holon keys through native finalized pagination. The
native test then decrypted with the original key and froze three explicitly
synthetic records in its trusted server-owned catalogue. Subsequent query execution
uses only those selected native keys; it does not scan the store. No historical
write outcome is inferred from the readable inventory.

Next: add the reusable runner and Mongo-paired C#/HTTP/browser five/four-result
presets, reusing this original catalogue/key/ledger across a full restart. Keep
historical unresolved writes untouched and their gate open; no new source writes
are needed for this MVP.

No public-network, full-parity or universal HyperDrive claim is made.
Return to [current status](CURRENT_STATUS.md); resources are tracked in
[the cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
