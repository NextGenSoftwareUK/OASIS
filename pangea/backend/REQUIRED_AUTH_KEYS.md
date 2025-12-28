# Required Authentication Keys for Deployment

Here are the authentication keys and secrets your team needs to deploy the backend:

## 🔑 Required Keys

### 1. **JWT_SECRET** (Required)

**Purpose**: Used to sign and verify JWT tokens for Pangea backend authentication

**Generate a secure key:**
```bash
# Option 1: Using openssl
openssl rand -hex 32

# Option 2: Using Node.js
node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
```

**Example:**
```
JWT_SECRET=a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
```

**Requirements:**
- Minimum 32 characters
- Should be random and cryptographically secure
- **Never commit to Git**
- Keep it secret and rotate periodically

---

### 2. **OASIS_API_KEY** (Optional - Auto-refreshed)

**Purpose**: API key for authenticating with OASIS platform APIs (wallet operations, etc.)

**Status**: ✅ **Automatic Token Refresh Enabled**

The backend now automatically authenticates with OASIS using admin credentials and refreshes the token every 10 minutes before expiration. You can still provide an initial token, but it's not required.

**Optional Admin Credentials** (for automatic token refresh):
```env
OASIS_ADMIN_USERNAME=OASIS_ADMIN
OASIS_ADMIN_PASSWORD=Uppermall1!
```

**Note**: If you don't provide these, the service will use the default admin credentials. The token is automatically refreshed every 10 minutes to prevent expiration.

---

## 📋 Complete Environment Variables Checklist

Here's the complete list of environment variables needed:

### **Required for Authentication:**

```env
# JWT Configuration (REQUIRED)
JWT_SECRET=<generate-random-32-plus-char-string>
JWT_EXPIRES_IN=7d

# OASIS API Configuration
OASIS_API_URL=https://api.oasisweb4.com
OASIS_API_KEY=<your-oasis-api-key-if-required>
```

### **Required for Infrastructure:**

```env
# Node Environment
NODE_ENV=production
PORT=3000

# Database (Railway auto-provides)
DATABASE_URL=${{Postgres.DATABASE_URL}}

# Redis (Railway auto-provides)
REDIS_URL=${{Redis.REDIS_URL}}

# CORS
CORS_ORIGIN=https://your-frontend-domain.vercel.app,http://localhost:3001
```

### **Optional:**

```env
# Blockchain RPC URLs (if needed)
SOLANA_RPC_URL=https://api.mainnet-beta.solana.com
ETHEREUM_RPC_URL=https://mainnet.infura.io/v3/YOUR_INFURA_KEY
```

---

## 🚀 Quick Setup for Team

### Step 1: Generate JWT_SECRET

Run this command to generate a secure JWT secret:
```bash
openssl rand -hex 32
```

Copy the output - this is your `JWT_SECRET`.

### Step 2: Get OASIS_API_KEY

Contact your OASIS platform administrator or check:
- OASIS dashboard
- OASIS documentation
- Existing environment variables from other services

### Step 3: Add to Railway

In Railway Dashboard → Backend Service → Settings → Variables:

1. Add `JWT_SECRET` with the generated value
2. Add `OASIS_API_KEY` (if required)
3. Add `OASIS_API_URL=https://api.oasisweb4.com`
4. Add `JWT_EXPIRES_IN=7d`

---

## ⚠️ Security Best Practices

1. **Never commit secrets to Git**
   - Use Railway's environment variables
   - Don't hardcode in source code

2. **Use different secrets for each environment**
   - Production JWT_SECRET should be different from development

3. **Rotate secrets periodically**
   - Especially if compromised
   - Update all services using the secret

4. **Restrict access**
   - Only team members who need deployment access should see secrets
   - Use Railway's team permissions

---

## 🔍 How to Verify Keys Are Set

After adding keys to Railway:

1. Go to Railway Dashboard → Service → Settings → Variables
2. Verify all keys are present:
   - ✅ `JWT_SECRET` (should be a long random string)
   - ✅ `OASIS_API_URL` (should be the OASIS API URL)
   - ✅ `OASIS_API_KEY` (if required)
   - ✅ `JWT_EXPIRES_IN` (usually `7d`)

3. Redeploy the service to pick up new environment variables

4. Test authentication:
   ```bash
   curl -X POST https://your-app.up.railway.app/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "test123",
       "username": "testuser"
     }'
   ```

---

## ❓ Questions to Answer

For your team, you need to determine:

1. **Is OASIS_API_KEY actually required?**
   - Check OASIS API documentation
   - Test without it (may work for some endpoints)
   - Contact OASIS support if unsure

2. **What's the production OASIS API URL?**
   - Production: `https://api.oasisweb4.com`
   - Make sure this is accessible from Railway

3. **Are there any other API keys needed?**
   - Blockchain RPC endpoints?
   - Smart contract generator API?
   - Other external services?

---

## 📝 Summary

**Minimum Required:**
- ✅ `JWT_SECRET` - Generate using `openssl rand -hex 32`
- ✅ `OASIS_API_URL` - `http://api.oasisweb4.com` (note: HTTP, not HTTPS)
- ✅ `OASIS_ADMIN_USERNAME` - Default: `OASIS_ADMIN` (for auto token refresh)
- ✅ `OASIS_ADMIN_PASSWORD` - Default: `Uppermall1!` (for auto token refresh)
- ⚠️ `OASIS_API_KEY` - Optional (token is auto-refreshed every 10 minutes)

**Recommended:**
- `JWT_EXPIRES_IN=7d` - Token expiration time
- `CORS_ORIGIN` - Your frontend domain(s)

All other variables (DATABASE_URL, REDIS_URL) are auto-provided by Railway services.




