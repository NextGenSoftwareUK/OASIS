# Post-Deployment Setup Guide

Your backend is now successfully deployed! Follow these steps to configure it and start using it.

## Step 1: Add Required Services

In your Railway project, you need to add:

### PostgreSQL Database
1. In Railway dashboard, click "+ New" → "Database" → "PostgreSQL"
2. Railway will automatically provision PostgreSQL
3. Note: Connection URL will be auto-available as environment variable

### Redis
1. Click "+ New" → "Database" → "Redis"
2. Railway will automatically provision Redis
3. Note: Connection URL will be auto-available as environment variable

## Step 2: Configure Environment Variables

Go to your backend service → Settings → Variables

### Required Variables

**Node Environment:**
```env
NODE_ENV=production
PORT=3000
```

**Database Connection (Auto-provided by Railway):**
- Click "New Variable" → "Reference"
- Select "Postgres" → "DATABASE_URL"
- This creates: `DATABASE_URL=${{Postgres.DATABASE_URL}}`

**Redis Connection (Auto-provided by Railway):**
- Click "New Variable" → "Reference"
- Select "Redis" → "REDIS_URL"
- This creates: `REDIS_URL=${{Redis.REDIS_URL}}`

**JWT Configuration:**
```env
JWT_SECRET=<generate-a-strong-random-32-plus-character-string>
JWT_EXPIRES_IN=7d
```

Generate a secure JWT_SECRET:
```bash
# Using openssl
openssl rand -hex 32

# Or using Node.js
node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
```

**OASIS API:**
```env
OASIS_API_URL=https://api.oasisweb4.com
OASIS_API_KEY=your-actual-oasis-api-key
```

**CORS (Important!):**
```env
CORS_ORIGIN=https://your-frontend-domain.vercel.app,http://localhost:3001
```

**Optional - Blockchain RPC URLs (if needed):**
```env
SOLANA_RPC_URL=https://api.mainnet-beta.solana.com
ETHEREUM_RPC_URL=https://mainnet.infura.io/v3/YOUR_INFURA_KEY
```

## Step 3: Run Database Migrations

After setting environment variables, run migrations:

### Option A: Using Railway Shell
1. Go to your backend service → "Deployments" → Latest deployment
2. Click "Shell" (or "Open Shell")
3. Run:
```bash
npm run migration:run
```

### Option B: Using Railway CLI
```bash
cd /Volumes/Storage/OASIS_CLEAN/pangea/backend
railway link  # Link to your project if not already linked
railway run npm run migration:run
```

## Step 4: Get Your Backend URL

1. Go to your backend service → "Settings" → "Networking"
2. Click "Generate Domain" to get a public URL
3. Your backend will be available at: `https://your-app.up.railway.app`

## Step 5: Test Your Deployment

### Health Check
```bash
curl https://your-app.up.railway.app/api/health
```

Expected response:
```json
{
  "status": "ok",
  "timestamp": "2025-12-18T...",
  "service": "Pangea Markets Backend",
  "version": "1.0.0",
  "checks": {
    "database": { "status": "ok", "message": "Database connection successful" },
    "redis": { "status": "ok", "message": "Redis connection successful" }
  }
}
```

### Test Authentication Endpoint
```bash
curl -X POST https://your-app.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "testpassword123",
    "username": "testuser",
    "firstName": "Test",
    "lastName": "User"
  }'
```

## Step 6: Update Frontend Configuration

Update your frontend to use the Railway backend URL:

**Development:**
```typescript
const API_BASE_URL = 'https://your-app.up.railway.app/api';
```

**Or use environment variable:**
```env
NEXT_PUBLIC_API_URL=https://your-app.up.railway.app/api
```

## Step 7: View Logs

Monitor your backend logs:

**In Railway Dashboard:**
1. Go to your service → "Deployments"
2. Click on a deployment → "View Logs"

**Using Railway CLI:**
```bash
railway logs
railway logs --tail  # Real-time logs
```

## Step 8: Common Operations

### Redeploy
- Dashboard: Deployments → Redeploy
- CLI: `railway up`

### View Environment Variables
- Dashboard: Settings → Variables
- CLI: `railway variables`

### Connect to Database
```bash
railway connect postgres
```

### Run Commands
```bash
railway run <command>
# Example: railway run npm run migration:run
```

## Troubleshooting

### Health Check Fails
- Check environment variables are set correctly
- Verify PostgreSQL and Redis services are running
- Check logs for connection errors

### Database Connection Fails
- Verify `DATABASE_URL` is set and references Postgres service
- Check PostgreSQL service is running
- Ensure migrations have run

### Redis Connection Fails
- Verify `REDIS_URL` is set and references Redis service
- Check Redis service is running

### CORS Errors from Frontend
- Update `CORS_ORIGIN` to include your frontend domain
- Include `http://localhost:3001` for local development

## Next Steps

1. ✅ Configure environment variables
2. ✅ Run database migrations
3. ✅ Test health endpoint
4. ✅ Update frontend to use backend URL
5. ✅ Test authentication flow
6. ✅ Test API endpoints

Your backend is now live and ready to use! 🚀








