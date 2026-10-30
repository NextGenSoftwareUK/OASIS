# Cassandra CQL MVP — controls verified; native acceptance pending

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

Create one retained authenticated local Apache Cassandra environment, recording
original cluster/schema/actor/IDs before acknowledged writes. SaveHolon acknowledgement ownership is fixed and unit-tested: staged payload/ID/keys
publish only after native write completion, IsSaved/SavedCount count acknowledgements,
and batches stop or continue as requested. Failed writes preserve caller identity.
HolonManager retains tracking acceptance ownership. Verify native3/filter2/missing0/page1,
JSON/write-time preservation across an actual restart, then one Mongo-paired
C#/HTTP/browser5/4 preset and hub registration. Do not mark native acceptance green
from these no-backend controls. No native source/runtime/container exists yet.

TLS configuration, arbitrary predicates, graph/children, conditional writes,
replication/history/migration and EF/NHibernate parity remain unverified/open.
Shared HyperDrive or other provider proof does not certify this adapter.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
