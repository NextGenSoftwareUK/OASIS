# COSMIC ORM / CQL current status

Updated 8 November 2026. Goal active and incomplete.

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
- Website C:/Source/Web4Site, branch work/cosmic-cql, publishedfc2714b5 (Self Protocol card79; earlier accepted cards retained).
- Dedicated Railway key cosmic-cql-recovery-laptop-20261107 was created on
  this laptop for mounted-source inspection. Its eventual revocation is in
  the old-laptop cleanup ledger. Private key remains outside Git.
- Unknown root logs and Docs/WIP/Cleanup/CLEANUP_LEDGER.md are outside this task.

## Verified

Coverage is **122 scoped local, 94 live HTTP and 28 functional browser**.
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

Self Protocol scoped candidate **916c62210a1eb51fa23353bccfca3024750f452b**,
Core **25a296e986f35fe118f30e7489c655151d57e97e**, is pushed to the existing
work/cosmic-teaching-release-candidate branches. Development is916c62210.
Only16reviewed paths differ; unrelated HostedBasicSqlQueryReader and dirty
checkout changes are excluded. Seven pins/manifest verified; only this scoped batch was deployed.

Task sourcef5c4826c5 passed26controls,1296shared checks and actual native CQL3/2
on42446401193; WEB4/Mongo42446401228 passed8boundary+1real HTTP case with
native3/2,paired5/4,foreign private0/public3,9receipts and databaseAbsent:true.
Both artifacts inspected/hash verified in memory. These are not hosted proofs.

All exact candidate gates now pass/no skips: registry42446975238 (26controls,
1296shared,1native), WEB442446977425 (8controls+1HTTP), and25admission cases
42446952053. Original artifact IDs/hashes and TRX counters verified in memory.
Redundant manual admission42446979977 is terminalCANCELLED, original run retained.

Image42447469381/job128629322069 isSUCCESS. Exact source/tag/teaching mode
verified, GHCR digestsha256:bb86863c7bf41311cf3e49b77203f87d41d39c66de2cb14f65c674bf27c66067.
No image download or mutable sandbox tag. Fresh branch/seven-pin/full253setting/
baseline checks passed; one scoped Development push916c62210 acknowledged.

Self code131667e6 and activation1fe29f38 bothSUCCESS. Exact settings package
verified255settings/208critical,6helpers,11SDK/provider assemblies and native/
Dapr/SubQuery fingerprints. Active instanceff2628fd-b511-4e26-9bc6-cb7b86a34b43;
actual Railway imagesha256:e2f01ef06f3471231df01ef5d09dd1af19551a46311ef7f21d2dae25565c0dd8.
One settings apply/activation only; native source writes0.

Self original owner2826f75b/foreign16769240 passed live native3/2,paired5/4,
first2saves,repeat0new/2existing,private0/public3 and exact SQLite/Neo4j2/2.
Durable acceptance sealed from original reports/twoACKs/empty pending registry;
no query/session/save replay. Reused PouchDB readiness actor. Browser still open.
Card79 is published on website task/mainfc2714b50fb032c8c8e0c3e7dbb9006ce039b883
with immutable parent acceptance892a215ec. Public270083byte HTML hash
4f9bade880ef8c24bfbdb8007a2fb9dbf9f3baa437bb5183ebf18547837aec80 matches exact Git.
Continue Holonym's scoped candidate/rollout from this accepted916c62210/Core25a296e
baseline. Preserve the current255setting snapshot and existing candidate branches.
Do not repeat any accepted Self/SubQuery/Dapr cohort or source operation.

Offline owner/source plan and protected253baseline/255candidate are fixed;
no native source writes. Website card79 task/main/public isfc2714b5; static publication verified,
functional browser gate remains separate. Browser URL-policy gate stays open.
[Self Protocol proof and gates](SELF_PROTOCOL_PUBLIC_SOURCE_STATUS.md).
Activation guard activate-self-protocol-settings.py is prepared/syntax checked only.
Holonym current Sign source now passes25controls+1real CQL3/2 on source25d8f3a03,
cloud42449086090/artifact13605126354 verified in memory; raw ABI/doc differences
preserved. WEB4 read profile/Coreabaedaf and8+1 actual HTTP/Mongo qualification
pass on91b08d8d6/cloud42449470293;9receipts/privacy/DBabsence verified, not deployed.
39Python catalogue controls+5publisher guards pass; no Holonym settings/source
owner created yet. [Next source](HOLONYM_PUBLIC_SOURCE_STATUS.md).

SubQuery activationeb09ea25 is accepted/sealed:253settings/package verified,
native3/2,paired5/4,reuse0new/2existing,privacy0/public3,replicas2/2. Original
owner51c28fc4 and initial unretained observer failure preserved. Card78 is live
at104a0064, public268592byte HTML hash7908eabb8c70743d2e0cc1023484da2c7bb1e480d68dce26857ca6af4e383001
matches exact Git. Do not replay its accepted cohort, settings or activation.
[SubQuery proof](SUBQUERY_PUBLIC_SOURCE_STATUS.md).

Coverage remains **122 scoped local / 94 live HTTP / 28 functional browser**.
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
- Latest observed free space is49,833,431,040bytes (~46.4GiB); no task-owned bulk
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
