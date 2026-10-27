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
| Q01 IQueryable verified increment | 22 selected-terminal/numeric/extrema/reselection focused +2,536 CQL regression tests passed; both Core targets build;1,455 native checks passed (SQLite500/Mongo500/Neo455);207 Mongo and137 Neo CQL/compiler/packet tests pass | `TestResults/CoreCurrent/linq-staged-numeric-{current,regression}.trx`; `TestResults/Neo4jCurrent/neo4j-linq-rows-current.trx` |
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
- Exact next step: implement Neo Q01e native lowering for PostWindowPredicate/PostWindowLimit; SQLite/Mongo Q01e now verified including expanded graphs/visibility. Preserve terminal position and protected-scope/cardinality errors; Q01c green on all3 with218 native cases/store; Q01b now green on all3 with37 wider native cases/store. Q01a is green on all3 (27 native cases/store); freeze it unless a reproducible bug appears. Selected Min/Max Q01c is now green on all3. Repeated Select Q01d now verified on all3 (26 native cases/store); Neo entity element predicates after paging and general post-window entity composition remain on the Q01 list. Keep Q01 red until full scoped proof.
- Current Q01 supports named single-provider entity Where/ordering/Skip/Take, synchronous/async rows and native Count/LongCount/Any/All. Flat Select, selected Where/order/windows and selected All/Count/LongCount/Any now work natively; cardinality has independent enrollment and exact native acknowledgement validation without DTO constructors. Entity First/Single/Last/ElementAt families use the native element owner; selected families now use the V2 composition owner with native reverse stages and cardinality-before-DTO proof. Selector Min/Max/Sum and decimal/Single/Double Average, plus unstaged scalar-selected aggregates now work. Int32/nullable Int32 Average double output is independently enrolled; fluent decimal Average is unchanged. All three providers' Int64/nullable Int64 double Average is verified with ordered prefix overflow, reverse order and post-page accumulator proof. Entity Count/LongCount/Any predicates now remain after source windows through independently enrolled scalar lowering, including protected child/descendant grammar. All3 staged signed Int32/Int64 Sum/Average (nullable included) now use the shared exact numeric acknowledgement with no DTO construction. Neo entity element predicates after paging and general post-window entity composition remain unsupported. Standard variables bind per execution; fluent CosmicQuery capture remains unchanged.
- Previous full Core regression:3,853 passed/6 failed because this worktree lacks the built WEB4 host artifact (AdmittedKeyManagerControllerTests). No skips; do not claim a full green suite. Build/verify that prerequisite later in this worktree, never copy old checkout outputs.
- Core increment pushed/remote-verified: `3632f007247253e13b1705483358e1809061383c`; parent gitlink and Docker manifest advance together.
- Source matrix TRX SHA256: `6574CE43391BAAF77C4C3AF2979DBF175D4561236D13E8DDD2FB2FB9822B4A47`; summary SHA256: `0D04CA0D491B252A556253A77DB7E29FF4854C3E86AD1513146B6FC78ECDB40E`.
- Demo TRX SHA256: `1329E5180985368F4B8BCFD00C5F2D3E4DE74AD23DC2CCB341B01752C8A982B5`; summary SHA256: `2B86ED1E976B4DA87C412A86BA0486D162100C6CF07893F6B456350F33EE0E20`.
- Q01 CQL regression SHA256: `6013982BE602A7BFFAB47B7130D3794B19EB4FBD63E707B6F22F4F4A2CC74651`; native TRX SHA256: `2F93441DEBCB9C191DB3375642EC10764765A42819EE012DC8172E64F22D2005`; summary SHA256: `C780318DEC724A3E2CEC8D43FD7D3FA21267FBCE045140236E6E41B3FD1A593A`.
- Int64 focused/Mongo TRX SHA256: `8BABB6B89F5885A83528D98E4EE38EF58F46F38361809441CBE358104C71C764` / `DD1929C2A0DA282E17E24F275A3153FA4D1D79D6A9A504A20AF735A7FC83CCA2`.
- SQLite numeric regression SHA256: `6A84F6B97D488974E7F1F0C4361FE874A331315A6308F6B6E0B7E8DF137E1BAD`; focused kernel SHA256: `D7AC0092963AA634E04A01C48A1D3D564D34455DE5A41D3FCB7C2C5095680FA4`.
- Neo4j focused CQL TRX SHA256: `A2388132CCA53A4DE097ECCBBF4EAF46CBB46C82425258BA2D75A365322366A1`.
- Selected-element focused TRX SHA256: `DEA9D66D1DCE01BFF9517631D5EC46EE722D8995D0F5E4D3CDA9A8375BA03ABE`; 5 tests cover pre-constructor cardinality, present null/default distinction, forged proofs, native errors, independent tail enrollment and V2 admission.
- Cardinality focused TRX SHA256: `2E44610153F33AE8E5CEAB4B75EE22C0177940999B16134F99B5524B6720F0F8`; reviewed native gate replayed no creates, exact-cleaned6 IDs/store and removed its completed journal. Do not rerun `-CosmicCqlLinqRowsReconcile` against the removed fixture; use the fresh mode below.
- Entity-cardinality focused TRX SHA256: `317BCB396D77CC0A38DDB410847022C6F9697FE60D9C5495EC70CEACB8147DFA`; native cases prove both paging positions, empty scopes, protected true-OR/false-AND traversal errors, child/descendant matching and exact Int64 metadata.
- Staged-numeric focused TRX SHA256: `BDFC8CE961AB7893C6BC4E97D23DAD6DA5128D16C11369C02AD514AF817BD7F0`; 14 tests include4 new numeric boundary/ownership cases. All3 native scope is27 additional signed-integer cases/store; SQLite wider domains now independently enrolled; all3 Q01b cells are green.
- Mongo staged numeric unit TRX: `TestResults/CoreCurrent/mongo-staged-numeric-current.trx`, SHA256 `B0F7416A1CA88DA453C7591AF1ACBED4F19E006FCD2714C92C2F0FFDCA8FAF95`;194 tests include4 new selected compiler/envelope/empty/legacy-admission cases.
- Neo staged numeric unit TRX: `TestResults/CoreCurrent/neo-staged-numeric-current.trx`, SHA256 `9DFDBB2AF8C4B5B3B078B5E6EC81FDEAD4F5357E16824054149802B97513331A`;129 tests include4 new selected compiler/packet/empty/legacy-admission cases.
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


[Earlier selected-numeric checkpoint evidence](Archive/CQL_SELECTED_NUMERIC_CHECKPOINTS_2026-10-27.md) is archived.


[Earlier selected-extrema/shared-binding checkpoints](Archive/CQL_SELECTED_EXTREMA_CHECKPOINTS_2026-10-27.md) are archived.

Q01d all3:26 native repeated-selection cases/store;1,365 native checks (455/store), no skips. Exact6-ID cleanup/store, unchanged original journal, completed pending fixture removed; owned services stopped. Core06a2cda remains current. Evidence SHA256: `TestResults\Neo4jCurrent\neo4j-linq-rows-current.trx` `D4515567874937A6B0015F5DEEEF3829481E1A8D9F530DFBF0E2826032612A56`; `TestResults\Neo4jCurrent\cql-linq-rows-result.json` `4F34E7DE8D12BC6FEE1969FDDC17F66AD265F5C7FCA5B9CCEA9516D072FA3E5F`. Shared regression proof:2,536 pass, `TestResults/CoreCurrent/repeated-select-regression.trx`. Next Q01e; Q01f remains red.

SQLite Q01e:45 native cases pass;1,410 native checks total (SQLite500/Mongo455/Neo455),35 SQLite compiler tests,2,540 Core CQL regressions including4 new owner/graph cases; no skips, both Core targets build. Exact6-ID cleanup/store, unchanged original journal and owned services stopped. Native+Core/SQLite evidence SHA256: `TestResults\Neo4jCurrent\neo4j-linq-rows-current.trx` `89A7D5A87935DA9A3B1138132A759323F8DC5E17F6AE5713972A97566903CA28`; `TestResults\Neo4jCurrent\cql-linq-rows-result.json` `7864B9CB4BD905436F48FF9F80D0B812F9630580694BDBCC5DF28628F5666D1F`; `TestResults\CoreCurrent\post-window-elements-regression.trx` `E690F96F49D71C532264314A5712A01877917E7A31F870AEFEB6D57A85518854`; `TestResults\CoreCurrent\sqlite-post-window-elements-unit.trx` `6D202547EA70F8EFB785C6CC3581EF6D0D9D76335AD3B9D235B1FF51CFEAC3C4`.

Mongo Q01e:45 native cases pass,1,455 native checks total (SQLite500/Mongo500/Neo455),207 Mongo CQL tests including5 new compiler/window/envelope/enrollment cases; no skips. Exact6-ID cleanup/store, original journal unchanged and owned services stopped. Native+unit evidence SHA256: `TestResults\Neo4jCurrent\neo4j-linq-rows-current.trx` `569C6CBAD49E1D8214D682216803C3C01E317458B96B72DFA2A284B5C5FD091D`; `TestResults\Neo4jCurrent\cql-linq-rows-result.json` `E123BD2442A8791D4A799735D4426B7432B71BC390B63C5B60DFB6DF375A1F01`; `TestResults\CoreCurrent\mongo-post-window-elements-unit.trx` `011077C0571108F6105237D61DABD67949F268E5A22DD41205ECF6DD77CA6DF6`. Core3632f00 unchanged; next Neo Q01e.
