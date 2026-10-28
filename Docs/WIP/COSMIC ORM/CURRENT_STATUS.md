# Current COSMIC ORM / CQL status

Updated28October2026 UTC. Read first after compaction; update each milestone.

## Work and priority

- Work/build/test only C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql.
- Full ORM/CQL roadmap remains active/incomplete. User28October:pause entity-selected All/element/numeric parity; prioritize demo breadth. Hosted Azure must be free only; never provision paid resources or billable fallbacks.
- Work only on red checklist cells. Freeze green features/cursors absent a reproducible bug.
- HyperDriveV2 only; document legacy gaps. Large POCO/migrations tooling stays deferred.
- [Checklist](CQL_PARITY_CHECKLIST.md), [demo priorities](CQL_DEMO_EXPANSION_ROADMAP.md), [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).

## Latest verified milestone

- Count/LongCount/Any after entity-selected stages/reselection now work in explicit SDK and standard LINQ on SQLite/MongoDB/Neo4j, including terminal predicates.
- Full entity/selected capture and all native columns survive each Select. Only the final DTO is constructed; dropped invalid cells remain errors even before Take(0).
- Both Core targets build;2182 CQL/4266 full Core tests pass. The same34 native cases/database pass:14 final projection,9 combined stages,3 reselection,8 cardinality.
- Per-read closures, explicit scope/capabilities, original page/order, typed exact values and protected errors remain native-owned. No application source/intermediate DTO constructor substitutes for execution.
- Logs/TRX/JSON stay in TestResults/CoreCurrent and TestResults/Neo4jCurrent. Exact hashes and lifecycle outcomes are in the [single resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
- All owned native fixtures removed; pending journals absent, Mongo27029/Neo17687 listeners absent; in-memory SQLite disposed.
- [Demo catalogue](../../../Examples/CosmicCqlDemo/README.md), [manual](CQL_MANUAL.md#cardinality-after-entity-selected-stages), [architecture](CQL_ARCHITECTURE.md), [shared tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicEntityProjectionReselectionTests.cs), [native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlEntitySelectedReselectionAcceptanceTests.cs).

- Protocol membership:ActivityPub321/IPFS195/SOLID393 controlled tests pass;2190 CQL regressions pass. Native3-record/2-record-subset gates pass for all3 protocols. [Usage/limits](CQL_PROTOCOL_MULTI_RECORD.md).
- Shared commented C# sample uses HolonManager/LINQ. All3 -MultiRecord launchers pass native/HTTP admission and exact cleanup.
- ActivityPub browser clicks prove3 then2 records; screenshot unavailable, layout QA open. IPFS/SOLID browser clicks remain open.
- Actual Mongo+Mastodon/IPFS/SOLID SDK pairings now each prove5 physical rows (Mongo2/protocol3), unmatched Mongo control exclusion and exact cleanup. Shared fixture logic; no local query substitute.
- Federated browser presets, cloud/chain pairings and hosted free Azure remain open. Native services stopped, protocol/Mongo pending journals absent.


## Exact next work

1. Hosted Azure Cosmos (not emulator): create/reuse a genuinely free test account only, validate live SDK/HTTP/browser queries and exact cleanup. No Azure CLI/account cache/connector or credential environment was found; official no-subscription sandbox currently displays Under Construction ([admission](CQL_HOSTED_AZURE_FREE_ADMISSION.md)); existing free-subscription sign-in is pending user input. Paid subscription/resource upgrades are unauthorized.
2. IPFS, SOLID and ActivityPub:small filtered queries over several native records, not point reads. ActivityPub native key-set membership is verified; IPFS/SOLID bounded encrypted key-set implementations pass controlled tests. Native collection gates now pass for all three protocols. Exact next step: wire the verified Mongo+Mastodon/IPFS/SOLID SDK pairings into federated browser presets, then finish remaining browser checks (all three protocol launchers/native/HTTP now verified; ActivityPub browser interaction verified), reusing retained fixtures through reviewed completed-journal archival. Native modes: Run-ReusableIpfsTests.ps1 -CqlKeySet; Run-ReusableSolidReadiness.ps1 -CqlKeySet -RequireCleanShutdown. Admit an explicit bounded collection and filter domain; preserve provider ownership, access and protected failures. Do not substitute Mongo/SQLite query results for native participation.
3. Solana functional browser gate verified:rows/nested metadata/projection/LongCount4 clicks, zero source writes, no reseed and clean shutdown. [Evidence](CQL_SOLANA_BROWSER_VERIFICATION.md). Screenshot/layout QA remains open because browser capture unavailable; Mongo+Solana preset remains open.
4. Keep one ready federated-query preset pairing Mongo with each provider. Report participating providers and provenance explicitly; no generic cross-provider parity claim.
5. LINQ depth is paused. Preserved parent/Core work/cosmic-cql-parity-deferred; see [handoff](DEFERRED_PARITY_HANDOFF.md). Do not resume All/element/numeric verification without user direction.

## Other verified scope and limits

- Twelve original investor-demo rows are green on SQLite/MongoDB/Neo4j; Q01a-e/Q23/Q23e/Q49a green within declared scopes. Q01f/full parity remain red.
- Fresh three-database hub:681 SDK queries,138 walkthrough HTTP requests,30 save checks,526 settled operations/608 attempts. Exact fixture reconciliation/cleanup passed; original/local SQLite preserved.
- Cosmos native/local browser:47 native cases,5 manager operations, rows/projection/LongCount controls, readable teaching page. Eight launcher profiles/16 plans pass. Hosted cloud/numeric metadata/conditional lifecycle/production auth and billing remain open.
- IPFS/SOLID:52 native storage checks each,191/384 CQL controls; manager/LINQ/HTTP point profiles. ActivityPub:311 controls/1964 affected regressions; public-status read-only point profile. Richer queries are not verified.
- Solana schema3 selected-metadata SDK/HTTP:50 requests,21 accepted/21 replay rejections/8 unsupported. Exact Int64/null and selected encryption-exempt roots verified; no new seed/source writes. Functional browser interactions now verified (4queries), no reseed/source writes; screenshot/layout QA open.
- Indexed ingestion advances only acknowledged cursors and stops on uncertainty; bounded full-range observation works. Production worker accounting, incremental transport, hosted acceptance and broad snapshots remain open.
- Original Solana root/chunk recovery remains red; no known backup. Never reset/replay the original ledger.

## Publication and resources

- Last Development batch:parent858edd8280294d748e0e4999e39c0a141c5b6042/Corebd86a37fa1df02d50e5d2d0e5ad4bd76cce1d26d; remote verified.
- Reselection/cardinality are later task-branch milestones. Batch further verified Development updates at the agreed daily cadence.
- Reuse SDK10.0.401/NuGet/Agave4.3.0, retained service runtimes and LocalState/COSMIC identities/journals. No new installations this milestone.
- Preserve SolanaCqlCurrent coverage3073, SolanaMetadataCurrent coverage3325, keys/acknowledgements/WSL ledger and reconciliation archive ReconciledFixture892A8AF7.
- All disposable output goes to worktree TestResults, never Docs. Persistent failure/unknown journals stay in LocalState and must be reconciled before reuse.
- Create genuinely missing environments as authorized; inspect/reuse suitable resources first and update the ledger alongside each action.

## Key commands

```powershell
git status --short
git -C 'OASIS Architecture/NextGenSoftware.OASIS.API.Core' status --short
& Scripts/TestHosts/Inspect-CqlDatabaseRetainedStorage.ps1 -MongoEntityProjection
& Scripts/TestHosts/Inspect-CqlDatabaseRetainedGraph.ps1 -EntityProjection
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases -ShowPlan
```

One provider-build lease; /m:2 /nr:false /p:UseSharedCompilation=false.
Commit named files only. Push/verify Core first, then parent gitlink/manifest;
validate after commit with python Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
Fetch/inspect before daily integration; preserve concurrent changes. No force push.
