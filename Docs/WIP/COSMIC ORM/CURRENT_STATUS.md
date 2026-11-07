# COSMIC ORM / CQL current status

Updated 6 November 2026. Goal active and incomplete.

## Current phase

Demo breadth across providers, with native queries, MongoDB federation,
live sandbox access and local examples. LINQ depth after entity-selected
stages remains paused. Work on unverified scopes and reproduced bugs.

## Recovery state

- Worktree C:/Source/OASIS-cosmic, branch work/cosmic-cql-ton-public.
- Core branch work/cosmic-cql-decentraland. Preserve independent dependency pins.
- Website C:/Source/Web4Site, branch work/cosmic-cql, published a88076d.
- Development and successful code release dcae58d97, Core f695b1cd.
- Successful settings deployment fb589d55-f737-4622-9160-6e63e0e55099.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **118 scoped local, 72 live HTTP and 28 functional browser**.
The [full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) records each scope.

- MongoDB, SQLite and Neo4j provide the principal owned-storage demo paths.
- IPFS, SOLID and ActivityPub passed hosted multi-row queries and Mongo federation.
- Solana and Google completed browser native 3/2, paired 5/4, reuse and isolation.
- LocalFile and Decentraland passed hosted native 3/2 and paired 5/4.
- Push Protocol, LayerZero v1 and Reclaim passed hosted native 3/2, paired 5/4,
  repeat preparation with zero saves, foreign-private zero and replicas 2/2.
  Their sandbox cards are published with local C# examples.
- Waku passed native Store 3/2/0, loopback WEB4/Mongo 5/4, restart and snapshot.
  Its descriptor requires all three reviewed records before activation.
  The exact archive is retained in LocalState/COSMIC/WakuQualifiedSourceCurrent.

## Next work

1. Finish open browser gates using retained cohorts and journals.
   Static publication and HTTP results are separate evidence.
2. Prepare a scoped GUN/OrbitDB image with teaching_sidecars=true.
   Bootstrap a separately journalled hosted source on the existing volume,
   qualify native/restart reads, then enroll its native/Mongo presets.
3. Finish Waku managed Store packaging and lifecycle. The earlier local
   supervisor-writing command was rejected by automatic tool policy.
4. Complete PouchDB hosted rollout after its original three source files
   arrive from the old laptop. The candidate image is already qualified.
5. Continue other providers' runtime, restricted-reader and hosting gates.

World ID, Discord, Livepeer and Pinata passed hosted HTTP acceptance.
Oracle Object Storage, Cloudinary and Proof of Humanity image CI 42336260220
passed for dcae58d97. Code900d0fe7/settingsfb589d55 both succeeded.
All three passed native3/2, Mongo5/4, repeat0new/2existing,
independent private0/public3 and both replicas2/2. Original cohorts are retained.
Website a88076d publishes their three cards and pinned evidence/C# links.
All114 website tests passed. Public HTML239108bytes matches the exact commit,
SHA2568773ee77638a24988968db32a15d58069424ac2e03604639a03d244198e65666.
Browser interaction remains open. GUN/OrbitDB original fixture directories
are absent here. The workflow now selects their existing optional runtime
with distinct commit-teaching tags; all three execution checks passed.
Next prepare the scoped enabled image, including the test-project correction.
Subscription protocol CI exposed a missing SandboxSubscriptionRules source link.
The project dependency is fixed locally: bounded build and 22 pure tests passed.
Include that test-project-only correction in the next verified Development batch.

## Constraints and unfinished roadmap

- Hosted Azure Cosmos, Oracle DB and PostgreSQL remain deferred by David.
- Preserve accepted source identities, failed cohorts and original receipts.
- Never replay completed acceptance or replace fixtures to rebuild evidence.
- Broader ORM children/depth/history/deltas, migration, placement and HyperDrive
  operator, transport and scale acceptance remain incomplete.
- Personal WSL Miniforge and ML models are explicitly retained.
- Disk target 107 GB free remains unmet. Clean owned disposable artifacts as work proceeds.

## Commands and entry points

Use one provider build lease and /m:2 /nr:false /p:UseSharedCompilation=false.
Logs/results go to TestResults and durable receipts to LocalState/COSMIC.
Stage named files. Development pushes are authorized for live sandbox testing.

Run git fetch origin Development before integration. Validate pins with
python -B Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
Observe a specific deployment with Scripts/Sandbox/Observe-HostedDeployment.py,
providing its ID, --expected-commit and an --output path under TestResults.

[Parity checklist](CQL_PARITY_CHECKLIST.md) · [CQL roadmap](CQL_COMPLETION_ROADMAP.md) ·
[ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) · [Implementation status](IMPLEMENTATION_STATUS.md) ·
[Manual](CQL_MANUAL.md) · [Architecture](CQL_ARCHITECTURE.md) ·
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) ·
[David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md) ·
[Archived status history](Archive/CURRENT_STATUS_history_2026-11-06.md)
