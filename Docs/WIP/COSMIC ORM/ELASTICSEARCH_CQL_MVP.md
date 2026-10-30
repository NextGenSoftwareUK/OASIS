# Elasticsearch CQL MVP — controls pass; native demo pending

Updated 30 October 2026. Inventory remains red until native/restart/paired acceptance.

## Implemented

ElasticsearchOASIS explicitly implements ICosmicQueryStorageProvider, bound to
its own authority through the shared bounded identity compiler. Complete trusted
catalogue at most100GUIDs, identity equality/membership/Boolean composition and
paging. One real-time native POST /oasis-holons/_mget; no index/search scan, arbitrary
content predicate or OpenSearch certification. Restore catalogue ordering before
paging, retain read provenance and reject unsupported query shapes.

HTTP transport/cancellation and4MiB response budget are enforced while streaming.
Every admitted ID must be accounted for once; foreign/duplicate IDs, index changes,
per-item errors, missing revisions and payload-ID/read-authority mismatch reject.
Missing documents are empty reads; no partial result on errors/cancellation/budgets.
Read authority is the supplied authoritative record catalogue: a creator mismatch
alone does not revoke an explicitly authorized ID. No distributed snapshot claim.

SaveHolon copies/stages ID/provider keys/payload and validates native index/ID,
version/sequence/primary term/result and successful/nonfailed shard acknowledgement
before publishing caller ID/key/IsSaved/SavedCount. Errors preserve caller state.
Batch stop/continue counts acknowledged writes only; HolonManager owns tracking
acceptance. Activation/deactivation now maintain active state correctly.

Original constructor retained. Additional (HttpClient, host, credentials...) overload
accepts a dedicated caller-owned HTTP client/TLS policy; no certificate bypass or
client disposal by the provider. Avoids ambiguity with existing host/null calls.

## Proving controls

32passed/0failed/0skipped: metadata4, shared compiler/admission/lifecycle9 and
native HTTP boundary19. Provider/unit builds and cache-only restores pass.
Proving [HTTP tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.UnitTests/ElasticsearchNativeBoundaryTests.cs)
and [query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.UnitTests/ElasticsearchCqlTests.cs).
Tests inject native responses, not a real Elasticsearch backend. Wrong IDs/index,
shard failure/malformed acknowledgement, incomplete acknowledgement, missing records,
ordering/page/budget/cancellation and batch stop/continue are exercised.
Stale metadata tests corrected to EnumValue.Value; nonexistent GetProviderVersion
assertion removed instead of adding a fake production method.

Evidence TestResults/ElasticsearchCqlCurrent/controls.trx SHA256 `1d8580ed64703d22a68e2714519ed3d5e2d56a541e86760c6c08eecc386d5537`.
Raw output stays in TestResults; no raw committed evidence or package downloads.

## Next

One official Elasticsearch9.5.5 Linuxamd64 runtime is cached by pinned digest:
`sha256:c40a1fb2bc6976821e5625d2329992a1e9c29df576e5955fdcba02421abf0d3c`.
Image ID952a43cd5ab721f7032b2dfd4dcc86a76ba9a791b4c34a223292cf26a832e040,
expanded1,631,515,940B; reuse it. No native container/source/credentials yet.
Next create one retained native source with authentication/TLS and free basic
license explicitly selected (no trial/hosted signup). Record original cluster/node/
index/actor/IDs before writes, prove native3/filter2/missing0/page1/budget and
JSON/native-revision preservation across actual restart. Then one Mongo-paired
C#/HTTP/browser5/4 preset and hub routing. No index alias substitution/reseeding.
[Official Docker installation](https://www.elastic.co/docs/deploy-manage/deploy/self-managed/install-elasticsearch-with-docker).

Arbitrary metadata/full-text query translation, child graphs, native CAS, history,
replication/migrations/unknown write recovery and full EF/NHibernate parity remain
open. Avatar save ownership and older unrestricted load/search APIs are outside
this scoped admission. Legacy HyperDrive issues documentation only.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
