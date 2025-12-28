# Transfer Railway Project to Launchboard Workspace

This guide will help you transfer your Railway backend deployment to the Launchboard workspace.

**Target Workspace:** Launchboard-HQ (https://github.com/Launchboard-HQ/pangea-markets)

## Prerequisites

Before transferring, ensure:
- ✅ You have accepted the workspace invite: https://railway.com/workspace/invite/VlDBDmqtfDiHI4uNLcWru
- ✅ You're logged into Railway with your account that owns the project
- ✅ The Launchboard workspace has appropriate subscription (Pro Plan recommended for team projects)

## Method: Transfer via Dashboard

### Step 1: Accept Workspace Invite

1. Open the invite link: https://railway.com/workspace/invite/VlDBDmqtfDiHI4uNLcWru
2. Click "Accept Invite" or sign in and accept
3. Verify you can see the Launchboard workspace in your Railway dashboard

### Step 2: Access Project Settings

1. Go to Railway Dashboard: https://railway.app/dashboard
2. Click on your **backend project** (likely named "pangea-production" or similar)
3. Click the **⚙️ Settings** button (or gear icon)
4. Navigate to **Settings** → **General** tab

### Step 3: Initiate Transfer

1. Scroll down to the bottom of the Settings page
2. Find the **"Transfer Project"** section
3. Click **"Transfer Project"** button
4. A modal will appear with available destinations

### Step 4: Select Destination Workspace

1. In the transfer modal, select **"Launchboard"** workspace (or search for it)
2. Review the transfer details
3. Confirm the transfer

### Step 5: Verify Transfer

1. Switch to the Launchboard workspace view in Railway
2. Verify the project appears in the workspace dashboard
3. Confirm all services are present:
   - Backend service
   - PostgreSQL database
   - Redis cache
4. Check environment variables are intact
5. Test the deployment URL (if it changed, update your frontend)

## Important Notes

### Subscription Requirements

- **To Team/Organization/Workspace**: May require **Pro Plan** on the workspace
- For company workspaces, Pro Plan is typically recommended
- Contact Launchboard team if transfer option is not available

### What Gets Transferred

- ✅ All services (backend, PostgreSQL, Redis)
- ✅ Environment variables
- ✅ Deployment history
- ✅ Custom domains (if any)
- ✅ Service configurations
- ✅ Build and deploy settings

### What to Verify After Transfer

- [ ] All services present (backend, PostgreSQL, Redis)
- [ ] Environment variables intact:
  - `DATABASE_URL` (should auto-reference PostgreSQL service)
  - `REDIS_URL` (should auto-reference Redis service)
  - `JWT_SECRET`
  - `OASIS_API_URL`
  - `OASIS_API_KEY`
  - `NODE_ENV=production`
  - `PORT=3000`
  - `CORS_ORIGIN`
- [ ] Health endpoint working: `curl https://your-app.up.railway.app/api/health`
- [ ] Database migrations still applied
- [ ] GitHub integration (if using) may need re-linking

### URL Changes

- The deployment URL may change after transfer
- Check the new URL in: Service → Settings → Networking
- Update your frontend environment variables with the new URL
- Update any documentation referencing the old URL

## Troubleshooting

### "Transfer Project" Option Not Visible

**Possible reasons:**
- You're not the project owner/admin
- You haven't accepted the workspace invite yet
- The workspace doesn't support transfers (subscription issue)

**Solutions:**
1. Ensure you've accepted the workspace invite first
2. Verify you're the project owner (check project settings)
3. Contact Launchboard team to verify workspace permissions

### Transfer Fails

**Possible reasons:**
- Subscription plan mismatch
- Workspace permissions issue

**Solutions:**
1. Verify Launchboard workspace has Pro Plan (if required)
2. Contact Railway support: https://railway.app/support
3. Consider alternative: redeploy to Launchboard workspace (see below)

### Services Missing After Transfer

**Solution:**
- All services should transfer together
- Check all projects in the Launchboard workspace
- Verify services are connected (check environment variable references)

## Alternative: Redeploy to Launchboard Workspace

If transfer doesn't work, you can redeploy from scratch:

1. **Log into Railway with Launchboard workspace**
2. **Create new project** in the Launchboard workspace
3. **Connect GitHub repository**: `Launchboard-HQ/pangea-markets`
4. **Set root directory**: `backend` (or wherever backend code is located)
5. **Add services**:
   - PostgreSQL database
   - Redis cache
6. **Configure environment variables** (copy from old project):
   - `NODE_ENV=production`
   - `PORT=3000`
   - `DATABASE_URL=${{Postgres.DATABASE_URL}}`
   - `REDIS_URL=${{Redis.REDIS_URL}}`
   - `JWT_SECRET=<your-secret>`
   - `OASIS_API_URL=https://api.oasisweb4.com`
   - `OASIS_API_KEY=<your-key>`
   - `CORS_ORIGIN=<your-frontend-url>`
7. **Run migrations**: `railway run npm run migration:run`

## Post-Transfer Checklist

After successful transfer:

- [ ] Project visible in Launchboard workspace
- [ ] All services present and running
- [ ] Environment variables verified
- [ ] Health endpoint tested: `GET /api/health`
- [ ] Database migrations verified
- [ ] Frontend URLs updated (if deployment URL changed)
- [ ] Team members have appropriate access
- [ ] GitHub integration re-linked (if needed)
- [ ] Documentation updated with new project location

## Need Help?

If you encounter issues:

1. Railway documentation: https://docs.railway.com/guides/projects
2. Railway support: https://railway.app/support
3. Check workspace permissions with Launchboard team

---

**Workspace Invite:** https://railway.com/workspace/invite/VlDBDmqtfDiHI4uNLcWru  
**Target Repository:** https://github.com/Launchboard-HQ/pangea-markets


