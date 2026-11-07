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
- Development3cc1ab108/Core9fac20cd; DuckDB code deployment858ac680 isBUILDING.
  Accepted Waku runtime3cdeaf40 at7672cd5ea/Core2fdd3e40 remains the prior good release.
  Waku hosted HTTP acceptance passed on this exact release. Its17th catalogue
  exposed the obsolete16-provider startup limit, now fixed and covered by5 tests.
  Original Waku archive/configuration ACKs remain; no settings or source replay.
- PouchDB settings/live acceptance job2045b41e succeeded.
- Teaching activation job593cd948-541c-4012-8d0d-654b58ba381d succeeded.
- Scoped release branch work/cosmic-teaching-release-candidate in both repos.
- Website C:/Source/Web4Site, branch work/cosmic-cql, publishede7dce1b.
- Dedicated Railway key cosmic-cql-recovery-laptop-20261107 was created on
  this laptop for mounted-source inspection. Its eventual revocation is in
  the old-laptop cleanup ledger. Private key remains outside Git.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **119 scoped local, 77 live HTTP and 28 functional browser**.
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

Waku card63 is published atf854fc2 with its real C# example and scoped
evidence. Public HTML matches the exact Git blob. The live HTTP gate is complete: native3/2, Mongo5/4, first2 saves, repeat0new/2existing,
foreign-private0/public3 and SQLite/Neo4j replicas2/2. Source receipt and
complete protected settings matched before and after acceptance. Exact
nanosecond strings survived the HTTP serializer. Original cohort receipt:
LocalState/COSMIC/HostedQueryAcceptance-WakuOASIS/cql-http-acceptance.acknowledged.json.
Never rerun that accepted cohort. The original502 readiness operation stays
retained; discovery explicitly reused the acknowledged PouchDB visitor.
Browser verification remains open: the updated Windows helper stopped when
it could not determine the browser URL reliably; no browser input occurred.

Continue DuckDB hosted preparation. Reuse its existing
read-only provider/registration and qualified original database. Linux run
42385940278 passed native3/2/0 across two read-only activations, unchanged
source/no WAL. Artifact13579442161 retains that source/seed/TRX for90days.
Owned source-placement runtime and82 shared controls pass. Six guarded
settings are privately prepared, none applied. Publisher6, response28 and
preset39 controls pass; publisher coverage is now9/9 including live-file,
ACK and deployment-drift checks. Scoped parent3cc1ab108/Core9fac20cd is
published on the reused candidate refs. Exact admission CI42388149011 passed
6/6,0skips. Image42388244083 passed native-library/ABI checks, digest81d462e1.
All seven dependency retention refs and the full live baseline were rechecked.
Development advanced normally to3cc1ab108; code deployment858ac680-3f46-4b5e-a4f8-491573304bfc
isBUILDING. Observe that exact job, verify its active image/instance/mount and
confirm the new source destination is unoccupied before staging settings.
Then apply the original six-key publisher once, request one settings deployment,
verify the mounted source hash/receipt/no-WAL and perform first readiness/HTTP
acceptance with an acknowledged existing discovery visitor. Settings/source
remain unapplied. Never resubmit a deployment or a save solely after a timeout. Do not
recreate or replay the accepted Windows fixture. Shared website navigation
now opens provider-card/panel hash links,119client controls pass, and public
HTML plus new navigation script match committed e7dce1b bytes.
[Waku evidence](WAKU_STORE_CQL_MVP.md) · [DuckDB evidence](DUCKDB_CQL_MVP.md).

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
