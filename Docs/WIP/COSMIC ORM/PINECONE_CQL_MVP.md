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

Implement capability-aware bounded multi-ID CQL using native fetch; test all
response identities/metadata/namespace/bounds/cancellation before publishing.
Validate explicit index activation (current GET/stale-state behavior needs repair).
Then use real admitted free hosted storage for native and Mongo-paired C#/HTTP/
browser proof; keep red while that evidence is missing.

## Remaining gaps

Generic avatar write ownership/runtime serialization, history, children, conditional
writes, metadata query semantics, native activation schema, query bounds and
vector-similarity behavior remain unverified. Existing placeholder vector is1dim;
index metric/vector type must be admitted rather than guessed. Whole-record JSON
lives in `_doc` metadata, so40KiB native metadata limits constrain holon size.
The count acknowledgement has no per-record revision/token, and storage visibility
is eventual. No full EF/NHibernate parity claim.

[Current status](CURRENT_STATUS.md) / [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
