# Subscription & Billing — Where Everything Is (for review)

Quick map for David (or anyone) to find and review the subscription/credits/usage work.

---

## 1. ONODE API (backend)

**Root:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/`

| What | Path |
|------|------|
| Subscription + Stripe checkout + webhooks | `Controllers/SubscriptionController.cs` |
| Request gating, usage increment, credits path | `Middleware/SubscriptionMiddleware.cs` |
| MongoDB store (subscriptions, credits, usage) | `Services/SubscriptionStoreMongoDb.cs` |
| Store interface | `Interfaces/ISubscriptionStore.cs` |
| Subscription record model | `Models/SubscriptionStore/SubscriptionRecord.cs` |
| Credits balance model | `Models/SubscriptionStore/CreditsBalanceRecord.cs` |
| API usage (request count per month) model | `Models/SubscriptionStore/ApiUsageRecord.cs` |
| OASIS_DNA loaded for MongoDB fallback | `Program.cs` (ConfigureAppConfiguration + SubscriptionStoreMongoDb uses it) |
| Stripe/config notes | `STRIPE_CHECKOUT_SETUP.md`, `appsettings.Development.json.example` |
| appsettings (SubscriptionStore, Credits) | `appsettings.json` |

---

## 2. Data (MongoDB)

**Database:** Same as MongoDBOASIS (e.g. `OASISAPI_DEV`). Connection from `OASIS_DNA.json` → `OASIS.StorageProviders.MongoDBOASIS` unless overridden by `SubscriptionStore:ConnectionString` in appsettings.

| Collection | Purpose |
|------------|---------|
| `subscriptions` | One doc per avatar: plan, status, Stripe ids, period |
| `credits_balance` | One doc per avatar: balance in USD |
| `api_usage` | One doc per avatar per calendar month: request count |

---

## 3. Docs (flow, design, next steps)

| Doc | Path |
|-----|------|
| Flow + next steps + where tables live | `Docs/SUBSCRIPTION_API_FLOW_AND_NEXT_STEPS.md` |
| Credits + subscriptions design | `Docs/OASIS_BILLING_CREDITS_DESIGN.md` |
| This map | `Docs/SUBSCRIPTION_BILLING_WHERE_EVERYTHING_IS.md` |

---

## 4. Pricing/marketing site (oasisweb4.com)

**Repo:** `oasisweb4.com-repo/` (may be separate repo; check with Max.)

- Pricing page (plans + buy credits): `pricing.html`
- Checkout success: `checkout-success.html`
- Local proxy to ONODE: `server.py`

---

## 5. How to run and test

- Start ONODE: from `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI` run `dotnet run`.
- Stripe keys: in `appsettings.Development.json` (gitignored); see `appsettings.Development.json.example`.
- MongoDB: uses MongoDBOASIS from `OASIS_DNA.json` by default; no extra config needed for subscription store.
