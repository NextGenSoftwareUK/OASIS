# HyperDrive V2 lineage migration

Status: migration execution remains red. The bounded source audit and SQLite target-head reader are implemented;
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

1. Mongo/Neo native target-head readers, SQLite hard-delete/concurrent-change audit
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
