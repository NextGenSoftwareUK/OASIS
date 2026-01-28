# Publishing the New Docs to GitBook

This guide explains how to publish the restructured docs in `Docs/Devs/docs-new/` to **https://oasis-web4.gitbook.io/oasis-web4-docs/**.

GitBook publishes from a **Git repository** via [GitHub Sync](https://docs.gitbook.com/getting-started/git-sync/enabling-github-sync). You need to get this folder’s content into the repo (and branch) that the space **oasis-web4-docs** is connected to, then let GitBook sync.

---

## Replacing the current docs (already configured)

The repo is set up so GitBook uses **only** the new docs:

- **`.gitbook.yaml`** at the **repository root** (next to `Docs/`, `ONODE/`, etc.) sets:
  - `root: ./Docs/Devs/docs-new/` — so GitBook reads content only from the new docs folder.
  - `structure.readme: index.md` and `structure.summary: SUMMARY.md` — homepage and sidebar.

After you **push this repo** to the branch that the GitBook space **oasis-web4-docs** is synced to, GitBook will build the site from the new docs and the live site will show the new content (replacing the old structure).

**What you need to do:**

1. Commit and push the repo (including the root `.gitbook.yaml` and all of `Docs/Devs/docs-new/`) to the branch GitBook watches (e.g. `main`, `master`, or `max-build2`).
2. In GitBook, if it doesn't update automatically: open the space → **Configure** → **GitHub Sync** → **Sync from GitHub** (or equivalent).
3. Open https://oasis-web4.gitbook.io/oasis-web4-docs/ and confirm the new "Choose Your Starting Point" homepage and WEB4/WEB5 API sidebar.

If the GitBook space is connected to a **different** repo (e.g. a dedicated docs repo), see the steps below to point that repo at the new docs or copy `docs-new` into it.

---

## Step 1: Find the repo connected to GitBook

1. Open **https://app.gitbook.com** and go to the space that publishes to **oasis-web4.gitbook.io/oasis-web4-docs**.
2. In the top-right, click **Configure** (or the space settings).
3. Open **GitHub Sync** (or **Integrations** → GitHub).
4. Note:
   - **Repository** (e.g. `NextGenSoftwareUK/OASIS` or a dedicated docs repo).
   - **Branch** (e.g. `main`, `master`, or `max-build2`).
   - **Root path** (if set), e.g. `docs/` or empty for repo root.

You’ll use this repo and branch in the next steps.

---

## Step 2: Choose how to put the new docs in that repo

### Option A: This repo is the main OASIS repo (e.g. `NextGenSoftwareUK/OASIS`)

The new docs already live under `Docs/Devs/docs-new/`. You only need to:

1. **Point GitBook at this folder**
   - In GitBook’s GitHub Sync (or content) settings, set **Root** to:
     - `Docs/Devs/docs-new`
   - If the UI doesn’t allow a subfolder, add a **`.gitbook.yaml`** at the **repository root** with:
     ```yaml
     root: ./Docs/Devs/docs-new/
     structure:
       readme: index.md
       summary: SUMMARY.md
     ```
   - Commit and push that `.gitbook.yaml` on the branch GitBook watches.

2. **Commit and push** any recent changes under `Docs/Devs/docs-new/` (including `SUMMARY.md`, `index.md`, `.gitbook.yaml` in docs-new, etc.) to that same branch.

3. **Trigger sync**
   - GitBook usually syncs on push. If not, use “Sync” or “Sync from GitHub” in the GitBook space so it pulls the latest commit.

Result: the live site will render from `Docs/Devs/docs-new/` with the new sidebar (from `SUMMARY.md`) and homepage (`index.md`).

---

### Option B: GitBook is synced to a separate “docs” repo

If the space is connected to a repo that only holds docs (e.g. `oasis-web4-docs`):

1. **Clone that repo** (and the branch GitBook uses):
   ```bash
   git clone <docs-repo-url>
   cd <docs-repo-name>
   git checkout <branch>
   ```

2. **Copy the new docs in**
   - Replace the repo’s content with the contents of `Docs/Devs/docs-new/`, **or**
   - Copy everything from `Docs/Devs/docs-new/` into the repo root (or into the subfolder that GitBook uses as root).

3. **Keep GitBook config**
   - Ensure the repo has:
     - `index.md` (homepage)
     - `SUMMARY.md` (table of contents)
     - `.gitbook.yaml` (optional; same as in docs-new):
       ```yaml
       structure:
         readme: index.md
         summary: SUMMARY.md
       ```

4. **Commit and push**
   ```bash
   git add .
   git commit -m "Publish restructured OASIS docs (docs-new)"
   git push origin <branch>
   ```

5. In GitBook, run **Sync from GitHub** if it doesn’t update automatically.

---

## Step 3: First-time sync direction (if the space is new or empty)

When enabling GitHub Sync for the first time, GitBook asks:

- **GitHub → GitBook**: Import existing Markdown from the repo (use this when the repo already has the new docs).
- **GitBook → GitHub**: Push GitBook’s current content to the repo (use only if you want to overwrite the repo with GitBook’s current content).

For publishing **these** new docs, the repo should already contain the contents of `docs-new`, so choose **GitHub → GitBook** (or equivalent) so the site is built from the repo.

---

## Step 4: Check the live site

- Open **https://oasis-web4.gitbook.io/oasis-web4-docs/**.
- Confirm:
  - Homepage is “Choose Your Starting Point” (`index.md`).
  - Sidebar matches `SUMMARY.md` (Getting Started, WEB4 API, WEB5 STAR API, Reference, etc.).
  - Links to Avatar API, NFT API, Wallet API, etc. work.

---

## What’s already in `docs-new` for GitBook

| Item | Purpose |
|------|--------|
| `index.md` | Homepage (“Choose Your Starting Point”). |
| `SUMMARY.md` | GitBook table of contents (sidebar). |
| `.gitbook.yaml` | Tells GitBook to use `index.md` as readme and `SUMMARY.md` as summary when this folder is the root. |
| All other `.md` under `docs-new` | API docs, guides, concepts, reference. |

Relative links in the markdown files are written for this folder structure; they work as-is when GitBook’s root is set to this folder (or when this folder’s content is at the repo root).

---

## Troubleshooting

- **Sidebar wrong or missing**  
  Ensure GitBook’s root directory contains `SUMMARY.md` and that `structure.summary` in `.gitbook.yaml` (if used) points to `SUMMARY.md`.

- **Homepage is wrong**  
  Set `structure.readme` to `index.md` (and ensure the root is `Docs/Devs/docs-new` or the folder that contains `index.md`).

- **Broken links**  
  Links are relative (e.g. `web4-oasis-api/overview.md`). They only work if the root is this folder (or the same structure in the synced repo).

- **GitBook doesn’t update**  
  Push to the correct branch and use “Sync from GitHub” in the space. Check [GitBook’s troubleshooting](https://docs.gitbook.com/getting-started/git-sync/troubleshooting) if sync still fails.

---

## References

- [GitBook: Enabling GitHub Sync](https://docs.gitbook.com/getting-started/git-sync/enabling-github-sync)
- [GitBook: Content configuration (root, SUMMARY)](https://docs.gitbook.com/getting-started/git-sync/content-configuration)
- [GitBook: Setting a custom subdirectory](https://docs.gitbook.com/publishing-documentation/setting-a-custom-subdirectory)
