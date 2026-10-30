# Qdrant CQL MVP — controls verified; native/paired demo pending

Updated 30 October 2026. Inventory remains red. No native-server acceptance yet.

## Implemented scope

QdrantOASIS implements ICosmicQueryStorageProvider bound to the shared bounded
identity compiler. Trusted complete catalogue at most100GUIDs; identity equality,
membership/Boolean composition and stable paging. Other predicates/metadata/order/
child graphs reject before IO. No scroll/search/full-store filtering fallback.

One POST /collections/oasis_holons/points retrieves admitted IDs with payload and
without vectors. Native timeout15s/linked cancellation, streamed response budget4MiB
before JSON parsing, strict HTTP/status/result/identity/payload/authority validation.
Missing records may be omitted by native retrieval; foreign/duplicate/corrupt rows
fail without partial results. Stable catalogue order precedes paging; complete-page
budget failure returns no partial rows. Read consistency remains moving native reads,
not a distributed snapshot. Native protocol/collection compatibility is still pending.

SaveHolon stages ID/copied provider keys/JSON until native PUT?wait=true returns
status=ok plus result.status=completed and an exact UInt64 operation_id. Zero is valid;
the value is returned as decimal string metadata Qdrant.OperationId. Queued-only,
malformed or error acknowledgements preserve caller identity/keys and report failure.
An operation ID is a write receipt, not a revision or conditional-write token.
An unknown write outcome does not prove no native write occurred; no automatic replay.
Batch stop/continue/error and acknowledged counts are honored. HolonManager owns
change-tracking acceptance. Shared point upsert checks the completed acknowledgement
for existing avatar/delete callers as well; their broader behavior remains unaudited.

Activation clears stale active state first. Only a confirmed native404 may create a
collection; HTTP/JSON errors or vector-schema mismatch fail rather than recreate.
Creation requires status=ok/result=true; existing collections require configured
unnamed vector size and Cosine distance. Activation publishes only after all three
collections pass. Deactivation clears state. Native PUT/provisioning envelopes are
bounded64KiB; owned request/response envelopes are disposed. A caller-owned HttpClient
overload supports controlled transport/TLS without changing the original constructor.

The unimplemented history flag rejects Holon saves/CQL before native IO. Existing
zero-vector placeholders are unchanged. This is payload retrieval, not embedding
generation or semantic/vector similarity search; description no longer claims that.

## Proving controls

38passed/0failed/0skipped: compiler/admission9, saveownership6, nativeHTTP23.
Provider/unit builds and cache-only restores pass. Tests cover authority capture,
unsupported shape/budgets, inactive/pre-cancelled queries, deactivation, staged payload
equality, failed new/existing identity and batch counts. NativeHTTP controls assert
actual endpoint/JSON key IDs/with_payload/with_vector/wait flags; native ordering,
missing/deleted rows, foreign/duplicate/payload identity corruption, HTTP/status
errors, wire/result budgets, active cancellation, queued/incomplete/completed
acknowledgements, zero/max UInt64 precision, unsupported history and collection
404/create-ack/schema/error/active-state behavior. No backend used by controls.

[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QdrantOASIS.UnitTests/QdrantCqlTests.cs),
[save tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QdrantOASIS.UnitTests/QdrantSaveOwnershipTests.cs),
[native boundary tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QdrantOASIS.UnitTests/QdrantNativeBoundaryTests.cs).
Raw TestResults/QdrantCqlCurrent/controls.trx SHA256
`f433240bbc5373c4ab9f688aea9db9b4c383fdcb9781c0bbeb078d992e794118`.

## Exact next step and remaining gaps

No cached native Qdrant image found. Select one official free release; journal one
authenticated local environment/source and preserve native node/collection identity
before business writes. Verify schema/protocol/operation receipts, native3/filter2/
missing0/page1/budget and original JSON/native point state across actual restart.
Then Mongo-paired C#/HTTP/browser5/4 and hub tests. Never replay unknown writes,
reseed source IDs or replace a baseline. Keep inventory red until complete proof.

Atomic conditional writes, history, children, migration, replication and unknown-write
recovery remain open. Older generic GET/POST/lookup/search/delete error/size semantics
require a separate native audit; new CQL does not silently reuse them. No hosted,
similarity, distributed snapshot or full EF/NHibernate parity claim.
[Native retrieval](https://api.qdrant.tech/api-reference/points/get-points),
[native upsert/wait contract](https://api.qdrant.tech/api-reference/points/upsert-points).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
