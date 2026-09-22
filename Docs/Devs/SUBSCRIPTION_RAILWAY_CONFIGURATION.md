# WEB4–WEB10 subscription ledger: Railway setup

This is the short deployment checklist for the subscription usage ledger. It assumes the current new installation has no users or historical accounting data.

## Confirmed existing Railway production variables

The production Railway environment already exposes these shared variables to all seven WEB4–WEB10 services:

- `GITHUB_PAT`
- `OASIS_DNA_JSON`
- `STRIPE_PUBLISHABLE_KEY`
- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`
- `STRIPE_WEBHOOK_TEST_TOKEN` (currently referenced only by one service)

The Docker entrypoints for WEB4–WEB10 write `OASIS_DNA_JSON` to `/app/OASIS_DNA.json` before starting each service. The ledger therefore receives the same MongoDB configuration as the existing OASIS services. Existing Stripe environment variables take precedence over Stripe values in that DNA file. Do not add another MongoDB URI or another copy of the Stripe keys for the ledger.

The screenshot supplied for this rollout shows only one shared variable in staging. Before staging validation, mirror the applicable DNA/Stripe shared variables into staging using staging values; never point staging at production Stripe or production customer data.

## Existing settings that are reused

Do not create duplicate Railway variables for these when they are already present in `OASIS_DNA_JSON` or the existing Railway shared environment:

| Existing configuration | How the ledger uses it |
|---|---|
| `OASIS.StorageProviders.MongoDBOASIS.ConnectionString` in `OASIS_DNA.json` | WEB4 accounting and WEB5–WEB10 durable outboxes connect to the existing MongoDB deployment. |
| `OASIS.SubscriptionConfig.Stripe.SecretKey` | Checkout, canonical subscription reads and reconciliation. `STRIPE_SECRET_KEY` remains an optional protected override. |
| `OASIS.SubscriptionConfig.Stripe.WebhookSecret` | Verification of Stripe webhook signatures. `STRIPE_WEBHOOK_SECRET` remains an optional protected override. |
| `OASIS.SubscriptionConfig.Stripe.PriceBronze`, `PriceSilver`, `PriceGold`, `PriceEnterprise` | Existing Stripe price IDs. The corresponding `STRIPE_PRICE_*` variables remain optional overrides. |

The ledger does not use the shared `holons` collection. It creates dedicated collections in the `oasis_subscription_accounting` database on the existing MongoDB deployment. Each consuming service uses a fixed outbox database:

| Service | Default outbox database |
|---|---|
| WEB5 | `oasis_web5_subscription_outbox` |
| WEB6 | `oasis_web6_subscription_outbox` |
| WEB7 | `oasis_web7_subscription_outbox` |
| WEB8 | `oasis_web8_subscription_outbox` |
| WEB9 | `oasis_web9_subscription_outbox` |
| WEB10 | `oasis_web10_subscription_outbox` |

`SUBSCRIPTION_MONGODB_CONNECTION_STRING`, `SUBSCRIPTION_MONGODB_DATABASE`, `SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING` and `SUBSCRIPTION_OUTBOX_DATABASE` are optional overrides for deployments that intentionally isolate those stores. They are not required for the normal shared-cluster layout.

## New variables required for launch

### 1. Internal service credentials

Generate six different random secrets of at least 32 bytes. Each secret is shared only between WEB4 and its named consuming service.

| Railway service | Variables to add |
|---|---|
| WEB4 | `SUBSCRIPTION_SERVICE_KEY_WEB5`, `SUBSCRIPTION_SERVICE_KEY_WEB6`, `SUBSCRIPTION_SERVICE_KEY_WEB7`, `SUBSCRIPTION_SERVICE_KEY_WEB8`, `SUBSCRIPTION_SERVICE_KEY_WEB9`, `SUBSCRIPTION_SERVICE_KEY_WEB10` |
| WEB5 | `SUBSCRIPTION_SERVICE_KEY_WEB5` with the same WEB5 value stored on WEB4 |
| WEB6 | `SUBSCRIPTION_SERVICE_KEY_WEB6` with the same WEB6 value stored on WEB4 |
| WEB7 | `SUBSCRIPTION_SERVICE_KEY_WEB7` with the same WEB7 value stored on WEB4 |
| WEB8 | `SUBSCRIPTION_SERVICE_KEY_WEB8` with the same WEB8 value stored on WEB4 |
| WEB9 | `SUBSCRIPTION_SERVICE_KEY_WEB9` with the same WEB9 value stored on WEB4 |
| WEB10 | `SUBSCRIPTION_SERVICE_KEY_WEB10` with the same WEB10 value stored on WEB4 |

These credentials prevent another service or client from submitting fabricated usage to WEB4. Never reuse one service's value for another service.

### 2. WEB4 address

Add `WEB4_API_BASE_URL` to WEB5, WEB6, WEB7, WEB8, WEB9 and WEB10. Use the HTTPS base URL of the WEB4 service in that Railway environment.

This tells each consuming service where to reserve and settle usage. Staging services must point to staging WEB4; production services must point to production WEB4.

### 3. WEB6 prices

No Railway price variable is required. WEB6 stores its provider/model prices in the deployed `Configuration/web6-model-catalogue.json`; `ModelCatalogueManager` validates and loads it, and subscription metering derives safe request reservations from it. The catalogue is separate from Stripe plan prices: Stripe charges the customer for a plan, while WEB6 accounts for measured AI-provider usage. `WEB6_MODEL_CATALOGUE_PATH` is optional when deliberately mounting a different reviewed file.

### 4. First launch only

Add this to WEB4:

```text
SUBSCRIPTION_LEDGER_INITIAL_STATE=empty-new-installation
```

On the first transactional ledger write, WEB4 checks that every ledger, billing and legacy source collection is empty. It records a durable initialization marker only when that check succeeds. If any record exists, startup accounting fails closed instead of silently treating existing usage as zero.

After the marker exists, remove `SUBSCRIPTION_LEDGER_INITIAL_STATE` from WEB4 and redeploy. Do not use this setting for an installation that already has users or accounting records.

## Optional operational settings

These are useful when their corresponding feature is enabled, but they are not required merely to connect the ledger:

| Variable | When it is needed |
|---|---|
| `SUBSCRIPTION_ADMIN_AVATAR_IDS` on WEB4 | Administrative corrections, reconciliation imports or audited dead-letter recovery. The JWT must also carry the administrator claim. |
| `SUBSCRIPTION_OTLP_METRICS_ENDPOINT` and `SUBSCRIPTION_OTLP_HEADERS` | Exporting subscription metrics to the deployed collector. |
| `SUBSCRIPTION_MIGRATION_APPROVAL_KEY` | Only for an installation with existing users/accounting data that needs a signed opening-state import. It is not needed for this new empty installation. |
| Stripe environment variables | Only when intentionally overriding the existing Stripe values from OASIS DNA. |
| MongoDB environment variables | Only when intentionally using a different cluster or database from the documented defaults. |

## Deployment check

1. Confirm `OASIS_DNA_JSON` contains the intended MongoDB value and the existing Stripe shared variables target the intended environment, without printing their values in logs.
2. Add the six paired service credentials, `WEB4_API_BASE_URL` and the one-time empty-installation setting.
3. Deploy WEB4 first, followed by WEB5–WEB10.
4. Run one test subscription and one request through each enabled service using stable idempotency keys.
5. Verify one WEB4 authorization, one settlement, one immutable audit trail and one outbox completion for each request.
6. Confirm the `v1-opening-balance` marker has `InitializationMode=empty-new-installation`, then remove the first-launch setting.
7. Run Stripe test checkout/webhook and reconciliation using the existing Stripe configuration before enabling paid traffic.

Never paste secrets into source control, pull requests, test reports or chat. Configure them as protected Railway variables or protected OASIS DNA values.
