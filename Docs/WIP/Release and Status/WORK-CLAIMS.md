# Work claims (who is working on what)

Every session, local or cloud, that works in the OASIS repo claims its work here first, so that no two sessions do the same thing.

## How to claim

Claims live on the **`claims` branch**, not on Development. Railway redeploys WEB4-WEB10 on every Development push, so claim-only pushes there restart the dev servers and fill the deploy queue.

1. `git fetch origin claims Development` and read `WORK-CLAIMS.md` on `origin/claims`, plus the status docs on Development:
   - `Docs/WIP/HyperDrive-Providers/CURRENT_STATUS.md`
   - `Docs/WIP/COSMIC ORM/` (the checklist and per-provider `*_CQL_MVP.md` files)
   - `Docs/WIP/Release and Status/ACTIONS-FOR-DAVID.md`
2. Pick an item that is not claimed (or whose claim is older than 24 hours with no commits since; mark that one `STALE` and take it over).
3. On a checkout of `claims`, add one row to the table, commit only this file with a message that starts `claim: <item> [skip ci]`, and push to `claims` **before** writing any code. If the push is rejected, pull, re-check, and pick again if someone else claimed it.
4. Work on your own branch (`work/<session>-<item>`), and merge to Development as usual. Batch your commits: one Development push per finished item, never a push just to retrigger CI.
5. When the item is done or blocked, change its row on `claims` (`DONE <commit>` or `BLOCKED: see ACTIONS-FOR-DAVID`) and take the next one.

Keep claims small (one provider, one checklist row or one feature), so that work spreads across sessions.

## Claims

| Item | Area | Session (local/cloud + name) | Branch | Claimed (UTC) | Status |
| --- | --- | --- | --- | --- | --- |
