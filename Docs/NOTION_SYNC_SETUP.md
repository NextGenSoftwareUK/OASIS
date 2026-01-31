# Notion Sync Setup — Auto-push Markdown to Notion

This repo can **sync Markdown files to Notion automatically** on every push, so you don’t have to manually import. Only files that opt in (via frontmatter) are synced.

---

## How it works

1. **GitHub Action** runs on push to `main`, `master`, or `max-build4` when files under `Docs/` or any `.md` file changes.
2. The action scans **commits** for changed Markdown files that contain **Notion frontmatter**.
3. For each such file, it **updates** the linked Notion page (or creates content under it).

So: **edit Markdown in the repo → push → Notion stays in sync.** No manual import.

---

## One-time setup

### 1. Create a Notion integration

1. Go to [Notion → My integrations](https://www.notion.so/my-integrations).
2. Click **New integration**.
3. Name it (e.g. “OASIS Docs Sync”), choose your workspace.
4. Under **Capabilities**, enable **Read content**, **Update content**, **Insert content**.
5. Click **Submit**. Copy the **Internal Integration Secret** (starts with `secret_`). This is your `NOTION_TOKEN`.

### 2. Add the token to GitHub

1. In your GitHub repo: **Settings → Secrets and variables → Actions**.
2. **New repository secret**: name = `NOTION_TOKEN`, value = the integration secret from step 1.

### 3. Create Notion pages and share with the integration

For each Markdown file you want to sync:

1. In Notion, **create a page** that will hold the synced content (e.g. “Holonic Data Centre & Real Proof”).
2. Open that page → **⋯** (top right) → **Connections** → **Connect to** → select your integration (e.g. “OASIS Docs Sync”).  
   The integration must have access to the page or it can’t update it.

### 4. Add frontmatter to the Markdown files you want to sync

At the **top** of the file, add a YAML block with `notion_page` (the Notion URL of the page) and optionally `title`:

```yaml
---
notion_page: https://www.notion.so/YourWorkspace/Your-Page-2f41dc4d449b808b84d1f86abb357b43
title: Your Page Title
---
```

- **`notion_page`** — Full URL of the Notion page (from the browser). Required for sync.
- **`title`** — Optional; can be used as the page title in Notion.

**Example:** To sync `Docs/HOLONIC_DATA_CENTRE_AI_AND_REAL_PROOF.md` to a Notion page:

1. Create that page in Notion and share it with your integration.
2. Copy the page URL (e.g. `https://www.notion.so/DEVELOPMENT_PLAN-2f41dc4d449b808b84d1f86abb357b43`).
3. Add at the very top of the Markdown file:

```yaml
---
notion_page: https://www.notion.so/YourWorkspace/Holonic-Data-Centre-Real-Proof-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
title: Holonic Data Centre, Holonic AI, and Real Proof
---
```

Save, commit, and push. The workflow will run and push the content to that Notion page.

---

## Which files get synced?

Only Markdown files that:

1. Were **changed** in the push, and  
2. Contain **frontmatter** with a **`notion_page`** key  

are sent to Notion. Files without that frontmatter are ignored, so you can mix synced and non-synced docs in the same repo.

---

## Workflow file

The action is defined in:

- **`.github/workflows/push-markdown-to-notion.yml`**

It runs on push to `main`, `master`, or `max-build4` when files under `Docs/` or any `*.md` change. To restrict to only `Docs/`, you can narrow the `paths` in that file.

---

## Limitations

- **Notion API limits** apply (rate limits, block size). Very large docs might need to be split.
- **Mermaid diagrams** in Markdown are converted to Notion blocks where possible; Notion doesn’t render Mermaid natively, so they may appear as code blocks. For full diagrams, you can keep using Mermaid in Notion via a Mermaid embed or export from the repo.
- The action uses [JoshStern/push-md-to-notion](https://github.com/marketplace/actions/push-markdown-to-notion) (third-party). If the action changes, you may need to update the workflow version.

---

## Troubleshooting

| Problem | Check |
|--------|--------|
| Nothing syncs | File has `notion_page` in frontmatter? Page is shared with the integration? `NOTION_TOKEN` secret set in GitHub? |
| “Unauthorized” | Integration has **Read / Update / Insert**; page is **connected** to the integration (Connections). |
| Wrong page updated | `notion_page` URL must be the exact page you want; copy from the browser. |
| Sync only some docs | Add frontmatter only to the files you want; leave others without `notion_page`. |

---

## Optional: Sync only on demand

To run the sync only when you choose (e.g. from the Actions tab):

1. In `.github/workflows/push-markdown-to-notion.yml`, add:

```yaml
on:
  workflow_dispatch:
```

2. Keep or remove the `push` trigger. With `workflow_dispatch` you can run “Push Markdown to Notion” from the **Actions** tab anytime.

You can have both `push` and `workflow_dispatch` so that sync runs on push and can also be triggered manually.
