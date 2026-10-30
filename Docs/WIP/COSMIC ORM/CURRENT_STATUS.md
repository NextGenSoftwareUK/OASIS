# Current COSMIC ORM / CQL status

Updated30October2026 UTC. Full programme remains incomplete. Read this first after compaction.

## Working state

- Backend work/build/test only C:/Source/OASIS-cosmic, branch work/cosmic-cql.
- Existing frontend C:/Source/Web4Site, work/cosmic-cql c17e5f7; main unpublished.
- Scoped fixes rejoined the broader task branch in parent3ee4a5d9f; Core69bb8a4 includes receipt fixfdca0ed.
- Development remains parent89a960111/Core432c613; Railway deploymente88fbed5 SUCCESS.
- Two Development batches already published today. Next deploy must batch verified changes.
- Unknown root `logs` remains untouched. No new worktrees/environments/installations.

## Priority and exact next step

1. Rejoin verified: all7 affected projects build against broader Core69bb8a4;30 API controls,189 Mongo CQL controls,8 native lifecycle and4 native ancestor cases pass/no skips;6 offline verifier tests pass.
2. Next verified deployment batch must carry receipt ownership fix, root-filter optimization and free write profilev2 together. Preserve current remote settings/concurrent changes.
3. Verify hosted Mongo/SQLite/Neo4j save/replication/rows/LongCount/federation/isolation, then publish the CQL tab. Hosted timeout resolution remains unproved.
4. While awaiting the next daily deploy batch, extend PostgreSQL content into Mongo-paired C#/HTTP/browser demos:16 controls and17 native query cases pass, repeated after service restart with unchanged JSON/xmin; native content scope verified, content paired/browser/sandbox remain red; existing Mongo/SQLite/Neo4j green scopes stay frozen. Continue [approved10-provider content queries](CONTENT_QUERY_DEMO_ROADMAP.md) and [sandbox coverage](SANDBOX_PROVIDER_DEMO_ROADMAP.md): Mongo, SQLite, Neo4j, PostgreSQL, SQLServer, Oracle, CosmosDB, DynamoDB, Elasticsearch, Solr.
5. Then extend remaining demo providers. Hosted Azure stays pending/free-only by user instruction. No paid subscription/account creation.

LINQ parity depth (All/element/numeric after entity selection), POCO mapping and migrations tooling are paused/deferred. Work only on red checklist cells; no green/cursor hardening without a reproduced bug. HyperDriveV2 only; legacy issues documentation-only.

## Latest verified fixes before branch rejoin

- Hosted stage limit:56to40stages, full16ancestor levels preserved; deployed89a960111.
- Physical receipt IDs reused across independent operations: Corefdca0ed binds IDs to admitted operation; regression failed before/passes after;8 native billing lifecycle cases pass/no skips. Old executed unsettled-query receipts remain immutable/unresolved; never replay provider execution to settle them.
- Free profilev2 explicitly enrolls canonical save/batch/delete/recovery with21 storage tariffs and3 query tariffs. Quota/measurement guards remain;10 actual-profile controls pass. Candidate not deployed.
- Mongo root-local filters now precede ancestor lookup; full access predicate stays after the walk.189 CQL controls+4 native ancestor cases pass, including16th admitted/17th denied and row/count agreement. Hosted latency still pending.
- Frontend ORM/Failover sends explicit request keys and preserves uncertain identities;4 helper+11 CQL client tests pass. No automatic write replay.
- Hosted verifier requires exact deployment/SHA, fits19 admitted owner calls and preserves durable intents;6 offline protocol tests pass. Those tests do not prove live acceptance.
- Original save56eaea32 was definitively rejected before manager acquisition: exact deployed source compared,7 boundary controls pass. Original bytes and reconciliation retained under LocalState/COSMIC/HostedSandboxCqlCurrent/Archive; no replay/reset. A new declared fixture may be created only against corrected deployment.
- [Scoped release evidence and hashes](CQL_SANDBOX_RELEASE_2026-10-30.md). Logs/TRX remain in TestResults/Web4SandboxCqlCurrent.

## Broader programme scope retained

- Prior task branch records62/226 scoped demos; this is demo coverage, not full ORM/CQL parity or hosted production readiness.
- Mongo/SQLite/Neo4j initial investor rows green. Q01f/full parity remain red; [strict checklist](CQL_PARITY_CHECKLIST.md).
- Oracle native/SDK/HTTP/browser/Mongo-paired proof: [scope](CQL_ORACLE_DEMO_ADMISSION.md).
- Azure Cosmos emulator proofs only; [hosted free gate](CQL_HOSTED_AZURE_FREE_ADMISSION.md).
- IPFS/SOLID/ActivityPub multi-record native and Mongo-paired proofs: [scope](CQL_PROTOCOL_MULTI_RECORD.md). Arbitrary content predicates remain open.
- Solana retained validator/index/SDK/HTTP/browser: [proof](CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md). Original missing-history recovery stays red; no reset/replay.
- AWS DynamoDB Local: [scope](AWS_CQL_MVP.md). Hosted cloud coverage must be proved separately.
- Other scoped native/restart/Mongo-paired/browser proofs and remaining provider gates are preserved in [previous checkpoint](Archive/CURRENT_STATUS_before_sandbox_rejoin_2026-10-30.md).
- Full correctness/replication/history/migration/placement/save-deltas and EF/NHibernate parity remain governed by [roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md), [CQL roadmap](CQL_COMPLETION_ROADMAP.md) and checklist; never infer100% from passing subsets.

## Resources and key commands

[Single resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) records paths/versions/ownership/cleanup. Reuse pinned SDKs/caches and existing owned provider sources; no repeated environments. Persistent keys/data/receipts/journals stay under LocalState or their retained source directories, never disposable TestResults.

```powershell
git status --short
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' status --short
python Scripts/Sandbox/test_hosted_cql_demo.py
# Only after exact corrected deployment and prior-intent reconciliation:
python Scripts/Sandbox/Verify-HostedCqlDemo.py --deployment <uuid> --commit <full-sha>
```

Use the provider-build lease, /m:2 /nr:false /p:UseSharedCompilation=false; no dependency restores without inspecting existing resources. Test output only in this worktree's TestResults. Commit named owned files, push Core/verify ls-remote before parent gitlink+manifest; validate HEAD with Scripts/validate_railway_dependency_manifest.py --require-gitlinks. Batch Development once/twice daily; task branch pushes do not imply deployment.
