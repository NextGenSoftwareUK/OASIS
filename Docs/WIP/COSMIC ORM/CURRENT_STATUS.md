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
- Development source82428c7a6/Core2a09c9be code592df8cd isSUCCESS.
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
  is now on Development. Code deployment75e5970b isSUCCESS on
  ca788724-9609-4630-8093-e61d4cc80c4d; package/settings/source fingerprints verified.
  Fresh Dapr generation acknowledged; activationf0b2771c isSUCCESS on
  0fb82716-5388-46ee-a6e7-a6536fd36bf5. All251settings/package and mounted
  source values/ETags verified. Dapr HTTP cohort is sealed; browser remains open.
  First planned source save is unacknowledged: Railway denies native libaio
  io_getevents. Original source/intent retained; do not reseed or recreate.
- Website C:/Source/Web4Site, branch work/cosmic-cql, publishedad7e9dd (six new cards71-76; earlier accepted cards retained).
- Dedicated Railway key cosmic-cql-recovery-laptop-20261107 was created on
  this laptop for mounted-source inspection. Its eventual revocation is in
  the old-laptop cleanup ledger. Private key remains outside Git.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **119 scoped local, 92 live HTTP and 28 functional browser**.
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

Development branch is74a7b60c0/Coreec7d440; activationf0b2771c isSUCCESS
on instance0fb82716-5388-46ee-a6e7-a6536fd36bf5,
actual image sha256:d67979ce202c656fda6552bc2dab2eb5cce04d4ea19b0a9cd50e4c172f2c4dbd.
Dapr scoped HTTP acceptance is sealed. The first original source save timed out without
acknowledgement because Railway denies the default libaio completion syscall.
Its original source/plan/intent are preserved: never recreate or resend.

Explicit Native/Uring full native qualification passes42425594492. The read-only
inspector/recovered-outcome metadata fixture passes42426250057:1save,2recoveries,
0inspection mutations, unchanged AOF and no fabricated save-response ACK. The
corrected scoped candidatef00eb9d48 passes exact native qualification42427310034;
its exact image42427405291 passed. Installed package/198critical settings and
full245setting baseline are verified unchanged. The one original-source inspection
failed: host Native/Uring initialization returns errno38 Function not implemented
on actual AOF reads. Original2048byte AOF hash unchanged; owned processes0.
Do not replay inspect or save. Its failed private journals/logs are preserved.

One separate RandomAccess diagnostic HSET reached durable storage without engine
errors. Its inventory stopped on Garnet's log-commits directory; a separately
journalled recovery-only continuation read the same value, changed no AOF bytes
and stopped its process. Original live source was not accessed by that diagnostic.
Task-only Linux contract now explicitly selects supported RandomAccess; no runtime
selection/fallback. Explicit corrected-device inspection transition is implemented
(25Node/11Python controls pass), preserving old diagnostic files and exact AOF.
Scoped candidate74a7b60c0 (Core unchanged) is published only to the candidate branch.
Prior native42431408413 passed normal WEB4 recoveries but failed the new fixture:
its GET fault injection missed fetch's implicit GET. Fixed the fixture boundary;
exact corrected native42431903229/job128582436232 isSUCCESS. Its artifact was
verified in memory: two real WEB4 TRX1/1/0skips and complete corrected-device
recovery proof. Exact image42432504316/job128584628139 isSUCCESS, digest
sha256:b704b8fa5a83f3db779eb5110c9cdd44faffed328fd02323d7b9f2240a7777d5. Superseded
owned image42431411318 is terminalCANCELLED with no active steps.
Fresh rollout review confirms seven advertised pins, matching shared branch bases
and unchanged245settings; one scoped Development push is acknowledged.
Installed package,198critical settings/full245baseline and original fingerprints
pass. Corrected-device inspection ran once after an unstarted mount preflight
stopped; it still failed record/ETag verification. Original2048byte AOF hash unchanged,
prior diagnostics archived intact,0owned processes and no storage engine errors.
No save replay or recovered metadata ACK. Original save outcome remains unknown.

Retirement operator177ad6a73 passes Linux42435459405/job128593209843:
6Node+3Python/0skips. Remote retirement and checked local archive are acknowledged;
all original files preserved under operationb75969e7-cd3d-40e1-be62-22ea3835eafa,
businessWrites0/originalSaveOutcomeunknown. No original save ACK fabricated.
Fresh source owner141c7116-ab36-432d-a570-d772403768e0 has a distinct plan/IDs,
one successful creation ACK and verified durable reader recovery. Generation lineage
links the retained original. Eight exact settings are staged/acknowledged; one
activationf0b2771c-49bf-466f-91d3-7dbf060dfaf5 isSUCCESS for the same74a7b60c0 code.
Exact package/251settings and fresh mounted values/ETags pass before and after HTTP.
Original PouchDB rehearsal visitor reused, one Dapr owner/foreign cohort sealed:
native3/2, Mongo5/4, first2private saves/repeat0new2existing, privacy0/public3 and
SQLite/Neo4j2/2. Next publish its accepted demo card and continue independent red
provider/browser gates. Never replay this accepted cohort or either source creation.

Coverage remains119local/92live HTTP/28functional browser. The six public readers
remain accepted on their original sealed cohorts: native3/2, Mongo5/4, repeat0new,
privacy0/public3 and exact SQLite/Neo4j replicas2/2. Never replay accepted cohorts.
Websitead7e9dd publishes cards71-76; all119client controls and exact public HTML
passed. Browser gates remain open. [Batch evidence](SIX_PUBLIC_READERS_HOSTED_BATCH.md).

Browser URL-policy helper failure, QuestDB403, Chroma/D1 access, Nostr recovery
and David's deferred hosted databases remain unchanged. Full provider/ORM parity
and broader protocol execution are separate from these bounded CQL proofs.

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

[Parity](CQL_PARITY_CHECKLIST.md) Â· [CQL roadmap](CQL_COMPLETION_ROADMAP.md) Â·
[ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) Â· [Status](IMPLEMENTATION_STATUS.md) Â·
[Manual](CQL_MANUAL.md) Â· [Architecture](CQL_ARCHITECTURE.md) Â·
[Resources](ENVIRONMENT_AND_CLEANUP_LEDGER.md) Â·
[David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md) Â·
[Previous status](Archive/CURRENT_STATUS_history_2026-11-07.md).
