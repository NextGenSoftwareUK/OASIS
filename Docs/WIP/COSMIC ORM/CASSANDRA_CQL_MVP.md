# Cassandra CQL MVP — native/restart verified; paired demo pending

Updated 30 October 2026. This provider is still red in the demo inventory.

## Implemented

CassandraOASIS now explicitly implements the shared COSMIC query interface.
The shared bounded identity compiler admits a complete trusted catalogue of at
most100 GUIDs, identity equality/membership/Boolean composition and local paging.
The native statement uses prepared partition-key `WHERE id IN ?`; no table scan,
ALLOW FILTERING, arbitrary content predicate or secondary-index assumption.
Native IN order is unspecified, so materialized rows follow the compiler's stable
catalogue order before paging. Read authority, native/payload identity agreement,
duplicate IDs, deleted rows, result budget and4MiB decoded payload budget are checked.
Errors and cancellation publish no partial rows. Driver I/O has no cancellation
parameter here: cancellation is checked before/after I/O and during materialization;
active network-operation cancellation and transport-byte bounds remain open.

Activation sets active state only after successful session/schema initialization;
deactivation clears it before disposal. Constructor rejects noncanonical keyspace
identifiers and invalid replication factors before interpolated schema statements.
No alias capability is inferred for ScyllaDB or Amazon Keyspaces.

## Proving controls

23passed/0failed/0skipped across the complete unit project: save ownership/batch5, metadata5, bounded
compiler/admission/lifecycle/budget controls and identifier rejection4.
Proving file: Providers/Storage/TestProjects/
NextGenSoftware.OASIS.API.Providers.CassandraOASIS.UnitTests/CassandraCqlTests.cs.
Evidence TestResults/CassandraCqlCurrent/controls.trx SHA256 `b78c691c3400393ae363cfbe661ba55c2489fc796500ea16c60c2f18b60139b3`.
Provider/unit builds pass with bounded workers and cached project references.
CassandraCSharpDriver3.21.0 was missing and restored from official NuGet once.

## Exact next step and remaining gates

One retained authenticated Apache Cassandra4.1.12 environment is now verified;
original cluster/schema/actor/IDs were recorded before acknowledged writes. SaveHolon acknowledgement ownership is fixed and unit-tested: staged payload/ID/keys
publish only after native write completion, IsSaved/SavedCount count acknowledgements,
and batches stop or continue as requested. Failed writes preserve caller identity.
HolonManager retains tracking acceptance ownership. Native3/filter2/missing0/page1, budget rejection and JSON/write-time preservation
across an actual restart pass. Next: one Mongo-paired C#/HTTP/browser5/4 preset
and hub registration. Do not mark native acceptance green
from these no-backend controls. Original source/runtime/container are retained and the node is stopped.

TLS configuration, arbitrary predicates, graph/children, conditional writes,
replication/history/migration and EF/NHibernate parity remain unverified/open.
Shared HyperDrive or other provider proof does not certify this adapter.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).


## Native runtime and original-source proof

Official Apache Cassandra4.1.12 Linuxamd64 image pinned to
`sha256:57b251cede6ebb8402c9cfc8ee8931438086ce2640a5657e80ecebc9f4cf3bbd`,
expanded339,932,214B. Source `/var/tmp/oasis-cassandra-cql`34,030,704B logical,
container oasis-cassandra-cql, localhost19042,2CPU/2GiB/512MiBheap. Original host
1390c422-56dd-4ee7-93d7-9cfe01579174/cluster COSMIC CQL Cassandra survives restart.
PasswordAuthenticator/CassandraAuthorizer enabled before boot; random private
local role replaces the default role. Credentials remain in private source files,
never argv/docs/logs. Native transport is local plaintext: no TLS/hosted claim.

Initial setup wrongly expected exit0; JVM SIGTERM exits143 after drain hooks.
Read-only `--verify-original` reconciled exact original identity/role acknowledgements
without replay/reset. Correct teardown requires143, stoppedByUser, noOOM/noError.
Original intent/receipts retained. cqlsh cannot write its home history; the warning
is retained in original identity evidence, no history directory/environment added.

Native initial1/restart1 pass; original3records/JSON hashes/write timestamps match,
source-loss/orphan baselines reject. Own node stopped after each phase, runner0.
Raw evidence TestResults/CassandraCqlCurrent/native-run.log and native-*.trx:
- native-first: `fbdf47e0c9d049f35b415bd0aead40c5a6c01f5d7a83bed0a24c40d6a5d535db`
- native-restart: `2ac6eb5f18b352d2c89cb74d56c38ffb53670cb5ecb5fb500010ac9b8851b6d4`

Run `Scripts/TestHosts/Run-CosmicCassandraLinuxDemo.ps1` for bounded native/restart
checks (retained environment required). Setup script refuses an existing source;
do not rerun it or replace seeds to recover test failures.
Proving native fixture: CassandraCqlNativeTests.cs in the integration project.
[Official image/configuration](https://hub.docker.com/_/cassandra).
