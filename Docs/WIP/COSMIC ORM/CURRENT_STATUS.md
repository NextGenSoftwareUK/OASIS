# COSMIC ORM / CQL current status

Updated 7 November 2026. Goal active and incomplete.

## Current phase

Demo breadth across providers: native queries, MongoDB federation,
live sandbox access and local examples. LINQ depth after entity-selected
stages remains paused. Work on unverified scopes and reproduced bugs.

## Recovery state

- Resumed on the old laptop at C:/Source/OASIS2 on 7 November after the
  recovered laptop failed again. Branch work/cosmic-cql-ton-public.
- Core task branch work/cosmic-cql-decentraland; preserve independent pins.
- Development9c6e93003, Coref0625521. Successful code job ac51f1c7.
- Teaching activation job593cd948-541c-4012-8d0d-654b58ba381d succeeded.
- Scoped release branch work/cosmic-teaching-release-candidate in both repos.
- Website C:/Source/Web4Site, branch work/cosmic-cql, published fae9c5a.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **119 scoped local, 74 live HTTP and 28 functional browser**.
The [full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) records each scope.

- MongoDB, SQLite and Neo4j provide the principal owned-storage demo paths.
- IPFS, SOLID and ActivityPub passed hosted multi-row queries and Mongo federation.
- Solana and Google completed browser native3/2, paired5/4, reuse and isolation.
- Latest HTTP additions Oracle Object Storage, Cloudinary and Proof of Humanity
  passed native3/2, Mongo5/4, reuse0new/2existing, privacy and replicas2/2.
  Their cards and local examples are published. Browser gates remain open.
- GUN and OrbitDB first generations are created and separately verified through
  their real SDKs on the existing /data volume. No creation writes were repeated.
  Their hosted CQL passed3/2, Mongo5/4, reuse, privacy and replicas2/2.
  Browser acceptance remains open.
- Image CI42339962522 passed for exact9c6e93003 with teaching runtimes enabled.
- Protocol CI42341070588 passed1493 tests/137 skips across six.NET suites
  plus WEB4-WEB10 source publish. Native source acceptance is separate.
- Waku native Store3/2/0, loopback WEB4/Mongo5/4, restart and snapshot passed.

## Exact next step

The GUN/OrbitDB cards are published at fae9c5a. Public HTML matches the
exact commit (241209bytes/SHA256 in provider reports);114 client tests passed.
The Graph Studio now passes12 provider controls, native3/2,32 admission cases
and local WEB4/Mongo5/4/privacy. Scoped parentce47a6f8/Coreacc6ed36 is pushed.
Image CI42346455177 succeeded with teaching_sidecars=true, exact source
ce47a6f89c9079ae8b3f61b874b1d73a48a4792a and digest
sha256:7f3eabf4b84182303452a58a9261633d12ba08f61555cd3f873b6bf464943237.
Four protected settings/four presets were prepared on the recovered laptop;
its machine-bound snapshots remain there. Old-laptop Railway access needs
private sign-in before observing the current baseline and rebuilding the
protected candidate from that baseline.
The live baseline was verified after private sign-in. Development advanced to
ce47a6f8 and exact Railway code deployment
ab79d580-2412-4a70-aadc-4c0a812ec44d is queued. Observe that job before
enrollment. Preserve active GUN/OrbitDB source generations.
The harness cleanup bug was reproduced and fixed locally: durable owner/write
journals now reject fresh execution before sessions/queries; all26 tests pass.
Do not rerun accepted HTTP schedules. Browser gates remain open.

## Source and rollout journals

LocalState/COSMIC/HostedTeachingSourceCurrent/{GUNOASIS,OrbitDBOASIS} holds
immutable source plans, public catalogues, create/verify intents, protected
outcomes and native acknowledgements. GUN verify completed normally.
Orbit's original verify failed on SSH connection before execution. Fresh
inspection proved no verification intent and unchanged source ack. Its one
explicit --resume-unstarted-verify completed; original failure remains retained.
Do not rerun creation or accepted verification. Readers activate only through
the current settings deployment. All13 settings/eight presets observed exactly.
Protected configuration prefix: teaching-public under HostedSourceRolloutCurrent.
Provision-HostedTeachingSource.py has five command tests; native helper has six.

## Remaining gates and constraints

- Finish open browser gates with original cohorts; HTTP/static proof is separate.
- Continue other providers' runtime, restricted-reader and hosting gates.
- Waku supervisor-writing command was rejected by automatic tool policy.
- PouchDB image qualified. All three original source files are present on this
  old laptop and their bundle/bootstrap hashes match native qualification.
  The 36 runtime controls passed without replaying source writes. Rebase the
  scoped candidate on the current Development baseline before its rollout.
- Hosted Azure Cosmos, Oracle DB and PostgreSQL remain deferred by David.
- Never replay accepted cohorts or replace original receipts to regenerate evidence.
- Broader ORM children/depth/history/deltas, migration, placement and HyperDrive
  operator, transport and scale acceptance remain incomplete.
- Disk target107GB remains unmet; preserve personal WSL Miniforge/ML models.
- Subscription20.6MB and finished Graph build outputs1.98GB retained after
  automatic cleanup-policy rejection; exact scopes are in the resource ledger.

## Commands and entry points

Use one provider build lease and /m:2 /nr:false /p:UseSharedCompilation=false.
Logs/results: TestResults. Durable identities/receipts: LocalState/COSMIC.
Stage named files. Development pushes authorized for live sandbox verification.
Fetch before integration. Validate pins with Scripts/validate_railway_dependency_manifest.py.
Observe only the exact deployment with Scripts/Sandbox/Observe-HostedDeployment.py.

[Parity](CQL_PARITY_CHECKLIST.md) · [CQL roadmap](CQL_COMPLETION_ROADMAP.md) ·
[ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) · [Status](IMPLEMENTATION_STATUS.md) ·
[Manual](CQL_MANUAL.md) · [Architecture](CQL_ARCHITECTURE.md) ·
[Resources](ENVIRONMENT_AND_CLEANUP_LEDGER.md) ·
[David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md) ·
[Previous status](Archive/CURRENT_STATUS_history_2026-11-07.md).
