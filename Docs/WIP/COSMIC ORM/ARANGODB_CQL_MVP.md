# ArangoDB CQL MVP — native/restart verified

Updated30October2026. Native/restart/Mongo-paired/browser scoped MVP verified. Dgraph was mistakenly named in the previous
queue; there is no exact Dgraph identity/source in the current226-provider inventory.

## Root fixes and supported CQL scope

The provider previously used PUT replacement for supposed upsert and discarded
HTTP errors. New records could fail while SaveHolon returned apparent success.
The shared document writer now uses documented POST collection overwriteMode=replace,
waitForSync=true and validates returned _key/_id/nonempty_rev before success.
SaveHolon stages concrete runtime JSON, ID and copied provider keys; caller ID/key
and result IsSaved/SavedCount publish only after acknowledgement. Native revision
is returned as result metadata ArangoDB.Revision. Manager owns clean-state/revision
publication. Batch error/continuation/counts now reflect acknowledged outcomes.
This is not expected-revision CAS; concurrent replace semantics remain unverified.

Activation checks database/collection creation and accepts only documented409
errorNum1207duplicate-name. Other errors keep activation false, including stale
reactivation. Reads treat only404/errorNum1202as missing; forbidden/server/missing
collection errors propagate. Hard-delete HTTP failures propagate. AQL failures
or absent result arrays no longer become empty success; broader AQL cursor paging
is not certified and is not used by this CQL adapter.

CQL binds shared bounded-key algebra to exact ArangoDB authority: complete trusted
catalogue at most100GUIDs, identity equality/membership, Boolean composition and
paging. Native GET each document key only; no table scan or managed content filter.
Native collection/key/revision, decoded ID and read authority are checked before
rows publish. Deleted/missing records skipped, children excluded, complete-stream
4MiB/result budgets enforced. In-flight HTTP cancellation and late failure yield
no partial rows. Version-control mode rejects before HTTP. Independent reads have
no distributed snapshot guarantee; plaintext provider JSON scope only.

## Evidence

Eight focused save tests failed on original production code, then passed after the
root fix. Initial delayed-receipt test exposed an unbounded wait in the test itself;
its exact owned testhost was stopped, the await bounded at2seconds, and reproduction
rerun terminally (8failed/0passed). No business/native environment was involved.
The final suite passes42/0/0:4existing provider controls,8save ownership,7compiler,
15execution and8HTTPboundary cases. A stale test calling nonexistentGetProviderVersion
was removed; no fake production API was added to satisfy it.

| Report under TestResults/ArangoDbCqlCurrent | Result | SHA256 |
|---|---|---|
| save-reproduction.trx |8failed/0passed, expected|4bb88f1767ca50817e90f1d69ef30ae94c47e6997c4bb6f0b7dbf4fe959957c6|
| controls.trx |42passed/0failed/0skipped|b2f93c7cb0b92cc209ec9fd73faeaf0a746a90f291416c6b38321abd27919422|

Provider and test builds pass with bounded workers and cached dependencies.
Focused tests reside in the provider UnitTests project: ArangoDbSaveOwnershipTests,
ArangoDbCqlTests, ArangoDbCqlExecutionTests and ArangoDbHttpBoundaryTests.

## Exact next step / open gaps

Inspect existing runtimes/images first, then create one authenticated retained
native ArangoDB source if absent. Verify original3/filter2/page1, payload/revision
stability and actual restart, then Mongo-paired C#/HTTP/browser5/4 and hub preset.
No proof enrollment from controls alone. No native database/process/runtime created
in this step. Follow resource ledger and preserve unresolved write acknowledgements.

Cross-client CAS, full graph/maxChildDepth semantics, history, content predicates,
metadata query depth, AQL cursor completion, central registration and replication
remain red/unverified. Avatar caller-state ownership is not fixed/certified here.
Legacy HyperDrive remains documentation-only.

[Official document API](https://docs.arango.ai/arangodb/stable/develop/http-api/documents/)
/ [current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Original native records and actual process restart verified

This supersedes the pending native step above. Official ArangoDB3.12.12 image
pinned to Linuxamd64 digest
`sha256:ddaeda57726a6ff42186a3fea205a848585c92fd54814b892193925338f9e25c`
was cached once after existing-runtime/image inspection found no reusable server.
One rootful WSL Podman container oasis-arangodb-cql maps127.0.0.1:18529to8529,
with2CPU/2GiB/256PIDs. Private credentials/env0600; data/apps retained under
`/var/tmp/oasis-arangodb-cql`. No public hosting, signup or payment.

Native setup version3.12.12 and original SERVER.serverId214237195051418survive
restart; protected anonymous access401, only_system database at setup. An initial
setup check called the cluster-only server-ID endpoint and received500in single
mode. The verifier was corrected to authoritative native data/SERVER.serverId;
explicit resume validates pending intent/container/image/mount/exit state. The
same container/data/credentials were preserved; no recreation or seed replay.

`ArangoDbNativeCqlTests.cs` passes1initial +1actual process-restart. Original
actor/three IDs were journalled before saves, with a native revision ack for each.
Native3(Espresso/Latte/Tea), ID-membership2 and page1 pass through the shipping
adapter. Exact native payload hashes and acknowledged revisions match restart;
CQL changes neither hashes nor collection revision. Fixture-only AQL catalogue
proves exactly the original3business IDs; CQL itself performs only key reads.
Caller remains new/dirty until manager-owned publication. Plaintext JSON scope.

Evidence under TestResults/ArangoDbCqlCurrent:

| Report | Passed | SHA256 |
|---|---:|---|
| native-first.trx |1|994240932320fa334d6fb0d908da146e34f207ebd84baf8c24a2b4bdc509d7f6|
| native-restart.trx |1|8c5670617458dcde0b01394d6c5d95ddc4f8e9fb43e99a65f1ccc39d3e76197f|

Original source233,611B retained, seed pending absent; owned container stopped0
after both phases. Image612,978,651B reusable; no repeated runtime downloads.
Linux.NET10.0.12 and portableVSTest18.11.0 reused, test child runs existing user1000.
No raw reports committed. Safe source cleanup requires explicit identity retirement.

Run native tests after bounded integration build under one provider lease:
`wsl -d Ubuntu -u root -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-cosmic-arangodb-linux.py`.
Creator refuses existing source; its explicit --resume only completes unresolved
environment admission after exact original identity checks, never resets data.

Next: same original records in Mongo-paired C#/HTTP/browser5/4, then hub enrollment.
Inventory stays red until that passes. Full CAS/graph/history/registration and
replication gates above remain open. [Official image](https://hub.docker.com/_/arangodb).

## Mongo-paired C#/HTTP/browser MVP verified

This supersedes pending paired steps above. Shared HolonManager V2 and native
adapters return5physical rows (ArangoDB3 + Mongo2), or4for a two-ID subset (2+2).
Each physical copy retains its provider provenance; the unrelated Mongo record
is excluded. These are bounded native identity predicates, not arbitrary property
filters, HyperDrive replication acceptance or a distributed snapshot guarantee.

```powershell
# Interactive teaching preset. Stop from the page to preserve native source.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ArangoDB -Federated

# Bounded native/restart/C#/HTTP/post-pair verification and owned cleanup.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ArangoDB -Federated -VerifyOnly
```

Browser5134verified both visible JSON responses (5/4), source provenance and
retained-source shutdown. Screenshot capture was unavailable; no image evidence
claimed. `ArangoDbMongoCqlNativeTests.cs` exercises the shipping shared C# example
and HTTP host, forged-actor/unknown-preset400 and inactive-provider503without rows.
Both bounded and interactive runs pass5one-test phases. Native post-pair rechecks
original hashes, write revisions and collection revision; all unchanged.

Hub routing passes8single/16plans,43paired/86plans,72paired and3archive rejections,
listeners unchanged. Exact temporary Mongo database verified/dropped, pending/ready
cleared; both owned services stopped0. Native state324,540B retained; RocksDB
physical housekeeping can change size while original logical hashes stay fixed.
No new downloads or source reseeding. Full parity gates above remain open.

Evidence under TestResults/ArangoDbCqlCurrent:

| Report | Passed | SHA256 |
|---|---:|---|
| mongo-federation-bounded.trx |1|1c872d0e63eafad78a0b89b08e92ab66881e20359c579d2c19edbb32706d8593|
| mongo-federation.trx |1|ecf1d2f2029c564820e165acf2adc4899319a8f1251746b0cb596dd26a6e2d90|
| native-postpair.trx |1|338974c35a489dd4986ee9e1235dae83b86c2f654825a0eca1d75f2537bdd435|

Final browser-run.log contains all5phase hashes. Earlier native hashes above refer
to the original admission run, not refreshed current TRX files. Reports remain
regenerable outside Docs. [Launcher](CQL_DEMO_LAUNCHER.md).
# ArangoDB bounded CQL MVP

The provider now has a bounded identity query path over the native AQL
cursor API. It uses the existing `oasis/holons` documents and binds
canonical GUID keys in a `DOCUMENT` lookup. It strips embedded children
before returning the records. Missing documents are excluded by the
native lookup, and paging follows identity and visibility checks. A
native error, incomplete cursor, foreign key or inconsistent payload
rejects the whole result. Budgets are 100 keys, 15 seconds, 8 MiB wire
and 4 MiB decoded payload. General graph and historical queries are open.

The public profile takes a caller-owned HTTP client and an existing
probe. It requires the probe and selected live records to remain public.
The test fixture covers the exact AQL request, paging, deleted and missing
records, private records and malformed cursor outcomes. The provider
build is pending the shared lease.

The opt-in hosted fixture is prepared for the pending WEB4 registration.
It will use `COSMIC_ARANGODB_HOSTED_CQL_SOURCE` and protected origin,
restricted reader and probe settings. Run native qualification with
an approved ArangoDB source. ArangoDB 3.12 requires Linux or the official
Linux container on this Windows host. There is no supported native
Windows package for 3.12.
[Native lookup contract](https://docs.arango.ai/arangodb/stable/aql/functions/miscellaneous/#document).
[Native platform support](https://docs.arango.ai/arangodb/stable/operations/installation/).

All 13 provider tests passed at Core `0492cec40a87a7ab84ac2ec7718e017ae4463871`.
The cursor tests verified 3/2, paging, deletion and absence, private record
rejection and strict failure on a foreign key, bad payload or incomplete
cursor. WEB4 registration and native server execution remain open.

WEB4 now reads protected `CQL_ARANGODB_QUERY_` settings with suffixes ORIGIN,
READER_USERNAME, READER_PASSWORD and PROBE_ID. The connection is pinned
to the configured HTTPS cursor endpoint in database `oasis`. The
transport checks the exact AQL, runtime limit and canonical key set before
sending a request. The protected settings preparer requires the first
reviewed live ID as the probe. Eight settings tests passed.

The native fixture is enabled with
`/p:EnableArangoDbHostedCqlNativeTests=true` and selected with
`TestCategory=ArangoDbHostedCqlNative`. The fixture build passed and all 11 WEB4 admission tests passed at Core
`c7743e6d9ee0432d180311e4a85a23dbd4980b93`. An approved TLS endpoint and restricted reader for
`oasis/holons` are required for the native run.

The original task branch records native restart and Mongo-paired browser
proof at `d36cc175c` and `ed5d9c6bb`. Those results cover the original source
and local demo. This recovery build covers the new WEB4 admission path.

Admission TRX SHA256
`a98217a443336f380162b69701b3b0130defedf71c64b7eea1c87262477c03ed`.
The fixture was compiled here. The approved hosted ArangoDB source and
live Development verification are still pending.

Offline enrollment and native qualification use the same descriptor with Origin. Use
the first LiveIds entry as the probe. The protected settings must match
the reviewed source and contain the restricted reader username and
password. Offline tests used synthetic settings. The owned native source
and live WEB4 acceptance are still pending.

11 November UTC: [ArangoDB provider integration CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43026834539) passed with the authenticated fixture and the scoped candidate `9e82b80e72`. The companion run also passed. Development advanced to `c0b8c1eef1` with the independently verified EOSIO native storage batch while this diagnostic ran. The ArangoDB correction is on the COSMIC branch pending integration, and its separate native restart and federation gates remain open.

11 November UTC: prepared ArangoDB integration candidate `5878dea5f9` on Development `c0b8c1eef1`. The two changed files produce a byte-identical patch to the successful authenticated-fixture CI candidate. The intervening Development changes concern EOSIO native storage. Development integration remains pending while the LayerZero deployment builds and its two successors are queued.

11 November UTC: integrated the authenticated ArangoDB CI fixture correction into Development `48fea5d0ed`, preserving the Solana and EOSIO changes. The two-file patch is byte-identical to the scoped candidate that passed ArangoDB provider integration CI. The change wires the synthetic test-container password into the test client. Hosted HTTPS teaching-source qualification and live WEB4 acceptance remain open. [Focused CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43026834539)

11 November UTC: standard Development CI exported the authenticated ArangoDB fixture settings successfully, but the full integration project still failed in its test step. The focused generic integration pass remains valid. Inspect individual test outcomes before attributing the broader failure to a specific fixture or provider path. [Development ArangoDB job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43028699870/job/130549848025)

11 November UTC: standard Development ArangoDB CI passed all four generic storage cases. The three failures were the Linux Mongo replica-set admission test (missing owned root), native Mongo federation test (missing replica-set connection), and retained ArangoDB restart test (missing retained runner root). The generic job starts only ArangoDB. The remaining CI work is to run the fixture-dependent tests with their own provisioned state and preserve coverage of both test groups. [Development job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43028699870/job/130549848025)

11 November UTC: added an isolated ArangoDB restart workflow using the pinned 3.12.12 image and the existing native CQL test. It uses one fresh fixture across initial load and graceful restart, checks that each phase executed one passing test, and removes the container and its anonymous volumes on exit. Diagnostic `348b78616c` runs on Development `877318b3c7`. Mongo federation and hosted CQL remain separate open gates.

11 November UTC: the first isolated restart job pulled the pinned ArangoDB image but received a connection reset during startup before any tests ran. The corrected workflow bounds authenticated readiness polling, checks that the owned container is running, and allows only transient transport errors while the service initializes. Authentication and version errors stop immediately. Corrected diagnostic `80e4f92139` is published for CI. [First restart attempt](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43030710056)

11 November UTC: corrected startup readiness is running in [ArangoDB native restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031012725). The initial startup failure and companion run are terminal. The old diagnostic tag was removed.

11 November UTC: isolated ArangoDB restart CI passed with one native CQL test and zero skips before restart and one pass and zero skips after restart on the same pinned 3.12.12 container. The retained fixture assertions checked record hashes and collection revision. The workflow completed graceful stop and container/volume removal. This qualifies the fresh CI restart path. Mongo federation and hosted CQL remain open. [ArangoDB restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031012725)

11 November UTC: Development `b22d4667ba` contains the verified QuestDB CI schema provisioning and ArangoDB native restart workflows. The ArangoDB workflow is manually dispatched. QuestDB hosted CQL and ArangoDB Mongo federation and hosted CQL remain open. This batch changes CI workflows only. [QuestDB focused CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43029367152), [ArangoDB restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031012725)

11 November UTC: ArangoDB federation diagnostic `ad775a8da6` extends the verified restart workflow with a disposable loopback Mongo replica set. It runs existing replica admission, native federation, and post-federation ArangoDB identity checks. It uses fresh CI state and leaves the original retained source alone. Hosted CQL remains open.

11 November UTC: ArangoDB CI passed all five phases with zero skips: initial native read, restart read, Mongo replica admission, native SDK/HTTP federation, and post-federation source recheck. The federation test verified 5 full and 4 filtered rows with provider provenance, rejected invalid requests, and verified the owned Mongo database was removed. The original retained source was not used. Hosted CQL remains open. [ArangoDB federation CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031787640)

11 November UTC: Development `73e0d4bb08` contains the verified ArangoDB MongoDB federation and QuestDB restart workflows. Both are manually dispatched. This batch changes only CI. Hosted HTTPS source admission and live browser checks remain open. [ArangoDB CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43031787640) · [QuestDB CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43033757190)

11 November UTC: candidate 880937b05f routes generic ArangoDB and QuestDB cases to the standard provider matrix and calls the dedicated native fixture workflows from the same pipeline. Diagnostic 37a10ab14d exercises all four jobs. Both fixture workflows retain manual dispatch. Development integration awaits the combined result.

11 November UTC: combined database CI passed all four jobs, covering generic ArangoDB and QuestDB plus their dedicated restart and federation fixture tests. Candidate `880937b05f` is ready for the next Development batch. Hosted CQL remains open for both providers. [Combined CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43034639175)

11 November UTC: Development `1c7e670e8d` integrates the verified ArangoDB and QuestDB CI routing. The generic matrix runs the four storage tests for each provider and calls the dedicated fixture workflows for native restart and federation coverage. The three-file patch is identical to the one qualified by all four focused jobs. Hosted CQL remains open. [Combined database CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43034639175)

11 November UTC: the standard Development pipeline at `1c7e670e8d` passed all four ArangoDB and QuestDB jobs, including both generic storage jobs and both dedicated native fixture jobs. This verifies the routing in the regular pipeline. Hosted CQL acceptance remains open. [Development CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43036514678)
