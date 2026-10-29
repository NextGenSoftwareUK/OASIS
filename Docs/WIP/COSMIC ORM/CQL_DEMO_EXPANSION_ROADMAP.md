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

Including Base, 18 identities now have scoped MVP evidence. Full production-provider parity remains open.
See [CURRENT_STATUS](CURRENT_STATUS.md) and individual guides for exact counts,
reused state, constraints and remaining gates. NEAR's historical interrupted writes
remain unresolved; its demo is read-only. No universal uptime, mainnet, hosted-cloud,
complete child/history/replication coverage or EF/NHibernate parity is claimed.

## Exact next steps

Development `87df54630` is now integrated with merged Core `c2b1627` and matching
pins. Core4528/Shared1599/WEB4schema+admission5 pass; WEB4 builds. The two reproduced
sharing input compile errors are fixed. [Verification](DEVELOPMENT_SYNC_2026-10-29.md).

1. The refreshed inventory has 226 concrete identities, 33 active central cases and
   11 additional project names requiring classification. These are source facts.
2. Base MVP is verified: controls7/family21, native3/2 and restart reuse, paired
   C#/HTTP1/1/browser5/4. [Scope](BASE_CQL_MVP.md). Avalanche is next: its active
   registration selects Web3Core, but CQL authority admission remains missing.
3. Continue missing registered storage MVPs, then unregistered/source-only entries.
   Track actual implementation, registration and external blockers independently.
4. After breadth, return to the strict [parity checklist](CQL_PARITY_CHECKLIST.md)
   and deferred ORM/HyperDrive gates; do not change their acceptance definitions.

[Previous expansion history](Archive/CQL_DEMO_EXPANSION_ROADMAP_PRE_ALL_PROVIDER_2026-10-29.md)
was archived with its superseded next steps intact. Update current status at each milestone.
