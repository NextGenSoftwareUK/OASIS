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
- Development7672cd5ea; catalogue-limit correction deployment3cdeaf40 is queued.
  Recovery deploymenta6688a2e serves the preceding working257404f8 snapshot.
  Waku's17th public catalogue exposed an obsolete16-provider startup limit.
  The correction passed all5 regression cases and exact teaching image42384082328.
  Original Waku archive/configuration ACKs remain; no settings or source replay.
- PouchDB settings/live acceptance job2045b41e succeeded.
- Teaching activation job593cd948-541c-4012-8d0d-654b58ba381d succeeded.
- Scoped release branch work/cosmic-teaching-release-candidate in both repos.
- Website C:/Source/Web4Site, branch work/cosmic-cql, published26e2b4a.
- Dedicated Railway key cosmic-cql-recovery-laptop-20261107 was created on
  this laptop for mounted-source inspection. Its eventual revocation is in
  the old-laptop cleanup ledger. Private key remains outside Git.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **119 scoped local, 76 live HTTP and 28 functional browser**.
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
- Graph Studio and PouchDB passed hosted native3/2, Mongo5/4, snapshot reuse,
  independent privacy and exact SQLite/Neo4j replicas2/2. Their website cards
  are published at26e2b4a and exact public HTML is verified. Browser gates open.
- Image CI42339962522 passed for exact9c6e93003 with teaching runtimes enabled.
- Protocol CI42341070588 passed1493 tests/137 skips across six.NET suites
  plus WEB4-WEB10 source publish. Native source acceptance is separate.
- Waku native Store3/2/0, loopback WEB4/Mongo5/4, restart and snapshot passed.

## Exact next step

Waku's original12KB archive passed real native restore/restart42366567554,
with all three messages, two native starts and unchanged original input/ack.
Artifact13571766234 retains the proof, original source and logs for90days.
Fourteen focused runtime controls and66 combined controlled cases pass.
Seven publisher controls,26 response-evidence and39 preset controls pass.

Scoped Waku parent257404f8f86df9fb34f7dc27c4499d8c1840fe67 with
Core2fdd3e40183e8a5ed81bccee1f30660c91135eb1 is published on the reused
work/cosmic-teaching-release-candidate refs. It preserves all accepted sources.
Native ABI/image42367817374 passed fore0ea3d57. A controlled regression then
caught browser numeric rounding of nanosecond metadata. The projection now
emits exact decimal text. Red1failed, corrected4 C# controls pass, zero skips.
Timestamp-corrected image42368952565 passed, digest
sha256:9671e1f694c02f1e6f26b84e7d705ee0585e41c49393be46dd0de822bf585226.
Native regression42368859021 first failed during checkout because the
already committed task ONODE pin9f5ad9df was unpublished. Its one ONET
source commit was advertised on the same agreed task branch, preserving
Development308a7860. No ONET functional acceptance is claimed. One exact
native CI recovery passed after publication, five provider cases and one
WEB4/Mongo case. The owned runner Mongo container/volume was removed.

Six protected Waku settings were prepared with205 preserved settings, prefix
waku-public under HostedSourceRolloutCurrent. The original archive, receipt
and actual cloud qualification are under WakuQualifiedSourceCurrent.
The exact image/native gates passed. Fresh live baseline and source checks
passed. Development advanced normally to257404f8. Code deployment
3fcf6d34-e591-4e24-a377-599a33022a4c is queued. Observe that exact job,
then verify its active instance/image and writable mount. Apply the existing
six-key guarded publisher once, request one settings deployment, and verify
its original source receipt before the first hosted readiness/acceptance.
Waku settings and hosted cohort remain uncreated.

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
- Waku native restore/restart is verified. Exact image qualification and
  guarded hosted activation/acceptance remain open.
- PouchDB hosted HTTP acceptance and website publication are complete.
  Retain its native first generation and original acceptance cohort.
  Its hosted browser gate remains open.
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
