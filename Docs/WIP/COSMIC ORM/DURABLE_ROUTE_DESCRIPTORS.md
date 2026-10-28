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
| Verified descriptor exposed through native readiness/inventory | ✅ SQLite/Mongo/Neo native scope |
| Worker resolves pending routes from the stored descriptor without recapturing the destination | ✅ controlled probe outage over native Mongo/SQLite receivers;Neo normal delivery |
| New writes after cold restart while a previously verified target is offline | ❌ |
| Explicit operator repair/rebind with preserved original history | ❌ |

Exact next step:implement authoritative binding lifecycle for new writes after cold restart when a previously verified destination is offline. Explicit V2 lineage migration/repair remains a rollout gate. Tenant/encryption/residency grants,Neo physical replacement/race and other directions remain separate.

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

## Snapshot-first readiness and worker recovery

`HolonDeliveryInspection.NativeRoute` is emitted by all3native providers only when readiness/inventory validates the original digested payload,target membership and descriptor. State-only Inspect retains its payload-free contract and returns null for this field. Older transport without descriptors also returns null;malformed descriptor-bearing transport fails validation instead of returning an older view.

The worker uses the stored source/destination/policy meaning directly. It checks source fingerprint and current host policy,then passes the recorded destination expectation to the native bound receiver. It does not recapture destination identity to construct a replacement meaning. Legacy type-only obligations remain quarantined. Older bound Format1 transport uses its explicit native-proof reconstruction path;the payload is never rewritten or upgraded.

Current gates:4411Core tests,4Mongo/SQLite shipping cases and6Neo shipping cases pass. Two Mongo/SQLite cases replace the destination registration after provider restart with a receiver-only proxy whose identity-probe method throws. The real native bound receiver still applies against its actual UUID/store identity. Both deliveries succeed with exactly0destination-probe calls and unchanged target revision on the following restart. This is a controlled metadata-probe outage over real stores,not a wholly offline destination or certification of its independent source role. New-write offline enrollment is not proved.

Neo readiness and inventory drive normal delivery/policy quarantine;the added older-bound-transport case proves completion/restart with original payload bytes preserved. The snapshot-first crash gate passes1native case in2m7s with original obligation replay and unchanged target revision. Native API Core612d4f0 is pushed;this report accompanies parent publication.

Latest snapshot-worker evidence (paths relative to worktree):

| Path | Passed | SHA-256 |
|---|---:|---|
| `TestResults/OwnedRoutingCurrent/snapshot-worker-core.trx` | 4411 | `FABB234DAED65ECBC74B3CA72F3A8C60DA4564FF368B690780F322A18AFB4B65` |
| `TestResults/OwnedRoutingCurrent/bound-worker-pair-current.trx` | 4 | `CB96B8606AAD57B90A16037B785C250F440795E2F7FCD6CF635A25ADA2BADFAE` |
| `TestResults/Neo4jCurrent/neo4j-hosted-admission-current.trx` | 6 | `A3D55E4BB8DFECE8E03C8BFDC1086EF1DF3F07A8C924A80C687C0589566AA597` |
| `TestResults/Neo4jCurrent/neo4j-hosted-crash-current.trx` | 1 | `34496FB3F68FE0F7EAA40884331D9ACDD44F4D2D726E7E629F1CE109BCFD2568` |

## Source-owned destination catalogue foundation

Core `da779a3` adds a native-source-scoped read-only catalogue contract. SQLite
records a physical destination approval in the same native transaction as the
primary graph, receipt and outbox. A new write to a different destination identity
returns `COSMIC_REPLICATION_DESTINATION_REBIND_REQUIRED`. Exact receipt replay is
checked first; failed transactions roll back newly introduced approvals as well.

The integrity-bound Shared record retains original establishment operation,
binding revision and policy provenance. Original policy is not current actor
authorization. Corrupt records fail closed; reading never repairs them. On upgrade,
an absent catalogue entry can be read from the latest complete immutable native
outbox descriptor, with a 256-row scan limit; the next primary transaction records
that original establishment. Older transport without descriptors is not identity
proof. Migration downgrade refuses to remove nonempty approval storage.

Five native SQLite scenarios cover independent-client/restart rebinding refusal,
second-target transactional rollback, read-only historical bootstrap, corrupt
approval refusal and same-destination/new-policy provenance. Mongo/Neo target
identities in these tests are declarations only, not native delivery proofs.

This is a foundation checkpoint, not completed automatic offline enrollment.
Mongo/Neo catalogue implementations, planner integration, operator rebinding,
V2 lineage migration, historical scan byte budgeting and all-provider lifecycle
acceptance remain open. No public deployment promotion is implied.

| Evidence | Passed | SHA-256 |
|---|---:|---|
| `TestResults/OwnedRoutingCurrent/destination-catalogue-core.trx` | 4411 | `F9DCDCEA5078A2BC94EFA53246292A705C746E23D39543137512388512424CB7` |
| `TestResults/OwnedRoutingCurrent/destination-catalogue-shared.trx` | 1316 | `1D80C80440E22FB5CB157FD11444FCFC9A25045F34344331E99E48FF6ADB4EF8` |
| `TestResults/OwnedRoutingCurrent/destination-catalogue-sqlite.trx` | 9 | `B4A4A32A74F65DEA60977CBF2F4DD17D0E6BDC77D1A29EC4826D8B6297BD9123` |

## Protected preparation recovery preserves native meaning

Reproduced two failures: status recovery reconstructed only SourceId/TargetIds,
losing SourceStorageIdentity both with and without complete NativeRoutes. Protected
preparation now authenticates those native fields and reconstructs the same bound
enrollment before any entity construction. Invalid/missing source, mismatched
membership and malformed policy fail closed. Older unbound preparations retain
their exact original format; no identities are guessed for old records.

The canonical native descriptor codec now lives in Core serialization. The existing
Shared entry point delegates to it, preserving the provider API and wire format.
Core builds both targets;4417Core and1316Shared tests pass, including2previously
failing cases and4malformed protected-intent cases. This is recovery correctness,
not proof that every provider's live replication/failover/load balancing is complete.

| Evidence | Passed | SHA-256 |
|---|---:|---|
| `TestResults/OwnedRoutingCurrent/bound-preparation-core-final.trx` | 4417 | `60969737D5C53BEC27088E2461D4CB930466E8DE18024544FBE4FE59EC1CA651` |
| `TestResults/OwnedRoutingCurrent/bound-preparation-shared.trx` | 1316 | `294859ACA60BE858512E6F007DE148786C5D450995DAA1CCE87523196B8D29B0` |

## Mongo destination catalogue

Mongo now records approvals in `CosmicDestinationBinding` using a unique native
`_id` scoped by source fingerprint and target authority. Approval insertion shares
the graph/receipt/outbox transaction; exact receipt replay remains read-only.
A replacement destination fails with the explicit rebind-required code. A second
mapping conflict rolls back a newly inserted first mapping and graph state.

Bound catalogue reads share native source identity admission. Missing records can
bootstrap read-only from committed validated outbox descriptors; malformed records
never fall through to discovery. Historical bootstrap has a256-row/16MiB decoded
BSON budget; oversized history requires explicit migration. Same-store policy
changes retain establishment provenance without authorizing the current actor.

`Scripts/TestHosts/Run-CosmicMongoDestinationTests.ps1` reuses the pinned Mongo
runtime/replica set, builds producers/consumer under one lease, requires exactly
5unskipped cases and checks journal teardown. All5native scenarios pass;8existing
owned-write/outbox scenarios also pass. No27029listener or pending fixture journals
remain. Destination identities in catalogue tests are declarations, not delivery.

Open:Neo catalogue, cross-client simultaneous first-approval race acceptance,
automatic cold/offline planner restoration and audited operator/V2 lineage repair.
No blanket replication/failover/load-balancing completion claim.

| Evidence | Passed | SHA-256 |
|---|---:|---|
| `TestResults/OwnedRoutingCurrent/mongo-destination-catalogue.trx` | 5 | `EE202937718CF2B0ACE61BFF9E923FC83EFA4E69D1F80AD0FEE91B41C8F10C06` |
| `TestResults/OwnedRoutingCurrent/mongo-owned-atomic-regression.trx` | 8 | `AB4FC2546D1268B2174EE869DCB09C2F82EE950C523B74DA879277822626608A` |

## Neo4j destination catalogue

Neo4j now implements the same source-owned destination catalogue. A unique
`CosmicDestinationBinding.Key` constraint scopes each approval to physical source
fingerprint and target authority; activation verifies its actual constraint shape.
Approval and primary graph/receipt/outbox share one transaction, after exact replay
admission. Changed physical destinations return the explicit rebind-required code.
Bound reads retain native source admission and never repair corrupt approval data.

The native scenario proves fresh-client replay/replacement refusal, rollback of an
AWS declaration inserted before an Oracle replacement conflict, corrupt digest
refusal, same-store policy provenance and read-only historical bootstrap. These
Oracle/AWS identities are declarations only; this is not cloud delivery evidence.
The fixture removes only its journaled graph IDs, operations and establishment IDs;
shared graph data and monotonic sequence remain intact. Related routed/hosted test
cleanup also removes only approvals established by their exact owned operation.

Run `Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicDestinationCatalogue`.
The existing pinned source-driver/Core/Shared hash guards remain enabled. The first
admission refused stale Shared in a consumer before starting services; the normal
producer build chain fixed this without copying DLLs or bypassing verification.
Native scenario1/1 and existing routed/primary regression8/8 pass.

Still open: simultaneous first-approval races, explicit rebinding, V2 lineage
migration and automatic cold/offline planner integration. No full HyperDrive
certification or hosted AWS/Oracle claim follows from this catalogue checkpoint.

| Evidence | Passed | SHA-256 |
|---|---:|---|
| `TestResults/Neo4jCurrent/neo4j-destination-catalogue-current.trx` | 1 | `C8D7A555C5C969BDCD68718C9F043B72922BF030A628BC04791D7C8A5826CEE2` |
| `TestResults/Neo4jCurrent/neo4j-routed-owned-outbox-current.trx` | 8 | `02B3C9B3AD154A97D419A9513AE623F08CC1FA5C8EFFD8A981E8B975E1E8B354` |

## Cold new-write planner restoration

Core7afa384 opens the transaction-bound source outbox and requires its destination
catalogue before enrollment. Every selected target is read explicitly: a verified
approval supplies its original physical target identity without destination
activation/capture. The target must still have its registered bound receiver role.
The new route uses the current host policy, not the establishment policy. Unknown
destinations require first native capture; catalogue errors/missing capability or
source/authority mismatch cannot become permission to discover another identity.

Five new controlled cases cover cold destination-probe outage/current policy,
catalogue refusal, missing catalogue capability, wrong physical source and wrong
target authority. Three failed before the fix. Existing owned-write mocks now
provide the required source catalogue contract; node/avatar/request routing tests
still reach their intended atomic write boundary. Core builds both targets;
4422Core tests pass. No native restart/save/delivery certification from these mocks.

Next required gate: real native source restart, a NEW save while destination
metadata is unavailable, durable obligation inspection and eventual bound delivery.
Simultaneous first approvals, audited rebinding/V2 migration and separate
failover/load-balancing acceptance also remain open.

Evidence: `TestResults/OwnedRoutingCurrent/cold-enrollment-core-final.trx`;4422passed/0failed/0skipped;SHA256 `266F646BB3419C1529CF485672F6872738165C012B67FD71235AA76747AFB185`.
