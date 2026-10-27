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
| Latest native checkpoint | 1,574 checks: SQLite 527, MongoDB 527, Neo4j 520 |
| Latest committed shared entity-stage foundation | 2,545 CQL regressions pass; both Core targets build |

Native evidence: `TestResults/Neo4jCurrent/neo4j-linq-rows-current.trx`.
SHA256: `0A26277C8392618C9A7D305F363224ACD008A18BB5A196AB59AB81EFC87645E4`.
Shared evidence: `TestResults/CoreCurrent/entity-composition-regression.trx`.
SHA256: `44C39A2E37FD6503FEBF78931FCFA9D96CAB03B008C1A39905AAB21319E8D5FE`.
Latest native gate includes18 entity-stage cases/store, Neo snapshot rejection, and9 entity-count cases each on MongoDB/SQLite.

Q01f remains red on all databases: Native rows/typed V2/graph/visibility checks pass on all three; staged terminal/projection integration remains pending.
IQueryable bridge committed in Core `b9bccf0fdb2f035ff5a460186ac9f17055e7cd71`: 2,550 CQL regressions pass.
Both Core targets build; deferred capture and typed hydration/tracking share the existing owner.
Evidence: `TestResults/CoreCurrent/entity-stage-bridge-regression.trx`.
SHA256: `2BD5563C7108302BE8949D9E4F15A37E37D99A7305033C0B4F55B544BDCEAB25`.
The full Core suite is not green: earlier run had 3,853 passes and six failures from a missing built WEB4 host prerequisite.

SQLite entity-stage increment: 42 CQL provider tests pass, including seven new SQL/native-owner/graph tests.
Evidence: `TestResults/CoreCurrent/sqlite-entity-stage-graph-current.trx`.
SHA256: `0691C0AA1A23F527EF3037DB2249E5C0B71EAC7E0195B28CA805C05ACC4661DF`.
Native summary: `TestResults/Neo4jCurrent/cql-linq-rows-result.json`, SHA256 `35AD188BEDB025B9E1210BDB6DA97EC081966573E102E091103256ED8B99AC7D`.
Provider build passes. Native materialized stages and ordinal positions share the row snapshot/budget/graph owner.

Mongo entity stages implemented under the shared row/graph snapshot and deadline owner.
Provider builds; 212 CQL compiler/admission tests pass, including five new tests. Live native proof passes (18 new cases/database, SQLite and MongoDB).
Evidence: `TestResults/CoreCurrent/mongo-entity-stage-current.trx`.
SHA256: `CF8E2C8895AD2E6918D3A477C5BBDE33813840BB78633AB83B0A674413AFFBD1`.

Neo entity stages: single native statement, bounded candidate references, sticky work/traversal outcomes and streamed source byte proof.
144 CQL/compiler tests pass; native gate includes 19 new Neo cases. ProviderSnapshot remains explicitly unsupported.
Unit evidence: `TestResults/CoreCurrent/neo-entity-stage-current.trx`, SHA256 `AD0C5715452DEACB8563D73C272E2ACA2AEEFB8BA30E6396C8DB1883EA712D0A`.

Shared entity-count contract/executor committed in Core `7b2a2d422f8ff03a4bf064edbbbc97108c12ba7f`; both targets build, 2,555 CQL regressions pass.
Five new tests cover native ownership, capability re-admission, acknowledgement/window bounds, cancellation/V2/identity and adapter snapshots.
Evidence `TestResults/CoreCurrent/entity-count-regression.trx`, SHA256 `703C6E13ECDE6521D82BCC1F6D350F5487871BB134E4457A0E5FD5CC936A1C34`.
SQLite/Mongo native count enrolled and live-verified; Neo count and standard terminal bridge pending.

SQLite count:44 CQL provider tests pass. Mongo count:215 compiler tests pass; both have9 live native count cases, including protected errors/budgets/no DTO or graph expansion.
Evidence `TestResults/CoreCurrent/sqlite-entity-count-current.trx`, SHA256 `190F7213AD5E4D621463A155994F578BDF68B6DEC796C8374CBE812B659622B1`.

Mongo count unit evidence `TestResults/CoreCurrent/mongo-entity-count-current.trx`, SHA256 `6D67F9138A154D8A1A69AF2CEF540C353A80DE32387107FB02BD429B673CB767`.

## Exact next step

Implement Neo4j native entity count, then bind staged Count/LongCount/Any/All. Follow with element terminals and projections, preserving stage ownership.
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
