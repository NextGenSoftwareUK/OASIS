# Three-database demo hub integration

27October2026. Hub routing verified; full native launch remains blocked by a
retained older fixture. Preserve its data; do not treat discovery as native proof.

## Implemented

- The Databases choice delegates Run-CosmicCqlV1Demo.ps1, which owns its build
  lease, native fixture, signed HTTP/SDK checks, interactive finish and cleanup.
- ShowPlan returns the MongoDB/SQLite/Neo4j group, correct runner and5088/demo URL
  without entering a build/service path. VerifyOnly selects bounded execution.
- Database fixture lifecycle rejects the protocol ArchiveCompletedDemo option.
- The joint runner reuses the existing LocalState/COSMIC/mongo-data environment
  instead of allocating another Mongo directory under disposable TestResults.
- Test-CosmicCqlDemoHubRouting.ps1 passes7profiles/14plans/2archive rejections and
  verifies that discovery leaves relevant listeners unchanged. No new dependencies.

```powershell
# Safe discovery; no services start.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases -ShowPlan
& Scripts/TestHosts/Test-CosmicCqlDemoHubRouting.ps1

# Run after retained-fixture reconciliation, not as an overwrite/reset operation.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases -VerifyOnly
# Interactive alternative: http://127.0.0.1:5088/demo ; use Finish demo.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases
```

## Actual native preflight findings

The installed Mongo8.3.11 runtime and retained stopped oasis-neo4j container are
present. Native preflight refused existing cosmic_cql_v1_web4_storage/usage
Mongo databases. An explicit namespace experiment then encountered146retained
Neo4j Holons. Neither attempt created new demo records; services shut down.

A copied ownership snapshot exists at
LocalState/COSMIC/Neo4jCurrent/cql-v1-web4-fixture.json, SHA256
892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5.
It declares147IDs and610requests at before-http-request, with a SQLite path in
the original C:/Source/OASIS checkout. Those identities/outcomes have not yet
been matched to the146live graph records. No cleanup/replay is authorized by a
matching count alone. Original checkout files remain untouched.

The namespace experiment was removed from source because it did not solve graph
isolation. No new database/clone was created. The empty scratch directory
LocalState/COSMIC/CqlV1Demo/cosmic_cql_demo_hub remains inventoried after automatic
review rejected deletion. An initial misplaced ShowPlan dispatcher was corrected;
subsequent discovery tests confirm no listener changes.

## Next

Perform a read-only audit of retained IDs, versions and durable operation outcomes
against the snapshot before designing exact reconciliation. Preserve SQLite,
receipts, protected keys and unknown outcomes. Never reset the shared namespace,
replay source mutations or silently substitute a fresh environment to obtain green.
Cloud teaching-host integration can proceed independently. Broad demo/parity
acceptance remains incomplete; prior three-database evidence is historical proof,
not proof of a successful current launch.

Raw routing/native preflight evidence stays under TestResults. Hashes/resources
and the empty scratch-directory lifecycle are in ENVIRONMENT_AND_CLEANUP_LEDGER.md.
