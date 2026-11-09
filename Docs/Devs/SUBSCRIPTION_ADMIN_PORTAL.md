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

## Deployment order

Deploy WEB4 and WEB6 admin APIs before OPORTAL. Remove the matching entries from `OPORTAL-JS/scripts/pending-endpoints.json` after regenerating endpoints from the deployed OpenAPI documents. The ledger activation procedure is separate and is documented in `SUBSCRIPTION_RAILWAY_CONFIGURATION.md`.
