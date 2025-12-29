# OASIS API Connection Fix

## Issue

The backend is trying to connect to OASIS API but failing with connection errors. This typically happens when:
1. `OASIS_API_URL` is set to `https://` instead of `http://`
2. Network connectivity issues from Railway to OASIS API

## Solution

### Step 1: Verify OASIS_API_URL in Railway

1. Go to Railway Dashboard → Your backend service
2. Click **Settings** → **Variables**
3. Find `OASIS_API_URL`
4. **Ensure it's set to:** `http://api.oasisweb4.com` (NOT `https://`)

If it's set to `https://api.oasisweb4.com`, change it to `http://api.oasisweb4.com`

### Step 2: Verify Other OASIS Variables

Make sure these are also set correctly:

- `OASIS_ADMIN_USERNAME` = `OASIS_ADMIN`
- `OASIS_ADMIN_PASSWORD` = `Uppermall1!`
- `OASIS_API_URL` = `http://api.oasisweb4.com` ✅ **Must be HTTP, not HTTPS**

### Step 3: Test OASIS API Connectivity

You can test if the OASIS API is reachable:

```bash
# Test from your local machine (should work)
curl -X POST http://api.oasisweb4.com/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}'

# This should return a JWT token if it works
```

### Step 4: Verify After Redeploy

After changing the environment variable:

1. Railway will automatically redeploy
2. Check the logs for OASIS connection attempts
3. Look for: `✅ OASIS API token refreshed successfully` (success)
4. Or: `Failed to refresh OASIS token` (still failing)

## Common Errors

### Error: `connect ECONNREFUSED 54.158.72.230:443`

**Cause:** Trying to connect via HTTPS (port 443) when OASIS API uses HTTP (port 80)

**Fix:** Change `OASIS_API_URL` from `https://api.oasisweb4.com` to `http://api.oasisweb4.com`

### Error: `connect ETIMEDOUT`

**Cause:** Network connectivity issue from Railway to OASIS API

**Possible fixes:**
1. Verify OASIS API is accessible: `http://api.oasisweb4.com`
2. Check if OASIS API has IP whitelisting (might need to whitelist Railway IPs)
3. Verify there's no firewall blocking the connection

### Error: `getaddrinfo ENOTFOUND api.oasisweb4.com`

**Cause:** DNS resolution failure

**Fix:** Verify the domain name is correct: `api.oasisweb4.com` (not `.world` or other domain)

## Why HTTP and Not HTTPS?

The OASIS API at `api.oasisweb4.com` only supports HTTP (port 80), not HTTPS (port 443). 

- ✅ Correct: `http://api.oasisweb4.com`
- ❌ Wrong: `https://api.oasisweb4.com`

## Impact on Application

Even if OASIS API connection fails:
- ✅ Backend will still start
- ✅ Public endpoints work (health, assets)
- ✅ Database and Redis connections work
- ❌ User registration/login will fail (requires OASIS API)
- ❌ OASIS token refresh will fail (but won't crash the app)

## Verification

After fixing the `OASIS_API_URL`, test registration:

```bash
curl -X POST https://pangea-production-128d.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "test123456",
    "username": "testuser",
    "firstName": "Test",
    "lastName": "User"
  }'
```

If it works, you should get a response with `user`, `token`, and `expiresAt` fields.




