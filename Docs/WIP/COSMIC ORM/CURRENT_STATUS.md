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
- Earlier NATS checkpoint: source82428c7a6/Core2a09c9be code592df8cd isSUCCESS.
  Eight NATS settings staged once; activation8c51a9da isSUCCESS and HTTP accepted.
  DuckDB accepted deployment32b26f74/source evidence remains preserved.
  DuckDB hosted HTTP acceptance passed with the original qualified database,
  unchanged mounted file hash and no WAL before/after the full flow.
- Waku accepted on7672cd5ea/3cdeaf40; PouchDB accepted on2045b41e.
  Both remain included in the current scoped release. Original sources and
  acceptance journals are preserved; the catalogue-cap fix remains covered.
- PouchDB settings/live acceptance job2045b41e succeeded.
- Teaching activation job593cd948-541c-4012-8d0d-654b58ba381d succeeded.
- Scoped release branch work/cosmic-teaching-release-candidate in both repos.
  Dapr correction74a7b60c0/Coreec7d440 passed exact native/image gates and
  preceded the new SubQuery batch. Code deployment75e5970b isSUCCESS on
  ca788724-9609-4630-8093-e61d4cc80c4d; package/settings/source fingerprints verified.
  Fresh Dapr generation acknowledged; activationf0b2771c isSUCCESS on
  0fb82716-5388-46ee-a6e7-a6536fd36bf5. All251settings/package and mounted
  source values/ETags verified. Dapr HTTP cohort is sealed; browser remains open.
  The original failed generation is separately retired with its unknown save
  outcome and original files preserved. Never replay that generation's save.
- Website C:/Source/Web4Site, branch work/cosmic-cql, published26b3172 (Dapr card77; earlier accepted cards retained).
- Dedicated Railway key cosmic-cql-recovery-laptop-20261107 was created on
  this laptop for mounted-source inspection. Its eventual revocation is in
  the old-laptop cleanup ledger. Private key remains outside Git.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **120 scoped local, 92 live HTTP and 28 functional browser**.
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

Observe SubQuery settings activation **eb09ea25-e7c0-4650-abc3-6d1419ee3794**.
It is BUILDING for verified source `9fd968a7b40d2decb389f7a2a5c7f75874b11153`,
Core `06df0885c398e0b16749565852d12a9d499288d2`. **Do not repeat the Development
push, four setting updates or activation request.**

Code deployment ac908014 succeeded on instance0deef829-024a-4248-958b-9c6162fc4e79.
Actual Railway image is
`sha256:88a422c48ff2014c6f7f1b8b08a50c2732e071836426a387e79a7d6f631fae29`.
Its package inspection verified all251settings/204critical values, six helper
hashes, five SDK/provider assemblies, runtime/native-library proof, the accepted
Dapr acknowledgement/owner141c7116 and the new SubQuery reader/registration.
SubQuery was correctly disabled during that code inspection.

The four settings passed preflight, were applied once with deploys skipped,
and have exact per-key ACKs. They preserve the other sources and prepare253total
settings. One guarded activation request is acknowledged; its original protected
response and exact handle are retained. No hosted SubQuery cohort or native
source creation has run. Private state/operators are under
`LocalState/COSMIC/RecoveryLaptopResume20261107`:

1. Observe eb09ea25 until SUCCESS, then invoke `inspect-subquery-code-package.py`
   with `--deployment eb09ea25-e7c0-4650-abc3-6d1419ee3794 --phase settings`.
   It verifies the actual instance/package and all253settings, enabled source
   ownerf0d6a43f-cdf4-4112-8397-b406571401a6, and preserved Dapr source/runtime.
   Do not reuse the GHCR digest as the actual Railway digest.
2. Reuse the acknowledged PouchDB readiness visitor after exact release
   verification. Run SubQuery's first native/federation/preparation/reuse/privacy/
   SQLite+Neo4j cohort once. Seal its original two saves and reports. Preserve
   uncertain operations; never replay accepted cohorts or replace native data.
3. After hosted acceptance, publish website card78, updating its note/evidence
   link. Website task73bacd67970877cfed7404595498ca12ad233c0d has the draft and
   all119client controls pass. Main/public source remains26b3172. Browser proof
   is separate; do not bypass the existing URL-policy failure.

All51scoped candidate checks pass/zero skips:17dictionary,9WEB4/HTTP and25admission
regressions. Task SubQuery8+1 and affected Waku5+1 also pass. Image42441146593
succeeded with GHCR digestc9e3748ba13de0e0134c7e0046e1b30840d27ab6cc56ff056fdf4f3b187530b5.
The workflow applied its default sandbox tag despite an empty requested extra
tag; actual tags and unchanged-live-service observation are recorded. Future
image requests must pass the explicit commit-specific teaching tag.
[Full proof and scope](SUBQUERY_PUBLIC_SOURCE_STATUS.md).

Coverage remains **120 scoped local / 92 live HTTP / 28 functional browser**.
The accepted Dapr cohort and retired failed generation remain preserved; no
source creation/save was replayed. [Dapr history](DAPR_CQL_MVP.md). While the
code deployment waited for a build slot, a first anonymous Self Protocol RPC
read confirmed reported chain42220/access only; existing verifier unchanged,
no CQL count increase. [Next provider review](SELF_PROTOCOL_PUBLIC_SOURCE_STATUS.md).

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
- Waku hosted HTTP acceptance and public card63 are complete; browser gate remains open.
- PouchDB hosted HTTP acceptance and website publication are complete.
  Retain its native first generation and original acceptance cohort.
  Its hosted browser gate remains open.
- Hosted Azure Cosmos, Oracle DB and PostgreSQL remain deferred by David.
- Never replay accepted cohorts or replace original receipts to regenerate evidence.
- Broader ORM children/depth/history/deltas, migration, placement and HyperDrive
  operator, transport and scale acceptance remain incomplete.
- Disk target107GB remains unmet; preserve personal WSL Miniforge/ML models.
- Latest observed free space is49,979,895,808bytes (~46.5GiB); no task-owned bulk
  deletion, local build/runtime/image download or rejected cleanup retry.
- Subscription20.6MB and finished Graph build outputs1.98GB retained after
  automatic cleanup-policy rejection; exact scopes are in the resource ledger.

## Commands and entry points

Use one provider build lease and /m:2 /nr:false /p:UseSharedCompilation=false.
Logs/results: TestResults. Durable identities/receipts: LocalState/COSMIC.
Stage named files. Development pushes authorized for live sandbox verification.
Fetch before integration. Validate pins with Scripts/validate_railway_dependency_manifest.py.
Observe only the exact deployment with Scripts/Sandbox/Observe-HostedDeployment.py.

[Parity](CQL_PARITY_CHECKLIST.md) Â· [CQL roadmap](CQL_COMPLETION_ROADMAP.md) Â·
[ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) Â· [Status](IMPLEMENTATION_STATUS.md) Â·
[Manual](CQL_MANUAL.md) Â· [Architecture](CQL_ARCHITECTURE.md) Â·
[Resources](ENVIRONMENT_AND_CLEANUP_LEDGER.md) Â·
[David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md) Â·
[Previous status](Archive/CURRENT_STATUS_history_2026-11-07.md).
