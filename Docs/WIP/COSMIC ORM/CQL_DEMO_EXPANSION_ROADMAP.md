# Eight-provider CQL demo expansion

Updated 27 October 2026. User priority: broader demo before full EF/NHibernate parity.
The original full roadmap remains active; defer depth the demo does not need.

## Priority and recommendations

| Order | Provider | Evidence and next work |
|---|---|---|
| Baseline | MongoDB, SQLite, Neo4j | Existing 12-row demo set is green. Use the proven common query subset; unfinished Q01f parity must not delay expansion. |
| 1: cloud | **Azure Cosmos DB for NoSQL** | Recommended first. Matrix records 52/52 storage checks with no blocker in that suite. Existing SDK/document owner and native SQL query language offer a direct translation path. Canonical/nested string/Boolean metadata rows, Count/LongCount/Any and projection now pass 47 native cases plus5 shared HolonManager/LINQ demo operations and46 focused tests; WEB4/native HTTP row/scalar/projection now pass with controlled identity/receipt storage;405 tariff/WEB4 tests pass. Full common-demo and hosted-cloud/production auth-billing acceptance remain open. See [native admission report](CQL_COSMOS_NATIVE_ADMISSION.md). |
| Alternatives | Firestore, AWS DynamoDB, Oracle | Firestore 52/52 is retained-emulator evidence, hosted acceptance pending. AWS/Dynamo also has historical runtime/environment caveats. Oracle 52/52 proves database storage, not OCI hosting. Reconsider if Cosmos access or schema creates a larger blocker; do not implement every cloud first. |
| 2: chain | **Solana** | First candidate, as requested. Existing SDK/signing/finalization work and retained validator are useful. Latest breadth matrix records 4/52, with an unavailable referenced memo chunk during collection decoding. Repair/verify current-record reads without replaying transactions, then explicitly admit native/indexed query execution. [Current source/admission audit](CQL_SOLANA_DEMO_ADMISSION.md) explains the memo/history boundary. |
| 3 | **IPFS** | Latest matrix records 0/52 blocked before native: original retained repository/configuration and storage key are missing. Locate/recover the existing resources rather than silently replacing them; then prove CID-backed records and declared querying. |
| 4 | **SOLID** | Existing HTTP/auth/encrypted CRUD/history/conditional/graph work is a useful foundation. Current shared suite is configuration-blocked; hosted authentication/access acceptance remains open. Reuse the retained Pod/auth environment and prove actor-bound read/write/query visibility. |
| 5 | **ActivityPub** | Published native Social/Network capability-v1 baseline5/5 and291 controlled regressions; this is not storage/CQL conformance. Existing Mastodon status projection offers an exact native-key query path. See [protocol admission](CQL_PROTOCOL_DEMO_ADMISSION.md); implement while earlier retained environments are unavailable. No federation/posting. |

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
- Active next: [IPFS admission](CQL_IPFS_DEMO_ADMISSION.md), then SOLID/ActivityPub. User reports no known Solana backup and requests other demo work. Preserve the failed source and defer recovery; [chain query contracts](CQL_CHAIN_QUERY_CONTRACT.md) now cover identity/checkpoint/receipts/lifetime and explicit composition/planning, but indexed execution/ingestion/WEB4 remain open. Continue IPFS, SOLID and ActivityPub in order; retain cloud numeric/lifecycle/hosted gaps on the full roadmap.
- Keep the full parity checklist strict and update CURRENT_STATUS each milestone. Unfinished Q01f depth remains deferred, not complete.
