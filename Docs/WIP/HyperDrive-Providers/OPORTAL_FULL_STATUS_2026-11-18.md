# OPORTAL and HyperDrive full status — 18 November 2026

**OPORTAL is not 100% complete or accepted end to end.** Subscriptions, performance and finishing every screen remain the priority. The full provider/HyperDrive programme is also incomplete.

## Latest verified results

| Area | Authoritative evidence | Limit |
| --- | --- | --- |
| Atomic profile backend and shipping JS SDK | Run44411526328/job135180639408: two mode cases passed, six actual SDK→compiled MVC→native MongoDB/SQLite/Neo4j flows; clears, reloads, stale409 and non-profile-field rejection | Full production Startup/JWT, paid admission and applicable hosted rollout gates remain |
| Actual profile browser component | Run44421853997/job135216589880 SUCCESS on parent6607c3f, Portalb380fff, Core2ae9c2 and SDKd7aa569; two cases/six flows, zero failures/skips | Actual fragment/CSS/bundle; not the whole portal or portrait upload |
| Profile component timings in isolated CI | Open172–1657ms; save540–792ms across those six flows | Not dashboard/subscription timings or current live-site performance |
| Portrait authorization and native persistence | Individual PortraitUploadBindsAllTargetsBeforeProviderActivationNative PASSED in run44428719265/job135240240363 on6e7cb7f; compiled action checks plus real owned Mongo PNG save | Overall job FAILED because its separate subscription fixture failed. Not production HTTP, browser upload, SQLite/Neo4j portrait or concurrent-update acceptance |
| Browser delivery and cache keys | Portal44423712983/job135222990234 SUCCESS; locked qualified WEB4 bundle, source-derived config/avatar/SDK references, dependency/cache cleanup | Source/build verification; no rollout |
| Subscription session read fix | Portala21efc7 binds deferred reads to the captured token and drops superseded/stale responses; focused state checks passed | New native subscription HTTP acceptance is still pending |
| Quest fake-success removal | STAR0269f11 removes four generated-data substitutions; conditions4→0, helper references10→0, thirteen actions retained | Source-only; parent/deployed STAR pin remains1330ffc. Build and real quest acceptance remain open |

The portrait test report was independently checked: artifact14486235839,1898 bytes, SHA25605ab2ee0453218089a184dce68f72171db5f3dcd894cab12cebe4f2c46be6495. Its test results explicitly record portrait Passed/subscription Failed. It was read and disposed in RAM; no archive or extracted file was written.

## What is being worked on now

**SubscriptionSelfReadsUseActualControllerAndOwnedMongo** is running as44430620795 on qualificatione32819d/sourcebbfdde0. Its latest correction initializes the native fixture avatar's role explicitly. It uses the actual controller, subscription service and Mongo repository, signed owned JWTs and native avatar lookup. It must prove self-only plan/cancellation/order reads, empty results for a real avatar without a subscription, ignored foreign userId query selectors and unchanged billing record counts. It is **not yet accepted**. The passed portrait case is excluded from this rerun.

Earlier setup failures are retained as failures, not counted as acceptance. Repository constructor, real DI interfaces, actor context and portrait controller dependencies were corrected from exact pinned source.

## Remaining OPORTAL acceptance

| Area | Remaining work |
| --- | --- |
| Subscriptions | Finish the native read case, current browser/API checkout return, billing portal, cancellation, Orders/invoices, usage/overages, signed webhook and error/recovery checks. Keep genuine Wizard/admin acceptance separate |
| Profile popup | End-to-end portrait HTTP/browser upload, errors, concurrency and applicable production identity/admission rollout |
| Dashboard/performance | Fresh isolated dashboard and subscription-popup measurements, real card/count/navigation flows and regression audit; no current live speed claim |
| Every screen/popup | Complete the source-derived acceptance ledger and fix actual wiring/backend gaps; static source checks do not sign off interactions |
| HyperDrive UI | Current status/preferences/deliveries/storage/error flows with real authenticated behavior and performance evidence |
| WEB5 read surfaces | Real reads and correct STARNET integration/deep links; qualify quest change without generated success data, then continue other affected controllers |

Historical development/Stripe test-mode browser acceptance dated13–14 October remains valid for its exact deployment. It includes checkout, upgrades/proration, cancellation, invoices and overages. It is not a current-release sign-off. Existing November hosted-payment/Wizard owner actions remain in ACTIONS-FOR-DAVID.md; do not duplicate or re-ask them. Three supplier-evidence recoveries remain deferred and must not be replayed, fabricated or deleted.

## Screen and SDK scope

Portal Docs/WIP/SCREEN_ACCEPTANCE_2026-11-18.md inventories44 modal fragments (43 shared-loader plus lazy Developer Portal), notifications and dashboard, five top-level HTML entries and21 hosting rewrites. These are inventory counts, not passing coverage.

The source wiring audit inspected39 script/template pairs. The SDK surface audit found99 regex-selected script/method pairs plus16 additional manually traced references on actual constructed shipping clients, with zero backend calls. These are not115 unique SDK methods, all call sites or an acceptance percentage. Dynamic receivers, direct HTTP routes, real results and browser interactions remain separate checks.

## Providers and HyperDrive

The provider matrix lists234 projects:56 non-storage and178 storage/specialized storage. This is an inventory, not234 fully implemented/accepted providers. Neon and Convex remain outside the priority list; HoloOASIS remains a priority and its desktop version is obsolete.

Selected failover, committed-only replication, selection and unknown-commit/no-replay cases have scoped acceptance for named providers/operations/revisions. Full all-provider failover, replication, load balancing and reliability are **not proved**. HoloOASIS listener cleanup/restart/public-read evidence exists for17 November; hosted authority-bound writes/replication remain open. See PROVIDER_MATRIX.md and PRIORITY_HYPERDRIVE_ROUTING.md rather than inferring provider completion from project counts.

## Isolation, deployment and cleanup

No changes in this phase were promoted to dev/staging/production. Shared OASIS Atlas tests/login/refresh/health probes remain stopped; worker pauses and protected deliveries535/543 are unchanged. No audit record for543 exists; do not authorize or replay it. Existing DNA settings were not changed.

Work remains on the existing owned branches/worktrees, preserving COSMIC/CQL. Development18675d6 was last observed; peer combined CQL source1fe2/Core0e67 has its separate acceptance. No unverified batch was swept into a deployment to synchronize branches.

Builds/native backends run in isolated CI with bounded workers and the build lease. Local source audits ran in RAM using existing Git objects/Node; source blobs and the tiny report were not materialized to disk. No new local environment, worktree, runtime install, build output or database was created. Successful cleanup is recorded per run; pending-run cleanup is not assumed.

Detailed records: CURRENT_STATUS.md, OPORTAL_PROFILE_SAVE_AUDIT_2026-11-18.md, OPORTAL_TEMPLATE_WIRING_AUDIT_2026-11-18.json, OPORTAL_SDK_SURFACE_AUDIT_2026-11-18.json, ../Cleanup/CLEANUP_LEDGER.md and ../Release and Status/ACTIONS-FOR-DAVID.md.
