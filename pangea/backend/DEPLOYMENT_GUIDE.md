# Backend Deployment Guide

## Platform Recommendation

**✅ Railway (Recommended)** - Best fit for this backend
- Supports long-running services (required for WebSockets)
- Managed PostgreSQL and Redis
- Simple Git-based deployment
- Environment variable management
- Auto-scaling

**⚠️ Vercel (Not Recommended)** - Serverless only
- Doesn't support WebSockets well
- Serverless functions have execution time limits
- Would require major refactoring to work

---

## Option 1: Deploy to Railway (Recommended)

Railway is ideal for NestJS backends with WebSockets, PostgreSQL, and Redis.

### Prerequisites

1. Create a Railway account: https://railway.app
2. Install Railway CLI (optional): `npm i -g @railway/cli`

### Step 1: Create Railway Project

1. Go to https://railway.app/dashboard
2. Click "New Project"
3. Select "Deploy from GitHub repo" (recommended) or "Empty Project"

### Step 2: Add Services

You'll need three services:
1. **PostgreSQL Database**
2. **Redis**
3. **Backend API**

#### Add PostgreSQL

1. Click "+ New" → "Database" → "Add PostgreSQL"
2. Railway automatically provisions a PostgreSQL instance
3. Note the connection details (they'll be available as `DATABASE_URL` env var)

#### Add Redis

1. Click "+ New" → "Database" → "Add Redis"
2. Railway automatically provisions a Redis instance
3. Connection URL will be available as `REDIS_URL` env var

#### Add Backend Service

1. Click "+ New" → "GitHub Repo"
2. Select your repository
3. Railway will detect it's a Node.js project

### Step 3: Configure Build Settings

Railway should auto-detect, but verify these settings in the service settings:

**Root Directory**: `pangea/backend` (if repo contains multiple projects)

**Build Command**: 
```bash
npm install && npm run build
```

**Start Command**:
```bash
npm run start:prod
```

**Node Version**: Set to `18` or `20` in Railway settings

### Step 4: Configure Environment Variables

Add these environment variables in Railway dashboard (Settings → Variables):

**Required Variables:**
```env
# Node Environment
NODE_ENV=production
PORT=3000

# Database (Railway provides this automatically)
DATABASE_URL=${{Postgres.DATABASE_URL}}
# Or use individual vars:
# DB_HOST=${{Postgres.PGHOST}}
# DB_PORT=${{Postgres.PGPORT}}
# DB_USERNAME=${{Postgres.PGUSER}}
# DB_PASSWORD=${{Postgres.PGPASSWORD}}
# DB_DATABASE=${{Postgres.PGDATABASE}}

# Redis (Railway provides this automatically)
REDIS_URL=${{Redis.REDIS_URL}}
# Or use individual vars:
# REDIS_HOST=${{Redis.REDISHOST}}
# REDIS_PORT=${{Redis.REDISPORT}}
# REDIS_PASSWORD=${{Redis.REDISPASSWORD}}

# JWT
JWT_SECRET=your-super-secret-jwt-key-min-32-chars-change-in-production
JWT_EXPIRES_IN=7d

# OASIS API
OASIS_API_URL=https://api.oasisweb4.com
OASIS_API_KEY=your-oasis-api-key

# CORS (allow your frontend domain)
CORS_ORIGIN=https://your-frontend-domain.vercel.app,http://localhost:3001

# Blockchain RPC URLs (if needed)
SOLANA_RPC_URL=https://api.mainnet-beta.solana.com
ETHEREUM_RPC_URL=https://mainnet.infura.io/v3/YOUR_INFURA_KEY
```

**Important Security Notes:**
- Generate a strong `JWT_SECRET` (at least 32 characters, random)
- Never commit secrets to Git
- Use Railway's environment variable system

### Step 5: Run Database Migrations

Before the first deployment, run migrations:

**Option A: Using Railway CLI**
```bash
railway run npm run migration:run
```

**Option B: Using Railway Shell**
1. In Railway dashboard, click on your backend service
2. Click "Deployments" → Select latest deployment → "Shell"
3. Run: `npm run migration:run`

**Option C: Add to Build Script (Not Recommended for Production)**
Add migration run to your build, but this can cause issues with concurrent deployments.

### Step 6: Deploy

1. Push your code to GitHub (if using GitHub integration)
2. Railway will automatically build and deploy
3. Check the deployment logs for any errors

### Step 7: Get Your Backend URL

1. In Railway dashboard, click on your backend service
2. Go to "Settings" → "Networking"
3. Click "Generate Domain" to get a public URL like `https://your-app.up.railway.app`
4. Or configure a custom domain

### Step 8: Update CORS Settings

Update `CORS_ORIGIN` environment variable to include:
- Your frontend domain
- Your Railway backend domain (if calling from different origins)

### Step 9: Test Deployment

```bash
curl https://your-app.up.railway.app/api/health
```

Expected response:
```json
{
  "status": "ok",
  "timestamp": "2025-01-27T...",
  "service": "Pangea Markets Backend",
  "version": "1.0.0",
  "checks": {
    "database": { "status": "ok", "message": "Database connection successful" },
    "redis": { "status": "ok", "message": "Redis connection successful" }
  }
}
```

---

## Option 2: Deploy to Vercel (Not Recommended)

⚠️ **Warning**: Vercel is serverless-only and doesn't support WebSockets well. This would require significant refactoring.

If you must use Vercel, you would need to:

1. **Remove WebSocket support** or move it to a separate service
2. **Convert to serverless functions** (NestJS adapter for Vercel)
3. **Use external PostgreSQL** (Vercel Postgres or external provider)
4. **Use external Redis** (Upstash or external provider)

### Why Not Vercel for This Backend?

- ❌ WebSockets don't work in serverless functions
- ❌ Long-running processes have timeout limits
- ❌ Cold starts can cause latency
- ❌ Would require major architectural changes

**Recommendation**: Use Railway instead.

---

## Deployment Checklist

Before deploying:

- [ ] All environment variables configured
- [ ] Strong `JWT_SECRET` generated
- [ ] `NODE_ENV` set to `production`
- [ ] Database migrations ready to run
- [ ] CORS origins configured correctly
- [ ] All external API keys configured
- [ ] Health check endpoint tested
- [ ] Build command works locally (`npm run build`)
- [ ] Production start works locally (`npm run start:prod`)

---

## Running Migrations in Production

### Manual Migration

```bash
# Using Railway CLI
railway run npm run migration:run

# Revert last migration (if needed)
railway run npm run migration:revert
```

### Automated Migration (Advanced)

You can add a migration script to run automatically, but be careful with concurrent deployments. Consider using a migration service or running migrations as a separate step.

---

## Monitoring & Logs

### Railway

- **Logs**: Available in Railway dashboard under "Deployments" → "View Logs"
- **Metrics**: Railway provides basic metrics (CPU, memory, network)
- **Alerts**: Configure in Railway dashboard for deployment failures

### Health Checks

Your backend has a health check endpoint: `GET /api/health`

You can configure Railway to use this for health checks:
- Settings → Health Checks
- Path: `/api/health`
- Interval: 30 seconds

---

## Troubleshooting

### Build Fails

1. Check build logs in Railway dashboard
2. Verify `package.json` has correct build script
3. Check Node version (should be 18+)
4. Ensure all dependencies are in `dependencies`, not `devDependencies`

### Database Connection Fails

1. Verify `DATABASE_URL` is set correctly
2. Check PostgreSQL service is running
3. Ensure migrations have run
4. Check database connection from Railway shell: `railway run psql $DATABASE_URL`

### Redis Connection Fails

1. Verify `REDIS_URL` is set correctly
2. Check Redis service is running
3. Test connection: `railway run node -e "const Redis = require('ioredis'); const r = new Redis(process.env.REDIS_URL); r.ping().then(console.log)"`

### WebSocket Issues

1. Ensure Railway allows WebSocket connections (should work by default)
2. Check CORS settings allow WebSocket origins
3. Verify Socket.IO is configured correctly in your code

### Port Issues

1. Railway automatically sets `PORT` environment variable
2. Your code should use `process.env.PORT || 3000`
3. Ensure `main.ts` uses `process.env.PORT`

---

## Cost Estimates (Railway)

Railway pricing (as of 2025):
- **Free tier**: $5 credit/month
- **Hobby**: $5/month + usage
- **Pro**: $20/month + usage

Typical usage:
- PostgreSQL: ~$5-10/month
- Redis: ~$5/month
- Backend service: Pay per usage (usually $5-20/month for small-medium apps)

Total estimate: ~$15-35/month for a small-medium application

---

## Alternative Platforms

If Railway doesn't meet your needs, consider:

1. **Render** - Similar to Railway, good for Node.js apps
2. **Fly.io** - Great for global distribution
3. **DigitalOcean App Platform** - Simple PaaS
4. **AWS/Azure/GCP** - More control, more complex setup

---

## Next Steps After Deployment

1. ✅ Set up monitoring and alerting
2. ✅ Configure custom domain (optional)
3. ✅ Set up CI/CD for automatic deployments
4. ✅ Configure backup strategy for database
5. ✅ Set up staging environment
6. ✅ Test all endpoints
7. ✅ Update frontend to use production backend URL








