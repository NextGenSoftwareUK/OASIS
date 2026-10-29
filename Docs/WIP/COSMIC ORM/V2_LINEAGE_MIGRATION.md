# HyperDrive V2 lineage migration

Status: migration execution remains red. The bounded source audit is implemented;
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

1. Native target replica-head inventory with protected snapshot/revision evidence,
   including hard deletes, plus source receipt/history proof for exact graph lineage.
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
