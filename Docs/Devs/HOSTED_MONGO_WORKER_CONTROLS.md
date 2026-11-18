# Hosted Mongo worker controls

These operator controls hold automatic work during an incident. They do not
repair storage identity, grant authorization, acknowledge deliveries, delete
records, change provider routing, or prove application recovery.

| Configuration key | Applies to | Behavior when `true` |
|---|---|---|
| `OASIS_PAUSE_HYPERDRIVE_WORKERS` | Compatible WEB4 host | Omits the registered HyperDrive command, fan-out, change-capture, compaction and delivery workers. Older hosts may have four rather than five of these workers. |
| `OASIS_PAUSE_USAGE_RECONCILIATION` | Compatible WEB4 host | Omits the usage reconciliation worker; retains its repository and foreground reader. |
| `OASIS_PAUSE_USAGE_SETTLEMENT` | Compatible Core ledger composition in WEB4–WEB10 | Initializes the durable outbox, then returns from automatic settlement execution before any claim/recovery cycle. |

All default to `false`. Values use the normal configuration Boolean binder;
invalid values fail composition rather than silently enabling traffic. Keep
these non-secret settings in the environment configuration store. They are not
OASIS DNA routing defaults: preserve environment endpoints, realms, database
names and provider admission.

## Preserve foreground and durable state

Do not disable `SUBSCRIPTION_USAGE_ENABLED` as a substitute for pausing workers.
Foreground authorization, reservations, provider execution evidence, inline
settlement and outbox initialization remain required. A paused worker does not
release reserved exposure or turn an unavailable durable store into a successful
operation. Pending work remains durable for a later authorized resume.

The settlement control holds automatic execution only. An explicit
`RunOnceAsync` call is still an operator action; the flag is not a database
firewall or authorization boundary. Do not invoke such drains during a shared
cluster stand-down.

The three controls do not cover every application worker. Audit logging, seal
policy refresh, reservation expiry, billing statements/collection, gardening,
sandbox maintenance and other host-specific work require separate analysis.
Keep required policy binding, initialization, audit draining and financial
invariants when changing their lifecycle.

## Stage, deploy and verify

1. Inspect the actual serving revision of each affected service. A saved flag
   has no effect on a version which does not implement it. Preserve that
   environment's other configuration and dependency pins.
2. Stage only the intended key, merging variables rather than replacing them.
   When using Railway, `skipDeploys=true` stages the setting without initiating
   another deployment. Read back only selected non-secret fields.
3. Qualify the exact immutable source and use the established rollout cadence.
   Saved configuration is distinct from the environment loaded by an existing
   process. The controls take effect in the new host instance.
4. Confirm the deployment is successful and inspect its running instance.
   [Read-HostedUsageSettlementPause.ps1](../../Scripts/TestHosts/Read-HostedUsageSettlementPause.ps1)
   reads selected process fields, verifies the exact API executable/DLL,
   identifies its mapped Core assembly and checks the new control literal.
   It makes no application or database request and does not print full SSH,
   environment, DNA or assembly contents.
5. For a registered settlement worker, additionally verify the positive
   `USAGE_SETTLEMENT_PAUSED` startup event. A process environment or an absence
   of error logs alone does not prove the host consumed the setting. If the
   worker is not registered, establish that from the host's composition and
   feature settings; do not introduce a synthetic worker to obtain a marker.

Example for a known running instance (IDs and source must come from its actual
deployment metadata):

```powershell
./Scripts/TestHosts/Read-HostedUsageSettlementPause.ps1 `
  -Service $serviceNumber -Environment dev `
  -InstanceId $runningInstanceId -DeploymentId $deploymentId `
  -ExpectedSource $immutableSourceSha
```

Do not wake sleeping services merely to inspect them. Do not restart a queued
or building deployment because an observation timed out; continue following
the same deployment handle.

## Resume and evidence

Resume only when the incident's operator hold is lifted and the relevant
storage, policy and queue issues have been resolved. Re-enabling a worker is not
authorization to replay quarantined operations, alter lineage, fabricate an
original writer, or advance a blocked delivery. Stage the intended false value
and roll out the already qualified compatible source, then verify the newly
loaded state and permitted work.

Never delete idempotency/financial/audit records or add a TTL merely to reduce
the backlog. A pause does not itself impose a storage quota or prevent new
foreground records from accumulating.

Keep current deployment IDs, acceptance results, remaining gaps and cleanup
evidence in [the provider status](../WIP/HyperDrive-Providers/CURRENT_STATUS.md)
and the associated incident report, rather than embedding a changing fleet
snapshot in this runbook. Tests must use owned isolated backends; never point
recovery or performance loops at a shared cluster to prove a pause.
