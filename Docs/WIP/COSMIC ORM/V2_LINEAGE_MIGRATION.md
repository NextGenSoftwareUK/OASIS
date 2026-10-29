# HyperDrive V2 lineage migration

Status: migration execution remains red. The bounded source audit and SQLite/Mongo/Neo target-head readers are implemented (with explicit consistency);
no automatic retarget, old-head relabel, state reset or historical payload rewrite.
Legacy HyperDrive V1 is documentation-only and outside implementation scope.

## Implemented first gate: read-only source audit

`HolonReplicationMigrationAudit.ReadPageAsync` lives in Shared so all native bound
outbox providers can use it. The caller supplies the provider's previously captured
physical identity; the audit never captures/initializes metadata or discovers a
destination. It opens only the bound source outbox, reads one bounded route-head
page, validates ordering/source descriptors, and returns redacted observations.

```csharp
// expectedSource is an existing host-owned physical binding, not request input.
// source implements IBoundHolonReplicationOutboxSource.
var audit = await HolonReplicationMigrationAudit.ReadPageAsync(
    source,
    expectedSource,
    pageSize: 32,
    afterTargetId: null,
    cancellationToken: cancellationToken);

if (audit.IsError)
    throw new InvalidOperationException(audit.Message, audit.Exception);

foreach (var head in audit.Result.Heads)
{
    Console.WriteLine($"{head.OperationId}: {head.State}, attempts={head.AttemptCount}");
    Console.WriteLine($"Binding proof required: {head.RequiresNativeBindingProof}");
}

// A later explicit page request may use audit.Result.NextAfterTargetId.
// This is not a cross-page snapshot or permission to apply a migration.
Console.WriteLine(audit.Result.ObservationDigest);
```

The digest binds the format, physical source, input/output cursors and exact
redacted head identities/states/attempts. An unchanged page has the same digest;
a changed observation has a different one. It is an observation checksum, not an
operator signature, authorization token or durable migration receipt.

A missing NativeRoute means physical destination proof is still required; the
audit does not infer identity from a provider enum, target name or current DNA.
Older explicit bound transport may also lack a complete descriptor and therefore
needs separate proof. A present descriptor establishes provenance only: current
policy admission and target-head evidence remain mandatory. Business payloads,
endpoint configuration, claim tokens and exception text are excluded from rows.

## Verification

- 11focused controls: Pending/InFlight/ReconciliationRequired retained, no claim/
  renew/finish/unbound calls, native code/cause retained, wrong physical source
  rejected without partial results, exact pagination/digest, bounds and cancellation.
- Full Shared1327/1327pass; no skipped cases.
- Native Neo admission6/6pass. Before worker delivery, the audit reports the exact
  source/operation/target, pending0 and whether descriptor proof is missing. Frozen
  payload remains byte-for-byte unchanged and native inspection still readsPending0.
  These native checks cover queued audit observations, not every blocked-state audit
  or all providers; controlled blocked-state coverage is separate.

## Remaining execution gates

1. SQLite/Mongo hard-delete and all-three concurrent-change audit
   acceptance, plus source receipt/history proof for exact graph lineage. The new
   SQLite exact-ID reader is a first verified target-evidence capability, not full
   source/target preflight or a receipt substitute.
2. Explicit operator authorization and immutable preview binding source/target,
   policy, old/new lineage, expected head revisions and every affected obligation.
3. Atomic per-store compare-and-swap plus durable idempotent migration audit receipts.
   Revalidate after preview; reject competing changes instead of replaying writes.
4. Recovery between source/target commits without a global transaction: preserve
   original receipts/payloads, classify pending outcomes and require exact evidence.
5. Quiescent cutover, old pending obligation disposition and post-cutover delivery
   acceptance on SQLite, Mongo and Neo. Do not clear a blocked obligation merely
   because the provider recovered, or treat a matching latest record as a receipt.
6. Native failure/race/restart tests and an operator runbook before rollout. No
   production lineage adoption/rebind is enabled by the current audit utility.

Raw evidence stays under worktree TestResults. These are milestone-specific hashes:

- `TestResults/OwnedRoutingCurrent/migration-audit-focused.trx`: 11passed; SHA256 `860ADFE62D6F9104F2CD729BD42EA687BDD692D0263E2118333D02893022FA2F`.

- `TestResults/OwnedRoutingCurrent/migration-audit-full.trx`: 1327passed; SHA256 `49E31ACBDDE0F45EEA7D313FFF45D20BDF40CD5D05FA2469E7AE06991D95B82C`.

- `TestResults/Neo4jCurrent/neo4j-hosted-admission-current.trx`: 6passed; SHA256 `C238FE69DBCEF1E2599C4C7EC794A891F2B2894C68561E72DE5CCC9823CD051F`.

## SQLite target-head evidence checkpoint

Core `f317bfa` defines `IHolonReplicaHeadAuditStorageProvider` and an immutable
`HolonReplicaHeadReadRequest`: explicit expected physical store plus1–256 distinct
nonempty Holon IDs. The result returns records in request order, protected head
lineage/version/revision/digest and current-record presence; no business graph.
An absent head is explicitly separate from a missing local record or receipt.

SQLite implements it with a separate existing-file read-only connection and one
deferred transaction. The actual durable store identity is checked inside that
snapshot before head/current-record reads. It cannot create the file, schema or
identity. A wrong physical store, invalid checksum/snapshot or current revision
mismatch returns a real error without partial observations. Cancellation does not
become an empty successful result. No public HTTP operator endpoint was added.

Shared `HolonReplicaHeadRecord` now owns protected snapshot/header validation for
both SQLite replica writes and audit reads, avoiding a second interpretation of
lineage, deletion and provider revisions. Existing replica lifecycle cohorts pass
with this validator. Mongo/Neo parser/reader integration remains open.

```csharp
// expectedTarget is a previously captured, host-owned physical binding.
var request = new HolonReplicaHeadReadRequest(expectedTarget, holonIds);
var evidence = await target.ReadHolonReplicaHeadsAsync(request, cancellationToken);
if (evidence.IsError)
    throw new InvalidOperationException(evidence.Message, evidence.Exception);

foreach (var record in evidence.Result.Records)
{
    Console.WriteLine($"{record.HolonId}: current={record.HasCurrentRecord}");
    Console.WriteLine(record.Head?.SnapshotDigest ?? "No replica head observed");
}
```

Verification: Core both targets build/4449Core tests,1338Shared tests (11new
protected-header/request controls),1825native SQLite/227SQLite unit tests all pass.
Native `-CosmicReplicaHeadAudit`1/1 passes in6seconds against the actual SQLite
replica of a Neo save. It verifies present/missing records, native revision/hash,
wrong-store refusal, corrupted-checksum refusal with no partial result, exact hash
restoration, and existing shipping-worker delivery/restart/idempotence. The fixture
restores only its own temporary corrupted hash before verified exact cleanup.
Native audit hard deletes, concurrent changes, source receipt proof and full
migration still require acceptance; ordinary head evidence does not authorize
rebind, replay, state reset or historical rewriting.

- `TestResults/OwnedRoutingCurrent/replica-head-core.trx`: 4449passed/0failed/0skipped; SHA256 `4349878E95C1D7CB635243FE19C9F90C1F9C48CCA7CC7E238B0DFE7DE9816268`.

- `TestResults/OwnedRoutingCurrent/replica-head-shared.trx`: 1338passed/0failed/0skipped; SHA256 `774973DC570E62F9AE78CC7A4FA31D7BE961A6431B3D6505937EA4E8397BE02B`.

- `TestResults/Neo4jCurrent/neo4j-sqlite-replica-head-audit-current.trx`: 1passed/0failed/0skipped; SHA256 `307F6AFA7133DC595772BC60B59897DEB5F43687A63A5FC778D8BAE396144292`.

- `TestResults/SQLiteCurrent/cosmic-production-native-current.trx`: 1825passed/0failed/0skipped; SHA256 `AFC3E06ADC4BA92868AD309DEDD8B6AEA13FD13DF7EF431E4B923EE4BE13F341`.

- `TestResults/SQLiteCurrent/cosmic-production-unit-current.trx`: 227passed/0failed/0skipped; SHA256 `3CBCED51DA29075853A7498631BBFEBC791668C4273C884533DA32F8CC5225D4`.

## Mongo target-head evidence checkpoint

Mongo now implements the same exact-ID audit contract. Its repository's actual
database client owns a snapshot transaction; native collectionUUID admission and
head/current-revision reads share that transaction. No listCollections preflight,
store initialization, write, commit or automatic retry is used. Heads stream in
one-record batches; current reads project only identity/revision fields. A mismatch,
corrupt header/checksum or divergent current revision rejects the whole result.
Known observations survive read-only session cleanup failures as warnings.
Mongo replica writes and audit reads now share the protected-header validator.

Native `Run-CosmicBoundSourceWorkerTests.ps1 -NativeMongoHeadAudit` passes1/1 in13s:
real outage/new-save/delivery recovery, exact present/missing head observations,
physical UUID mismatch, corrupt checksum rejection, exact CAS restoration and
unchanged receipts/data. Modern Mongo controlled suite628 and Shared1340pass.
A stale no-I/O test double was corrected: database.Client is a local handle lookup;
actual session calls still fail/count. A new control proves session ownership
comes from the native database rather than a mutable context client.

`Run-CosmicMongoDestinationTests.ps1 -ReplicaLifecycle` passes2native sync/async
cases (existing-head updates, graph lineage, rollback, soft/hard deletes, uncertain
commit receipt proof/replay). Default catalogue mode passes6native cases. The
runner now selects each mode explicitly, requires fresh exact-count reports and
enables native test commands only on its owned local node. Lifecycle tests check
that prerequisite before creating data.

The first lifecycle attempt lacked native test commands and preserved its synthetic
source/target databases. Read-only inspection1/1 captured108554bytes at
`LocalState/COSMIC/OwnedRoutingCurrent/mongo-replica-lifecycle-failure.json`, SHA256
`13BD5E08EA2260AF089B6FB86C752F89A8D648467055A25EA2707D3AE9A80C7E`.
Explicit reconciliation1/1 verified every collection UUID/document and protected
receipt digest against that pinned snapshot before dropping only those2private
fixture databases. No source save or target replay was used for cleanup. The
snapshot and completed `.cleanup.json` receipt are retained; fresh native reruns
then passed. An initial output-directory/cleanup diagnosis was incorrect: a new
runner mode had an unset filename and the reports remained intact. That runner
error and its ledger entry are corrected.

The auxiliary older TestProjects/MongoOASIS.UnitTests project has missing restore
assets and stale unsupported-operation expectations; it was not restored, changed
or counted as passing. This does not replace the current628-test provider suite.
Neo head reads, native hard-delete/concurrent-change audit acceptance and complete
source-receipt/operator migration proof remain open. Fixture cleanup is not a
production migration or permission to rewrite lineage.

- `TestResults/OwnedRoutingCurrent/mongo-head-shared.trx`: 1340passed; SHA256 `DF8CE5E9BA77C5756C6C84A92EF00ABFD5A2ADD7211553C7454205861559F63E`.

- `TestResults/OwnedRoutingCurrent/mongo-head-unit.trx`: 628passed; SHA256 `980DB713DD3165BD56CD026ECD3FA2E5CF21CADD8CA5F38DDA4ED5E9FA74BF1C`.

- `TestResults/OwnedRoutingCurrent/worker-offline-head-audit.trx`: 1passed; SHA256 `3A13B9288ACEFB9CE4319140EC08E8864D778C9EB634340C812006936252787C`.

- `TestResults/OwnedRoutingCurrent/mongo-replica-lifecycle.trx`: 2passed; SHA256 `D7D3301FC845CE7BE56CC3BEF3139FDAE1D612C64C1D0F30148A808F2A48A10C`.

- `TestResults/OwnedRoutingCurrent/mongo-destination-catalogue.trx`: 6passed; SHA256 `FB59E345E34BB9CAA972B165E725A1E5BCEBD90501F2D3D5CB7124998C4A7439`.

- `TestResults/OwnedRoutingCurrent/mongo-replica-lifecycle-inspection.trx`: 1passed; SHA256 `C64A60434818C299B6AFAEF6061AF60CDA95A254C754B6909CDB1821A6090BEA`.

- `TestResults/OwnedRoutingCurrent/mongo-replica-lifecycle-reconciliation.trx`: 1passed; SHA256 `D88AC1D754EB617EE4F6E894938D7D6C6173A36A23711B26DFB9D5A9B5BF7F95`.

- `TestResults/OwnedRoutingCurrent/mongo-head-unit-admission-fake-failure.trx`: 7failed/620passed before test-double repair; SHA256 `89F9EFC2C7E2844DBC988B81A7CDF9CF4D4C26F921D63E7FE1112C1F3191931F`.

- `TestResults/OwnedRoutingCurrent/mongo-replica-lifecycle-failpoint-prerequisite-failure.trx`: 2failed before environment prerequisite repair; SHA256 `886A572513E6C1742E16E6FD4330913DAE10019F211636A004EB0202CC75683A`.

## Neo head reader and explicit consistency

Core `209d478` retains NativeSnapshot as the default request/response guarantee.
A caller may explicitly opt into ReadCommittedValidated; results report their
actual guarantee. Existing SQLite/Mongo constructors still return native snapshots.
No provider may silently satisfy a strict request with a weaker observation.

Neo's default read-committed isolation does not guarantee repeatable reads.
[Official Neo4j concurrency documentation](https://neo4j.com/docs/operations-manual/current/database-internals/concurrent-data-access/).
The new read-only Neo reader therefore rejects NativeSnapshot before native I/O.
It does not acquire write locks or retry queries to simulate stronger isolation.

```csharp
// Choose this only when the operator workflow accepts read-committed evidence
// and will revalidate exact revisions with conditional writes before cutover.
var request = new HolonReplicaHeadReadRequest(
    expectedNeoStore,
    holonIds,
    HolonReplicaHeadReadConsistency.ReadCommittedValidated);

var evidence = await neo.ReadHolonReplicaHeadsAsync(request, cancellationToken);
if (evidence.IsError)
    throw new InvalidOperationException(evidence.Message, evidence.Exception);

Console.WriteLine(evidence.Result.Consistency);
```

Native database identity is verified in the same explicit read transaction before
one exact-ID head/current-record query. Canonical snapshots, protected hashes,
lineage/revisions and current-record presence are checked. Inconsistent observations,
wrong store, duplicates or missing requested identities reject the entire result.
A known observation retains cleanup errors as warnings; cancellation/failed reads
do not become successful absence. Neo writes and audit reads share the same header
validator as SQLite/Mongo. These observations are not an operator migration grant
or a common snapshot spanning several stores.

Verification: both Core targets build;4449Core/1343Shared/616Neo unit controls pass.
The native6-case replica cohort passes in10seconds. Its4sync/history lifecycle cases
now audit live and hard-deleted records, explicit missing IDs, strict refusal,
read-committed labeling, physical-store mismatch and one corrupt-checksum/exact-CAS
restoration. Existing graph lineage, rollback, versions and receipt/restart checks
also pass. A stale seven-constraint test fixture caused56initial unit failures;
it now models the actual8-constraint schema, with2controls proving7is insufficient.
The production schema requirement was not relaxed.

The native cohort's SQLite source and atomic pre-write intent journal now use
`LocalState/COSMIC/Neo4jCurrent/committed-replica-source.db` and
`committed-replica-source.pending.json`. Failed outcomes survive build-output
cleanup. Successful tests remove only their owned graph records/receipts/history
and source/journal after closing providers. The retained graph/sequence/runtime
survives. No new runtime, restore or environment was introduced.

Source-receipt proof, concurrent-change acceptance and guarded migration execution
remain open. Neo's weaker explicitly chosen evidence must be accounted for there;
these tests do not establish a native repeatable snapshot capability.

- `TestResults/OwnedRoutingCurrent/neo-head-core.trx`: 4449passed; SHA256 `632E41802EC4855D429B800DA3DB3A6E5C01F2FF577E019DA9CD57C775018718`.

- `TestResults/OwnedRoutingCurrent/neo-head-shared.trx`: 1343passed; SHA256 `BFF528A0D2E08A0DA9588E34295E6685BD0340333305C706F7635E589541DA7B`.

- `TestResults/OwnedRoutingCurrent/neo-head-unit.trx`: 616passed; SHA256 `E64EC7FD373352D7682D2EBD2DBAA3371897BE366269DEC51EC2210B808A04DD`.

- `TestResults/Neo4jCurrent/neo4j-cosmic-replica-current.trx`: 6passed; SHA256 `6E228B7265C7562CB198A9867B1D02E48322896A0BC2632E55473D01BA165414`.

- `TestResults/OwnedRoutingCurrent/neo-head-unit-schema-fake-failure.trx`: 56failed/558passed before test-fixture correction; SHA256 `5CF94502613D745CC4A25A3EE7EAE3771CCD3EF3A03014ACEEAFB6D8E5FDA341`.

## Source-bound original receipt checkpoint (29 October)

`IBoundConditionalHolonWriteRecoveryStorageProvider` adds
`ReadBoundConditionalHolonWriteOutcomeAsync(original, expectedSource, token)`.
SQLite, MongoDB and Neo4j implement it through their existing receipt validators.
The explicit physical source is checked in the same native read transaction as
receipt lookup. It is separate from the immutable original intent: older type-only
operations retain their original fingerprint and enrollment. An already-bound
operation cannot be checked against a different enrolled identity.

Null/foreign bindings, a different native store, cancellation or invalid receipts
return an error with no outcome. Absent remains unknown, not permission to replay.
No identity capture, schema creation, claim or original write occurs. Neo4j uses
its existing read-committed transaction; this is not a cross-store snapshot.

Verified: both Core targets and all three provider builds; Core4462/4462,
Neo619/619 controlled tests, SQLite2/2 native copied-file cases (bound and historical
type-only intents). SQLite covers absent/committed/null/cancelled checks and
unchanged copied-store data version. The same historical receipt verifies only
when the caller explicitly supplies that copy's actual identity. This establishes
where the receipt was observed, not that the copy is an authorized new authority.
No native Mongo/Neo validation of the new overload is claimed yet.

Next: native Mongo/Neo bound historical-receipt tests, then redacted selected-record
lineage/content evidence and source/target preflight. Operator authorization,
atomic migration receipts, cutover and recovery remain red.

Evidence: worktree `TestResults/DevelopmentSyncCurrent/bound-source-{core,neo,sqlite}.trx`.
- TestResults/DevelopmentSyncCurrent/bound-source-core.trx SHA256 756C6BF18EC3F29BB874F7C799F3CED97A7606F613DCEDB980DBC2E89306C668.
- TestResults/DevelopmentSyncCurrent/bound-source-neo.trx SHA256 38B120421A67ACD39698E9DBB5B1024FBCAF4EE43658DC1F362E426C38693E90.
- TestResults/DevelopmentSyncCurrent/bound-source-sqlite.trx SHA256 EC875469CAF8F8A254217014FD8A467500C065C09220AF4325BBA805BB4D11EE.

## Native source-bound receipt verification (29 October)

Mongo2/2 native cases cover historical type-only and already-bound original intents.
Both verify absent/committed states, keep the original enrollment unchanged, reject
wrong source identities/cancellation, and preserve receipt/outbox bytes. The bound
lookup uses the activated repository even if the public Database descriptor is
cleared. Dropping/recreating the actual Holon collection while retaining the old
receipt rejects the old physical source; the lookup does not repair a missing
collection. Run `Scripts/TestHosts/Run-CosmicMongoDestinationTests.ps1 -SourceReceipt`.

Neo2/2 native cases cover the same historical/bound intent distinction, wrong
source and foreign actor rejection, redaction, unchanged graph/receipt/queue and
receipt recovery after provider close/reopen. Run
`Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicSourceReceipt` inside the standard
provider build lease. This proves read-committed receipt lookup, not process-crash
recovery, native database replacement or snapshot isolation.

Existing Mongo data and Neo container reused; successful exact fixtures removed,
private Mongo databases verified absent, pending journals removed, both services
stopped and Neo attachment joined. An initial Neo pre-service build check correctly
rejected a stale Shared copy; the runner now builds its normal consumers before
running this new cohort. No stale DLL bypass, new runtime or environment used.

This closes the new receipt-read capability's three-database native gate. It does
not authorize migration: selected-record lineage/content comparison, concurrent
change checks, preview authorization, atomic migration receipts and cutover remain red.
- TestResults/OwnedRoutingCurrent/mongo-bound-source-receipt.trx SHA256 173E1788BCF47E0C5A50E1E72981167AB33471BE2341B73121A85F440B3BE033.
- TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx SHA256 D90E4F353CF2E1947F13C44E897EB4295AB5377243D2DD559EC8FCFD9064ECCA.

## Selected receipt content evidence (29 October)

`IConditionalHolonReceiptEvidenceStorageProvider` is now implemented by SQLite,
MongoDB and Neo4j. The original native receipt owner performs its complete existing
integrity/actor/intent/topology validation, then projects only 1–256 requested IDs.
The full original receipt is validated even for a subset; the projection limit is
not a limit on receipt size. Unknown/duplicate/out-of-scope IDs are rejected.

```csharp
// Both identities were previously captured and approved by the host.
// This reads evidence; it never executes the original save or captures identities.
var request = new ConditionalHolonReceiptEvidenceRequest(
    original,
    expectedSource,
    intendedTarget,
    selectedHolonIds);

var receipt = await source.ReadConditionalHolonReceiptEvidenceAsync(
    request,
    cancellationToken);

if (receipt.IsError)
    throw new InvalidOperationException(receipt.Message, receipt.Exception);

// Absent means unknown. It never grants permission to replay a save.
if (receipt.Result.ReceiptState == ConditionalHolonReceiptState.Committed)
{
    var evidence = receipt.Result.ReceiptEvidence;
    Console.WriteLine(evidence.OriginalSelectedRecordCount);
    Console.WriteLine(evidence.Records.Count);
}
```

The redacted evidence binds operation/actor/operation kind, actual source binding,
intended target binding/authority, original selected-record count and ordered IDs.
Each record includes logical lineage, source provider revision, hard-delete state
and a versioned portable-content digest. No graph, business values or historical
membership list is returned. A subset cannot be mistaken for the complete save.
A comparison target is an explicit hashing intention; the source read does not
contact that target or prove its current physical identity.

`CommittedHolonReplicaPlan.PortableContentDigest` owns the shared canonicalizer:
`cosmic-portable-record-v1` binds target authority and normalized logical content.
It reuses replica acknowledgement rules, excluding native storage keys/revisions
and target-local provider metadata. Other portable metadata, business values,
logical lineage and audit content remain bound. Existing immutable intent hashes
and stored receipts are unchanged. Protected-head readers now return the same
portable digest/format after validating their native snapshot checksum/header.
Existing public constructors retain their original signatures for binary consumers;
legacy head observations may lack a digest and cannot prove portable equality.

Verified: Core4462, Shared1360 (17new controls), SQLite2/Mongo2/Neo2 native source
projection cases, shipping GraphQL1; all pass, no skips. Six controls compare live/
hard-deleted receipt and protected-head digests across all three target authorities.
Native projection tests select a declared comparison target without contacting it;
these are not source-to-target migration/cutover proofs. Neo retains its documented
read-committed source observation. No cross-store snapshot or authorization granted.

Exact next step: read-only source/target comparison report with strict identity,
operation, scope and consistency validation, explicit missing/lineage/content
mismatch classifications, then native pairs. Guarded execution remains red.
- TestResults/DevelopmentSyncCurrent/receipt-evidence-core.trx SHA256 C4F21D608AF4FCA4882BFF59AE1C04C2ABAC99BDA20DCC4375B871BE3B38CB19.
- TestResults/DevelopmentSyncCurrent/receipt-evidence-shared.trx SHA256 F26769032586F20C8E5FC45E9018AFACA83153D085300798AEC8E60863ED5638.
- TestResults/DevelopmentSyncCurrent/receipt-evidence-sqlite.trx SHA256 AF229B2499F2322879E5BBF905424B7C473483DD9A3C21149C309423CA38A48D.
- TestResults/DevelopmentSyncCurrent/receipt-evidence-graphql.trx SHA256 9CB37B9E2262714D1CC88642BB83BABFE649F611DB8B3F1D13BC88281DC20F89.
- TestResults/OwnedRoutingCurrent/mongo-bound-source-receipt.trx SHA256 B84B95F97D737AFD768F9E69F5E007518A99714FD71FC4C1BC383399EF13AE48.
- TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx SHA256 D6D7CE81A849F17348ACF7A103BAECB386F3CE473BD207B90EA352E3DD7D3B76.

## Read-only source/target comparison report (29 October)

`HolonReplicationReceiptPreflight.ReadAsync` verifies the original bound receipt
before contacting the explicitly bound target-head reader. `Compare` validates
separately obtained observations through the same rules. Neither method captures
identities, claims work, writes data, retries a save or grants migration permission.

```csharp
var request = new ConditionalHolonReceiptEvidenceRequest(
    original,
    expectedSource,
    expectedTarget,
    selectedHolonIds);

var comparison = await HolonReplicationReceiptPreflight.ReadAsync(
    source,
    target,
    request,
    cancellationToken: cancellationToken);

if (comparison.IsError)
    throw new InvalidOperationException(comparison.Message, comparison.Exception);

foreach (var row in comparison.Result.Records)
    Console.WriteLine($"{row.Original.HolonId}: {row.Kind}");

Console.WriteLine(comparison.Result.IsCompleteOriginalGraph);
Console.WriteLine(comparison.Result.ObservationDigest);
```

The target defaults to NativeSnapshot. A Neo target requires explicit
`targetConsistency: HolonReplicaHeadReadConsistency.ReadCommittedValidated`;
a weaker result cannot satisfy a strict request. A Neo source retains its
read-committed immutable-receipt observation, independently of target consistency.
The two reads do not form a global snapshot.

Rows distinguish MatchesOriginalReceipt, MissingReplicaHead, UntrackedCurrentRecord,
SourceLineageConflict, LogicalVersionConflict, DeletionConflict, ContentConflict and
PortableContentEvidenceMissing. A local record without a protected head is never
inferred to be a replica. Provider revision equality across different stores is
not required; each native reader verifies its own protected revision.

Wrong operation/actor/store/identity scope, absent receipt or insufficient consistency
returns an error without partial rows. A mismatch in verified content/lineage instead
returns an explicit comparison row. Original source lineage must come from the
original replication enrollment. Old evidence without an original-input binding is
insufficient. The source projection now binds the unchanged supplied input using
`ConditionalHolonPlan`'s existing canonical fingerprint; this observation binding is
separate from the native persisted SQLite receipt hash. Changing the original input
or its enrollment after observation invalidates the evidence.

The versioned redacted report includes source/target authorities and physical
fingerprints, operation/actor, original input fingerprint, original graph count,
requested/actual target consistency, ordered records and an observation checksum.
The checksum binds observed target revisions even when portable content is equal.
It is reproducible from the documented ordered report fields, not a signature or
operator authorization. A subset explicitly reports IsCompleteOriginalGraph=false.
Source/target revisions and obligations must still be rechecked atomically at cutover.

Verified: Core4462, Shared1380 (20new preflight controls), shipping GraphQL1;
SQLite2 native copied-file preflights classify untracked current records without
changing the copy's data version. Mongo1 native cohort proves missing -> matching ->
newer-version conflict -> matching later receipt -> matching hard-delete, with
unchanged source/target graph/head/receipt/queue snapshots during reads. Neo2 native
cohorts (historical/already-bound intents) prove actual Neo -> separate SQLite
receiver missing/matching states and stable observation checksums. No skipped cases.
Native cohorts preceded the final report-only serialization field layout; final
serialization/digest binding is covered by the Shared controls. These do not certify
all ordered provider pairs, concurrent mutation windows or migration execution.

Commands: existing Mongo runner `-MigrationPreflight`, Neo runner
`-CosmicSourceReceipt` under the provider build lease, and the selected SQLite
`CopiedSourceReceiptCannotVerifyOrReplayOriginalBoundOperation` cohort.

Next: remaining native SQLite/Mongo/Neo pair/race/hard-delete comparisons, then an
immutable authorized operator preview and per-store CAS/migration receipts. Quiescent
cutover, crash recovery and pending-obligation disposition remain red.
- TestResults/DevelopmentSyncCurrent/receipt-preflight-core.trx SHA256 B746B07BB98CABDC0FC74C511E6E1692B8D8A1BD20B7DF7FAF1D1060F7B97FB1.
- TestResults/DevelopmentSyncCurrent/receipt-preflight-shared.trx SHA256 FDE4197BA683062AEF16F04E3635FA34F231645FC0E68C1DA3A9AA3A2A3112B1.
- TestResults/DevelopmentSyncCurrent/receipt-preflight-sqlite.trx SHA256 EB17AEB2BC4E92220E1E382EC4D808DDE3AFE7B4ADB4ED624BC3DBD86AE040EE.
- TestResults/DevelopmentSyncCurrent/receipt-preflight-graphql.trx SHA256 2028834FD6AC0B440FAED98027FBD2885634B96834F18B5904A8FB3E5E357F9D.
- TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx SHA256 1A10509DB7D9E702D1B8B6B40A1C18322049B5C5745D452EE166F51896ADC5D3.
- TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx SHA256 69F325FAD72E656BB4B7E64A7BF463196A899CD5FFDADA6B974423D7FB01394C.

## Current-body divergence bug fixed (29 October)

A native SQLite -> Mongo witness reproduced a false MatchesOriginalReceipt result:
the target Holon.Name changed while its logical/provider revisions and protected
head remained unchanged. The previous reader projected only revision fields, so
comparison certified the old protected snapshot without checking the current body.
The preserved characterization report records the explicit BUG WITNESS; that
characterization assertion was replaced by rejection assertions in shipping tests.

Root fix: `HolonReplicaHeadRecord.ValidateCurrent` now verifies current identity,
logical/previous versions, target-owned revision and portable business/audit content
against the checksum-validated protected head. Hard-deleted heads require no live
record. All three V2 audit readers use this one validator. Native keys and target-local
provider metadata retain their existing allowed local-observation semantics.

Mongo streams complete native bodies one at a time (batch size1), hydrates through
the native mapper and retains only redacted heads/identity membership. SQLite uses
an untracked mapper context attached to the exact existing read-only connection
and snapshot transaction; it does not consult the writer context or initialize
schema. Neo verifies its parsed current snapshot within the existing explicit
read-committed audit scope. No legacy save code was changed, no repair/replay occurs.

Verification: Shared1392 (12new current-body controls) and Neo619 controls pass.
Native SQLite -> Mongo2 proves matching copies, hard-delete and rejection of a
native body edit that leaves the token unchanged. Neo content/race3 comprises two
Neo -> SQLite historical/bound cases with SQLite native body edits rejected, plus
one SQLite -> Neo case: strict read refused, explicit weak read matched, then an
independent target edit between source and target reads rejected without repair.
The Neo race edits both the valid stored snapshot and its Name lookup projection,
leaving its revision untouched. Native Mongo graph/receipt/head lifecycle2 regression
also passes, including live/hard-deleted audit observations. No skipped cases.

Commands: Mongo runner `-SqlitePreflight` and `-ReplicaLifecycle`; Neo runner
`-CosmicPreflightRace` under the build lease. Tests reuse fixed journaled datasets;
all successful exact fixture data is removed and services stopped afterward.

This closes this reproduced current-body false-match bug. It does not prove every
ordered source/target pair or all internal read interleavings. Observation reports
remain unsigned read-only evidence, never authorization to rebind a replica.
Next: immutable authorized operator preview, then per-store CAS/migration receipts
and interrupted-cutover recovery, with remaining pair/race acceptance still open.
- TestResults/OwnedRoutingCurrent/sqlite-mongo-content-drift-witness.trx SHA256 7156998895DE3F34350A9E93297934255CE221907D3B1E844A0382FFD0FF116C.
- TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx SHA256 02A60577C3B50FABE250BA4FA80BCD0160D982D59B1195214063A38EDDD49F30.
- TestResults/Neo4jCurrent/neo4j-receipt-preflight-content-race.trx SHA256 A836AC98D860685F35762236E70B7D721D14C39DDF43A45EF2BC30D6B22D1EDD.
- TestResults/OwnedRoutingCurrent/mongo-replica-lifecycle.trx SHA256 BB02F1D1779781E14FCD8DCA2D5992A858A8A1D6AC5AE0A4DF297D9E8FFC9D55.
- TestResults/DevelopmentSyncCurrent/current-content-shared.trx SHA256 FC289C80414D50E7A23E66059B63DCEA87C2DA309B8905BB9CD283327FD5076C.
- TestResults/DevelopmentSyncCurrent/current-content-neo-controls.trx SHA256 A6B92D59F60248AB5342B67AA8730D4408F32ADB62CC98B7B619EF68E94F2D3F.

## Complete original-operation inventory (29 October)

Route heads are not an exhaustive obligation list: they omit Completed work and
later operations queued behind the oldest target head. The new optional Core
`IHolonReplicationOperationInventoryOutbox.ReadOperationObligationsAsync` exposes
all states for one exact original operation, on SQLite, Mongo and Neo. Obtain it
from the explicitly bound source factory; unbound instances refuse the capability.

```csharp
var opened = source.GetBoundHolonReplicationOutbox(expectedSource);

if (opened.IsError || opened.Result is not IHolonReplicationOperationInventoryOutbox inventory)
    throw new InvalidOperationException("Complete operation inventory is unavailable.");

var request = new HolonReplicationOperationReadRequest(
    expectedSource,
    originalOperationId,
    pageSize: 32);

var page = await inventory.ReadOperationObligationsAsync(request, cancellationToken);

if (page.IsError)
    throw new InvalidOperationException(page.Message, page.Exception);

foreach (var obligation in page.Result.Obligations)
    Console.WriteLine($"{obligation.TargetId}: {obligation.State}");

// Use this cursor in a separate explicit request if more targets remain.
Console.WriteLine(page.Result.NextAfterTargetId);
```

Pages bind physical source, operation, exact cursor/limit, required/actual consistency,
ordered obligations and next cursor. Rows expose source/actor/target, sequence/state/
attempts, payload checksum, validated frozen route and a state fingerprint. Hidden
lease token/worker/deadline, retry/completion times and native recovery-evidence
checksum participate in that fingerprint. Renewing a lease changes it even when
state/attempt count remain unchanged. Business payloads, claim tokens, worker IDs,
transaction lists and exception messages are excluded from the returned evidence.
Unknown operation means empty observation, never permission to recreate obligations.

SQLite uses an existing-file read-only connection and native snapshot; Mongo uses
the existing bound snapshot owner and batch-size1 payload validation. Neo requires
explicit ReadCommittedValidated opt-in. Completed rows are validated as well.
Each page is independent: no cross-page/global snapshot or quiescence is implied.

### Neo Unicode cursor root cause and recovery

Native inspection found all4 ASCII/BMP/astral obligations, while scalar cursor
filtering returned only3. The pinned runtime's scalar string comparison and native
range-index ordering disagree for supplementary Unicode. The exact-operation reader
now explicitly uses its existing operation/target range index for cursor requests,
aligning admission with indexed ordering. No plugin/index/runtime was installed;
there is no managed business-payload sort, error-triggered alternative or replay.

The preserved fixture was inspected without mutation: original source receipt
Committed,4 payload checksums/identities intact, one Completed/three Pending. After
the index-plan fix the same fixture returns all4. Reconciliation verified the
SQLite receiver's original replica receipt, protected head and current identity,
then removed only that owned fixture. A fresh historical/bound Neo cohort passed.
The failure, inspection and reconciliation evidence remains in TestResults.

Verified: Core4462/Shared1404 (12new controls)/GraphQL1; native SQLite2/Mongo1/Neo2,
plus reviewed-failure reconciliation1 pass, no skips. Native cases exercise4 mixed
ASCII/BMP/astral targets, page-size1, completed/later/non-head obligations, stable
checksums, hidden renewal changes, unknown operation, wrong source and cancellation.
Neo strict consistency is explicitly refused. Source saves were not replayed.

**Related remaining V2 gap:** the existing global Neo route-head reader still uses
the scalar cursor predicate. Correct its native index/order plan and shared ordering
validation before relying on Unicode route discovery. This checkpoint fixes the
exact-operation reader, not that global reader. Source-wide lineage inventory/
quiescence, authorized previews, original target-receipt proof for pending-obligation
disposition, native CAS/migration receipts and interrupted cutover remain open.

- TestResults/DevelopmentSyncCurrent/operation-inventory-core.trx SHA256 7690BF7897C206682ACE7E55391954726C024BC54FDEAE605A67A74D059E1FB3.
- TestResults/DevelopmentSyncCurrent/operation-inventory-shared.trx SHA256 8DB58F5252944F1385B72DEF910B2BAE0993DAF5FCC2CE38372263DCFC24860D.
- TestResults/DevelopmentSyncCurrent/operation-inventory-graphql.trx SHA256 5276E6E69FAF51EFBADA18B8DF2BE26DFC890830EF3EE69DA1620B3B19EBD7AF.
- TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx SHA256 B5B53295D0F52800B3E5801230EBAC16DB65BD77E0C8139129481A5C87CA2CA5.
- TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx SHA256 C9EEEA3F8BE144266F753E9D7523C60E604658F8F222055A5C3BD6F45749E1E4.
- TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx SHA256 57CB3C10FE3D17F481B655DC5FCF62882FF1022BD648D1992414F003CE79DA39.
- TestResults/Neo4jCurrent/neo4j-operation-inventory-unicode-failure.trx SHA256 46FC3D369D9A35333B86A24EFD3108FF437C2267590E4CCD6FB9F5D8E1AA27B5.
- TestResults/Neo4jCurrent/neo4j-operation-inventory-inspection.trx SHA256 AD6C23B84077FE64698DBB9E9EA4392332E6BB7DB000424B0ADAE7A21D954AB8.
- TestResults/Neo4jCurrent/neo4j-operation-inventory-reconciliation.trx SHA256 B6EE642F6536D40B6BD2697177CDDC150CD97115FE9F7A1CF4FF79699DFFBC32.
- TestResults/Neo4jCurrent/operation-inventory-unicode-inspect.json SHA256 E51B85ED74C4C054348432E656FD7FAE611B85388016846D75AEB31292E77FF6.
- TestResults/Neo4jCurrent/operation-inventory-unicode-reconcile.json SHA256 FC9791D218AAD629AC24E22C7F353EE7AC6ED4DF6BBDBB03B7AE085859033074.

## Global route-head Unicode discovery fixed (29 October)

The related existing V2 Neo route-head reader now uses a native target-range index
for cursor admission, matching native indexed ordering rather than the pinned
runtime's inconsistent scalar string predicate. The oldest-unfinished-per-target
contract remains intact: this is route discovery, not complete obligation inventory.

Provider activation owns `oasis_cosmic_outbox_target_order` (RANGE on
CosmicHolonOutbox.TargetId). It accepts an existing equivalent online index by shape,
creates only missing ordering schema, resolves the actual index name, waits through
the bounded native population procedure and verifies ONLINE state before readiness.
A name collision, denied creation, missing/wrong shape or population failure rejects
activation. The eight existing uniqueness guarantees remain unchanged. Read-only
inventory never initializes schema and does not switch to another query on failure.

`HolonReplicationRoutePage.CompareTargetIds` is the single Core owner for native
UTF8/binary target ordering. Exact-operation page validation and Shared migration
audit use it; the audit no longer applies UTF16 ordinal ordering to native Unicode
pages. Native fixture probes now read the global audit at page-size1 and require
all unfinished original targets to remain discoverable, even after newer operations
are queued. This exercises ASCII, BMP private-use and supplementary characters.

Verified: Neo625 (6new schema controls), Shared1405 (native-order audit control),
Core4462 and GraphQL1 pass; native Neo2, SQLite2 and Mongo1 pass with global discovery
checks alongside complete-operation/receipt/content verification. No skipped cases.
The new ordering guarantee is installed and checked on the retained Neo graph;
its exact equivalent name was not separately inventoried, so the resource identity
is the required RANGE/label/property shape in that existing container/database.

This closes the reported Unicode route-discovery gap within those scopes. It does
not establish source-wide quiescence, a cross-page snapshot, operator authorization
or migration execution. Next: complete lineage scope/fencing, immutable authorized
preview binding all affected obligations and original target receipts, then native
CAS/migration receipts and interrupted-cutover recovery. V1 remains documentation-only.
- TestResults/DevelopmentSyncCurrent/global-route-unicode-core.trx SHA256 468EE9F4D4FEBB793FB30D08E8DF4DB49A2348BD5D31E713DA5D8841AD8CD150.
- TestResults/DevelopmentSyncCurrent/global-route-unicode-shared.trx SHA256 4C78AC1EDC7FA754F3D0876AB9CA461F3FB79BA7A31D550ECBAACE1993C94C84.
- TestResults/DevelopmentSyncCurrent/global-route-unicode-neo.trx SHA256 53CEE7A353C4299544A813F289F3B25944698E15C119EE77909FBC87F4B1037B.
- TestResults/DevelopmentSyncCurrent/global-route-unicode-graphql.trx SHA256 C2DA7C030FF63C7E03E98D8B4CDE28CCC041DD48BF8CED01EDB037CDEA0FAF13.
- TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx SHA256 6560DAE50032E921051B7EFC5726397AD7187F81DB8E180C237957FB12C9E9EB.
- TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx SHA256 6E8F96829F5C1638F1E549C6ED268FB5AA032F9681C98E84DEBDA3BDF5649964.
- TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx SHA256 93E1B313D058C3DA7C4B114AA46BFC7C685B92BFB320735866EA21E962AF3EB7.


## Source-wide sequence windows — 29 October 2026

Implemented `IHolonReplicationLineageInventoryOutbox` on SQLite, MongoDB and Neo4j. The first bounded page captures the upper sequence for one exact physical source and lineage; continuation retains that boundary. Completed obligations remain visible. Shared operation observation validation and fingerprints are reused. Native tests append a legitimate second source operation between pages: the captured scan excludes it, while a fresh scan includes it. Neo requires explicit read-committed opt-in; no global snapshot is claimed.

This is read-only evidence. It does not freeze earlier states, acquire a write/delivery fence, authorize a migration, or prove original replica receipts. Next: an immutable authorized preview and native revalidation/fences, then durable idempotent cutover and old-obligation disposition.

| Check | Passed | Evidence path (worktree relative) | SHA256 |
|---|---|---|---|
| Core | 4462/4462 | `TestResults/DevelopmentSyncCurrent/lineage-inventory-core.trx` | `5AFA42F84587E15EA41CA1A9AF477AFED50631370D702F48B6DC31C56A787FC8` |
| Shared | 1416/1416 | `TestResults/DevelopmentSyncCurrent/lineage-inventory-shared.trx` | `390A5B3CAA3161FF7814BE1438C727B0EAE68A5C23A2B6B50B76B715EB6BBAA8` |
| GraphQL | 1/1 | `TestResults/DevelopmentSyncCurrent/lineage-inventory-graphql.trx` | `36D85395548034528169A1FD2B147A694010EF3EBFCCB1D94042A1BC030D047E` |
| SQLite | 2/2 | `TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx` | `4101551779C9D2525F1A10770D008D828D051E8A82E56C0F458E1902E47D80C3` |
| Mongo | 1/1 | `TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx` | `3034ED5AC7160CD1EA65BFBD748B4D1C736BBD52A784D0A6D32A9AAE92171D76` |
| Neo | 2/2 | `TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx` | `7BF13776AADFC91FC1D5D6315062AE9AA306919BC51BFE5434811281BC7B062C` |

Owned fixture journals and temporary receiver files are absent; native fixture cleanup verified, Mongo/Neo ports have no listeners. Reusable runtime/data resources retained.


## Exact operator authorization — 29 October 2026

Shared now defines a separate `IHolonReplicationMigrationAuthorizationPolicy`.
`ScopedHolonReplicationMigrationPolicy` is an immutable host-owned allowlist;
an empty configuration denies every request. Ordinary committed-owner replication
permission does not implement this administrative contract.

Each host grant binds the authenticated operator, migration ID, exact physical
source and original target, original source/target IDs, replacement native route
(including its target store and replication policy), preview digest and
administrative policy revision. All are included in `ContextDigest`. Grants have
an inclusive start and exclusive expiry. The host must derive the operator from
its authenticated context and create grants through its administrative process;
HTTP callers must never supply the grant list. Replace the registered policy to
revoke grants; execution must reauthorize immediately before native mutation.

```csharp
// Host-owned configuration following operator review; never construct grants
// directly from an untrusted request body.
var grant = new HolonReplicationMigrationGrant(
    reviewedContext,
    reviewTime,
    reviewTime.AddMinutes(5));

var policy = new ScopedHolonReplicationMigrationPolicy(
    reviewedContext.AuthorizationPolicyBinding,
    new[] { grant });

var admitted = await policy.AuthorizeAsync(
    contextFromAuthenticatedHost,
    cancellationToken);

if (admitted.IsError || !admitted.Result)
    throw new InvalidOperationException(admitted.Message);

// Admission alone cannot execute a migration. The native executor still needs
// complete verified evidence, write/delivery fences, CAS and durable receipts.
```

This checkpoint supplies authorization, not a complete preview generator or an
execution endpoint. `PreviewDigest` is the exact reviewed checksum; this policy
does not certify the completeness or truth of the evidence it identifies. The
future preview builder and native executor must establish that independently.
There is no automatic registration, migration write or change to ordinary saves.
The current contract retains one physical source while rebinding lineage/routes;
cross-source relocation needs its own explicitly reviewed protocol.

Verification: 14 new controls, full Shared1430/1430, no failures/skips. Cases
reject changed operator/migration/source/old or new target store/lineage/route
policy/administrative policy/preview, missing grants, early/expired grants,
cancellation, mismatched host policy, duplicate grants and invalid context.
The caller's mutable grant list cannot alter a registered policy.
Evidence: `TestResults/DevelopmentSyncCurrent/migration-authorization-shared.trx`;
SHA256 `B00E8D2222328A64E71027AFC295ACB4CAB563AB5D6E369AD65C59908F274A08`.


## Immutable review preview — 29 October 2026

`HolonReplicationMigrationPreviewReader.ReadAsync` now consumes the native
source-wide sequence-window reader and the existing exact receipt/target
preflight. Shared owns orchestration for all providers. It retains every route
and every state in the captured source lineage, including completed obligations,
then obtains original receipt evidence for each operation on the selected route.
The host supplies the exact historical-intent reader; the preview never replays
an intent, initializes a binding, claims delivery or applies a mutation.

The preview rejects missing pages, inconsistent source/window/consistency,
backtracking sequences, duplicate operation/target pairs, changed operation
actor/payload observations, exceeded explicit bounds, missing receipts and wrong
receipt operation/actor/lineage/physical source/target. No partial preview is
returned on failure. Defaults: 64 rows/page, 4096 obligations/review; explicit
maximum 65536. Increase a review bound deliberately; no silent truncation.

The immutable preview checksum binds physical source/original target, old IDs,
replacement native route/policy, captured upper sequence/consistency, complete
observed obligation rows/state digests, receipt comparison digests and findings.
`preview.AuthorizationContext(migrationId, authenticatedOperator, adminPolicy)`
connects that exact checksum to the separate host-owned administrative policy.
No business values, leases or worker identities are included.

Findings remain visible for active deliveries, partial original graph evidence
and conflicting current heads. Four execution gates are always declared by this
version: original replica receipts unverified, original payload-to-receipt binding
unverified, native write/delivery fences required, and replacement-target evidence
unverified. A matching latest head cannot close any of these. Approval of the
review does not authorize ignoring them. There is no mutation endpoint or
executable plan yet; explicit fences/CAS/idempotent receipts/cutover remain red.

Tests: 23 new preview controls; full Shared1453/1453, no skipped/failing tests.
They include concurrent appends excluded by the captured window, changed preview
digests, in-flight delivery findings, wrong evidence scope, page substitution,
review bounds, native failures and cancellation before/during evidence reads.
Native SQLite2/Mongo1/Neo2 cases each collect all four original obligations with
page-size1, retain the completed target, compare a stable second preview, and
preserve unresolved execution findings. Mongo also verifies unchanged native
source/target documents. These are native inventory/receipt/preview proofs, not
all-pair migration execution acceptance. The same-authority Mongo fixture uses
an explicitly synthetic distinct-authority replacement proposal named
`preview-only-not-contacted`; it is never contacted or admitted. Other fixtures
use their captured target as the proposed replacement. Replacement admission is
not claimed in any case.

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| Shared | 1453/1453 | `TestResults/DevelopmentSyncCurrent/migration-preview-shared.trx` | `8A76A54E7A3CD6D5676DAAFCBFF61588F7AC67AB29F6877784219FEF656CC886` |
| SQLite source / Mongo target | 2/2 | `TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx` | `7FC8219BDBA221FFD455AEB71E3EF005061CEBCA94E524A30BC3441E903757B6` |
| Mongo source / Mongo target | 1/1 | `TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx` | `063DFE55815B330B0549F041B3A5E42573159376E5255E10F8D8E6867573B411` |
| Neo source / SQLite target | 2/2 | `TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx` | `6F8398A3D2CF4AE2B31BBD6FCF0415B039547FE95094341256A6D36BD68A074D` |

Native batch71146 and final Shared controls41065 exited0. Neo attachment13160
joined/stopped. Exact pending journals/receiver files/sidecars absent; ports27029
and17687 have no listeners. Free disk102800048128bytes. No new environment,
installation, download or restore. Detailed resource lifecycle remains in the
existing cleanup ledger; raw evidence remains in TestResults.

Next: expose read-only exact historical replica-receipt evidence and verify each
original frozen payload against its original source receipt. Feed those proofs
into preview findings; then implement native write/delivery fencing and CAS with
durable migration receipts. Do not interpret sequence windows as quiescence or
current-head matches as historical delivery receipts.


## Historical replica receipt capability — SQLite first, 29 October 2026

Core adds the optional `ICommittedHolonReplicaReceiptStorageProvider` capability.
A request carries the unchanged original committed transport, exact physical
receiver and explicit required consistency. The redacted outcome binds original
operation/actor/source/operation kind, receiver fingerprint, canonical original
intent fingerprint, full selected-record count, observation consistency and
verified acknowledgement checksum. Receipt absence is explicit and never grants
permission to replay, cancel, reset or migrate an obligation.

Shared `CommittedHolonReplicaReceiptProjection` uses the existing
`CommittedHolonReplicaPlan` and `ConditionalHolonReceipt.Read` owners. It validates
all selected graph identities/topology, committed logical content/audit/lineage,
target-owned revision and existing receipt checksum. Review is bounded to1–256
selected records. A changed original intent, partial receipt, wrong native
fingerprint, tampered checksum or forged acknowledgement returns no proof.
Existing fingerprint semantics are preserved; this does not change stored
historical bytes or add a second acknowledgement validator.

SQLite is the first native implementation: a separate existing-file read-only
connection and deferred snapshot validate physical identity and read the exact
original receipt in the same scope. It never reads today's business record/head,
initializes identity/schema or executes the original mutation. Missing stores,
wrong bindings and unverifiable receipts return explicit errors.

```csharp
// originalTransport must be the actual historical committed transport,
// not a new request made from today's destination record.
var observed = await receiver.ReadCommittedHolonReplicaReceiptAsync(
    new CommittedHolonReplicaReceiptReadRequest(
        originalTransport,
        capturedReceiverIdentity),
    cancellationToken);

if (observed.IsError)
    throw new InvalidOperationException(observed.Message);

Console.WriteLine(observed.Result.ReceiptState);
// Committed proves that exact canonical historical delivery, not current content.
// Absent does not prove rejection or permit replay.
```

Native Neo-source/SQLite-receiver cases verify Absent before delivery, Committed
after delivery, wrong physical-store rejection, and the same original receipt
checksum after a later source save and an independent current-target edit. The
current-head reader correctly rejects that changed current body while the
historical receipt remains verifiable. No write replay is used for observation.
Full Shared1462/1462 (9new controls), native2/2 pass; Core declared-target and
Shared/SQLite/provider-runner builds pass. An initially omitted repository
interface declaration and test codec-overload mismatch were fixed before native
execution; no failed/unknown database mutation resulted from those build errors.

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| Shared | 1462/1462 | `TestResults/DevelopmentSyncCurrent/replica-receipt-shared.trx` | `EEF060046D3D2BD68562BE965ABA7760E74EFBAB0B20254C85C6D5C0B8CEC7B7` |
| Neo source / SQLite historical receiver | 2/2 | `TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx` | `57851E8DCEF8A73EB4D5CF2A05F2FB0F431D74F5100641D80E8B7A19FAF0E02F` |

Native runner48843 exited0; Neo attachment53244 stopped/joined. Exact SQLite
receiver DB/sidecars and pending journal absent; port17687 has no listener.
Reusable Neo runtime/container and schema retained. No new download, restore,
installation or environment. Logs/raw evidence remain TestResults.

Mongo and Neo receiver implementations are still open. The immutable preview
still declares original replica receipts unverified until this capability is
connected for every selected operation/target and its returned scope is checked.
Original payload-to-source-receipt binding, replacement admission, native fences,
CAS/idempotent migration receipts and cutover also remain open.


## Mongo/Neo historical replica readers — 29 October 2026

All three native providers now implement the optional historical replica-receipt
read capability. Mongo uses one read-only snapshot transaction: the existing
UUID-bound physical-store validator acquires the actual native Holon namespace,
then the exact historical receipt is read in that scope. Neo uses its activated
driver/lifecycle gate and a read-only transaction, with native database identity
validated before the receipt query. Neo explicitly rejects NativeSnapshot and
requires ReadCommittedValidated opt-in. Neither reader commits, claims delivery,
initializes identity/schema, reads current heads or replays the mutation.

Both use Shared's existing plan/receipt validator and return only redacted
historical evidence. Existing native scope cleanup semantics are preserved:
an observed result survives cleanup failure as a warning; an unobserved or
invalid outcome returns a real error, with cancellation and cleanup causes kept.

Native Mongo cases cover Absent before initial and hard-delete delivery,
Committed after delivery, exact operation/actor/source/target scope, wrong-store
rejection and cancellation. An independent current-body edit invalidates the
head audit but leaves the original historical receipt provable.
Native Neo cases cover the same admission/absence/commit proof with explicit
weaker consistency; the original acknowledgement remains unchanged after later
logical mutations, deletions and failure cases. Four Neo receiver cases cover
sync/async plus history enabled/disabled; the runner also retains two SQLite
receiver controls. These are receipt checks, not migration execution or global
snapshot certification. Shared1462 controls are retained from the preceding
checkpoint; no Shared implementation changed in this native-reader checkpoint.

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| SQLite source / Mongo historical receiver | 2/2 | `TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx` | `CEF9C9D72244254413E9E46DE19D547F8BB660B840BBE5A1D81CC1BF4EFAB200` |
| Native replica group: 4Neo receipt cases + 2retained SQLite cases | 6/6 | `TestResults/Neo4jCurrent/neo4j-cosmic-replica-current.trx` | `7C791BD3100F77CE4116201B9843324DD20D5C85C26436D4B46DA15D7745A0EC` |

Native batch77454 exited0; Neo attachment46712 stopped/joined. Pending fixture
journals and temporary SQLite source/receiver files are absent; ports27029 and
17687 have no listeners. Neo free bytes before102803189760/after102753132544;
observed102753202176after batch. No new download, restore, installation or
environment; reusable Mongo/Neo runtimes/data and SDK/cache retained.

Next: integrate per-operation exact receiver evidence into the immutable preview.
An explicit Absent observation must remain a finding, and wrong actor/operation/
lineage/target evidence must reject the preview. Original frozen-payload/source
receipt binding remains separate proof; neither a valid historical replica
receipt nor a matching current head establishes it. Native fences, replacement
admission, CAS/idempotent cutover and recovery remain open.


## Preview v2 includes historical receiver evidence — 29 October 2026

`HolonReplicationMigrationPreviewReader.ReadAsync` now accepts an explicit
host-owned `readOriginalReplicaReceipt` callback. For every selected-route
operation it verifies historical receiver operation/actor/source ID/authority,
physical target, operation kind and full original selected-record count against
the source obligation and source-receipt report. Missing/error/wrong-scope evidence
rejects the review without a partial result; native errors/causes remain intact.

The immutable preview now exposes `ReplicaReceipts`. Its format is
`cosmic-lineage-migration-preview-v2`; the review checksum binds all these redacted
outcomes (including exact intent/acknowledgement checksums, state and actual
consistency), plus the explicitly required receiver consistency. Old review
checksums must not be reused as approval of a new v2 review.

NativeSnapshot remains the receiver-evidence default. Weaker Neo historical reads
require explicit `replicaReceiptConsistency: ReadCommittedValidated`; a weaker
outcome cannot silently meet a strict review request. The authenticated operator
context continues to bind the exact preview digest and administrative policy.

Three distinct states remain clear:
- No historical reader supplied: `OriginalReplicaReceiptsUnverified` remains.
- A valid native Absent outcome: `OriginalReplicaReceiptAbsent` remains, even
  when the source obligation claims Completed. Absence does not permit replay.
- All selected original receipts observed Committed: the blanket unverified
  finding is removed, while original payload/source-receipt equivalence,
  replacement admission and native fences remain explicit independent gates.

The callback must use the real unchanged historical transport. It must never
construct a replacement transport from today's destination record. The current
API supplies an audit review with visible missing evidence when the optional
reader is absent; it does not try another writer or authorize execution.

Developer integration (host owns both the historical registry and captured store):
```csharp
// Within the existing preview reader call:
readOriginalReplicaReceipt: (obligation, token) =>
    receiver.ReadCommittedHolonReplicaReceiptAsync(
        new CommittedHolonReplicaReceiptReadRequest(
            originalTransports[obligation.OperationId],
            capturedReceiverIdentity,
            requiredReceiverConsistency),
        token),
replicaReceiptConsistency: requiredReceiverConsistency
```

Verification: 11new controls, full Shared1473/1473. Controls cover scope substitution,
Absent-after-Completed, changed evidence digest, retained independent findings,
strict/weaker consistency and native read failure with cause/no partial result.
Native SQLite2/Mongo1/Neo-source2 cases collect all four obligations and include
one exact historical receiver receipt in stable repeated previews. Mongo and
SQLite are the native receivers in these combined tests; the Neo receiver's
four direct historical-reader cases remain proved by the preceding checkpoint.
No all-pair migration execution or simultaneous source/target snapshot claim.

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| Shared | 1473/1473 | `TestResults/DevelopmentSyncCurrent/preview-receipts-shared.trx` | `CF8F2A93DBBE0445E49E54A3EFD49BCEFC3ADE117D7594F054D6A7FF6B53A6A3` |
| SQLite source / Mongo receiver | 2/2 | `TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx` | `46FE835235781F5E1F8FEB0AF7F7AB21CFFBF64378EB87B4AF0C903D7233D49A` |
| Mongo source / Mongo receiver | 1/1 | `TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx` | `0AA4A8FE143BD5C63D3C541C27A3A2C03A23F8E40B6A904774EE41ACCB6FC1C1` |
| Neo source / SQLite receiver | 2/2 | `TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx` | `8F428B78BFC96739578ED889FD6CB9AAFBED82146397C0FB4483A6FBA6DF13D2` |

Native batch50228 exited0; Neo attachment22000 joined/stopped. Exact pending
journals/receiver DBs/sidecars absent and ports27029/17687 have no listeners.
Neo free before102413889536/after102373027840bytes. No new installation, download,
restore or environment; reusable SDK/cache/database runtimes/data retained.

Next: verify the original frozen outbox transport against the original source
receipt, including graph membership, logical/source revisions and portable values.
Then implement native write/delivery fences, replacement-target admission,
per-store CAS, durable idempotent migration receipts and interrupted cutover.
Migration mutation remains disabled.


## Frozen transport/source receipt equivalence — 29 October 2026

Core's optional `IHolonReplicationPayloadAuditOutbox` is implemented by SQLite,
Mongo and Neo. It reads exactly one existing frozen obligation in a native
read-only scope, with an expected physical source and immutable inventory row.
The existing payload owner validates the stored envelope/checksum, and Shared's
observation check requires unchanged sequence/operation/actor/source/target,
payload checksum and complete hidden-state checksum. A changed lease or retry/
completion/recovery state invalidates the read rather than claiming/resetting it.
Neo rejects strict snapshot requests and requires explicit weak-read opt-in.

This is a HOST-ONLY raw transport read containing business data. It must not be
exposed over HTTP, written to audit reports or logged. The migration layer uses
it transiently and retains only redacted proof. No identity/schema initialization,
claim, delivery, replay, relabel, historical receipt rewrite or state clearing.

Shared `HolonReplicationPayloadReceiptVerifier.Verify` checks the original source
intent fingerprint/enrollment against the verified complete source-receipt report,
then checks exact queued bytes against the observed payload checksum. It reuses
the existing payload codec, graph planner, route descriptor codec and portable
content canonicalizer. It verifies operation/actor/source, selected target,
operation kind, selection flags/depth, native descriptor meaning, root order,
complete membership/child-edge order, each logical version/predecessor, source
provider revision, deletion kind and portable content against receipt evidence.
No equivalent proof is issued for a partial source-receipt graph.

The returned `HolonReplicationPayloadReceiptProof` contains no graph or business
values. It binds original obligation/state/payload checksums, physical source and
target fingerprints, source receipt observation, original input fingerprint and
the frozen transport's target-specific replica intent fingerprint. Native fixture
integration also compares that replica intent with the historical receiver's
validated original-intent fingerprint. This closes the logical chain from source
save to queued bytes to exact receiver acknowledgement for those tested cases.
The verifier itself is a pure validator, not native attestation: hosts must first
obtain bytes through the exact bound audit read, use verified receipt evidence,
and check the resulting scope. Native reads are separate observations, not fences.

Verification: 12new controls, full Shared1485/1485. Rehashed valid transports with
changed content, source revision, logical version, selected graph/root structure,
parent or source identity are rejected. Native descriptor policy changes, raw
checksum changes, partial source receipt and changed original actor are rejected.
Valid legacy type-only and descriptor-bearing transports produce stable redacted
proof; changed hidden lease evidence invalidates exact observation comparison.

Native SQLite2/Mongo1/Neo2 cases verify unchanged completed original payloads,
source-receipt equivalence and equality with actual historical receiver intent.
Each source also renews a claimed fixture lease and confirms the old observation
can no longer read raw payload; delivery can then finish under the real owner.
Wrong physical source is rejected and Neo strict reads are explicitly refused.
Native equivalence cases cover the existing one-record original create with four
obligations; unit controls cover the two-record selected graph. This is not full
native graph/delete/race or all-pair migration execution certification.

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| Shared | 1485/1485 | `TestResults/DevelopmentSyncCurrent/payload-proof-shared.trx` | `52269BF00D79FD22818E1CB1FD6DA7BAE42F5A2533757E5536BC17FE8B8E4A66` |
| SQLite source | 2/2 | `TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx` | `7474B9E2805979C8D726F78E1CBC07E6531607A82FE228C108BA286B8BD0C600` |
| Mongo source | 1/1 | `TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx` | `27905E8F25DC18A23BBF446AB15F0205B50D900993C4D60F81DD38EA38DCE1E7` |
| Neo source | 2/2 | `TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx` | `A29BD0D902718741DCE0107B4382FF947E765100B38545383C72B41387449980` |

Native batch13013 and final controls65943 exited0; Neo attachment44776 joined/
stopped. Exact pending journals/receiver DBs/sidecars absent; ports27029/17687 no
listeners. Final free102087045120bytes; Neo before102383046656/after102097076224.
Existing runtime/data/SDK/cache retained; no download/install/restore/environment.
Initial missing namespace import and an invalid parent-mutation test fixture were
corrected before final controls; no failed native outcome resulted.

The production preview still retains `OriginalPayloadReceiptBindingUnverified`:
next integrate these proofs per selected operation, bind them into its digest and
compare their replica intent with historical receiver evidence. Native fencing,
replacement admission, CAS/idempotent migration receipts and cutover remain open.


## Preview v3 binds the source/queue/receiver evidence chain — 29 October 2026

`HolonReplicationPayloadReceiptVerifier.ReadAsync` now orchestrates the host-only
bound source read and pure equivalence validator. It rejects a receipt naming
another physical source before the raw read, holds raw business transport only
transiently, and returns only redacted proof. Native failures/cancellation yield
no proof; native cleanup warnings and error causes/counts remain visible. Proof
binds the explicitly requested source consistency; it is not a global snapshot.

`HolonReplicationMigrationPreviewReader` now accepts an explicit host-owned
`readPayloadReceiptProof` callback. It validates each proof against the exact
operation/actor/sequence/target, physical source/target, observed payload/state
checksums, original input fingerprint and source-receipt observation. When
historical receiver evidence is supplied, its canonical original intent must
equal the frozen transport's target-specific replica intent. A valid proof for
another obligation or another state cannot substitute for this review.
Strict source review refuses weaker proof unless explicitly opted in.

Preview format is now `cosmic-lineage-migration-preview-v3`; `PayloadProofs` and
all their redacted bindings enter the review fingerprint and therefore the
operator authorization context. Existing v2 approvals must not be reused for a
new v3 preview. Only complete per-operation proof coverage removes the generic
`OriginalPayloadReceiptBindingUnverified` finding. No callback keeps the finding;
missing/error evidence from a supplied callback rejects the whole review without
publishing partial results. Receiver absence remains its own finding. Native
fences and replacement admission remain independent mandatory execution gates.

Host integration within the preview call:
```csharp
readPayloadReceiptProof: (obligation, receipt, token) =>
    HolonReplicationPayloadReceiptVerifier.ReadAsync(
        boundSourceOutbox,
        capturedSourceIdentity,
        obligation,
        originalSourceIntents[obligation.OperationId],
        receipt,
        requiredSourceConsistency,
        token)
```

Verification: 12new controls, full Shared1497/1497. They cover the complete chain,
changed review fingerprints, stable repeated review, different obligation/state
proof rejection, historical intent mismatch, explicit weak-source opt-in, missing
or failed proof with no partial review, bound read scope/cancellation, preserved
native error count/cause and cleanup warning. Existing review tests now use real
canonical queued intent fingerprints rather than arbitrary synthetic hashes.

Native SQLite2/Mongo1/Neo-source2 production preview cases include source payload
proof AND historical receiver evidence. Both previously unverified evidence
findings are cleared for the scoped complete create; native fence/replacement
findings stay. Combined cases remain one original record/four obligations; the
broader native graph/delete/race acceptance remains open. Direct native Neo
receiver historical proofs are retained from their preceding four-case scope.

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| Shared | 1497/1497 | `TestResults/DevelopmentSyncCurrent/preview-payload-shared.trx` | `A7E65064DC84118E03C55A246CD7FDEBA2DC94692DC1F35A99E374A84AF499B6` |
| SQLite source / Mongo receiver | 2/2 | `TestResults/OwnedRoutingCurrent/sqlite-mongo-receipt-preflight.trx` | `1219807E0A2EF8EF79A396CB20CD80E7B3CA97DAC9C259586875F3CC900BCA66` |
| Mongo source / Mongo receiver | 1/1 | `TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx` | `AF484F8ECE0FBAC073317200B9A0CE10ACDC6AB5B780A8782A8411E640AD6F46` |
| Neo source / SQLite receiver | 2/2 | `TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx` | `646F3702B098A0D1854E8E5B19D89E7285D7EF91D3C2E8A08EF0B6438DD5B454` |

Native batch79036 exited0; Neo attachment36716 joined/stopped. Exact pending
journals/receiver DBs/sidecars absent; ports27029/17687 no listeners. Neo free
before102104432640/after102063054848bytes. No new installation/download/restore/
environment; reusable runtime/data/SDK/cache retained. Raw evidence stays TestResults.

Next invariant: migration fences must cover affected V2 record writes and delivery
through old AND new lineage IDs, plus existing in-flight or unknown outcomes.
They must survive restart and cannot expire away an uncertain cutover. A route
review must not silently become permission to interrupt an entire physical store.
Revalidate exact preview/authorization and replacement identity/content under
native fencing, then CAS durable per-store receipts and recover interrupted
cutover/old obligation disposition. Migration execution is still disabled.


## First native record barrier — SQLite, 29 October 2026

Core adds `IHolonMigrationFenceStorageProvider` and canonical bounded requests
(1–4096 exact distinct record IDs). Scope binds migration/operator, physical
store, reviewed preview and administrative context digests. Input order does not
change scope meaning; mutable caller lists cannot alter it. Native observations
distinguish Absent/Held/Cancelled. Owner tokens are trusted-host operational
values: do not publish or log them. Storage primitives do not authenticate or
authorize the operator; a host coordinator must admit the exact preview first.
No coordinator/HTTP execution endpoint is enabled by this checkpoint.

SQLite creates `CosmicMigrationFence` through its existing activation schema
owner and an explicit EF migration. Downgrade refuses any retained fence history.
Each acquisition runs in BEGIN IMMEDIATE with physical identity validation and
canonical receipt verification. Identical repeated acquisition returns the same
owner; overlapping scopes from separate provider instances cannot both win.
The table stores the exact request/checksum, owner and terminal state durably.
There is no automatic expiry. Cancellation requires the exact scope/owner and
is idempotent; a cancelled migration ID cannot be reacquired. Cancelled audit
metadata is retained. This cancellation protocol is PRE-MIGRATION-MUTATION only;
no data-mutation transition exists yet. The future executor must atomically add
its started/uncertain phase before enabling mutation and bar this cancellation
path from releasing started cutover.

The common SQLite V2 atomic write path checks held record scopes under the same
native writer transaction before receipt replay or business mutation. This covers
owned save, soft/hard delete and committed replica apply/replay. Outbox claim
admission checks the selected frozen graph under its writer transaction before
lease/attempt updates. It uses record IDs, independent of old/new source lineage
strings. Unrelated record writes remain admitted. Historical receipt reads stay
available while fenced, so operators can establish actual delivery outcomes.
Activation/lifecycle errors return explicit results; uncertain native fence commit
is marked reconciliation-required and must be read by its exact original request.
A known outcome remains known with cleanup warning if scope disposal fails.

Held DOES NOT mean quiescent: prior in-flight/unknown delivery is not stopped by
source acquisition alone. Existing lease renew/finish remains possible; paired
target fences plus receipt/state reconciliation and CAS are still required.
The primitive does not claim coverage for direct SQL, legacy/unconditional APIs,
Mongo/Neo or other providers. Legacy gaps remain documentation-only by instruction;
host cutover must account for clients outside the V2 admission path.

Native two-case verification includes separate-instance concurrent acquisition
(exactly one owner and an absent loser), overlapping scope rejection, idempotent
owner recovery, ownership-preserving restart, inactive-provider rejection, old/new
lineage save rejection, soft/hard delete rejection, replica receipt replay rejection,
blocked pending claim, allowed unrelated save and historical receipt read, wrong
cancellation owner rejection, durable cancellation and post-cancel normal admission.
No remote delivery occurs in these SQLite-only protocol fixtures. Source/receiver
records are separately selected records, not native child-graph acceptance.
Shared1506/1506 adds nine canonical scope/state controls. Mongo/Neo barriers,
paired authorization, child graph/race breadth, fault/uncertain commit acceptance,
cutover, replacement admission and long-history performance remain open.

Run the fixed owned fixture (one build lease, no restore/new runtime):
```powershell
Scripts/TestHosts/Run-CosmicSqliteMigrationFenceTests.ps1
```

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| Shared contract/regressions | 1506/1506 | `TestResults/DevelopmentSyncCurrent/native-fence-shared.trx` | `D73F3B0D18917AE8B8216A5204CB40546E4AA9D3EC8AE678328E082BEF6DCAB9` |
| SQLite native barrier/race/restart | 2/2 | `TestResults/DevelopmentSyncCurrent/native-fence-sqlite.trx` | `802399A42CFA0EE223767A9288EE3242ED9D962FAC8C027BADB2326043BCB282` |

The initial fixture reached final post-cancellation claim release, but omitted
Retry's required future time. It failed on that fixture assertion; the second
case correctly refused retained state. Read-only inspection verified the original
journal, one Cancelled fence, three unchanged version1 records/receipts and one
owned InFlight claim with valid frozen checksum. Fixture code made no remote
call. After runner19754 stopped, exact isolated DB/sidecars/journal were removed
without replay, and the fixture was corrected to supply an explicit future retry.
Preserved review `TestResults/DevelopmentSyncCurrent/native-fence-initial-outcome-review.json`
SHA256 `6BA83843E7A534B89E9BF398B3030988CA020E85E77537BD766AAAB3CE6710DF`; initial failure TRX
`TestResults/DevelopmentSyncCurrent/native-fence-sqlite-initial-failure.trx`
SHA256 `C1508C73B905434FFD01A34C42CDD4D658EC89BECC7346D95E988F940DCDD1DF`. No production fence defect was inferred from that test error.

Final native61723/26183 and Shared63652 verification scopes exited0; exact fixed
`LocalState/COSMIC/MigrationFenceCurrent/fence.db`/sidecars/pending.json absent.
Last native free102084452352bytes. No network service, install/download/restore
or new environment used; reusable SDK/cache retained. Raw evidence remains
worktree TestResults; required summaries/hashes remain in this report.

Next: implement equivalent Mongo/Neo barriers and native cross-store paired
admission/recovery. Derive scope from the exact authorized preview, never widen
one route's grant to whole-store interruption. Account for existing in-flight
outcomes, replacement identity/content, native CAS and restart-safe phase receipts
before any lineage mutation. Advance source native revision on cutover to reject
outstanding pre-cutover conditional writes rather than allowing stale routing.


## Mongo native record barrier — 29 October 2026

Mongo implements the same Core record-fence storage contract as SQLite. Snapshot
reads alone are insufficient: a source save and a separately inserted fence could
both commit through write skew. Every affected V2 write and new delivery claim
therefore touches the same per-record `CosmicMigrationRecordGate` document that
fence acquisition owns, within its native snapshot transaction. Record gates are
addressed by logical Holon ID and touched in canonical order; unrelated records
have distinct gates. There is no global process-local mutex or automatic retry.
A native conflict rejects admission explicitly for a new authorized observation.

`CosmicMigrationFence` retains canonical scope/checksum, opaque host owner and
Held/Cancelled history. Acquisition, all selected record gates and cancellation
commit atomically with majority+journal acknowledgement. Owner recovery is
idempotent; cancellation validates each gate's exact migration/hash, and terminal
identities cannot be reacquired. Physical Holon namespace identity is verified
inside the transaction before fence access. Native unknown commit keeps its real
session/transaction reconciliation evidence. Known outcomes retain cleanup warnings;
cancellation before commit remains cancellation. No automatic expiry is used.

The activation schema owner initializes both metadata collections/indexes and a
versioned admission marker. V2 writes/claims require that marker in their native
transaction before touching record gates. Loss of the admission collection cannot
silently erase protection: activation checks retained fence history BEFORE any
metadata recreation and refuses the missing marker/namespace. Exact receipt
absence also rejects orphan record ownership. No read/audit path initializes it.
The synchronous initializer delegates to the same async schema implementation.

Affected V2 save/soft-delete/hard-delete and replica apply/replay are barred before
receipt replay or business mutation. Source claims are barred before lease/attempt
changes. Historical receipt reads remain available. Operation receipt replay may
touch admission records transiently, but aborts those touches and commits neither
admission nor business changes. Existing lease renew/finish is deliberately not a
claim of quiescence: paired receiver fencing and outcome reconciliation remain open.
Legacy/unconditional/raw native writes remain outside V2 implementation scope.

Native two-case acceptance verifies:
- Separate provider instances racing one scope: exactly one owner, with the loser
  explicitly rejected and no partial fence receipt.
- An independent native transaction holding the real source admission document
  makes acquisition fail with concurrent-change; rollback leaves no partial owner.
- Old/new lineage save and both delete kinds, replica receipt replay and new claims
  are fenced; unrelated writes and historical receipt reads remain available.
- Owner identity survives provider deactivation/reactivation. This is NOT a Mongo
  server-process crash/failover proof. Wrong cancellation owner is rejected;
  cancellation is durable/idempotent and restores ordinary admission.
- Dropping the admission namespace in the controlled private fixture causes V2
  admission and reactivation to fail without recreating it over retained history.
  Historical replica evidence is still readable; no metadata repair/reset is used.

The no-held-fence source receipt/preview regression also passes1case. Shared1506
and SQLite native2 controls are retained from the prior checkpoint; no Shared/Core
implementation changed in this Mongo-only checkpoint. Two protocol fixture cases
cover selected source and incoming replica records, not native child graph or all
fencing/failover/transaction-fault directions. Host operator admission, Neo barriers,
pairing, durable data-mutation phases, cutover and broad performance remain open.

```powershell
Scripts/TestHosts/Run-CosmicMongoDestinationTests.ps1 -MigrationFence
```

| Check | Passed | Worktree-relative evidence | SHA256 |
|---|---|---|---|
| Mongo record barrier/race/provider reactivation | 2/2 | `TestResults/OwnedRoutingCurrent/mongo-migration-fence.trx` | `0AE66E311ACC7AAB81F8704EFD10F50FDD0A482B10C308E464B5FA36BC4F3A71` |
| No-held-fence receipt/preview regression | 1/1 | `TestResults/OwnedRoutingCurrent/mongo-receipt-preflight.trx` | `9DE11997177E91A4913A4104FAB9AB2B17BFE4A0272691D2BFECCEDA964CC17E` |

Final native54003 and regression11474 exited0. The existing journaled private
databases cosmic_storage_identity_fixture_a/b were verified dropped by their
owner, then the extra fence journal was removed. No remote delivery occurred.
Pending journals absent, port27029 no listener. Last native free101810343936bytes;
regression prior free101819949056bytes. Existing Mongo8.3.11/oasisRecoveryRs runtime,
data directory and SDK/cache reused; no install/download/restore/environment.
Raw TRX/logs stay TestResults. Initial compiler index-overload ambiguity was fixed
with explicit native index model types before any native fixture write.

Next: implement Neo's native record admission coupling using real write locks,
not read-committed existence checks; then pair all stores under exact authorized
preview and reconcile in-flight/unknown outcomes. Migration execution remains off.

## Neo record barrier baseline, 29 October 2026

Controlled625/625 and native2/2 pass. NativeMigrationBarrierPreservesReceiptAndRejectsAffectedWrites covers separate provider instances, repeat owner, optional provider reactivation (not server restart), held receipt-replay rejection, historical receipt readability, unrelated saves, overlap rejection and exact-owner terminal cancellation. Fixture journal cleared only after exact scoped native cleanup verified zero. Evidence: TestResults/Neo4jCurrent/neo4j-migration-fence.trx SHA256 569BA50EC86CB8278E494405E8509E8BFC5602BE289DC66A352E257EA41DAD1B. Runner: Run-ReusableNeo4jTests.ps1 -CosmicMigrationFence under one external build lease. Initial runner selector incorrectly selected an existing failure-review test; fixed selector before native barrier run, no barrier fixture mutation from that attempt. Concurrent acquisition/write/delete/claim tests and paired coordinator remain open; Held is not quiescent.

Native follow-up2/2 passes concurrent same-scope acquisition through separate drivers with identical durable owner, plus held new save/soft-delete/hard-delete rejection. TRX SHA256 D33CFBC234C16F3A1AB0287750ECD74D673BF66A7D72F9C64DE2042950603092. This is not yet a write-vs-acquisition race or source-claim proof. Exact fixture cleanup succeeded.

Neo claim follow-up2/2 passes: actual source enrollment, held claim rejects COSMIC_MIGRATION_RECORD_FENCED with all queue properties unchanged, cancellation admits claim, no remote delivery sent. Same baseline assertions retained. TRX SHA256 EEB582E578569A857DA104CEAEBA41FF08993710609EEABA3F4910240412D2DD. Claim-vs-acquisition scheduling and preexisting in-flight delivery remain open.

## Neo acquisition overlap, 29 October 2026

Native2/2 passes deterministic overlap: a transparent driver decorator pauses the real acquisition before native commit without changing the captured production endpoint; SHOW TRANSACTIONS observes two operations blocked on native record gates. On release, receipt replay/new conditional save (one case each) and source claim reject COSMIC_MIGRATION_RECORD_FENCED. Existing baseline history/unrelated save/delete/cancel/reactivation/claim-state checks remain. Evidence TestResults/Neo4jCurrent/neo4j-migration-fence.trx SHA256 1FC8A2531E2FF1936F1DBC6D77D7C7BBE5A6C88CB70C2107D51DE23F146CAD22. First wrapper attempt failed physical identity capture before first save; retained journal exact operation ad1c9a62-6bb4-45d6-9209-2bc4c366e754 was reviewed through native count across exact Holon/gate/fence/receipt/outbox identities (zero), then unused journal removed. Review count and original journal retained under TestResults/Neo4jCurrent/race-prewrite-*. No native evidence rewritten or operation replayed during review.

Next: exact-preview/operator paired-store fence coordinator with durable partial/unknown acquisition recovery; preexisting in-flight delivery is not stopped by a held source fence. Replacement validation, cutover CAS, durable mutation phases, graph/restart coverage remain open.

## Shared pre-mutation barrier coordinator, 29 October 2026

HolonReplicationMigrationBarrierCoordinator.AcquireAsync accepts the immutable original preview, its exact authenticated administrator context/current policy and one explicitly selected native provider per distinct source/original-target/replacement store. It derives record identities from complete original graph evidence, rejects unresolved original payload/receiver/graph proof, coalesces identical physical stores and orders participants by binding fingerprint. In-flight delivery remains a reconciliation requirement, never permission to mutate.

Every invocation reads all exact native fence receipts before any acquire. Held scopes are retained; only absent scopes get one acquisition call, with current grant rechecked before each and at completion. Native errors/throws/cancellation retain partial states and uncertain outcomes; no retries, automatic release or compensation. Explicit subsequent invocation reconciles native receipts, including lost commit acknowledgements. Progress exposes store/scope hashes and states, never owner tokens or business payload. AllBarriersHeld is an observation only: no global snapshot, quiescence or cutover authority. Host must retain the exact reviewed preview/context for recovery; durable host evidence retention, real paired/triple native proof, cancellation after mutation-start guards, replacement validation/CAS/idempotent cutover remain open. No HTTP execution path registered.

Shared1516/1516 passes including10 new coordinator controls: two/three stores, alias coalescing, scope derivation/redaction, no reacquire on resume, lost commit acknowledgement recovery, failed/foreign reads before any acquire, cancellation/revocation partial progress, unproved/missing/wrong scope rejection, terminal cancellation, native warning preservation. Evidence TestResults/DevelopmentSyncCurrent/barrier-coordinator.trx SHA256 CC839FD6824154D1B75A47ED3116580C4D61BF920228DA5576575ED2D6F6BDF4. Controlled provider doubles establish coordinator ordering/error semantics; native primitive tests remain separate and do not prove this coordinator end-to-end.

## Native Neo + SQLite coordinator acceptance, 29 October 2026

NativeBoundOriginalReceiptPreservesHistoricalIntentAndQueue2/2 now feeds actual source save/outbox frozen payload/committed SQLite receiver receipt into the production preview and acquire-only coordinator. Both physical barriers are held; unchanged Neo source save replay and SQLite receiver replica replay reject COSMIC_MIGRATION_RECORD_FENCED while historical target receipt acknowledgement remains unchanged. Fresh Neo and SQLite provider instances recover the same scopes with IsSaved=false. Exact owner reads/cancellation release only pre-mutation scopes; the rest of the existing historical-content/window test still passes. No migration data mutation, remote delivery or host process restart is claimed.

Evidence TestResults/Neo4jCurrent/neo4j-bound-source-receipt.trx SHA256 28BB6AEDDD552E773FB265BFEC3DF06698A4C8E34B9ABC1B978FFC67F1ADE5F0. Shared1516 remains retained; native coordinator acceptance is two stores, same record, replacement aliases receiver. Mongo/three distinct stores, graph scope, native partial/unknown acknowledgement/crash and durable host evidence recovery remain open. Successful journal-owned Neo gate/fence rows removed and count zero; owned target DB/sidecars removed after disposal, source journal cleared. Shared native preview helper now returns its verified immutable preview to enable this composition rather than synthesizing a second review.
