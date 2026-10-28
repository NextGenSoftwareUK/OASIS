# COSMIC ORM implementation status

Updated28October2026. [CURRENT_STATUS](CURRENT_STATUS.md) owns recovery/next steps;
[checklist](CQL_PARITY_CHECKLIST.md) owns per-database acceptance. Full programme incomplete.

## Latest snapshot-first worker milestone

[Durable route recovery](DURABLE_ROUTE_DESCRIPTORS.md):all3native readiness/inventory
implementations publish verified descriptors;worker uses their stored meaning.
4411Core,4Mongo/SQLite worker cases (2cold receiver-only metadata-probe outages),
6Neo worker cases and snapshot-first crash1pass. No destination probe is used for
new-format pending recovery. New-write cold/offline restoration and explicit V2
lineage migration/repair remain red. Core612d4f0 pushed.

## Latest durable route persistence milestone

[Durable route descriptors](DURABLE_ROUTE_DESCRIPTORS.md):complete frozen native source/
target/policy snapshots now share graph/receipt/outbox transactions in all3databases.
4401Core/1308Shared,SQLite8/Mongo8/Neo8native,shipping pair2/admission5/crash1pass.
Snapshot-first worker recovery and new-write cold/offline restoration remain red.
Core mergec194d536 includes incoming sandbox/DNA changes;4411Core/19DNA tests
and WEB4 builds pass. Native proofs retain their original revision scope.

## Latest source-outbox milestone

[Bound source outboxes](SOURCE_OUTBOX_BINDING.md):all3native providers expose guarded
factories;automatic planning requires the capability and the worker consumes it.
4401Core,SQLite6/Mongo8/Neo4native cases and Neo→SQLite shipping admission5/crash1 pass.
SQLite/Mongo native race windows verified;shipping Mongo→SQLite/SQLite→Mongo now pass2native
restart/idempotence cases. Neo physical replacement/race and other pair permutations
remain red. Durable cold-start route descriptors and
operator repair remain open. Core809a7c5 pushed.

## Automatic route binding — current verification

[Automatic bound routing](AUTOMATIC_BOUND_REPLICATION.md) now passes4400Core/1289Shared,
SQLite4/Mongo8 routed-primary cases, shipping Neo→SQLite restart1 and admission5
(including legacy-route/policy-change reconciliation). Native abrupt-process recovery1 and corrected Neo runner8 also pass.
Corec128178 pushed;source-outbox ownership/cold-start/operator gates remain open.

## Prior verified milestone

[Atomic primary-source binding](PRIMARY_SOURCE_BINDING.md):all3local stores enforce
optional source expectation before native commit/replay and original-outcome recovery.
SQLite5/Mongo5/Neo4native,4386Core/1285Shared pass. Real SQLite copy/Mongo UUID replacement
retain old receipts but cannot falsely verify/replay them. Plain atomic capability is
insufficient for bound intent;Mongo queue/recovery use the activated native owner.
Automatic routes now supply these expectations; final native gates are in progress.

## Prior inventory milestone

[Durable route inventory](REPLICA_ROUTE_INVENTORY.md) now discovers bounded pages of
unfinished native targets independently of current configuration. SQLite3/Mongo2/Neo2
native cases and4384Core pass. SQLite also proves blocked/leased/deferred visibility,
completed-head skip,case-sensitive targets,read-only state and no partial corrupt page.
Worker inventory consumption and automatic binding are implemented under verification;operator repair remains open.

## Prior receiver milestone

Mongo transaction-bound UUID admission now passes15native cases, including actual
collection recreation, bounded concurrent DDL rejection and mutable-context pinning.
All3local receiver capabilities exist. Shared acknowledgement reuses the original
owner-bound membership capture after a reproduced post-commit getter bug;scope guards
and later dirty edits remain protected. Latest4379Core/1283Shared,SQLite23/Neo6native
compatibility pass. [Full evidence and limits](REPLICA_STORAGE_BINDING.md).

## Prior receiver checkpoints

[Transaction-bound SQLite receiver](REPLICA_STORAGE_BINDING.md):7new/23affected native
cases,1280fullShared and4377fullCore pass. Receiver reads endpoint/store identity under
BEGIN IMMEDIATE before receipt replay or mutation. A competing native identity writer
is blocked until commit; replaced/missing/corrupt identity rejects without repair/write.
Neo now adds3new/6identity-binding native cases plus6existing replica regressions;db.info checked in the write transaction before receipt replay, lifecycle gate pins the driver. Mongo acceptance is recorded in the newer milestone above.
Shared bound catalogs hash the native identity and binding contract separately from
unbound descriptors; receivers cannot silently downgrade. Automatic routes now use native-bound enrollment under verification.

## Preserved verified scope

- Original12investor rows green on SQLite/Mongo/Neo;34native entity/reselection/cardinality cases/database. Full LINQ parity remains red; All/element/numeric depth paused.
- Oracle/localCosmos/Solana/IPFS/SOLID/ActivityPub have scoped SDK/HTTP/browser proofs and Mongo-paired presets. HostedAzure user-deferred; no hosted completion claim.
- Native SQLite→SQLite/Mongo import:3created/3unchanged/1updated, avatar-scoped visibility/stable IDs/source preservation. Mapping preserves save-owned metadata.
- Three-database routed-primary atomic enrollment/no-op/second-target rollback proofs; native identity capture implemented for all3. Capture alone does not bind delivery.
- Post-commit wake failures preserve acknowledgement and later observers. Shipping Neo→SQLite delivery/restart and one actual receiver-commit process-crash recovery proof; unchanged native lease/original operation.
- Automatic owner policy and explicit actor grant denial/expiry:3native shipping cases, retained reconciliation across restart/no rejected replica. Broader policy admission remains open.
- Latest Development merge:parent953cb7968/Core261fce1 integrated; [compatibility evidence](DEVELOPMENT_SYNC_2026-10-28.md).

## Remaining acceptance

1. Automatic native source/target/policy binding, durable route discovery and old/unbound operator reconciliation. Tenant/encryption/residency/placement admission remains separate.
2. Other native crash directions, lost primary outcomes, target outage/reconciliation, grant revocation, target-set changes and retention/operator repair. X10 stays red on all3.
3. Full graph/child parameters, history/restore, authorized delta values/retention, controlled migration/cutover, gas-aware placement and remaining EF/NHibernate rows require independent acceptance.
4. HostedAzure pending;originalSolana historical root/chunk has no backup. Preserve ledger/identities;never reset/reseed.

Only V2 implementation;legacy gaps documented. Green scopes/cursors frozen absent bugs.
No resumption of paused LINQ depth or deferred large POCO/schema-migration tooling.
Work only C:/Source/OASIS-cosmic, branch work/cosmic-cql; named commits and bounded builds.

[Earlier milestone details](Archive/IMPLEMENTATION_STATUS_before_bound_receiver_2026-10-28.md).
[Original full roadmap](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md).
[Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
