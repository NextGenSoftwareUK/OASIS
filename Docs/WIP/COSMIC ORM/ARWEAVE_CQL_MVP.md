# Arweave CQL MVP audit

Updated 29 October 2026. Native CQL/paired demo remains red.

## Verified root fix

`ArweaveService.GetTransactionDataAsync` swallowed all SDK/protocol failures and
returned null. It now rejects blank transaction IDs before dispatch and propagates
transport/invalid-payload failures to the provider's existing OASISResult boundary.
Exact acknowledged bytes remain unchanged. No legacy fallback added.
`ArweaveServiceReadTests.cs` and the protocol-only `read-boundary.cjs` fixture prove
this boundary: initial5failed/1passed, fixed full unit suite11passed/0failed/0skipped.
No native gateway/wallet was involved. Evidence
`TestResults/ArweaveCqlCurrent/read-fixed.trx`, SHA256
`ce87e415eb477af33c6767be77c3b720e4d23c4f26aa8050204f44ea739faa1b`.

## Gaps identified in current source

- No ICosmicQueryStorageProvider adapter/capability admission yet.
- SDK GraphQL bridge truncates at100 transactions without page completion evidence;
  malformed/error responses currently become empty successful lists. Fix before
  CQL relies on native tag discovery.
- GUID point load selects the first returned transaction without a specified
  revision policy; no deterministic historical/version conflict proof.
- SaveHolon mutates the provider key after posting but does not publish IsSaved;
  saved-state ownership/acknowledgement and unknown write outcomes need verification.
- Existing graph/depth/history and tombstone rules need provider acceptance tests.
- Native runtime/SDK cache, original wallet/source and free local gateway availability
  have not yet been inventoried; no installation or demo source created this step.

## Exact next step

Fix GraphQL response completeness/error semantics with protocol tests, then add a
bounded explicit-authority CQL adapter using native filters (no full-store managed
scan). Inventory/reuse existing locked arweave-js SDK and local gateway resources;
create only missing free environment with journalled original identity/source.
Prove native several-record filtered query, persistence/restart and Mongo-paired
C#/HTTP/browser5/4 before inventory enrollment. Public permanent-network payment,
finality, full parity and HyperDrive correctness remain unverified.
