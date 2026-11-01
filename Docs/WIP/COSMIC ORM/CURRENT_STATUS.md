# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demos. Deep LINQ parity remains paused.

## Verified scope

- 41 implemented query types: 3 canonical, 36 bounded source profiles, 2 local Azure/Oracle implementations.
- 38 providers have scoped hosted HTTP proof and deployed query tariffs. Browser proof: 5; 33 browser gates open. TON is not yet included in hosted counts.
- MongoDB/SQLite/Neo4j parity checklist: 50/82 each; original investor demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed hosted cohort: Arweave, native 3/2, Mongo 5/4, repeat 0 new/2 existing, foreign-private 0/public 3, SQLite/Neo4j replicas 2/2. Completion ACK exists; never replay.
- TON native/manager CQL 3/2 and local HTTP native 3/2, paired 5/4, privacy 0/3, nine receipts pass. Two original saves acknowledged once; local database cleaned and Mongo stopped. Never replay its cleaned fixture.
- TON duration/admission/schema controls: 15; merged actual-preset controls: 16; latest changed OLAND/integration checks: 32; publisher/revision controls: 5. All scoped runs pass without skips.
- Website main 299c376: query timer plus preparation duration fixes published and exact HTML/JS bytes verified. All 90 client tests pass. Browser interaction is not inferred.

## Current release and settings

- Original TON code 24f548550 / Core 2ac915f: app job 4f6eba6c-5ee4-4afe-95a5-07849a92d7ec SUCCESS.
- Newer reviewed Development fb29aa936870dcfb3182102642e5997b2cedd90f preserves TON code and Core/ONODE pins, adds OLAND contracts; task merge 99b4f080 preserves it. App job 571cb5f0-85f4-40fa-9d99-568339833b3e is BUILDING. Observe this same job; never resubmit.
- Two live settings changed after preparation. Explicit reviewed revision ton-public-duration-oland preserves all current unrelated values; only three TON keys differ, and their values/public review bytes equal the tested candidate.
- Protected before/candidate snapshots: LocalState/COSMIC/HostedSourceRolloutCurrent/ton-public-duration-oland-*.dpapi. Public review: TestResults/TonPublicSourceCandidateCurrent/ton-duration-oland-prepared-review.json.
- Earlier ton-public and ton-public-duration candidates are stale. No TON mutation intent, apply, hosted acceptance session or card exists.

## Exact next steps

1. Observe job 571cb5f0 with expected commit fb29aa936870dcfb3182102642e5997b2cedd90f until terminal. A timeout is not failure or permission to restart.
2. After SUCCESS, recheck the entire current variables dictionary against the revised protected before snapshot. Preserve concurrent changes; never overwrite or silently rebase.
3. In this same worktree temporarily select the exact reviewed deployed commit. Set COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT, run unchanged require_matching_release(configuration), then generic configuration.apply_values("ton-public-duration-oland", KEYS). KEYS are exactly CQL_TON_PUBLIC_SOURCE_JSON, PREPARED_STORAGE_ADMISSION_JSON, CQL_HOSTED_QUERY_EXAMPLES__TONOASIS. Return task branch before doc commits.
4. Verify all three per-key ACK hashes and whole candidate match. Journal exactly one settings rollout; preserve unknown outcomes without replay. Observe its exact job.
5. After exact settings SUCCESS, run python -B Scripts/Sandbox/Verify-HostedQuerySource.py TONOASIS --prepare once, using the task branch's explicit TON selector. Original hosted cohort must retain its sessions, IDs, save intents/ACKs and completed stages across any failure.
6. Prove native 3/2, paired 5/4, reuse 0/2, privacy 0/3 and exact SQLite/Neo4j copies, then completion ACK. Publish the TON card with local demo/guide links only after these gates pass.

## Limits and deferred work

- Hosted Azure and Oracle explicitly deferred by David; free only, no signup/spending re-asks. Local/emulator proof is not hosted proof.
- Browser transport/control remains closed; no OS automation resumed. Solana and other browser gates remain open; David already opened the page.
- TRON remains unqualified: native 429; explicit PublicNode diagnostic 403 before any validated header. No guessed delay, retry, alternate runtime path or provider count increase.
- Public-source MVPs do not certify source writes, broad metadata scans, independent consensus, distributed snapshots, full ORM parity or universal uptime.
- Full tracking/concurrency/children/history/deltas/migration/placement audit and broader HyperDrive qualification remain incomplete. V2 code only; legacy gaps documentation only.

## Recovery and resource rules

- Work only C:/Source/OASIS-cosmic on work/cosmic-cql-ton-public; Core work/cosmic-cql-google-public. Frontend C:/Source/Web4Site on work/cosmic-cql. Unqualified TRON code remains excluded from this release.
- Raw outputs TestResults/; durable source/config/identities/ACKs LocalState/COSMIC/. Completed cohorts never replay. Unknown root logs preserved.
- Existing pinned runtimes/environments only; resource ownership/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. No new accounts, installs or service environments in this checkpoint.
- Builds under Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 with /m:2 /nr:false /p:UseSharedCompilation=false. Committed dependency pins validate; fetch before integrations and verify submodule pushes. No production/master promotion.

[TON evidence and procedure](TON_PUBLIC_SOURCE_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity checklist](CQL_PARITY_CHECKLIST.md) | [Deferred work](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
