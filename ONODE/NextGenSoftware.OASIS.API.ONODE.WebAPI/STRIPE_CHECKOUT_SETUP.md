# Stripe Checkout Setup (Subscription / Pricing)

When a user clicks **Choose Silver** (or any plan) on the pricing page, the flow is:

1. **Frontend** (`pricing.html`) → `POST {API_BASE}/subscription/checkout/session` with `planId`, `successUrl`, `cancelUrl`, `customerEmail`.
2. **ONODE** `SubscriptionController.CreateCheckoutSession` → checks `STRIPE_PUBLISHABLE_KEY` and `STRIPE_SECRET_KEY` → creates a Stripe Checkout Session → returns `SessionUrl`.
3. **Frontend** → redirects the user to `SessionUrl` (Stripe’s hosted checkout).

If you see **“Stripe keys not configured. Set STRIPE_PUBLISHABLE_KEY and STRIPE_SECRET_KEY”**, the API is running but has no Stripe keys.

---

## 1. Get Stripe keys (test mode)

1. Go to [Stripe Dashboard](https://dashboard.stripe.com) and sign in.
2. Turn **Test mode** on (toggle in the sidebar).
3. **Developers** → **API keys**.
4. Copy:
   - **Publishable key** → `pk_test_...` → use as `STRIPE_PUBLISHABLE_KEY`.
   - **Secret key** → `sk_test_...` → use as `STRIPE_SECRET_KEY`.

(Optional) **Webhooks** → **Add endpoint** → later use **Signing secret** `whsec_...` as `STRIPE_WEBHOOK_SECRET` for `POST /api/subscription/webhooks/stripe`.

---

## 2. Configure ONODE locally

Use **one** of these.

### Option A: Environment variables (good for terminal)

Before starting ONODE:

```bash
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI

export STRIPE_PUBLISHABLE_KEY="pk_test_your_key_here"
export STRIPE_SECRET_KEY="sk_test_your_key_here"

dotnet run
```

On Windows (PowerShell):

```powershell
$env:STRIPE_PUBLISHABLE_KEY = "pk_test_your_key_here"
$env:STRIPE_SECRET_KEY = "sk_test_your_key_here"
dotnet run
```

### Option B: `appsettings.Development.json` (good for IDE)

1. Copy the example file (do **not** commit the copy):

   ```bash
   cp appsettings.Development.json.example appsettings.Development.json
   ```

2. Edit `appsettings.Development.json` and replace the placeholders with your test keys:

   ```json
   {
     "STRIPE_PUBLISHABLE_KEY": "pk_test_xxxxxxxx",
     "STRIPE_SECRET_KEY": "sk_test_xxxxxxxx",
     "STRIPE_WEBHOOK_SECRET": "whsec_xxxxxxxx"
   }
   ```

3. Run with `ASPNETCORE_ENVIRONMENT=Development` (or use the “Development” profile in your IDE).  
   `appsettings.Development.json` is in `.gitignore`; keep it that way so real keys are never committed.

---

## 3. Restart ONODE and test

1. Restart the ONODE API so it loads the new config.
2. In the browser, open the pricing page (e.g. `http://localhost:8080/pricing.html`).
3. Click **Choose Silver** (or any non-Enterprise plan).

You should be redirected to Stripe Checkout. Use test card `4242 4242 4242 4242`, any future expiry, any CVC, and any billing details.

---

## 4. Where the keys are used in code

| Key                      | Used in |
|--------------------------|--------|
| `STRIPE_PUBLISHABLE_KEY` | Checked in `CreateCheckoutSession`; can be sent to the frontend if you ever need it for Stripe.js. |
| `STRIPE_SECRET_KEY`      | `SubscriptionController.CreateStripeCheckoutSessionAsync` sets `StripeConfiguration.ApiKey` and calls Stripe API. |
| `STRIPE_WEBHOOK_SECRET`  | `StripeWebhook()` when verifying `Stripe-Signature` and processing events. |

All are read via `_configuration["..."]`, so they work from env vars or from `appsettings*.json`.
