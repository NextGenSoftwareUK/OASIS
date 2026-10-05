# Run Database Migrations

## Option 1: Using Railway Dashboard Shell (Easiest)

1. Go to Railway Dashboard: https://railway.app/dashboard
2. Click on your **Pangea backend service**
3. Go to **"Deployments"** tab
4. Click on the **latest deployment**
5. Click **"Shell"** button (or "Open Shell")
6. In the shell, run:
   ```bash
   npm run migration:run
   ```

## Option 2: Using Railway CLI

First, link your project:
```bash
cd /Volumes/Storage/OASIS_CLEAN/pangea/backend
railway link
# Select your project when prompted
```

Then run migrations:
```bash
railway run npm run migration:run
```

## Expected Output

You should see output like:
```
> pangea-markets-backend@1.0.0 migration:run
> typeorm-ts-node-commonjs migration:run -d src/config/data-source.ts

query: SELECT * FROM "migrations"
query: SELECT * FROM "migrations"
query: CREATE TABLE "users" ...
query: CREATE TABLE "tokenized_assets" ...
...
Migration 1738080000000-InitialSchema has been executed successfully.
```

## After Migrations

Test your API:
```bash
curl https://your-app.up.railway.app/api/health
```

You should see:
```json
{
  "status": "ok",
  "timestamp": "...",
  "service": "Pangea Markets Backend",
  "checks": {
    "database": { "status": "ok" },
    "redis": { "status": "ok" }
  }
}
```




