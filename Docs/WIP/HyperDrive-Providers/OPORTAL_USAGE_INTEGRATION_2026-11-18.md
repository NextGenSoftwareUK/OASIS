# Usage and Billing integration — 18 November 2026

Current status: scoped native/browser read acceptance PASSED in44449998164/job135311323457 on root61449a7f8e37151b9128352f5b5b26eca54f932d/Portalbc615734. One case passed, zero failures/skips, three actual owned actors. Full Usage/Billing and OPORTAL remain incomplete.

The Portal calls `/api/subscription/usage/series`, `/usage/estimate`, `/usage/notifications` and `/app-owner/apps`. The task branch lacked the series and owner controllers already present in Development18675d6fad4544f1a2ab9b6dc992bffd4ca2ecc4. This is a branch source gap, not evidence that Development's existing implementation is missing. Do not recreate it or import unrelated COSMIC/Seal/Purge changes blindly.

The first scoped integration copies these existing Development blobs byte for byte:

| File under WEB4 | Development blob |
| --- | --- |
| Controllers/SubscriptionUsageSeriesController.cs | d9807f91f956eb48184b30670005a4462e983b47 |
| Services/Subscription/MongoSubscriptionUsageRepository.Series.cs | 5d7291b5a94180229429df65e25dd29209b8d8e4 |
| Services/Subscription/UsageSeries.cs | 051b1fb93a3152e78d5c4c196e39348713de4489 |
| Services/Subscription/UserOverageEstimate.cs | b88d97579c35bd10ee42e77ba53e870433756cc1 |
| Services/Subscription/AppOverageEstimate.cs | 636644b2e0e12a83d594067e5cc072a1e3d3f7a5 |

Startup registers IUsageSeriesSource against the existing real Mongo repository. Four reviewed controller actions were added to the route inventory, preserving all prior rows: 767 actions / 655 provider or external-manager classifications. Local Git blob hashes match Development exactly. No private gitlink, manifest pin, DNA setting, worker control or COSMIC runtime file was changed.

The new `SubscriptionUsageBrowserUsesActualControllerAndOwnedMongo` case reuses the owned native HTTP/browser lifecycle and freezes Portalbc615734952962f9c445a655eec24476538eeb70. Actual persisted day/month buckets differ between two subscribed avatars; a third real avatar has no subscription. Actual avatar details contain karma1000/0, resolved through a temporary, restored Program.AvatarManager bound exclusively to the owned Mongo provider. The real SDK/component checks summary counts, history SVG, estimate, consistent monthly limits, authoritative karma daily limits, prices/statements reads, invalid dates, admin refusal and a delayed response after account change. It sends no billing writes. Seeded ledger/billing records are fixtures, not provider work receipts or Stripe payments.

Notification and app-owner settings/actions depend on additional Development code and remain open. This focused read check does not accept them, Stripe payment, production Startup/JWT, whole-portal navigation or live/dashboard timings. Existing accepted profile and subscription/Orders cases are not selected again.

Actual WEB4 build passed with zero errors; source/pin gates and the focused native browser case passed. SummaryMs654/60/55; subscribed history/estimate completion810/92ms, missing-plan gate65ms. Zero usage write requests; billing and bucket record counts unchanged. Native karma1000/0 produced correct daily quotas and monthly limits agreed across summary/chart/estimate. Invalid dates returned400; regular-user admin request403. Shipping and fixture catalogue hashes matched. Browser/runtime/cache/control cleanup marker, provider/database finalizer and Mongo container stop passed. These are isolated component timings, not live/dashboard or Stripe acceptance. No local runtime installation, build, environment, database or generated test output. The temporary route-review CSV `Docs/WIP/HyperDrive-Providers/.usage-route-review-377b7bda00a34f65882017ce333b85d6.csv` was deleted and absence independently checked. CI verified its owned browser/runtime/cache/control files, provider deactivation/exact database drop-absence and Mongo container removal. No shared Atlas/API/Stripe requests or promotion.


Earlier44448192100 failed: the reflected API host lacked the shipping pricing file, causing prices500; pending response waits then masked the failure as a45s handoff timeout. The test build now links the real catalogue (API shipping build already did), verifies matching bytes and observes responses with bounded awaited diagnostics. No production pricing fallback or fake response was added. Failed artifact14493759371/4550B was SHA256-verified and inspected/disposed in RAM, zero local files. Accepted artifact14494924289/4412B SHA25677549798d7aeb506bd94114145496ed6d280e0b300df885acd7c9e9a65612dc2 was inspected through logs/metadata only; no ZIP download. See OPORTAL_USAGE_BROWSER_EVIDENCE_2026-11-18.json.
