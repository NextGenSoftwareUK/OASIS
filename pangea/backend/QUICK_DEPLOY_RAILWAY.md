# Quick Deploy to Railway

## Quick Start (5 minutes)

### 1. Prepare Your Repo

Ensure your backend code is in a Git repository (GitHub recommended).

### 2. Create Railway Account

1. Go to https://railway.app
2. Sign up with GitHub
3. Create a new project

### 3. Add Services

In your Railway project, add:

1. **PostgreSQL**: Click "+ New" → "Database" → "PostgreSQL"
2. **Redis**: Click "+ New" → "Database" → "Redis"  
3. **Backend**: Click "+ New" → "GitHub Repo" → Select your repo

### 4. Configure Backend Service

**Settings → Deploy:**
- Root Directory: `pangea/backend` (if your repo has multiple projects)
- Build Command: `npm install && npm run build`
- Start Command: `npm run start:prod`

**Settings → Variables:**
Add these environment variables:

```env
NODE_ENV=production
PORT=3000

# These are auto-provided by Railway services:
DATABASE_URL=${{Postgres.DATABASE_URL}}
REDIS_URL=${{Redis.REDIS_URL}}

# Add these manually:
JWT_SECRET=change-this-to-a-random-32-char-string
JWT_EXPIRES_IN=7d
OASIS_API_URL=https://api.oasisweb4.com
OASIS_API_KEY=your-oasis-api-key
CORS_ORIGIN=https://your-frontend.vercel.app
```

### 5. Run Migrations

After first deployment succeeds:

```bash
# Install Railway CLI (if not installed)
npm i -g @railway/cli

# Login
railway login

# Link to your project
railway link

# Run migrations
railway run npm run migration:run
```

### 6. Get Your URL

Railway will provide a URL like: `https://your-app.up.railway.app`

Test it:
```bash
curl https://your-app.up.railway.app/api/health
```

### 7. Update Frontend

Update your frontend to use the Railway URL:
- Development: Keep using `localhost:3000`
- Production: Use `https://your-app.up.railway.app`

---

## Troubleshooting

**Build fails?**
- Check logs in Railway dashboard
- Verify Node version (should be 18+)
- Check that all dependencies are in `package.json`

**Database connection fails?**
- Verify `DATABASE_URL` is set
- Check migrations have run: `railway run npm run migration:run`

**Redis connection fails?**
- Verify `REDIS_URL` is set

**500 errors?**
- Check logs in Railway dashboard
- Verify all environment variables are set
- Test health endpoint: `/api/health`

---

## Next Steps

1. ✅ Configure custom domain (optional)
2. ✅ Set up monitoring
3. ✅ Configure automatic deployments from Git
4. ✅ Set up staging environment

For detailed instructions, see `DEPLOYMENT_GUIDE.md`.








