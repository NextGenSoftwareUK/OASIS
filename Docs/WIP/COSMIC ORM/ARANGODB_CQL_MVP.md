# ArangoDB CQL MVP — native/restart verified

Updated30October2026. Native/restart pass; paired/browser acceptance remains red. Dgraph was mistakenly named in the previous
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
