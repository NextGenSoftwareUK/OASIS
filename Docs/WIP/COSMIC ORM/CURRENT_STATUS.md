# Current COSMIC ORM / CQL status

Updated: 27 October 2026. Read this first after compaction; update it at every milestone.

## Phase and scope

- Work only in `C:/Source/OASIS-cosmic`; parent and Core branch: `work/cosmic-cql`.
- Full COSMIC roadmap remains active. Current focus: CQL parity on SQLite/MongoDB/Neo4j.
- Work only on red checklist rows. Green features and federation cursors are frozen unless a bug is reproduced.
- The 12 investor-demo rows are green; full EF/NHibernate parity is not complete.
- Current red rows: **Q49a source relationship composition**, then **Q23e projected All acceptance**.

## Verified checkpoints (separate cohorts)

| Scope | Result | Evidence location |
|---|---|---|
| Core source capture/admission | 3,851 passed, zero skips; net10.0/netstandard2.1 builds passed | Historical [Core TRX](Evidence/Verification/source-relationships-core.trx.gz) |
| SQLite projected All | 47 native + 9 compiler passed | Historical Evidence/Verification/projected-all-sqlite*.trx.gz |
| MongoDB projected All | 49 native passed | Historical Evidence/Verification/projected-all-mongo.trx.gz |
| Neo4j projected All | 18 native passed; retained graph fingerprints matched | Historical Evidence/Verification/projected-all-neo4j.trx.gz |
| Isolated Mongo compiler worktree | 185 compiler tests passed, zero skips | `TestResults/CoreCurrent/isolated-mongo-source-unit.trx` (regenerable) |

Latest compiler evidence SHA256: `6e0e742808535f38509095987b8704fe86d7b6676d54c9612b119d0bd80597ca`.
The isolated Mongo fix and four new compiler tests are **uncommitted code work**, not native acceptance proof.
This documentation-only checkpoint must not stage that code or tests.

## Open defect and preserved state

- The combined 168-check native gate is incomplete. The reviewed run verified six fixture IDs/store and matching actor/version controls, without creates.
- Reproduced defect: Mongo scalar root translation rejects relationships nested below Boolean logic.
- Draft fix reuses its existing protected relationship evaluator and full-AST membership detection; compiler tests pass, native proof remains pending.
- Mongo source capability enrollment is draft/uncommitted; SQLite/Neo4j remain unenrolled for Q49a.
- No cycle mutation, create replay, reset or fixture cleanup has occurred.
- Persistent fixture data/journals: ignored `LocalState/COSMIC/`; **never clear it with TestResults**.
- Pending journal SHA256: `FE53AE55DF84AC35E5A7D0A6787CDC51BB7AC4B5CDFC4207164D38C4B300EA1F`.
- Original WEB4 journal SHA256: `892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5`.

Documentation consolidation is complete: 10 active root reports, 126 archived reports and 334 historical artifacts moved without changing their bytes. Recovery read set stays below 300 lines.

## Exact next step

1. Resume the draft Mongo source fix after the separate docs-only tidy commit; leave source commits separate.
2. Read `MongoCqlTranslator.Ordering.cs`, `MongoCqlTranslator.cs` and the active [acceptance contract](CQL_PROJECTED_ALL_EXECUTION_CONTRACT.md).
3. Bootstrap only missing local consumer dependencies from the global NuGet cache; reuse existing runtimes and frozen sibling-library inputs, never build in the old checkout.
4. Verify the Mongo fix natively against the pinned reviewed fixture; then implement/enroll SQLite/Neo4j source composition as needed.
5. Complete all protected-scope, scalar-domain, cycle and rotated cross-provider budget checks before promoting Q49a/Q23e.
6. Once green, update demos/docs, commit named source files, push and resume the next red checklist row.

## Key commands (PowerShell, cwd above)

```powershell
git status --short
git fetch origin
git merge --no-edit origin/Development
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' fetch origin
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' merge --no-edit origin/Development
# Prepared reviewed native gate; missing local build assets must be bootstrapped first.
& 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1' -Owner 'cosmic-cql' -Operation {
    & 'Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1' -CosmicCqlProjectedAllAcceptanceReconcile
}
```

Build with `/m:2 /nr:false /p:UseSharedCompilation=false`; reuse one local environment.
No routine Providers-agent messages; contact only for overlapping Core/Shared public API work.
Merge verified work to Development once/twice daily; merge Development back daily. Verify submodule pushes with `git ls-remote`.
Logs/TRX/seed output go only in this worktree's `TestResults/`. Do not commit new raw evidence; record counts, SHA256 and paths in reports.
[Parity checklist](CQL_PARITY_CHECKLIST.md) controls scope. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) controls state/cleanup. [Archive](Archive/README.md) holds history.
