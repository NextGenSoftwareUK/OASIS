# Subscription API: How It Works & What Comes Next

Walkthrough of how the subscription/credits layer connects to the actual OASIS API, and what to do now that Stripe payments work.

---

## 1. End-to-end flow (today)

### Payments (working)

```
User on pricing page
  → Clicks "Choose Silver" or "Buy credits" ($50)
  → Frontend: POST /api/subscription/checkout/session (plan) or POST /api/subscription/checkout/credits (amount)
  → ONODE SubscriptionController: creates Stripe Checkout Session (subscription or one-time payment)
  → User pays on Stripe
  → Stripe sends webhook: POST /api/subscription/webhooks/stripe
  → SubscriptionController.HandleStripeEventAsync:
       • Subscription: UpdateUserSubscriptionAsync(...)  ← currently just logs / TODO
       • Credits: adds amount to CreditsBalanceByAvatar[avatar_id]  ← in-memory, works
```

So: **Stripe and checkout work.** Subscription webhook runs but doesn’t persist anywhere yet. Credits webhook **does** persist (in-memory) per avatar.

### How the actual API uses it (request-by-request)

Every API request (except skipped paths) goes through **SubscriptionMiddleware** (registered in Startup after JWT):

```
Request hits ONODE (e.g. POST /api/nft/mint-nft)
  → JwtMiddleware: validates token, sets context.Items["Avatar"]
  → SubscriptionMiddleware.InvokeAsync:
      1. ShouldSkipSubscriptionCheck(path) → true for /api/health, /api/auth, /api/avatar/signin|signup, /api/subscription, etc.
         → if skipped: call next(), request continues (no subscription check)
      2. avatar = context.Items["Avatar"]
         → if null: call next() (no subscription check today)
      3. GetSubscriptionInfo(avatar.Id)
         → TODAY: returns mock (PlanId = "silver", Status = "active", ...)  ← not from DB or Stripe
      4. IsSubscriptionActive(subscriptionInfo) → true for mock
      5. GetCurrentMonthUsage(avatar.Id)
         → TODAY: returns mock 50000  ← not real usage
      6. currentUsage >= planLimit?
         → if over and PayAsYouGoEnabled: LogOverageUsage, then next()
         → if over and not PayAsYouGo: return 429 Limit Exceeded
      7. IncrementUsageCounter(avatar.Id)
         → TODAY: just logs  ← not persisted
      8. next() → request reaches your actual controller (e.g. NftController)
```

So today:

- **Access**: Middleware only “sees” mock subscription (everyone with an avatar looks like Silver). It does **not** yet:
  - Read real subscription from DB/Stripe.
  - Check **credits** balance or allow “credits-only” access.
- **Usage**: GetCurrentMonthUsage and IncrementUsageCounter are not persisted; limits and overage are not real.

**Summary:** Payments and credits balance (in-memory) work. The **gate** in front of the API (SubscriptionMiddleware) still uses mock subscription and mock usage; it does not yet use Stripe/subscription persistence or credits.

---

## 2. How the subscription API is *meant* to work with the actual API

Intended behavior:

1. **Who can call the API**
   - User has a **valid subscription** (active plan, current period), **or**
   - User has **credits balance > 0** (and we deduct per request).
   - Otherwise → 402 Subscription/Credits Required.

2. **Subscriptions**
   - Stripe webhook → persist “this avatar has this plan, this period, this Stripe customer/subscription id.”
   - Middleware: GetSubscriptionInfo(avatarId) reads from that store (DB).
   - Middleware: GetCurrentMonthUsage(avatarId) and IncrementUsageCounter(avatarId) read/write real usage (DB).
   - Over limit → 429 or pay-as-you-go (if enabled), using real usage.

3. **Credits**
   - Stripe webhook (one-time payment, type=credits) → add to avatar’s credit balance (already done in memory; next: persist in DB).
   - Middleware: if no subscription, check credits balance; if > 0, allow request and deduct cost per request; if balance would go negative → 402.
   - Cost per request: e.g. configurable “Credits:CostPerRequestUsd”.

So: **subscription API** = “who is allowed to call the API and how much have they used?” It works with the actual API by sitting in front of it (SubscriptionMiddleware) and allowing or denying the request before it hits your controllers.

---

## 3. Where the tables live (MongoDB)

Subscription, credits, and usage data are stored in **MongoDB** (same DB as MongoDBOASIS by default):

- **Config:** If you set `SubscriptionStore:ConnectionString` and optionally `SubscriptionStore:DatabaseName` in appsettings, that is used. **Otherwise the subscription store uses the same MongoDB as MongoDBOASIS** from `OASIS_DNA.json` (`OASIS:StorageProviders:MongoDBOASIS:ConnectionString` and `DBName`). So with the default ONODE setup (OASIS_DNA.json in place), no extra config is needed—subscription/credits/usage collections live in the same database (e.g. `OASISAPI_DEV`). If no connection is available, the store is “not configured” and the API uses in-memory/mock behavior.
- **Collections:** `subscriptions` (one doc per avatar), `credits_balance` (one doc per avatar), `api_usage` (one doc per avatar per month). See `SubscriptionStoreMongoDb` and `Models/SubscriptionStore/`.
- **Credits cost:** `Credits:CostPerRequestUsd` (default 0.001). Middleware deducts this when the user has no subscription but has credits.

## 4. What comes next (in order)

Steps 1–4 below are **implemented** (MongoDB store, middleware wired, credits path, webhooks). Optional: portal UI, auto-reload.

### Step 1: Persist subscription and credits (DB) — done

- **Subscriptions:** When Stripe webhook fires for subscription events, write to a table (e.g. `avatar_id`, `plan_id`, `status`, `stripe_customer_id`, `stripe_subscription_id`, `current_period_start`, `current_period_end`). Read it in GetSubscriptionInfo.
- **Credits:** Replace in-memory `CreditsBalanceByAvatar` with a table (e.g. `avatar_id`, `balance_usd`, `updated_at`). On credits webhook, add to balance (or insert row). Optionally: `credit_transactions` (avatar_id, amount, type=purchase|deduction, created_at) for history.
- **Usage (for subscriptions):** Table (e.g. `avatar_id`, `period_start`, `request_count`). Increment in IncrementUsageCounter; read in GetCurrentMonthUsage.

Until this is done, the API will not enforce real limits or real “no subscription” vs “has subscription” vs “has credits.”

### Step 2: Wire SubscriptionMiddleware to real data

- **GetSubscriptionInfo(avatarId):** Query your new subscription store by avatar_id. Return null if no row (or inactive). Return plan_id, status, period, PayAsYouGoEnabled from DB (or from Stripe if you prefer to mirror Stripe as source of truth).
- **GetCurrentMonthUsage(avatarId):** Query usage table for this avatar and current month. Return count.
- **IncrementUsageCounter(avatarId):** Increment (or upsert) the current month’s count for this avatar.

After this, subscription limits and overage (and 429/402) are real.

### Step 3: Add credits path in SubscriptionMiddleware

- If GetSubscriptionInfo returns null (no subscription), **before** returning 402:
  - Read credits balance for avatar_id from DB (same store you added in Step 1).
  - If balance > 0: allow request, then **deduct** cost per request (e.g. Config["Credits:CostPerRequestUsd"] or fixed 0.001). If deduction would make balance < 0, return 402 “Insufficient credits.”
  - If balance <= 0: then return 402 Subscription/Credits Required.

Optional: add a small “usage” or “deduction” log (e.g. credit_transactions with type=deduction) so you have an audit trail.

### Step 4: Webhook persistence (finish the loop)

- In SubscriptionController, replace the TODOs:
  - **UpdateUserSubscriptionAsync:** Upsert into your subscription table (from Stripe subscription/create/update events and checkout.session.completed for mode=subscription).
  - **UpdateSubscriptionStatusAsync:** Update status and period in DB when you receive customer.subscription.updated/deleted or invoice.payment_succeeded/failed.
- For credits you already update balance in HandleCheckoutSessionCompletedAsync; once balance is in DB, that handler should update the DB instead of (or in addition to) in-memory.

After this, Stripe → DB and DB → middleware are fully connected.

### Step 5: (Optional) Portal / billing UI

- **Balance:** GET /api/subscription/balance already returns credits (and can be extended with subscription summary). Use it in the portal to show “Credits: $X” and “Plan: Silver (renews …)”.
- **Usage:** GET /api/subscription/usage already exists; wire it to your real usage store so the portal shows “X / Y requests this month.”
- **Auto-reload:** Design doc (OASIS_BILLING_CREDITS_DESIGN.md) describes GET/PUT /api/subscription/auto-reload; implement when you want that feature.

---

## 4. One-paragraph summary

**Flow:** User pays on Stripe (subscription or credits) → webhook hits ONODE → subscription/credits state should be stored in DB. On every API request, SubscriptionMiddleware runs after JWT, calls GetSubscriptionInfo (and, for credits, balance), GetCurrentMonthUsage, and IncrementUsageCounter. Today those are mocks, so the API doesn’t actually enforce limits or “subscription/credits required.” **What comes next:** (1) Persist subscriptions, credits balance, and usage in a DB; (2) Wire the middleware to that DB; (3) Add the “credits-only” path in the middleware (allow + deduct); (4) Make the webhandlers write to the same DB. Then the subscription API and your actual API are fully connected.
