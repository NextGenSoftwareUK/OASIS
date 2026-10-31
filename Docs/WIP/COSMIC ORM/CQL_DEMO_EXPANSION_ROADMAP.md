# Hosted CQL demo expansion

Updated 31 October 2026. User priority: broader hosted demo before full EF/NHibernate parity.
The original full roadmap remains active; defer depth the demo does not need.

## Priority and recommendations

| Order | Provider | Evidence and next work |
|---|---|---|
| Baseline | MongoDB, SQLite, Neo4j | Existing 12-row demo set is green. Use the proven common query subset; unfinished Q01f parity must not delay expansion. |
| 1: cloud | **Azure Cosmos DB for NoSQL** | Hosted Azure explicitly deferred by David: free only, no existing subscription. Existing local/emulator/native CQL proofs remain separate; do not re-ask signup/payment. [Earlier native evidence](CQL_COSMOS_NATIVE_ADMISSION.md). |
| Cloud breadth | **Public AWS S3 metadata** | Hosted native3/2, Mongo5/4, repeat0writes, isolation and exact SQLite/Neo4j copies verified through HTTP. Browser gate open. This is anonymous object HEAD metadata, not owned encrypted S3/DynamoDB acceptance. |
| Deferred hosting | **Oracle Database** | Read-only activation, provider-scoped catalogue/discovery and local native HTTP3/2+Mongo5/4 verified. SDK seeds2private copies; production billing/login/JS REST setup/replica/browser proof remain separate. David has no Oracle account and explicitly deferred hosting. |
| 2: chain | **Solana public account metadata** | Hosted native3/2, Mongo5/4, repeat0writes, isolation and exact SQLite/Neo4j copies verified through HTTP; verbatim C# LINQ sample passes mainnet3/2. Browser pending. Read-only configured accounts; original memo/history recovery and full ORM remain separate. |
| 3 | **IPFS** | Hosted HTTP/browser native3/2, Mongo5/4, private preparation/reuse/isolation and exact SQLite/Neo4j copies verified. Bounded configured CIDs on isolated Kubo; no general search or public-network resilience claim. [Earlier evidence](CQL_IPFS_DEMO_ADMISSION.md). |
| 4 | **SOLID** | Hosted HTTP native3/2, Mongo5/4, private preparation/reuse/isolation and exact SQLite/Neo4j copies verified. Browser gate open. Bounded configured encrypted resources, not general Pod search. |
| 5 | **ActivityPub** | Hosted HTTP/browser native3/2, Mongo5/4, private preparation/reuse/isolation and exact SQLite/Neo4j copies verified. Read-only configured statuses; no posting/authenticated mutation/general account search acceptance inferred. [Earlier profile](CQL_ACTIVITYPUB_NATIVE_PROFILE.md). |

Repository evidence: [provider matrix](../HyperDrive-Providers/PROVIDER_MATRIX.md),
[Solana submission work](../HyperDrive-Providers/SOLANA_SUBMISSION_CONCURRENCY.md),
[SOLID audit](../HyperDrive-Providers/SOLID_PROVIDER_IMPLEMENTATION_AUDIT.md).
Storage conformance is not CQL conformance; emulator/local proof is not hosted-cloud proof.
Refresh published provider evidence before implementation because that workstream can advance separately.

Official references: [Cosmos query overview](https://learn.microsoft.com/en-us/cosmos-db/query/overview),
[Solana account-query RPC](https://solana.com/docs/rpc/http/getprogramaccounts).
Cosmos supports SQL-style selection/filtering/ordering; Solana RPC account filters are narrower.
Neither reference proves the current OASIS adapter already supports CQL.

## Bounded common demo acceptance

1. One readable C# sample and one API demonstration, using synthetic holons and explicit provider scope.
2. Save/load, edit/save an acknowledged revision, inspect its delta, and query the selected backend.
3. Start with proven common shapes: typed/metadata equality and range filters, ordering, bounded paging, simple projection and Count/Any where admitted. Freeze the exact cases first; arbitrary LINQ is not required everywhere.
4. Show provider, execution mode, authoritative record/revision, consistency/finality and any index watermark/lag.
5. Prove the selected backend supplied the data. Do not silently present a Mongo copy as native chain querying.
6. Reject unsupported operators during planning. No hidden full-store download, managed filtering, source substitution or implicit retries.
7. Preserve actor visibility and existing encryption/reserved-metadata policy. Verify the child/depth and conditional-write semantics actually demonstrated; unsupported conditional writes remain explicit.
8. Test success and failure, record hashes/counts in reports, keep raw evidence in TestResults, and clean only exact owned fixtures.
9. A cloud claim needs a hosted endpoint. Label chain local-validator/devnet/mainnet accurately; real-money transactions and new paid infrastructure are not implementation prerequisites.

## Different backends, honest execution

Use native supported query shapes first. A persistent query index may be a deliberately designed,
independently admitted capability for a chain or protocol, with source references and finality/lag
semantics. It must not be an automatic fallback when native translation fails. Design and disclose
that execution mode before enrolling richer shapes.

IPFS deletion must describe the actual catalog/tombstone/unpin behavior, not global erasure of copies.
SOLID preserves Pod ownership/access; ActivityPub preserves actor/activity semantics.
Distinguish replicas from distinct holons and do not promise one snapshot across providers.
HyperDrive automatic failover/replication needs its own verified demo gate; individual CQL support
does not establish global failover or 100% uptime.

## Next actions

- SQLite projection checkpoint is committed; unfinished parity-only depth remains deferred.
- Cosmos canonical and string/Boolean metadata rows are enrolled/native-proven; native Count/LongCount/Any and projection also pass; conditional Save/durable deltas are explicitly unsupported with pre-activation rejection tests; next implement numeric semantics and remaining lifecycle/HTTP/hosted acceptance; shared manager/LINQ row demo and canonical Version/Id index profile now pass.
- Solana declared-range source/index SDK, LINQ, rows/projection/cardinality HTTP and interactive launcher now pass. Original-history recovery remains red; no replacement seed is claimed as recovery. Next: common demo metadata/lifecycle capabilities, ongoing index ingestion, hosted-cloud and production auth/durable settlement; preserve all narrower point-profile limits.
- Keep the full parity checklist strict and update CURRENT_STATUS each milestone. Unfinished Q01f depth remains deferred, not complete.

Environment update: user explicitly authorized creating missing environments.
Fresh persistent IPFS and isolated SOLID demo identities now pass native readiness
using cached runtimes; [commands/evidence](CQL_DEMO_ENVIRONMENTS.md). Active next
IPFS V2 storage baseline/CQL admission, then SOLID; old-data recovery stays open.

Unified demo launcher now selects SQLite coffee or IPFS/SOLID/ActivityPub point hosts. ActivityPub nativeinteractivegate1pass against existingpublicstatus, no posting/authentication. See [commands/evidence/limits](CQL_INTERACTIVE_POINT_DEMO.md). Fourproviders inlauncher doesnotcertify full8provider/commonquerydemo orfullparity.

Live Solana declared-range SDK/index/manager/LINQ and shared C# walkthrough now
pass against the retained validator with new coverage3073; original recovery stays
red. [Native command and limits](CQL_SOLANA_DEMO_ADMISSION.md#live-declared-range-sdk-demo-new-range-not-original-recovery).
Live-source shipping HTTP now passes27requests against the same seed/index and
validator. Interactive hub now adds Solana on5089 with native leave-running/shutdown acceptance. Next: ongoing index ingestion, production authentication/durable billing and common-demo capability gaps.

SolanaMetadata now explicitly selects the separate schema3 acknowledged fixture/index: live SDK/HTTP42requests and read-only/interactive reuse pass, including nested string/exactInt64 filters and pre-reservation private-root rejection. It shares the retained validator and preserves schema2/original history. Next selected metadata and remaining red demo capabilities; production identity/durable settlement remain open. See CQL_INDEXED_METADATA.md.

Selected schema3 metadata is now native SDK/HTTP/demo proven (50requests plus
interactive selection), with no new seed/source writes. Next demo integration:
reuse Run-CosmicCqlV1Demo.ps1 for the existing three-database group, then the retained
Cosmos DB CQL profile/runtime for a cloud teaching host. Current hub lacks these
entries; do not label the Cosmos blockchain runner as the Azure cloud runner.
This integration takes priority over additional parity-only Solana operators.

Databases hub native launch is restored after exact archived fixture reconciliation:681SDK queries/138walkthrough HTTP requests/30save checks pass, with cleanup. CosmosDB teaching-host/hub integration also passes native and interactive HTTP lifecycle; browser rendering and remaining red demo/parity gates follow. See CQL_DATABASE_DEMO_HUB.md and CURRENT_STATUS.md.
