# WEB4-WEB10 subscription ledger: exact Railway runbook

Follow this checklist in Railway's **development** environment first. Do not enable production until the complete development test at the end passes.

## What is already configured

Railway already has `OASIS_DNA_JSON`, the existing `STRIPE_*` variables, and `GITHUB_PAT`. Keep them. Do not create another MongoDB connection or another set of Stripe variables. WEB4-WEB10 write `OASIS_DNA_JSON` to `/app/OASIS_DNA.json` and reuse its MongoDB connection.

The ledger creates dedicated collections in the same MongoDB instance. It does not store accounting records in the shared `holons` collection.

| Owner | Default database |
|---|---|
| WEB4 ledger and billing | `oasis_subscription_accounting` |
| WEB5-WEB10 outboxes | `oasis_web5_subscription_outbox` through `oasis_web10_subscription_outbox` |

WEB6 pricing is loaded from `Configuration/web6-model-catalogue.json`. No Railway price variable is required.

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

Telemetry is a separate optional operational feature. Its collector settings are not part of initial ledger activation.
