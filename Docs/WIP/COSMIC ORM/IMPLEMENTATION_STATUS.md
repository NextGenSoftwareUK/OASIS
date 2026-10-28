# COSMIC ORM implementation status

Updated 28 October 2026. [CURRENT_STATUS](CURRENT_STATUS.md) owns recovery/next steps;
[checklist](CQL_PARITY_CHECKLIST.md) owns per-provider acceptance. Full programme incomplete.

## Verified scope

- Original12investor rows green on SQLite/Mongo/Neo. Entity/reselection/cardinality stages have34native cases/database; full LINQ parity remains red. All/element/numeric depth is paused.
- Oracle, local Cosmos, Solana, IPFS/SOLID/ActivityPub have scoped SDK/HTTP/browser proofs and Mongo-paired presets. Bounded membership/indexed reads are not universal querying. Hosted Azure is user-deferred; no account or resource provisioned.
- Native SQLite source → SQLite/Mongo target:3created/3unchanged/1updated, stable avatar-scoped identities/private visibility/source preservation. Mapping ownership root fix prevents false unchanged-import revisions. Migration-tooling/chain/encryption/replication migration acceptance remains open.
- V2 routed owned primary transactions:SQLite4new/21related native cases;Mongo4/6;Neo4j4/6. Effective targets, operation/actor/revisions, no-op suppression and second-target rollback pass. These transaction matrices use declaration-only targets.
- Post-commit wake bug fixed in Cored980bdf:subscriber exceptions become retained warnings, preserve the native acknowledgement/caller publication and do not block later observers. Four focused cases/4325full Core pass; both Core targets build. No new source-save retry.
- Actual shipping WEB4 service now delivers a routed Neo4j save to a real SQLite receiver after being absent at save. Recreated service/providers do not redeliver completed work.

## Latest milestone: actual worker-process crash after replica commit

The frozen native test assembly runs a child instance of the shipping service. Its
receiver delegates to real SQLite, flushes boundary evidence and terminates its own
process before returning the acknowledgement. The parent verifies source InFlight/
attempt1 and the committed replica. It waits for the native default two-minute lease
to expire without editing state or clock, then starts a fresh shipping worker. The
same obligation completes on attempt2; frozen payload/replica version remain unchanged.
Another service/provider restart leaves it completed at attempt2. No new source save.

- Crash acceptance1/1pass;ordinary restart regression1/1pass;no skipped parent cases.
- Child vstest run is intentionally aborted by actual test-host termination; it is evidence of the injected crash, not a passing child test.
- Exact source ID/operation cleanup verified;owned SQLite files and both journals absent;retained Neo container exited. Shared sequence retained. No dump was requested;child temporary directory verified empty and retained for reuse.
- No production change in this milestone. Previous dependency-injection refactor retains the original service constructor and one delivery loop.
- Reproduce under one build lease:`Run-ReusableNeo4jTests.ps1 -CosmicHostedReplicaCrash`;ordinary regression:`-CosmicHostedReplicaRestart`. Bounded workers;frozen child performs no restore/build/seed.

Evidence (raw output only in the worktree):

| Path | Result | SHA256 |
|---|---|---|
| TestResults/Neo4jCurrent/neo4j-hosted-crash-current.trx |1pass,2m8s|D381A3014C5A2110B81B9BF72512E317F92A708B07F370DC82B0E49E608F5345|
| TestResults/OwnedRoutingCurrent/replica-crash-child.trx |expected aborted child|9616F7A24429863223DDF0A51F5AFBA765A0940DAF1D7E223B27877D800DEC19|
| TestResults/Neo4jCurrent/neo4j-hosted-restart-current.trx |1pass after shared-fixture refactor|0F4522767A377841C0192505293BDD1E4E928D7F0E7689A748430E3BA0D78EB9|
| TestResults/OwnedRoutingCurrent/wake-full-core.trx |4325pass,last Core checkpoint|03D5E49BAD788D88BFA85DEE97EDD98EF200FA5E9B1D1EA5DE9FAD091F1E7651|

## Shared authorization boundary prerequisite

Configured catalog authorization now lives in the reusable policy constructor of
`CommittedHolonReplicaTarget`;catalog route hashes and captured policy binding remain
unchanged. It retains frozen transport/graph admission, actor-grant expiry, before/after
policy-binding checks and warning evidence. The source-only constructor remains a
low-level trusted adapter, not caller authorization. Automatic WEB4 still uses it;
explicit automatic policy wiring/denial/expiry acceptance is next, not complete.

- 1272Shared tests pass, including3new direct policy cases;2native configured-host cases pass;zero skips.
- Evidence TestResults/OwnedRoutingCurrent/authorization-shared.trx SHA2569777F7BBAFE985BEBE16490F3F2979D0F2A7786E23060C86990CC76D233F35CB.
- Native TestResults/Neo4jCurrent/neo4j-cosmic-configured-host-current.trx SHA256F2FF849EE7325529C28C3C7DE71248AB0842602CED3166A34FD96E8B40CEC02B.
- Sandbox ProviderFaultGate explicitly excludes background work and is unchanged;it is not a grant/placement policy.

## Remaining acceptance

1. Delivery-time actor/tenant/encryption/placement authorization and physical route binding. Automatic WEB4 delivery currently uses source enrollment, not the configured catalog's explicit expiring policy;do not conflate these.
2. Other native crash directions, lost primary outcomes, target outage/reconciliation, grant revocation, target-set changes and retention/operator repair. One Neo→SQLite crash case does not close X10.
3. Full graph/child parameters, history/restore, deltas' authorized stored values/retention, controlled migration/cutover, gas-aware placement and remaining EF/NHibernate checklist rows remain independently open.
4. Hosted Azure pending;original Solana historical root/chunk recovery has no backup. Preserve identities/ledger;no reset/reseed.

Only HyperDriveV2 implementation;legacy gaps documented. Green scopes/cursors frozen
absent reproducible bugs. No resumption of user-paused LINQ depth or deferred large
POCO/schema-migration tooling. Work only C:/Source/OASIS-cosmic/work/cosmic-cql.

[Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns retained environments.
[Earlier checkpoint details and hashes](Archive/IMPLEMENTATION_STATUS_before_replication_crash_acceptance_2026-10-28.md).
[Original full scope](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md).
