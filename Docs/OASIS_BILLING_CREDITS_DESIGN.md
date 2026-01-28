# OASIS Billing: Anthropic-Style Credits + Subscriptions

Design for an easy-to-use billing system that offers **prepaid credits** (like [Anthropic's API billing](https://support.anthropic.com/en/articles/8977456-how-do-i-pay-for-my-claude-api-usage)) plus optional **monthly subscriptions**, so users can choose "pay for credits" or "subscribe per month."

---

## 1. How Stripe Connects to the Subscription API Today

### Current flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  PRICING PAGE (oasisweb4.com/pricing.html)                                        │
│  • GET /api/subscription/plans  →  Loads Bronze/Silver/Gold/Enterprise           │
│  • User clicks "Choose Silver"                                                    │
│  • POST /api/subscription/checkout/session  { planId, successUrl, cancelUrl }     │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  ONODE SubscriptionController                                                     │
│  • CreateCheckoutSession(planId)                                                  │
│  • Stripe Checkout Session, mode: "subscription"                                   │
│  • GetOrCreateStripePriceAsync(plan) → monthly recurring price                     │
│  • Returns SessionUrl → frontend redirects to Stripe Checkout                     │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  STRIPE                                                                          │
│  • Customer pays → subscription created                                          │
│  • Webhook: checkout.session.completed, customer.subscription.*, invoice.*        │
│  • POST /api/subscription/webhooks/stripe  (handled by SubscriptionController)   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  SUBSCRIPTION LAYER (today: mostly mock)                                          │
│  • UpdateUserSubscriptionAsync(userId, planId, customerId, subscriptionId)       │
│  • SubscriptionMiddleware: GetSubscriptionInfo(avatarId) → plan, status, limits │
│  • Usage: GetCurrentMonthUsage(avatarId), IncrementUsageCounter(avatarId)         │
│  • Pay-as-you-go overage: LogOverageUsage when over limit & PayAsYouGoEnabled    │
└─────────────────────────────────────────────────────────────────────────────────┘
```

**Where it lives**

| Piece | Location |
|-------|----------|
| Plans + checkout session | `ONODE/.../Controllers/SubscriptionController.cs` |
| Stripe config | `appsettings.Development.json` → `STRIPE_*` keys |
| Webhook handler | `SubscriptionController.HandleStripeEventAsync` |
| Access control & usage | `ONODE/.../Middleware/SubscriptionMiddleware.cs` |
| Frontend | `oasisweb4.com-repo/pricing.html` |

So: **Stripe** drives *payment*; **SubscriptionController** creates sessions and reacts to webhooks; **SubscriptionMiddleware** (and a future persistence layer) decide *who can call the API* and *how much they’ve used*. Today subscription/usage state is mostly in-memory/mock.

---

## 2. Anthropic’s Model (What to Emulate)

From [Anthropic’s help](https://support.anthropic.com/en/articles/8977456-how-do-i-pay-for-my-claude-api-usage)) and [pricing](https://www.anthropic.com/pricing):

- **Prepaid usage credits** – Buy credits before use; API usage consumes them at published rates (e.g. per token).
- **No subscription required** – Users can use the API with credits only.
- **Buy credits** – One-time purchase; credits available immediately.
- **Auto-reload** – Optional: when balance &lt; threshold, automatically buy a set amount (user configurable).
- **Billing UI** – [Settings → Billing](https://platform.claude.com/settings/billing): balance, “Buy credits,” auto-reload, payment method.
- **API keys** – [Settings → API keys](https://platform.claude.com/settings/keys); separate from billing.
- **Credits** – Expire 1 year from purchase; non-refundable; failed requests not charged.

So Anthropic is **credits-first**, with **optional** auto-reload, and **optional** higher-tier/subscription products for power users.

---

## 3. Proposed OASIS Model: Credits + Optional Subscriptions

### Principles

1. **Credits-first** – Any user can “Buy credits” and use the API without a monthly subscription.
2. **Subscriptions optional** – Keep Bronze/Silver/Gold/Enterprise for teams or users who want predictable monthly caps + overage.
3. **Single entry point** – One billing/pricing surface: “Buy credits” and “Subscribe” both visible and easy.

### Two ways to pay

| Option | How it works | Best for |
|--------|----------------|----------|
| **Credits** | Prepaid balance in USD (or “credit units”). Each API call deducts N units (or $X). Balance tracked per avatar/org. | Occasional use, try-before-commit, variable workload. |
| **Subscription** | Monthly plan (Bronze/Silver/Gold) with included requests + optional pay-as-you-go overage. | Steady usage, predictable monthly spend. |

### Credits system (new)

- **Balance** – Stored per avatar (and later per org): `CreditBalanceUSD` or `CreditUnits`.
- **Rates** – Configurable cost per API call (or per request-type). E.g. 1 request = $0.001 or 1 unit.
- **Buy credits** – One-time Stripe Checkout (**payment** mode, not subscription). Presets: e.g. $20, $50, $100, $250, or custom.
- **Webhook** – On `checkout.session.completed` with `mode == "payment"` and metadata `type == "credits"`, add purchased amount to avatar’s balance.
- **Auto-reload** (optional) – User setting: “When balance &lt; $X, buy $Y.” When balance dips below X, either auto-create a “buy credits” session (with stored payment method) or prompt “Top up now” and redirect to Checkout.
- **Expiry** – Optional: credits expire 12 months from purchase (Anthropic-style); can start without expiry and add later.

### Subscriptions (existing, clarified)

- Unchanged: monthly plans, Stripe subscriptions, webhooks for `customer.subscription.*` and `invoice.*`.
- SubscriptionMiddleware already has plan limits and pay-as-you-go overage.
- **Access rule:**  
  - If user has **any** valid subscription → use plan limits + overage.  
  - Else if user has **credits balance > 0** → allow API calls and deduct from credits.  
  - Else → subscription/credits required (today’s “subscription required” logic).

---

## 4. API Design for Credits

All under the same base path as today (e.g. `/api/subscription/` or `/api/billing/`).

### 4.1 Get balance and billing summary

```http
GET /api/subscription/balance
Authorization: Bearer <jwt>
```

Response (concept):

```json
{
  "result": {
    "creditsBalanceUsd": 47.50,
    "currency": "USD",
    "subscription": {
      "planId": "silver",
      "status": "active",
      "currentPeriodEnd": "2025-02-28T00:00:00Z"
    },
    "autoReload": {
      "enabled": true,
      "triggerBelowUsd": 10,
      "topUpUsd": 50
    }
  },
  "isError": false
}
```

If no subscription, `subscription` is null. If no auto-reload, `autoReload.enabled` is false.

### 4.2 Create “Buy credits” checkout session (one-time payment)

```http
POST /api/subscription/checkout/credits
Content-Type: application/json
Authorization: Bearer <jwt>

{
  "amountUsd": 50,
  "successUrl": "https://oasisweb4.com/checkout-success.html?type=credits",
  "cancelUrl": "https://oasisweb4.com/pricing.html"
}
```

`amountUsd` must be in an allowed set (e.g. 20, 50, 100, 250) or a configured range (e.g. min 10, max 5000).

Response (concept):

```json
{
  "sessionUrl": "https://checkout.stripe.com/...",
  "sessionId": "cs_...",
  "isError": false
}
```

Backend creates a Stripe Checkout Session with:

- `mode: "payment"`
- One-time Price or inline amount: `amountUsd` cents
- `metadata.avatar_id`, `metadata.type = "credits"`, `metadata.amount_usd`
- Success/cancel URLs from body

### 4.3 Optional: credit usage history

```http
GET /api/subscription/usage/credits?from=2025-01-01&to=2025-01-31
Authorization: Bearer <jwt>
```

Returns a list of deductions (timestamp, amount, endpoint/operation) for that period. Can be added in a later phase.

### 4.4 Auto-reload settings

```http
GET  /api/subscription/auto-reload
PUT  /api/subscription/auto-reload
Body: { "enabled": true, "triggerBelowUsd": 10, "topUpUsd": 50 }
Authorization: Bearer <jwt>
```

Used by the billing UI to show and edit “when balance &lt; X, top up by Y.”

---

## 5. Stripe Side for Credits

### 5.1 One-time “Buy credits” session

- **Mode:** `payment` (one-time), not `subscription`.
- **Line item:** Single item “OASIS API Credits – $50” (or whatever amount), `amount_total` = `amountUsd * 100` (cents).
- **Metadata:** `avatar_id`, `type = "credits"`, `amount_usd`.
- **Success URL:** e.g. `.../checkout-success.html?type=credits&session_id={CHECKOUT_SESSION_ID}`.

No recurring Price/Product needed for credits; you can use a generic “OASIS Credits” product and one-off PaymentIntents, or create a Price per preset (e.g. $20, $50, $100) and re-use them.

### 5.2 Webhook handling

In `HandleStripeEventAsync` (or equivalent):

- **`checkout.session.completed`**
  - If `session.Mode == "payment"` and `metadata["type"] == "credits"`:
    - Read `metadata["avatar_id"]`, `metadata["amount_usd"]`.
    - Add `amount_usd` to that avatar’s credit balance (in DB or wherever balance is stored).
  - If `session.Mode == "subscription"`: keep current logic (link subscription to user, set plan, etc.).

No change to `customer.subscription.*` or `invoice.*` handling; those stay for subscriptions.

---

## 6. Middleware and Access Rules

Extend **SubscriptionMiddleware** (or equivalent) so that “has access” can come from **either** subscription **or** credits:

1. Resolve `avatarId` (e.g. from JWT / session).
2. **If** avatar has an active subscription (existing logic):
   - Apply plan limits and overage as today.
   - Optionally: still allow using credits for overage instead of charging card (if you add that product choice later).
3. **Else if** avatar has `creditsBalanceUsd > 0` (or balance in smallest unit):
   - Allow the request.
   - After the request, decrement balance by the cost of that call (e.g. fixed “cost per request” or by endpoint).
   - If balance would go negative, reject with 402 “Insufficient credits” and return `credits_required` or similar.
4. **Else**:
   - Same as today: 402 “Subscription or credits required” with link to pricing/billing.

Cost per call can be a constant for now (e.g. $0.001 per request), or a small table by route; later map to “credit units” and exchange rate.

---

## 7. Frontend (Billing / Pricing) – Easy to Use

### 7.1 Single billing entry (Anthropic-style)

- **Pricing/billing page** (or **Settings → Billing** when logged in):
  - **Credits**
    - Current balance: “$47.50 credits”
    - **Buy credits** with presets: **$20** | **$50** | **$100** | **$250** and optionally “Custom amount.”
    - Optional: “Auto-reload when balance &lt; $ ___ , add $ ___ .”
  - **Subscriptions** (existing)
    - “Or choose a monthly plan” with Bronze/Silver/Gold/Enterprise and “Choose Silver” etc.

So the **first** thing users see is “credits” and “buy credits,” and **then** “or subscribe monthly.”

### 7.2 Buy-credits flow

1. User selects preset ($50) or enters custom amount.
2. Frontend calls `POST /api/subscription/checkout/credits` with `amountUsd: 50`, `successUrl`, `cancelUrl`.
3. Backend returns `sessionUrl`; frontend does `window.location.href = sessionUrl`.
4. User pays on Stripe; redirect to `successUrl?type=credits`.
5. Success page shows “Credits added. Your balance is now $X.” Optional: poll `GET /api/subscription/balance` a few times to show updated balance.

### 7.3 Balance and API keys (Anthropic-style)

- **Billing** – Balance, buy credits, auto-reload, payment method, and (optionally) “Manage subscription.”
- **API keys** – Separate UI/section (e.g. “Settings → API keys”) for creating/revoking keys; no need to mix billing and keys on the same screen, but both under “Settings” or “Account” is fine.

---

## 8. Data and Config

### 8.1 What to store (per avatar or per org)

- `credits_balance_usd` (or units and exchange rate).
- `auto_reload_enabled`, `auto_reload_trigger_below_usd`, `auto_reload_top_up_usd`.
- Optional: `credit_transactions` (purchases and deductions) for history and auditing.

Subscription/plan state stays as today (or in DB when you implement it).

### 8.2 Config (e.g. appsettings or env)

- `Credits:CostPerRequestUsd` (e.g. 0.001).
- `Credits:PresetAmountsUsd`: [ 20, 50, 100, 250 ].
- `Credits:MinCustomUsd`, `Credits:MaxCustomUsd` (e.g. 10, 5000).
- Optional: `Credits:ExpiryMonths` (e.g. 12); if 0, no expiry.

---

## 9. Implementation Order

1. **Credits balance and “Buy credits” API**
   - Schema or in-memory store for `credits_balance_usd` per avatar.
   - `POST /api/subscription/checkout/credits` creating Stripe one-time session.
   - Webhook: on `checkout.session.completed` for `type=credits`, add to balance.
2. **Middleware**
   - Allow access when `credits_balance_usd > 0`; deduct per request; 402 when insufficient.
3. **Balance and presets**
   - `GET /api/subscription/balance`; configurable presets and min/max for custom amount.
4. **Pricing/billing UI**
   - “Buy credits” presets + optional custom; link from pricing page to this flow.
   - Success page for `?type=credits` and, if desired, balance polling.
5. **Auto-reload**
   - Settings (GET/PUT `/api/subscription/auto-reload`); background or on-next-request check “balance &lt; trigger → create top-up session or notify user.”
6. **Optional**
   - Credit usage history, expiry, and “use credits for overage” on top of subscriptions.

---

## 10. References

- [Anthropic – How do I pay for my Claude API usage?](https://support.anthropic.com/en/articles/8977456-how-do-i-pay-for-my-claude-api-usage) – Prepaid credits, buy credits, auto-reload.
- [Anthropic Pricing](https://www.anthropic.com/pricing) – API pricing, tokens, tools.
- [Stripe Checkout – One-time payments](https://stripe.com/docs/payments/checkout/one-time) – `mode: "payment"` for credits.
- Existing OASIS: `SubscriptionController`, `SubscriptionMiddleware`, `oasisweb4.com-repo/pricing.html`.
