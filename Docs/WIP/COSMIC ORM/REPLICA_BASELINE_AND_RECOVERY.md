# Replica baseline and recovery status

Updated 31 October 2026. X10a/X10b remain incomplete across all providers. The reviewed Mongo-to-SQLite demo repairs have completed; do not repeat them. Historical checkpoints below are implementation history, not new repair instructions.

## Current verified checkpoint

The shell-only reviewed initialization completed held source sequence6 and reviewed sequence8. Preserved follow-up inspection at2026-10-30T21:32:05.5938103Z shows all6selected obligations Completed with null errors, no held SQLite queue head, and zero mutation commands in that inspection. The host command acknowledged successful target application/fenced source completion. The independent follow-up proves source/outbox completion only; it explicitly does not inspect destination contents or query acceptance. Do not use that inspection as destination-row proof. Current demo writes also have independent matching SQLite/Neo4j replica/query proofs in CURRENT_STATUS.

Evidence under `TestResults/Web4SandboxCqlCurrent`:

| File | SHA256 |
| --- | --- |
| `hosted-repair-command.log` (sequence6 success) | `c0a5cc31358e009154ef51281cfe11788897fe36ce34048b9855e46131912ec7` |
| `hosted-repair-8-reviewed-command.log` (sequence8 success) | `9211fe9a24c19dd43454b11e91c2fa5369d28bf51f0439820baa576ffb0b0c5b` |
| `hosted-repair-after-8.log` (completed obligations/no held SQLite head) | `4679f2115b557eddc3a64ed96a4f67bb2b9eeb13bb95ffeca78082534d698662` |

Reinspection here read preserved evidence only; it did not reconnect to native stores, mutate the host, replay repair operations or retest green scopes. These observations do not certify today's entire queue, general tenant-safe operator APIs, all source/target routes, automatic future-write cutover or larger-store audit admission. Those remain red. Preserve original request/journal/receipt evidence; no replacement requests.

## Implemented: explicit SQLite initial baseline

The trusted native capability `ICommittedHolonReplicaInitializationStorageProvider` accepts a committed source snapshot through `InitializeCommittedHolonReplica` / `InitializeCommittedHolonReplicaAsync`. The calling application must authorize the transfer and verify the source snapshot. No HTTP endpoint, automatic delivery permission or source-authentication policy is introduced by this interface.

- Uses the existing atomic SQLite write owner and an IMMEDIATE writer reservation.
- Can preserve a committed version greater than 1, including logical IDs, audit and selected graph edges. Generates target-owned provider revisions.
- Requires a nonempty actor and a Save snapshot; rejects tombstones and incoherent/uncommitted source content.
- Fences every selected target node against current data, replica heads, archived revisions and retained write receipts. Existing conditional and replica receipt envelopes are inspected explicitly; unknown envelope shapes prevent initialization.
- A conflict anywhere rejects the complete graph, receipt and replica heads atomically. Concurrent initializations have one winner.
- A separate fingerprint domain binds initialization receipts. Normal sequential delivery cannot impersonate initialization or reuse its operation ID.
- Exact initialization replay returns its original acknowledgement, including after restart or subsequent versions, without rewinding current data.
- Normal delivery still requires version 1 on an uninitialized target and an exact predecessor thereafter. No implicit baseline or lineage relaxation was added.

Source: [capability](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/ICommittedHolonReplicaInitializationStorageProvider.cs), [shared plan](../../../Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/CommittedHolonReplicaPlan.cs), [SQLite transaction](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/Persistence/Repositories/HolonRepository.Conditional.cs), [SQLite baseline fence](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/Persistence/Repositories/HolonRepository.Replica.cs).

## Implemented: MongoDB native baseline

MongoDB implements the same trusted capability and shared initial-baseline plan. Its existing snapshot transaction owns graph, replica heads and exact receipt. Every atomic graph mutation now updates a persistent `CosmicHolonLineageFence` for each selected ID in deterministic order, including hard deletes; initial baseline installation requires that fence to have been absent. This makes competing writers conflict natively rather than trusting an unlocked missing-row snapshot. Positive operation replay remains read-only and does not touch fences.

Atomic receipts now contain explicit `HolonIds` membership. Initialization refuses retained receipts with absent, empty or malformed membership with `COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED`; it does not infer absence from historical receipt JSON. An explicitly reviewed migration of older receipts is required before initialization in such a database; the native migration below now implements that step, but has not been run on hosted data. Current rows and replica heads still prevent installation. Mongo history is embedded in its current record, which the current-row fence protects. Legacy non-atomic repository mutations are outside this V2 primitive's concurrency guarantee; no legacy implementation was changed.

Two native sync/async scenario runs pass on the reused Mongo replica set. Each actually commits a source graph through versions 1–4, installs/reopens/replays its baseline, delivers version 5 without rewinding on replay, races two initializers, rejects independent child data atomically, rejects reinitialization after an independent hard delete, and refuses an unknown historical receipt. Eight shipping sandbox billing lifecycle cases also pass against this changed atomic owner.

Source: [Mongo capability](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.Replica.cs), [transaction owner](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.Import.cs), [lineage fences](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.Replica.cs), [native scenarios](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/MongoReplicaInitializationTests.cs). Runner: `wsl -d Ubuntu -u dellams -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-mongo-replica-initialization-linux.py`, after bounded builds under the worktree lease.
## Implemented: Neo4j native baseline

Neo4j implements the same trusted capability/shared initial-baseline plan in its existing native transaction. The normal write owner locks every selected Holon node, including placeholders for absent records, before testing baseline absence. Initializers preserve versioned source graph/audit content with target-owned revisions; independent current records or existing replica heads reject the graph.

While those selected-node locks remain held, initialization checks `OasisRecordVersion` history and performs an explicit streamed audit of at most 10,000 retained committed receipts. Every receipt is validated by the existing shared canonical receipt decoder before its graph identities are inspected. Unknown/corrupt receipts or a larger audit require explicit reconciliation (`COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED`). This administrative audit is not a CQL managed-query fallback; it adds no APOC/plugin dependency, infers no metadata, mutates no old receipts and grants no HTTP permission. Larger-store admission/migration still needs its own acceptance.

Four native cases cover sync/async initialization with history ON/OFF: actually committed source graph versions 1–4, baseline installation, restart/exact replay, ordinary version-5 delivery, one-winner initialization race, independent-child graph rollback, hard-delete protection through history or retained receipts, and unknown-receipt refusal. Six existing sequential-replica/SQLite-target cases also pass. Source: [native entry/absence audit](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/Neo4jOASIS.Replica.cs), [transaction/receipt owner](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/Neo4jOASIS.Conditional.cs), [native cases](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/Neo4jNativeReplicaInitializationTests.cs).

Runner: `Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicReplicaInitialization` inside one provider build lease. It reuses the retained owned Neo4j container/source driver. Exact generated IDs/operations are journaled and flushed before writes under ignored `LocalState/COSMIC/Neo4jReplicaInitialization/fixture.json`. A prior unknown outcome stops reuse. Cleaned state requires native absence checks and source disposal/removal; cleanup never depends on a surviving TestResults log.
## Implemented: explicit audited Mongo receipt-membership migration

Trusted concrete-provider maintenance APIs:

- `AuditReplicaReceiptMembershipAsync(operatorId, selectedOperationIds, cancellationToken)` reads 1–128 explicitly selected receipts. It verifies canonical receipt integrity, graph/delta links and outer operation/fingerprint binding, returning only the configured-store hash, operator ID, receipt hashes and canonical member IDs.
- `MigrateReplicaReceiptMembershipAsync(stableMigrationId, reviewedAudit, cancellationToken)` atomically compares the complete audited native receipt hashes, revalidates canonical membership, adds only `HolonIds`, and commits an integrity-bound migration acknowledgement in `CosmicReceiptMembershipMigration`.

The application must authorize the operator and intended store; these native methods introduce no HTTP route or authorization grant. Export the complete reviewed audit and stable migration ID before submission. The audit constructor supports reconstruction from persisted metadata; the native tests exercise that reconstruction after reopening the provider. Never regenerate an audit under an uncertain existing migration ID: its fingerprint is bound to the original audit/operator/store.

Original `ResultJson`, `Fingerprint`, timestamps and other receipt fields remain byte-for-byte unchanged. A changed audit rejects the whole batch, rolling back earlier metadata updates. Competing job claims cannot both commit independently. Reopening/replaying an acknowledged job returns its original updated-count, without repeating holon writes or changing logical history. A reused ID with another intent is refused. Commit uncertainty, a corrupt acknowledgement or unknown cleanup require reconciliation of the same job; they never justify a fresh automatic submission.

Two native migration scenarios (ordinary/stale batch), combined with the two existing Mongo baseline scenarios, pass on the reused native replica set. They verify wrong-store and forged-member rejection, full receipt-byte preservation after removing only the new membership field, stale second-row rollback, metadata-only replay/reconstruction, operator binding, same-job concurrency, pre-write cancellation, corrupt-job acknowledgement refusal, invalid receipt refusal, fresh version-4 baseline admission after migration, and continued protection of an independent hard delete whose pre-upgrade fence is absent.

Persistent test ownership and every submitted job's audit are flushed before writes to ignored `LocalState/COSMIC/MongoReplicaMembershipMigration/fixture.json`. Exact fixture databases are removed only after owner-marker checks; cleaned state is recorded after native absence verification. No hosted receipts were migrated.

Source: [audit metadata contract](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoReplicaReceiptMembershipAudit.cs), [trusted entry points](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.ReplicaMembershipMigration.cs), [atomic migration](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.ReplicaMembershipMigration.cs), [native scenarios](../../../Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/MongoReceiptMembershipMigrationTests.cs).
## Implemented: authorized read-only initialization preparation

`IHolonReplicationObligationReader` is a separate trusted capability implemented by the SQLite/MongoDB/Neo4j outboxes. It returns the original atomically enrolled payload with its validated digest and delivery observation. Existing redacted inspection APIs stay redacted. This capability requires operator authorization before access; it never claims, renews, finishes, initializes schema or retries a mutation.

Shared `HolonReplicaInitializationPreparation` requires a server-owned source/target route and `IHolonReplicaInitializationPolicy`. Regular replication grants do not automatically grant initialization. It explicitly authorizes source inspection, validates the native frozen payload/route/actor/digest, requires the current held queue head with a lineage conflict and affirmative evidence that no native execution remains unresolved, then authorizes the exact proposed baseline. Policy errors/warnings remain failures with their exception; denial and changed bindings are distinct. Policy cannot rewrite the snapshot it approves.

The returned intent contains only operator, source/initialization operation IDs, sequence/attempt, root IDs and route/policy/source/baseline fingerprints. No private graph payload is exported. Preparation is read-only: it does not apply a baseline, complete a delivery, enroll future source writes or promise an atomic cutover. Persist the reviewed metadata before any later target phase, and require matching authoritative source proof and current authorization again when applying it.

Verification: 84 shared controls (17 preparation cases plus 67 existing replica cases), three SQLite native cases (including a real version-5 held obligation prepared under an explicit actor/root/route policy with target and queue unchanged), six Mongo native cases (including two immutable-source inspection cases), and two Neo primary-outbox cases pass without skips. Native comparisons prove source rows/lease state unchanged by inspection. Invalid digests/routes/states, unresolved/missing native evidence, stale heads, denied/failed/changed policy, policy snapshot mutation and cancellation are rejected before any target mutation. Existing owned fixtures/services are torn down after verification.

Source: [trusted read capability](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/IHolonReplicationObligationReader.cs), [shared preparation](../../../Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/HolonReplicaInitializationPreparation.cs), [shared controls](../../../Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests/HolonReplicaInitializationPreparationTests.cs). The Core capability is additive; no public HTTP endpoint or default authorization is installed.
## Verification and limits

Seven new native cases plus existing replica/outbox controls: 31 pass, zero skips. Shared replica/transport controls: 67 pass, zero skips (four new baseline plan cases). Native source graph actually committed through versions 1–4; tests cover sync/async installation, version-5 delivery, exact replay, provider restart, competing clients, child conflict rollback, independent hard delete with history disabled, and unknown receipt rejection.

The first hard-delete test exposed a new implementation defect: the initial receipt fence only inspected replica envelopes. It now also inspects the canonical conditional graph wrapper. The failed proof is retained separately in TestResults; no failing implementation was deployed.

## Still required

1. Operator/API composition of the implemented Mongo migration where historical receipts require it, plus larger-store Neo receipt-audit admission. Native primitives and local migration proofs still do not establish end-to-end enrollment acceptance.
2. Complete atomic future-write enrollment/cutover and generalize authorized composition beyond the verified Mongo-to-SQLite route. Durable one-obligation application, reauthorization, target receipt recovery and source completion already exist; reuse them instead of adding another owner.
3. Broader source/target and hosted tenant-isolation acceptance for the existing fenced reconciliation/resume protocol. The reviewed Mongo-to-SQLite held obligations have been completed with exact target acknowledgements; this is not a general public repair authorization. Preserve original delivery evidence and independently verify targets.
4. Operator/UI/API composition and hosted three-database acceptance, including tenant isolation. Never expose the trusted primitive directly as unauthenticated repair.

At the earlier read-only checkpoint, sequence6 held a version4holon with no matching target history. The reviewed baseline application and fenced completion subsequently succeeded (current checkpoint above). Do not repeat that repair or reset/skip/replay obligations. Hosted Azure remains separately deferred/free-only.

## Demo-specific Mongo source completion

MongoHolonReplicationOutbox.CompleteInitializedBaselineAsync is a trusted native operator capability, not an HTTP authorization grant. It requires the application to reauthorize the reviewed intent and obtain the exact receipt directly from the configured target. In one native transaction it compares original payload/operation/attempt, reconstructs the initial-baseline fingerprint, requires affirmative no-unresolved-execution evidence and the current held head, records the initialization acknowledgement and completes only that obligation. Original failure/recovery evidence is retained. Exact replay checks the persisted acknowledgement without changing timestamps or later rows; changed receipt/intent refuses. An absent target receipt never permits completion.

Seven native cases pass/no skips (new MongoBaselineCompletionTests plus six baseline/migration/outbox cases). The new scenario uses actual conditional versions1-4 with enrollment only at4, a real target lineage rejection, policy-prepared intent, actual target baseline/immutable receipt, stale attempt refusal, exact read-only replay and normal sequential version5 delivery. This does not yet prove durable application/journal composition, Mongo-to-SQLite hosted repair or full X10 acceptance. Read-only hosted inspection has now confirmed no current/head/history/receipt matching the held holon in the actual SQLite database opened by API process1; no hosted mutation occurred.

## Durable one-obligation application

HolonReplicaInitializationApplication composes explicit host policy/preparation, configured target receipt recovery and native fenced source completion. A host-owned protected journal path records only the reviewed metadata, checksum, receipt digest and Prepared/Submitted/TargetCommitted/SourceCompleted phase. Exclusive file ownership and flushed atomic publication precede submission. The current source, route and policy are reauthorized on every run and again before source completion. A submitted operation with no target receipt refuses resubmission; a committed immutable receipt recovers the same operation after a lost response. Completed replay verifies exact source/target acknowledgements without rewriting the journal. Unknown temporary publication, journal mismatch, revoked policy or uncertain native result remains an explicit error. No automatic future-write cutover or public repair grant is added.

Verification:23 shared controls pass (six new interrupted/application scenarios);8 real Mongo native cases pass, including durable application against real source/target, original held version4, read-only completed replay and ordinary version5 delivery. This local proof preceded the hosted Mongo-to-SQLite repair, which subsequently passed as recorded in the current checkpoint above. Other routes and automatic cutover remain unverified.

## Shell-only demo repair host

WEB4 now recognizes --cosmic-repair-baseline followed by the protected reviewed request path, before HTTP host creation. Requires OASIS_SANDBOX=true and configured V2 OASIS_DNA_JSON; no public endpoint. The strict request contains operator/catalog/init operation identities, exact source sequence/operation/actor/root/payload SHA256, nonsecret configured-store identity and existing target file. Source credentials/endpoints come only from node configuration. Missing target files and endpoint/payload mismatches refuse before target writes. A separate .application.json beside the request is the protected durable journal; preserve request, operation ID and journal across interrupted calls.

Example inside the deployed dev service: dotnet NextGenSoftware.OASIS.API.ONODE.WebAPI.dll --cosmic-repair-baseline /data/COSMIC/ReplicaRepair/request.json . Never generate a replacement request after a submitted/unknown outcome. The deployed command supports Mongo-to-SQLite. Task code additionally verifies Neo-to-SQLite and SQLite-to-Mongo as detailed below; these additional routes are not deployed. Broader operator tooling remains deferred.

Run Scripts/Sandbox/Test-CqlReplicaBaselineRepair.ps1 after the bounded producer/consumer builds. One real native command case passes/no skips; source versions1-3 were never enrolled, held4 initialized and source-completed, replay unchanged, then ordinary5 delivered. Exact source DB/SQLite/journal fixtures removed after ownership/absence checks.71 HTTP/schema/admission controls also pass. Hosted Mongo-to-SQLite sequence6/8execution subsequently passed; see the current checkpoint above. Broader operator/API composition remains pending.


## Reviewed request boundary fix — pending release

The shell command previously used ordinary JSON deserialization, which silently accepted duplicate reviewed fields with last-value semantics. A request could therefore display one operator/operation identity to a reviewer while deserializing another. The command now uses one bounded reader (8KiB, depth4), requires an object, rejects duplicate decoded property names and unknown fields, and preserves the canonical Request record/serialization used by existing policy bindings. No native connection is created by parsing. Existing completed requests/journals were not modified or replayed.

13API/schema controls pass, including reproduction of the old duplicate behavior on the shipping Request type, actual new-reader rejection for6identity fields, canonical identity preservation, nonobject/unknown/oversize rejection and the actual file read boundary. The exclusive owned8,193B test file was removed after handles closed. Evidence `TestResults/HostedCqlIntegrationCurrent/replica-reviewed-request.trx`, SHA256 `F5048790B4E42E8F177A2D6F96D97C977705553B1046A7EA1D188CD585A61398`. This is a reproduced parser bug fix, not another test of completed native repair.

This initial Mongo-only implementation checkpoint is superseded by the Neo-to-SQLite and SQLite-to-Mongo task milestones below. All three sources now have native fenced completion evidence; the two additional shell routes still need release/hosted acceptance. Remaining route pairs, operator/API acceptance and automatic future-write cutover remain gaps.


## SQLite source-side fenced completion — pending release

`SqliteHolonReplicationOutbox.CompleteInitializedBaselineAsync` now composes with the existing `HolonReplicaInitializationApplication`. This trusted capability requires application reauthorization and the exact committed receipt read from the configured target; it grants no HTTP/operator permission by itself.

An IMMEDIATE native transaction validates the frozen source payload, original operation/attempt/digest, target-bound baseline fingerprint, current oldest held target head and affirmative no-unresolved-execution evidence. It records the exact initialization acknowledgement and completes only that obligation atomically, preserving payload, original error/message and recovery evidence. Exact replay (including reopened clients) observes the same acknowledgement without timestamp or source changes. Absent/changed receipts and stale attempts fail; cancellation before submission does not mutate the source.

Acknowledgements live in `CosmicHolonOutboxInitializationCompletion`, created by both the existing EnsureCreated schema-upgrade owner and migration20261031180000. No historical migration was changed. Downgrade refuses when this table contains acknowledgements rather than dropping required recovery evidence.

Native SQLite-to-SQLite tests actually commit source1..4, enroll5, obtain a real target lineage rejection, review/initialize/complete the held5, reopen and exactly replay, reject absent/changed receipts/stale attempts, race two completion clients (one acknowledgement), refuse destructive downgrade, then deliver ordinary6. The durable application variant uses the existing coordinator and verifies unchanged completed journal replay. This is not yet cross-provider/hosted SQLite-source acceptance or automatic cutover. Neo4j source completion was subsequently implemented/native-verified below; host/operator composition and automatic cutover remain open.

Evidence under `TestResults/HostedCqlIntegrationCurrent`: `sqlite-source-completion-regressions.trx`6passed/0skipped SHA256 `991DEFAD993E811A05CF56F990296DF5D85DF04A0D518AECFFDDB84407F2D25E`; `sqlite-source-completion-outbox.trx`15passed/0skipped SHA256 `BD3991D393F5029D11FEA9048B0D84DAF6B7C2FCEC2B797AC2D10E66030FC557`. Runs overlap; do not sum them as distinct cases. New test source: `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/ReplicaBaselineCompletionTests.cs`.

Owned source/target DBs and WAL/SHM/application files were removed after successful teardown; absence verified. `LocalState/COSMIC/SqliteBaselineCompletionCurrent/fixture.json`407B retains cleaned ownership evidence. Existing runtimes/caches reused, no new services/downloads. Task-only, no deployment.


## Neo4j source-side fenced completion — pending release

`NeoHolonReplicationOutbox.CompleteInitializedBaselineAsync` now uses the existing native transaction owner. It locks the reviewed obligation, validates the frozen payload/source operation/attempt/digest, target-bound baseline fingerprint and exact current held head, and requires affirmative no-unresolved-native-execution evidence. Source completion and checksum-bound acknowledgement properties commit together on that node. Original payload/error/recovery evidence remains intact. Exact replay rolls back the observational transaction and returns the existing acknowledgement without rewriting timestamps. Changed/absent receipts, stale source and invalid state refuse; unknown commit outcomes retain the existing reconciliation behavior.

Two native Neo4j-to-SQLite cases pass/no skips: actual source1..4with enrollment at4, real target lineage rejection, direct and existing durable-coordinator completion, absent/changed receipt rejection, stale attempt refusal, pre-write cancellation, competing native clients, exact reopened-client/completed-journal replay, and ordinary5delivery. The target independently loads version5and the source owner is checked. No public repair grant, new recovery owner or automatic future-write cutover was introduced.

Source/test: `NeoHolonReplicationOutbox.Initialization.cs` and `Neo4jSourceBaselineCompletionTests.cs`. Existing runner mode `Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicSourceCompletion` owns the retained environment/build lease composition; use one outer provider build lease. Evidence `TestResults/Neo4jCurrent/neo4j-source-completion-current.trx`, SHA256 `579AD59BF37232304B4DA8C756C8C4283EA5393A9D8F24176492111548CE70DC`. No new runtime/image/restore/download.

The first consumer build exposed missing shipping CQL source dependencies in the integration project; exact PublicReadCatalogue/HostedSources/HostedQueryExamples files are now linked, without stubs. It failed before native startup/writes; original failure log remains under TestResults/HostedCqlIntegrationCurrent.

Owned source root/history/receipts/outbox nodes were deleted by exact journalled identities and absence rechecked; owned SQLite target/WAL/SHM/application files removed. Global counters/schema/independent records remain reusable. Durable `LocalState/COSMIC/Neo4jSourceCompletionCurrent/fixture.json`520B remains cleaned=true. Owned attached process37680 exited; exact retained container/image is stopped, with no listeners17687/17688. Broader hosted routes, operator/API isolation and automatic cutover remain red.


## Reviewed Neo4j-to-SQLite host composition — pending release

The existing shell-only command now supports exactly two reviewed source profiles: omitted `SourceProvider` retains MongoDB; explicit `Neo4jOASIS` selects the configured Neo source. All other names/aliases/default/provider types are rejected before native composition. Target remains the existing configured SQLite file. Native endpoints/credentials come exclusively from node DNA; no URI, password or connection string is accepted in the request.

The optional source field is omitted when null during serialization, preserving the original ten-field request and policy binding for previously reviewed Mongo operations/journals. Neo source identity hashes the configured absolute endpoint and principal (not password); it is not a databaseUUID claim. The exact frozen source operation/digest must match before target activation. Both routes use the same preparation policy, durable application and native fenced completion owners; there is no new public repair endpoint or failover/retry path.

20API/schema cases pass/no skips, including legacy serialization, unsupported-profile rejection before native I/O, endpoint/principal binding and the earlier strict request controls. One new native reviewed Neo-to-SQLite case passes/no skips: actual held4, wrong configured identity/digest refusal, reviewed application/completion, byte-unchanged completed-journal replay, unchanged failure evidence, exact receipt binding and ordinary5delivery. The source queue must be empty for that test target before writes, so it cannot claim an unrelated obligation. Existing direct/durable source-completion proofs remain separate.

Evidence: `TestResults/HostedCqlIntegrationCurrent/replica-reviewed-source.trx` SHA256 `E2FBEEE5478DACEA2A7D19FF4491D9493A4AD65B38562406D0E44893F9721996`; `TestResults/Neo4jCurrent/neo4j-reviewed-source-repair-current.trx` SHA256 `9FEBA42290C72D41A6687D30FA4096813C854028F57301A08C2A2543D7C99324`. Runner `Run-ReusableNeo4jTests.ps1 -CosmicReviewedSourceRepair` under one outer provider build lease.

Owned native IDs and SQLite/application files were removed and absence rechecked; retained container stopped/attached process45344exited. No hosted repair/deployment or new installation. Hosted Neo route acceptance, SQLite-source operator routes, public/operator API isolation, larger-store admission and automatic enrollment/cutover remain incomplete.


## Reviewed SQLite-to-Mongo host composition — pending release

The same shell command now enrolls a third explicit route, SQLite-to-Mongo. `SourceProvider: SQLLiteDBOASIS`, `TargetProvider: MongoDBOASIS`, `TargetStoreIdentity` and null `TargetFile` bind this route; endpoints/credentials remain node-owned. Omitted optional fields retain the original ten-field Mongo-to-SQLite serialization/binding. Neo-to-SQLite remains supported. Same-store and all other route pairs fail before native composition.

SQLite source identity hashes its existing configured file. Both source and target files (where applicable) must already exist; memory/replacement files are refused. Mongo target identity pins configured servers/database, and the command checks that database exists before activation can create collections. A reviewed missing target cannot silently create another environment. Source completion delegates to its native SQLite owner; target initialization/recovery remains the existing immutable-receipt owner. Target and source resources close independently on every exit.

21API/schema controls pass/no skips, including preserved legacy binding and explicit route matrix. One native SQLite-to-Mongo reviewed-command case passes/no skips: actual source1..4/enroll4, real lineage rejection, wrong target identity and nonexistent reviewed database refusal (database remains absent), durable reviewed application/completion, exact journal replay, preserved original failure and ordinary5delivery with target version/owner verified. This is local/native host composition, not hosted SQLite-source or public-API authorization proof.

Evidence: `TestResults/HostedCqlIntegrationCurrent/sqlite-source-host-controls.trx` SHA256 `B5505773C431B06F223010B2100393E47DAEEA8806968940EB7330FFF4A647C2`; `TestResults/ReplicaRepairNativeCurrent/sqlite-mongo-repair.trx` SHA256 `DFFFE0AA32F772033E31055ED0373125A3024FE354C9C8CBD8DBC66026B6644C`. Runner `Scripts/Sandbox/Test-CqlReplicaBaselineRepair.ps1 -SqliteSource` owns its existing native/build lease; do not nest another lease. Its original mode now filters only the original Mongo-to-SQLite case, preserving one-pass/no-skip counts.

Owned target DB `cosmic_sqlite_source_baseline_target` was dropped only after marker verification and absence rechecked. Owned persistent source/application files under `LocalState/COSMIC/SqliteSourceRepairNative` removed after closure; cleaned fixture454B and lock0B remain. Mongo27029stopped/no listener; reusable runtime/data directory retained. No new install/download/environment or hosted mutation. Remaining routes/hosted acceptance/operator isolation and automatic cutover stay red.
