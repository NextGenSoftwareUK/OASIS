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

- Bounded ICosmicQueryStorageProvider adapter now has compiler/protocol/execution
  controls below; native gateway acceptance remains open.
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

Inventory/reuse existing locked arweave-js SDK and local gateway resources;
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

## Bounded CQL adapter controls verified

ArweaveCqlCompiler reuses the existing shared bounded-identity algebra, pinned to
Arweave authority (at most100selected IDs). GUID equality/membership/AND/OR and
paging admitted; unsupported business/metadata/graph/order shapes reject before
activation. Each selected identity becomes native OASIS tag lookup **pinned to the
configured wallet's derived Arweave owner**. Public GUID copies cannot impersonate
that wallet. Wallet stays in the local SDK bridge; only its address goes to GraphQL.
Generic unowned discovery is never used by CQL.

Zero native hits skips; multiple native revisions fail CQL_REVISION_AMBIGUOUS rather
than guess latest. Owner-pinned GUID and transaction-ID tombstones skip the record.
Native payload ID and read authority verified before publication; deleted rows skip,
children excluded, complete decoded payload bounded4MiB. Paging occurs after native
selection/deletion; over-budget complete pages and any later read failure return no
partial rows. Reads may observe different moments. Cancellation checked before and
after native operations; an in-flight SDK child is not yet actively cancelled and
its output allocation is not a certified4MiB wire/process bound. Payloads currently
use the provider's existing plaintext JSON codec; no encryption claim added.

21Node protocol tests (including owner filter preservation across pages) and28full
.NET tests pass. Execution tests use an explicit protocol/service double, no native
gateway: paging, missing records, both tombstones, wrong identity, malformed data,
later transport failure, ambiguity and result budget. No native demo proof link yet.

Evidence `TestResults/ArweaveCqlCurrent/query-owner-protocol.log`, SHA256 `51c7c9890def4e2d6fa955f428b05e9dbd7a3b4383836c020c98864b015f58e4`.

Evidence `TestResults/ArweaveCqlCurrent/cql-execution.trx`, SHA256 `cc0fb4b1334db48b8c6596bf7cd04dcc49b60ed4afcccbd79caf5c6367a1c717`.
