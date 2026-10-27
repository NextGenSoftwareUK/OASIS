# Current COSMIC ORM / CQL status

Updated: 27 October 2026. Read this first after compaction; update it at every milestone.

## Phase and scope

- Work only in `C:/Source/OASIS-cosmic`; parent and Core branch: `work/cosmic-cql`.
- Full COSMIC roadmap remains active. Current focus: CQL parity on SQLite/MongoDB/Neo4j.
- Work only on red checklist rows. Green features and federation cursors are frozen unless a bug is reproduced.
- The 12 investor-demo rows are green; full EF/NHibernate parity is not complete.
- Q23/Q23e/Q49a are green. Active red row: Q01 standard IQueryable/IQueryProvider integration; native rows, flat Select/selected stages and initial scalar/selected All terminals are verified, full row remains red.

## Verified checkpoints (separate cohorts)

| Scope | Result | Evidence location |
|---|---|---|
| Q01 initial IQueryable rows | 5 selected-element focused +2,514 CQL regression tests passed; both Core targets build;321 native checks passed (107/store) | `TestResults/CoreCurrent/linq-selected-elements-{current,regression}.trx`; `TestResults/Neo4jCurrent/neo4j-linq-rows-current.trx` |
| SQLite checked Int64 Average | 82 native numeric regression tests passed, including3 new ordered-prefix/cancellation kernel tests; no skips | `TestResults/CoreCurrent/sqlite-int64-average-{current,regression}.trx` |
| Neo4j checked Int64 Average | 125 focused CQL/compiler/packet tests pass; no skips. Native proof includes both overflow signs and genuine empty sources on all3 stores | `TestResults/CoreCurrent/neo-int64-average-unit.trx`; native cohort above |
| Shipping source/projected-All C# demo | 24 examples passed,8/store through V2 HolonManager; one test, no skips | `TestResults/Neo4jCurrent/neo4j-source-demo-current.trx` and `cql-source-demo-result.json` |
| Native source-composition matrix | 216 checks passed,72/store; one test, zero skips, across row/count/projection consumers | `TestResults/Neo4jCurrent/neo4j-source-matrix-current.trx` and `cql-source-composition-result.json` |
| Combined native projected-All acceptance | 168 checks passed:50/store plus18 rotated shared-budget cases; one test, no skips | `TestResults/Neo4jCurrent/neo4j-native-current.trx` and `cql-projected-all-acceptance-result.json` |
| Core source capture/admission | 3,851 passed, zero skips; net10.0/netstandard2.1 builds passed | Historical [Core TRX](Evidence/Verification/source-relationships-core.trx.gz) |
| SQLite projected All | 47 native + 9 compiler passed | Historical Evidence/Verification/projected-all-sqlite*.trx.gz |
| MongoDB projected All | 49 native passed | Historical Evidence/Verification/projected-all-mongo.trx.gz |
| Neo4j projected All | 18 native passed; retained graph fingerprints matched | Historical Evidence/Verification/projected-all-neo4j.trx.gz |
| SQLite source composition | 26 projected-All native tests +35 CQL compiler tests passed, zero skips; 12 new source scenarios also verify row/scalar/projection errors and valid traversal controls | `TestResults/CoreCurrent/sqlite-source-{current,unit}.trx` |
| Neo4j source compiler | 163 CQL/compiler tests passed, zero skips; includes two new Boolean-source regressions | `TestResults/CoreCurrent/neo-source-regression.trx` |
| Isolated Mongo compiler worktree | 185 compiler tests passed, zero skips | `TestResults/CoreCurrent/isolated-mongo-source-unit.trx` (regenerable) |

SQLite native/compiler SHA256: `5D6A20E5C935BB5CDD66E50A3196FB3E3CDB1DBAF60CE057EBB0D53E646360D2` / `BAEFE5EF4435B5B176B70B3B43C252CF760CB7A97D3C091E7C4B50C3B265DC15`.
Neo4j compiler evidence SHA256: `1B9160325E95DCA84BEA9E9BBD4DD7B0FCCDC1EF8AF3CAB912E56D03721F4B34`.
Mongo compiler evidence SHA256: `6e0e742808535f38509095987b8704fe86d7b6676d54c9612b119d0bd80597ca`.
The isolated Mongo fix and four new compiler tests are checkpointed separately from the documentation tidy-up. Focused native proof now passes: one test, 53 checks; all six records and both journals unchanged, zero creates replayed.
Native TRX SHA256: `F2823DBE989A4950244CF6EA74930A408E3EF6FF4AE935EF978E685CB944ED3F`.
Native evidence: `TestResults/SubscriptionUsageCurrent/mongo-source-relationships-current.trx`; the fixture summary is alongside it. Documentation-only commits must not stage code or tests.

## Current state and next step

- All three providers enroll source composition. Mongo185 compiler/53 native checks, SQLite35 compiler/26 native tests, Neo4j163 compiler tests; combined gate now proves the protected-source/selected-All intersection natively.
- Q23e gate passed all168 checks, including protected scopes before empty windows, visibility,12 exact scalar domains, cycles, rotated shared budgets and zero-budget empty participation.
- The reviewed gate replayed no creates, then installed its owned cycle controls and verified exact cleanup of its six IDs/store. Its completed pending journal was removed only after success. Do not invoke reviewed reconciliation again against the removed fixture.
- Original WEB4 fixture/journal unchanged: SHA256 `892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5`.
- Persistent data/original journal remain ignored in `LocalState/COSMIC/`; never clear this with TestResults. Mongo27029 and Neo4j17687 stopped after the gate.
- Native test SHA256: `C5D5DF88D3C5F95818BB08B820091CDC8A04A6AD244EDB6AE3B4B389A38F189C`; summary SHA256: `1E28A326798382BC5DC8236AB5F91E5B97C8BE1FBCC342A95F20C7CB47CC76E2`.
- Q49a matrix passed216 checks: all four nesting orientations, AND/OR/NOT, positive/negative results, hidden intermediate parents and protected limit errors before empty windows across row/count/projection consumers. Exact cleanup and unchanged original fixture verified.
- Use the small `Providers/Storage/TestProjects/CosmicCql.NativeAcceptanceTests/` project for native-only acceptance. It links the same authoritative gate without requiring the unrelated WEB4 host graph.
- Parent Q23 audit complete: independent Q23a–e/Q49a evidence plus24 shipping C# examples through the V2 manager; freeze these green rows unless a bug is reproduced. Full681-case legacy SDK/HTTP walkthrough is not newly verified by these focused gates.
- Exact next step: implement Q01 selected cardinality and post-window cardinality predicates through existing native planners/manager owners, preserving standard per-execution parameter binding and window semantics; add focused/native tests. Keep Q01 red until full scoped proof.
- Current Q01 supports named single-provider entity Where/ordering/Skip/Take, synchronous/async rows and native Count/LongCount/Any/All. Flat Select, selected Where/order/windows and selected All now work natively. Entity First/Single/Last/ElementAt families use the native element owner; selected families now use the V2 composition owner with native reverse stages and cardinality-before-DTO proof. Selector Min/Max/Sum and decimal/Single/Double Average, plus unstaged scalar-selected aggregates now work. Int32/nullable Int32 Average double output is independently enrolled; fluent decimal Average is unchanged. All three providers' Int64/nullable Int64 double Average is verified with ordered prefix overflow, reverse order and post-page accumulator proof. Staged numeric reductions, selected cardinality, repeated Select and post-window cardinality predicates remain explicit unsupported. Standard variables bind per execution; fluent CosmicQuery capture remains unchanged.
- Previous full Core regression:3,853 passed/6 failed because this worktree lacks the built WEB4 host artifact (AdmittedKeyManagerControllerTests). No skips; do not claim a full green suite. Build/verify that prerequisite later in this worktree, never copy old checkout outputs.
- Core increment pushed/remote-verified: `31b74ceaccdf7aadfcf0ff4b6f612f3c13edd827`; parent gitlink and Docker manifest advance together.
- Source matrix TRX SHA256: `6574CE43391BAAF77C4C3AF2979DBF175D4561236D13E8DDD2FB2FB9822B4A47`; summary SHA256: `0D04CA0D491B252A556253A77DB7E29FF4854C3E86AD1513146B6FC78ECDB40E`.
- Demo TRX SHA256: `1329E5180985368F4B8BCFD00C5F2D3E4DE74AD23DC2CCB341B01752C8A982B5`; summary SHA256: `2B86ED1E976B4DA87C412A86BA0486D162100C6CF07893F6B456350F33EE0E20`.
- Q01 CQL regression SHA256: `4955556CFFF2C2E23993E8115EFB787ACFFD4A5072DD963382AA5C23060185D0`; native TRX SHA256: `5B3F448C79DB1960AF33E6484D58B11ACA09BE52F960FD2625737308A889AC80`; summary SHA256: `46A7AFB1176F84EA48BBFE435AD1953ECDA4C3DDEECE0358A6085A3E7FA3752D`.
- Int64 focused/Mongo TRX SHA256: `8BABB6B89F5885A83528D98E4EE38EF58F46F38361809441CBE358104C71C764` / `DD1929C2A0DA282E17E24F275A3153FA4D1D79D6A9A504A20AF735A7FC83CCA2`.
- SQLite numeric regression SHA256: `6A84F6B97D488974E7F1F0C4361FE874A331315A6308F6B6E0B7E8DF137E1BAD`; focused kernel SHA256: `D7AC0092963AA634E04A01C48A1D3D564D34455DE5A41D3FCB7C2C5095680FA4`.
- Neo4j focused CQL TRX SHA256: `A2388132CCA53A4DE097ECCBBF4EAF46CBB46C82425258BA2D75A365322366A1`.
- Selected-element focused TRX SHA256: `DEA9D66D1DCE01BFF9517631D5EC46EE722D8995D0F5E4D3CDA9A8375BA03ABE`; 5 tests cover pre-constructor cardinality, present null/default distinction, forged proofs, native errors, independent tail enrollment and V2 admission.
- Documentation consolidation:10 active root reports,126 archived reports,334 byte-preserved historical artifacts; recovery set stays below300 lines.
## Key commands (PowerShell, cwd above)

```powershell
git status --short
git fetch origin
git merge --no-edit origin/Development
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' fetch origin
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' merge --no-edit origin/Development
# Native-only acceptance: fresh owned fixture only after the previous gate has completed exact cleanup.
& 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1' -Owner 'cosmic-cql' -Operation {
    & 'Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1' -CosmicCqlLinqRows
}
```

Build with `/m:2 /nr:false /p:UseSharedCompilation=false`; reuse one local environment.
No routine Providers-agent messages; contact only for overlapping Core/Shared public API work.
Merge verified work to Development once/twice daily; merge Development back daily. Verify submodule pushes with `git ls-remote`.
Logs/TRX/seed output go only in this worktree's `TestResults/`. Do not commit new raw evidence; record counts, SHA256 and paths in reports.
[Parity checklist](CQL_PARITY_CHECKLIST.md) controls scope. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) controls state/cleanup. [Archive](Archive/README.md) holds history.
