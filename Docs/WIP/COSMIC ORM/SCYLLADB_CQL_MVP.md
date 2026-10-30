# ScyllaDB CQL MVP — controls pass; native setup pending

Updated 30 October 2026. Demo inventory remains red; Cassandra proof is separate.

## Implemented and proved without backend

ScyllaDB explicitly implements ICosmicQueryStorageProvider, using the shared
bounded identity compiler bound to ScyllaDBOASIS. Catalogue at most100GUIDs,
identity equality/membership/Boolean composition and paging. Prepared native
partition-key IN statement; no scan/ALLOW FILTERING/arbitrary-content predicate.
Native unordered rows restore stable catalogue order before paging. Identity,
authority, duplicates/deletion,4MiB decoded payload and result budget are checked;
errors/cancellation publish no partial rows. Cancellation checks surround driver
I/O; active I/O cancellation and transport-byte bounds remain open.

SaveHolon stages copied payload/ID/provider keys, publishes caller identity and
IsSaved/SavedCount only after the native write task completes. Failure preserves
caller state; batch stop/continue and acknowledged counts fixed. Tracking acceptance
stays with HolonManager. Activation cannot retain stale active state on failure;
deactivation clears it before disposal. Canonical schema identifiers and positive
replication checked before interpolated schema statements.

All23unit tests pass: query/admission/lifecycle/schema-name controls and injected
save-boundary failures/incomplete acknowledgement/batch stop-or-continue tests.
Provider/unit/native-fixture builds pass; cache-only restores, no new packages.
Proving files ScyllaDbCqlTests.cs / ScyllaDbSaveOwnershipTests.cs in the unit project.
Raw TestResults/ScyllaDbCqlCurrent/controls.trx SHA256 `8e3be116816f3e701d186f95445c6e3c813c2d826fec17da607c880ad065c223`.

## Native source and exact next step

One official2026.3 Linuxamd64 image is cached/reusable, pinned
`sha256:2a3cf99b55d793a41e19b4d8afa4084744bc05517f4b4a211483c8d2abe4ef3a`,
image IDae6d996ec593faef2a0cbdce559de45f8fbaa397309b23c4f461d73047d40bcf,
577,655,941B expanded. Local non-production evaluation only, no paid cloud.
[Official Docker guide](https://docs.scylladb.com/manual/stable/getting-started/install-scylla/run-in-docker.html)
/ [vendor license](https://raw.githubusercontent.com/scylladb/scylladb/master/LICENSE-ScyllaDB-Source-Available.md).

Original source `/var/tmp/oasis-scylladb-cql`, container oasis-scylladb-cql,
loopback19043only,1CPU/2GiB/Scylla1Gmemory/smp1/developer-mode1/overprovisioned1.
Private random credentials/cqlshrc and environment.pending/container ack retained.
Image mount inspected vendorUID998/GID1000/entrypoint then unmounted.
PasswordAuthenticator/CassandraAuthorizer configured from first boot, version
housekeeping disabled, no TLS/hosted/performance/production claim.

Setup exposed a vendor wrapper defect: it joins --cluster-name without quoting;
COSMIC CQL ScyllaDB becomes three positional arguments and native child exits2.
No native node/system identity, roles, schema, actor or business records created.
Only source data/.cassandra/cqlshrc observed. Supervisor container kept running,
so owned container was explicitly stopped0/noOOM/noError; setup runner65773terminal1.
Original pending setup remains intentionally unresolved. Do not recreate/reseed.

Next: fix/prove the startup argument boundary while retaining exact original
container/source/credentials, explicitly reconcile the pre-bootstrap pending
setup, then native3/filter2/page1/budget/JSON+writetime/restart tests. The built
ScyllaDbCqlNativeTests.cs fixture requires admitted environment.json and refuses
missing/orphan original seed/snapshot. Then Mongo-paired C#/HTTP/browser5/4 and hub.
No native success inferred from compilation or shared Cassandra wire protocol.

## Deferred scope

IsVersionControlEnabled exists but has no implemented save/history behaviour;
record it as unverified rather than advertising history support. Avatar save
ownership/child graph/history/CAS/replication/migrations/arbitrary predicates and
full EF/NHibernate parity remain open. Legacy HyperDrive issues documentation only.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
