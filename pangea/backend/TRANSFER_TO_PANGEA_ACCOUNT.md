# Transfer Railway Project to Pangea Company Account

This guide will help you transfer your Railway backend deployment to the Pangea company account.

**Target Project ID:** `9eb02f3c-a006-4faa-abef-87021dacefeb`

## Prerequisites

Before transferring, ensure:
- ✅ You have admin access to the Pangea company account/team on Railway
- ✅ The Pangea account has a Pro Plan (required for Team transfers)
- ✅ You're logged into Railway with your current account

## Method 1: Transfer via Dashboard (Recommended)

### Step 1: Access Project Settings

1. Go to Railway Dashboard: https://railway.app/dashboard
2. Click on your **Pangea backend project**
3. Click the **gear icon** (⚙️) at the top right of the project canvas
4. Or go to **Settings** from the project menu

### Step 2: Initiate Transfer

1. Scroll down to the bottom of the Settings page
2. Find the **"Transfer Project"** section
3. Click **"Transfer Project"** button

### Step 3: Select Destination

1. A modal will appear with available destinations
2. Select **"Pangea"** team/organization (or search by the project ID if needed)
3. Confirm the transfer

### Step 4: Verify Transfer

1. The recipient (Pangea team admin) will receive an email notification
2. Log into Railway with the Pangea account
3. Verify the project appears in the Pangea team dashboard
4. Confirm all services (backend, PostgreSQL, Redis) are present

## Method 2: Using Railway CLI (Alternative)

If you have CLI access to both accounts:

```bash
# 1. Link to your current project
cd /Volumes/Storage/OASIS_CLEAN/pangea/backend
railway link  # Link to current project

# 2. Switch to Pangea team/organization
railway whoami  # Check current account
# Note: CLI transfer may require dashboard access

# 3. The transfer must be done via dashboard (see Method 1)
```

## Important Notes

### Subscription Requirements

- **To Team/Organization**: Requires **Pro Plan** on both accounts
- **To Another User**: Both accounts need **Hobby Plan** or higher
- For company accounts, Pro Plan is typically required

### What Gets Transferred

- ✅ All services (backend, PostgreSQL, Redis)
- ✅ Environment variables
- ✅ Deployment history
- ✅ Custom domains (if any)
- ✅ Service configurations

### After Transfer

1. **Verify Services**: All services should be present in the new account
2. **Check Environment Variables**: Review and update if needed
3. **Update Access**: Team members will need access to the new project
4. **Update CI/CD**: If you have GitHub Actions or CI/CD, update tokens

## Troubleshooting

### "Transfer Project" Option Not Visible

- **Reason**: You don't have admin rights, or the account doesn't support transfers
- **Solution**: 
  - Ensure you're the project owner
  - Verify the target account has Pro Plan (for Team transfers)
  - Check you have admin access to the Pangea team

### Transfer Fails

- **Reason**: Subscription plan mismatch
- **Solution**: 
  - Ensure Pangea account has Pro Plan
  - Contact Railway support if issues persist

### Services Missing After Transfer

- **Reason**: Services might be in a different project
- **Solution**: 
  - Check all projects in the Pangea account
  - Services should transfer together, but verify

## Post-Transfer Checklist

After successful transfer:

- [ ] Log into Pangea Railway account
- [ ] Verify project appears: `9eb02f3c-a006-4faa-abef-87021dacefeb`
- [ ] Verify all services are present (backend, PostgreSQL, Redis)
- [ ] Check environment variables are intact
- [ ] Test health endpoint: `curl https://your-app.up.railway.app/api/health`
- [ ] Update team member access if needed
- [ ] Update any CI/CD pipelines with new tokens
- [ ] Update documentation with new project location

## Alternative: Redeploy to Pangea Account

If transfer doesn't work, you can redeploy:

1. **Log into Railway with Pangea account**
2. **Create new project** (or use existing: `9eb02f3c-a006-4faa-abef-87021dacefeb`)
3. **Add GitHub repository**: `maxgershfield/pangea`
4. **Set root directory**: `backend`
5. **Add services**: PostgreSQL, Redis
6. **Configure environment variables** (copy from old project)
7. **Run migrations**

This is more work but ensures a clean setup in the correct account.

## Need Help?

If you encounter issues:

1. Check Railway documentation: https://docs.railway.com/guides/projects
2. Contact Railway support: https://railway.app/support
3. Verify subscription plans match requirements

---

**Project ID to transfer to:** `9eb02f3c-a006-4faa-abef-87021dacefeb`




