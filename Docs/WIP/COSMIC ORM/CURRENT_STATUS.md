# Current COSMIC ORM / CQL status

Updated28October2026 UTC. Read this first after compaction; update each milestone.

## Work and priority

- Work/build/test only C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql.
- Full ORM/CQL roadmap remains active/incomplete. Eight-provider demo breadth precedes remaining EF/NHibernate depth.
- Work only on red checklist cells. Freeze green features/cursors unless a bug is reproduced.
- HyperDriveV2 implementation only; document legacy gaps. Large POCO/migrations tooling stays deferred.
- [Checklist](CQL_PARITY_CHECKLIST.md), [demo priorities](CQL_DEMO_EXPANSION_ROADMAP.md), [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).

## Latest verified milestone

- Shared indexed SDK/LINQ/V2 manager and stock WEB4 support rows, canonical/metadata projection, Count/LongCount/Any and explicit actor/source/index/freshness admission.
- Schema3 exports only selected encryption-exempt metadata roots, authenticates projection/profile/revision and publishes history/rows/checkpoint atomically. Schema2 is retained unchanged.
- Selected cells are compared with validated source witnesses before DTO construction. Wildcard/private roots, unsupported domains and changed intent reject; exact Int64/null wire values are preserved.
- Final Core4243pass; full SQLite provider219pass (112indexed-store checks). Existing Solana799unit checkpoint remains verified; no new provider implementation edits in this milestone.
- Live retained-validator schema3 SDK/HTTP passes1/1:50requests,21accepted,21replays rejected,8unsupported before reservation. Standard LINQ Select returns exact DTO values/source receipts.
- Interactive HTTP page/profile/selection/shutdown passes with an additional selected request: amount"9007199254740993", missingnullablecellnull, no privateNote in response. Node syntax/example build pass; browser click/rendering unverified.
- No new seed/source writes this milestone. Validator18899and page5089listeners verified absent after teardown.
- [Metadata contract/evidence scope](CQL_INDEXED_METADATA.md), [HTTP contract](CQL_INDEXED_HTTP.md), [manual](CQL_MANUAL.md), [launcher](CQL_INTERACTIVE_POINT_DEMO.md).

## Exact next work

1. Three-database hub is restored: pinned fixture reconciliation passed; 14 persistent archive/journal files total207492bytes, original/local SQLite preserved. Fresh native demo passes681SDK queries,138walkthrough HTTP requests,30save checks,526settled operations/608provider attempts; exact fixture cleanup and service teardown pass. See CQL_DATABASE_DEMO_HUB.md.
2. CosmosDB teaching page/hub native, interactive HTTP and actual browser rendering/clicks are verified:rows/projection/LongCount each200; code23.4px line height at13px font, no page horizontal overflow; clean teardown. Eight launcher profiles/16discovery plans pass. Same arbitrary query across all eight providers is not verified; IPFS/SOLID/ActivityPub remain point profiles.
3. Continue only red demo/parity gates: first remaining non-cloud browser/demo acceptance, then Q01f Mongo/Neo entity projection and numeric/stage integration. Freeze green features/cursors unless a reproducible bug appears.
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
