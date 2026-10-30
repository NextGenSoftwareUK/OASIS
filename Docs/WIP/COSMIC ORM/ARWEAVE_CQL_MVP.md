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
- SDK GraphQL bridge completeness/error root fix now passes protocol tests below;
  native gateway/schema integration and CQL admission remain unverified.
- GUID point load selects the first returned transaction without a specified
  revision policy; no deterministic historical/version conflict proof.
- SaveHolon mutates the provider key after posting but does not publish IsSaved;
  saved-state ownership/acknowledgement and unknown write outcomes need verification.
- Existing graph/depth/history and tombstone rules need provider acceptance tests.
- Native runtime/SDK cache, original wallet/source and free local gateway availability
  have not yet been inventoried; no installation or demo source created this step.

## Exact next step

Add a
bounded explicit-authority CQL adapter using native filters (no full-store managed
scan). Inventory/reuse existing locked arweave-js SDK and local gateway resources;
create only missing free environment with journalled original identity/source.
Prove native several-record filtered query, persistence/restart and Mongo-paired
C#/HTTP/browser5/4 before inventory enrollment. Public permanent-network payment,
finality, full parity and HyperDrive correctness remain unverified.

## GraphQL completion/error boundary verified

The shipping bridge uses `SdkBridge/query.mjs`, one native cursor traversal path.
Each response must have HTTP200, no GraphQL errors, valid edges/transaction IDs and
boolean continuation evidence. Duplicate IDs (including across pages), invalid or
repeated continuation cursors, oversized pages and later transport failures reject
without publishing partial results. Empty complete results remain valid. Requests
preserve tag values and advance the server cursor. The explicit traversal limit is
100pages of at most100edges; exceeding it rejects rather than truncates. This is
not a distributed snapshot guarantee or a revision-selection policy.

19 Node built-in protocol tests pass;11 .NET regression tests pass, bridge syntax
and shipping module copy hash match. No npm restore/native gateway involved.
Run `node --test Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests/query.test.mjs`.
[Native cursor query structure](https://gql-guide.arweave.net/) informed the request.

Evidence `TestResults/ArweaveCqlCurrent/query-protocol.log`, SHA256 `2eb707609e7d6a4bd38bfb6594050e7e6ad7ca4ad8b9b153785b182470420700`.

Evidence `TestResults/ArweaveCqlCurrent/query-boundary-regression.trx`, SHA256 `4b6018431ec466132343c1482dd75e0e8cfb91783877d25fcb742f89841c8e6d`.
