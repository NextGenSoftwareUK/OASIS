# Railway Deployment Troubleshooting

## Common Issues and Fixes

### Issue 1: Root Directory Not Set

**Problem**: Railway is trying to build from the repo root, but your backend is in a subdirectory.

**Fix**: 
1. Go to your backend service in Railway dashboard
2. Click "Settings" → "Source"
3. Set **Root Directory** to: `pangea/backend`
4. Save and redeploy

### Issue 2: Build Command Not Configured

**Problem**: Railway isn't running the build command correctly.

**Fix**:
1. Go to Settings → "Deploy"
2. Set **Build Command**: `npm install && npm run build`
3. Set **Start Command**: `npm run start:prod`
4. Save and redeploy

### Issue 3: Node Version Not Set

**Problem**: Railway might be using the wrong Node version.

**Fix**:
1. Go to Settings → "Variables"
2. Add environment variable: `NODE_VERSION=18` (or `20`)
3. Or create a `.nvmrc` file in `pangea/backend/` with:
   ```
   18
   ```

### Issue 4: Missing Environment Variables Causing Build Fail

**Problem**: Build might fail if certain env vars are required at build time.

**Fix**: Make sure you've added at least these for the build to work:
- `NODE_ENV=production`
- Database/Redis vars can be added after build (they're runtime, not build-time)

### Issue 5: TypeScript Build Errors

**Problem**: TypeScript compilation errors.

**Fix**:
1. Test build locally first: `cd pangea/backend && npm run build`
2. Fix any TypeScript errors
3. Check Railway logs for specific error messages

### Issue 6: Missing Dependencies

**Problem**: Some dependencies might be in devDependencies that are needed for production.

**Check**: Make sure `@nestjs/cli` and TypeScript are in `dependencies` if needed for build, or ensure build doesn't require them.

---

## Step-by-Step Fix

### 1. Check Railway Logs

**Most Important**: Look at the deployment logs to see the actual error:

1. In Railway dashboard, click on your service
2. Click "Deployments"
3. Click on the failed deployment
4. Click "View Logs"
5. Scroll to see the error message

Common errors you might see:
- `package.json not found` → Root directory issue
- `command not found: nest` → Build command issue
- `Cannot find module` → Missing dependencies
- `Type error` → TypeScript compilation error

### 2. Verify Root Directory

If your repo structure is:
```
your-repo/
  └── pangea/
      └── backend/
          ├── package.json
          ├── src/
          └── ...
```

Then set **Root Directory** to: `pangea/backend`

### 3. Test Build Locally

Before deploying, test that it builds:

```bash
cd pangea/backend
npm install
npm run build
```

If this fails locally, it will fail on Railway too. Fix local issues first.

### 4. Verify package.json Scripts

Your `package.json` should have:
```json
{
  "scripts": {
    "build": "nest build",
    "start:prod": "node dist/main"
  }
}
```

### 5. Check for .env Issues

Make sure `.env` files aren't required for build (they shouldn't be).

---

## Quick Fix Checklist

- [ ] Root Directory set to `pangea/backend` in Railway settings
- [ ] Build Command: `npm install && npm run build`
- [ ] Start Command: `npm run start:prod`
- [ ] NODE_VERSION set to 18 or 20
- [ ] Build works locally (`npm run build`)
- [ ] Checked Railway logs for specific error
- [ ] No TypeScript errors in code
- [ ] All dependencies are in `package.json`

---

## What to Share for Help

If you need more help, share:
1. The error message from Railway logs
2. Your repo structure (where is backend located?)
3. The Root Directory you've set
- Whether `npm run build` works locally








