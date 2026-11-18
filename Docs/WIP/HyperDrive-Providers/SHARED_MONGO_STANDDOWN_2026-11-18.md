# Shared Mongo stand-down and activity report — 2026-11-18 UTC

Owner request: stop shared OASIS Atlas tests/probes and background polling while the database incident is investigated. This overrides the earlier live diagnostic plan. No further shared-cluster test will run without prior notice and an explicit small bound, pacing and retry cap. Do not use shared deployment connection strings in tests.

## What is stopped and what is still active

- No new Mongo command, authentication probe, service warmup, benchmark or application HTTP probe has been initiated since this request.
- Owned-process inspection, excluding the scanner itself, found no running dotnet/Python/Node/test runner referring to this worktree or the portal worktree. The known owned Edge test profile has no running browser process. The portal health/benchmark checks were stopped/closed earlier; no owned browser polling remains.
- All nine qualification/build runs listed below are terminal. No live job required cancellation. Native fixtures used CI-local Mongo/SQLite, not the shared cluster; the local timing test has also exited. No session-owned soak, stress or sync-test timer is running.
- A client deadline is not a server cancellation guarantee. I cannot prove all earlier timed-out API work ended solely from the client exit. The old containers were replaced; the latest PIPER proxy invocation can also outlive its client. No more probes will be issued to check this.
- The deployed application workers are NOT yet confirmed paused. Their serving code has no dedicated operator pause switch. Source controls for the five HyperDrive workers and usage reconciliation have now been prepared on the task branch, preserving queues and foreground services; compile/qualification, environment staging and rollout remain to do. Other periodic worker registrations still need audit. Do not describe this as all background work stopped.

## Activity since 5 pm ET on 17 November (22:00 UTC)

| Run | UTC creation | Work | Backend |
|---|---|---|---|
|44192504510|17 Nov22:41|Cleanup source qualification failed before compilation on a manifest guard|No native test execution|
|44195119239|17 Nov22:58|One compiled cleanup/discovery contract|Compiled contract, no shared Atlas fixture|
|44199944286|17 Nov23:30|One real seal-routing case|Owned CI Mongo container and SQLite|
|44210252857|18 Nov00:44|One administrative equal-timestamp pagination case|Owned CI Mongo container|
|44211498602|18 Nov00:53|API compilation|No application/test host started|
|44212477609|18 Nov01:00|Docker image build|Build only; no shared Atlas fixture|
|44215740740|18 Nov01:25|One native cleanup failure/recovery case|Owned CI Mongo container and SQLite|
|44229792538|18 Nov03:21|API compilation +5 timing contracts|Source-linked diagnostic host, no DB|
|44235411252|18 Nov04:09|API compilation +6 affected trace contracts|Source-linked diagnostic host, no DB|

All are completed; all except the first guard failure succeeded. Evidence links: https://github.com/NextGenSoftwareUK/OASIS/actions/runs/ followed by each run ID. Native CI fixture URI is the explicit loopback replica-set endpoint on127.0.0.1:27029; test code/runner rejects other endpoints for these cases. Containers/volumes and fixture databases were cleaned by the recorded jobs. No shared dev/staging/production credential was supplied as their fixture URI.

Separately, I DID run bounded manual diagnostics against dev/shared Atlas during the incident. The recorded successful native commands total22 explicit commands: ping+one dbStats (2), listDatabases+18 dbStats (19), and one server-projected currentOp summary (1). Driver discovery/authentication/heartbeats or automatic retries were not separately metered. These returned counters/metadata, not full avatar/holon content, and did not create a database or fixture. This count does NOT bound the application calls below or establish total cluster traffic.

I also made five single nonexistent-account sign-in attempts during the incident (three direct Python attempts, one through the installed real SDK, one through PIPER's admin proxy), plus empty-model validation requests and bounded health/UI checks. They were not a soak or scheduled loop, but their client timeouts DID NOT cap the backend's internal lookup/failover work. I cannot rule out those attempts contributing more load than their request count suggests. No account was created. The latest direct failed probes did not appear in the inspected latest deployment logs, so their exact server-side destination/work is not proven.

One existing PIPER service warmup was called. It returned200 in2,445ms; matching fresh WEB4 authentication finished200 in1,155ms with a SQLite session save. This was a normal service-account session operation, not a new fixture or student/plan/report mutation. Credentials/session values were not extracted or printed. Internal Mongo reads/failover work from that application call were not counted. No further warmup will be run during stand-down.

Portal health/benchmark UI validation used the shipping health-measurement function targeting /api/health. Those controls were stopped and their browser closed. The WEB4 health response is static liveness, not a Mongo benchmark. Existing evidence does not justify claiming every UI request was free of database work.

The current incident work also published two scoped parent Development batches:1a95f764726d (root timing) anddfc6e7aedd9c/Core25dd06fd (detailed timing). API startup and the already-registered production workers can generate database work on rollout. A duplicate WEB4 build appeared for each/some source snapshots without an additional redeploy request from this session; its trigger was not established. Do not attribute every service request or deployment to test jobs, and do not dismiss worker traffic as harmless because tests use local fixtures.

## Replication and reconciliation loops

Serving HolonReplicationDeliveryHostedService polls every15 seconds and can also wake on an enrolled-write event. It permits up to20 rounds per target, enumerates native sources, captures their bindings and reads durable route inventories. AutoReplication=false does not turn this durable-drain worker off. This is an ongoing application loop, not a test loop. Repeated binding failures and blocked deliveries can therefore still cost native reads/writes and transfers. Claude reports dev535 COSMIC_REPLICA_LINEAGE_CONFLICT and543 COSMIC_REPLICA_AUTHORIZATION_DENIED; this stand-down has not queried or modified either delivery.

UsageReconciliationWorker initializes its reconciler and scans up to25 account-month keys every60 seconds. The peer change skips failure writes for accounts lacking subscriptions, but still scans and reads subscriptions. Reducing log spam is not equivalent to eliminating that database workload.

Prepared operator controls are OASIS_PAUSE_HYPERDRIVE_WORKERS and OASIS_PAUSE_USAGE_RECONCILIATION. They suppress hosted-worker registration before construction/start, without disabling foreground sign-in, changing HyperDrive routing flags or acknowledging/deleting outboxes. They require code deployment plus configured flags and a restart; saving a flag in an old deployment does not pause it. They do not yet cover every other Mongo-using background component. Further audit and isolated qualification are agent-owned work.

## Evidence and cleanup

Source/evidence: PIPER_INCIDENT_2026-11-18.md, CI_ACCEPTANCE.md, WORKSPACE_CLEANUP_LEDGER.md and the exact refs/run IDs above. Activity counts are this session's recorded work, not another agent's full workload or an Atlas bandwidth measurement. Current source cannot establish who caused the cluster-wide10–30ops/s or1–2MB/s. No shared Mongo probe is authorized merely to improve this report.

No new local environment/download/build output was created for this stand-down. The previous local test's remaining146,335-byte restore metadata is an existing automatic-review cleanup exception. The temporary remote stack tool and bundle/home were already removed and absence-verified.
