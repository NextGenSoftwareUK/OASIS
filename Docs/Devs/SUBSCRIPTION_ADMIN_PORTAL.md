# OPORTAL subscription administration

OPORTAL exposes the administration workspace at `/admin` after an authenticated OASIS `Wizard` login. The dashboard shows the Billing & Analytics card only for a locally identified Wizard, while WEB4 and WEB6 independently enforce administrator authorization on every request.

## Screens

- **Analytics:** customer and active-subscription totals, operations, failures, settled units, revenue, provider cost, gross margin, plan mix, provider/model mix and operation status for a selected UTC date range.
- **WEB6 Pricing:** displays the active external JSON catalogue, formats and validates a proposed document, and publishes a new immutable version with administrator, time and required reason metadata. Existing versions are never edited.
- **Subscriptions:** current plan, state, period and pay-as-you-go preference for all customers.
- **Invoices:** immutable Stripe-derived invoice records and payment states. Existing Railway Stripe and webhook configuration is reused.
- **Usage:** global operation projections, measured units, provider/model identity, cost and settlement state.
- **Audit:** append-only ledger history and administrative corrections.
- **Outboxes:** per-service durable settlement state. A dead-letter can be requeued only with a new action UUID and a reason; the operation's immutable settlement is reused and provider execution is never repeated.

## API boundaries

WEB4 serves `/api/subscription/admin/*` for customer, invoice, ledger, audit, outbox and analytics data. WEB6 serves `/v1/admin/config/catalogue*` for catalogue validation and immutable publication. Browser code sends the existing avatar bearer token; it never receives or embeds Railway service credentials, MongoDB connection strings, Stripe secrets or `WEB6_ADMIN_API_KEY`.

The backend accepts OASIS's established authenticated `Wizard` avatar as the interactive administrator. Dedicated automation JWTs remain supported only when they contain the subscription-admin claim and their avatar UUID is listed in `SUBSCRIPTION_ADMIN_AVATAR_IDS`.

### Granting a plan without Stripe

`POST /api/subscription/admin/plan-grant` (administrator only, same authorisation as the other admin routes) puts an avatar on a plan without a Stripe subscription, for example Enterprise for an internal or partner application. Body: `{ "userId": "<avatar UUID>", "planId": "free|bronze|silver|gold|enterprise", "reason": "<at least 5 characters>", "expiresAtUtc": "<optional, at most 10 years>" }`.

- Stripe stays canonical: it returns `409` if the avatar has an active Stripe subscription (cancel it first).
- Every grant is written to the billing audit (`admin-plan-grant`) with the acting administrator, the reason, the previous plan and the expiry, and the avatar's authorization version is bumped.
- Granted plans carry no pay-as-you-go billing. Granting `free` reverts the avatar to the free plan.
- A later Stripe event for the avatar replaces the granted plan with the canonical Stripe state.

### App accounts (plan per application)

A product with many users (for example PIPER) can hold a plan of its own, so its users are not limited by their personal Free quota and no grant is needed per user.

- `GET /api/subscription/admin/app-accounts` lists apps (keys are never returned).
- `POST /api/subscription/admin/app-accounts` with `{ "appId": "piper", "name": "PIPER", "planId": "enterprise", "reason": "..." }` creates an app. The response contains `apiKey` **once**; store it as a secret. Only a SHA-256 hash is kept.
- `POST /api/subscription/admin/app-accounts/{appId}/rotate-key` issues a new key and invalidates the old one.
- `POST /api/subscription/admin/app-accounts/{appId}` with `planId` and/or `status` (`active` or `disabled`) changes the app's plan or disables it; a disabled app's credentials stop working at once.
- All changes are audited (`app-account-*`) with the administrator and reason. `appId` is 3 to 40 lowercase letters, digits or hyphens.

**How it is used.** The app's server sends `X-OASIS-App-Id` and `X-OASIS-App-Key` with the user's normal bearer token. On WEB4's own authorization path the request is then evaluated against the more generous of the app's plan and the user's personal plan, so an app can never make a user worse off. Invalid or partial app credentials are rejected (401); requests without app headers behave exactly as before. Usage is still recorded against the signed-in avatar and the settlement protocol is unchanged.

**Enterprise only unless pooled quotas are switched on.** By default the app plan is a per-user entitlement, so a finite app plan would multiply its limits by the number of users; create and update therefore accept only `enterprise`. Setting the server environment variable `OASIS_POOLED_APP_QUOTAS=true` allows finite plans **with `"pooledQuota": true`**: the plan is then ONE pool shared by every user of the app (counted in the same transaction as each user's own usage), and users keep personal limits at the app's `perUserPlanId` (default `silver`, never `enterprise`) or their own plan, whichever is more generous. When the pool is exhausted, requests are denied with `APP_`-prefixed codes (for example `APP_MONTHLY_REQUEST_LIMIT_EXCEEDED`). `GET /api/subscription/admin/app-accounts/{appId}/usage` shows the pool against its limits. Enterprise apps and requests without app credentials never touch a pool. `GET /api/subscription/admin/app-accounts/{appId}/top-users?limit=10` lists the avatars using the most of the pool this month, and the usage view carries a `status` (`ok`, `warning` from 80%, `exhausted` at 100%). The `subscription.ledger.pool_threshold` metric fires when a request crosses 80% or 100%, with matching Prometheus alerts in `Docker/monitoring/subscription-alerts.yaml`. For very high traffic set `OASIS_POOL_SHARDS` (1 to 16, default 1) to spread the pool across several documents; limits then become approximate by the number of requests in flight at once. See `Docs/WIP/Subscriptions/APP_ACCOUNT_SHARED_POOL_DESIGN.md`. Accepting app credentials on `usage/authorize` for WEB5 to WEB10 is done on the server side; those services must forward the headers.

## Deployment order

Deploy WEB4 and WEB6 admin APIs before OPORTAL. Remove the matching entries from `OPORTAL-JS/scripts/pending-endpoints.json` after regenerating endpoints from the deployed OpenAPI documents. The ledger activation procedure is separate and is documented in `SUBSCRIPTION_RAILWAY_CONFIGURATION.md`.
