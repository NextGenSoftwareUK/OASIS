# GitBook Auto-Sync Setup Guide

**Purpose:** Automatically sync OASIS documentation to GitBook  
**Date:** November 6, 2025

---

## Option 1: GitHub Integration (Recommended - Easiest)

GitBook can automatically sync with a GitHub repository.

### Setup Steps:

**1. Create GitBook Account:**
- Go to https://www.gitbook.com
- Sign up (free for open-source projects)

**2. Create New Space:**
- Click "New Space"
- Choose "Import from GitHub"
- Select your OASIS repository
- Choose branch: `main` or `master`

**3. Configure Documentation Path:**
In GitBook settings, specify where docs are:
- Primary docs: `/Docs/`
- API docs: `/Docs/Devs/API Documentation/`
- Guides: Root level `*.md` files

**4. Auto-Sync:**
GitBook will automatically:
- Pull latest commits every 10 minutes
- Re-render documentation
- Update live site

**Every time you commit:**
```bash
git add OASIS_PROVIDER_ARCHITECTURE_GUIDE.md
git commit -m "docs: update"
git push
# GitBook automatically syncs within 10 minutes
```

---

## Option 2: GitBook CLI (More Control)

Use the GitBook CLI for programmatic uploads.

### Installation:

```bash
npm install -g gitbook-cli
```

### Setup:

**1. Initialize GitBook in your repo:**
```bash
cd /Volumes/Storage/OASIS_CLEAN
gitbook init
```

This creates:
- `SUMMARY.md` - Table of contents
- `README.md` - Home page

**2. Create SUMMARY.md:**
```markdown
# Summary

## Getting Started
* [Introduction](README.md)
* [OASIS Provider Architecture](OASIS_PROVIDER_ARCHITECTURE_GUIDE.md)

## Core Documentation
* [HyperDrive Architecture](HYPERDRIVE_ARCHITECTURE_DIAGRAM.md)
* [Web4 Token System](WEB4_TOKEN_TECHNICAL_DEEP_DIVE_AND_VALUATION.md)
* [Web4 Ecosystem](WEB4_ECOSYSTEM_COMPLETE.md)

## Platform Guides
* [Web4 Token Platform](WEB4_TOKEN_PLATFORM_FINANCIAL_ANALYSIS.md)
* [HyperDrive Liquidity Pools](HYPERDRIVE_LIQUIDITY_POOLS_PLATFORM.md)
* [qUSDC Implementation](QUSDC_COMPLETE_ARCHITECTURE.md)

## API Documentation
* [API Endpoints Summary](OASIS_API_COMPLETE_ENDPOINTS_SUMMARY.md)
* [WEB4 OASIS API](Docs/Devs/API Documentation/WEB4_OASIS_API_Documentation_Comprehensive.md)

## Smart Contracts
* [Contract Generator](SmartContractGenerator/README.md)
* [Bridge Contracts](UniversalAssetBridge/contracts/BUILD_BRIDGE_CONTRACTS.md)

## Architecture Diagrams
* [Web4 HyperDrive Diagrams](WEB4_HYPERDRIVE_ARCHITECTURE_DIAGRAMS.md)
* [HyperDrive Q&A](WEB4_TOKENS_HYPERDRIVE_ARCHITECTURE_QUESTIONS.md)
```

**3. Build GitBook:**
```bash
gitbook build
# Creates _book/ folder with HTML
```

**4. Serve Locally (Test):**
```bash
gitbook serve
# View at http://localhost:4000
```

---

## Option 3: GitBook API (Full Automation)

Use GitBook's API to push docs programmatically.

### Setup:

**1. Get GitBook API Token:**
- GitBook → Settings → Developer Settings
- Create new token
- Save token securely

**2. Create Upload Script:**

```bash
#!/bin/bash
# upload-to-gitbook.sh

GITBOOK_TOKEN="your-token-here"
GITBOOK_SPACE_ID="your-space-id"

# Upload main provider guide
curl -X POST "https://api.gitbook.com/v1/spaces/${GITBOOK_SPACE_ID}/content" \
  -H "Authorization: Bearer ${GITBOOK_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "path": "provider-architecture.md",
    "content": "'$(cat OASIS_PROVIDER_ARCHITECTURE_GUIDE.md | jq -Rs .)'",
    "message": "Update provider architecture guide"
  }'

# Upload other docs
for doc in WEB4_*.md HYPERDRIVE_*.md QUSDC_*.md; do
  curl -X POST "https://api.gitbook.com/v1/spaces/${GITBOOK_SPACE_ID}/content" \
    -H "Authorization: Bearer ${GITBOOK_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{
      "path": "'$doc'",
      "content": "'$(cat $doc | jq -Rs .)'",
      "message": "Update documentation"
    }'
done

echo "✓ Documentation uploaded to GitBook"
```

**3. Make Executable:**
```bash
chmod +x upload-to-gitbook.sh
```

**4. Run Manually or Add to CI/CD:**
```bash
./upload-to-gitbook.sh
```

---

## Option 4: GitHub Actions (Fully Automated)

Set up automatic sync on every push to main branch.

### Create Workflow File:

**File:** `.github/workflows/sync-gitbook.yml`

```yaml
name: Sync to GitBook

on:
  push:
    branches:
      - main
    paths:
      - '**.md'  # Only trigger on markdown changes

jobs:
  sync:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout repository
        uses: actions/checkout@v3
      
      - name: Sync to GitBook
        uses: gitbook/publish-action@v1
        with:
          gitbook_token: ${{ secrets.GITBOOK_TOKEN }}
          space_id: ${{ secrets.GITBOOK_SPACE_ID }}
          docs_path: '.'  # Sync entire repo
          
      - name: Notify on completion
        run: echo "✓ Documentation synced to GitBook"
```

### Add Secrets to GitHub:

1. Go to GitHub repo → Settings → Secrets
2. Add `GITBOOK_TOKEN` (from GitBook settings)
3. Add `GITBOOK_SPACE_ID` (from GitBook space URL)

**Result:** Every commit automatically syncs to GitBook within minutes.

---

## Recommended Approach

**For your use case, I recommend Option 1 (GitHub Integration):**

**Why:**
- ✅ Zero configuration needed
- ✅ Automatic sync (no scripts to maintain)
- ✅ Works with your existing Git workflow
- ✅ Free for public repos
- ✅ No API tokens to manage

**Setup Time:** 5 minutes

**Steps:**
1. Create GitBook account
2. Click "Import from GitHub"
3. Select OASIS repository
4. Done - auto-syncs forever

**Your workflow becomes:**
```bash
# Edit documentation
vim OASIS_PROVIDER_ARCHITECTURE_GUIDE.md

# Commit as usual
git add .
git commit -m "docs: update"
git push

# GitBook automatically updates within 10 minutes
# Your friend sees updates at: https://yourspace.gitbook.io
```

---

## GitBook Structure Recommendation

**Create a clean docs structure:**

```
/Volumes/Storage/OASIS_CLEAN/
├── .gitbook.yaml (configuration)
├── README.md (home page)
└── docs/
    ├── SUMMARY.md (table of contents)
    ├── getting-started/
    │   ├── provider-architecture.md
    │   └── quickstart.md
    ├── api-reference/
    │   ├── avatar-api.md
    │   ├── data-api.md
    │   └── wallet-api.md
    ├── guides/
    │   ├── multi-chain-integration.md
    │   └── hyperdrive-guide.md
    └── architecture/
        ├── hyperdrive-diagrams.md
        └── web4-tokens.md
```

**Or keep current structure** and let GitBook organize automatically.

---

## Quick Start (Option 1):

**Right now, you can:**

1. Go to https://www.gitbook.com/signup
2. Sign up with GitHub account
3. Click "New Space" → "Import from GitHub"
4. Select your OASIS repo
5. Choose "main" branch
6. Select docs to publish (or all `.md` files)
7. Click "Import"

**Done!** Your docs are live and auto-syncing.

**Share with your friend:**
```
Documentation: https://oasis.gitbook.io
API Endpoint: https://api.oasisweb4.one
Contact: @maxgershfield on Telegram
```

---

## Cost

**GitBook Pricing:**
- **Free:** Public documentation (perfect for open-source)
- **Plus ($6.70/month):** Private docs, custom domain
- **Pro ($12.50/month):** Teams, advanced features
- **Enterprise:** Custom pricing

**For open-source projects like OASIS: FREE ✓**

---

**Want me to create the `.gitbook.yaml` and `SUMMARY.md` files to get you started?**

