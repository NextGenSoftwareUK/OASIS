# Create branch: conflict message and/or new branch based on wrong (old) commit

## Summary

When creating a new branch from the current branch (master) with "bring uncommitted changes to the new branch", GitHub Desktop showed a **conflict** involving a few files. Creating a branch in Git is just adding a new ref at the current commit—there is no merge—so a conflict in that flow was unexpected. After the branch was created, the new branch also did **not** contain commits that were on local master; it was effectively based on an older commit (e.g. `origin/master` or another ref), so recent work appeared "missing" on the new branch.

## Steps to reproduce

1. Be on **master** with several commits ahead of `origin/master` (unpushed).
2. Have some uncommitted changes (optional).
3. In GitHub Desktop: **Create branch** (e.g. name: `feature-branch`).
4. When prompted, choose to **bring uncommitted work** to the new branch (yes).
5. Observe: GitHub Desktop reports a **conflict** for one or more files during this flow.
6. After the branch is created: `git log master..feature-branch` is empty and `git log feature-branch..master` shows many commits—i.e. the new branch was not created from the tip of local master.

## Expected behavior

- **No conflict** when only creating a branch (no merge is performed).
- The new branch should be created from the **current** commit (tip of the branch I was on, i.e. local master), so all commits that were on master are also on the new branch.

## Actual behavior

- A **conflict** was shown during "create branch" (confusing, since branch creation alone does not involve a merge).
- The new branch was based on an **older** commit (likely `origin/master` or another ref), not the tip of local master, so recent work was not on the new branch.

## Environment

- **OS:** Windows
- **GitHub Desktop version:** [please fill in: Help → About GitHub Desktop]
- **Repo:** Local repo with local `master` ahead of `origin/master` (unpushed commits).

## Related issues

- **Wrong base when creating branch:** [#6085 – Creating branch from detached HEAD branches from master](https://github.com/desktop/desktop/issues/6085) (similar outcome: new branch not based on the commit the user thought).
- **Conflict UI / merge checker:** [#17420 – "Merge into current branch" conflict checker out of sync](https://github.com/desktop/desktop/issues/17420) (possible confusion between "create branch" and merge/conflict state).

## Note

This may be a combination of (1) the app doing more than a plain "create branch" (e.g. syncing with remote) and showing conflict from that, and/or (2) using `origin/master` (or another ref) instead of the current branch’s HEAD when creating the new branch. Either way, the UX was misleading: "create branch" shouldn’t show a conflict, and the new branch should be created from the commit the user is on.

Thank you for maintaining GitHub Desktop.
