# WEB4-WEB10 subscription ledger: exact Railway runbook

Follow this checklist in Railway's **development** environment first. Do not enable production until the complete development test at the end passes.

## What is already configured

Railway already has `OASIS_DNA_JSON`, the existing `STRIPE_*` variables, and `GITHUB_PAT`. Keep them. Do not create another MongoDB connection or another set of Stripe variables. WEB4-WEB10 write `OASIS_DNA_JSON` to `/app/OASIS_DNA.json` and reuse its MongoDB connection.

The ledger creates dedicated collections in the same MongoDB instance. It does not store accounting records in the shared `holons` collection.

| Owner | Default database |
|---|---|
| WEB4 ledger and billing | `oasis_subscription_accounting` |
| WEB5-WEB10 outboxes | `oasis_web5_subscription_outbox` through `oasis_web10_subscription_outbox` |

WEB6 pricing is loaded from `Configuration/web6-model-catalogue.json` inside the deployed application. The source file is **`WEB6/NextGenSoftware.OASIS.Web6.Core/Configuration/web6-model-catalogue.json`**, locally `C:\Source\OASIS\WEB6\NextGenSoftware.OASIS.Web6.Core\Configuration\web6-model-catalogue.json`. [Open the JSON in the WEB6 repository](https://github.com/NextGenSoftwareUK/OASIS-WEB6/blob/Development/NextGenSoftware.OASIS.Web6.Core/Configuration/web6-model-catalogue.json). No Railway price variable is required.

Paid plan amounts and currencies are read from the existing Stripe prices. For example, if the configured Bronze price is GBP 9, OPORTAL must display GBP 9 and its paid invoice must remain GBP. AI provider usage is accounted for separately in USD; revenue from different currencies is never added together as USD.

The Stripe webhook destination must use the API version supported by the deployed Stripe.net SDK. The current SDK is Stripe.net 47.0.0, using `2024-10-28.acacia`. Configure that version on the subscription webhook destination and enable `checkout.session.completed`, `customer.subscription.created`, `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.paid`, `invoice.payment_succeeded`, and `invoice.payment_failed`. An incompatible event schema is rejected visibly. Reuse `STRIPE_WEBHOOK_SECRET` for the matching destination's signing secret.

On checkout return, OPORTAL confirms the authenticated session through `GET /api/subscription/checkout/session/{sessionId}`. A redirect alone never activates a paid plan. Activation requires a completed checkout, a paid/no-payment-required session, and the matching canonical subscription committed by its verified webhook.

## What an endpoint costs

The existing Stripe monthly price funds included usage. Extra USD charges require an active paid subscriber to opt in to the published overage prices and a spending cap. Provider measurement alone does not activate extra billing. [Paid overage prices and steps](SUBSCRIPTION_PAID_OVERAGES.md) explain the separate USD invoice contract.

Reconciliation uses the included-plan billing contract by default. A closed period with USD provider costs does not require an additional USD Stripe usage invoice. Explicit pass-through reconciliation remains available to callers that actually have that billing contract; tagged usage invoices are checked when present. When opted-in customer overages occur, their charge totals must match standalone USD invoices; the invoice must not charge all included provider costs again. The report's `requiresStripeUsageInvoice` field makes this distinction visible. Provider evidence and ledger/audit checks remain required, and an open UTC month remains pending rather than being reported fully balanced.

| API | Current accounting policy |
|---|---|
| WEB4 | Subscription, Stripe webhook, quota and ledger authority. Direct WEB4 business routes are not metered by the WEB5–WEB10 consumer adapters. |
| WEB5 | One included request per covered business operation; USD provider cost is zero under `web5-included-request-v1`. |
| WEB6 | One authorized operation; supported AI operations settle actual measured tokens/units using their immutable model catalogue version. Other business operations use the included-request policy. |
| WEB7–WEB10 | One included request per covered business operation; USD provider cost is zero under the corresponding `webN-included-request-v1` policy. |

Public identity routes, health, discovery and specified control operations are exempt. Administrator catalogue operations require administrator authorization, not a customer subscription. An operation ID prevents repeated execution; repeat calls with new IDs are separate operations.

For token models, cost is `(input tokens × input USD/million + output tokens × output USD/million) / 1,000,000`. The JSON stores `inputPer1kUSD` and `outputPer1kUSD`; the per-million values are derived from those fields. Examples from the bootstrap catalogue:

| Provider/model | Minimum plan | Input USD/million | Output USD/million |
|---|---|---:|---:|
| OpenAI / `gpt-4o-mini` | Bronze | 0.15 | 0.60 |
| OpenServ / `gpt-5.4-mini` | Bronze | 1.00 | 6.00 |
| OpenServ / `gpt-5.4-nano` | Bronze | 0.25 | 1.60 |
| Ollama / `llama3.3` | Free | 0 | 0 |

These are configured contractual rates, not a promise that an unconfigured provider is available. Model availability and supported receipt adapters are checked before execution. OpenServ requires a system/developer message. For a minimal request, use `tools: []`, `useFAHRN: false`, `useHolonicBraid: false`, `injectAvatarContext: false`, and a small `maxTokens`; otherwise reservation includes generated tools and possible enrichment.

## Defaults and limits

WEB4 reads the canonical paid plan from its Stripe-backed subscription record. A plan value in an AI request cannot unlock a higher model tier. The current base policy in `SubscriptionUsagePolicyProvider.cs` is:

| Plan | Existing Stripe price | Requests/month | Calls/day | Tokens/day | USD usage budget/month |
|---|---:|---:|---:|---:|---:|
| Free | 0 | 1,000 | 20 | 50,000 | 1 |
| Bronze | GBP 9 | 10,000 | 100 | 250,000 | 10 |
| Silver | GBP 29 | 100,000 | 500 | 1,000,000 | 50 |
| Gold | GBP 99 | 1,000,000 | 2,000 | 5,000,000 | 250 |
| Enterprise | Contact Sales | Unlimited | Unlimited | Unlimited | Unlimited |

Daily limits apply in addition to the monthly limit, across the consuming APIs. Authoritative karma multiplies the daily call allowance: 500→1.5×, 1,000→2×, 5,000→3×, 20,000→5×, 100,000→10×. Resets use UTC calendar days/months. An AI reservation also reserves an input/output/fan-out bound before provider work; the final cost uses measured usage, not the reservation estimate.

The existing fixed monthly plans and included quotas are the default. Optional paid overages use reviewed external JSON request rates and WEB6 catalogue costs with no AI margin. Enabling them requires price-version/fingerprint consent, a default payment method and a monthly USD cap; missing consent returns `400 OVERAGE_CONSENT_REQUIRED`. USD overages are collected separately from GBP monthly plans. Server-wide HyperDrive configuration changes require an administrator; they cannot change a subscriber's purchased plan. Legacy GraphQL/gRPC HyperDrive stubs return explicit unsupported errors instead of empty usage or unconditional quota approval.

The JSON is the bootstrap catalogue. Administrator publication saves a new immutable version and the previous bootstrap document in Holon settings, activates the new version, and reloads that published version after restart. Existing operations keep their original version. Use a new version name and a publication reason; never edit an already published version.

## Add the required variables

### Step 1: generate six secrets

Generate six different secrets of at least 32 random bytes. Temporarily label them `WEB5_SECRET` through `WEB10_SECRET`. Do not save their values in source control or this document.

Run this PowerShell once for each secret:

```powershell
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

### Step 2: configure WEB4

In Railway, select **development**, open **WEB4 OASIS API**, then **Variables**. Add:

```text
SUBSCRIPTION_SERVICE_KEY_WEB5=<WEB5_SECRET>
SUBSCRIPTION_SERVICE_KEY_WEB6=<WEB6_SECRET>
SUBSCRIPTION_SERVICE_KEY_WEB7=<WEB7_SECRET>
SUBSCRIPTION_SERVICE_KEY_WEB8=<WEB8_SECRET>
SUBSCRIPTION_SERVICE_KEY_WEB9=<WEB9_SECRET>
SUBSCRIPTION_SERVICE_KEY_WEB10=<WEB10_SECRET>
SUBSCRIPTION_LEDGER_INITIAL_STATE=empty-new-installation
```

The initialization setting is required once because this installation has no existing users or accounting records. It is not a historical migration. The first ledger write verifies that the collections are empty and creates a permanent initialization marker. If data unexpectedly exists, it fails without overwriting it.

Do **not** add `SUBSCRIPTION_USAGE_ENABLED` to WEB4 yet.

### Step 3: configure WEB5

Open **WEB5 STAR API** > **Variables** and add:

```text
WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one
SUBSCRIPTION_SERVICE_KEY_WEB5=<the same WEB5_SECRET stored on WEB4>
```

Do not enable usage yet.

### Step 4: configure WEB6

Open **WEB6 AI API** > **Variables** and add:

```text
WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one
SUBSCRIPTION_SERVICE_KEY_WEB6=<the same WEB6_SECRET stored on WEB4>
```

Do not enable usage yet.

### Step 5: configure WEB7-WEB10

Repeat the same pattern:

| Railway service | URL variable | Matching secret variable |
|---|---|---|
| WEB7 SYMBIOTIC API | `WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one` | `SUBSCRIPTION_SERVICE_KEY_WEB7=<WEB7_SECRET>` |
| WEB8 IGN API | `WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one` | `SUBSCRIPTION_SERVICE_KEY_WEB8=<WEB8_SECRET>` |
| WEB9 SINGULARITY API | `WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one` | `SUBSCRIPTION_SERVICE_KEY_WEB9=<WEB9_SECRET>` |
| WEB10 THE SOURCE API | `WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one` | `SUBSCRIPTION_SERVICE_KEY_WEB10=<WEB10_SECRET>` |

Each consumer gets only its own secret. Its value must exactly match the same variable on WEB4. Never reuse one secret for multiple services.

## Activate one service at a time

Complete one step before moving to the next. If a service fails, remove its `SUBSCRIPTION_USAGE_ENABLED` variable or set it to `false`, redeploy it, and investigate before continuing.

### Step 6: activate WEB5 only

1. On WEB5, add `SUBSCRIPTION_USAGE_ENABLED=true`.
2. Wait for Railway deployment to complete.
3. Confirm `https://dev.api.starnet.oasisomniverse.one` returns HTTP `200`.
4. Perform one authenticated WEB5 operation covered by subscription usage.
5. In OPORTAL admin, confirm its authorization, settlement, usage event, audit record and completed WEB5 outbox operation.

### Step 7: activate WEB6

1. On WEB6, add `SUBSCRIPTION_USAGE_ENABLED=true`.
2. Wait for deployment and confirm `https://dev.api.web6.oasisomniverse.one` is reachable.
3. Perform one supported AI request.
4. In OPORTAL, confirm its catalogue price, reservation, measured usage, settlement, audit record and completed WEB6 outbox operation.

### Step 8: activate WEB7-WEB10

Enable and test them individually in this order:

1. WEB7: verify `https://dev.api.web7.oasisomniverse.one` and one metered operation.
2. WEB8: verify `https://dev.api.web8.oasisomniverse.one` and one metered operation.
3. WEB9: verify `https://dev.api.web9.oasisomniverse.one` and one metered operation.
4. WEB10: verify `https://dev.api.web10.oasisomniverse.one` and one metered operation.

After every operation, verify usage, audit and outbox records in OPORTAL before enabling the next service.

### Step 9: enable WEB4 workers

After WEB5-WEB10 have all passed:

1. Add `SUBSCRIPTION_USAGE_ENABLED=true` to WEB4.
2. Wait for deployment.
3. Confirm `https://dev.api.web4.oasisomniverse.one` returns HTTP `200`.
4. Without logging in, confirm these protected routes return `401`:
   - `/api/subscription/admin/analytics`
   - `/api/subscription/admin/outboxes`
5. Log in to OPORTAL with a Wizard administrator and confirm all subscription admin pages load.

### Step 10: remove the one-time initialization variable

After the first successful metered operation appears in OPORTAL:

1. Confirm the stored marker is `v1-opening-balance` with `InitializationMode=empty-new-installation`.
2. Remove `SUBSCRIPTION_LEDGER_INITIAL_STATE` from WEB4.
3. Redeploy WEB4.
4. Confirm WEB4 is healthy and the existing ledger records remain visible.

## Complete development test

Use Stripe test mode:

1. Create or select a test customer in OPORTAL.
2. Start a test subscription through the existing Stripe checkout flow.
3. Deliver the existing webhook to WEB4 and confirm the subscription becomes active.
4. Run one metered operation from at least WEB5 and WEB6.
5. Confirm reservations, settlements, immutable audit entries and completed outboxes.
6. Confirm invoice and analytics screens show the same customer and usage.
7. Run reconciliation and confirm there are no unexplained ledger-versus-Stripe differences.
8. Restart WEB4 and one consumer. Confirm they remain healthy and no settled operation is charged twice.

Do not merge `Development` to `master` until this entire test passes.

## Production rollout

After development passes:

1. Merge OASIS `Development` to `master` using the repository promotion workflow.
2. Merge tested OPORTAL `dev` to its production branch.
3. Confirm production has the intended `OASIS_DNA_JSON` and Stripe variables.
4. Generate six new production-only service secrets. Do not copy development secrets.
5. Repeat Steps 2-10 with the production WEB4 URL.
6. Activate and verify one consumer at a time.

## Do not add these for the normal rollout

| Variable | Why it is unnecessary |
|---|---|
| `SUBSCRIPTION_MONGODB_CONNECTION_STRING` | Optional override; normal deployment reuses MongoDB from `OASIS_DNA_JSON`. |
| `SUBSCRIPTION_MONGODB_DATABASE` | Optional override; WEB4 uses `oasis_subscription_accounting`. |
| `SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING` | Optional override; consumers reuse the DNA MongoDB connection. |
| `SUBSCRIPTION_OUTBOX_DATABASE` | Optional override; each consumer has a dedicated default. |
| Duplicate Stripe keys or `STRIPE_PRICE_*` | Existing OASIS DNA and Railway Stripe settings are reused. |
| `WEB6_MODEL_CATALOGUE_PATH` | The reviewed JSON catalogue is deployed at its default path. |
| `SUBSCRIPTION_MIGRATION_APPROVAL_KEY` | Historical migration is unnecessary for this confirmed empty installation. |
| `SUBSCRIPTION_ADMIN_AVATAR_IDS` | Existing authenticated Wizard administrators already qualify. |

Telemetry is a separate optional operational feature. Leave `SUBSCRIPTION_TELEMETRY_ENABLED` absent or `false` during initial activation. If exporting metrics later, set it to `true` and configure a real `SUBSCRIPTION_OTLP_METRICS_ENDPOINT` (HTTP/protobuf, normally an HTTPS `/v1/metrics` destination) and any required `SUBSCRIPTION_OTLP_HEADERS`. A loopback address is appropriate only when a collector actually runs in that same container; do not add a dummy localhost endpoint to satisfy startup.

## Manage an existing subscription in Stripe

1. In Stripe's development **test mode**, configure one active customer portal with metadata `oasis_application=oasis-subscriptions`. Enable invoice history, payment-method updates, cancellation at the end of the billing period, and Bronze/Silver/Gold plan changes using the existing DNA price IDs. Development portal configuration `bpc_1UQHMID2t0mRpwzcfrzfp6C3` has these features enabled; plan changes invoice prorations immediately.
2. In OPORTAL, open **My Subscriptions → Manage billing**. WEB4's authenticated `POST /api/subscription/billing/portal` creates a session for the logged-in subscriber's stored Stripe customer. No new Railway variable or customer ID input is needed.
3. Change Bronze to Silver in the Stripe test portal. Confirm the signed subscription webhook updates WEB4, the proration invoice appears once in Orders, and the original Stripe subscription remains the only active subscription. Selecting a plan while already subscribed also opens billing management rather than creating a second subscription.
4. Cancel at period end. Confirm My Subscriptions shows the end date and access remains available through that date. Undo cancellation and confirm renewal is restored.
5. Before production promotion, configure the corresponding live-mode Stripe customer portal and repeat the approved release checks with production settings.

Paid overage rates are agreed in USD: Bronze 0.001, Silver 0.0005, Gold 0.0002 and Enterprise 0.0001 per extra successful request, with WEB6 AI excess at catalogue cost and no margin. Follow [the paid overage runbook](SUBSCRIPTION_PAID_OVERAGES.md) to verify consent, caps, USD collection, signed webhooks and administrator statements.
