# LanceDB CQL MVP — bounded remote implementation, native pending

30October2026. Inventory stays red; no native source/account/environment created.

Existing provider explicitly targets LanceDB Cloud REST, not embedded OSS.
Its original implementation sent JSON row envelopes to create/merge_insert and
parsed query responses as JSON arrays. Merge writes are now corrected below;
explicit table admission and query Arrow decoding are also corrected below. The [official remote client source](https://github.com/lancedb/lancedb/blob/4d7a64712f7fbe39bffaf110f3eb790943bd7874/rust/lancedb/src/remote/table.rs)
uses Arrow IPC upload/response contracts. Current provider wire behavior therefore
needs protocol correction and proving tests before any native claim. Do not create
a custom JSON server and label it native LanceDB, or silently switch Cloud to OSS.
The [Enterprise guide](https://docs.lancedb.com/enterprise/quickstart) distinguishes
remote service endpoints from OSS storage-path access.

Additional observed root gaps:
- Original EnsureTableAsync ignored HTTP errors and wrote a `__schema__` sentinel;
  activation is now read-only and schema-validated below. Existing sentinels need
  deliberate migration; none are deleted automatically.
- Holon save identity/batch ownership is corrected below; other write paths still need review.
- Original query errors became empty lists and all-documents used placeholder-vector
  similarity/topK1000. Shared Arrow reader/bounded overflow checks now replace that
  behavior; unrestricted generic listings are still limited to1000 rows.
- Existing typed lookup literals are now escaped and unsupported lookup columns
  reject; capability/response validation is implemented below. Broader predicates
  and complete generic query/graph semantics remain open.
- Runtime holon serialization, acknowledged save ownership, explicit table schemas,
  history/children/concurrency/replication/migration remain unverified.

Next: real admitted free remote storage verification and Mongo-paired demo.
Preserve the explicitly selected Cloud transport; do not substitute embedded OSS.
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

## Explicit native table admission

Activation now POSTs `/v1/table/{table}/describe/` with `{"version":null}` for
`oasis_avatars`, `oasis_avatar_details`, `oasis_holons`; all must be explicitly
provisioned. No create requests, schema seed rows or replacement-table fallback.
Fresh activation clears active state before validation and preserves exceptions.
Require HTTP200/application-json, positive UInt64 version, schema<=256KiB and a
15second deadline per table. These describe reads select current native heads;
no snapshot or concurrency ownership is inferred from the returned versions.

Canonical field order: id,doc,username,email,parent_holon_id as nonnullable string;
vector as nonnullable fixed_size_list(length1), with nullable item:float child,
matching the actual emitted Apache Arrow schema. Native JsonSchema's `string`,
`float`, nested `fields` and `length` representation is validated, rather than
using the JSON envelope the previous producer guessed. Incompatible primitive
nesting, names/order/nullability/counts/vector lengths/types fail admission.

[Pinned remote describe contract](https://github.com/lancedb/lancedb/blob/4d7a64712f7fbe39bffaf110f3eb790943bd7874/rust/lancedb/src/remote/table.rs)
and [official Lance Arrow JSON representation](https://github.com/lancedb/lance/blob/main/rust/lance/src/arrow/json.rs)
provide the wire types. No attempt to mutate a pre-existing incompatible schema;
provision/migrate explicitly before activation. No local storage was created.

**35passed/0failed/0skipped**, including16 new schema/activation tests and prior19
save controls. Wire codec tests also verify item child name/nullability agree.
[Admission tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests/LanceDbSchemaTests.cs).
Evidence `C:/Source/OASIS-cosmic/TestResults/LanceDbCqlCurrent/schema-controls.trx`,
SHA25648C9A5EE3937A747A9CCBF8BBF02F730A1BD7A3DCC1F42938D5D748D3991E0B9.
Bounded leased builds/tests pass, no restore/download required. Real remote native,
query/CQL and Mongo-paired/browser proof remain open; inventory stays red.

## Bounded Arrow reads and CQL

**67passed/0failed/0skipped**: prior35 save/table tests,9 compiler/adapter cases,
23 Arrow query/generic-lookup cases. Negotiated native query response is
`application/vnd.apache.arrow.file`; JSON, stream/unknown media types and incomplete
file framing reject. One shared reader serves CQL and existing typed lookups.
Native HTTP errors are surfaced, not returned as empty lists. No vector similarity
is injected into document queries. Explicit id/doc projection, prefilter=true,
offset0 and k=maximumRows+1 detect overflow instead of silently truncating.
Typed username/email/parent lookup columns are whitelisted and string literals
escaped; generic listings explicitly fail beyond their1000-row adapter budget.

Arrow reader checks every record batch: id/doc UTF8 projection, matching lengths,
no nulls, canonical unique nonempty GUIDs, document<=1MiB, response<=4MiB encoded,
row bounds and linked15second deadline. Empty projections are validated too.
Existing holon loads verify native key versus deserialized payload ID. These are
encoded/row/document budgets, not a universal expanded-heap bound for arbitrary
malicious IPC/compression metadata; broader codec resource hardening remains open.

CQL binds the shared bounded-identity algebra to exact LanceDB authority and a
trusted complete catalogue<=100GUIDs. Captured identity Boolean/membership selection
becomes a single native SQL ID predicate; stable admitted-key paging happens after
missing/deleted omission. Native foreign/payload-mismatched rows fail the whole
result. Unsupported names/metadata/order/graphs reject before native execution;
empty catalogue causes no I/O. No history/general similarity/distributed snapshot
capability or native certification is implied.

[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests/LanceDbCqlTests.cs)
and [actual Arrow-file tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests/LanceDbNativeQueryTests.cs)
cover multi-batch ordering, native overflow, duplicate/foreign IDs, payload mismatch,
projection/type/null errors, bad HTTP/media, truncated files, byte/document/result
budgets, cancellation, deleted/missing rows and typed-lookup SQL quoting/error paths.
Evidence `C:/Source/OASIS-cosmic/TestResults/LanceDbCqlCurrent/cql-controls.trx`,
SHA256ADEF7E2B9B90B2A22BE905AB47A726F270664BF21A4AE2F099D664D9B37439BC.
Cache-only restores/bounded leased builds pass. Initial test compilation exposed an
Arrow/System.Array name ambiguity, corrected explicitly; no runtime workaround.

Native/free remote account configuration remains unadmitted. No hosted calls,
local substitute, mock certification or new environment allocated. Inventory remains
red; continue QuestDB breadth while retaining this native/pair/browser acceptance.
