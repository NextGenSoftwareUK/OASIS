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
- Development source82428c7a6/Core2a09c9be is queued in code deployment592df8cd.
  Active service remains3cc1ab108/Core9fac20cd, DuckDB deployment32b26f74 SUCCESS.
  DuckDB hosted HTTP acceptance passed with the original qualified database,
  unchanged mounted file hash and no WAL before/after the full flow.
- Waku accepted on7672cd5ea/3cdeaf40; PouchDB accepted on2045b41e.
  Both remain included in the current scoped release. Original sources and
  acceptance journals are preserved; the catalogue-cap fix remains covered.
- PouchDB settings/live acceptance job2045b41e succeeded.
- Teaching activation job593cd948-541c-4012-8d0d-654b58ba381d succeeded.
- Scoped release branch work/cosmic-teaching-release-candidate in both repos.
- Website C:/Source/Web4Site, branch work/cosmic-cql, published698e102.
- Dedicated Railway key cosmic-cql-recovery-laptop-20261107 was created on
  this laptop for mounted-source inspection. Its eventual revocation is in
  the old-laptop cleanup ledger. Private key remains outside Git.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **119 scoped local, 78 live HTTP and 28 functional browser**.
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

DuckDB hosted HTTP acceptance is complete on32b26f74-01b7-4ce2-a84e-08959ff46468,
instance36653fc7-c5f3-4dbc-86c6-0af48bc96f5a, exact3cc1ab108/Core9fac20cd.
Native3/2, Mongo5/4, first2 saves/repeat0new2existing, foreign-private0/public3
and exactSQLite/Neo4j replicas2/2 passed. Full protected settings, all six
original write ACKs and mounted database/receipt hashes matched before and
after; no WAL exists. Original hosted cohort is sealed under
HostedQueryAcceptance-DuckDBOASIS/cql-http-acceptance.acknowledged.json.
Never replay it or recreate the accepted Windows fixture.

DuckDB card22 is published at698e102 with the hosted path, preserved local5116
launcher and real C# example. Public HTML matches the exact committed blob;
all119 client controls pass.

NATS JetStream is the current hosted scope. Real Linux TLS/native3/2,
explicit write/foreign permission denials and untrusted CA rejection passed
in42396383665. Restored-source restart42397239148 passed with unchanged
values/revisions/TLS identity and no seed/save. Task API42397587804 passed14/14.
Parent candidate82428c7a6441e6088cfdd6960f899641dae54dd1 and
Core2a09c9be620b3f3c2b1bae05bf62758bc5b13983 are published on the reused
candidate branches. All seven exact dependency pins are advertised remotely;
manifest/gitlinks pass. Exact candidate API42398316205 passed14/14 with
zero skips; image42398318179 passed with teaching runtimes enabled.
Code deployment592df8cd-a820-4dbe-b6b0-6ebfb5f0b04e is queued after one
verified normal Development push. Track that exact job before settings staging. Eight settings are prepared only in protected local snapshots;
four presets/source bindings agree with original IDs. Next inspect those same
jobs and preserve terminal evidence before scoped Development rollout and
one-shot settings activation. Mounted/live HTTP gates remain open.
See [NATS evidence and remaining gates](NATS_JETSTREAM_CQL_MVP.md).

Do not bypass the preserved QuestDB public403. Chroma enrollment remains open.
Keep browser acceptance separate: its URL-policy helper failure persists.
Preserve accepted Waku/DuckDB/PouchDB cohorts and all deferred owner decisions.

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
