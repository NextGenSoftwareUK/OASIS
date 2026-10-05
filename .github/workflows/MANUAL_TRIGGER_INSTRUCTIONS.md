# Manual Workflow Trigger Instructions

If the workflow doesn't auto-trigger, you can manually trigger it:

## Method 1: GitHub UI (Easiest)

1. Go to your GitHub repository
2. Click **Actions** tab
3. In the left sidebar, click **"Build Scrypto Package"** workflow
4. Click **"Run workflow"** button (top right, next to "Filter workflow runs")
5. Select branch: **`max-build2`**
6. Click green **"Run workflow"** button

## Method 2: Via Commit (What we just did)

I've created a trigger commit that should start the workflow automatically. Check the Actions tab in a minute or two.

If it still doesn't appear, use Method 1 above.

## Verify the Workflow is Running

After triggering, you should see:
- A new workflow run appear at the top of the list
- Commit should be `d7b71b27` or newer
- Status will show "Queued" or "In progress"
- It should complete in 5-15 minutes



