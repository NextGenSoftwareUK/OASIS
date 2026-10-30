# TimescaleDB CQL MVP — controls verified, native acceptance pending

Updated30October2026. Native/restart/paired/browser acceptance remains red.

## Explicit product binding

TimescaleDBOASIS reuses PostgreSqlFamilyProviderBase/Npgsql CRUD and query execution.
It now explicitly binds CQL to TimescaleDB authority via the existing protected
native query-authority policy. Other unbound aliases remain unsupported; changing
an ordinary PostgreSQL provider enum cannot confer TimescaleDB query capability.
No duplicate CRUD, alternate execution path or PostgreSQL-server certification.

Product compiler binds shared bounded identity algebra: complete trusted catalogue
at most100GUIDs, ID equality/membership, Boolean composition and paging. Shared
parameterized native SELECT/materialization/budgets/cancellation are reused.
Unsupported content/sorting/graph shapes reject. Native TimescaleDB product/wire/
ordering/paging/save semantics must still be proved on a loaded Timescale extension.
Time-series aggregates/hypertables/compression are not implemented by this binding.

## Verified controls

12controls pass:4metadata,7compiler/inactive/pre-cancel and1ordinary-PostgreSQL
identity-tampering guard. UnitTests/TimescaleDbCqlTests.cs is the proving test.
Two stale metadata-test assumptions corrected: provider type is EnumValue.Value,
and nonexistentGetProviderVersion is not a production API. The stale version test
was removed rather than adding a fake version method. No backend used by controls.

Evidence TestResults/TimescaleDbCqlCurrent/controls.trx,12passed/0failed/0skipped,
SHA256 `87b743e5c87701c3b49e60a0244ef9e74eb1ec11f139279c2288c9c9d1667434`.
Bounded provider/test builds and cached restores pass, no new package downloads.

## One pinned official OSS runtime cached

Runtime/image inspection found no reusable TimescaleDB. One official
`timescale/timescaledb:2.31.0-pg17-oss` Linuxamd64 image was pinned/pulled:
`sha256:9192a05d435e360c861cfdb0f4b9f5ae11d33609713163465e46a64fcc577d49`.
Image ID9d6818ce427d460cb5f6750ea5c97ad7d121e0b76c02425c9020fb6a34034198,
expanded1,262,993,877B, compressed layers387,828,285B. Reuse existing rootful WSL
Podman/image; no separate runtime install or native database/container/certs yet.
No hosted signup, paid key/cloud instance or production capability claim.
[Official OSS image](https://hub.docker.com/r/timescale/timescaledb/tags)
/ [official release2.31.0](https://github.com/timescale/timescaledb/releases/tag/2.31.0).

## Exact next step

Inspect the pinned image's PostgreSQL UID/entrypoint, then create one retained
local authenticated/TLS native source. Verify timescaledb is loaded at2.31.0 and
record original PostgreSQL cluster/database IDs before original journalled saves.
Run native3/filter2/page1/hash/xmin checks and actual restart with strict Npgsql
validation, then Mongo-paired C#/HTTP/browser5/4 and hub enrollment. No ordinary
PostgreSQL replacement, capability inference, seed replay or replacement baseline.
Keep private keys/data/acknowledgements outside TestResults; logs/TRX only TestResults.
Inventory stays red until native and paired acceptance pass.

Full CAS, graph/history/metadata depth, Timescale-specific querying, replication/
migration and EF/NHibernate parity remain open. Legacy HyperDrive documentation only.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
