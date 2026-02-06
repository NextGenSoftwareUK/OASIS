# Q2P Railway – Variables (CORS & secrets)

Use these in each service’s **Variables** in Railway. Replace placeholders with your real URLs and secrets.

---

## Medusa service

**Where to set:** Medusa service → Variables

| Variable | Example / where to get it |
|----------|---------------------------|
| `DATABASE_URL` | **Must use Railway’s internal URL** so Medusa can connect. In Medusa service → Variables → **Add a variable** → choose **“Add a reference”** (or “From another service”) → select your **PostgreSQL** service → pick the variable Railway exposes (e.g. `DATABASE_URL` or `POSTGRES_PRIVATE_URL`). Do **not** paste the public URL from the Connect tab; the container needs the internal host (e.g. `postgres.railway.internal`) or you’ll get `Pg connection failed` / `KnexTimeoutError`. |
| `JWT_SECRET` | Any long random string, e.g. `q2p-jwt-secret-change-me-32chars-minimum` |
| `COOKIE_SECRET` | Any long random string, e.g. `q2p-cookie-secret-change-me-32chars-min` |
| `STORE_CORS` | Your **storefront** public URL, e.g. `https://q2p-storefront-production-xxxx.up.railway.app` |
| `ADMIN_CORS` | Your **Medusa** public URL, e.g. `https://q2p-medusa-production-xxxx.up.railway.app` |
| `AUTH_CORS` | Same value as `ADMIN_CORS` |
| `STRIPE_API_KEY` | Same as your OASIS subscription API **secret** key: `sk_test_...` or `sk_live_...` (see below). |
| `STRIPE_WEBHOOK_SECRET` | Webhook signing secret for **Medusa’s** Stripe webhook: `whsec_...` (see below). |

### Stripe keys (same Stripe account as OASIS subscription)

- **STRIPE_API_KEY (Medusa)**  
  Use the same **secret key** you use for the OASIS subscription API:
  - From **Stripe Dashboard** → Developers → API keys → Secret key, or  
  - From your ONODE config: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/appsettings.Development.json` (or your production config) → copy the value of **`STRIPE_SECRET_KEY`** into Railway as **`STRIPE_API_KEY`** for Medusa.

- **STRIPE_WEBHOOK_SECRET (Medusa)**  
  This must be the signing secret for a webhook that points at **Medusa**, not ONODE:
  1. Stripe Dashboard → Developers → Webhooks → Add endpoint.
  2. URL: `https://YOUR-MEDUSA-URL.up.railway.app/store/webhooks/stripe` (or Medusa’s store webhook path per Medusa v2 docs).
  3. Select the events Medusa needs (e.g. `checkout.session.completed`, `payment_intent.succeeded`).
  4. Copy the **Signing secret** (`whsec_...`) into Railway as **`STRIPE_WEBHOOK_SECRET`** for Medusa.

If you haven’t set up a webhook for Medusa yet, leave `STRIPE_WEBHOOK_SECRET` empty until the endpoint is created; you can add it later and redeploy.

---

## Storefront service

**Where to set:** Storefront service → Variables

| Variable | Example / where to get it |
|----------|---------------------------|
| `NEXT_PUBLIC_MEDUSA_BACKEND_API` | Your **Medusa** public URL, no trailing slash, e.g. `https://q2p-medusa-production-xxxx.up.railway.app` |
| `MEDUSA_API_KEY` | Publishable API key from Medusa (after running `pnpm run seed` in Medusa – get from Medusa Admin or API). |
| `SITE_NAME` | e.g. `Q2P Shop` |

Optional (OASIS / branding):

- `NEXT_PUBLIC_OASIS_ONODE_URL` – ONODE API URL if you use quest proofs.
- `MEDUSA_REVALIDATION_SECRET` – e.g. a random string for revalidation.
- `TWITTER_CREATOR`, `TWITTER_SITE` – for meta tags.

---

## Copy-paste summary (replace placeholders)

**Medusa**

```bash
DATABASE_URL=<from Postgres service>
JWT_SECRET=q2p-jwt-secret-change-me-32chars-minimum
COOKIE_SECRET=q2p-cookie-secret-change-me-32chars-min
STORE_CORS=https://YOUR-STOREFRONT-URL.up.railway.app
ADMIN_CORS=https://YOUR-MEDUSA-URL.up.railway.app
AUTH_CORS=https://YOUR-MEDUSA-URL.up.railway.app
STRIPE_API_KEY=<same as STRIPE_SECRET_KEY from OASIS/ONODE config>
STRIPE_WEBHOOK_SECRET=whsec_<from Stripe webhook for Medusa>
```

**Storefront**

```bash
NEXT_PUBLIC_MEDUSA_BACKEND_API=https://YOUR-MEDUSA-URL.up.railway.app
MEDUSA_API_KEY=<from Medusa after seed>
SITE_NAME=Q2P Shop
```

---

## Where your Stripe keys live (OASIS subscription)

- **ONODE (OASIS subscription API)** reads: `STRIPE_PUBLISHABLE_KEY`, `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET` from:
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/appsettings.Development.json` (or your environment).
- **Medusa** uses:
  - `STRIPE_API_KEY` = same value as ONODE’s **STRIPE_SECRET_KEY**
  - `STRIPE_WEBHOOK_SECRET` = signing secret for a webhook pointing at **Medusa**, not ONODE (create in Stripe Dashboard if needed).

So: reuse the **secret** key from your OASIS subscription setup for Medusa’s `STRIPE_API_KEY`; get a **new** webhook signing secret for Medusa’s store URL and use it as `STRIPE_WEBHOOK_SECRET`.

---

## Troubleshooting: “Pg connection failed” / KnexTimeoutError

If Medusa deploy logs show:

- `Pg connection failed to connect to the database. Retrying...`
- `KnexTimeoutError: Timeout acquiring a connection`

then the **Medusa container cannot reach Postgres**. Fix it by:

1. **Use the internal database URL**
   - In Railway, open the **Medusa** service.
   - Open **Variables**.
   - Remove any manually pasted `DATABASE_URL` (e.g. the public `interchange.proxy.rlwy.net` URL).
   - Add a **reference** from the **PostgreSQL** service:
     - “New variable” / “Add variable” → “Reference” or “From service”.
     - Select your **PostgreSQL** service.
     - Choose the variable (often `DATABASE_URL` or `POSTGRES_PRIVATE_URL`). That value uses the **internal** host (e.g. `postgres.railway.internal` or similar) so the Medusa container connects inside Railway’s network.
   - Save and **redeploy** Medusa.

2. **Link Postgres to Medusa**
   - In the Medusa service, ensure the **PostgreSQL** service is linked (e.g. in the service’s dependencies or “Connect” so Railway injects the reference).

3. **If you must use the public URL**
   - In Medusa, set `DATABASE_URL` to the **public** URL from Postgres (e.g. from the Connect tab: `postgresql://...@interchange.proxy.rlwy.net:42471/railway`). The app will automatically add `sslmode=require` and `connect_timeout=30` for Railway public URLs.
   - Or add a reference to the Postgres variable **`DATABASE_PUBLIC_URL`** as `DATABASE_PUBLIC_URL` in Medusa, and remove `DATABASE_URL`; the config will use the public URL with SSL when `DATABASE_URL` is not set.
   - Prefer fixing with the internal reference (step 1) instead.

4. **Startup wait**
   - The Medusa start command now runs a short **wait-for-db** step (up to ~60s) before starting the server, so Postgres has time to accept connections. Redeploy after pulling the latest Q2P/medusa changes.
