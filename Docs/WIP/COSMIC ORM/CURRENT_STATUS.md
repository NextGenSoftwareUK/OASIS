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

Observe Railway code deployment **ac908014-1a41-40ef-aec9-3d5df89b5ff8**. It is
QUEUED for verified SubQuery candidate `9fd968a7b40d2decb389f7a2a5c7f75874b11153`
and Core `06df0885c398e0b16749565852d12a9d499288d2`. One normal Development push
is acknowledged. **Do not rerun the integration operator or request another
code deployment.** The accepted Dapr release still serves while this is queued.

Image run42441146593/job128610449048 succeeded. Qualified GHCR digest:
`sha256:c9e3748ba13de0e0134c7e0046e1b30840d27ab6cc56ff056fdf4f3b187530b5`.
All 51 scoped checks pass with zero skips: 16 dictionary controls + 1 real native
CQL case, 8 WEB4 controls + 1 actual HTTP/Mongo case, and 25 admission regressions.
Native3/2, Mongo5/4, foreign-private0/public3, nine usage receipts and exact owned
fixture database absence are verified. Task SubQuery8+1 and affected Waku5+1 also
pass. [Exact source, hashes and scope](SUBQUERY_PUBLIC_SOURCE_STATUS.md).

The image workflow used its default mutable sandbox tag despite the requested
empty extra tag. Actual tags are recorded; a fresh protected observation proved
that it did not change the live service. Future image requests must explicitly
set the commit-specific teaching tag rather than an empty string. The qualified
GHCR digest is separate from the eventual actual Railway image digest.

Read-only preflight verified all seven advertised pins, the accepted deployment
and all 251 settings. A second guarded review passed before the acknowledged
Development push. Dapr remains enabled; SubQuery is not enrolled. Private state
and guarded operators are under `LocalState/COSMIC/RecoveryLaptopResume20261107`:

1. Observe ac908014 until SUCCESS. The read-only inspector may safely report
   QUEUED without source access. Then run `inspect-subquery-code-package.py`
   with `--deployment ac908014-1a41-40ef-aec9-3d5df89b5ff8 --phase code`.
   It binds the actual instance/package/settings, accepted Dapr acknowledgement,
   runtime versions and new SubQuery assembly/registration. Preserve its receipt.
2. Review and explicitly publish the four settings with
   `Publish-HostedFixedPublicCatalogue.py`, prefix `subquery-dictionary-public`.
   The retained baseline has 251 settings; the prepared candidate has 253.
   Recheck the live baseline and exact successful deployment before publication.
3. After all four setting ACKs, invoke `activate-subquery-settings.py` once.
   The operator refuses an existing intent and verifies the original source owner.
   Observe that new activation handle, then inspect it with `--phase settings`.
4. Reuse the acknowledged PouchDB readiness visitor only after exact release
   verification. Run SubQuery's first hosted native/federation/preparation/reuse/
   privacy/SQLite+Neo4j cohort once; seal its original two saves and reports.
   Never replay accepted cohorts or create/replace an existing native source.
5. Publish website card78 only after hosted acceptance, updating its note and
   evidence link. Website task73bacd67970877cfed7404595498ca12ad233c0d has the draft
   and all119client controls pass. Main/public source remains26b3172. Browser
   interaction stays separate; do not bypass the existing URL-policy failure.

The original SubQuery plan keeps ownerf0d6a43f-cdf4-4112-8397-b406571401a6 and
its three mapped IDs. It creates no native data. Its dictionary network pin
agrees with an independent pinned Firehose registry and earlier accepted Arbitrum
binding; fresh independent RPC access remains denied. Full logs, consensus
verification and general ORM/storage operations are not claimed.

Coverage is **120 scoped local / 92 live HTTP / 28 functional browser**.
The accepted Dapr cohort remains sealed at activationf0b2771c on its fresh native
generation. Its retired failed predecessor retains its unknown save outcome and
original files. [Dapr history](DAPR_CQL_MVP.md) and
[prior status](https://github.com/NextGenSoftwareUK/OASIS/blob/f1295f01fcc38a11ad4a09baa11003d802928b23/Docs/WIP/COSMIC%20ORM/CURRENT_STATUS.md)
preserve recovery detail. No SubQuery settings mutation or source operation has
happened during this code rollout.

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
