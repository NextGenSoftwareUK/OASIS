# Session range registration — 21 November 2026

AddRange and AttachRange share RegisterCore with single-root registration. They perform no native I/O and do not flush; SaveChanges remains the explicit acknowledged write owner. Attach captures the existing root baseline; Add marks a new root.

One call accepts at most 10,000 roots. Enumeration runs under the session operation guard, so reentrant session operations are refused. On an item/enumeration failure, earlier accepted roots remain tracked and are returned in the immutable Result list; this is explicit partial local registration, not an atomic database transaction. Inspect/detach those roots before retrying.

Source published only. The focused strict-provider contract test, compilation and representative native save qualification remain pending. No full framework parity credit or live acceptance. The frozen snapshot demo image remains pinned to Core13bd050 and excludes this unqualified SDK increment.

Cleanup: no local build, runtime, database, container or downloaded artifact; the scoped temporary Git index is removed by the publisher. Existing active demo handoff journals retained.

Qualification uses the existing session-entry workflow with the new range contract plus the three entry compatibility checks, because both now share the refactored registration owner. Actual WEB4 compilation follows in the same bounded CI build lease. No local build or shared Atlas operation.

## Focused contract and WEB4 compile accepted

Run44945637242/job136972218756 on e2161fb6 succeeded: one new range rule/partial-result/reentrancy/no-I/O test plus three entry compatibility tests,4passed/0failed/0skipped. Actual WEB4 compiled0errors5m49.13s. Artifact14723827093 is1808bytes, SHA2569b59cd0d788ed61d7ee977e190932265efea0a1cf765f4b3a63c02ff2dfcd77c; exact entry.trx and all four names/outcomes verified in RAM. Source Core3b69eedb. Earlier source-only status is superseded by this scoped contract/build acceptance; native save acceptance and full framework parity remain open.

Native representative qualification prepared: three cases only, selecting real MongoDB/SQLite/Neo4j, two private roots, absence before SaveChanges, explicit create, AttachRange baseline/no-op, one update and foreign-private read exclusion. Existing fixture owns exact DB/work/container cleanup and writes evidence only after cleanup. Isolated CI Mongo/Neo; no shared Atlas/live operations. Native results pending; frozen timeout image excludes range Core.

## Update and removal ranges — source increment

UpdateRange and RemoveRange now use the same guarded range traversal as AddRange/AttachRange, and the same UpdateCore/RemoveCore rules as their single-root methods. No native I/O or implicit flush. Explicit update revisions remain required by SaveChanges; removing uncommitted added roots detaches them. On failure the Result list identifies accepted earlier local operations, which remain applied (including detach for added removals); no rollback or database transaction claim.

One strict no-I/O contract covers update/deletion intent, added-root detach and invalid update refusal. Five selected checks including the existing entry compatibility and registration checks qualify the changed shared owner. This increment remains source-only until results. Existing native44947147551 is pinned to the earlier AddRange/AttachRange Core3b69 and must not be claimed to cover the new update/removal range methods. Timeout image7c5f remains unchanged.

## Native qualification compile blocker identified and repaired

Run44947147551 failed before native execution/no artifact: two CS1503 at MapController44. ONODE34a8685e MapManager supports (IOASISStorageProvider,Guid,OASISDNA), while the root caller still passed (HolonManager,Guid) from the earlier dependency contract. Corrected the caller to pass the already admitted provider, actor and DNA. No overload shim, dependency rollback, new fallback or permission bypass.

Queued own image44946907870 and five-check44947664565 cancellation acknowledged because they contain the same proven compile mismatch; not an observation timeout and no accepted result discarded. New native qualification runs the five shared Core controls followed by only the three new native range cases under one build lease, retaining both TRX files and exact cleanup evidence. No accepted old native cohort replay.

## One representative native journey for all four range methods

The same three provider cases now exercise AddRange/AttachRange/UpdateRange/RemoveRange: two private creates, attach baseline/no-op, explicit existing-root update intent for both roots, foreign-private read exclusion, then one homogeneous hard-delete batch and detached/physical-absence checks. Six acknowledged logical root writes per store (2create+2explicit update+2delete), two exact owned IDs, four timed operations; no benchmark matrix. Five strict Core controls run first in the same lease. Native pass remains unproven until artifact/cleanup inspection.

This cohesive qualification source carries current Developmentd403 dependency pins and the real Map caller fix; frozen timeout42a132/Core13bd is unchanged. Earlier queued narrower native cohort will be reconciled and cancelled only if still queued after replacement dispatch is visible; no accepted result is discarded.

Replacement run44948941338 is confirmed queued on55b8ba31; narrower44948216478 was still queued and cancellation acknowledged after replacement visibility. Keep original compile-failed44947147551 evidence. Current qualification alone selects all four methods on current graph; native acceptance remains open.

All-four-range native acceptance:44948941338/job136982373307 succeeded on55b8/Core7165. Strict4891byte artifact14725643611/SHAba354d9344262417cae8aff94850c4313170473b33c392c8b862edccd4be3ac7 verifies exactly2TRX+3reports,5Core+3native passed/0failed/0skipped; all8required flags/store,6logical acknowledged root writes/2cleanup identities/4timings. Exact provider-native cleanup proved. Representative isolated timings only, not production benchmarks. This closes the four-method root-batch feature slice, not broader graph/mixed-operation/full-framework or hosted acceptance. Frozen timeout Core13bd still excludes it.
