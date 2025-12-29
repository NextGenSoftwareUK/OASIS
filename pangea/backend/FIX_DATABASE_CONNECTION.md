# Fix Database Connection Error

## Problem

Your deployment is crashing with:
```
Error: connect ECONNREFUSED ::1:5432  (PostgreSQL)
Error: connect ECONNREFUSED ::1:6379  (Redis)
```

This means the app is trying to connect to `localhost` instead of Railway's services.

## Solution: Configure Environment Variables in Railway

### Step 1: Check Current Environment Variables

In Railway Dashboard:
1. Go to your backend service
2. Click "Settings" → "Variables"
3. Check if `DATABASE_URL` and `REDIS_URL` are set

### Step 2: Add Database Connection Variables

You need to add these environment variables:

#### Option A: Using Railway Service References (Recommended)

1. **For PostgreSQL:**
   - Click "New Variable"
   - Click the "Reference" button (or select from dropdown)
   - Choose "Postgres" service → "DATABASE_URL"
   - This creates: `DATABASE_URL=${{Postgres.DATABASE_URL}}`

2. **For Redis:**
   - Click "New Variable"
   - Click the "Reference" button
   - Choose "Redis" service → "REDIS_URL"
   - This creates: `REDIS_URL=${{Redis.REDIS_URL}}`

#### Option B: Manual Configuration (If services have different names)

If your PostgreSQL/Redis services have different names, you can reference them by name:

1. **For PostgreSQL:**
   - Variable name: `DATABASE_URL`
   - Value: `${{Postgres.DATABASE_URL}}`
   - Or if your service is named differently: `${{YourPostgresServiceName.DATABASE_URL}}`

2. **For Redis:**
   - Variable name: `REDIS_URL`
   - Value: `${{Redis.REDIS_URL}}`
   - Or: `${{YourRedisServiceName.REDIS_URL}}`

### Step 3: Verify Services Exist

Make sure you have added these services to your Railway project:

1. **PostgreSQL:**
   - In Railway project, click "+ New" → "Database" → "PostgreSQL"
   - Note the service name (usually "Postgres")

2. **Redis:**
   - Click "+ New" → "Database" → "Redis"
   - Note the service name (usually "Redis")

### Step 4: Check Service Names

If the service references don't work:

1. Go to your Railway project
2. Check the exact names of your PostgreSQL and Redis services
3. Use those exact names in the variable references:
   - Example: `${{MyPostgresService.DATABASE_URL}}`

## Quick Fix Steps

1. ✅ **Add PostgreSQL service** (if not already added)
2. ✅ **Add Redis service** (if not already added)
3. ✅ **Set `DATABASE_URL`** → Reference from Postgres service
4. ✅ **Set `REDIS_URL`** → Reference from Redis service
5. ✅ **Redeploy** the backend service

## Verify Environment Variables

After setting variables, verify they're correct:

In Railway Dashboard → Service → Settings → Variables, you should see:
- `DATABASE_URL=${{Postgres.DATABASE_URL}}` (or similar)
- `REDIS_URL=${{Redis.REDIS_URL}}` (or similar)

**NOT:**
- ❌ `DATABASE_URL=localhost:5432`
- ❌ `DATABASE_URL=postgresql://localhost:5432/...`
- ❌ `REDIS_URL=localhost:6379`

## After Fixing

1. **Redeploy** your backend service
2. **Check logs** to verify connection succeeds
3. **Test health endpoint**: `curl https://your-app.up.railway.app/api/health`

The health check should show:
```json
{
  "checks": {
    "database": { "status": "ok" },
    "redis": { "status": "ok" }
  }
}
```

## Common Issues

### "Service not found" in variable references
- **Solution**: Ensure PostgreSQL and Redis services are in the same Railway project
- **Check**: Services must be added before you can reference them

### Variables not updating
- **Solution**: After adding/changing variables, redeploy the service
- **Note**: Railway doesn't automatically restart on variable changes

### Still connecting to localhost
- **Check**: Verify variable names are exactly `DATABASE_URL` and `REDIS_URL` (case-sensitive)
- **Check**: Ensure you're using the "Reference" button, not typing localhost values manually




