# CQL content-query demo expansion

Approved30October2026. This is the next priority after the running QuestDB native
checkpoint, ahead of adding further ID-only provider demos. Full breadth and ORM
roadmaps remain active; deferred work is not cancelled.

## First10providers

| Provider | Starting point / deployment scope |
|---|---|
| MongoDB | Existing richer CQL; reuse proving tests and work only missing rows. |
| SQLite | Existing richer CQL; reuse proving tests and work only missing rows. |
| Neo4j | Existing richer CQL; reuse proving tests and work only missing rows. |
| PostgreSQL | Name/Description equality+Contains, metadata equality/AND/OR and UUID paging:16controls+17native queries/restart pass. Content paired C#/HTTP/browser4presets2/4/4/2 verified; deployed sandbox remains red. [Proof](POSTGRESQL_CQL_MVP.md#native-and-restart-acceptance). |
| SQL Server | Native content compiler/adapter builds;17 controls pass. Full-length JSON strings and ordinal text semantics specified. Native content/restart/paired/browser/sandbox remain red. [Scope](SQLSERVER_CQL_MVP.md#content-query-candidate-exact-semantics). |
| Oracle | Extend the existing admitted native/Mongo-paired demo. |
| Azure Cosmos DB | Emulator initially; hosted Azure remains pending/free-only. |
| AWS DynamoDB | DynamoDB Local initially; hosted coverage must be proved separately. |
| Elasticsearch | Extend the verified scoped native/Mongo-paired demo. |
| Solr | Extend the verified scoped native/Mongo-paired demo. |

This is10providers total, not10new implementations plus the existing three.
After the first group is verified, extend compatible remaining demo providers.
IPFS/SOLID/ActivityPub/chains need an explicit native-index or bounded-content
query design; they do not inherit arbitrary filtering from a successful ID query.
No promise of identical operators on every backend without proving semantics.

## Common first query set

- Name equality.
- Name and description text matching (`Contains`, plus StartsWith/EndsWith only
  where already supported or explicitly admitted/proven).
- Simple string metadata equality, including a captured metadata key/value.
- AND/OR combinations of the supported name/description/metadata predicates.
- Ordering/paging when admitted, with deterministic results for the demo dataset.
- An explicit Mongo-paired federated preset showing selection and provider provenance.

Use the existing CQL expression model and HolonManager/provider abstraction.
Translate supported predicates to the native provider query/index. Do not silently
fetch an unrestricted collection and filter in memory, or change semantics based
on a failed native query. Native persistence/index representation changes need an
explicit migration/provisioning policy; preserve existing sources and credentials.

## Acceptance and proof

1. Audit the existing [parity checklist](CQL_PARITY_CHECKLIST.md) and linked tests
   first. Reuse green proofs; work only on missing/red features. No unrelated
   hardening of green rows or federation cursors without a reproduced bug.
2. State supported operators/types, case/Unicode/text matching, missing/null keys,
   escaping, metadata nesting and ordering/paging semantics before enabling them.
   Reject unsupported shapes with explicit capability errors.
3. Add targeted compiler/adapter tests, then native cases over several records
   with positive/negative selections, mixed predicates and stable paging.
4. Verify C#/HTTP/browser and [WEB4 sandbox integration](SANDBOX_PROVIDER_DEMO_ROADMAP.md) with clear spacing/comments and provider
   provenance. Preserve existing ID-query presets where useful.
5. Link each completed feature to its proving test/demo in the existing checklist
   and provider report; do not mark an entire provider complete from one query.
6. Keep CURRENT_STATUS current, logs/TRX under worktree TestResults, original data/
   seed/identity receipts outside it, and update the shared cleanup ledger.

## Checkpoint when priority changed

QuestDB native first/restart tests passed1+1 after its65controls. Source retained,
owned service stopped. Mongo-paired/browser preset remains pending and is deferred
behind this newly approved content-query set. Existing overall verified scoped
demo count remains62/226. Pinecone/LanceDB hosted native gates remain open.

The earlier1–3days for a first provider / several more days for a small group was
an unmeasured planning estimate, not a delivery commitment. Establish a more useful
estimate after the initial capability audit and first native implementation.

[Current status](CURRENT_STATUS.md) / [completion roadmap](CQL_COMPLETION_ROADMAP.md).

WEB4 sandbox coverage is required for existing and future provider demos; local
MVP proof remains separate from deployed sandbox proof. Follow the linked sandbox
roadmap for API/UI/runtime configuration, visitor isolation and cleanup acceptance.
