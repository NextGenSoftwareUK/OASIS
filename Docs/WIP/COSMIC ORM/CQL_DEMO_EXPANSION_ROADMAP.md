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

43 identities now have linked scoped MVP evidence; see the generated inventory. Full production-provider parity remains open.
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
2. Base and Avalanche MVPs are verified. Avalanche additionally needed an actual
   factory/encryption chain-identity fix and verified empty-original continuation;
   [proof](AVALANCHE_CQL_MVP.md). Azure Blob now also has native/restart and paired C#/HTTP/browser proof.
   S3/TRON adapters and controls exist but native admission remains open. LocalFile
   native/restart/paired proof is verified. Aptos direct paired proof passes (hub review-gated); Sui full local paired/browser
   and hub proof passes. Hedera native fee/funding admission is pending; Redis encrypted native/restart/paired
   and hub proof passes. PostgreSQL native/restart/paired/browser and hub proof passes; continue remaining red provider MVPs.
3. Continue missing registered storage MVPs, then unregistered/source-only entries.
   Track actual implementation, registration and external blockers independently.
4. After breadth, return to the strict [parity checklist](CQL_PARITY_CHECKLIST.md)
   and deferred ORM/HyperDrive gates; do not change their acceptance definitions.

[Previous expansion history](Archive/CQL_DEMO_EXPANSION_ROADMAP_PRE_ALL_PROVIDER_2026-10-29.md)
was archived with its superseded next steps intact. Update current status at each milestone.

PostgreSQL breadth milestone: control8/native1/restart1/paired1 and browser5/4 pass;
[scope and retained evidence](POSTGRESQL_CQL_MVP.md). Full parity remains open.

SQL Server breadth milestone: control9/native/restart/paired and browser5/4 pass;
[scope and evidence](SQLSERVER_CQL_MVP.md). Wider parity stays deferred.

DuckDB breadth milestone: control14/native/reopen/paired and browser5/4 pass;
[scope and evidence](DUCKDB_CQL_MVP.md). Wider parity remains deferred.

Upstash hosted breadth milestone: control17/native/repeat/paired/browser5/4 pass;
[scope and expiry](UPSTASH_CQL_MVP.md). Existing free scratch expires October31.

Litestream Linux breadth milestone: native/live file replication/restore/paired and
browser5/4 pass; [scope/resources](LITESTREAM_CQL_MVP.md). Windows fsync stays red.

Memcached breadth milestone: native volatile generations/paired/browser5/4/CAS pass;
[scope/lifecycle](MEMCACHED_CQL_MVP.md). Wider atomicity/durability remain open.

PocketBase native/restart/paired/browser/hub scope is verified: [proof](POCKETBASE_CQL_MVP.md). Next reconcile fetched Development before Supabase resource/storage inspection.

Supabase native REST/PostgreSQL/JWT/restart/paired/browser/hub verified in its [local data-plane scope](SUPABASE_CQL_MVP.md); hosted gateway/Auth separate. Next Firebase Realtime Database source/query/runtime audit.

Firebase RTDB native/export-restore/paired/browser/hub scope verified: [proof](FIREBASE_CQL_MVP.md). Next Optimism authority/native-source admission, preserving EVM source/keys and registration gaps.

Optimism V2 local encrypted native/restart/paired/browser/hub scope verified: [proof](OPTIMISM_CQL_MVP.md). Next Rootstock authority/chain/native audit; retain public-network/registration distinctions.

Rootstock shipping-factory local native/restart/paired/browser/hub scope verified: [proof](ROOTSTOCK_CQL_MVP.md). Next BNB Chain authority/chain/native audit on reused EVM resources.

BNB V2 local native/restart/paired/browser/hub scope verified: [proof](BNB_CHAIN_CQL_MVP.md). Next Fantom authority/chain/native audit; keep public-network/registration distinctions.

Fantom V2 local native/restart/paired/browser/hub scope verified: [proof](FANTOM_CQL_MVP.md). Next Scroll authority/chain/native audit, reusing retained EVM resources.

Linea and Scroll local native/restart/paired/browser/hub MVP verified; [Linea](LINEA_CQL_MVP.md), [Scroll](SCROLL_CQL_MVP.md). Next zkSync runtime compatibility/chain/query audit before choosing native environment.

Monad local native/restart/paired/browser/hub MVP verified: [proof](MONAD_CQL_MVP.md). Next Abstract runtime/chain/query audit; zkSync original checkpoint retained/native reload gate stays red.

30 October: CouchDB native/restart/Mongo-paired C#/HTTP/browser5/4 and hub pass.
All226 provider identities remain in MVP breadth scope;47 linked scoped proofs.
SurrealDB native/restart/paired/browser5/4 now pass. ArangoDB native/restart/paired/browser5/4 now pass. RavenDB secured native/restart/paired/browser5/4 now pass. Next red provider: CockroachDB. Dgraph has no exact current source/identity. Deep parity remains deferred. [CouchDB scope](COUCHDB_CQL_MVP.md).
