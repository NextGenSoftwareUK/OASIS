# How to Trigger the Workflow Manually

## Issue
The old workflow run (de8d6e0) appears, but the new commit (4dfdacac) isn't showing up because we need to manually trigger it.

## Steps to Trigger a New Run

### Step 1: Go to Workflow (Not the Run List)

1. Go to your GitHub repository
2. Click **Actions** tab (top navigation)
3. In the left sidebar, find and click **"Build Scrypto Package"** workflow name

**Important**: You need to click on the workflow NAME in the sidebar, not on the individual run!

### Step 2: Trigger New Run

1. You should see a **"Run workflow"** button on the right side (next to "Filter workflow runs")
2. Click **"Run workflow"** dropdown button
3. Select branch: **`max-build2`**
4. Click the green **"Run workflow"** button

### Step 3: Verify New Run Started

1. The page should refresh
2. You should see a new workflow run appear at the top of the list
3. The commit should be `4dfdacac` (your latest commit)
4. Status should show "Queued" or "In progress"

## Visual Guide

```
GitHub Repository
  └─ Actions Tab
      └─ Left Sidebar
          └─ [Build Scrypto Package] ← Click HERE (the workflow name)
              └─ Right side: [Run workflow ▼] ← Click this button
                  └─ Select branch: max-build2
                  └─ Click "Run workflow"
```

## Alternative: Trigger via Push

If manual trigger doesn't work, you can also trigger it by making a small change to a contract file:

```bash
cd Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/contracts
touch src/lib.rs  # Just touch the file (no actual change)
cd /Volumes/Storage/OASIS_CLEAN
git add .
git commit -m "Trigger workflow"
git push origin max-build2
```

This will trigger the workflow because it's watching for changes to contract files.



