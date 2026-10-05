# GitBook Upload Workflow (Git Sync via GitHub)

## Why This Exists

Our GitBook space (`oasis-web4-docs`) is wired to the GitHub branch `max-build2`.  
Whenever we push documentation commits to that branch, GitBook automatically ingests
the changes within a few minutes.  
This document captures the exact workflow, tooling, and tips to keep the sync
running smoothly.

---

## Quick Checklist

1. Make sure your local repo is on `max-build2` and up to date.
2. Edit or add Markdown docs.
3. Stage and push using the helper script:
   ```bash
   ./upload-docs-to-gitbook.sh
   ```
4. Wait for GitBook to pull the commit (typically <10 minutes).
5. Verify at <https://oasis-web4.gitbook.io/oasis-web4-docs/>.

---

## Helper Script: `upload-docs-to-gitbook.sh`

The script lives at the repo root and wraps the standard Git flow:

```bash
./upload-docs-to-gitbook.sh
```

- Ensures we are inside the repository.
- Confirms we are on the expected branch (default `max-build2`).
- Stages a curated list of key documentation files.
- Commits with a default message `docs: sync with GitBook`.
- Pushes to `origin/max-build2`.
- GitBook (already connected to that branch) syncs automatically.

### Configuration Options

- `GITBOOK_BRANCH`: override the branch (default `max-build2`).
- `GITBOOK_REMOTE`: override the remote (default `origin`).
- `GITBOOK_COMMIT_MESSAGE`: customize the commit message.
- `GITBOOK_DOCS`: space-separated list of files to stage.
- `GITBOOK_DOCS_FILE`: path to a file containing one doc path per line  
  (takes precedence over `GITBOOK_DOCS`).

Example: upload a single document with a custom message.

```bash
GITBOOK_DOCS="Docs/OASIS_COMPLIANCE_SOLUTIONS.md" \
GITBOOK_COMMIT_MESSAGE="docs: add compliance solutions" \
./upload-docs-to-gitbook.sh
```

---

## Adding New Docs

1. Create the Markdown file under an appropriate folder (e.g., `Docs/`, `x402/`).
2. Run the helper script. If the new file is outside the default list, pass it via
   `GITBOOK_DOCS` or `GITBOOK_DOCS_FILE`.
3. Commit will include the new file (script automatically stages it).
4. Push triggers GitBook sync.

---

## Updating the Doc List

The default list in the script includes core platform docs and the latest X402 materials.
If you find yourself repeatedly staging additional files, update the array in
`upload-docs-to-gitbook.sh` or create a text file and reuse `GITBOOK_DOCS_FILE`.

---

## Verifying GitBook Sync

1. Open <https://oasis-web4.gitbook.io/oasis-web4-docs/>.
2. Look for the updated page and confirm the timestamp near the page title.
3. If the change is missing after ~10 minutes:
   - Confirm the commit landed on `max-build2` (`git log` / GitHub UI).
   - Check if GitBook reports sync errors (Space → Settings → Integrations → GitHub).
   - Re-run the script if needed.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
| --- | --- | --- |
| Script exits with branch warning | You are not on `max-build2` | `git checkout max-build2` |
| Git push rejected | Remote has new commits | `git pull --rebase origin max-build2`, then re-run script |
| GitBook doesn’t reflect updates | Sync still running or failed | Wait a few minutes, check GitBook integration logs |
| Need to upload different docs | Not in default list | Use `GITBOOK_DOCS`/`GITBOOK_DOCS_FILE` overrides |

---

## Reference Links

- GitBook Space: <https://oasis-web4.gitbook.io/oasis-web4-docs/>
- GitBook Git info API (for debugging):
  ```bash
  curl -s -H "Authorization: Bearer $GITBOOK_TOKEN" \
    https://api.gitbook.com/v1/spaces/d9PSTWwwb6Zr3tz3I1tY/git/info | jq
  ```
- Helper script source: `upload-docs-to-gitbook.sh`

Keep this document up to date as the integration evolves.


