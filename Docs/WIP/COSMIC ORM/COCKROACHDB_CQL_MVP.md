# CockroachDB CQL MVP — native/restart verified

Updated30October2026. Secured native/restart pass; paired/browser acceptance remains red.

## Shared implementation and explicit authority

CockroachDB already derives from PostgreSqlFamilyProviderBase, sharing Npgsql CRUD,
serialization and native execution. CQL formerly rejected every non-PostgreSQL
identity, correctly preventing aliases from inheriting unverified certification.
The shared adapter now has an explicit protected native query-authority binding;
CockroachDB opts in under its own identity. Other facades still have no CQL
capabilities unless explicitly bound. Merely changing a PostgreSQL provider enum
to CockroachDB does not confer its authority. No duplicate CRUD or execution path.

Product compiler binds the shared bounded-key algebra to CockroachDB only:
complete trusted catalogue at most100GUIDs, ID equality/membership, Boolean
composition and paging. The inherited parameterized SELECT/materialization and
budgets/cancellation are reused. Unsupported content/sorting/graph shapes reject.
Native CockroachDB collation, SQL types, arrays, paging, wire and save semantics
are not certified by these control tests; PostgreSQL compatibility alone is not proof.

## Verified controls

12CockroachDB controls pass (5existing metadata +7CQL) and10focused PostgreSQL
family binding regressions pass. The latter check existing PostgreSQL compilation,
identity tampering rejection and facade implementation identity. They are required
regression checks for changed shared admission, not further green-row hardening.

Tests: provider UnitTests/CockroachDbCqlTests.cs and shared PostgreSQL UnitTests/
PostgreSqlCqlTests.cs, PostgreSqlFamilyIdentityTests.cs. Builds bounded and cached.

Evidence under TestResults/CockroachDbCqlCurrent:

| Report | Passed | SHA256 |
|---|---:|---|
| controls.trx |12|17cedda080e830a18c65c3a3275d68c3486abe6969a45509c13b9834fb84f4c1|
| family-binding.trx |10|faa304a2abc32d44307acdad07ffcf20647370e36674d104f61d9002774ca1d6|

## One pinned native runtime admitted

Runtime/cache inspection found no reusable native CockroachDB. The official
v26.2.9 Linuxamd64 archive144,515,867B was downloaded once and vendor SHA256
`e9c41b8ab520fa2cd7cb3d61a08622bd413df4fa1ef725e1e2d75cc9517722d3`
verified before extraction. Exact owned archive removed after version verification.
Binary/native libraries/licenses/runtime manifest retained at
`/var/tmp/oasis-runtimes/cockroach-26.2.9`; binary329,878,936B SHA256
`7e2a7a869f52c083343dcefc01852a3f34f15292c0a4cc63895182296d2d778f`.
No database/node/certificates created, no active port dependency. Reuse this runtime.

Vendor licensing FAQ permits no-key single-node internal development/testing via
start-single-node; this does not establish a public/production/distributed license.
No signup, paid license or hosted cluster created. [Official licensing scope](https://www.cockroachlabs.com/docs/stable/licensing-faqs).

## Exact next step

Create one retained certificate-authenticated single-node source with this runtime;
record original cluster/actor/record IDs before writes, retain CA/node/client keys
outside TestResults, and use strict Npgsql TLS validation. Verify actual provider
schema/save acknowledgements, native3/filter2/page1, payload/revision stability and
process restart. Fix any reproduced SQL dialect mismatch at its owning policy
layer; no PostgreSQL server substitution or silent fallback. Then Mongo-paired
C#/HTTP/browser5/4 and hub enrollment. Inventory stays red until verified.

CAS/graph/history/metadata depth, distributed retries, replication/migrations and
full EF/NHibernate parity remain open. Preserve all existing provider acceptance.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Secured original native environment and CQL restart verified

This supersedes the pending native steps above. One original source at
`/var/tmp/oasis-cockroachdb-cql` retains vendor-generated CA/node/root-client
certificates/private keys, node data, setup/database acknowledgements and original
actor/three holon IDs/write receipts/snapshot. No unsafe internals, no insecure mode,
no global trust changes or PostgreSQL-server substitution. SQL26259and HTTP18004
listen only127.0.0.1. Runtime cache128MiB/SQLmemory128MiB. No-key single-node
internal development scope only; public/distributed licensing not inferred.

Original cluster83847e13-8e1f-4702-b6be-1137902cfc35and oasis database oid104survive
process restart. Initial verifier used a restricted internal cluster-ID function
(42501); its guard was left enabled. HTTPadmin identity requires separate session
credentials (401), which were not minted. The final verifier uses the exact owned
server startup's native cluster ID plus supported SQL database oid, both checked
across restart. All phases reuse the same original data/certs. No setup replay/reset.

`CockroachDbCqlNativeTests.cs` passes1initial +1actual process-restart through the
shipping CockroachDB provider/Npgsql9.0.3 with strict VerifyFull/client-certificate
configuration. Schema cosmic_cql and indexed provider tables created natively.
Three original holons acknowledged once, metadata journal retained. Native3,
ID-membership2, missing0, page1 and no-partial result-budget rejection pass.
Original plaintext JSON hashes and native MVCC timestamps remain unchanged after
reads/restart. Exact original row count3; no missing source recreated. Existing
inherited C-collation/paging SQL succeeded on this pinned product, no dialect shim.

Evidence under TestResults/CockroachDbCqlCurrent:

| Report | Passed | SHA256 |
|---|---:|---|
| native-first.trx |1|e0cdd345dab13af503fa8cfad3d24bc8ac400649295aea70bb301522dc7c3721|
| native-restart.trx |1|e857c0af73cf9f0701d8320445363d53c81413fe7db9313f086a8e12745e04af|

Both owned starts stopped0; setup/seed pending absent. Source logical1,076,985,277B,
allocated1,148,727,296B measured by du; retained native files are not disposable
build output. Logs/TRX only worktree TestResults; runtime/certs/data retained.
Cfree71,099,400,192B after native acceptance, no unrelated-change attribution.
Reused Linux.NET10.0.12/portableVSTest18.11.0, no new download in record tests.

Run after bounded integration build under one provider lease:
`wsl -d Ubuntu -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-cosmic-cockroachdb-linux.py`.
Creator refuses existing source; explicit --resume only completes pre-database
admission after original intent/certificate/runtime checks, never resets data.

Next: same original rows in Mongo-paired C#/HTTP/browser5/4 and hub preset.
Inventory remains red until that passes. Broader CAS/distributed retries/graph/
history/metadata/replication gates above remain open. Source-loss recovery must
never bless a replacement baseline or replay an unresolved original write.

The CockroachDB fixture now requires an admitted environment and a matching
seed/snapshot pair before any network/write action; it cannot silently create a
missing source or bless an orphan/replacement baseline. Final1+1recheck passes
with the original rows, source logical1,077,138,658B. The earlier allocated-size
measurement above is from initial acceptance; native housekeeping changes size.
