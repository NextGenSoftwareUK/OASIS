# All-provider CQL MVP demo expansion

Updated 29 October 2026. MVP breadth first; deepen afterward. Full programme incomplete.

## Scope and acceptance

- Cover **all provider identities**, including explicit review of source-only projects,
  aliases, shared backends and non-storage roles. [Inventory and queue](CQL_ALL_PROVIDER_MVP_INVENTORY.md).
- Each storage MVP needs a bounded native filtered query over several records, a
  Mongo-paired C# example, HTTP/browser presets, provider provenance, focused tests
  and documented capabilities/consistency/hosting limits.
- Native indexed key-set selection is an explicitly labelled MVP. Full-store managed
  filtering, silently redirected queries and inherited interfaces alone prove no MVP.
- Green denotes only its linked scope. Work on missing proofs/capabilities; stop
  deepening green rows unless a reproducible bug appears. Legacy HyperDrive gaps
  are documented only. Deep LINQ parity, migrations/cutover and POCO work stay deferred.
- Free-only infrastructure; reuse caches/runtime/state and journal new resources.
  Hosted Azure remains pending by explicit user instruction. Do not repeat signup asks.

## Verified initial list

SQLite/MongoDB/Neo4j and Oracle, IPFS, SOLID, ActivityPub, Solana, Azure Cosmos
emulator, AWS DynamoDB Local, Ethereum, Polygon, Arbitrum V2, Google Firestore
emulator, Cosmos blockchain, Polkadot and NEAR have linked scoped proofs.

This is 17 identities with scoped evidence, not 17 production-complete providers.
See [CURRENT_STATUS](CURRENT_STATUS.md) and individual guides for exact counts,
reused state, constraints and remaining gates. NEAR's historical interrupted writes
remain unresolved; its demo is read-only. No universal uptime, mainnet, hosted-cloud,
complete child/history/replication coverage or EF/NHibernate parity is claimed.

## Exact next steps

1. Reconcile fetched Development (`87df54630`) into the task branch, including Core
   and matching deployment manifest/gitlink state; preserve concurrent work. Do not
   sweep unverified task work into Development merely to synchronize.
2. Refresh source inventory after integration. The current snapshot has 226 concrete
   enum identities, 33 active central registration cases and 11 additional project
   names requiring classification. Registration observations are not a runtime audit.
3. BaseOASIS next: active factory path selects the shipped Web3Core implementation;
   its inherited CQL compiler currently rejects Base authority. Add authority tests,
   native selection and the Mongo-paired demo using the retained EVM environment.
4. Continue missing registered storage MVPs, then unregistered/source-only entries.
   Track actual implementation, registration and external blockers independently.
5. After breadth, return to the strict [parity checklist](CQL_PARITY_CHECKLIST.md)
   and deferred ORM/HyperDrive gates; do not change their acceptance definitions.

[Previous expansion history](Archive/CQL_DEMO_EXPANSION_ROADMAP_PRE_ALL_PROVIDER_2026-10-29.md)
was archived with its superseded next steps intact. Update current status at each milestone.
