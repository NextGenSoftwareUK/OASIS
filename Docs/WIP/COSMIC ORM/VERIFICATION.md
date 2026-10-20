# COSMIC ORM verification â€” 19 October 2026

Verification of tracking/save-state, SQLite conditional saves/deletes with durable receipts, and child traversal across MongoDB/SQLite/Neo4j. This report is maintained in source control; raw TRX/log files are local generated artifacts ignored by the existing repository policy. Recreate them with the commands below. No native chain/hosted-service certification is claimed.

## Executed results

| Suite | Passed | Failed | Skipped | New COSMIC cases | Raw TRX SHA-256 |
|---|---:|---:|---:|---:|---|
| Core unit suite | 984 | 0 | 0 | 106 | `2e474eacdd78f1e5505597c7c7e6ce3513326db4e0b5edcce6fce1e7148d236c` |
| SQLite native integration suite | 580 | 0 | 0 | 64 | `ea4452073efaad8ff610d54dd14f97891507e37e142946a8bed17af5ba25dacd` |

Core netstandard2.1 build: succeeded with 0 errors and 1041 warnings. Warnings were not suppressed by these changes.

## Earlier batch binary identity

Per-consumer Core assembly identities are recorded separately. Concurrent Core work changed binaries during this batch; these reports must not be represented as one identical-binary release certification. Native suites retain their tested consumer snapshots; the final core suite was rerun against the current workspace.

- `OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: `dcbfb9c9252277051ad8dc33860f0186d775f6ce14227baa5d8dc9b165c12dc4`
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: `cba1b06bd11ebb6409927ff143f6d6f4344dafffd9d3b7e9e18393697a8062dd`
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: `cba1b06bd11ebb6409927ff143f6d6f4344dafffd9d3b7e9e18393697a8062dd`
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: `a54a245df0eb299b63372bdd1cf54ade40a6cc764afed962caf61856c7e424b1`

Documentation checks also passed: 120 incoming Markdown links to exactly the 34 moved documents, plus COSMIC local links and current source line references. Generic README links were excluded from filename-based relocation checks.

## Reproduction

Run from C:/Source/OASIS using the existing installed SDK and restored dependencies:

```powershell
dotnet test "OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/NextGenSoftware.OASIS.API.Core.UnitTests.csproj" --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --logger "trx;LogFileName=core-regression.trx" --results-directory "Docs/WIP/COSMIC ORM/TestResults"
dotnet test "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests.csproj" --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --logger "trx;LogFileName=sqlite-regression.trx" --results-directory "Docs/WIP/COSMIC ORM/TestResults"
dotnet build "OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj" -f netstandard2.1 --no-restore /m:2 /nr:false /p:UseSharedCompilation=false
```

SQLite uses the existing TestResults/SQLiteCurrent lease and fixed disposable integration-current.db. Its fixture refuses to overwrite a pre-existing database. All provider instances were disposed; no integration-current.db/WAL/SHM remained after this run. Five redundant early-run generated reports were removed after the full suites passed; current raw suite reports and compact summaries were retained. Those reports can be regenerated with the commands above.

Free space on C: was approximately 67.4 GiB before and 67.2 GiB after this batch; concurrent workspace activity can also affect that measure. No runtime/dependency installs, restores, downloads or duplicate environments were created. Reusable build outputs were retained.

See [implementation status](IMPLEMENTATION_STATUS.md) for capability limits and remaining roadmap work.

The conditional batch additionally verifies private accepted preconditions, stable new-graph retry identities, unsupported-authority rejection before request-driven activation, simultaneous native writers, stale-child all-or-nothing rejection, provider restart/original receipt replay, changed-intent operation-ID rejection, raw-write revision invalidation, failed native graph rollback and exact retry, and soft/hard conditional deletion. Manager single/batch saves cover legacy/V2 and sync/async; deletion covers sync/async and soft/hard variants. MongoDB/Neo4j child-parameter certification is recorded as remaining roadmap work, not inferred from these SQLite results.

## Child traversal batch

The [child certification matrix](CHILD_HOLON_CERTIFICATION.md) records the exact direct-provider scope and remaining manager/query/history gates. BootLoader now constructs the canonical native Neo4j adapter; production build succeeded with zero errors. Secure/routing URI schemes and host/port validation have unit coverage.

| Suite | Passed | Failed/skipped | Report SHA-256 |
|---|---:|---|---|
| SQLite native integration suite | 573 | 0/0 | `5e9ebfc9345f39459923a42082b96ceecab78623cbfaa9fa4948f274689db0b6` |
| Shared storage suite | 456 | 0/0 | `83293417da2bc053d8e5d11493f525fa92c117345c4b939866680d3031165939` |
| Neo4j unit suite | 103 | 0/0 | `60f3b4b5806c0ce281bf6d0996e290a86193ad0bcf4e43d19f92a873f566a948` |
| Neo4j native suite | 127 | 0/0 | `55f868089196ada03df5aa2987aa7fd14e0f7b1d30484e8905f3fa28a97073b5` |
| MongoDB unit suite | 68 | 0/0 | `024a10f2f8ea490c612a818dba0d6c67c68f332b040453971bfb4156bb5527fa` |
| MongoDB native transaction suite | 52 | 0/0 | `18b504eafc846576eb6b072d6b6c1e75763e50ae632b2e75f49c2b170cca28c0` |

Reproduction additionally uses the retained `Scripts/TestHosts/Run-ReusableNeo4jTests.ps1` and `Run-ReusableUsageRecoveryTests.ps1 -CosmicGraphTraversal` / `-MongoTransactions` runners. Neo4j expected native count is 127; the selected Mongo transaction suite is 52. Both owned runtimes stopped after successful terminal runs; no listeners remained on owned ports 17687/27029. The existing Mongo log limiter reduced only the stopped owned raw runtime log from 9,810,095 bytes to a 65,201-byte tail; full current TRX evidence remains and rerunning the native suite regenerates runtime logs. No source, credentials or database reset was involved. C: free space was approximately 66.7 GiB after the batch.

The final core rerun initially detected a concurrent policy addition (`Walking100Steps = 10`) missing from portable Hyperdrive karma policy. Added that same policy entry without reverting the concurrent Core change. The existing exhaustive positive/negative weighting parity test and all 921 core tests then passed. This compatibility correction is separate from child traversal.

## Transfer and replication batch — native qualification remains open

Core suite: 943 passed, zero failures/skips, including 22 new transfer cases; netstandard2.1 build succeeded. See [transfer findings and retained fixture](TRANSFER_AND_REPLICATION.md). The full native Mongo/SQLite runner selected 12 cases and failed: four pre-existing logical-version replication assertions, one numeric JSON assertion-side cast, and seven subsequent fixture-ownership refusals. The source-identified invariant is that provider-local CAS tokens must not overwrite logical version identity. The assertion representation was corrected; the acknowledged fixture is preserved pending independent Mongo inspection. No native replay or broad cleanup was attempted.

Failed native report SHA-256: `f619c147f8ebeb39a79c69d159a4858138a733576fc1ea4c1e2574ad964687c6`. This report supersedes any implication that the current full routing matrix passes.

The absent native test assets file was regenerated using `dotnet restore` with `--source C:/Users/david/.nuget/packages --ignore-failed-sources --disable-parallel`; this reused existing cached packages and did not download/install a runtime or create another environment. Existing owned runtime stopped after the terminal native run.

## Provider-local revision correction and native qualification

This batch supersedes the earlier provisional transfer failures below: all 12 MongoDB/SQLite routing cases pass, including eight explicit transfers. SQLite now distinguishes provider-local CAS tokens from logical version lineage. The retained original transfer fixture was independently inspected in both stores before cleanup; fresh-process Mongo discriminator registration and embedded nested-child persistence were corrected. Mongo unit suite: 70 passed. Targeted migration/model/token checks: four passed. See [transfer evidence](TRANSFER_AND_REPLICATION.md) for ownership and migration-hang reconciliation.

Latest report identities (SHA-256):

- `TestResults/SubscriptionUsageCurrent/mongo-sqlite-v2-current.trx`: {'total': '12', 'executed': '12', 'passed': '12', 'failed': '0', 'error': '0', 'timeout': '0', 'aborted': '0', 'inconclusive': '0', 'passedButRunAborted': '0', 'notRunnable': '0', 'notExecuted': '0', 'disconnected': '0', 'warning': '0', 'completed': '0', 'inProgress': '0', 'pending': '0'}; `ba06342356cd5280bb9a6ec4d85de76ddc5575a4fc54188de2289ae60ad7ad34`.
- `TestResults/SubscriptionUsageCurrent/cosmic-transfer-original-reconciliation.trx`: {'total': '1', 'executed': '1', 'passed': '1', 'failed': '0', 'error': '0', 'timeout': '0', 'aborted': '0', 'inconclusive': '0', 'passedButRunAborted': '0', 'notRunnable': '0', 'notExecuted': '0', 'disconnected': '0', 'warning': '0', 'completed': '0', 'inProgress': '0', 'pending': '0'}; `5706132c3a31876689d174c5543db3f00825b8f0dd2ffd9df6ddc1e188a3dc82`.
- `Docs/WIP/COSMIC ORM/TestResults/provider-revision-migration.trx`: {'total': '4', 'executed': '4', 'passed': '4', 'failed': '0', 'error': '0', 'timeout': '0', 'aborted': '0', 'inconclusive': '0', 'passedButRunAborted': '0', 'notRunnable': '0', 'notExecuted': '0', 'disconnected': '0', 'warning': '0', 'completed': '0', 'inProgress': '0', 'pending': '0'}; `b06fa1f8a6ac2d66db0e47c44c9d4b240d9574d3f8569e17923e14d39971108b`.

Current consumer Core DLL hashes (recorded separately; no identical-binary release claim):

- `OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: `dcfbb2f0499da619dfbcf8b70eea812682ed31e98f081739cb060556dd8650d5`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: `dcfbb2f0499da619dfbcf8b70eea812682ed31e98f081739cb060556dd8650d5`.
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: `dcfbb2f0499da619dfbcf8b70eea812682ed31e98f081739cb060556dd8650d5`.

After verification: no Mongo listener on owned port 27029; owned transfer SQLite file absent. Reused installed runtimes and offline package cache. No deployment or commit performed. Legacy deletion, durable outbox integration, remaining provider/child/history coverage, placement, deltas and CQL remain open.

## V2 outcome/deletion follow-up

Full core regression: **968 passed, zero failed/skipped**. Eighteen new cases cover six native-uncertainty flag combinations, two import publication/replay cases, four all-target deletion identity cases and six invalid deletion acknowledgement cases. Two other cases came from concurrent workspace changes. Tests preserve the original provider payload/cause and assert no unsafe downstream writes. No legacy deletion implementation was modified; legacy findings are documentation-only by user direction. Earlier native provider results above predate this router-only follow-up and are not a claim of native certification of the new deletion contract.

Latest unit consumer Core SHA-256: `e459467004cc10a6f65354efc675e488595de4d10ea38ac8efec6da9a3a35247`.

## V2 manager deletion follow-up

Current full core regression: **984 passed**, zero failures/skips. Current full SQLite suite: **580 passed**, zero failures/skips, 51 seconds. Sixteen new unit cases cover root-only detached tombstones, captured authority and direct hard deletion across all overload shapes. Four native SQLite cases prove complete child-row/token preservation. The existing typed diagnostic test retains every diagnostic assertion and now rejects prerequisite reads. Core netstandard2.1 build: zero errors, 1041 warnings. No package downloads or new environments. Owned SQLite database absent after the terminal successful run; free C: space approximately 66.43 GiB during verification.

These results do not certify MongoDB/Neo4j embedded-child preservation or cross-provider tombstone delivery. Legacy Hyperdrive remains documentation-only.

- `Docs/WIP/COSMIC ORM/TestResults/v2-delete-sqlite.trx` SHA-256: `569911a5c9e2fe4b6b94a654a977688d67d7bf8db948e8fee7ae04190eeb363b`.
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll` SHA-256: `aea1a74eef09109f78e11ea7229ba77d4b01534d4aec7321239af01efe0e0bb0`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll` SHA-256: `689dd324591371b9156f73c1875121a6c024957da73df208198c6d7d6380cb06`.

## MongoDB root-only and owned inventory follow-up

The four native child traversal cases now include root-only excluded-child tests: exact embedded BSON preservation, byte-identical native child/grandchild records, unrelated submitted child exclusion, and both embedded/native reloads. The selected native transaction suite passes **52/52** (30 seconds), and MongoDB unit suite passes **71/71**. The added unit case verifies owned-inventory versus graph serialization and renders a typed quantity query through the owned member serializer.

A first root-only attempt failed in pre-dispatch inherited-member lookup. Original fixture reconciliation passed one independently verified native case before cleanup, with record identities/parent links/BSON hashes in its TRX. A separate full-suite run exposed two inventory serialization failures; its original report is retained once. The corrected member-scoped owned-value serializer preserves graph semantics and all existing native inventory assertions now pass.

Latest evidence (SHA-256):

- `TestResults/SubscriptionUsageCurrent/mongo-cosmic-children-current.trx`: `4675d26157cc0a06f68b4b260b8864e5dd2f926ddf1171afa89dee0a97ab010b`.
- `TestResults/SubscriptionUsageCurrent/mongo-cosmic-child-reconciliation.trx`: `0a9cc9521886cdaf48ac44234dce4a3556a20478be74ee230284d0b6d3f41d4e`.
- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: `b4a546edf527648928e55982ad76c6827ea1818432ea4ba13bbd85159e64f4a0`.
- `Docs/WIP/COSMIC ORM/TestResults/mongo-unit.trx`: `a6d3b15ab6cf7e1fa8a598438c2e0f1f0148cc086fb97971d27801167b4e90a5`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `46a1cfede570fef481aee27d3119cd99f7ce3b59c6e54f2c6fb6c05521e2d378`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `3ae31b043426c2055eb081bd231c9798953995c309e3d98685aa0e91445936c7`.

Native and unit consumer snapshots are recorded separately: native serialization passed before the final array query-introspection interface was added; the current unit suite verifies that interface and filter rendering. No identical-binary release certification is claimed. Installed MongoDB and the existing retained environment were reused; no downloads. Owned port 27029 has no listener after the terminal run. Free C: space approximately 66.13 GiB. The retained unrelated inventory fixture/database was not removed. Depth-limited/bulk graph frontier preservation is not yet certified.

## MongoDB graph depth-boundary follow-up

- Shared suite: **484 passed**, zero failed/skipped, including six new `SelectsChildren` contract cases; concurrent additional cases are not attributed to this batch.
- MongoDB unit suite: **71 passed**, zero failed/skipped.
- Selected native MongoDB transaction suite: **52 passed**, zero failed/skipped, 38 seconds.
- Subsequent native traversal suite: **four passed**, zero failed/skipped, including V2 manager pending-edit assertions added after the full transaction run. Each case covers the direct bounded/bulk parameter matrix and native/embedded graph reloads.

The native cases inject an unknown BSON field with exact integer `9007199254740993` at an excluded boundary and verify exact frontier BSON, unchanged standalone grandchild bytes, selected acknowledgement scope and manager source ownership. The existing transaction continues to own observation, preparation and all selected mutations; no post-commit repair write was introduced.

Latest report and consumer snapshot hashes:

- `Docs/WIP/COSMIC ORM/TestResults/shared-unit.trx`: `cea155c422188ecd3414114c5ef54972b6aeaacdae7691a13d630a9a52884677`.
- `Docs/WIP/COSMIC ORM/TestResults/mongo-unit.trx`: `61273f5ffd55711ffb24fe6995f3f4d520be1e23d9cf4ccb952e6fdb45aaa9b0`.
- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: `906f1e537d0aed198e734f0d0906951bcdede5ef4918fd64c4c3c0dcdaca4aea`.
- `TestResults/SubscriptionUsageCurrent/mongo-cosmic-children-current.trx`: `5ef32336417c074ced3648eb2290496a834a33e7004d40555a9aec7e13be3f05`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `fbc189cb9b3d926e291a3113fa904dc44cf9be34702b5ced8f2b07e7da291b09`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `fbc189cb9b3d926e291a3113fa904dc44cf9be34702b5ced8f2b07e7da291b09`.
- `Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/bin/Debug/net10.0/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll`: `9d7ae87495a939755bd6b3ea02db27398876274a397fd3b4013250a7acd0f7bc`.

Owned native runtime stopped normally; no listener on port 27029. The existing stopped Mongo raw-log limiter retained a 65,010-byte tail from 14,046,380 bytes without modifying databases or the runtime. No downloads or duplicate environments. Free C: space approximately 66.09 GiB. Core/SQLite/Neo4j results from earlier batches are historical evidence and were not rerun for this provider/shared-only correction. No deployment or commit performed.

## MongoDB provider-local token prerequisite

Current MongoDB unit suite: **72 passed**, zero failures/skips, including one new mapping/isolation case. Selected native transaction suite: **52 passed**, zero failures/skips, 39 seconds; its four extended traversal cases now assert token rotation despite submitted observations, unchanged logical versions, and selected child acknowledgement tokens matching persisted physical fields. Ordinary graph preparation now resolves all selected native ownership fields before response materialization and ancestor serialization.

No atomic conditional-write capability is claimed yet. The adapter still needs native token comparison plus receipts in one transaction. No Core/SQLite/Neo4j implementation changed in this batch, and their earlier reports were not rerun.

Latest hashes:

- `Docs/WIP/COSMIC ORM/TestResults/mongo-unit.trx`: `c751249381f351cf748550a51c33720199418f1c0be9e8d21d732c04565b537c`.
- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: `d9eca9bc9eef0518ba250092f02fc655132bf4b76d64425ca9b434433d88304f`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `b37b667783af611ab53e92b1eb3e972149a96f074dd94245d4625b8ff76af281`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `b37b667783af611ab53e92b1eb3e972149a96f074dd94245d4625b8ff76af281`.

Reused the installed runtime and retained MongoDB environment. No downloads, deployment or commit. Fixture cleanup and runtime shutdown use the existing runner ownership rules.

## MongoDB atomic conditional save/delete batch

Current selected native MongoDB suite: **54 passed**, zero failed/skipped, 40 seconds. Current MongoDB unit suite: **75 passed**, zero failed/skipped. The two new native theory cases exercise sync/async scenario sequences including provider reconstruction/replay, changed actor/content rejection, stale selected child rollback, conditional version/audit ownership, concurrent updates/create-only/same-operation writes, forced receipt insertion rollback and exact retry, corrupt receipts, legacy missing tokens, soft/hard deletion and V2 manager publication/conflict ownership. Three new unit cases reject invalid/cancelled requests before an unactivated repository can be reached.

The reusable full runner builds current shared/Mongo producer assemblies before the native consumer. No new runtime/environment or download was used. Existing native transaction and child-boundary cases remain in the full suite. Process-crash and lost-commit-response injection remain unverified, and are not inferred from provider reconstruction.

Reproduce: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -CosmicConditionalWrites` for the two conditional scenarios, or `-MongoTransactions` for all 54 selected cases. The latter report is the final post-legacy-token assertion verification; earlier focused reports precede those added assertions.

Latest SHA-256 evidence:

- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: `b3fce1535f9daf0c64c4e7a2c4203b7c33b257c8db6126f1a77275fdf5a756c7`.
- `Docs/WIP/COSMIC ORM/TestResults/mongo-unit.trx`: `de36d376c70b2d3afe79b7c6100fe5ee24c5d888ec5952788e710d4995c8e8d7`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `ec868a8d68fe708500453e0b1cbc663a7300cf92db5d29267b7c29192c3867d2`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `ec868a8d68fe708500453e0b1cbc663a7300cf92db5d29267b7c29192c3867d2`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `ec868a8d68fe708500453e0b1cbc663a7300cf92db5d29267b7c29192c3867d2`.

Source remains local on the existing Development branches. No commit, push or deployment was performed. Owned databases are removed only by successful fixture lifecycles; the retained unrelated reusable inventory database is preserved. Free C: space was approximately 65.85 GiB after this batch.

## MongoDB uncertain commit and causal receipt resolution

Final native suite: **54 passed**, zero failed/skipped, 38 seconds. Unit suite: **75 passed**, zero failed/skipped. The two conditional theory sequences now include a real app-scoped `commitTransaction` write-concern error 64. Command subscribers verify native inserts occur once, the uncertain transaction is not aborted, original session/transaction/native cause remain available, and caller IDs/versions stay unpublished. Independent majority receipt reads are causally constrained by the native reply's operation time. Exact receipt replay after that proof publishes clean caller state without additional inserts or commits.

The first assertion used an unqualified majority snapshot and retained its fixture when the receipt was not yet visible there. The runner force-terminated its owned Mongo process. Original-only inspection after restart independently verified six synthetic rows and 14 valid receipts, including one majority-visible uncertain-commit receipt, before scoped cleanup. Relevant original identities: operation `8f4c9e32-005d-4956-9530-333900612832`, root `bf9bc549-5239-e74c-b8ea-da1ddb6197c8`, child `cebf9b81-1dbd-5bd2-0a98-5e5b49d632c9`. Original receipt SHA-256: `408AC1B5495C264B6ABA9852E6CED674BFD2A0010F1EB48A02735659ECC09416`. Detailed record hashes are retained in the inspection TRX. No save was replayed to discover that original outcome.

This proves the stated post-commit acknowledgement fault and retained-fixture forced-restart recovery, not all dropped-network-response or crash timing modes. Dedicated repeatable crash/drop-response automation and operator reconciliation remain open.

Latest SHA-256 evidence:

- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: `8de70e8ef3a7b55a312e8224696f308080b5a5343ca29c51b26d8e9693190ae5`.
- `Docs/WIP/COSMIC ORM/TestResults/mongo-unit.trx`: `3b403dca6ec6f19b8ac825722de3f89c0acd87348537000bea269aa73a42c2b3`.
- `TestResults/SubscriptionUsageCurrent/mongo-cosmic-conditional-reconciliation.trx`: `0be807ffb2a5c9bd7ff2f55a4f9652ba814e0d070748744f2ec832d3fa00c25e`.
- `TestResults/SubscriptionUsageCurrent/mongo-conditional-before-causal-receipt-read.trx`: `07ec60423ad1fe273fa036186b4820ae0121fcaafedbf888fcfcae4780795dec`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `43239b55aeac209626e81abb9c23819ec23f09bde4f55e99064d4e4122a46a42`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: `43239b55aeac209626e81abb9c23819ec23f09bde4f55e99064d4e4122a46a42`.

The inspection/failure reports are retained once; current reports overwrite fixed paths. Existing runtime/data directory were reused; no downloads or duplicate database environment. Owned port 27029 has no listener after the final run. Free C: space was approximately 65.86 GiB during verification. No commit, push or deployment was performed.

## Neo4j local revision prerequisite

Neo4j unit suite: **108 passed**, zero failures/skips, including five new physical-token mapping/integrity cases. Full native suite: **127 passed**, zero failures/skips, 45 seconds. Four existing child traversal cases now exercise root-only token rotation, preservation of logical version 4 with history disabled, unchanged descendant canonical snapshots/tokens, native/DTO token agreement, legacy missing physical tokens and isolation from spoofed submitted observations. All existing history, routing, timeout and codec cases remain in the native suite.

This verifies ordinary local-token ownership, not conditional-write support. Activation currently has no holon uniqueness constraint; native identity/receipt constraints plus atomic token comparison and receipt persistence remain required. No conditional capability was advertised.

Latest evidence SHA-256:

- `Docs/WIP/COSMIC ORM/TestResults/neo4j-unit.trx`: `f7c1980bb1d9152d27cfc71c66b2fa55d5a5139b6a4991f50eb5503bbb71b73a`.
- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: `a1192e78d3f6df86da0de752677eb3c51fd09a8cf8a9be77358aa82b540d8ab3`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: `8c3c831a5f722383c27a746c71cdf088ac8e14e4bb916b60624b35e7806580d4`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: `8c3c831a5f722383c27a746c71cdf088ac8e14e4bb916b60624b35e7806580d4`.

Reused retained container `oasis-neo4j`, exact pinned image checked by the runner. It returned to stopped/exited state after verification. Native fixture cleanup used the original per-record ID scopes. No installation, download, new environment, commit or deployment. Free bytes before/after: 70,752,759,808 / 70,750,646,272 (about 65.89 GiB). Core/MongoDB/SQLite suites were not rerun for this Neo4j-only batch.

## Neo4j native uniqueness and activation schema

Unit suite: **112 passed**, zero failed/skipped, including four new sync/async schema admission cases. Final full native suite: **129 passed**, zero failed/skipped, 47 seconds. Two additional native races create the same holon ID / receipt operation ID from independent sessions and verify one winner and one native constraint rejection, then clean up only the fresh owned identity. Existing 127 native cases remain intact.

The focused `-CosmicSchema` run first established and tested the real constraints (two cases passed). The subsequent final full run exercises the completed shape-first activation path; unit probes verify bootstrap, incomplete-schema rejection, original-error retention and no DDL when the guarantee already exists. No native low-privilege account/grant matrix is claimed from the simulated DDL-denial unit case.

Reproduce with `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicSchema` or omit the switch for all 129 native cases. Native constraints remain in the reusable service as required provider schema; no existing user data or constraints were dropped. Conditional capability remains disabled pending atomic lock/comparison/receipt work.

Latest SHA-256 evidence:

- `Docs/WIP/COSMIC ORM/TestResults/neo4j-unit.trx`: `d2de036c44f6eb23ce51c07dbdacf15b5cd0f4a2418e751a20210e6b0113eee4`.
- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: `9f43e771f43a02f143ad3e4657c69a0d22c31c9e6f652c5cfe18bf38b10d8f11`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-schema-current.trx`: `58b21040deb13b4a30dd626c7e9595091f4ab248e852e96e1e305ddf242c4cf8`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: `4f369b43e432c71f9243ee5e4a9f10a0874f087eaadc21090247935161efac33`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: `4f369b43e432c71f9243ee5e4a9f10a0874f087eaadc21090247935161efac33`.

The focused report predates the shape-first refinement; the final full/unit reports cover the current code. Existing stopped-container identity was checked before each run; no runtime download, installation, new environment, commit or deployment. Final native free bytes before/after: 70,707,703,808 / 70,708,817,920 (about 65.85 GiB). Core/MongoDB/SQLite suites were not rerun for this Neo4j-only batch.

## Neo4j conditional completion checkpoint — 19 October 2026

- Unit: 117 passed, zero failures/skips. Report `TestResults/neo4j-unit.trx`; SHA256 `EADE0A80C27FBB9290BD1E0A583F96109980D86290573047858147C2A4A87E6B`.
- Full native: 134 passed, zero failures/skips, 46 seconds. Repository report `TestResults/Neo4jCurrent/neo4j-native-current.trx`; SHA256 `D1939801CBBF6367958D6EB90BDEA19E17201E1821D502A0C06AF158E3FBF8B5`.
- Focused conditional: four theory cases passed; report `TestResults/Neo4jCurrent/neo4j-cosmic-conditional-current.trx`; SHA256 `3D403BC8D2C16B5C6EFFC2898F2488027148D0E1870BB2B8DBD10673E3A3FA6F`. Each theory contains multiple scenario assertions; these are not counted as independent test cases.

The final full run includes all three native uniqueness races and the latest post-real-commit application acknowledgement exception scenarios. Independent native receipt inspection occurs before replay. The fault proxy throws after the real SDK commit; actual transport loss/process termination was not injected. Two unit cases cover validated receipt cleanup failure. Earlier focused schema reports predate the third history identity case; current full-suite evidence covers it.

Runner reused the retained service and bounded builds. `podman inspect oasis-neo4j` confirmed `exited` after completion. Free bytes before/after: 70,690,201,600 / 70,655,033,344. No download, runtime installation, deployment or legacy Hyperdrive repair occurred. Compact logs: `neo4j-native.log`, `neo4j-unit.log`, `neo4j-conditional-native.log`. This checkpoint verifies the Neo4j batch, not whole-roadmap completion.

## Neo4j parent assignment checkpoint — 19 October 2026

- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: **142 passed**, zero failures/skips; SHA256 `7FB69CE9631B19B0356B0AF2E2A744F4E39EB5C034D0C221D6A3048B89D3A649`.
- `Docs/WIP/COSMIC ORM/TestResults/neo4j-unit.trx`: **117 passed**, zero failures/skips; SHA256 `DD0EDF37C49C64956AB14904ADF07067ECA4C5C076004B3FBB96171D02B7961A`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-parent-assignment-current.trx`: **8 passed**, zero failures/skips; SHA256 `E1F0BAD40F9E9ED1DAB00938503822CA18041D8F4AA0DA9EFB29615ED527E622`.

The final full run rebuilt the current producer/consumer and includes all eight new parent-assignment cases plus explicit history-count rollback assertions. Its duration was 51 seconds. The earlier focused eight-case run preceded the additional history-count assertion and the incoming-direction cycle-query optimization; the final full run verifies both. The unit run preceded that native query-direction-only optimization. No identical-binary claim across reports is made.

Current native graph owner source SHA256: `7A5B0BA64FF81796A0CBF6AAD8AB6C6F117B6A9041CA3EF1F8FC90001EF67212`. The reusable runner enforces exactly 142 full native cases and provides an eight-case `-CosmicParentAssignment` mode. Fixture cleanup uses only recorded synthetic IDs/operation IDs after complete success; failed fixtures would be retained. Native container stopped normally. Free bytes before/after the final run: 70,617,784,320 / 70,568,284,160. No installation or duplicate service was created.

This establishes locally present parent move/unlink and transaction rollback semantics for the canonical Neo4j adapter. It does not certify MongoDB embedded membership, SQLite parity, remote-parent arrival/backfill, migration of previously inconsistent graphs, historical graph coherence, aggregate membership revisions or all provider implementations. Legacy Hyperdrive remains documentation-only.

## Neo4j ordinary transaction outcome checkpoint — 19 October 2026

- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: **158 passed**, zero failures/skips; SHA256 `B081139411AE32EAECC0B235301DCDC17811CD3B8431E671A92ABDF1D1530D8D`.
- `Docs/WIP/COSMIC ORM/TestResults/neo4j-unit.trx`: **117 passed**, zero failures/skips; SHA256 `56B8DDA90695DEEC41202DD4072AC153888834FC60C8A1814A477CA0CB743CE4`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-ordinary-outcomes-current.trx`: **16 passed**, zero failures/skips; SHA256 `BF227B63931F1E03BD02273675B2E62E1690BF4B7EFFA082599D88859C39DEF1`.

The final 49-second native run rebuilt the current producer and consumer, includes all sixteen new outcome cases, the amended holon acknowledgement-validation error assertions, and all eight parent-assignment cases requiring definite rollback after cycle rejection. The focused sixteen-case report preceded whitespace-only indentation and the older payload-test assertion update; the final full run covers the current sources. Unit tests include the two conditional receipt replay cleanup cases. Reports use reusable fixed paths: old checkpoint hashes are historical digests, not claims that the latest file still has that old digest.

Faults delegate actual queries, rollback and commit to the real SDK, then inject application acknowledgement/disposal exceptions. Independent native reads prove absent rows after rollback and present original rows after a real commit. Tests assert one commit or rollback attempt, no abort after commit dispatch, unchanged caller identities for uncertain outcomes, committed graph/count/warning propagation for known outcomes, and retention of all query/rollback/cleanup causes. This is not actual network-drop or process-crash certification.

Current graph-owner source SHA256: `720726E27BAB2856FB98F63351D52A9D915904E9699CBFA6DF59C1A4BACD686D`. The runner enforces 158 full cases and sixteen in `-CosmicOrdinaryOutcomes` mode. Synthetic fixture cleanup follows successful independent inspection and uses only recorded native IDs. The reused container stopped normally; no runtime/service installation occurred. Free bytes before/after the final native run: 70,475,730,944 / 70,475,038,720.

Ordinary saves have no durable operation receipt and are not automatically replayed after uncertain commit. Avatar/AvatarDetail-specific mutation owners and other providers require their own outcome certification. Legacy Hyperdrive was not changed. The full roadmap remains active.

## Hosted fan-out delivery invariants — 19 October 2026

Full core unit report: `TestResults/core-regression.trx`, **1,001 passed**, zero failures/skips. SHA256 `362E9D19D9921D18D447FB8E28989C377231020C51673738A97EB19F7F06916B`. XML inspection confirms thirteen `OASISProviderFanOutHandlerTests` cases in that report: the original three plus ten new cases. The full count also includes concurrent contributors' tests; this batch does not claim authorship of all increases. The earlier focused twelve-case report predates the additional negative-acknowledgement theory row; current full evidence covers it.

Producer and consumer were rebuilt from current sources with bounded workers and project-reference builds disabled to reuse dependencies. Core net10.0 build: zero errors, 130 warnings; netstandard2.1 build: zero errors, 122 warnings. Logs: `core-build.log`, `core-netstandard-build.log`, `core-regression.log`. These are compiler totals, not a claim that every warning was introduced or resolved by this batch.

Tests prove immutable accepted delivery copies across mutating targets, continued later targets after thrown/returned failures, native cause aggregation, direct/wrapped reconciliation markers overriding success flags, independent target timeout versus caller cancellation, known-committed cleanup warnings, and absent/negative acknowledgements. The original registered idempotent-target contract is retained. Native direct CRUD enrollment, generic replica targets and crash/restart delivery are not established by unit tests; required gates are in [durable CRUD delivery](DURABLE_CRUD_DELIVERY.md). No new service/runtime/download or legacy Hyperdrive change occurred.

## Committed replica receiver checkpoint — 19 October 2026

- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: **163 passed**, zero failures/skips; SHA256 `A1D38477ACF919B46673286B096D54159D0FF8EA0A9B0671858EC85172CAA3D5`.
- `Docs/WIP/COSMIC ORM/TestResults/neo4j-unit.trx`: **117 passed**, zero failures/skips; SHA256 `110B99B6B344CD48CA35DEE49878049AF213323407066EC729818D1D37C0FDC6`.
- `Docs/WIP/COSMIC ORM/TestResults/shared-storage.trx`: **502 passed**, zero failures/skips; SHA256 `762DFCF33FD082333F43835E3B81C4EE9CFEE058BF912BC12440E244A0D5138C`.
- `Docs/WIP/COSMIC ORM/TestResults/core-regression.trx`: **1001 passed**, zero failures/skips; SHA256 `ED44CB7C05F2D92E610E5F02D73AF11BD27D0E0D4E05DE678D25DA495E4020D4`.

The final full native run lasted 58 seconds and rebuilt current Neo4j producer/consumer. It contains four replica theory sequences with actual SQLite committed graphs, target history off/on and sync/async, including head snapshot checksum rejection. Earlier focused four-case reports predate the checksum addition; the final full report is authoritative for that assertion. Native total increases from 158 to 163: four replica cases plus the fourth native uniqueness case. The unchanged 117 unit count includes updated four-constraint lifecycle assertions. Shared XML identifies twelve new replica-plan/receipt cases; its total also includes concurrent contributors' work. Core total is 1,001.

Both core target frameworks build. A netstandard2.1 compile rejected an unqualified `Enum` reference; the boundary now explicitly uses `System.Enum.IsDefined`. The final net10.0 build was rerun after that portability-only qualification; earlier test binaries predate that qualification. No identical-binary claim across those reports is made. Core logs are `core-build.log` and `core-netstandard-build.log`; native/shared/unit logs use their existing fixed paths.

The native sequences prove receipt-first replay after provider reconstruction/intervening writes/deletion, source logical/audit preservation, ordered application and gaps, local raw-write detection with unchanged logical IDs, durable hard-deletion heads, stale-create rejection, duplicate workers, concurrent ordered events, graph/head/receipt rollback on invalid acknowledgement, and checksum rejection without overwriting current data. The lost-response fault delegates a real SDK commit and then throws at the application boundary; independent native inspection precedes replay and commit count remains one. Actual process kill or wire loss is not claimed.

The retained Neo4j service stopped normally (`exited`). The owned source database `TestResults/Neo4jCurrent/committed-replica-source.db` is absent after successful fixture cleanup. Selected target rows, replica heads, history and receipts are cleaned only by their recorded synthetic IDs/operation IDs after success. Free bytes before/after final native run: 70,313,897,984 / 70,075,555,840. No installation or duplicate environment was created.

[Receiver contract](COMMITTED_REPLICAS.md) records material remaining gates: other receivers, hosted target adapter, baseline/source authorization, primary atomic outbox enrollment, crash recovery, history/migration/encryption/retention and later deltas/CQL. No legacy Hyperdrive implementation was changed.

## SQLite persistent-value mapping checkpoint — 19 October 2026

- `Docs/WIP/COSMIC ORM/TestResults/sqlite-regression.trx`: **597 passed**, zero failures/skips; SHA256 `890B81D3FF9FD1A5B31342E1E604715D364456E0116A21B38074C3CA7BE8979A`.
- `TestResults/SQLiteCurrent/sqlite-persistent-values-current.trx`: **17 passed**, zero failures/skips; SHA256 `500E4A6904D8D927169452869AA4B2950323672B1B421DC6A69152518DD4CBDA`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-replica-current.trx`: **4 passed**, zero failures/skips; SHA256 `FAD2FCA37ABA77208562D3A54612059810AAF9BD5CD1C0CDCE011D0E9E85B699`.

The full SQLite run lasted 47 seconds and includes seventeen new mapping/receipt/corruption/schema-upgrade/EF-migration cases. The producer and consumer were rebuilt before testing. The final focused seventeen-case run additionally verifies that rejected downgrade leaves no `__EFMigrationsLock` row; that assertion was added after the full run, and no production code changed afterward. Both up/down paths execute real EF migrations, and existing runtime-versus-snapshot model-differ cases pass in the full suite. Producer build: zero errors, 37 warnings.

The updated four-case cross-provider run rebuilt the SQLite/Neo4j producers and native consumer, then used actual SQLite conditional commits with custom key, extra parent ID and origin metadata. These values survive at Neo4j while source-supplied target-local metadata is excluded. This focused run uses the current SQLite mapping; the earlier full 163-case Neo4j report predates this richer source fixture. Do not interpret that earlier report as current SQLite full-suite evidence.

Initial focused tests found the pre-existing constructor-new flag on receipt replay: fifteen passed and two failed. Clearing that runtime flag for every replayed committed node fixed the cause; the seventeen-case focused and 597-case full results above pass. No source/receipt persistent format was discarded or rewritten to make replay succeed.

Owned SQLite fixture databases and sidecars are absent after normal lifecycle cleanup. The reused Neo4j container stopped normally. Cross-provider run free bytes before/after: 68,847,706,112 / 68,846,792,704. No runtime/tool download or duplicate environment was created. [Mapping and migration contract](SQLITE_PERSISTENT_VALUES.md) states remaining receiver and codec/history gates. Legacy Hyperdrive is unchanged.

## SQLite committed receiver checkpoint — 19 October 2026

- `Docs/WIP/COSMIC ORM/TestResults/sqlite-regression.trx`: **616 passed**, zero failures/skips; SHA256 `4594302573A7A7D5588C46C22866EACB1C1F5D2C497D7681649DF595BA45FF3A`.
- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: **165 passed**, zero failures/skips; SHA256 `DDD36A905ACF05C0C8D0599FC58454B30D06AA808E3A7E16DD8DDDC91CAD392D`.
- `Docs/WIP/COSMIC ORM/TestResults/shared-storage.trx`: **510 passed**, zero failures/skips; SHA256 `D6906F1690CEE6F226391DB37089E476781A27FA95F1C6F0C6470B89A8FD5712`.
- `Docs/WIP/COSMIC ORM/TestResults/mongo-unit.trx`: **75 passed**, zero failures/skips; SHA256 `BCAD70F4CE52F1DDEE69BF98966575685CF6943D249366A8C68433351AAA913D`.
- `Docs/WIP/COSMIC ORM/TestResults/neo4j-unit.trx`: **117 passed**, zero failures/skips; SHA256 `6444D5486FEFACB4798E6C3A7F1CD0AEE2E60BAA2E7F13D3379CD5E5571B924A`.

Final full SQLite and Neo4j runs rebuilt/copy-loaded the current producers and consumers after the shared single-capture/topology changes. SQLite has nineteen new cases beyond 597 (ten receiver/transaction/topology/head-schema and nine audit-presence/metadata cases). Neo4j adds two reverse-provider cases to the prior 163. Shared includes two new source getter/topology capture cases; its final 510 total also includes concurrent contributors' additions (the earlier 504 count was superseded). MongoDB's producer and unit consumer were rebuilt against the current shared plan; no native MongoDB receiver/delivery guarantee is implied by those unit results. Neo4j unit count remains 117 and passes with the current shared assembly.

Native receiver tests cover actual separate-SQLite source commits, duplicate competing workers, ordering, replay after reconstruction/intervening writes/deletion, local raw-write conflict, retained head checksums and source binding. Five fault cases cover a real committed transaction followed by an injected application acknowledgement exception, late cancellation, head insertion rejection, receipt insertion rejection and known-result connection-cleanup failure. Independent native inspection proves graph/head/receipt state before replay; commit count stays one. Physical transport loss/process termination remain unverified. Two reverse cases use actual Neo4j commits with nested JSON, large integer, custom key/extra parent and null creation-audit enum.

The first source fixture run failed because literal `{}` in a test SQL format string was not parameterized. An original-only read-only inspection verified four expected source rows, nine primary receipts, one actor and no other populated tables before deleting that owned disposable source database. Evidence: [retained source inspection](TestResults/sqlite-replica-source-reconciliation.md). No primary mutation was replayed. Subsequent tests identified and fixed audit null-to-default conversion, JSON-token serialization mismatch, and deletion-UTC validation being normalized before rejection. The final suites above include the relevant regression cases and pass.

Schema tests execute the real replica-head/audit-presence EF migrations, compare the runtime model/snapshot, reject lossy downgrade and verify native lock release. The earlier focused receiver report predates the final full set of audit/codec and capture assertions; current full reports above are authoritative. Normal warnings remain in build logs; no dependency/runtime installation or second environment was created. SQLite fixture/source databases and sidecars are absent after lifecycle cleanup; retained Neo4j container is `exited`. Final Neo4j run free bytes before/after: 68,319,657,984 / 68,317,790,208.

[SQLite receiver contract](SQLITE_COMMITTED_REPLICAS.md) records remaining MongoDB receiver, hosted adapter, source atomic outbox, recovery/history/relationship/security/migration/placement/delta/CQL gates. No legacy Hyperdrive branch was changed and the full roadmap remains active.

## Mongo audit precision and receipt cleanup checkpoint — 19 October 2026

See [implementation and explicit remaining limits](MONGO_AUDIT_PRECISION.md). No legacy Hyperdrive implementation changes. Existing pinned MongoDB runtime and replica-set environment reused; no installs/downloads. All commands bounded with `/m:2 /nr:false /p:UseSharedCompilation=false`; producer builds preceded consumer tests.

| Report | Passed | Failed | SHA256 |
| --- | ---: | ---: | --- |
| `TestResults/CosmicMongoCurrent/mongo-unit-current.trx` | 86 | 0 | `d3d914ab8e84cf614f13feb51032a4a695e5a399e7b2f6132b767bcc58ecf79c` |
| `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx` | 54 | 0 | `cbcc4500f14812d102dfc3738f6131379e44dd1eaec513f5999c2eb7d1e4ac4a` |
| `TestResults/SubscriptionUsageCurrent/mongo-cosmic-conditional-current.trx` | 2 | 0 | `d10005aeaed5f0bb4cc043f2614466d3539f8fc3cd1001ce205e9ee42e37d4e8` |
| `TestResults/SubscriptionUsageCurrent/mongo-cosmic-conditional-reconciliation.trx` | 1 | 0 | `ddd44f0219d4903c3979022e54d1ca8fe7c73234cc57442defe15cb90b891942` |

The 54-case native suite used the production changes and initial abort-cleanup fault coverage. The final two-case conditional run additionally exercises combined native-abort and session-disposal acknowledgement failures; both cleanup causes survive and no repeated insert/commit occurs. The native receipt/body timestamp comparison reads actual BSON, rather than reserializing a returned DTO. The eleven new unit cases are included in the 86-case unit run.

The retained-fixture reconciliation inspected seven owned rows, fifteen validated receipts and one majority-visible uncertain-commit receipt before removing only that owned fixture. No primary operation was replayed for reconciliation. Reports use fixed reusable paths and can be overwritten; these digests identify this checkpoint, not a release certification. Embedded Core child precision, arbitrary metadata date precision, Mongo committed-replica receiver support and atomic CRUD outbox enrollment remain open.

## MongoDB receiver and embedded graph checkpoint — 19 October 2026

See [Mongo receiver implementation, tests and open gates](MONGO_COMMITTED_REPLICAS.md). Legacy Hyperdrive remains documentation-only. Commands use existing runtimes and the exclusive reusable Mongo replica-set environment; no installation or duplicate environment was created.

| Report | Passed | Failed | Not executed | SHA256 |
| --- | ---: | ---: | ---: | --- |
| `TestResults/CosmicMongoCurrent/mongo-unit-current.trx` | 108 | 0 | 0 | `838586123a3e11e282f3bce4b4ab9cc1eb8781002b8a56b945e82e09a8bbc6ab` |
| `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx` | 56 | 0 | 0 | `33f639227e6451344512393e2120ed5bd0d0bc59615a4510a7c95ba87fe17767` |
| `TestResults/SubscriptionUsageCurrent/mongo-cosmic-conditional-current.trx` | 4 | 0 | 0 | `7dbc8fccd2becc14b2e3150967b001de736509524197959487733ec77e8a4c1d` |
| `TestResults/SubscriptionUsageCurrent/mongo-cosmic-conditional-reconciliation.trx` | 1 | 0 | 0 | `b19afe575ae53c90ae4b2029531ccb14ba6f0a107ce42d981b41791a6db72ece` |

The final full 56-case run includes the captured single-save routing repair and the getter-changing regression probe, along with the two new sync/async committed-replica sequences. Native replica commit-acknowledgement faults preserve session/transaction evidence; independent causal majority reads prove the row, head and original receipt before replay, and command monitoring verifies no repeated mutations/commit. Server validators separately reject head and receipt writes to prove all-or-nothing rollback.

The final owned-fixture reconciliation report concerns the retained captured-topology probe: one exact owned source root, zero target rows, no populated auxiliary collections, inspected with majority reads before removal of both exact fixed owned fixtures. This differs from the earlier seven-row/fifteen-receipt reconciliation checkpoint, whose report path was reused. The probe initially exposed four caller child-getter reads; the repaired route has one, with the original child persisted and acknowledged.

Producer-before-consumer builds and tests use `--no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false`. Selected runs: `Run-ReusableUsageRecoveryTests.ps1 -CosmicConditionalWrites` (four cases) and `-MongoTransactions` (56 cases). The initial count-only runner failure after four passing selected tests was corrected from two to four, and full-suite expectation from 54 to 56; no test was skipped or removed.

Existing build warnings remain. Reports are fixed reusable files and may later be overwritten; these digests identify this checkpoint only. Mongo cross-provider receiver directions, arbitrary metadata precision, nondefault node implementations, hosted adapter, primary atomic outbox and broader roadmap acceptance are not certified by these results.

## Final strict-capture and V2 checkpoint — 19 October 2026

This supersedes the preceding interim unit counts and V2 verification caveat. All six reports below pass with no failures or skipped cases. The final full Mongo/SQLite and V2 runs include SerializeInput, explicit delegate rejection, cyclic-value rejection, single-admission routing and the input-versus-storage error classification fix.

| Suite | Passed | Report | SHA256 |
| --- | ---: | --- | --- |
| Core unit | 1001 | `TestResults/CosmicMongoCurrent/core-unit-current.trx` | `8fc2759281800f98109df96a1a9990ac283bba4f54fcedd24aead7d059145151` |
| Shared unit | 518 | `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `7f409fca13f2cdea118eba3002982185828b77e14691402270bccacb1b8dc789` |
| Mongo unit | 112 | `TestResults/CosmicMongoCurrent/mongo-unit-current.trx` | `df1e905d03ebb7dc7bf0a6046aae0363bc635e9b76663dc06d02f77c742174ee` |
| Mongo full native | 56 | `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx` | `5a4d789f9673e50cfe8aee7dea6d9c6ddf48b7455cc93508d20529869e7fafa1` |
| SQLite full native | 616 | `TestResults/CosmicMongoCurrent/sqlite-shared-plan-current.trx` | `85a264c05175294b38d8d296c5d1dd6582ecbb56bb52d4ab296cdd66701dc571` |
| V2 Mongo/SQLite routing | 12 | `TestResults/SubscriptionUsageCurrent/mongo-sqlite-v2-current.trx` | `14e0deca4b61831a559a8155c1e1a81fadddb7811ba1c7fe79c5777211420d29` |

Core builds for net10.0 and netstandard2.1 both pass with existing warnings and zero errors. The shared count includes concurrent contributors' cases. Input delegates now have an explicit JsonSerializationException with a preserved NotSupportedException; input cycles fail instead of being omitted. Valid acyclic capture is byte-identical to the prior serializer, preserving existing valid intent fingerprints.

The broad V2 regression initially failed only its old BSON-specific delegate exception assertion. The corrected test specifies the earlier exact JSON rejection for holons, retains exact BSON/NotSupportedException checks for avatar/detail writes, and keeps all no-dispatch, no-failover, retained-record and replication checks. All twelve final V2 cases actually executed and passed. No tests were skipped, removed or replaced with an unconditional success expectation.

Fixed reusable report paths may be overwritten by subsequent checkpoints. Successful owned Mongo/SQLite fixture cleanup is part of the native tests/runners; the captured-topology failed fixture was independently inspected before its exact cleanup as recorded above. No provider runtime was installed or duplicated. No release/deployment or whole-roadmap completion is claimed.

## Metadata and cross-provider checkpoint — 19 October 2026

The current checkpoint passes 130 Mongo unit, 523 shared unit, 616 SQLite native, six Neo4j/SQLite replica sequences, four SQLite/MongoDB replica sequences, 56 Mongo native and 16 V2 Mongo/SQLite routing cases. The final V2 run exited successfully with zero failures/skips (1 minute 58 seconds). These supersede the earlier 112/518/12 counts for the affected suites; earlier report hashes describe their historical runs. See MONGO_METADATA_CODEC.md for fixes, retained-fixture reconciliation and remaining gates. Atomic primary CRUD/outbox enrollment and the generic hosted adapter remain open. No commit, push, release or deployment was performed.

## Hosted graph payload/adapter checkpoint — 19 October 2026

Full shared suite: **548 passed, zero failed/skipped**; includes eleven payload and fourteen adapter cases. Native SQLite/MongoDB transfer suite: **four passed, zero failed/skipped**, now exercising the hosted adapter for actual creation, updates and soft/hard deletion before direct receipt replay. Existing producer builds pass with warnings. No provider/runtime installation was required.

| Report | SHA256 |
| --- | --- |
| `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `9348F4D9247BF8AC5E6285781A0B4742A84F33425F54307AFCD3D8011AE045B2` |
| `TestResults/SubscriptionUsageCurrent/cosmic-cross-provider-replicas-current.trx` | `83D72B2E3CF914AE9969D043C4726ABCCE20925ACFE5DF21999D060EF2C7F491` |

See [contract, evidence and remaining gates](HOSTED_COMMITTED_GRAPH_DELIVERY.md). This is an explicit hosted delivery boundary, not atomic ordinary CRUD enrollment. No commit, push, release or deployment was performed. The next implementation priority remains primary mutation plus immutable per-target obligations in the same native transaction.

## SQLite atomic primary/outbox checkpoint — 19 October 2026

Final complete suites pass **1,012 Core unit, 549 shared unit and 633 SQLite native** cases, zero failures/skips. Seventeen new SQLite cases cover manager publication/rollback/no-op, frozen graph enrollment, insert/receipt failures, exact and competing-client replay, enrollment fingerprint changes, ordered updates/delete tombstones, cancellation and migration protection. Eleven new Core cases cover enrollment identity validation, option propagation and sync/async capability dispatch without plain-write downgrade. One shared case rejects dropping required enrollment in a plain conditional plan. Core net10.0/netstandard2.1 and shared/SQLite producer builds pass with existing warnings. Original constructor signatures are retained for binary consumers.

| Report | SHA256 |
| --- | --- |
| `TestResults/CosmicMongoCurrent/core-unit-current.trx` | `698FA96663B8AF891D0CE90286A0AD31926548C9B27B25B9BD553D95CDD41B94` |
| `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `C5EC9D0374B56C43F7350F48F5E3A3E757BF4DD69662A70293BA5F12E53FFF0B` |
| `TestResults/CosmicMongoCurrent/sqlite-atomic-outbox-full-current.trx` | `E263A087EF0D1378864871868FD6DA6459AF0CE22E82AC6B43020E9485E6662F` |

See [atomic contract, reproduction scope and remaining gates](ATOMIC_HOLON_OUTBOX.md). SQLite uses its existing reusable fixture and native transaction owner. Successful disposable fixture cleanup is part of the suite; no provider runtime or duplicate environment was installed. Free C: space remains approximately 75.36 GB decimal. No commit/push/deployment or whole-roadmap completion is claimed. MongoDB/Neo4j enrollment and lease-fenced delivery remain open.

## SQLite delivery-lease checkpoint — 19 October 2026

Final suites pass **1,012 Core unit, 549 shared unit and 646 SQLite native cases**, zero failures/skips. Thirteen new native cases cover claim exclusivity, expired/reused-worker fencing, renewal, target-isolated ordered retries, reconciliation blocks, corruption before mutation, rollback of a failed claim, missing-database admission, v1 migration/downgrade protection and writer reservation before expiry clock reads. Core builds pass for net10.0/netstandard2.1; shared and SQLite producers build with existing warnings.

| Report | SHA256 |
| --- | --- |
| `TestResults/CosmicMongoCurrent/core-unit-current.trx` | `122CC1A7EEDDAED3A956C2287CB6527F98A99E7B8C02657832C464E29CC68BC6` |
| `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `6F438E6CF62F03DB6596E7D06221E64D6C776527C58DE29A38BC46235DC74566` |
| `TestResults/CosmicMongoCurrent/sqlite-delivery-leases-full-current.trx` | `646804F3A1526BFAEEB9D3262A4B04CDF87AE7330FD0C362652D8C781C611E1E` |

The first targeted test attempt had a fault-injection formatting failure before provider dispatch (EF raw SQL interpreted literal JSON braces). The injection is now parameterized; all final cases execute and pass, with no skipped/deleted test or relaxed corruption assertion. See [protocol and remaining gates](HOLON_OUTBOX_DELIVERY_LEASES.md). Successful owned SQLite fixture is absent after suite shutdown; C: free space approximately75.36GB decimal. Same environment/runtime reused, no installation/download. Direct coordination exchanged with the active Hyperdrive & Providers session; its Oracle/OCI changes and pinned enum/dependencies are preserved. Automatic worker dispatch, operator reconciliation, other-provider stores and whole-roadmap acceptance remain open. No COSMIC commit/push/deployment was performed.

## Fenced outbox dispatcher checkpoint — 19 October 2026

Final shared suite passes **564 cases**, including fifteen new dispatcher cases. Four standalone native SQLite/MongoDB combinations pass; the final broader **V2 suite passes all16cases**, zero failures/skips,2m34s. The final V2 producer build includes preserved warning/error chains and covers all four combinations: SQLite-source cases now use atomic enrollment -> fenced claim/renewal -> actual MongoDB committed receiver -> durable Completed state -> exact native receipt replay. MongoDB-source cases retain direct hosted receiver coverage without claiming a MongoDB outbox. Both source directions still validate actual stored values through independent readers. Existing SQLite646/Core1012 results remain the preceding contract/store checkpoint; they are not represented as newly rerun here.

| Report | SHA256 |
| --- | --- |
| `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `F3C9C4C9D25AB2532E2BB5709C060C2A4C5E344BF361C293C931B789891ECE79` |
| `TestResults/SubscriptionUsageCurrent/cosmic-cross-provider-replicas-current.trx` | `CB14A73631BD4F4F2C0B8B72D7C854D0AD378AB2CA5654CD6271A9AE3C72802C` |
| `TestResults/SubscriptionUsageCurrent/mongo-sqlite-v2-current.trx` | `58283F20AA9B525C9114E65DDB57E98FB1D5E2A9B1A2CD95B176B01CEAFCF584` |

An early targeted invocation used an older producer after a local variable-shadowing build failure; it is discarded as evidence. The error was fixed, the producer built successfully, and the full final564-case suite and final16-case native V2 suite above ran against rebuilt producers. No assertion was relaxed, no test skipped and no target result substituted. See [worker protocol and open gates](HOLON_OUTBOX_DISPATCHER.md). Native runner terminal0; its owned log was bounded from10,432,006bytes to65,505bytes, preserving the reusable database/runtime. Successful cross-provider fixture cleanup is part of the passing native cases. No new runtime/download/environment or COSMIC commit/push/deployment.

## Durable recovery evidence checkpoint — 19 October 2026

Full suites pass **1,017 Core unit, 566 shared unit and 657 SQLite native cases**, zero failures/skips. New coverage: five Core DTO cases, two marker-only worker cases and eleven native evidence/state/migration cases. Initial Core (net10.0/netstandard2.1), shared and SQLite producers build successfully.

| Report | SHA256 |
| --- | --- |
| `TestResults/CosmicMongoCurrent/core-unit-current.trx` | `67E1055DFDF18DD6227D2E7D9AAE8A9BD85E7FE2D5D6AAE0D46D98D1B5133200` |
| `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `20FAF8E4F79AC02E104AE04F717F45AA044B3386621382E568BA450FC7D6B1F5` |
| `TestResults/CosmicMongoCurrent/sqlite-recovery-evidence-full-current.trx` | `C9A269CD0F39B838EC9E5379D0639F932F7B58280974913D86271DA288130248` |

The first migration fixtures incorrectly began with EnsureCreated while testing migration-history transitions; both now initialize through Database.Migrate before exercising downgrade/upgrade. Final full657-case execution includes all eleven evidence cases, without skips or relaxed assertions. A later -CosmicReplicaTransfers attempt stopped at producer compilation before native dispatch: concurrent EncryptedKeyValueBackend CAS edit temporarily passed IKeyValueBackend where IConditionalKeyValueBackend was required. That edit belongs to the coordinated provider session and was left untouched; final native rerun remains pending its build window. Earlier four/16native transfer evidence does not certify the new stricter recovery capability until rerun. See [recovery contract and remaining gates](HOLON_OUTBOX_RECOVERY_EVIDENCE.md). No COSMIC commit/push/deployment or whole-roadmap completion.

### Final recovery-capability native rerun

The coordinated provider session corrected its own in-progress CAS wrapper and confirmed source stability. Fresh Core/shared/Mongo/SQLite producers and the dedicated integration consumer build successfully. **All four native SQLite/MongoDB transfer combinations pass**, zero failures/skips,5seconds, against the stricter recovery-capable worker/store. This resolves the preceding producer-window caveat; no contributor file was reverted or patched by COSMIC. The existing primary receiver/caller content assertions and durable outbox completion checks still execute. No Oracle/OCI test consumer was rebuilt by this rerun.

Report `TestResults/SubscriptionUsageCurrent/cosmic-cross-provider-replicas-current.trx`, SHA256 `4363BBB29833C352CB72D7B877DCE141B11CA2E7C388281AD8547CD73E82B690`. Final SQLite fixture cleanup is part of the passing cases; the source disposable DB is absent after completion. Same environment reused; no installs/downloads/COSMIC commit/push/deployment.

## MongoDB primary enrollment checkpoint — 19 October 2026

Final Mongo unit134/shared594/full Mongo native58cases pass, zero failures/skips. Five new shared capable-plan cases bind source/target/plain semantics and reject inconsistent admission; four new Mongo unit cases reject bad enrollment/cancellation before repository access. Two new sync/async native sequences validate graph+counter+all targets+receipt atomicity, exact BSON/metadata/child payload, replay binding, later-target/counter/receipt rollback and exact retry, soft/hard tombstones, manager publication/failure/no-op and pending edits. Final per-obligation sequence/index/counter-regression checks are included in the last58-case run (36seconds), superseding earlier operation-level allocation. New uniqueness/order indexes use the existing activation owner.

| Report | SHA256 |
| --- | --- |
| `TestResults/CosmicMongoCurrent/mongo-unit-current.trx` | `0B88DE47F2132BB5A2227C8F339789773F93C2FD6046D20543C53AD91BAFFEFD` |
| `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `4409FB5B43A157B81D7442BCB116627796A3298AE786F3FF402B0729DBE404B3` |
| `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx` | `78E7855AE96B33AC350D677CFA318D04AF922186215144B9DE61A3AE318B0608` |

Native runtime reused; final owned Mongo log bounded from9,169,260bytes to65,388bytes. No duplicate install/environment or COSMIC commit/push/deployment. Shared count includes concurrent provider-session CAS coverage. A short fresh cross-provider transfer rerun is in progress to verify the activation/index change with the existing SQLite/Mongo receiver matrix. [Mongo enrollment contract and remaining gates](MONGO_PRIMARY_OUTBOX.md): Mongo fenced delivery/inspection and automatic host scheduling are still open.

### Final MongoDB enrollment compatibility check

Fresh producers and the native integration consumer build successfully after the final unique sequence/index change. All four SQLite/MongoDB receiver combinations pass, zero failures/skips,4seconds. Report `TestResults/SubscriptionUsageCurrent/cosmic-cross-provider-replicas-current.trx`, SHA256 `497721FF82699EAC7FF5C1E4A28255DEB47DEE04D90D0342C487D35AB54B061A`. This validates the new activation/index behavior with the existing receiver/SQLite-worker matrix; it does not claim a MongoDB delivery consumer. All owned native runs terminal0; successful fixture cleanup executed.

## MongoDB delivery store checkpoint — 19 October 2026

Final suites pass Mongo unit140, shared630, SQLite native657, complete Mongo native60 and four both-direction outbox-worker cases, zero failures/skips. Mongo full60runs44seconds, SQLite657runs1m13s, final matrix4runs4seconds. Six new no-session admission cases, ten shared persisted-codec cases and one store-warning worker case are included; two new native producer sync/async sequences exercise the store protocol in depth. Shared total includes concurrent provider-session CAS coverage.

Native proof includes server-clock expiry, reused-worker tokens, retry/head ordering, competing sessions, checksum/corrupt-state refusal, literal strings, evidence atomicity, real post-commit acknowledgement uncertainty, and independent causal majority reconciliation using original operationTime without another update. The final fault also injects disposal failure and retains BOTH original native marker and cleanup cause. Read-only abort+dispose acknowledgement failures preserve the known result and both causes without a second abort. The final matrix now uses enrolled outboxes/workers in BOTH source directions rather than direct Mongo transport.

| Report | SHA256 |
| --- | --- |
| `TestResults/CosmicMongoCurrent/mongo-unit-current.trx` | `A7E7A91B319363D1352F9391E5A2595B63326BE2459FA69CF486D653C667AD5B` |
| `TestResults/CosmicMongoCurrent/shared-unit-current.trx` | `52F27A66BBEE303FE2819DFC7B8D82AAB085C90E0E2EEF1A541C0BB15051A917` |
| `TestResults/CosmicMongoCurrent/sqlite-common-recovery-codec-current.trx` | `24B576CF64C1A3219888F2DF38E65F2D7D800011914BDEB9AA5B5B16DE057BD8` |
| `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx` | `C34F1839AD8A6BAED44BA97EB3DBB8344E1773E7BE8CB919E3BC5B129C9025A4` |
| `TestResults/SubscriptionUsageCurrent/cosmic-cross-provider-replicas-current.trx` | `627464974E6251203A413ADEBF9B62EF2662B68765DC0F876F13AB7AD52ED673` |

Original codec format1 bytes are locked by a golden test and reused by SQLite/Mongo through one implementation. Fresh producers and consumers build with existing warnings, no errors. Successful owned fixtures were removed after native verification; retained Mongo log was bounded from16,003,034bytes to65,360bytes, preserving database/runtime reuse. No install/download, COSMIC commit/push/deployment or whole-programme completion. Core1021 is the separate provider-session checkpoint; this batch made no Core-source edit and does not relabel that suite as its own new run. See [protocol and open gates](MONGO_OUTBOX_DELIVERY.md).


## Neo4j atomic primary enrollment — 19 October 2026
The existing graph transaction now owns frozen per-target outbox obligations, a serialized sequence range and the exact receipt. Enrollment is explicit and fingerprint-bound; unchanged plain-write fingerprints are preserved. Native activation verifies seven uniqueness guarantees by shape. No COSMIC commit, push or deployment was performed.
Verification: provider build and 117 unit cases passed. The full native suite passed 170 cases. After adding payload-digest and no-sequence-consumption replay assertions and clarifying schema error text, the fresh focused native rerun passed both sync/async cases. All reports have zero failures/skips. The earlier test-consumer build failed on an ambiguous INode import before native submission; an explicit Neo4j alias corrected the test source, then both successful runs used rebuilt producers/consumers.
- `TestResults/Neo4jCurrent/neo4j-unit-current.trx`: 117/117; SHA256 `FD974C469AAF3BF4F23FB11406F0FA7F4192C96E484F0BD534E6E351A9601E5C`.
- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: 170/170; SHA256 `C8F4A856CBBBF5CC778F8B1FDC2E2C51FE6998E467999A4F791E7DE32ABDC0E8`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-primary-outbox-current.trx`: 2/2; SHA256 `82889A0A226E2314424DDADC01E6CA2F3D1B36D15CB2625722D17C272ED6387F`.
- Final focused binary `Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: SHA256 `B0644A4515BFD407A52F0CC408D974498F4EC908E99EBAA7A8047A3CB80A928E`.
- Final focused binary `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.dll`: SHA256 `E63A27A869D104B4EF318015A9DEF946921806433595111AC701FD0720761CCE`.

Reproduce with the retained environment: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicPrimaryOutbox`; full suite omits that switch. Runner holds its exclusive environment lease, reuses the pinned existing container, performs bounded builds and stops its owned service in finally. Both invocations terminated successfully. Final free C space was 73,971,318,784 bytes; no download or new environment was created. Source graph/receipt/obligation fixtures were cleaned only after successful assertions; retained sequence state remains monotonic.

Open: Neo4j fenced delivery/inspection and all larger hosting, security, crash recovery, provider certification, migration, placement, deltas and CQL gates. See [Neo4j enrollment](NEO4J_PRIMARY_OUTBOX.md).


## Neo4j fenced outbox delivery — 19 October 2026
Implemented caller-driver-owned `NeoHolonReplicationOutbox` with server-time-after-lock lease decisions, ordered target heads, fresh tokens, non-shortening renewal, fenced outcomes and typed recovery inspection. No schema creation, repair, hidden retries, Core/shared-source change, COSMIC commit, push or deployment was performed.
Fresh verification passed 128 unit, four focused native and the complete 174 native cases. Unit coverage includes six pre-I/O admission and five known-read/failure/rollback/disposal owner cases. Native delivery covers expiry/reclaim, reused worker fencing, independent targets, retry head blocking, immutable payloads, corruption without consuming attempts, concurrent claimants and >4KB opaque recovery identity retention. A real claim commit followed by lost acknowledgement and both disposal failures retains the reconciliation marker and every cause, performs one commit and zero rollbacks, and is reclaimed only after native lease expiry. Actual Neo4j-to-SQLite create/update/soft-delete/hard-delete passes through the dispatcher and fenced source completion; target hard-delete receipt replay remains exact.
- `TestResults/Neo4jCurrent/neo4j-unit-current.trx`: 128/128; SHA256 `3D3B2FE03506A8D412F6ECC1465C21C9F4D75FF1EFC9DA04B773B089E778CCC2`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-outbox-delivery-current.trx`: 4/4; SHA256 `83490FB834933BB12B89E7F436C22B2712ADCF7F6D98BBDEEF8EB74800A5A743`.
- `TestResults/Neo4jCurrent/neo4j-native-current.trx`: 174/174; SHA256 `C109BD209A2E1138663D2EC6437CDFF6BCCD9319833F2F8A8E962C3D31AF07D9`.
- Final full-suite binary `Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: SHA256 `978C274E540268C173185FA342066EB8DA03253EA16472FA4967B14BC706B2FC`.
- Final full-suite binary `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.dll`: SHA256 `518B81FBF6DFEE9508682A18AD1687E3F70A212B5D934BE212213A4CDCEB3BC7`.

Reproduction: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicOutboxDelivery` (four cases); omit the switch for the full 174. Both runners terminated zero, released the exclusive lease and stopped the owned retained container. Exact owned `TestResults/Neo4jCurrent/outbox-delivery-target.db` and its WAL/SHM siblings were removed only after passing assertions and provider close; absence was verified. Queue/source fixtures were cleaned after successful tests; the retained sequence owner remains monotonic. No installation/download or duplicate environment was created. Full-run free C bytes: before 73,959,104,512; after 73,960,652,800.

Remaining: SQLite-to-Neo4j and MongoDB/Neo4j worker direction certification, automatic hosting/authorized catalogs and context, operator reconciliation and pre-submission/process-loss journaling, deployment/retention limits, the broader provider/child/history/migration/placement programme, committed deltas and CQL. These tests do not close those gates. See [Neo4j delivery](NEO4J_OUTBOX_DELIVERY.md).


## Selected six-direction durable worker matrix — 19 October 2026
Added actual SQLite-to-Neo4j source outbox delivery and both MongoDB/Neo4j source directions. The tests use atomic enrollment, native fenced source stores, the dispatcher, registered targets, durable Completed inspection and exact target receipt replay. The new MongoDB/Neo4j cases check fresh-reader logical lineage, source/target physical revision separation, graph children, audit, sub-millisecond UTC and binary metadata. Native hard-delete counts verify physical absence independently of public load errors.
Fresh selected results: six SQLite/Neo4j/store cases pass; four MongoDB/Neo4j cases pass. The previous 174-case full Neo regression and 128-unit checkpoint remain historical to this test-reference expansion; the revised full Neo mode expects 176 cases and was not rerun in this batch. Existing four-case SQLite/MongoDB native worker results complete the six directed-pair matrix; those earlier reports are not represented as freshly rerun here.
Failure/reconciliation: the initial new case committed and completed creation/update/soft-delete, then expected Mongo public load to expose its tombstone. It failed; the three later cases refused to overwrite the retained fixture. Source inspection confirms the public read visibility difference. The failed report is preserved, and a reviewed one-time native test validated exact fixture IDs, version3 records and three Completed obligations, archived both stores (106,205 bytes), then removed only that owned fixture. Its reconciliation case passed. The normal four cases subsequently passed with native tombstone verification; [public read-policy harmonization remains open](SOFT_DELETE_READ_CONTRACT.md). This is a corrected test assumption and a documented production API gap, not a production fallback.
- `TestResults/Neo4jCurrent/neo4j-cosmic-outbox-delivery-current.trx`: 6/6; SHA256 `1A4A86464E1C2314D20CC2A91B30CFADFB314DAFD5E08F08E5BAC8055F904B20`.
- `TestResults/Neo4jCurrent/neo4j-mongo-outbox-delivery-current.trx`: 4/4; SHA256 `AC9A6FF5F4F0E602E3768954C4A621FABEEF3759B7CE538A3982509D7C28EB12`.
- `TestResults/Neo4jCurrent/neo4j-mongo-reviewed-soft-reconciliation.trx`: 1/1; SHA256 `DFFF59F3021D0F7AE8ADBCC7A5D21EB1491129F2E7BF20C6C34E00BCADAC0609`.
- `TestResults/Neo4jCurrent/neo4j-mongo-outbox-before-soft-read-fix.trx`: SHA256 `066AC3E10F3DB3AA4265CE1B3CE96D8849BF19B3569964D494C1A8DE0D268D24`.
- `TestResults/Neo4jCurrent/mongo-neo-reviewed-soft-fixture.json`: SHA256 `7241D32738F1E1FE0C46C8C0C7ED5B89E5C4DA8091CFEEAA61AF023BFA46DF6C`.
- `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.dll`: SHA256 `FE3A7F255767940B73E96173D409B0B5B27D4BB5943DBF6093612B45071A7880`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: SHA256 `6DFE2A51B2396D86D19FE6B7A9931C7ED55DDA71B4B4EB4426A86BE82557C84F`.
- `Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: SHA256 `7B49CC48440A41D24A7E7C6FF580E9DE2BF385D31222F3073D6335E0E6C998EB`.

Reproduction and exact environment ownership: [provider direction matrix](OUTBOX_PROVIDER_DIRECTIONS.md). The combined runner holds the existing Mongo environment lease and invokes the existing Neo runner, which holds its own lease. The Mongo project reference was restored from only `C:/Users/david/.nuget/packages` with no network package source. No runtimes, packages or data directories were downloaded/duplicated. Source/provider/Core/shared implementations were unchanged in this test batch; other runner modes and concurrent changes were preserved. Both final runner handles terminated zero, stopped their owned services and released leases. Ports27029/17687/17688 were verified unoccupied; SQLite source fixture absence was verified. Final free C bytes:73,989,783,552. No COSMIC commit, push or deployment.

Still open: explicit uniform deleted-row visibility, automatic host/catalog/context authorization, pre-submission/process/wire-loss recovery and operator reconciliation, queue/retention controls, provider-wide child/history correctness, migration, placement, committed save deltas and CQL. Selected pairwise CRUD delivery does not close these gates.


## Retained tombstone identity-read correction — 19 October 2026
Root invariant: current ID/native-key lookup resolves the retained current record, including lifecycle tombstones. Mongo repository getters previously hid it; the provider-linked snapshot also removed it based on the child-source hint. Removed the active-only predicates at those two existing owners, preserving a single read path and duplicate-identity validation. Discovery predicates and public signatures remain unchanged. Repository identity resolution used by single-save preparation now observes retained creation-audit state as well; no extra read or fallback is added.
Fresh verification passes140 Mongo unit,64 full native and four MongoDB/Neo4j worker cases, zero failures/skips. The new four native identity cases cover GUID/key × sync/async with24 traversal combinations each (two source hints, children/recursion on/off, depths0/1/2), exact current-version hits, old-version misses and physical hard-delete absence. Fully selected root/child/grandchild tombstone versions and audit are retained. The cross-provider worker regression now asserts the public Mongo tombstone as well as its native record. The earlier failed-test archive/reconciliation remains historical evidence; it is not deleted or relabeled as a new failure.
- `TestResults/CosmicMongoCurrent/mongo-unit-current.trx`: 140/140; SHA256 `242D9378F4BB725933216F50FA217FF4E6742BC73028D0CF02349C657F6FFF99`.
- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: 64/64; SHA256 `4546BAF67AD7646043E98A73C4FAE350F4C08D9EE3EADF895B4BC142067717D0`.
- `TestResults/Neo4jCurrent/neo4j-mongo-outbox-delivery-current.trx`: 4/4; SHA256 `2C08D2455D72C328DF6CA2A1425C0434F2DCD968935D15364A91F4CB75FD03EE`.
- Final producer/consumer `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: SHA256 `2F0D43277EC3137B57DA2120B071AF5F01017271A37C5BF75753B38E09023D46`.
- Final producer/consumer `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests.dll`: SHA256 `AE2CADDCCA9D4B4017DF36D2169087F3C703C8315CBE973AF2065BE9A518E107`.
- Final producer/consumer `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.dll`: SHA256 `8D8BCCE146854422B6F6DF463E54D7E5FB109DC73168EA6E2F83075EE8D71F3F`.

Commands: bounded Mongo provider build and MongoDBOASIS.UnitTests; `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -MongoTransactions`; `... -CosmicNeo4jReplicas`. Producers and consumers built before native submission. Both final runner handles terminated zero, stopped their owned retained services and released leases. No duplicate environments, runtime/package downloads or Core/shared-base changes; no Git/index mutation, COSMIC commit, push or deployment. The other provider session was notified of resource use/release and that no Git lock was owned by this task. Final observed free C bytes73,948,008,448.

Compatibility: Mongo exact identity reads now expose retained lifecycle state as SQLite/Neo4j already did; callers must inspect deletion state rather than equate soft deletion with hard absence. Remaining explicit discovery/parent/metadata/CQL visibility, mixed-state or historical graph semantics, automatic hosting/catalog/security/recovery/migration/placement and save deltas remain open. See [read contract](SOFT_DELETE_READ_CONTRACT.md).


## Explicit Generic Host/catalog and cancellation ownership — 19 October 2026
Implemented HolonReplicationCatalog, versioned content-bound target IDs, mandatory route authorization, finite actor grants, and AddCosmicHolonReplication/HolonReplicationHostedService. Independent bounded route loops invoke the existing dispatcher/native stores; no second queue, post-commit enqueue, provider/Core/base/CAS change or legacy Hyperdrive change by this batch. Endpoint/store descriptors remain trusted application configuration. Tenant/record/encryption authentication, durable manifests and ONODE configuration/readiness remain open.
Root cancellation fixes: throwing target callbacks cannot skip task observation or erase returned native warning/marker evidence. A known completed target is not canceled again; only the private renewal timer is canceled. Unexpected route faults cancel/join siblings. StopAsync reports an expired deadline as failure rather than successful shutdown while owned targets still run.
Fresh evidence:653 shared unit cases pass (21 host/catalog plus two additional dispatcher cancellation-owner cases). Two native Generic Host SQLite-to-Neo4j cases pass for sync/async enrolled source writes, create/update/soft/hard deletion and target receipt replay, without manual dispatch. Expiring the actor grant leaves a source ReconciliationRequired head at attempt1 with zero target records/receipts. This is authorization before dispatch, not revocation of an already accepted native write.
Evidence correction: an audit caught stale copy-local Shared DLLs after successful incremental builds. Earlier native successes against BCA6A907 (and older Mongo019D20EE) are not proof of the final DBB6AADD cancellation fix. The reusable runner now non-incrementally builds Shared, SQLite, Mongo, Neo and consumer in dependency order for every mode, then verifies every Shared copy by SHA256 before service startup. A corrected fresh native run passed both cases. No manual DLL copy, alternate runtime load path or environment replacement was used.
- `TestResults/CosmicMongoCurrent/shared-unit-current.trx`: 653/653; SHA256 `91EAA356D9FA0787AE617BAB7565C4BD968E49FBCD2E01D0D418A1FB27F2CA3D`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-outbox-hosting-current.trx`: 2/2; SHA256 `F7879C3107F7107F1B93E9D4938A617EBE897AA3C12C850D5C972B051EA110FF`.
- Verified native startup Shared hash: `DBB6AADD732011BF0BF06B7DDC51A48CC759511929941B784938DE470BC822B2`; manifest `TestResults\Neo4jCurrent\hosting-native-binary-manifest-current.json` SHA256 `B4A1DED90220E00BA3BE398213CF7CF403A45261508599A29E1A9B3DB6DB0ADB`.
- Pinned native consumer `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Core.dll`: SHA256 `70B13689452BC690FE7A621528C1A2D638E621E079755CCD77D54DA229CD841C`.
- Pinned native consumer `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll`: SHA256 `DBB6AADD732011BF0BF06B7DDC51A48CC759511929941B784938DE470BC822B2`.
- Pinned native consumer `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll`: SHA256 `4AEB73076775645FFD1B0C6773B8C2208D53D856BA363B9170B8F9EBE749775C`.
- Pinned native consumer `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.dll`: SHA256 `857B71E4339C1D436D10558085A0E06E98246485F2001C9F7EFD2B5614C0F412`.
- Pinned native consumer `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.dll`: SHA256 `8AE6A29B4BF5D12BCBBE214DA1B008C807FC7B149BD8DCE44AC4508A1BD19FA4`.
- Pinned native consumer `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.dll`: SHA256 `32C5B65207BAF8DA5A0FCE9D4D32EF184D4F38E6BCE8BD93F40C126999A24519`.

All pinned native consumer binaries were checked unchanged after terminal success. Mutable shared producer output advanced after the coordinated build window was released; that is not substituted for the tested consumer. The other provider session separately reports a one-line MetadataPropertyHandling.ReadAhead Core correction and Core1025 tests passing while preserving the existing dirty COSMIC codec diff. That is its checkpoint, not a new Core rerun by this task. It also reports an unrelated existing Slip10Ed25519 netstandard2.1 API build failure; hosting here targets net10.

Commands: bounded Shared producer build, complete Shared unit suite and `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicOutboxHosting`. A missing namespace and incorrect native HostBuilder lambda were fixed before successful builds. Initial20/650 and stale-copy native checkpoints are superseded by the final653/2 results. Default full Neo mode now expects178 but was not rerun for this focused host batch.

Final runners terminated zero, stopped the retained owned Neo service and released the lease. Exact outbox-host-source.db/WAL/SHM fixtures are absent after successful joined host stop/provider close; ports17687/17688 are unoccupied. No download, installation, duplicate environment, Git/index mutation or COSMIC publication. Final observed free C bytes73,449,402,368.

Open: ONODE validated configuration/provider factory/readiness and default enrollment, authenticated tenant/record/encryption context and durable catalog/grant migration, pre-submission/process-loss recovery and operator reconciliation, retention/queue/metrics, broad provider/graph/history conformance, migration, placement, committed deltas and CQL. See [host/catalog](COSMIC_HOLON_REPLICATION_HOST.md).


## Native queue-head inspection and truthful host readiness — 19 October 2026
Added IHolonReplicationReadinessOutbox.PeekNextAsync to the existing recovery contract. SQLite/MongoDB/Neo4j implementations select the oldest unfinished target obligation without claim, attempt increment, lease mutation or repair. They validate native identity/state/evidence and immutable payload binding/checksum. Inspection results exclude business payload and claim token. SQLite claim/peek share one payload validator; Mongo status inspection explicitly uses primary/majority, independently of general secondary-read preferences; Neo reuses its known read-only transaction/cleanup owner.
Configured hosting now requires native readiness inspection and registers HolonReplicationHostState/IHealthCheck with the ready tag. A persisted reconciliation head is degraded after service reconstruction, not interpreted as healthy no-work. Unavailable/corrupt heads, retry outcomes and failed routes remain unhealthy; recovered native availability can become healthy. Starting/stopped/faulted states are not ready, faults remain sticky, and health/data use one locked snapshot. Responses exclude raw exception text, business payload and lease token. This is operational queue/worker readiness, not tenant authentication, settlement finality, a liveness SLA or completed ONODE startup wiring.
Final local evidence:656 Shared (24 host/catalog),658 SQLite native,141 Mongo unit,64 Mongo native,129 Neo unit,6 Neo focused delivery and2 native host/restart cases pass, zero failures/skips. Native tests verify Pending/InFlight/reconciliation selection, reconstruction, read-only attempt/lease state, corrupt-head refusal and no successor bypass. The single-member Mongo test overrides a configured secondary read preference to validate authoritative primary inspection. The actual Generic Host restarts on the same expired-grant blocked SQLite source and remains degraded without another claim/attempt or target write.
- `TestResults/CosmicMongoCurrent/shared-unit-current.trx`: 656/656; SHA256 `C8DD54B1D8D5CEBE4D00578E8B37DC3A1C6A39BB97D5039F0FA254DE05A941E1`.
- `TestResults/CosmicMongoCurrent/sqlite-readiness-current.trx`: 658/658; SHA256 `2AAB4FA2CFE7E65763B734F057AD7EB566DF07C4D0AAE7E6189D2ED323C860A1`.
- `TestResults/CosmicMongoCurrent/mongo-unit-current.trx`: 141/141; SHA256 `D4A0FD10BAA0BE03C5E4C9096F1BB9F29731F87254EBEEA071B3E87C8CF378A5`.
- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: 64/64; SHA256 `2C710647C1D3C6B91B4F772781BD82656924D354624C9B9C5AC083CF2B00FF8F`.
- `TestResults/Neo4jCurrent/neo4j-unit-current.trx`: 129/129; SHA256 `3E3E4461651B5A21987AD874A2CDFB9B56806F4B39897109F1E68854D97E679E`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-outbox-delivery-current.trx`: 6/6; SHA256 `8AF11DFC45690C2DD4DC7D819EA8CABE4002960E439DD268B248D9F4629BC76C`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-outbox-hosting-current.trx`: 2/2; SHA256 `C45256E798F9D4107C29E43D63542F1D6A47201C574E27D2C8688D8F44D5630A`.
- Pinned Core/Shared consumer manifest `TestResults\CosmicMongoCurrent\readiness-consumer-manifest-current.json` SHA256 `0BE4299F7FA6C422FA39345BF24D41702FE140BC8AB6A265A4C1A35A68BF583B`.
  - `Providers\Shared\NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Core.dll` SHA256 `FC8036A3CA0734FC288093BEA2D12F4156D2E03E2D80E587C37C4E1270681DB4`.
  - `Providers\Shared\NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests\bin\Debug\net10.0\NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll` SHA256 `0DFB2BDAA35FECD3F86228CBC9D3E9CB7E67B4A5C433DB668175197013FEBA21`.
  - `Providers\Storage\NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Core.dll` SHA256 `FC8036A3CA0734FC288093BEA2D12F4156D2E03E2D80E587C37C4E1270681DB4`.
  - `Providers\Storage\NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll` SHA256 `0DFB2BDAA35FECD3F86228CBC9D3E9CB7E67B4A5C433DB668175197013FEBA21`.
  - `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Core.dll` SHA256 `FC8036A3CA0734FC288093BEA2D12F4156D2E03E2D80E587C37C4E1270681DB4`.
  - `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll` SHA256 `0DFB2BDAA35FECD3F86228CBC9D3E9CB7E67B4A5C433DB668175197013FEBA21`.
  - `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.API.Core.dll` SHA256 `FC8036A3CA0734FC288093BEA2D12F4156D2E03E2D80E587C37C4E1270681DB4`.
  - `Providers\Storage\TestProjects\NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests\bin\Debug\net10.0\NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll` SHA256 `0DFB2BDAA35FECD3F86228CBC9D3E9CB7E67B4A5C433DB668175197013FEBA21`.

Both native Neo startup logs verify Shared0DFB2BDAA35FECD3F86228CBC9D3E9CB7E67B4A5C433DB668175197013FEBA21 and CoreFC8036A3CA0734FC288093BEA2D12F4156D2E03E2D80E587C37C4E1270681DB4 across all direct copies before starting the retained service. The runner now checks Core as well as Shared. Test consumers were built separately with --no-incremental, then tested with --no-build; dotnet test does not accept --no-incremental (initial rejected command submitted no tests).

The first Shared run exposed two test assertions selecting the additional framework health-publisher hosted service; they now select the COSMIC worker explicitly. It also saw two concurrent latest-only credential-selector assertions against the preceding producer. The other session completed those separate guards, released the producer window, and the fresh coordinated656-case suite passed. Its reported Core1066 checkpoint is separate from this task's net10 producer build; source/code ownership was preserved. No credential-selector, crypto, CAS/base or legacy Hyperdrive source was edited by COSMIC in this batch.

Commands: bounded current Core net10/Shared/native-provider builds; full Shared/MongoDBOASIS/Neo unit consumers; SQLite integration consumer; Run-ReusableUsageRecoveryTests.ps1 -MongoTransactions; Run-ReusableNeo4jTests.ps1 -CosmicOutboxHosting and -CosmicOutboxDelivery. All corresponding handles are terminal0. Owned Mongo/Neo services stopped and leases released; fixed SQLiteCurrent/integration-current.db and Neo outbox-host-source.db fixtures are absent. Other session's Dynamo environment/output was untouched. No installations/downloads, duplicate environments, Git/index mutations or COSMIC publication. Final observed free C bytes73,097,146,368.

Remaining: enabled ONODE configuration/provider factory/lifetime and explicit readiness endpoint wiring, default durable enrollment, authenticated tenant/record/encryption context and catalog/grant migration, process-loss journaling and operator recovery, queue/retention/metrics, full provider/graph/history conformance, migration, placement, committed save deltas and CQL. See [readiness](COSMIC_REPLICATION_READINESS.md).


## ONODE readiness HTTP and activated native source capture — 19 October 2026
Production Startup now maps /health/cosmic/ready through CosmicHolonReplicationEndpoints. The exact named check is required; missing health service, an empty aggregate or unrelated checks return503/not-configured, named unhealthy returns503/not-ready, named healthy returns200/ready. Responses are fixed plain text, anonymous operational status and no-store, without health diagnostics, credentials, payload or tokens. Five actual Kestrel/HttpClient cases pass using the linked production endpoint module, and the complete ONODE project builds0errors/253warnings. This is not a deployed application or a full enabled-startup/authentication test.
Added IHolonReplicationOutboxSource and three native partial implementations, capturing each activated provider's existing readiness store without reflection or exposing its connection string/driver. Inactive capture fails before I/O; unusable durable binding is a real OASISResult error. Capture does not activate, create schema, enqueue, claim, consume attempts or transfer lifecycle ownership. The caller must hold providers stable and stop workers before source shutdown.
Final evidence: HTTP5, Mongo unit142/native64, Neo unit130/native6 and SQLite native659 pass, zero failures/skips. Existing selected native sequences now use the activated-provider capture capability; inactive source tests and SQLite existing-database/no-lease assertions pass. Shared656/host-restart2 remain the preceding readiness checkpoint, not fresh reruns of this source-capture batch.
- `TestResults/CosmicMongoCurrent/cosmic-readiness-http-current.trx`: 5/5; SHA256 `92D8D911DC0D7AE03354799FBB50D055306F21DD5549605B4CFFCCDB27A5C568`.
- `TestResults/CosmicMongoCurrent/mongo-unit-current.trx`: 142/142; SHA256 `D96C54DCD2FDE6A3618AFACE3311C9012623F7320515B20D3AFAE1ACB9329BD3`.
- `TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`: 64/64; SHA256 `41265465BA02A5D548B859D21021BECBA1AA293FE68205C38C10AC92C3759918`.
- `TestResults/Neo4jCurrent/neo4j-unit-current.trx`: 130/130; SHA256 `76F3715DDFB89CF609DC357B64FD823B2E1FAF94638B910828FB54D67059F717`.
- `TestResults/Neo4jCurrent/neo4j-cosmic-outbox-delivery-current.trx`: 6/6; SHA256 `A2F1BBB34DADDB7C568EE7E5B7E95EC4681818732AA88BE619416B7175B2F9D2`.
- `TestResults/CosmicMongoCurrent/sqlite-source-capture-current.trx`: 659/659; SHA256 `6B2C8B411EE7BFE54EB5116FE01AAA11EAE38A3CE74952EFA0C7551547F523BA`.
- Consumer `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin/Debug/net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll`: SHA256 `3205A51187E8B0F634E51FF2C433A15F65E2CD1687896C3B95082CFB72AB5354`.
- Consumer `OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.dll`: SHA256 `7659A0B734C7142C59140F5FCACA156F18F7DE9DE852FE302677192E1BE3C4B9`.
- Consumer `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll`: SHA256 `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`.
- Consumer `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll`: SHA256 `55C25E51FC432E8BF52C77C6B2F6CA65B40A8043D39BFEA3FBA854AD9ADB6D9F`.

Neo native startup admitted Core089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6 and Shared55C25E51FC432E8BF52C77C6B2F6CA65B40A8043D39BFEA3FBA854AD9ADB6D9F across every direct copy before runtime start. Core/Shared producer windows were explicitly coordinated and released after consumer pinning; the other session's Cosmos runtime/output and local-only ActivityPub checkpoint were untouched/not represented as live certification. Native Mongo/Neo runners and SQLite suite all terminated0; owned services stopped/leases released. Mongo raw log10,770,761bytes was bounded by its existing owner to65,176-byte tail after shutdown, preserving retained data/runtime. Fixed SQLiteCurrent/integration-current.db is absent. Final free C bytes73,060,524,032. No installations/downloads, duplicate environments, Git/index mutations, COSMIC commit/push or deployment.

Commands: bounded Core net10/Shared/native provider and test-consumer builds; full ONODE build with existing references; filtered CosmicReadinessHttpTests; complete Mongo/Neo unit suites; SQLite integration suite; reusable Mongo -MongoTransactions and Neo -CosmicOutboxDelivery modes.

Remaining gate: complete strict enabled ONODE catalog/configuration factory, initialization/failure/lifetime ownership and default durable enrollment. Source/target/tenant/record/encryption authorization, persisted manifests/migration, process-loss journaling and operator reconciliation, retention/queue/metrics, broad provider/graph/history conformance, migration/placement, save deltas and CQL remain open. See [startup integration](ONODE_COSMIC_REPLICATION_STARTUP.md).


## 2026-10-19 configured ONODE replication startup final verification

- Full Shared unit suite:685 passed,0 failed,0 skipped (`TestResults/CosmicMongoCurrent/shared-unit-current.trx`). Includes28 strict configuration/factory cases and explicit proof shared initialization completes before registry lookup.
- Native configured-host Neo4j to SQLite:2 passed,0 failed,0 skipped, sync/async source writes; initially inactive providers, factory activation/capture, automatic create/update/soft-delete/hard-delete delivery and exact replay (`TestResults/Neo4jCurrent/neo4j-cosmic-configured-host-current.trx`). Retained service stopped and runner exited0; no provider ownership transfer.
- Native startup byte parity: Shared SHA256 `811D2438E54E3B361667985878E7E41676C17C1F085C4E029FD15E713FB078BC`; Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`. Producer and consumer builds completed before native startup. See `TestResults/Neo4jCurrent/configured-host-final-current.log`.
- Full ONODE nonincremental build:0 errors,253 warnings; exit0 (`TestResults/CosmicMongoCurrent/configured-onode-build-current.log`). Free C bytes after native runner:72,817,758,208. No installs/downloads, Git/index writes, deployment or COSMIC publication in this batch.
- This closes explicit configured startup only. Default CRUD enrollment, physical/authenticated admission, process-loss durability/operator recovery and the remaining roadmap remain open.


## 2026-10-19 authorization envelope ownership

Invariant: the exact frozen bytes and routing assertions presented for authorization are the bytes and assertions dispatched to the committed receiver. A caller-owned mutable HostedSyncFanOutItem must not be read again after awaited host policy.

- Red proof: `authorization-envelope-before-current.trx` contains one failing deterministic awaited-policy case against the prior producer. Replacing all fields during the policy await applied the replacement actor despite authorization of the original actor. Consumer build completed successfully before this reproduction.
- Fix: AuthorizedTarget captures all eight envelope fields privately before policy invocation and dispatches that captured envelope. Policy's deserialized graph remains separate from the receiver's reparse.
- Green proof: full Shared692 passed,0 failed,0 skipped (`TestResults/CosmicMongoCurrent/shared-unit-current.trx`, `authorization-envelope-shared-current.log`). Seven new cases cover actual asynchronous payload/actor/operation replacement and six originally invalid routing assertions that policy tries to repair. Both producer/consumer builds exit0.
- Producer and tested consumer Shared SHA256 both `378A87AC5170746D1F1BEDDB0F6079B17356A7178E2A4295CA240F30CF23A75B`. No provider/native storage implementation changes in this batch; prior native startup evidence retains its original hash and scope.
- No Git/index writes, installs, downloads, native services or deployments. External provider session notified of Shared build window and release. This closes one authorization ownership defect; broader authenticated source/tenant/record/encryption admission remains open.


## 2026-10-19 committed graph strict wire admission

Invariant: the existing version1 producer format has explicit field names and token kinds. General Newtonsoft conversion is not a second wire contract; malformed scalar types must fail before record deserialization and provider mutation.

- Red evidence:18/18 cases failed against the prior decoder (`TestResults/CosmicMongoCurrent/wire-scalars-before-current.trx`). Fifteen payload cases cover numeric/string/enum/boolean/depth/GUID coercion; three adapter cases prove coercible payloads actually reached the receiver. Consumer build succeeded before the red run.
- Fix: validate exact envelope/node fields and token kinds before ToObject. Identity fields use GUID strings in D format. ActorId preserves the existing request contract's empty-value semantics; authorization still decides whether that actor is allowed. Capture format and receipt fingerprints are unchanged.
- Final full Shared713 passed,0 failed,0 skipped (`shared-unit-current.trx`, `wire-scalars-shared-current.log` under TestResults/CosmicMongoCurrent).21 added cases:18 coercion/dispatch, two field-casing and one empty-actor/fingerprint compatibility case. Producer/consumer builds exit0; both final Shared DLL SHA256 `6948494F4A11E9182EB7DB0734EC336567FB345DF3EC2A473DC972405DB28786`.
- Native configured-host2 passed,0 failed/skipped (`TestResults/Neo4jCurrent/neo4j-cosmic-configured-host-current.trx`, log `wire-scalars-configured-host-current.log`). Native consumer was byte-pinned to initial strict Shared `D9FF8207D2AB3EDFB49DE9AB171AA3C7A22A700306F518A2F664F415DE27485E`, Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`. Afterwards the decoder was corrected to preserve already-supported empty ActorId, verified by the final Shared suite; native cases use nonempty actors. Do not present the earlier native DLL hash as the final producer hash.
- Native runner exited0 and stopped its retained service; no owned fixtures remain in use. Free C bytes after native runner72,827,437,056. Shared/SQLite output windows and Neo lease explicitly released to the external provider session. No EVM output/Mongo lease interference, installs/downloads, Git/index writes or COSMIC publication.
- This is strict syntax/shape admission, not authenticated source/tenant/record/encryption security, bounded payload/graph/queue resource policy, crash recovery or full roadmap certification.


## 2026-10-19 shared graph traversal and native regression

Invariant: an unlimited selected graph must not require one CLR call frame per child edge. Parent-query root depth is current depth plus one without32-bit wrap/overflow. Root/sibling preorder, single child capture and graph membership validation remain unchanged.

- HolonGraphWritePlan and HolonGraphProjection now traverse explicit heap stacks; depth counters are64-bit internally. No hidden depth cap/truncation is added. Source intent serialization, canonical fingerprints and provider transaction ownership are unchanged.
- Full Shared723 passed,0 failed/skipped (`TestResults/CosmicMongoCurrent/shared-unit-current.trx`, `graph-stack-shared-current.log`). Ten new cases cover4096-node unlimited write planning,12000-node embedded/indexed read projections, four parent-query int.MaxValue offsets, path cycles/frontiers and preorder/repeated-reference rules. Producer and consumer builds exit0.
- Full SQLite659 passed,0 failed/skipped, consumer exit0 (`sqlite-graph-stack-current.trx`, `graph-stack-sqlite-current.log` under TestResults/CosmicMongoCurrent). Consumer Shared hash `BAE37137E58AD4EAF0639CC136CD8F72FBBBE57D1AD1F0559D90C49ACE7CDC88`; own SQLiteCurrent/integration-current.db is absent after disposal.
- Initial full Neo180 run:158 passed,22 failed,0 skipped. Every failure stopped at an initial empty-store assertion before test mutation; preserve `TestResults/Neo4jCurrent/neo4j-graph-stack-baseline-failed.trx`, SHA256 `BF743621A2C88663DB77120F628F1E3F6D605DBDFBAA268B4F8A70BFC77F8213`. Read-only inventory found exact local Avatar `af1bc282-2a9e-47bb-b6ca-69f07fab4c25`, username `history-a094a17af9d14277b7099f8051a4abda`, and its version1 archive. These match the existing obsolete credential-history native fixture.
- Root cause of the retained fixture: the older native test expected archived credential selectors to authenticate; the concurrent session correctly tightened the existing shared selector to latest-only. Updated the test to require HYPERDRIVE_PAYLOAD_INVALID/ArgumentOutOfRangeException for nonzero credential versions, while explicitly verifying identity-based historical reads and current rotation/deletion. Production authentication was not weakened or changed.
- One reviewed reconciliation test passed (`neo4j-reviewed-credential-fixture-current.trx`). It pins the failed report hash, exact fixture ID/marker, full current/version1 identity/lineage and known test credential values, and verifies zero relationships before archiving all properties. Archive `graph-stack-reviewed-credential-fixture.json` is8272bytes, SHA256 `4415340A413ABD7EA22E442FA123FF938F2AC8262E2FF35F463B4071A3F36708`. After archive flush/reparse, one native transaction conditionally removes only the two exact unchanged property maps. Other nodes/queues/schema are untouched. This is a one-time reviewed test-fixture operation, not production recovery or routine data reset.
- Final Neo full run passes180,0 failures/skips (`TestResults/Neo4jCurrent/neo4j-native-current.trx`, `graph-stack-full-final-current.log`); runner exits0 and stops its retained service. Native consumers pin Shared `BAE37137E58AD4EAF0639CC136CD8F72FBBBE57D1AD1F0559D90C49ACE7CDC88`, Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6` before startup. External EVM/Mongo runtime/output ownership is preserved.
- Scope remains the shared traversal owners. Native hydration/mapping/history and manager recursion, SQLite checked parent offsets and Neo native/history32-bit offsets require additional direct certification/corrections. No COSMIC publication, new runtime/download or Git/index operation in this batch.


Graph batch terminal follow-on: MongoDB64 native cases plus retained replica initialization1 pass,0 failures/skips (`TestResults/SubscriptionUsageCurrent/mongo-single-node-transactions-current.trx`, log `TestResults/CosmicMongoCurrent/graph-stack-mongo-native-current.log`). Runner exits0, stops its owned process and releases the Mongo lease. Existing owner bounds raw Mongo log8,618,761bytes to65,064-byte tail after shutdown; retained database/runtime are preserved.

Mongo consumer and current producer Shared SHA256 are `4FD44F9A3F8FF12C61625F90044BD42B4306F1373756B65256FA576494EB4BE2`, Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`. Shared was rebuilt after the concurrent session's scoped local commit; do not relabel the earlier BAE assembly hash as this later DLL. Read-only PE metadata comparison of all8 method signatures/IL bodies in HolonGraphWritePlan/HolonGraphProjection and their generated nested types matches the unit-tested BAE snapshot exactly, digest `0403DFBFEB9D3BFBFC14698EE0C32073E39B0603D7D9517A6B74CCE99EC51E64`. This is exact graph-method parity, not proof that unrelated assembly metadata or code is identical. No consumer binary was overwritten during its running test.

Final graph batch: Shared723, SQLite659, Neo4j180, MongoDB64; zero failures/skips. Reviewed fixture reconciliation1 also passed. Initial failed180 report and exact8272-byte fixture archive remain retained. Native/consumer handles are terminal; Shared/SQLite build windows and Mongo/Neo leases explicitly released. Provider-specific hydration/offsets, history/query/manager recursion and the broader roadmap remain open; no COSMIC commit/push/deployment was performed.


## 2026-10-19 SQLite native hydration and full-range depth

Invariant: public current depth is any nonnegative32-bit position. Parent-query result roots are one edge later; that arithmetic must not wrap or force rejection of a valid request. Selected native hydration must not consume one CLR call frame per edge.

- Red evidence:36/36 new native boundary cases failed against the prior producer (`TestResults/CosmicMongoCurrent/sqlite-depth-before-current.trx`, `sqlite-depth-before-current.log`).32 parent combinations and four collection cases exposed int.MaxValue rejection, including child loading disabled.
- Source fix at the existing repository owner: accept all nonnegative current-depth arguments, widen four parent offsets and private hydration depths to64-bit, and replace recursive AttachLoadedChildren calls with explicit frames and path exit markers. Mapping, child order, deleted-child visibility and negative-input rejection retain their existing semantics. No alternate persistence route, retry, cap or truncation is added.
- Final full SQLite701 passed,0 failed,0 skipped (`sqlite-depth-current.trx`, `sqlite-depth-current.log`).42 new cases:36 boundaries, two actual2048-record native save/load graphs with bounded reread, two selected-cycle/frontier cases and two deleted-child/retained-ID checks. Initial697 also passed before four parity tests were added; final701 supersedes that intermediate success.
- Producer/consumer builds nonincremental, references built in order, exit0. Before native startup SQLite producer and consumer SHA256 both `78002FFE2139092C41592ADC143534F5D10613CE515DA4315BCCB110687B4E42`; tested consumer Shared `4FD44F9A3F8FF12C61625F90044BD42B4306F1373756B65256FA576494EB4BE2`, Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`. See `sqlite-depth-binaries-current.log`. Existing reusable environment and fixture owner used; no downloads/installations.
- Final test handle97070 exits0. The SQLite provider build window was coordinated/released to the other session; no shared/Core source edits, Git/index operations or external runtime/lease interference. Generic DTO, manager tracking, historical graph, query/cancellation and other provider routes are not claimed by this native repository suite. Neo4j-specific arithmetic/hydration remains next.


## 2026-10-19 Neo4j native hydration/frontier work

Invariant: parent-root offsets do not wrap public nonnegative current depth, and no child query is issued after the requested finite frontier. The native hydration owner must not recursively call itself once per selected edge.

- Eight native boundary cases first fail against the prior producer (`TestResults/Neo4jCurrent/neo4j-read-depth-before.trx`, `read-depth-before-current.log`): four current cases return an unwanted grandchild after int.MaxValue wrapping; four historical cases reject the wrapped negative offset. Sync/async and ID/key variants cover both current and historical roots. Red fixture writes were acknowledged before reads; only those owned setup IDs/archives were cleaned.
- Source fix:64-bit parent offsets/private history depth and an explicit frame stack at HydrateNativeChildrenAsync. The owner enforces maximum before querying, preserving native row order, repeated-identity/parent-edge validation and existing partial descendant I/O evidence. No retry/fallback, cap or alternate provider is added.
- Existing Neo unit130 passes,0 failures/skips (`neo4j-unit-current.trx`, `read-depth-unit-current.log`). Initial full190 run passes all10 new cases, including two1024-node real graph save/load cases. It has141 passes/49 failures overall: the first actual failure is the older collection test expecting one child at current depth1/max depth1;48 subsequent failures are global-empty-store guards caused by its retained fixture. Preserve `read-depth-full-before-frontier-assertion.trx`, SHA256 `EB6F27E9DF11A94C5D9B12246624A32289C39227D3461F29391C20C87729A38A`.
- Corrected that exhausted-frontier assertion to zero children. Its setup now explicitly acknowledges the write before enabling cleanup; all subsequent operations are read-only, so a failed read assertion no longer strands its known fixture. Uncertain setup writes remain preserved. Production behavior is not rolled back to satisfy the stale assertion.
- Read-only inventory identified exact root `1fbeae3b-852f-4efc-9185-4575a452637b`, child `ab1acc6e-d855-4b9e-9a29-4eeb2c6596f1`, leaf `9af6e34b-795d-4896-8342-c3eb81f335a2`, kinds Holon/Avatar/Holon and two HAS_CHILD edges. One reviewed reconciliation test passes (`neo4j-reviewed-read-frontier-current.trx`). It pins the failed report, exact IDs/kinds/parent snapshots/selected metadata/edges, archives all properties/relationships and verifies them before atomic conditional deletion of only those three unchanged nodes. Archive `read-depth-reviewed-frontier-fixture.json` is9302bytes, SHA256 `DCA3F566C0401C308C0A31BBE503D18A98B8720C68C60B48780EABA8C0A01BD2`. No broad database reset or unrelated cleanup.
- Initial native/unit/red Shared snapshot `FC5391A1967466E3DFC0307619C0486BD25AD50D7600C8D101501BBF4A842720`, Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`. Initial tested Neo producer/consumer SHA256 `B942FDBF558E0BEB7A527F0F474D5782EF23EC7A68B8C8C0E89F36F6BC8C76B5`. Final190 rerun in progress uses newly rebuilt Shared `2ED11E8C86D8251761C5DBD580D9A65AC4C12363DFDB96D5CF51B0A3B3F6C52B`, same Core; record its terminal result separately. These snapshots are not interchangeable byte hashes.
- Historical roots retain the existing current-descendant semantics. Coherent historical graph reconstruction, generic/manager/query/visibility/cancellation and all-provider conformance remain open. Existing runtime/test service reused, producer windows/native lease coordinated, no installs/downloads, Shared/Core source edits, Git/index writes or COSMIC publication.


Neo4j depth batch terminal follow-on: final full190 native passed,0 failed/skipped in91seconds (`TestResults/Neo4jCurrent/neo4j-native-current.trx`, `read-depth-full-final-current.log`). Latest matching producer unit130 also passed,0 failed/skipped (`neo4j-unit-current.trx`, `read-depth-unit-current.log`). All10 new native cases pass, including1024-node sync/async identity reads. Runner31130 exits0 and stops the retained service; ports17687/17688 have no listeners and the Neo lease is released. Final producer, native consumer and unit consumer Neo SHA256 all `1A7CBF1173B915780D5DCA52B8ECAB1B538F95E7ED0ECA1833A560A96F6DC9BF`; final Shared `2ED11E8C86D8251761C5DBD580D9A65AC4C12363DFDB96D5CF51B0A3B3F6C52B`, Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`. Earlier B942/FC5391 hashes remain historical checkpoints, not the final tested binaries.

Final runner reports free C bytes before72,381,546,496 and after72,429,703,168. Failed eight-case/initial190 reports and the exact reviewed fixture archive remain retained. Only known test fixtures were removed; reusable runtime/database/schema/other queues remain. External provider session notified of terminal cleanup and released windows. No Core/Shared source edit, Git/index operation, installation/download or COSMIC publication. Full objective remains active: this batch certifies this native hydration/depth owner, not coherent historical graphs, all routes/providers or the wider roadmap.


## 2026-10-19 Core lifecycle/save-plan traversal and single capture

Invariant: one operation captures persistent input once for initial/staged values and one physical child list for accepted membership/descendant acceptance. Deep lifecycle operations must not require a CLR frame per selected edge. Stable preorder/operation-derived identity positions, lease cleanup and pending-edit publication remain owned by the existing plan.

- Red2: both deterministic capture cases fail against prior Core089E (`TestResults/CosmicMongoCurrent/core-lifecycle-before-current.trx`, `core-lifecycle-before-current.log`). AcceptCommitted followed by a second child getter could omit acceptance of a baseline child; separate initial/staged serializations could observe different persistent values. These failures occur without provider I/O.
- Fix scope: HolonChangeTracking and HolonSavePlan only. Explicit stacks replace recursive accept/detect/reject planning, staging and acknowledgement walks; internal levels are64-bit. Acceptance reuses one selected physical child capture; initial/staged canonical values reuse one serialization. Semantic AllChildren selection is retained, not replaced with physical Children.
- Core builds net10.0/netstandard2.1,0 errors (`core-lifecycle-producer-build-current.log`). Full Core1073 passed,0 failed/skipped (`core-lifecycle-current.trx`, `core-lifecycle-current.log`). Seven added cases cover capture defects,4096-node lifecycle scope/membership undo,2048-node missing-baseline preflight/staging/deterministic replay/publication and all-lease release after late repeated-node rejection.
- Tested Core net10 producer/unit consumer SHA256 both `7D1D0B879660005331BB2F4BC42318273181B7F090F39909620CB9B391A0CE01`; netstandard `84FABECB5E5017DADE31C8FC32D3155AEBFCA942F9E4CA50A5B4ADF212452D61`. Unit consumer rebuilt nonincrementally after producer, references in order.
- Shared/SQLite producers and native consumer rebuilt in order. Full SQLite701 passed,0 failed/skipped (`sqlite-core-lifecycle-current.trx`, `core-lifecycle-sqlite-current.log`); Core producer/native-consumer byte parity checked before startup. Copied Shared `4610BA6F96D8459B985131817D8A5A165F14AAE91424489E7DD2DC3D8A7E9E67`, SQLite `D51F5EB7ACA50A5C0E89DA12F706C0D82292F704704D35CA4003CBFA410F338B`; see `core-lifecycle-sqlite-binaries-current.log`. This verifies existing public manager/concurrency/outbox routes, not arbitrary-depth manager preparation.
- Downstream full Shared732 passed,0 failed/skipped (`shared-unit-current.trx`, `core-lifecycle-shared-current.log`), consumer rebuilt against the new Core/Shared snapshot. Handles78747/96425/93572 all terminal0, fixed SQLiteCurrent/integration-current.db absent. C free bytes before Core phase72,386,359,296 and final72,363,925,504. No installations/downloads or disposable fixture accumulation.
- External session's copied NEAR/SOLID consumers/native leases were preserved; Core/Shared/SQLite windows explicitly coordinated and released. No source edits outside our Core files/tests/COSMIC docs, Git/index operations or COSMIC publication.
- Remaining root causes: manager PrepareHolonForSaving/SetParentIdsForHolon mutual recursion, typed semantic AllChildren versus physical Children tracking/undo, and declared failure behavior of arbitrary setters/notifications. Broader graph history/query/provider guarantees, durable recovery/security/migration, committed deltas and CQL remain in the full active objective. No completion claim.


## 2026-10-19 ordered manager preparation and checked revisions

Invariant: preparation is an operation-local graph pass, with parent audit/context available to child custom extraction and parent navigation stripping/encryption performed after descendants. Logical revision counters cannot wrap and reach a provider save.

- Red2: sync/async V2 version-exhaustion cases fail against the prior producer (`TestResults/CosmicMongoCurrent/preparation-before-current.trx`, `preparation-before-current.log`). The old unchecked increment can reach the controlled provider and publish a wrapped revision. No real provider was used for the red reproduction.
- Source fix confined to HolonManager-Private.Helpers.cs: enter/exit heap frames replace mutual PrepareHolonForSaving/SetParentIdsForHolon recursion; a shallow audit/custom/parent pass and exit strip/encrypt preserve ordering. Captured semantic children retain order; null/repeated graph nodes fail explicitly. The revision increment uses checked arithmetic. No alternate persistence path or retry added; Legacy Hyperdrive routing is unchanged.
- Core net10.0/netstandard2.1 build0errors (`preparation-core-build-current.log`). Full Core1079 passed,0 failed/skipped (`preparation-core-current.trx`, `preparation-core-current.log`). Six new cases: two version exhaustion, two2048-node public V2 preparation/audit/navigation and two parent-context/plaintext-before-encryption tests. They verify provider input encrypted/stripped and caller references/plaintext preserved after acknowledgement.
- Core net10 producer SHA256 `0EFD46F8DAB43D55692016203383A973B5198E691C79C45FA8CDF0557FF22D50`; netstandard `F2BF08E8531B3269BEA37101BC568757E29DDBC5AAA2AA2C2C02566FDCF9723B`.
- Full SQLite705 passed,0 failed/skipped in116seconds (`sqlite-preparation-current.trx`, `preparation-sqlite-current.log`). Four new public V2 cases persist/load1024 nodes through sync/async and saveChildrenOnProvider flags, verify logical version1/actor/parents/clean tracking and a no-op without extra revision. Core producer/native-consumer byte parity verified before startup. Copied Shared `7605DF5A2524DF86C6E6C95DA115CCA0F9608738C20C118DBDC61414DF96FF01`; SQLite `D1517757F9D35878FF39CB8096B62349052D3C3619C579DE4C23038671F37226`; see `preparation-sqlite-binaries-current.log`.
- Downstream Shared732 passed,0 failed/skipped (`shared-unit-current.trx`, `preparation-shared-current.log`) after fresh consumer rebuild against final Core/Shared. Handles20194/17074/90753 all terminal0; SQLiteCurrent/integration-current.db absent. Final C free bytes71,956,344,832. Existing runtime/dependencies reused, no downloads/installations or accumulated disposable DBs.
- Core/Shared/SQLite output windows coordinated with the provider session; its copied NEAR/Google consumers and native services were untouched. No Shared/provider production source edits, Git/index writes or COSMIC publication. Broader typed semantic tracking/undo, arbitrary callbacks/constructors, encryption key-context policies, history/query/provider certification, durable recovery/migration/deltas/CQL remain in the full active goal.
