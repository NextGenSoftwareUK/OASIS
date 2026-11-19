# App accounts: shared (pooled) quotas - design

Status: **implemented, off by default.** Set `OASIS_POOLED_APP_QUOTAS=true` to enable; until then every request behaves exactly as before and finite app plans are still refused. Not yet soak-tested against a real replica set or under load (see Rollout).

Built: pool buckets (`app:{appId}` subject) updated in the same transaction as the avatar buckets; pool admission with `APP_`-prefixed denial codes; per-app `PooledQuota` and `PerUserPlanId` (default silver, never enterprise); `GET /api/subscription/admin/app-accounts/{appId}/usage`. Also built: `GET .../app-accounts/{appId}/top-users?limit=10` (this month, busiest first, net audited request/unit/cost deltas joined to operation identities; existing indexes `ix_pool_app_authorized` and `ix_operation_audit`); a `status` of `ok` / `warning` (80%) / `exhausted` (100%) on the usage view; metric `subscription.ledger.pool_threshold` (tags `app_id`, `threshold` 80 or 100, `limit`) fired by the request that crosses each threshold, with Prometheus rules `SubscriptionPoolWarning` and `SubscriptionPoolExhausted`; and optional pool sharding via `OASIS_POOL_SHARDS` (1 to 16, default 1). Current task source fixes cross-shard admission write skew by writing the existing app AuthorizationVersion inside every pooled admission transaction. Bucket writes remain sharded; concurrent admissions and app configuration changes share that app write. Native run44477583569 passed the focused 1/2/16-shard admission case (eight contenders for one remaining slot, one committed event/audit/fence per layout; owned cleanup passed). This is correctness acceptance only, not throughput/soak or app charging acceptance. Reads sum all 16 possible shards. Changing shard count while operations are in flight is NOT yet accepted: settlement/expiry still derives its target from the current count rather than an immutable operation binding; this remains a required accounting fix. Not built: emailing the app owner contact on a threshold (metrics and the status field only).

## Problem

An app account (see `SUBSCRIPTION_ADMIN_PORTAL.md`, "App accounts") currently grants a **per-user entitlement**: a request carrying valid app credentials is judged on the more generous of the app's plan and the user's own plan, and counted against the user's own buckets. That is correct for Enterprise (unlimited) but wrong for a finite plan: a Gold app with 1,000 users allows 1,000 x Gold, because nothing caps the app as a whole. Until this is built, app accounts accept only `enterprise`.

How comparable platforms do it: the quota belongs to the project/key and is shared by all its users; per-user limits sit inside the pool.

## Goals

1. A finite app plan is a **single pool** (monthly requests, daily calls, daily tokens, monthly USD budget) shared by every user of the app.
2. Per-user limits still apply inside the pool, so one user cannot drain it.
3. Fully backwards compatible: requests without app credentials, and Enterprise apps, behave exactly as today.
4. No change to the settlement protocol that WEB5 to WEB10 use.

## Non-goals

Pricing and billing of app plans (see Open decisions), per-second rate limiting, and accepting app credentials from WEB5 to WEB10 (separate follow-up).

## Design

**Where counters live.** All usage movements go through one method, `ApplyDeltaAsync`, which updates the avatar's month and day buckets. Add a second update in the same transaction: when the operation carries an `AppId`, apply the identical delta to buckets whose subject is `app:{appId}`. `AppId` is already stored on the usage event, so authorize, settle, expiry, recovery, correction and migration all reach it without extra plumbing, and the avatar and app buckets cannot drift apart because they commit together.

**Admission.** In `AuthorizeAsync`, after the per-user check, load the app aggregate (`app:{appId}`) and run the existing `SubscriptionUsagePolicyEvaluator.EnsureAuthorized` with the **app plan's policy** (karma 0). Either check failing denies the request. Denials use the existing codes, plus an `APP_` prefix variant (for example `APP_MONTHLY_REQUESTS_EXCEEDED`) so clients can tell "you are out" from "the app is out".

**Per-user limits inside the pool.** Each app account gets a `PerUserPlanId` (default `silver`, administrator-set). A user's personal limits for requests made through the app are the more generous of their own plan and `PerUserPlanId`. This keeps PIPER-style users out of the Free-tier trap without letting any single user consume the pool. Enterprise apps skip the pool and keep today's behaviour.

**Budget and overages.** The app pool has the plan's monthly USD budget. Pay-as-you-go stays per avatar and is not extended to apps in this phase (an exhausted pool denies).

**Storage and migration.** No schema migration: app buckets are new documents in the existing bucket collection (`app:` subject prefix, which cannot collide with avatar UUIDs). Existing events and buckets are untouched. Usage already recorded under an app before this ships is not back-filled; pooled limits start counting from rollout, and the audit trail has the data if a back-fill is ever wanted.

**Reporting.** Add `GET app-accounts/{appId}/usage` (administrator) returning the pool's month and day aggregates, top users by usage, and days-to-exhaustion. This is what makes a pooled plan operable.

**Alerts.** At 80% and 100% of any pool dimension, write an audit entry and raise the existing quota notification for the app owner contact.

## Testing

- Unit: pooled admission denies on whichever of user or pool is exhausted first; per-user limit inside pool; Enterprise app skips pool; no app headers unchanged.
- Property test: for any sequence of authorize/settle/expire, the sum of avatar buckets for an app equals the app bucket.
- Mongo replica-set integration: concurrent authorizations from many users near the pool limit never exceed it (the transaction serialises on the app bucket document).
- Contention check: all users of one app write the same two bucket documents, so measure throughput under load before enabling for a high-traffic app.

## Risks

- **Hot document.** Every request from an app updates the same two app buckets inside a transaction, which can cause write conflicts and retries under heavy load. Mitigation: measure first; if needed shard the app bucket by a small hash of the avatar and sum on read (admission then uses a slightly conservative estimate).
- **Latency.** One extra bucket read and write per request through an app.
- **Blast radius.** Touches the ledger's core transaction. Ship behind an app-account flag (`PooledQuota` true only when set) so every existing flow is provably unchanged.

## Open decisions (need the business owner)

1. **App plan catalogue and prices.** What pooled app tiers exist and what each costs. A natural starting point is pool = N x the matching avatar plan, but the multiple and the price are commercial decisions.
2. **Default `PerUserPlanId`** and whether apps may choose it.
3. **What happens at exhaustion**: hard deny, or optional overage for apps.
4. **Who may create app accounts**: today only administrators; a self-service developer flow needs billing first.

## Rollout

1. Build behind the flag, tests above green.
2. Enable for one internal finite app on staging, soak under load.
3. Lift the Enterprise-only restriction on create/update only after the soak and an agreed catalogue.


## 19 November 2026 read-path update

The actual owner usage view and net aggregation passed isolated browser/MVC/native Mongo qualification44461701598 on root1c85d38d/Portald115c2b9 (one new case, three actors). Expired reservations net to zero; settled corrections are included; foreign apps and older months are excluded. Every possible pool shard is summed, and the UI renders net requests/units/USD costs using authoritative avatar IDs without per-row directory/provider hydration. The owned five operations/eleven audit deltas/twelve buckets stayed unchanged; only the explicit view-disable action wrote a settings audit. No admission/settlement/payment or load/soak acceptance is inferred from these read fixtures.

The existing reviewed app USD catalogue is now being wired into owner billing reads. The full app consent/admission/immutable statement/Stripe path is still required; see [the billing plan](../HyperDrive-Providers/APP_BILLING_PROTOCOL_PLAN_2026-11-19.md) and [current status](../HyperDrive-Providers/CURRENT_STATUS.md). Old phase/non-goal notes above describe the original pool-only rollout; they are not a claim that current app billing is complete. Pool flags, node DNA, hosted workers and shared Atlas were not changed.


2026-11-19 EXACT POOLED ADMISSION ACCEPTED (SCOPED): Run44477583569/job135401757200 SUCCESS on dd4f980646f562a1df1ede759daf256274414ab0/Core2ae9c2/STARff20026. Actual WEB4 API built with zero errors; only PooledAdmissionIsExactAcrossShardsAndRejectsStaleAppState selected, passed1/failed0/skipped0,1s. For each 1/2/16 writer-shard layout eight simultaneous actual native reservations competed for one remaining slot, reading16 historical shards: exactly one event, attributed authorized audit/request delta and app version fence committed; seven quota refusals; aggregate exactly300000. Correct committed replay and cross-app operation-ID conflict verified. Inactive/missing/stale-plan/stale-per-user/disabled-pool requests created no further usage/audit writes. Independent personal monthly/daily/token/budget restrictions remained enforced.

Artifact14507890583 (1723B, SHA2561518571252dc289cbd2a09a6dd4e09f1ec64443e07ea370fb5c244c1aeea975f) contains exactly one passed seal-routing-native.trx. Native stdout inspected in RAM confirms all three layouts and preserved personal guards; streams/archive disposed, filesWritten0. Native unique database absence and temporary process settings restoration verified; workflow exact Mongo container/volume removal and absence marker passed. Isolated case elapsed415/144/215ms are correctness-case durations, NOT throughput or live dashboard measurements. No earlier accepted suite rerun, shared Atlas/Stripe/live API call, deployment/DNA/worker change, new index/collection or local runtime/build/database/archive.

Still required: immutable operation shard and payer/terms bindings with an evidence-based legacy transition; positive Stripe readiness/opt-in; app monetary cap admission, expiry/settlement/corrections/reconciliation, immutable statement allocation and actual collection/webhooks. Full OPORTAL screens/dashboard/performance, hosted/Wizard actions and all-provider HyperDrive reliability remain incomplete.
