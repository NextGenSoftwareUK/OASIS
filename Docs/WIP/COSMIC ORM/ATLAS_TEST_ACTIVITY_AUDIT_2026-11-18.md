# Shared Atlas test-activity audit — 18 November 2026 UTC

David requested that CQL/HyperDrive work make no test calls to the shared OASIS Atlas cluster while the database incident is investigated. Cutoff: approximately 17:00 Eastern on 17 November, equivalent to 22:00 UTC. This report records inspected evidence, not attribution of all cluster traffic.

## Immediate restrictions and stop check

- No new shared dev/staging/production database query, sandbox query, authentication probe, benchmark, soak, sync or replication test was issued for this audit. Further tests must use isolated stores. Any proposed shared-cluster test must be described to David first, with bounded operations, pauses and retries.
- No local dotnet process was present in the scan. No established connection on Mongo ports27017/27018/27019/27029 was found. This snapshot does not exclude another port, an already-exited process, or remote hosted workers.
- The local mongod process has run since14 November; it was not created by these current CI changes and was not killed.
- Four older looping PowerShell shells belong to claude.exe. Their inspected commands contained no database, HTTP, Railway or GitHub call indicators; they were left alone. Current owned Python processes were Git publication, not Mongo clients.
- No matching OASIS/COSMIC/CQL/Mongo/HyperDrive scheduled task or Codex automation was found. Broad reconciliation-name matches were Windows system tasks, not this programme.
- No Atlas-targeting owned test process was identified to stop. No WEB4 host, unrelated process, live application worker, DNA flag, queue, obligation or PIPER record was stopped, rewritten or deleted.

## Recorded work during the window

The CQL native runs inspected use the GitHub Actions runner's owned Mongo8.0.15 container on127.0.0.1:27029, replica set oasisRecoveryRs. The native Mongo entity/projection fixture itself pins that loopback URI, creates its owned test database and records exact cleanup. The workflow also sets SUBSCRIPTION_TEST_MONGODB_URI and OASIS_OWNED_RECOVERY_REPLICA_URI to loopback. Its startup readiness polling and fault injection target that container, not Atlas. Native Neo4j and SQLite are likewise test fixtures.

Recorded runs include:
-44203942482: native string/source qualification; Mongo129 scoped groups, Neo126, SQLite110; fixture cleanup inspected in memory.
-44214292882,44217286334,44221309989: computed-projection/policy foundations through the isolated qualification workflow.
-44225090080,44226245452,44228168270: policy-only regression/persistence qualification. The first two exposed defects; the final run passed44 selected cases and2 owned SQLite cases. No Mongo service is required by that policy-only job.
-44235554771 and44238225378: three-store computed projection qualification. Both terminal failures, not retry loops. The second progressed beyond the fixed cell-domain failure and exposed unsupported framework nullable HasValue capture. No provider capability was enabled live.
-44239827185: terminal SUCCESS on0d04cb0470b9.55 selected policy/save/wire/proof cases and2 owned SQLite cases passed; NATIVE_POLICY_CLEANUP_VERIFIED=true. No hosted-avatar settings were saved.
-44208771977 and44230134893: image construction; no image was pulled or application launched on this laptop.

No Atlas stress/soak loop was identified among these runs. Tiny native timing observations concern isolated fixtures; they are not hosted performance measurements. This list substantiates the recorded runs and does not prove the absence of every historical HTTP request or every other agent's workload.

A Development push at approximately01:04 UTC published1b1aad9f5ae9 and triggered ordinary service deployment. The replication worker blob is identical before and after that push (ba9ca1ad1018 and1b1aad9f5ae9 both bf64ee9f31dfbf7f58a29967d6f57e842f0f5923). Restarting WEB4 can restart its ordinary hosted workers; this was not a separate test worker. Subsequent feature-only policy and computed-query work has not been deployed by this chat. Peer incident deployments are separate activity.

## Hosted replication and usage workers

Inspected authoritative Development source dfc6e7aedd9c; the replication worker is byte-identical to its predecessor5646c390f0fc. A source ref does not establish which revision currently serves.

HolonReplicationDeliveryHostedService:
- Registered as a WEB4 hosted service; enabled by booted HyperDrive V2.
- Polls every15 seconds and also wakes on an enrolled committed write.
- Captures source bindings and reads route-head inventory, bounded to8 pages of64 heads per source per pass.
- A head already in ReconciliationRequired logs blocked-at-sequence and continues before receiver activation or DispatchOnceAsync.
- Other eligible heads may dispatch up to20 rounds, breaking on error or lack of completed work.
- Therefore, if sequences535 and543 are producing this exact blocked warning, the warning is not evidence of another replica-write attempt. The route inventory is still reread and logged each pass, so those barriers can contribute database reads/log volume.
- No standalone delivery-worker pause setting was found in the inspected service. Turning off automatic load balancing does not disable this worker. No barrier was cleared, replayed, skipped or reauthorized.

UsageReconciliationWorker:
- Registered when subscription usage is enabled.
- Uses batches of25 work items; fetches subscriptions and collects evidence; waits60 seconds between passes.
- Its production impact and overlap with replication have not been measured here. No DB scans or queue reads were made to measure them.

No claim is made that either worker explains the Atlas load or PIPER authentication incident. Live workers remain unchanged; coordinating a pause must preserve the API and accounting/replication invariants rather than killing the whole host.

## Verification and resource handling

Only local process/task inspection, Git source inspection and GitHub control-plane logs were used. No connection string, token, complete DNA or process command line was printed. Logs were inspected through pipes/in memory; no Mongo client/runtime/environment/database/results archive was downloaded. Temporary Git indexes are scoped to automatic cleanup. Disk last observed17.47GiB free.
