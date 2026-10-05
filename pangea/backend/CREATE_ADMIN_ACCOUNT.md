# How to Create an Admin Account

## Quick Start (Easiest Method)

### Step 1: Register the User (if not already registered)
```bash
curl -X POST https://pangea-production-128d.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "secure-password",
    "username": "admin-user",
    "firstName": "Admin",
    "lastName": "User"
  }'
```

### Step 2: Run the Script
```bash
cd backend
ADMIN_EMAIL=admin@example.com npx ts-node scripts/create-admin-account.ts
```

That's it! The script will:
- ✅ Find the user by email
- ✅ Update their role to 'admin'
- ✅ Show confirmation

### Step 3: Re-login to Get New JWT Token
After promoting to admin, you need to login again to get a JWT token with the admin role:

```bash
curl -X POST https://pangea-production-128d.up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "secure-password"
  }'
```

### Step 4: Test Admin Access
```bash
TOKEN="your-jwt-token-from-step-3"
curl -X GET https://pangea-production-128d.up.railway.app/api/admin/users \
  -H "Authorization: Bearer $TOKEN"
```

If you get a list of users (instead of 403), you're an admin! ✅

---

## Alternative: Direct Database Update

If you prefer SQL or the script doesn't work:

### Via Railway Dashboard:
1. Go to Railway dashboard → PostgreSQL service → "Data" tab
2. Run this query:
```sql
UPDATE users 
SET role = 'admin' 
WHERE email = 'admin@example.com';
```

### Via Railway CLI:
```bash
railway connect postgres
# Then in psql:
UPDATE users SET role = 'admin' WHERE email = 'admin@example.com';
```

---

## How It Works

- Users have a `role` field in the database (default: `'user'`)
- Setting `role = 'admin'` grants admin access
- The `AdminGuard` checks `user.role === 'admin'` to protect admin routes
- After updating the role, you must re-login to get a new JWT token

---

## Promoting Other Users (Once You Have Admin)

After you have admin access, you can promote other users via the API:

```bash
# Update user role
PUT /api/admin/users/:userId
Body: {
  "role": "admin"
}
```

**Note:** This requires the `role` field in `UpdateUserDto` (recently added).

---

## Troubleshooting

**"User not found" error:**
- Make sure the user registered first via `/api/auth/register`

**"403 Forbidden" after updating:**
- You must re-login after updating the role to get a new JWT token

**Script can't connect to database:**
- Make sure `DATABASE_URL` environment variable is set
- Or use Railway's environment variables



