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
- Save acknowledgement/count/failed-batch root fixes pass controls below;
  native unknown outcomes and broader saved-state ownership remain unverified.
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

## Native environment preparation — 30 October 2026

Reused Node24.19.0 and existing npm cache. Locked official arweave2.1.0 SDK restored
with scripts disabled:7packages/1,502,062B under source SdkBridge/node_modules.
Pinned community ArLocal1.1.66 gateway runtime restored with scripts disabled at
`LocalState/COSMIC/ArweaveRuntime`:554packages,147,619,139B before required native
binding. One pinned git dependency avsc/Irys commit a730cc8018b79e114b6a3381bbb57760a24c6cef;
no moving branch resolution. Registry/lock integrity retained in ignored local lock.
Only sqlite3 5.1.7's N-API prebuild downloader was then invoked; no compiler fallback
or broad lifecycle scripts. Driver1,892,864B SHA256
F806F89DC41DDE00CA7124DC1E649BDC9B08FF2EFF5C891B764F3E5AEFA9548C imports successfully.

[ArLocal](https://github.com/textury/arlocal) is an archived community gateway-like
local test server with persistent mode, not an Arweave public-network node. Its
pinned source starts an unspecified-host listener; no gateway has been started.
Provide explicit loopback binding before activation. No wallet, native transaction,
fixture database or business data created yet. Native/paired acceptance still red.

Development835653f31/Coreb60796d fetched on30October. Exact next: integrate inbound
Development with task Core and matching manifest; verify scoped changes, then
address gateway listener/startup and native fixture admission. Preserve all task
work and do not deploy the unverified full task programme to Development.

## Initial empty gateway lifecycle — superseded by nonempty proof below

`Prepare-CosmicArweaveRuntime.cjs` verifies exact ArLocal1.1.66 vendor source then
repairs its native listener and stop method: explicit127.0.0.1 binding, awaited
HTTP close/SQLite persistence/Apollo stop/Knex destroy, propagated failures. No
monkeypatch/global hook, alternate startup path or suppressed teardown error. Patch
manifest retained under LocalState runtime; unknown vendor hash refuses patching.
Original app.js SHA256a3aefb60f5438c79949493b52b4b381661790849933160b82c4558a0cd1163b7;
patched SHA256d7282066e53e8548b0e74c610df5b70200905d09ae5413e173262dcf0587c6aa.

`cosmic-arweave-node.cjs` owns gateway18549/persist=true; stdin stop awaits its real
teardown. `python Scripts/TestHosts/Test-CosmicArweaveGateway.py` passed first/restart
read-only lifecycle against the same source. Native SQLite quick_check passes;
all five table counts and73,728B database hash match after restart:
f643b26168fd56546aa825318589fc94f575d07f5b6b6cef290fae04525b70c9.
One genesis block, zero transactions/chunks/tags/wallets. Both owned processes exited0;
no listener remains. This proves empty-schema equivalence only, **not** data persistence or record durability
or native CQL/paired acceptance. No public Arweave permanence/consensus claim.

Next: journal original local-only wallet/record identities, verify acknowledged saves
and multi-record CQL against actual owner-pinned gateway responses, restart, then
Mongo-paired C#/HTTP/browser5/4. SDK2.1.0/native schema compatibility still unproven.

Lifecycle evidence `TestResults/ArweaveCqlCurrent/gateway-smoke-summary.log`, SHA256 `adcb9841d7d39c25b89de8308b035a7900f97b2063c2ced2a5c5a25b80fd2f76`.

## Save acknowledgement ownership root fixes — 30 October 2026

Six controlled save tests initially4failed/2passed. Full fixed unit suite34passed.
Upload requires a syntactically valid43character native transaction acknowledgement
before IsSaved/SavedCount or caller provider key publication. Single saves count1;
manager-owned clean-state flags remain untouched. Failure/malformed receipt leaves
original key and saved flags unchanged; reconcile unknown write outcome, no retry.
Batch counts acknowledged records and reports errors even with continueOnError=true;
continue controls iteration rather than converting failed writes into success.
Messages distinguish gateway transaction acceptance from confirmation/finality.

Tests prove delayed acknowledgement, malformed/empty acknowledgement, rejected post,
batch count and all-failed continueOnError. Native record admission remains next;
no wallet or business transaction has been created yet. Root untracked `logs` file
is preserved/excluded, provenance unverified; no unrelated cleanup performed.

Evidence `TestResults/ArweaveCqlCurrent/save-fixed.trx`, SHA256 `f1788cd5b99b990e033622591bceaf39e48250a91c6a46c4eb4c713a99fb2fbf`.

## Nonempty persistence root fix and proof — 30 October 2026

Before signed business writes, source audit found initialize.down(persist=true)
drops native tables. The original awaited-stop repair inherited that wrong predicate;
empty restart merely recreated identical tables/hash and could not detect it.
That earlier test is not nonempty persistence evidence. No business data was lost.

The exact-source runtime repair now skips table teardown in persistent mode and
awaits connection closure. Known revision1 migrates explicitly to revision2; unknown
vendor/patch hashes refuse. Current patched app.js SHA256
459bb94c457609406c1104e46ca95e76dc5b8987548aa70b04a6ac79368a5856.
No reset/reseed, alternate runtime or swallowed error was introduced.

The lifecycle test now journals one synthetic local probe wallet and mints12345
local test units once. It verifies that exact original balance on every reuse,
after real gateway shutdown and process restart. One wallet/five native table counts
and SQLite73,728B hash match across restart:
a2c23d4be97e1a293bbdbe2d56fb10fc56229f63636dad2b46cc82334c3cc70b.
Original probe/ack/data retained under LocalState/COSMIC/ArweaveCql; pending absent.
Both owned gateway processes exited0; no18549listener. Probe has no signing key,
real funds, business transactions or credentials. Signed-record CQL remains unproven.

Nonempty lifecycle evidence `TestResults/ArweaveCqlCurrent/gateway-retained-probe-summary.log`, SHA256 `51edd625abefe8b4d49588fd23870226200885127c7814261d4f63e30d9e2b18`.

## Signed native fixture automatic-review gate — 30 October 2026

Automatic approval review rejected the command to create/build the signed native
fixture, stating only `blocked by policy` (no more specific reason supplied).
The command did not execute: ArweaveCqlNativeTests.cs is absent, no ProtectedData
package/reference changes, signing wallet, mint or business records were created.
Do not retry the same blocked action through another shell/tool or split commands.
Existing runtime/probe/database are preserved; gateway remains stopped. This is a
review gate, not evidence of an Arweave provider/native transaction failure.
Native signed-record/restart/paired acceptance stays red. Continue breadth with
CouchDB source/query correctness audit; no full-programme blocker declared.
