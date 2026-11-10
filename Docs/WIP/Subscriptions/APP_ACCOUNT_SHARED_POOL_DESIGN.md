# App accounts: shared (pooled) quotas - design

Status: **implemented, off by default.** Set `OASIS_POOLED_APP_QUOTAS=true` to enable; until then every request behaves exactly as before and finite app plans are still refused. Not yet soak-tested against a real replica set or under load (see Rollout).

Built: pool buckets (`app:{appId}` subject) updated in the same transaction as the avatar buckets; pool admission with `APP_`-prefixed denial codes; per-app `PooledQuota` and `PerUserPlanId` (default silver, never enterprise); `GET /api/subscription/admin/app-accounts/{appId}/usage`. Not built yet: top-users report, 80%/100% alerts, pool sharding.

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
