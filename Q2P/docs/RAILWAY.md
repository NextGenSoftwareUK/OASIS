# Deploy Q2P on Railway

Use this after you’ve added **PostgreSQL** and **two services** from the [Q2P repo](https://github.com/NextGenSoftwareUK/Q2P).

---

## 1. Configure each service

### Service 1 – Medusa (backend)

- **Settings** → **Root Directory**: `medusa`
- Build/start are set via `medusa/railway.json` (no need to set in UI unless you want to override).
- **Build** runs `pnpm install`, `medusa build`, then `pnpm install --prod` inside `.medusa/server` so the server has its own deps.
- **Start** runs `wait-for-db` and `ensure-admin-build` from the repo root, then runs `medusa start` from **`.medusa/server`** so the admin `public/admin/index.html` is found (required by Medusa v2).

### Service 2 – Storefront (Next.js)

- **Settings** → **Root Directory**: `storefront`
- Build/start are set via `storefront/railway.json`.

### Service 3 – PostgreSQL

- No root directory. Use Railway’s **Variables** (or **Connect**) to copy `DATABASE_URL` for Medusa.

---

## 2. Medusa – Variables

In the **Medusa** service → **Variables**, add:

| Variable        | Value |
|-----------------|--------|
| `DATABASE_URL`  | From PostgreSQL service (Variables or Connect tab – copy the URL). |
| `STORE_CORS`    | `https://YOUR-STOREFRONT-URL.up.railway.app` (update after storefront is deployed). |
| `ADMIN_CORS`    | `https://YOUR-MEDUSA-URL.up.railway.app` |
| `AUTH_CORS`     | `https://YOUR-MEDUSA-URL.up.railway.app` |
| `JWT_SECRET`    | Any long random string (e.g. 32+ chars). |
| `COOKIE_SECRET` | Any long random string. |

Optional (Stripe): `STRIPE_API_KEY`, `STRIPE_WEBHOOK_SECRET`.

---

## 3. Deploy order

1. **PostgreSQL** – already added.
2. **Medusa** – set Root Directory and variables above, then deploy. Note the public URL (e.g. `https://q2p-medusa-production-xxxx.up.railway.app`).
3. **Storefront** – set Root Directory and variables (step 4), then deploy. Note the storefront URL.
4. **Medusa again** – set `STORE_CORS` to the real storefront URL, redeploy.

---

## 4. Storefront – Variables

In the **Storefront** service → **Variables**, add:

| Variable                          | Value |
|-----------------------------------|--------|
| `NEXT_PUBLIC_MEDUSA_BACKEND_API`  | `https://YOUR-MEDUSA-URL.up.railway.app` (no trailing slash). |
| `MEDUSA_API_KEY`                  | Publishable API key from Medusa (get after step 5). |
| `SITE_NAME`                       | `Q2P Shop` (or your name). |

Optional: `NEXT_PUBLIC_OASIS_ONODE_URL`, `MEDUSA_REVALIDATION_SECRET`, etc.

---

## 5. Seed Medusa (products + API key)

After Medusa is running:

1. Open the **Medusa** service.
2. Use **Settings** → run a one-off command, or open the **Shell** (if available).
3. Run:  
   `pnpm run seed`

Then get the **publishable API key**:

- **Option A:** Medusa Admin (if you deploy it) → Settings → API Keys → copy the “Webshop” publishable key.
- **Option B:** Call the API (e.g. list API keys) and copy the publishable key.

Put that value in the Storefront variable **`MEDUSA_API_KEY`** and redeploy the storefront.

---

## 6. Update CORS and redeploy Medusa

After the storefront has a public URL:

1. **Medusa** → **Variables** → set `STORE_CORS` = `https://your-actual-storefront-url.up.railway.app`.
2. Redeploy Medusa.

---

## Quick checklist

- [ ] PostgreSQL added; `DATABASE_URL` copied.
- [ ] Medusa: Root Directory = `medusa`, all variables set, deployed.
- [ ] Medusa: `pnpm run seed` run; publishable API key copied.
- [ ] Storefront: Root Directory = `storefront`, `NEXT_PUBLIC_MEDUSA_BACKEND_API` and `MEDUSA_API_KEY` set, deployed.
- [ ] Medusa: `STORE_CORS` set to storefront URL, redeployed.
