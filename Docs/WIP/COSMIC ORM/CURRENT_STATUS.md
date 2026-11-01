# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demos. Deep LINQ parity remains paused.

## Verified scope

- 44 currently integrated query types: 3 canonical, 37 bounded source profiles, 4 local PostgreSQL/Azure/Oracle/AWS implementations.
- 39 providers have scoped hosted HTTP proof and deployed query tariffs. Browser proof: 5; 34 browser gates open. TON is now included in the scoped hosted count.
- MongoDB/SQLite/Neo4j parity checklist: 50/82 each; original investor demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed hosted cohort: TON, native 3/2, Mongo 5/4, repeat 0 new/2 existing, foreign-private 0/public 3, SQLite/Neo4j replicas 2/2. Completion ACK exists; never replay.
- TON native/manager CQL 3/2 and local HTTP native 3/2, paired 5/4, privacy 0/3, nine receipts pass. Two original saves acknowledged once; local database cleaned and Mongo stopped. Never replay its cleaned fixture.
- TON duration/admission/schema controls: 15; merged actual-preset controls: 16; latest changed OLAND/integration checks: 32; publisher/revision controls: 5. All scoped runs pass without skips.
- Website main 6cfa7dc: query timer plus preparation duration fixes published and exact HTML/JS bytes verified. All 90 client tests pass. Browser interaction is not inferred.

## Current release and settings

- Original TON code 24f548550 / Core 2ac915f: app job 4f6eba6c-5ee4-4afe-95a5-07849a92d7ec SUCCESS.
- Newer reviewed Development fb29aa936870dcfb3182102642e5997b2cedd90f preserves TON code and Core/ONODE pins, adds OLAND contracts; task merge 99b4f080 preserves it. App job 571cb5f0-85f4-40fa-9d99-568339833b3e is SUCCESS; reviewed code is deployed.
- Two live settings changed after preparation. Explicit reviewed revision ton-public-duration-oland preserves all current unrelated values; only three TON keys differ, and their values/public review bytes equal the tested candidate.
- Protected before/candidate snapshots: LocalState/COSMIC/HostedSourceRolloutCurrent/ton-public-duration-oland-*.dpapi. Public review: TestResults/TonPublicSourceCandidateCurrent/ton-duration-oland-prepared-review.json.
- All three ton-public-duration-oland settings applied once under exact fb29aa936 release/HEAD guard. Per-key ACK hashes and whole candidate matched. Earlier candidates are stale; never replay any apply.
- One journalled settings deployment e3ae497c-3f19-45b2-a577-b0fc40a7ae6b at fb29aa936 is SUCCESS. Original rollout intent/protected outcome retained. First hosted TON cohort completed; completion ACK retained. TON card published/byte-verified at website6cfa7dc; guide34ae56281 and C#fb29aa936 pinned.

## Exact next steps

1. TON card is published at website6cfa7dc, with pinned updated guide34ae56281 and tested C#fb29aa936. Public HTML and both scripts match exact git blobs; browser interaction remains open.
2. PostgreSQL restoration now passes16controls, native17cases and local paired SDK/HTTP2/4/4/2 plus identity5/4. Original JSON/xmin and15source metadata files unchanged; exact temporary Mongo cleanup/owned service stops verified. Hosted inventory found no PostgreSQL service in observed OASIS project; existing dev connection/deferral question recorded in David list and pending; no admission or paid provisioning. No green-baseline rerun. See POSTGRESQL_CQL_MVP.md. AWS retained native and local federation now pass; see AWS_CQL_MVP.md.
3. TRON remains unqualified after native429/diagnostic403; no blind acceptance rerun. Azure/Oracle hosting remains deferred; do not re-ask. Deep LINQ parity remains paused.
4. Original HostedQueryAcceptance-TONOASIS has completion ACK: native3/2, paired5/4, reuse0/2, privacy0/3, replicas2/2, two saves at version1 and no pending preparation intents. Never rerun this completed cohort or resave its copies.

## Limits and deferred work

- Hosted Azure and Oracle explicitly deferred by David; free only, no signup/spending re-asks. Local/emulator proof is not hosted proof.
- Browser transport/control remains closed; no OS automation resumed. Solana and other browser gates remain open; David already opened the page.
- TRON remains unqualified: native 429; explicit PublicNode diagnostic 403 before any validated header. No guessed delay, retry, alternate runtime path or provider count increase.
- Public-source MVPs do not certify source writes, broad metadata scans, independent consensus, distributed snapshots, full ORM parity or universal uptime.
- Full tracking/concurrency/children/history/deltas/migration/placement audit and broader HyperDrive qualification remain incomplete. V2 code only; legacy gaps documentation only.

## Recovery and resource rules

- Work only C:/Source/OASIS-cosmic on work/cosmic-cql-ton-public; Core work/cosmic-cql-google-public. Frontend C:/Source/Web4Site on work/cosmic-cql. Unqualified TRON code remains excluded from this release.
- Raw outputs TestResults/; durable source/config/identities/ACKs LocalState/COSMIC/. Completed cohorts never replay. Unknown root logs preserved.
- Existing pinned runtimes/environments only; resource ownership/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. No new accounts or paid hosting; the existing Dynamo CQL runtime binding was repaired with the original pinned runtime, preserving its data.
- Builds under Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 with /m:2 /nr:false /p:UseSharedCompilation=false. Committed dependency pins validate; fetch before integrations and verify submodule pushes. No production/master promotion.

[TON evidence and procedure](TON_PUBLIC_SOURCE_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity checklist](CQL_PARITY_CHECKLIST.md) | [Deferred work](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).

Dynamo checkpoint:13controls plus retained native3/2 and Mongo SDK/HTTP5/4 pass. Original records unchanged, temporary Mongo fixture deleted, container exited and18002/27029/5096 absent. AWSOASIS local qualification adds one type; DynamoDBOASIS alias is not independently native-qualified. General Dynamo content predicates and hosted admission remain open; no replay of green cohorts. See AWS_CQL_MVP.md.

Azure Blob milestone: real public Azure properties3/2, documented manager/LINQ3/2 and local HTTP/Mongo5/4/privacy0/3 pass;24source/43APIcontrols, zero skips. Existing free public dataset, no account/body download. Native count44 includes this separate public profile; hosted39 unchanged. Console binary also passes3/2. Azure protected candidate/public review now prepared;6publisher controls pass, exactly3keys/free4presets/DNA unchanged. Next integrate verified code, wait for its exact successful deployment, apply the reviewed settings once and verify the first hosted cohort; browser remains open. See AZURE_PUBLIC_BLOB_STATUS.md.
