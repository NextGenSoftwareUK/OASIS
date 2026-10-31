> Superseded by ../WEAVIATE_CQL_MVP.md for current integration; retained dated historical gRPC/native proof and limits.

# Weaviate CQL MVP — native, restart and federated demo verified

Updated 30 October 2026. Scoped MVP verified using official Weaviate.Client1.2.0
over gRPC against authenticated TLS Weaviate1.39.8 with GraphQL disabled.

## Why gRPC

GraphQL was deprecated26October2026; new Cloud clusters already disable it. REST
Search is currently preview and not recommended for production. The official C#
client queries over gRPC and requires server1.32+. New CQL does not call GraphQL or
fall back to existing load helpers. Existing GraphQL lookup/search paths remain a
documented migration gap, independently of HyperDrive v1's document-only scope.
[Official migration guide](https://docs.weaviate.io/weaviate/api/graphql/migration),
[C# client requirements](https://docs.weaviate.io/weaviate/client-libraries/csharp).

## Implemented scope

ICosmicQueryStorageProvider binds the shared bounded identity compiler to Weaviate.
Trusted complete catalogue at most100GUIDs; identity equality/membership/Boolean
composition and stable paging. Other predicates/order/metadata/graphs reject before
native reads. FetchObjectsByIDs constrains the native collection OasisHolon, selects
dataJson/isDeleted and requests at most selected-count+1 results. Linked15s deadline
and caller cancellation pass to the SDK. Native UUID/property types/payload identity/
authority/duplicates and cumulative4MiB decoded payload budget are checked before
publishing any results. Failure returns no partial rows; stable order precedes paging.

Construct with a dedicated caller-owned HttpClient and explicitly configured
Weaviate.Client.WeaviateClient for the same authority:
`new WeaviateOASIS(restClient, host, apiKey, queryClient)`.
The original constructor remains available for existing REST use. Missing gRPC
configuration returns CQL_PROVIDER_CONFIGURATION_REQUIRED for nonempty native reads;
no host/port guessing, implicit second connection path or GraphQL fallback. Both
clients remain caller-owned. Native SDK UUID/string/bool mapping and advertised4MiB message configuration pass.
Unary gRPC adversarial response buffering remains unverified; decoded budget alone
does not certify wire-byte or preallocation bounds.

SaveHolon stages ID/copied provider keys/JSON until native single-object REST batch
upsert reports the matching class/ID and per-object SUCCESS without errors. This
removes GET-before-POST/PUT race and checks item errors even under HTTP200. No native
write retry or replay. An unknown write outcome does not prove nothing was stored.
Caller ID/keys and IsSaved/SavedCount publish only after acknowledgement. Batch
stop/continue/counts are honored; HolonManager owns tracking acceptance. Existing
avatar/delete calls share the corrected upsert boundary but need separate full audit.

Activation clears stale state, creates only after confirmed404, verifies native
class/vectorizer/required property types for both creation and existing classes,
then publishes active state. HTTP/schema errors cannot trigger replacement classes.
Deactivation clears state. Provisioning envelopes bounded64KiB; native upsert
envelope bounded4MiB. Owned HTTP envelopes are disposed. The unused history flag
rejects Holon saves/CQL. Vectorizer remains none; no semantic search claim.

## Proving controls

38passed/0failed/0skipped: compiler/admission9, saveownership6, queryboundary8,
nativeHTTP/schema/configuration15. Provider/unit builds and cached unit restore pass.
Tests cover authority capture/unsupported shapes/budgets/inactive/cancelled reads,
stable paging, foreign/duplicate/native/payload identity, terminal error/no partial,
empty/missing/early decoded budget stop, staged/failing identity, history and batch
counts. NativeHTTP asserts one batch upsert without existence probe, class/ID/item
errors/empty/HTTP/oversized acknowledgements,404/schema/vectorizer/create failures
and active-state publication. Missing gRPC config makes no REST/GraphQL request.
NativeIO is injected; SDK live transport/mapping are not certified by these tests.

[Query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.UnitTests/WeaviateNativeQueryBoundaryTests.cs),
[save tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.UnitTests/WeaviateSaveOwnershipTests.cs),
[HTTP/schema tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.UnitTests/WeaviateNativeHttpTests.cs).
Raw TestResults/WeaviateCqlCurrent/controls.trx SHA256
`efa487b35efb76b931f805719ea9c4145990f3f022ef2151fed64bb3448fd227`.

## Native and paired proof / running the demo

One retained TLS/API-key environment: `/var/tmp/oasis-weaviate-cql`, native REST18080,
gRPC15051, official1.39.8 image pinned in the environment helper. No global trust
change. Original node/source inode/schema, three object IDs/JSON/native timestamps
survive actual restart. Initial creates plus one acknowledged update verified.
Native3/filter2/missing0/page1/budget and GraphQL-disabled rejection pass.

Five fresh integration phases pass (1 each, zero failed/skipped): native-first,
native-restart, Mongo initialization, federation and native-postpair. Federation
checks C#/HTTP5/4, provider provenance, excluded data, forged actor/unknown preset,
inactive-provider error without partial results and fixture ownership cleanup.
Browser buttons independently returned5/all and4/subset on port5146. UI stop led
to runner exit0, temporary Mongo fixture removal, native post-pair preservation
and both owned services stopped. Retained source380,339B logical; no reseeding.

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Weaviate -Federated -Serve
# Or the dedicated native runner:
Scripts/TestHosts/Run-CosmicWeaviateLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5146/ once ready; use the stop button to finish verification.
[Native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests/WeaviateCqlNativeTests.cs),
[paired proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests/WeaviateMongoCqlNativeTests.cs).
Raw evidence: `TestResults/WeaviateCqlCurrent/` (regenerable; never committed).
Latest federation TRX SHA256 `da9a39f921968ef1576a0545a5b7f2f41f80e58d97503af8c5ea6d570cd1464a`;
post-pair `da0754476469d15ffaed1bc1d4af98d7605ec0c87a99e97466d9f8cc273db2c7`.

## Remaining gaps

Full generic load/search migration, history/children/replication/migrations/atomic
conditional writes/unknown-write recovery remain open. No hosted, similarity,
distributed snapshot or full EF/NHibernate parity claim.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
