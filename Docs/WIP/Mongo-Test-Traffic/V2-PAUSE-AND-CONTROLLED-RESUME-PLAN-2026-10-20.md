# HyperDrive v2 pause and controlled recovery plan

## Current operational decision

David requested pausing v2 background workers again while the Atlas issue is investigated, then a staged restart after normal performance returns. On October 20, WEB4 dev `OASIS.OASISHyperDriveConfig.EnableHostedSync` was set to **false** again. Only this field changed. Redeployed the same successful fixed image (commit `18aa2be48e324476126c916dd185af32a9d99d35`) as deployment `75734b55-9c9d-4a74-8428-58dfc9644af0` (SUCCESS).

Startup logs at 08:52 UTC explicitly confirm command execution, capture, fan-out and history compaction disabled. Evidence: `dev-workers-paused-again-logs.json`. This supersedes the earlier resume report. Preserve this pause across deployments. No automatic restart, recurring Atlas benchmark or scheduled polling has been created. Hosted synchronization API availability also follows this master flag. Queues/checkpoints are preserved. v1's `StorageProviders.AutoReplicationEnabled` and ordinary write concurrency protection are unchanged. Staging/production untouched.

## What we established

- Exact matching BSON results are fast locally: 32 quest records / 419,565 bytes took 2–15 ms locally versus 4.57–4.61 seconds through Atlas; avatar detail 1–18 ms locally versus 665–715 ms through Atlas. See `ATLAS-LOCAL-COMPARISON-2026-10-20.md` and its JSON. This isolates the large delay to the hosted database/network path; quota throttling remains unconfirmed.
- An expired change-stream checkpoint returns MongoDB code 286. The fixed capture worker now logs this permanently and stops rather than retrying forever. **Stopping the retry loop is not checkpoint reconciliation.** Do not reset the token and silently skip missed mutations.
- Fixed workers back off to 30 seconds when idle; empty command/fan-out polls avoid acquiring write leases. No claim/CAS/ordering protection was removed. The earlier immediate pause did not make Atlas reads fast, so polling alone is not the established immediate cause.

## Before resuming

1. Get Atlas support's quota-accounted rolling seven-day ingress/egress totals and confirmation of whether transfer or operation throttling is active. There is no fixed daily reset. Do not infer the quota total by summing replica network graphs.
2. With workers still paused, take a small, bounded baseline of identical full-result reads plus server execution stats. Confirm performance has returned to a stable acceptable range from the same client/host. Avoid continuous large-result probes while waiting for recovery.
3. Record the active image, flags, replica count, client location, record counts/hashes/bytes, query latency, network rate and MongoDB read/write rate. Avoid unrelated deployments/test traffic during a comparison.
4. Add independently selectable command/capture worker controls before promising a one-at-a-time restart. Currently both share `EnableHostedSync`; fan-out additionally uses v2 `AutoReplicationEnabled`, and compaction uses `EnableSyncHistoryCompaction`. Merely toggling the master flag cannot independently start command and capture. Preserve compatibility/defaults, test the gates locally and deploy while master sync remains false.

## Staged restart experiment

Choose a quiet observation window and one WEB4 replica. Keep all other workers disabled. Start command execution alone, then fan-out with only the intended targets, then maintenance as a separate controlled run. Start capture only after local recovery tests and safe missed-state reconciliation are complete. After each stage, observe for at least 15 minutes; retain a longer quiet soak before adding load. These are proposed observation windows, not proof that a seven-day quota effect will be visible immediately.

For each stage record database commands/second, writes/second, network bytes, queue backlog/oldest age, lease contention, errors/retries, and a small fixed query latency sample. Compare to the paused baseline. Unexpected continuous idle writes, tight retry loops, growing backlog, duplicate processing, or a material sustained latency/traffic increase are reasons to pause that stage and investigate before enabling another. Do not treat a temporarily fast result as proof that accumulated transfer usage is safe. Coordinate other chats/test clients to avoid confounding traffic.

## Isolated local soak testing

Use the installed MongoDB at **127.0.0.1:27017**. No Atlas URI, Railway credential or application DNA is loaded by the test. The harness creates its own `hd_soak_*` database, initializes the real Mongo provider, seeds the production provider accessor with that exact local instance, runs the real command hosted service and real fan-out dispatcher, and profiles operations only inside the disposable database. It restores static test configuration, stops owned loops and drops the owned database in finally. It never changes server-wide profiling or the user's other local databases.

```powershell
# Run from C:/Source/OASIS after normal dependency restore.
./Scripts/Test-HyperDriveLocalWorkers.ps1 -DurationSeconds 65

# Longer bounded observation (maximum 3600 seconds):
./Scripts/Test-HyperDriveLocalWorkers.ps1 -DurationSeconds 1800
```

`-ReuseBuiltDependencies` skips rebuilding referenced projects and is appropriate only when the current production worker/provider assemblies have already been built and verified. Coordinate shared builds with other chats. Reports/logs reuse fixed files in this folder, rather than accumulating per-run databases or artifacts. Use the fresh test log/exit status to distinguish a failed run from an older JSON report.

The test asserts bounded idle poll counts, zero **steady-state** global lease writes (startup/shutdown are separate), no hosted service fault, and exactly one winner when two native command claimers race for a queued diagnostic record. It samples local query latency during the soak. It does **not** execute domain commands end-to-end, capture changes from a replica set, deliver to external replication targets, or establish hour/day-scale stability from a minute-long run. Those need additional scenarios against the existing reusable local replica-set environment; never redirect them to Atlas. Capture scenarios should cover idle cursors, two competing workers, valid checkpoints, expired history, cancellation and reconciliation without missed/duplicate state.

Initial 65-second run passed: seven command polls, seven fan-out polls, zero steady-state lease writes, 33 query-latency samples (median 1.3397 ms, maximum 5.9127 ms), and exactly one successful claim in the two-worker race. Evidence: `local-worker-soak-65s.json`. A subsequent five-second runner smoke check verifies the reusable script and cleanup paths; the fixed `local-worker-soak.json` and `local-worker-soak-tests.log` hold the most recent run. Startup/shutdown, profiler management and latency probes are excluded from the idle lease-write count.
