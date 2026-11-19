# Usage and Billing integration — 18 November 2026

Current status: source integrated; real qualification pending. Full Usage/Billing and OPORTAL are not accepted.

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

Node syntax and source/route checks passed locally; real compile/runtime acceptance is pending. No local runtime installation, build, environment, database or generated test output. The temporary route-review CSV `Docs/WIP/HyperDrive-Providers/.usage-route-review-377b7bda00a34f65882017ce333b85d6.csv` was deleted and absence independently checked. CI must verify its own browser/runtime/cache/control-file and provider/database/container cleanup before acceptance. No shared Atlas/API/Stripe requests or promotion.
