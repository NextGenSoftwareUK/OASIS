# Native source-outbox ownership

Updated28October2026. Bound source factories/automatic integration verified within the scopes below;X10 stays red.

## Required invariant

Native source identity must be validated in the same native transaction/snapshot as queue access. A preflight capture alone cannot authorize a later claim, lease renewal or acknowledgement. Copying a queue and its receipt/lease token must not transfer its source ownership. Failure must change neither queue state nor attempt counts.

## Current implementation

SQLite's existing two-argument outbox constructor remains the explicit unbound contract. A new three-argument constructor accepts an immutable expected source identity. Bound claim, renewal, finish and recovery finish validate `SqliteStorageIdentity.Read` after BEGIN IMMEDIATE and before queue access. Bound inspect, peek and route inventory keep identity and queue reads in one read transaction. No schema/identity initialization happens in the outbox.

Mismatch returns `COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH`;unverifiable native identity returns `COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_INVALID`. Neither reports a successful claim or mutation. A foreign provider authority is rejected at construction.

| Acceptance | SQLite | MongoDB | Neo4j |
|---|---|---|---|
| Transaction-bound native outbox implementation | ✅ | ✅ | ✅ |
| Actual native queue copy/replacement refuses original identity | ✅ native backup copy | ✅ native collection missing/replacement | ❌ |
| Provider factory exposes transaction-bound source outbox | ✅ native probe | ✅ native probe | ✅ native probe |
| Shipping worker executes with bound source | ✅ SQLite→Mongo | ✅ Mongo→SQLite | ✅ Neo→SQLite |
| Concurrent source replacement/claim race proof | ✅ native identity UPDATE refused after admission | ✅ native UUID/DDL transaction | ❌ physical lifecycle proof pending |

## Native proof

`OutboxSourceBindingTests.cs` → `CopiedOutboxCannotReadClaimRenewOrAcknowledgeTheOriginalSourceLease` creates a real source write/lease, then SQLite BackupDatabase copies payload, UUID and exact lease token into an owned different file. Bound inspection/peek/inventory/claim/renew/finish/recovery finish all reject the copy. `PRAGMA data_version` proves those refusals commit no changes. Corrupting only the owned copy's identity produces an explicit invalid-binding error without delivery repair. The actual source still renews/completes its original lease and returns no unfinished heads.

Six native tests pass including existing lease fencing, competing clients, expiry ordering, read-only blocked-head inspection and atomic recovery-evidence failure. Evidence:`TestResults/OwnedRoutingCurrent/outbox-source-sqlite.trx`;SHA-256:`EA8B327B8756E6F95ED6735AD186160360EEDFF0FE6AB996DA3DFAC109284407`. Build/log outputs are adjacent. Fixture copy/sidecars are removed only after successful checks/connection disposal;unknown outcomes retain the existing pending journal.

## Mongo native gate

`MongoHolonReplicationOutbox(context, expectedSource)` captures the actual native database/client once. Every bound mutation runs the existing native UUID validator as the first statement in its existing snapshot transaction. Bound inspect/peek/inventory use the same transaction owner, including known-outcome cleanup handling. Explicit unbound reads retain their majority observation contract. The shared validator is reused, with outbox-specific mismatch/invalid error codes;no duplicate UUID/endpoint algorithm.

`MongoBoundOutboxTests.cs` proves missing/recreated native Holon collections cannot read, claim, renew, finish or attach recovery evidence to the retained original lease. Raw native BSON equality proves no outbox mutation;the original write receipt remains. Repointing the public context cannot redirect the captured owner or create the forbidden Ghost database.

The native race case pauses the real driver after UUID admission and before the lease query. Another client attempts to drop Holon with maxTimeMS500 and receives native timeout50 while the same claim transaction owns the namespace. The test releases the original transaction, verifies its operation identity, then renews and completes its lease. This is live namespace/claim proof, not a mocked preflight. Collection UUID admission remains server-version-specific (local Mongo8.3.11);unsupported servers refuse binding rather than using an unbound path.

Mongo gate:8passed (3new outbox cases plus4existing atomic-source cases and1physical original-receipt recovery case). Evidence:`TestResults/OwnedRoutingCurrent/source-binding-mongo.trx`;SHA-256:`6B59EE59A1891E4C2A738A9FB91B14BF1EE332FB63C2AC24025BF7882CCA4C43`. Existing fixture journals absent and owned Mongo process stopped after success. No new runtime/download/database environment.

## Neo and automatic integration — verified scope

Neo outbox transactions now reuse the same db.info/native endpoint validator as bound primary and receiver writes. Its bound factory captures driver and endpoint together under the provider lifecycle gate;mutable Host is not proof. A missing captured endpoint cannot create a bound factory.

`IBoundHolonReplicationOutboxSource` is the explicit provider contract. All three providers expose it using their activated native owners. Automatic enrollment rejects a source missing this capability before submitting a write. The shipping worker captures the pinned source expectation and requires its bound factory before inventory discovery, claiming or handing payloads to a target. No unbound outbox downgrade is attempted.

The shared `CosmicOutboxSourceBindingProbe` runs inside each existing bound-primary fixture. It uses the real provider factory, verifies the original obligation, claims it, then sends an alternate expected source through inspection/peek/inventory/claim/renew/finish/recovery finish. All must return mismatch with unchanged original state/attempt count. Only the actual source can renew and finish that lease. This verifies native metadata admission/factory wiring, not actual Neo database destruction/replacement.

Final verification:4401Core,SQLite6,Mongo8,Neo4primary/factory and5shipping admission/restart cases pass. Bound source crash recovery1 passes after actual receiver commit/process exit/lease expiry. The final lifecycle-gate refinement was verified by fresh Neo producer/primary and worker admission runs;the crash run preceded that capture-only refinement.

SQLite's new native race uses the production IHyperDriveClock boundary after identity admission and before each lease query/update. A second real connection attempts to replace the owned identity and receives native SQLITE_BUSY for claim,renew and finish. All three original-owner operations complete, and native identity remains unchanged. No provider test hook or speculative delay is added.

All three provider factories are exercised by the shared primary probe. The original worker gate covers Neo→SQLite;the new two-case shipping gate independently covers Mongo→SQLite and SQLite→Mongo. This establishes all three source implementations in those directions,not every source/target permutation. No physical Neo database reset/replacement was performed.

## Exact next step

Retain separate native Neo physical replacement/race acceptance and unverified source/target permutations. Next persist immutable complete route descriptors with native atomic obligations so cold-start/offline target identity recovery does not rely on process-local pins;operator repair remains a separate gate. No full X10 completion or universal availability claim.

## Latest evidence

Core commit:`809a7c5d58c0f306946e0b1da880fe27b1d4aab8`. Raw outputs are regenerable in TestResults,not committed into Docs.

| Worktree-relative evidence path | Passed | SHA-256 |
|---|---:|---|
| `TestResults/OwnedRoutingCurrent/source-outbox-core.trx` | 4401 | `CE43C61938FF7D414CFA9A27D6661B69023F4AECA95E4F4D78F3229BF38866D7` |
| `TestResults/OwnedRoutingCurrent/source-outbox-sqlite-factory.trx` | 6 | `429D0639073680DF3B3BF8054BFE8EE9281E076ECBEA8EDA44894358D42D0320` |
| `TestResults/OwnedRoutingCurrent/source-binding-mongo.trx` | 8 | `9E5392197B68690574BF4740C1CBD443FF288CD00125D2FA7D1E026BC36A3C8A` |
| `TestResults/Neo4jCurrent/neo4j-cosmic-primary-outbox-current.trx` | 4 | `1E015209ABEBE38305A49904756F1ABAC701D0DC6B6B3A1292336A37317B9D20` |
| `TestResults/Neo4jCurrent/neo4j-hosted-admission-current.trx` | 5 | `B99618294F3B43E9B11BFF1279800FF8F839C3F06863B0B4B7F93D5582CC06EA` |
| `TestResults/Neo4jCurrent/neo4j-hosted-crash-current.trx` | 1 | `7EFE1BB0D31020E1442436B0BA9F812312DF109BDA13C8B34BEEDDEF87769829` |

## Shipping Mongo/SQLite source directions

`NativeHostedReplicaSqliteMongoTests.cs` → `ShippingWorkerUsesBoundMongoAndSqliteSourcesAcrossProviderRestart` runs both source directions through the actual WEB4 hosted worker. No worker exists during the owned write. Request routing disappears,providers restart before delivery,and fresh registrations recapture stable native identities. The worker discovers the original native obligation,delivers it once,and records one attempt. After another provider/worker restart the obligation stays completed,source version stays unchanged,and target provider revision is unchanged. Both native stores retain the original actor and holon ID.

Run `Scripts/TestHosts/Run-CosmicBoundSourceWorkerTests.ps1`. It uses the existing Mongo8.3.11 replica-set runtime/data and one fixed owned SQLite file;no Neo service/new environment/download is needed. Producer hashes must match WEB4 and the native consumer. Exactly2cases must pass with no skips. The test journals actor/operation/holon/file before any mutation,preserves unknown outcomes,and verifies exact native holon/receipt ownership before teardown.

Latest evidence:`TestResults/OwnedRoutingCurrent/bound-worker-pair-current.trx`,2passed,SHA-256:`921C4F8C9B82C20184E417C5A59EE736DCCF7AED28E2A0A3A64D41D9F5598D40`. Mongo DB `cosmic_bound_source_worker_fixture`,owned SQLite file/sidecars and pending journal were removed after provider shutdown;the owned Mongo process was joined. This is ordinary restart/idempotence proof;these two directions do not yet have abrupt-process-exit or hosted-production certification.
