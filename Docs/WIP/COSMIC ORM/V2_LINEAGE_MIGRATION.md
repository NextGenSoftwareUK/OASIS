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
`HolonReplicaHeadReadRequest`: explicit expected physical store plus1â€“256 distinct
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
