# Durable native route descriptors

Updated28October2026. Persistence/transport milestone;full cold-start/offline recovery and X10 remain incomplete.

## Required invariant

An authoritative graph write must atomically retain the complete original source,destination and authorization-policy meaning of every enrolled obligation. A target ID hash alone cannot reconstruct that meaning after restart. Recovery must not guess a destination from current configuration or rewrite old obligations.

## Implemented

- `HolonReplicationEnrollment.NativeRoutes` freezes1..256 complete native routes,sorted by target ID. They must cover the enrolled target set exactly,with the same bound source and source ID. Caller collection mutation cannot change the frozen set. Existing constructors remain available.
- `CommittedHolonReplicaRequest.NativeRoutes` preserves that immutable set through transport. Source authority/source identity and descriptor meaning must agree.
- `HolonReplicationRouteDescriptors` uses a strict versioned JSON schema of native source/target identities and policy binding. It stores provider names,endpoint fingerprints and native store IDs;no connection strings,passwords or keys. Duplicate/missing/unknown fields,wrong token types,unknown versions and inconsistent route sets are refused.
- Descriptor-bearing committed transport uses envelope Format2 with RouteDescriptors. Existing Format1 emits its original fields without a null/extra descriptor property. New malformed envelopes never downgrade to Format1.
- Primary replication fingerprints use a new descriptor-bearing domain;target receipt fingerprints also bind descriptors. The same operation cannot silently reinterpret older intent as descriptor-bearing intent. Older formats remain unchanged.
- SQLite,Mongo and Neo write descriptors inside the existing digested immutable committed payload,within the same native graph/receipt/outbox transaction. Each target obligation carries the complete selected route set. No table/schema migration,host-local cache or post-commit side write is used.
- Native outbox validation checks that a descriptor-bearing payload actually enrols its row's target ID,before any claim mutation. The shared receiver validates declared target membership,native expected storage and policy meaning. Native receivers refuse descriptor-bearing unbound apply.

## Evidence and exact scope

Core4401/Shared1308pass,including19new codec/transport/fingerprint cases in [HolonReplicationRouteDescriptorTests](../../../Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests/HolonReplicationRouteDescriptorTests.cs).

All3native routed-primary fixtures prove the stored payload contains the complete original route set and native source binding. Second-target failures roll back graph,receipt and all obligations. Target declarations in these primary tests are not real target delivery:

- [SQLite routed primary](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/RoutedOwnedHolonOutboxTests.cs):4routing cases plus4older primary compatibility cases. A native target-row edit outside the descriptor set,without changing payload/digest,is refused by inventory and claim;attempt count stays0. The exact owned edit is restored only after successful assertions.
- [Mongo routed primary](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/CosmicRoutedOwnedOutboxTests.cs):4routing plus4older primary compatibility cases.
- [Neo routed primary](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/Neo4jRoutedOwnedOutboxTests.cs):4routing plus4older primary compatibility cases.

Real shipping-worker Mongo→SQLite/SQLite→Mongo2cases and Neo→SQLite admission/restart5cases pass with the new descriptor-bearing payload. New-format Neo→SQLite abrupt receiver commit/process-exit/replay also passes1native case in2m6s;the original obligation completes after lease expiry without advancing target revision twice.

## Remaining acceptance

| Requirement | State |
|---|---|
| Complete immutable descriptor capture/strict transport | ✅ controlled tests |
| Native atomic descriptor persistence/rollback | ✅ SQLite/Mongo/Neo scoped fixtures |
| Destination membership/no unbound downgrade | ✅ controlled cases,SQLite native target-row fault,positive worker directions |
| New-format abrupt receiver commit/process-exit/replay | ✅ native Neo→SQLite |
| Verified descriptor exposed through native readiness/inventory | ❌ |
| Worker resolves pending routes from the stored descriptor without recapturing the destination | ❌ |
| New writes after cold restart while a previously verified target is offline | ❌ |
| Explicit operator repair/rebind with preserved original history | ❌ |

Exact next step:expose the verified per-target descriptor on native inventory/readiness,then make the worker use that recorded meaning. Prove recovery with a cold target registration whose identity-probe path is unavailable while native bound receiving still works. New-write offline restoration needs its own authoritative binding lifecycle;do not introduce a local-file cache or config guess as a substitute. Tenant/encryption/residency grants and other failure directions remain separate.

Core descriptor API commit:`17ff24071672850fe98452d9820bd47f0a679ca7`. This report accompanies the parent persistence milestone. Raw logs/TRX stay under worktree TestResults;resource lifecycle is in ENVIRONMENT_AND_CLEANUP_LEDGER.md.

## Latest evidence

Paths are relative to C:/Source/OASIS-cosmic. Raw evidence is regenerable;only summaries/hashes are committed here.

| Path | Passed | SHA-256 |
|---|---:|---|
| `TestResults/OwnedRoutingCurrent/route-descriptor-core.trx` | 4401 | `E8851173CAE70BD05AF3089321ABFBD83A50598F659E2388F1F91C946B8768AB` |
| `TestResults/OwnedRoutingCurrent/route-descriptor-shared.trx` | 1308 | `68A7CC59F509B0019D7F3E84EDFA838F36A3B7D7530DAAF65AB87A09E861EC16` |
| `TestResults/OwnedRoutingCurrent/route-descriptor-sqlite.trx` | 8 | `469C3EA51053CB2C3E842D7D231512D240797569B2A590ABCB1228C23C04CC7F` |
| `TestResults/OwnedRoutingCurrent/mongo-owned-atomic-regression.trx` | 8 | `94CDEB49A87FA8A11DD53917A244CCCF6B16F36A85BBBDF7D0ACD514ABD3D99C` |
| `TestResults/Neo4jCurrent/neo4j-routed-owned-outbox-current.trx` | 8 | `F943B34B9281CC9DECC207E403DC93CD4EDB76AB588CA75988614955961AB272` |
| `TestResults/OwnedRoutingCurrent/bound-worker-pair-current.trx` | 2 | `214CD350CA048410DF0F35EE062D50CCA4685715FD260A36E3957370450EAEC4` |
| `TestResults/Neo4jCurrent/neo4j-hosted-admission-current.trx` | 5 | `9AB372A9BFA44E7F7E49BD0F16F00CDCA4DC46B613380CE8825A72FD7F21FD6B` |
| `TestResults/Neo4jCurrent/neo4j-hosted-crash-current.trx` | 1 | `55E58BADFE6FE93F0EFF649EE4808ED41439471359674C2AEDC406E9643AC6CF` |
