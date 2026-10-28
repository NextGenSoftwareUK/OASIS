# Current COSMIC ORM / CQL status

Updated 27 October 2026. Read this first after compaction; update each milestone.

## Work and priority

- Work/build/test only C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql.
- Full roadmap remains active/incomplete. Demo breadth takes priority over remaining EF/NHibernate parity.
- Work only on red checklist cells. Freeze green features/cursors unless a bug is reproduced.
- HyperDrive V2 implementation only; document legacy gaps. Large POCO/migrations tooling remains deferred.
- [Checklist](CQL_PARITY_CHECKLIST.md), [demo expansion](CQL_DEMO_EXPANSION_ROADMAP.md), [completion roadmap](CQL_COMPLETION_ROADMAP.md).

## Latest verified checkpoint

- Indexed SDK: authenticated durable SQLite history/projection/checkpoint, canonical Solana observer/ingestion, typed manager/clean tracking, LINQ rows and explicit service-tariff physical-read capture implemented.
- WEB4 indexed controller now consumes a frozen server alias/query/grant, exact usage handle/tariffs and sufficient cost/unit/attempt reservation. Changed intent and duplicate consumption reject before dispatch; public source/index/checkpoint/receipts retained. Slots are exact strings.
- Full Core suite: 4,180 passed / 0 failed / 0 skipped. Solana unit suite: 799 passed. Actual SQLite indexed/store checks: 76 passed. Includes 12 controlled loopback HTTP requests, single-use/policy cases, receipt precision and reservation failures.
- Shipping WEB4 and five shared-controller consumers build. Missing outputs/assets regenerated cache-only for 61 in-worktree projects; 8 external sibling projects read-only. Full Core's prior missing-host prerequisite failures are resolved.
- Daily Development integration verified: contributor RecordNotFound fix/regressions preserved; Core4eb3dab46b7658f1c574f432f5169035bb19acea published/remote-verified on Development and work/cosmic-cql. Parent manifest/gitlink aligned for the same verified batch.
- [HTTP milestone/limits](CQL_INDEXED_HTTP.md), [billing contract](CQL_INDEXED_BILLING.md), [indexed SQL contract](CQL_INDEXED_SQL_ROWS.md).
- Stock pipeline adds 12 tests: rows/empty/replay, malformed or unsupported intent, route ceiling, uncertain receipt, registration composition, shared aggregate budgets and preparation failure. Production JWT/durable authority remains open.

## Exact next work

1. Stock usage/storage middleware integration now verified: raw HTTP reservation identity and frozen semantic query identity remain independently checked. AddCqlIndexedReadAdmission composes existing singleton storage admission. Shared handle budgets cannot reset across repeated prepared executions.
2. Actual SQLite/canonical Solana decoder now pass through shipping HTTP plus stock middleware (rows, empty, missing source, wrong network; 799 Solana tests). Native declared-range SDK acceptance now passes1/1 with live source and reopened SQLite (coverage starts3073), including the shared C# walkthrough. Native-source HTTP now passes1/1: nine requests, real SQL/live RPC, stock middleware; 9/6 billed physical reads for entity and projected rows/empty. Identity/outbox remain controlled; production JWT/durable settlement unverified. Next: indexed scalar/projection/metadata policies and common demo gaps.
3. Older finalized minimum/checkpoint/previous identity bug reproduced and fixed. Shared admission requires independent bounded anchor observations; actual SQL/decoder HTTP and ingestion prove accept/reject, including empty pages. Still trusted-source-observation, not inclusion/ancestry proof.
4. Indexed native selected-cell SDK implemented: explicit reader capability, one SQLite snapshot, shared proof-before-DTO owner, V2 manager; Core8/SQLite11 new checks. Standard indexed Select, flat repeated Select/list/enumeration and evidence terminal now pass7newSQLite cases plus live-chain demo1/1. Indexed HTTP projection now passes11newCore cases and the nine-request live gate. Indexed Count/LongCount/Any SDK implemented with native SQL source windows, authenticated index manifest and fresh head/anchors (13Core/18SQLite new cases). Standard indexed LINQ cardinality now passes9newSQLite cases and the shared live-source demo (Count1/LongCount1/Anytrue, no new seed). HTTP cardinality foundation now preserves nullable source windows and deep scalar intent (6 targeted tests pass); route/admission/execution wiring is next. Metadata/other aggregates/selected stages remain open. Same query across all eight providers remains open.
5. Resume remaining red parity rows after demo acceptance, including Q01f Mongo/Neo projection/standard Select and numeric terminals.

## Other verified scope / limits

- MongoDB/SQLite/Neo4j:12 original investor-demo rows green; Q01a-e, Q23/Q23e/Q49a scoped green. Q01f remains red despite native stages/count/element/default gates; SQLite entity projection only is integrated.
- Cosmos:46 provider controls,47 native cases/5 manager demo operations and controlled-identity native HTTP rows/scalar/projection passed. Hosted cloud/production auth/billing, numeric metadata and conditional lifecycle remain open.
- IPFS/SOLID:52 native storage checks each;191/384 CQL controls. Native manager/LINQ/sample and controlled-identity WEB4 point profiles passed. Persistent identities under LocalState/COSMIC retained; services stopped.
- ActivityPub:311 provider controls,1,964 affected CQL regressions; native public-status manager/LINQ/sample/HTTP point gates passed. Broader queries/production admission remain open.
- Solana original recovery stays red/deferred: no known backup; retained root/chunk unavailable. Native diagnostic rejects declared start 0 at first available block 1402, without writes/checkpoint/index. Never reset/replace the original retained ledger or claim a fresh demo repairs it.
- [Solana admission](CQL_SOLANA_DEMO_ADMISSION.md), [interactive demo guide](CQL_INTERACTIVE_POINT_DEMO.md), [manual](CQL_MANUAL.md).

## Resources and recovery

- [Single cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) records exact build/runtime/evidence identities. Latest inventory:TestResults/ProtocolCqlCurrent/index-http-build-resource-inventory.json; 1,770,881,866 observed build bytes, mixed inherited/regenerated ownership.
- Retained Solana validator reused and stopped; no new runtime/node. New persistent demo seed/index under LocalState/COSMIC/SolanaCqlCurrent retained; declared range3073 is separate from unrecovered original history.
- TestResults/bin/obj are regenerable. Preserve LocalState/COSMIC identities/keys/journals and original failure evidence. Logs/TRX stay in TestResults, never Docs.
- Shipping rebuild graph/target plans are in TestResults/ProtocolCqlCurrent/index-http-web4-*.json. 69 projects, 61 inside. Do not recursively build 8 outside sibling projects; reuse their binaries. Highest compatible runtime target precedes .NET Standard (ONET requires net8.0).
- Earlier detailed status/hashes: [archived checkpoint](Archive/CURRENT_STATUS_before_indexed_http_2026-10-27.md).

## Key commands (worktree above)

```powershell
git status --short
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' status --short
# Fetch/inspect before daily integration; preserve concurrent changes.
git fetch origin
# One build lease per batch; no nested leases.
& 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1' -Owner cosmic-cql -Operation {
    dotnet test 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/NextGenSoftware.OASIS.API.Core.UnitTests.csproj' --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --results-directory TestResults/ProtocolCqlCurrent
}
python Scripts/validate_railway_dependency_manifest.py --require-gitlinks
```

Commit named owned files only; Core push/ls-remote first, then parent gitlink/manifest. Batch verified Development integration once/twice daily; task-branch pushes do not imply a Development merge.
