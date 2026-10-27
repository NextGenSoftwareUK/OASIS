# Current COSMIC ORM / CQL status

Updated: 27 October 2026. Start here after compaction; update every milestone.

## Phase and next action

- Worktree `C:/Source/OASIS-cosmic`; parent/Core branch `work/cosmic-cql`; daily Development integration verified (Core08dec1c).
- Full COSMIC programme remains incomplete. Active priority: eight-provider demo before full parity.
- Cosmos:46 provider tests,47 native cases/5 manager-demo ops plus native HTTP rows/scalar/projection pass under controlled identity/receipts.405 affected tariff/WEB4 tests pass. Core08dec1c merge pushed/verified, parent pin updated. [Admission report](CQL_COSMOS_NATIVE_ADMISSION.md). Next: [Solana retained-record/query admission](CQL_SOLANA_DEMO_ADMISSION.md); cloud numeric, hosted/prod auth-billing and conditional lifecycle remain open.
- Order: existing Mongo/SQLite/Neo demo, Cosmos candidate, Solana, IPFS, SOLID, ActivityPub. See [demo roadmap](CQL_DEMO_EXPANSION_ROADMAP.md). Defer unfinished parity-only depth; Q01f stays red.
- Solana diagnostic milestone:765 unit tests pass; provider/unit/integration builds pass. Typed missing-chunk references preserve fail-closed reads/no resubmission. Original-ID diagnostic and bounded exact-history probe completed: missing chunk0/5 identified; later node returns root/chunk/status absent. Recovery/CQL admission remain red; [chain query contract](CQL_CHAIN_QUERY_CONTRACT.md) now specifies explicit source/index selection, checkpoint/receipt/freshness ownership and ingestion gates. Core ca32960 source/checkpoint/record receipts and observation lifetime pass38 tests/no skips; both targets build. No adapter enrolled. Next source selection/planning and snapshot-wide publication; backup location pending.
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
| Latest native gate | 1,791 checks: SQLite603/Mongo593/Neo595, one test, no skips |
| Focused provider CQL tests | SQLite47, Mongo216, Neo148 pass; provider builds pass |
| Shared count foundation | 2,578 CQL regressions pass; both Core targets build; Core7da53a7 pushed/verified |

Q01f stays red: native count and standard staged Count/LongCount/Any/All pass on all three;
forward elements now pass20 native cases/store; standard Reverse/Last/from-end now pass12 additional cases/store; explicit defaults now pass12 cases/store; projections and remaining numeric terminals remain pending. No client reduction substitutes for native execution.
Neo uses ProviderDefault graph consistency; ProviderSnapshot explicitly rejects.
Count and rows share provider read owners and source/stage proofs. Native count ignores final graph expansion.

Native TRX: `TestResults/Neo4jCurrent/neo4j-linq-rows-current.trx`.
SHA256 `79E394F129D1BA5A675BD7D03E5B08EE066405DA18D98EF6EEA120691FCD8D6A`.
Summary: `TestResults/Neo4jCurrent/cql-linq-rows-result.json`.
SHA256 `1FC2A66C94E24E39DF5DA538855B30245E9F186CE738E89875B5AD7A9CCF731E`.
Cardinality binding TRX: `TestResults/CoreCurrent/entity-cardinality-binding-regression.trx`.
SHA256 `F8EC790EC9471A964DAC0A168C85C9886E9A8CB012D64ADA39A97B7A31450718`.
Four new shared tests and16 live standard-terminal cases/database verify predicates after windows, All counterexamples, closure rebinding and no entity construction.
[Earlier checkpoint evidence](Archive/CURRENT_STATUS_pre_terminal_binding_2026-10-27.md) preserves other hashes/cohorts.
Full Core suite is not green: earlier3,853 passes/six failures from missing built WEB4 host prerequisite remain unverified.

Forward elements: First/Single/int-index/default/predicate forms verified through native stage and typed hydration owners.
Three new binding tests;20 native cases/store prove ambiguity before DTO construction, defaults/indexes, protected errors and tracking.
Evidence `TestResults/CoreCurrent/entity-element-binding-regression.trx`, SHA256 `BE822F71BD8233CC3BA4A73EBFDE5DEC69AA563A09AD7CAC766A8D6710583F86`.
Last/default/predicate and forward/from-end Index forms now bind native reversal; Last requires explicit sequence ordering. Explicit default-value overloads preserve caller identity only after acknowledged empty native selection. Staged projection/numeric integration remains pending.

Native Reverse stage now passes6 cases/store: source/intermediate windows, double reversal, identity ties, protected errors and count.
Independent permission verified. Standard/tail binding:3 new tests plus12 new native cases/store pass.
Binding evidence `TestResults/CoreCurrent/entity-tail-binding-regression.trx`, SHA256 `922BCB725C78366588DF1D9A061DC76A48D848CA21693AD884382BAE5009344E`.

Explicit defaults:3 new shared tests,12 native cases/store; errors/ambiguity never return the default and caller tracking is preserved.
Evidence `TestResults/CoreCurrent/entity-default-binding-regression.trx`, SHA256 `622118AC0C0A393EEA73B8F10441C019FFEEE0FE6F8322DFBD253BF069EF1B02`.

Shared entity-projection query/plan/contract/executor/V2 manager now preserve the immutable entity source and use existing native-cell validation/DTO construction ownership.
Four new tests verify stage/capability capture, DTO validation order, impossible-window counts and cancellation/completion gates.
Evidence `TestResults/CoreCurrent/entity-projection-regression.trx`, SHA256 `BB77C95E32C10A801B22650E17C332D46BB66F749F73139847A6F3520325EEEF`.
SQLite entity projection enrolled:47 CQL tests and10 live projection cases pass; row/count/projection share source/stage proof checks.
Evidence TestResults/CoreCurrent/sqlite-entity-projection-current.trx SHA256 8F7FB89EE07B43310682CA47B49D98D99CE128BE738BC068BA9DF52CDDAD04C4.
Mongo/Neo entity projection and standard Select remain pending, deferred unless needed by the broader demo.

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
