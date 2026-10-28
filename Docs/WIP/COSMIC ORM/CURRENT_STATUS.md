# Current COSMIC ORM / CQL status

Updated28October2026 UTC. Read this first after compaction; update each milestone.

## Work and priority

- Work/build/test only C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql.
- Full ORM/CQL roadmap remains active/incomplete. Eight-provider demo breadth precedes remaining EF/NHibernate depth.
- Work only on red checklist cells. Freeze green features/cursors unless a bug is reproduced.
- HyperDriveV2 implementation only; document legacy gaps. Large POCO/migrations tooling stays deferred.
- [Checklist](CQL_PARITY_CHECKLIST.md), [demo priorities](CQL_DEMO_EXPANSION_ROADMAP.md), [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).

## Latest verified milestone

- Q01f: native final entity-stage Select remains verified on all3 (14 cases/store).
- New combined entity-selected contract preserves both immutable sequences, re-admits actual capabilities and validates all selected input cells before DTO construction. Flat execution rejects combined plans.
- SQLite, MongoDB and Neo4j now execute selected Where/order/ThenBy/Skip/Take/Reverse after entity stages under their existing native read owners. Standard sync/async LINQ works, closure values rebind per read, original offsets are not replayed, source application constructors are not invoked.
- Both Core targets build;2173 CQL tests and221 full SQLite provider tests pass. SQLite proof covers exact Int64, invalid cells hidden by Take(0), protected traversal and entity work limits. Mongo220/Neo151 CQL controls and23 native cases per database (14 prior +9 combined) now pass; no Q01f cell promoted.
- Combined graph sources, reselection and element/cardinality/numeric terminals after entity selection remain unsupported. Earlier green scopes stay frozen.
- Neo/SQLite evidence in TestResults/Neo4jCurrent: neo-entity-selected-unit.trx:ADEF3FD50AF8394691D7D085322ED2F4CADF89E94EC97405ACC64824BD83777F; neo-entity-projection-native.trx:AD4BB05BC55FC7106D35537BE8F32BF2A7CE1D311A7BA4D8292E55B2E03D398E; Neo JSON:2AC91B0F9409A877CC084103D5D318D9B5858E7F2100C57F8961B35BC702FC7A; SQLite JSON:898EEFACE177E922DB613431DE9F1506C24520B30BC37ADF6ABD4B02786641A2. Exact fixture cleanup/journal removal, stopped container and absent17687 verified.
- Mongo evidence in TestResults/Neo4jCurrent: mongo-entity-selected-unit.trx:E3B631277F86D2D9AFF00BEBDF55C73A1F8622D2C9524A5CCA8247B8B3EB435B; mongo-entity-projection-native.trx:69C16B3B9F615F5EE782CA0ED7411D58FCF75A146AFC07F539160EDE329C1F86; JSON:AA04C90275F44D9E914AA01007D378F8BA2684F10EDEA2C46965859F30D67924. Exact fixture cleanup, journal removal and27029 listener teardown verified.
- Evidence in TestResults/CoreCurrent: entity-selected-owner-core.trx:8B2D359FCEB9C1E74B780E345F3698EC7693F2C1E6AAA854F53723F3DD3694E9; entity-selected-sqlite-full.trx:629CCDA3A1E72922E36752B2CCB5125DC334C7C348D2BF61B6A6ADDE0ED38AC9.
- See [manual](CQL_MANUAL.md#explicit-capture-of-selected-stages-after-entity-projection), [shared owner tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicEntitySelectedExecutionTests.cs), [SQLite proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests/CqlEntitySelectedCompositionTests.cs).

## Exact next work

1. Three-database hub is restored: pinned fixture reconciliation passed; 14 persistent archive/journal files total207492bytes, original/local SQLite preserved. Fresh native demo passes681SDK queries,138walkthrough HTTP requests,30save checks,526settled operations/608provider attempts; exact fixture cleanup and service teardown pass. See CQL_DATABASE_DEMO_HUB.md.
2. CosmosDB teaching page/hub native, interactive HTTP and actual browser rendering/clicks are verified:rows/projection/LongCount each200; code23.4px line height at13px font, no page horizontal overflow; clean teardown. Eight launcher profiles/16discovery plans pass. Same arbitrary query across all eight providers is not verified; IPFS/SOLID/ActivityPub remain point profiles.
3. Exact next: implement reselection after entity-selected stages, preserving retained columns, source identity/order and native proof-before-DTO; then integrate element/cardinality/numeric terminals and combined graph sources. Q01f stays red. All3 combined stage implementations now have the same23-case native proof; Neo keeps ProviderDefault restrictions and rejects unsupported snapshot scope.
4. Indexed other aggregates/selected stages, hosted production auth/durable settlement, scaling and full ORM/EF/NHibernate parity remain open. Create genuinely missing environments as authorized, reusing suitable resources first.

## Other verified scope and remaining limits

- Three databases:12original investor-demo rows green; Q01a-e/Q23/Q23e/Q49a scoped green. Q01f and full EF/NHibernate parity remain red.
- Cosmos DB:46controls/47native cases and5manager operations; controlled-identity HTTP rows/scalar/projection passed. Hosted cloud, numeric metadata, conditional lifecycle and production auth/billing remain open.
- IPFS/SOLID:52native storage checks each;191/384CQL controls. Native manager/LINQ/sample/HTTP point profiles passed; richer general queries remain open.
- ActivityPub:311provider controls/1964affected regressions; native public-status point manager/LINQ/sample/HTTP passed. No posting/federation or broad query claim.
- Indexed ingestion session serializes compare-and-publish, advances only acknowledged cursors and stops on failure/uncertainty. Host uses5second bounded complete-range re-observation; incremental transport/production worker accounting/scaling remain open.
- Production JWT, durable external outbox/authority settlement, hosted-chain acceptance and broad cross-provider snapshots are unverified.
- Original Solana recovery remains red: no known backup and missing original root/chunk. Never reset/replay the original ledger or call a new declared-range fixture recovery.

## Resources and recovery

- [Single cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns resource paths/hashes/retention. Reuse SDK10.0.401/NuGet/cache/Agave4.3.0; no new installations in this milestone.
- Persistent schema2 state:LocalState/COSMIC/SolanaCqlCurrent, coverage3073. Schema3:LocalState/COSMIC/SolanaMetadataCurrent, coverage3325, acknowledged recorda03306d4-649f-4871-8f27-8e13869b59a1/index86d676b6-c2c5-4c8f-98b2-a88e0fb8bb11.
- Preserve keys, acknowledgements, uncertain/pending journals and retained WSL ledger. Schema3 index32768bytes/ack511bytes; no pending seed. Completed prior TRX/JSON evidence is hash-archived by the runner.
- All logs/TRX/generated JS stay in worktree TestResults; owned disposable SQLite fixture is torn down. Build outputs are regenerable; source/keys are not cleanup targets.
- Shipping dependency graph plans:TestResults/ProtocolCqlCurrent/index-http-web4-*.json (69projects/61in-worktree). Reuse8external sibling outputs read-only; never recursively rebuild outside this worktree.
- [Earlier detailed checkpoint](Archive/CURRENT_STATUS_before_selected_metadata_live_2026-10-27.md).

## Key commands

```powershell
git status --short
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' status --short
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -ShowPlan
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -VerifyOnly -ArchiveCompletedDemo
# Interactive alternative; stop via POST http://127.0.0.1:5089/demo/shutdown
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -ArchiveCompletedDemo
```

All builds use one provider-build lease and /m:2 /nr:false /p:UseSharedCompilation=false.
Commit named owned files only. Core push/ls-remote first, then parent gitlink/manifest;
validate with python Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
Fetch/inspect before daily integration; preserve concurrent changes. Batch verified
Development pushes once/twice daily; task-branch push does not imply a dev merge.
