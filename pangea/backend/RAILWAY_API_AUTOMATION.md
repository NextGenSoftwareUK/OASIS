# Railway API & Automation Guide

## Overview

Railway doesn't have a publicly documented REST API, but provides API tokens that work with their CLI. The CLI uses Railway's GraphQL API under the hood.

## API Token Types

### 1. Project Token
- **Scope**: Single project and environment
- **Use Case**: CI/CD pipelines, automated deployments for specific project
- **Limitations**: Can't create projects or link to workspaces
- **Good for**: Deploying code, viewing logs, redeploying

### 2. Account Token (Personal)
- **Scope**: All your projects and workspaces
- **Use Case**: Managing multiple projects, creating new projects
- **Limitations**: None (full account access)
- **Good for**: Full automation across projects

### 3. Team Token
- **Scope**: Team workspace only
- **Use Case**: Team-wide automation
- **Limitations**: Limited to team workspace actions
- **Good for**: Team CI/CD pipelines

## Getting API Tokens

### Project Token
1. Go to Railway dashboard
2. Select your project
3. Settings → Tokens
4. Click "New Token"
5. Select "Project Token"
6. Copy the token (save securely, shown only once)

### Account Token
1. Go to Railway dashboard
2. Click your profile → Settings
3. Go to "Tokens" section
4. Click "New Token"
5. Select "Personal Account Token"
6. Copy the token

## Using Tokens

### Environment Variables
```bash
# For project-specific actions
export RAILWAY_TOKEN=your_project_token

# For account-level actions
export RAILWAY_API_TOKEN=your_account_token
```

**Note**: Only set one at a time. `RAILWAY_TOKEN` takes precedence.

### In CI/CD (GitHub Actions Example)

```yaml
# .github/workflows/deploy.yml
name: Deploy to Railway

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
      
      - name: Install Railway CLI
        run: npm install -g @railway/cli
      
      - name: Deploy to Railway
        env:
          RAILWAY_TOKEN: ${{ secrets.RAILWAY_TOKEN }}
        run: |
          railway up
```

**GitHub Secrets Setup**:
1. Go to your repo → Settings → Secrets and variables → Actions
2. Add new secret: `RAILWAY_TOKEN`
3. Paste your Railway project token

## Common CLI Commands for Automation

### Deployment
```bash
# Deploy current directory
railway up

# Deploy with specific environment
railway up --environment production

# Deploy from specific directory
railway up --cwd pangea/backend
```

### Managing Deployments
```bash
# View deployments
railway status

# Redeploy latest
railway redeploy

# View logs
railway logs

# View logs with tail
railway logs --tail
```

### Environment Variables
```bash
# Set environment variable
railway variables set KEY=value

# Get environment variables
railway variables

# Unset variable
railway variables unset KEY
```

### Database Operations
```bash
# Open database shell
railway connect postgres

# Run migrations
railway run npm run migration:run
```

## Advanced: Direct GraphQL API

Railway uses GraphQL internally, but it's not officially documented. The CLI uses these endpoints. If you need direct API access:

### Option 1: Use CLI (Recommended)
The CLI handles authentication and GraphQL queries for you.

### Option 2: Inspect CLI Traffic (Not Recommended)
You can inspect the CLI's network requests, but this is:
- Unstable (API might change)
- Not officially supported
- Complex to implement

### Option 3: Contact Railway Support
If you have a specific use case, contact Railway support about API access needs.

## Example: Automated Deployment Script

```bash
#!/bin/bash
# deploy.sh

set -e

# Set Railway token
export RAILWAY_TOKEN="${RAILWAY_TOKEN}"

# Install Railway CLI if not present
if ! command -v railway &> /dev/null; then
  npm install -g @railway/cli
fi

# Navigate to backend directory
cd pangea/backend

# Run tests (optional)
# npm test

# Build locally to catch errors early
npm install
npm run build

# Deploy to Railway
echo "Deploying to Railway..."
railway up

# Check deployment status
echo "Checking deployment status..."
railway status

# Show recent logs
echo "Recent logs:"
railway logs --tail 50
```

## Example: Environment Sync Script

```bash
#!/bin/bash
# sync-env.sh - Sync .env.local to Railway

export RAILWAY_TOKEN="${RAILWAY_TOKEN}"

# Read .env.local and set Railway variables
while IFS='=' read -r key value; do
  # Skip comments and empty lines
  [[ $key =~ ^#.*$ ]] && continue
  [[ -z "$key" ]] && continue
  
  # Remove quotes from value
  value=$(echo "$value" | sed 's/^"\(.*\)"$/\1/')
  
  echo "Setting $key..."
  railway variables set "$key=$value"
done < .env.local

echo "Environment variables synced!"
```

## Security Best Practices

1. **Never commit tokens**: Add to `.gitignore` or use secrets management
2. **Use least privilege**: Use Project Tokens instead of Account Tokens when possible
3. **Rotate regularly**: Regenerate tokens periodically
4. **Use environment variables**: Don't hardcode tokens in scripts
5. **Limit scope**: Use Project Tokens for specific projects, not Account Tokens

## Troubleshooting

### "Token not found"
- Verify token is set: `echo $RAILWAY_TOKEN`
- Check token hasn't expired
- Ensure correct token type for the operation

### "Permission denied"
- Project Token can't create projects (use Account Token)
- Team Token can't access personal projects
- Verify token has correct permissions

### CLI not found
```bash
npm install -g @railway/cli
# or
yarn global add @railway/cli
```

## Alternative: Webhooks

Railway supports webhooks for deployment events. You can:
- Trigger deployments on Git pushes
- Get notified of deployment status
- Integrate with other services

Set up in Railway dashboard → Project Settings → Webhooks

## Summary

For most use cases, **Railway CLI with API tokens** is the way to go:
- ✅ Well documented
- ✅ Stable API surface
- ✅ Handles authentication
- ✅ Works great in CI/CD

Direct GraphQL API access is not recommended unless you have a specific need and coordinate with Railway support.








