> Superseded by ../QDRANT_CQL_MVP.md for current integration; retained historical native point/vector proof and limits.

# Qdrant CQL MVP — controls/native/restart/paired/browser verified

Updated 30 October 2026. Green means the bounded identity demo scope only.

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
not a distributed snapshot. Native protocol/collection compatibility passes on the retained server.

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

Continue red providers, next Weaviate capability/native-source audit. Preserve this
acceptance; no green hardening without a reproduced bug.

## Native and Mongo-paired proof

Official free Qdrant1.16.3 runs authenticated HTTPS/private CA on loopback16333,
standalone mode. Native persisted raft peer8294379389961945 was recorded exactly
before business writes. Original source inode/container/image/mount/peer are checked
on every phase. Three original GUID points, payload JSON,128-dimensional placeholder
zero vectors and native collection config hash survive actual restart unchanged.
Native3/filter2/missing0/page1/budget/schema/completed-write receipt checks pass.

Launch `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Qdrant -Federated`
from the task worktree; add `-VerifyOnly` for bounded checks. Browser5145. The C#
example `Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs` uses HolonManager's
actual executor and explicit per-provider read catalogues. HTTP/browser all returns
Qdrant3+Mongo2=5physical result rows; subset2+2=4. Copies retain provider provenance.
Forged actor/unknown preset400 and inactive provider503/no partial rows pass.

Bounded and interactive runners each passed five phases: native-first,
native-restart, mongo-init, mongo-federation and native-postpair. Browser all5/subset4
verified. Hub8single/16plans,54paired/108plans,94pairedrejections,3archive pass,
listeners unchanged. Exact temporary cosmic_cql_mongo_qdrant_demo verified/dropped,
pending/ready cleared; own native/Mongo/demo services stopped, nativeexit0/noOOM/noError.
Original source /var/tmp/oasis-qdrant-cql482,560,525B logical,667,648B allocated (sparse
native files) retained with privateCA/TLSkey/APIkey/raft state/actor3IDs/write receipts/
baseline/collections. No payload/native config/point/vector rewrites during verification.
Raw interactive evidence in TestResults/QdrantCqlCurrent (not committed):
- native-first.trx SHA256 `a8892eb6ab80a155622f24958991e70c2be1849314beaa00acd546f97dce1126`
- native-restart.trx SHA256 `0c041a9094072c298fd6d5f7c28c9c47d35ee5557f7750bc383bd9882386680a`
- mongo-init.trx SHA256 `cbab61c900f2af7490e75bfe2825b8307bb506cfc4b2cd1fd9951dab51e4dda5`
- mongo-federation.trx SHA256 `8bb20f21cbf7009d04a2dd3456565e0a00e7b47db9282b48e67ce7175116f4c4`
- native-postpair.trx SHA256 `256529c33ed947d9c7b618400db6b3d1f397349554ed74babd5a404e58316b0f`
Browser screenshot browser.jpg155,879B, runner44627terminal0. Native source and
credentials stay outside TestResults; raw logs/TRX/images are regenerable.

Atomic conditional writes, history, children, migration, replication and unknown-write
recovery remain open. Older generic GET/POST/lookup/search/delete error/size semantics
require a separate native audit; new CQL does not silently reuse them. No hosted,
similarity, distributed snapshot or full EF/NHibernate parity claim.
[Native retrieval](https://api.qdrant.tech/api-reference/points/get-points),
[native upsert/wait contract](https://api.qdrant.tech/api-reference/points/upsert-points).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
