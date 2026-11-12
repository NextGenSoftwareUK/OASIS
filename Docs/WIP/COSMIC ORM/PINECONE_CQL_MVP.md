# Pinecone CQL MVP — in progress

30 October2026. Inventory stays red; injected controls do not prove a native demo.

## Verified save ownership

17tests pass/0failed/0skipped. Native vector-upsert HTTP200 must acknowledge exactly
one `upsertedCount` before publishing caller ID/provider key/IsSaved/SavedCount.
Derived holon fields and exact decimal JSON are staged using runtime serialization;
failed/disconnected writes preserve existing identity/keys and never replay.
HolonManager retains tracking acceptance ownership. Batches expose errors, count
acknowledged writes and honour stop/continue. History rejects before allocation/I/O.
Metadata JSON is limited to40KiB before I/O; acknowledgement64KiB/15second deadline.
The native acknowledgement accepts the write; eventual visibility is separate and
no commit/CAS/read-after-write guarantee is claimed. API version header2026-07 is
explicit, matching the official vector-upsert contract.

[Tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests/PineconeSaveOwnershipTests.cs).
Evidence `C:/Source/OASIS-cosmic/TestResults/PineconeCqlCurrent/save-controls.trx`,
SHA256 AE0365FACF105FBEC5A1E4B339EBD2C2EB3164E1FF787484A002E1BF9D41324A.
Cache-only restore/bounded leased build pass, no runtime/package download.

## Native admission

[Pinecone's current local-testing guide](https://docs.pinecone.io/guides/production/mock-the-api)
says Pinecone Local reached end of life7October2026. Its replacement is a canned
OpenAPI mock: it does not store records or prove write/read/search behavior.
Do not install that mock and count it as a native demo or replay business data into
retired ephemeral sources. No PINECONE-named environment variables found (names
only); retained configuration/account discovery remains pending. No account,
credentials, hosted writes, download, paid resource or new container created.
User free-only restriction remains; hosted native proof needs an admitted free
account/index. Preserve the API/mock/native distinction.

## Next

Discover an admitted free hosted endpoint/index configuration; retain native and
Mongo-paired C#/HTTP/browser gates while unavailable. No matching directory found
at this worktree's LocalState/COSMIC top level; that is not proof of global absence.
Continue other red providers, beginning with LanceDB's actual Arrow transport.
Do not substitute a canned mock for native data acceptance.

## Remaining gaps

Generic avatar write ownership/runtime serialization, history, children, conditional
writes, metadata query semantics, hosted native index/query behavior and
vector-similarity behavior remain unverified. Placeholder vector is1dim; native
activation now requires its compatible dense index and metric. Whole-record JSON
lives in `_doc` metadata, so40KiB native metadata limits constrain holon size.
The count acknowledgement has no per-record revision/token, and storage visibility
is eventual. No full EF/NHibernate parity claim.

[Current status](CURRENT_STATUS.md) / [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Index admission and bounded CQL implementation

**58passed/0failed/0skipped**: prior17save controls,14index controls,9compiler/adapter
cases and18native-response cases. Bounded leased builds/cache-only restores pass.
Schema-only checkpoint31tests also passed. No real hosted calls or native fixture.

Activation uses the official POST `/describe_index_stats` with `{}` and version
header2026-07 (previous implementation incorrectly used GET). Require dimension1,
vectorType dense, metric cosine/dotproduct/euclidean, bounded256KiB/15second native
response, valid nonnegative Int64 counts, namespace map and finite0..1fullness.
Failed reactivation clears active state. Never creates/replaces storage.
Unit placeholder `[1.0]` replaces invalid zero cosine inputs on future writes/
legacy similarity calls; existing stored records are not rewritten. Native stats
are eventually updated; aggregate counters are not treated as transactional state.

Shared bounded-key compiler is explicitly bound to Pinecone authority/catalogue
<=100GUIDs. Identity equality/membership/Boolean selection and stable paging only;
other shapes reject. One GET `/vectors/fetch?namespace=holons&ids=...&ids=...` over
admitted keys; no similarity search or scan. Validate exact namespace, map keys,
vector IDs, metadata IDs and JSON holon IDs; reject duplicate/foreign/malformed
rows. Require one finite dense coordinate, metadata<=40KiB, total response<=4MiB,
linked15second deadline and final result budget before publication. Missing/deleted
rows are omitted; cancellation/errors publish no partial data. Empty catalogue
causes no native fetch. Read authority remains the trusted explicit catalogue.
No freshness/distributed snapshot/history capability is advertised.

[Official2026-07 data-plane specification](https://github.com/pinecone-io/pinecone-api/blob/main/2026-07/db_data_2026-07.oas.yaml)
defines the POST stats, fetch map/namespace and upsert acknowledgement fields.
[Schema tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests/PineconeSchemaTests.cs),
[compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests/PineconeCqlTests.cs),
[response tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests/PineconeNativeQueryTests.cs).
Evidence `C:/Source/OASIS-cosmic/TestResults/PineconeCqlCurrent/cql-controls.trx`,
SHA2562E6A8FE829010D18BC320D57BA9BE176B2C4DC946B1ED82D0B70A0FD5153AF0E.
Schema TRX SHA256386ADAAECE0AE46ED933E3CBBDBFB04C3A9921865FABB3FFAD0C8D3D925819DC.
Native/Mongo-paired/browser acceptance stays red; inventory remains62/226.
