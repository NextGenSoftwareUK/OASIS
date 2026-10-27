# Current COSMIC ORM / CQL status

Updated: 27 October 2026. Update at every milestone; start here after compaction.

## Phase

- Worktree: `C:/Source/OASIS-cosmic`; parent/Core branch: `work/cosmic-cql`.
- Full ORM/CQL parity remains incomplete. Active red row: Q01f entity composition.
- Investor demo: 12 scoped rows green across SQLite, MongoDB and Neo4j.
- Work only on red checklist cells. Green features/cursors stay frozen unless a bug is reproduced.
- Implement HyperDrive V2 only; document legacy gaps.

## Verified and pending

| Scope | Verified result |
|---|---|
| Source composition Q49a | 216 native checks, 72/database |
| Projected All Q23e | 168 native checks; Q23 parent audit and 24 C# demo examples pass |
| Q01a–e | Selected numeric/extrema/reselection and post-window entity elements green on all three |
| Latest native checkpoint | 1,537 checks: SQLite 518, MongoDB 518, Neo4j 501 |
| Latest committed shared entity-stage foundation | 2,545 CQL regressions pass; both Core targets build |

Native evidence: `TestResults/Neo4jCurrent/neo4j-linq-rows-current.trx`.
SHA256: `8C912F4F21CBDD616ABBD10161A064CAC403897DC951517725BA64C26DEC8CE9`.
Shared evidence: `TestResults/CoreCurrent/entity-composition-regression.trx`.
SHA256: `44C39A2E37FD6503FEBF78931FCFA9D96CAB03B008C1A39905AAB21319E8D5FE`.
These are separate cohorts: the latest native gate includes shared stage ownership and 18 new entity-stage cases each on SQLite/MongoDB.

Q01f remains red on all databases: SQLite native rows/typed V2/graph/visibility checks pass; broader native matrix and terminal/projection integration remain pending.
IQueryable bridge committed in Core `b9bccf0fdb2f035ff5a460186ac9f17055e7cd71`: 2,550 CQL regressions pass.
Both Core targets build; deferred capture and typed hydration/tracking share the existing owner.
Evidence: `TestResults/CoreCurrent/entity-stage-bridge-regression.trx`.
SHA256: `2BD5563C7108302BE8949D9E4F15A37E37D99A7305033C0B4F55B544BDCEAB25`.
The full Core suite is not green: earlier run had 3,853 passes and six failures from a missing built WEB4 host prerequisite.

SQLite entity-stage increment: 42 CQL provider tests pass, including seven new SQL/native-owner/graph tests.
Evidence: `TestResults/CoreCurrent/sqlite-entity-stage-graph-current.trx`.
SHA256: `0691C0AA1A23F527EF3037DB2249E5C0B71EAC7E0195B28CA805C05ACC4661DF`.
Native summary: `TestResults/Neo4jCurrent/cql-linq-rows-result.json`, SHA256 `3AB39028B8B76F35F833D64B00A67F51CC15E5BA8AFFF24897BA2F5FE6C4BF90`.
Provider build passes. Native materialized stages and ordinal positions share the row snapshot/budget/graph owner.

Mongo entity stages implemented under the shared row/graph snapshot and deadline owner.
Provider builds; 212 CQL compiler/admission tests pass, including five new tests. Live native proof passes (18 new cases/database, SQLite and MongoDB).
Evidence: `TestResults/CoreCurrent/mongo-entity-stage-current.trx`.
SHA256: `CF8E2C8895AD2E6918D3A477C5BBDE33813840BB78633AB83B0A674413AFFBD1`.

## Exact next step

Implement Neo4j native entity stages; extend the native matrix, then integrate staged terminals/projections.
Preserve typed hydration, protected scopes and stage order.
Keep Q01f red until native proof covers its complete scope, including staged terminal/projection integration.
Use the [parity checklist](CQL_PARITY_CHECKLIST.md) for subsequent red rows.
Large POCO mapping and migrations tooling retain their deferred priority.

## Fixture and resource safety

- No pending acceptance fixture remains; the last gate exact-cleaned six owned IDs/database.
- Do not invoke reviewed reconciliation against the removed fixture or replay creates.
- Original retained WEB4 journal: `LocalState/COSMIC/Neo4jCurrent/cql-v1-web4-fixture.json`.
- Journal SHA256: `892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5`.
- Persistent ignored fixtures belong in `LocalState/COSMIC/`; never clear with test output.
- Reuse MongoDB port 27029 and Neo4j Bolt 17687; owned services stopped after native verification.
- See [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) before setup or cleanup.

## Commands (PowerShell, worktree above)

```powershell
git status --short
git fetch origin
# Daily integration only after inspecting concurrent parent/submodule state:
git merge --no-edit origin/Development
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' fetch origin
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' merge --no-edit origin/Development
# Fresh native acceptance, only after confirming no pending fixture:
& 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1' -Owner 'cosmic-cql' -Operation {
    & 'Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1' -CosmicCqlLinqRows
}
```

Build flags: `/m:2 /nr:false /p:UseSharedCompilation=false`; reuse existing dependencies.
Logs, TRX, JSON and seeds go in this worktree's `TestResults/`, never Docs or new commits.
Commit named files only. Push/verify Core first; advance parent gitlink and Docker manifest together.
Batch verified Development merges once/twice daily. No routine Providers-agent coordination.
[Archived checkpoints](Archive/CURRENT_STATUS_checkpoint_2026-10-27.md) preserve older hashes and cohorts.
