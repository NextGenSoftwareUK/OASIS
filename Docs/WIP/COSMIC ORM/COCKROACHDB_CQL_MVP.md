# CockroachDB CQL MVP — controls verified, native acceptance pending

Updated30October2026. Existing product identity; no native or paired demo claim yet.

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
