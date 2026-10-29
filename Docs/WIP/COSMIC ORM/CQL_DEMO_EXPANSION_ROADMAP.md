# Priority-provider CQL demo expansion


## Latest MVP breadth-first priority — 29 October

Finish AWS first, then Ethereum, Polygon, Arbitrum, Google, Cosmos blockchain,
Polkadot and NEAR, in that order. Cosmos blockchain is tracked separately from
Azure Cosmos DB; Google means the suitable existing Google provider after inspection.
Then add other popular providers where an existing implementation can support a real MVP.

For each provider: one small native filtered query over several records, one
Mongo-paired federated preset, readable C# examples, HTTP/browser integration and
focused proving tests. Clearly expose supported filters and consistency, source/work
bounds, test-network/emulator/hosted identity, and unsupported operations. Native
indexed key-set selection is an acceptable explicitly labelled MVP; a full-store
managed scan/filter or silently redirected query is not. Prove the smallest useful
complete demo, then move on; defer advanced operators, parity depth and deeper
migration/cutover work. Reuse environments and free test infrastructure; do not
provision paid services. Hosted Azure remains pending by the user's instruction.

Existing SQLite/Mongo/Neo/Oracle/IPFS/SOLID/ActivityPub/Solana and Cosmos-emulator
proofs remain scoped to their recorded tests. Do not present the new queue as
implemented, or local/free test services as hosted production acceptance.

## Earlier priority and retained scope — 28 October2026

HyperDrive V2 correctness comes first; then ORM/CQL acceptance for MongoDB,
SQLite, Neo4j, IPFS, Azure Cosmos, AWS, Oracle, SOLID and ActivityPub. Preserve
Solana's existing demo as well. The older eight-provider selection below is
historical; AWS and Oracle are now required scope, not optional alternatives.

AWS local query-demo MVP is verified: official DynamoDB transport, bounded native
ID selection, controlled tests13/13, native DynamoDB Local1/1 and Mongo-paired
C#/HTTP/browser5/4 rows. Hub command: `-Provider AWS -Federated`. This is a native
local-service proof, not hosted AWS, arbitrary content queries or full parity.
[Scope, source and evidence](AWS_CQL_MVP.md). Ethereum controls12/12 and native local-chain query3/2 acceptance1/1 pass, with original-seed restart reuse. Mongo-paired C#/HTTP1/1 and actual browser5/4 are now verified; Polygon is next; [scope and next step](ETHEREUM_CQL_MVP.md).

1. Hosted Azure Cosmos test account, free only. Emulator proof does not satisfy this gate; no paid provisioning/upgrade/fallback.
2. OracleDBOASIS:filtered multi-record native CQL and Mongo-paired preset required. Initial ID-membership/authoritative-catalogue profile implemented;473unit tests pass; native CQL and Mongo+Oracle SDK pairing now pass (5rows/4subset, exact cleanup). HTTP/browser now verified5/4rows with provenance and exact cleanup. [Admission](CQL_ORACLE_DEMO_ADMISSION.md).
3. IPFS/SOLID/ActivityPub:at least a small filtered native collection over several records, with explicit bounded scope and authorization. Existing point demos are insufficient.
4. Actual Solana browser rendering/click verification against retained source/index, without reseeding the original ledger.
5. One ready federated query preset per provider, each paired with Mongo. Show native provenance and capability errors explicitly.

Current evidence28October:protocol native multi-record/HTTP and Mongo-paired browser
presets pass for IPFS/SOLID/ActivityPub; Solana functional browser passes with no source
writes/reseed. Oracle and mixed Mongo+Solana screenshots now inspected/readable;
Mongo+Solana mixed SDK/HTTP/browser2matching/0empty gates pass with original receipts.
Hosted Azure remains open (free-only authenticated setup missing). Mixed contract
and explicit reader dispatch are implemented; [audit and next sequence](CQL_MIXED_FEDERATION_ADMISSION.md).
Below is the27October provider selection baseline, not the current acceptance ledger;
use CURRENT_STATUS.md and linked proving reports for current status.

Entity-selected All/element/numeric parity is paused and preserved on the deferred branch. Full original roadmap remains open.


Updated 27 October 2026. User priority: broader demo before full EF/NHibernate parity.
The original full roadmap remains active; defer depth the demo does not need.

## Priority and recommendations

| Order | Provider | Evidence and next work |
|---|---|---|
| Baseline | MongoDB, SQLite, Neo4j | Existing 12-row demo set is green. Use the proven common query subset; unfinished Q01f parity must not delay expansion. |
| 1: cloud | **Azure Cosmos DB for NoSQL** | Recommended first. Matrix records 52/52 storage checks with no blocker in that suite. Existing SDK/document owner and native SQL query language offer a direct translation path. Canonical/nested string/Boolean metadata rows, Count/LongCount/Any and projection now pass 47 native cases plus5 shared HolonManager/LINQ demo operations and46 focused tests; WEB4/native HTTP row/scalar/projection now pass with controlled identity/receipt storage;405 tariff/WEB4 tests pass. CosmosDB teaching page/hub rows/projection/LongCount and exact-cleanup lifecycle now pass; full common-demo and hosted-cloud/production auth-billing acceptance remain open. See [native admission report](CQL_COSMOS_NATIVE_ADMISSION.md). |
| Alternatives | Firestore, AWS DynamoDB, Oracle | Firestore 52/52 is retained-emulator evidence, hosted acceptance pending. AWS/Dynamo also has historical runtime/environment caveats. Oracle 52/52 proves database storage, not OCI hosting. Reconsider if Cosmos access or schema creates a larger blocker; do not implement every cloud first. |
| 2: chain | **Solana** | First candidate, as requested. Existing SDK/signing/finalization work and retained validator are useful. Latest breadth matrix records 4/52, with an unavailable referenced memo chunk during collection decoding. Repair/verify current-record reads without replaying transactions, then explicitly admit native/indexed query execution. [Current source/admission audit](CQL_SOLANA_DEMO_ADMISSION.md) explains the memo/history boundary. |
| 3 | **IPFS** | Fresh explicitly authorized reusable environment; native storage52/52 and controlled191pass. Current-CID CQL rows pass a native Kubo/HolonManager/LINQ/C# sample gate; exact fixture cleanup complete. General indexed queries, WEB4 and full common-demo acceptance remain open. [Evidence](CQL_IPFS_DEMO_ADMISSION.md). |
| 4 | **SOLID** | Retained container/runtime reused with isolated fresh demo Pod/account/client/key; native discovery/DPoP ACL read passes. Native52/52 now passes after namespace/persistentjournal/deletecount rootfixes;362controlledpass. Original fixtures independently reconciled without replay, exactcleanup complete, containerstopped. Current-record CQL point now passes384controls/22newcases and1nativeprivatePod/HolonManager/LINQ/C#sample gate withdenial/cleanup. Next WEB4/API demo enrollment; richerqueryindex stillopen. Hosted auth/access and full common-demo remain open. |
| 5 | **ActivityPub** | Published native Social/Network capability-v1 baseline5/5 and291 controlled regressions; this is not storage/CQL conformance. Exact native-key CQL rows now pass311 provider controls/1,964 affected regressions and a native manager/LINQ/sample gate. See [profile](CQL_ACTIVITYPUB_NATIVE_PROFILE.md). WEB4 row-route code/native HTTP gate now pass with controlled identity/receipts; interactive enrollment, production deployment and full common-demo queries remain open while earlier retained environments are unavailable. No federation/posting. |

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
