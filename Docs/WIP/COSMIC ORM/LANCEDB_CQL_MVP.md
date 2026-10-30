# LanceDB CQL MVP — Arrow writes implemented, native pending

30October2026. Inventory stays red; no native source/account/environment created.

Existing provider explicitly targets LanceDB Cloud REST, not embedded OSS.
Its original implementation sent JSON row envelopes to create/merge_insert and
parsed query responses as JSON arrays. Merge writes are now corrected below;
creation and query protocol corrections remain. The [official remote client source](https://github.com/lancedb/lancedb/blob/4d7a64712f7fbe39bffaf110f3eb790943bd7874/rust/lancedb/src/remote/table.rs)
uses Arrow IPC upload/response contracts. Current provider wire behavior therefore
needs protocol correction and proving tests before any native claim. Do not create
a custom JSON server and label it native LanceDB, or silently switch Cloud to OSS.
The [Enterprise guide](https://docs.lancedb.com/enterprise/quickstart) distinguishes
remote service endpoints from OSS storage-path access.

Additional observed root gaps:
- EnsureTableAsync ignores the HTTP response entirely and writes a `__schema__`
  sentinel; activation can report success after a failed native request.
- Holon save identity/batch ownership is corrected below; other write paths still need review.
- Native query errors become empty lists; all-documents uses placeholder-vector
  similarity/topK1000, not a complete document scan.
- Interpolated identity/metadata predicates are unescaped; capability admission,
  response identity/Arrow schema checks and byte/time bounds are absent.
- Runtime holon serialization, acknowledged save ownership, explicit table schemas,
  history/children/concurrency/replication/migration remain unverified.

Next: correct remote table admission and query Arrow decoding with capability-aware
CQL. Preserve the explicitly selected Cloud transport; do not substitute embedded OSS.
Native remote acceptance requires an admitted free endpoint/account. No LANCE-named
environment variable found (names only); broader configuration discovery pending.
Do not claim credentials are absent everywhere or allocate a paid service.

[Current status](CURRENT_STATUS.md) / [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Arrow writes and save ownership — 30 October2026

Remote merge now sends one Apache Arrow IPC stream with Content-Type
`application/vnd.apache.arrow.stream`. Fixed schema: id/doc/username/email/
parent_holon_id nonnullable UTF8, vector nonnullable fixed-size-list(float32,1).
The entire runtime-typed holon is stored as JSON string in doc, preserving derived
properties/exact decimal text. A1MiB document adapter limit is checked before I/O.
Merge URL explicitly selects `on=id`, update-all/insert-all true and
when-not-matched-by-source-delete false. No JSON row envelope or speculative retry.

Require HTTP200/application-json, bounded64KiB/15second acknowledgement, positive
UInt64 commit version, exactly one inserted-or-updated row and zero deleted rows.
Legacy empty/no-version acknowledgements reject; native receipt is not a CAS
promise. The [pinned official MergeResult](https://github.com/lancedb/lancedb/blob/4d7a64712f7fbe39bffaf110f3eb790943bd7874/rust/lancedb/src/table/merge.rs)
and remote client define these fields and merge options. Compatibility with an
actual remote deployment still needs native acceptance.

Holon IDs/provider keys are staged in the wire document and published only after
acknowledgement. Failures preserve caller IDs/key dictionary and retain the error;
batch results expose errors/counts and stop/continue correctly. History rejects
before mutation/I/O. Tracking acceptance remains with HolonManager. Generic row
serialization now retains runtime properties; avatar caller-state ownership is
still a separate gap.

**19passed/0failed/0skipped**; tests decode the actual produced Arrow stream, check
schema/vector/derived JSON/decimal precision and inspect insert/update/native
version/count/deletion errors, transport failures, byte limits, caller keys and
batch ownership. Dedicated injected HTTP client stays caller-owned.
[Tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests/LanceDbSaveOwnershipTests.cs).
Evidence `C:/Source/OASIS-cosmic/TestResults/LanceDbCqlCurrent/save-controls.trx`;
SHA256D84548972A9726916414229EEE6974E6C4A084E9ADC8EF604EC43FB1FC54FBA4.
These are actual Arrow codec/injected-transport controls, not a native server demo.

Pinned Apache.Arrow23.0.0 added once after checking Windows/Linux caches; official
NuGet restore succeeded, transitive Apache.Arrow.Scalars23.0.0 recorded in ledger.
Existing .NET/Core/cache reused; subsequent test restore is cache-only. No native
runtime, cloud account, table, credential or server allocated. Provider/unit builds
bounded/leased and pass. Native/paired/browser/complete CQL remain red;62/226 demo
coverage is unchanged.
