# Current COSMIC ORM / CQL status

Updated: 27 October 2026. Start here after compaction; update every milestone.

## Phase and next action

- Worktree `C:/Source/OASIS-cosmic`; parent and Core branch `work/cosmic-cql`.
- Full COSMIC programme remains incomplete. Active red row: Q01f entity composition.
- Exact next step: bind standard staged forward element terminals, then prove native acceptance; add tail/reversal support.
- Then finish staged projections/remaining numeric terminals; verify full scope before promoting Q01f.
- Work only on red checklist cells. Green features/cursors stay frozen unless a bug is reproduced.
- Implement HyperDrive V2 only; document legacy gaps. Large POCO mapping/migrations remain deferred.

## Verified scope

| Scope | Current evidence |
|---|---|
| Investor demo | 12 scoped rows green on SQLite/MongoDB/Neo4j; full ORM parity incomplete |
| Q23/Q23e/Q49a | Source/projected-All proofs and 24 C# examples pass; frozen |
| Q01a–e | Scoped numeric/extrema/reselection/post-window element rows green on all three |
| Q01f entity rows | 18 native cases/database plus Neo snapshot rejection; stage order, visibility, graphs, protected errors and bounds verified |
| Q01f native entity count | Nine cases/database; stage cardinality, protected errors, budgets, no DTO construction/final graph expansion |
| Latest native gate | 1,631 checks: SQLite543/Mongo543/Neo545, one test, no skips |
| Focused provider CQL tests | SQLite44, Mongo215, Neo147 pass; provider builds pass |
| Shared count foundation | 2,563 CQL regressions pass; both Core targets build; Core7fd3b11 pushed/verified |

Q01f stays red: native count and standard staged Count/LongCount/Any/All pass on all three;
entity element terminals, projections and remaining numeric terminals are still pending. No client reduction substitutes for native execution.
Neo uses ProviderDefault graph consistency; ProviderSnapshot explicitly rejects.
Count and rows share provider read owners and source/stage proofs. Native count ignores final graph expansion.

Native TRX: `TestResults/Neo4jCurrent/neo4j-linq-rows-current.trx`.
SHA256 `688480271D5E3F125AF131EEA9DFBD55CF77E75A349694D6C9EE7EE13ECCFD9A`.
Summary: `TestResults/Neo4jCurrent/cql-linq-rows-result.json`.
SHA256 `2A38A09274A3F4F48F076E270926541EE2006F5D6B6D8DC5A99127A4B01D80B1`.
Cardinality binding TRX: `TestResults/CoreCurrent/entity-cardinality-binding-regression.trx`.
SHA256 `F8EC790EC9471A964DAC0A168C85C9886E9A8CB012D64ADA39A97B7A31450718`.
Four new shared tests and16 live standard-terminal cases/database verify predicates after windows, All counterexamples, closure rebinding and no entity construction.
[Earlier checkpoint evidence](Archive/CURRENT_STATUS_pre_terminal_binding_2026-10-27.md) preserves other hashes/cohorts.
Full Core suite is not green: earlier3,853 passes/six failures from missing built WEB4 host prerequisite remain unverified.

Shared forward-element planner/executor and V2 manager now append native terminal windows; Single cardinality precedes application construction.
Four new tests cover ambiguity, hydration/tracking, index/default semantics and gates. Standard binding/live proof/tail reversal pending.
Evidence `TestResults/CoreCurrent/entity-element-regression.trx`, SHA256 `0C29CE9DAC24EBC46E995C081E568DC46F3B028A32987214FA97EE7FC374E31E`.
The latest 1,631 native gate predates this element foundation Core increment.

## Fixture and resource safety

- Latest gate exact-cleaned six owned IDs/database; completed pending journal removed; services stopped.
- Do not invoke reviewed reconciliation against the removed journal or replay creates.
- Original retained journal: `LocalState/COSMIC/Neo4jCurrent/cql-v1-web4-fixture.json`.
- SHA256 `892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5`; original unchanged.
- Persistent ignored fixtures stay in `LocalState/COSMIC/`, separate from disposable `TestResults/`.
- Reuse Mongo27029 and Neo Bolt17687; consult the [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Commands (PowerShell, worktree above)

```powershell
git status --short
git fetch origin
# Daily, after inspecting parent/submodule state:
git merge --no-edit origin/Development
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' fetch origin
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' merge --no-edit origin/Development
# Fresh gate only after confirming no pending fixture:
& 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1' -Owner 'cosmic-cql' -Operation {
    & 'Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1' -CosmicCqlLinqRows
}
```

Build flags `/m:2 /nr:false /p:UseSharedCompilation=false`; reuse installed dependencies.
Logs/TRX/JSON/seeds only in this worktree's `TestResults/`, never Docs or new commits.
Commit named files only. Push/verify Core first; advance parent gitlink and Docker manifest together.
Batch verified Development integration once/twice daily. No routine Providers-agent coordination.
[Parity checklist](CQL_PARITY_CHECKLIST.md) owns acceptance; [archive](Archive/README.md) holds history.
