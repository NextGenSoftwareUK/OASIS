# Branch cleanup runbook

Keeps the OASIS parent repo and every submodule down to `Development`, `master`/`main` and branches still in use. Run it after a batch of feature work lands, before a release, or whenever `git branch -r` gets long.

## Quick start

```bash
Scripts/branch_cleanup.sh            # dry run: lists what is safe to delete and what needs review
Scripts/branch_cleanup.sh --apply    # deletes the safe branches (remote and local)
```

On Windows, double-click `Scripts\branch_cleanup.bat` for a dry run (the window stays open), or run it from a command prompt with the same options, for example `Scripts\branch_cleanup.bat --apply`. It runs the same script through Git Bash; Git for Windows must be installed.

Options: `--remote-only`, `--local-only`. Extra branches to protect: `KEEP_BRANCHES='codex/live-work|feature/x' Scripts/branch_cleanup.sh`.

Exit codes: `0` nothing needs review, `3` some branches hold work not in `Development` (listed at the end), `2` bad option.

## What the script decides on its own

A branch is deleted with `--apply` only when its work is provably preserved:

| Class | Meaning | Applies to |
| --- | --- | --- |
| merged | Tip is an ancestor of `origin/Development` | remote and local |
| identical | Every commit has an identical patch in `origin/Development` (rebased or cherry-picked work) | remote and local |
| preserved | Tip is contained in a remote branch or tag (for example `master` or `archive/*`) | local only |

Never deleted: `Development`, `master`, `main`, `promotion/*`, `submodule-sync/*`, `archive/*`, any branch checked out in a worktree of the parent or any submodule (protected by name in every repo, because agents use the same branch name across them), and anything in `KEEP_BRANCHES`.

## What needs a person: the review list

Branches listed under "work not in Development" have commits that exist nowhere in `Development`. For each one:

1. See what it adds: `git log origin/Development..origin/<branch>` and `git diff --stat origin/Development...origin/<branch>`.
2. Check whether the work already landed in a different form (rebased, reworked, moved to a submodule or to another repo). Search for the files, classes or endpoints it adds. Old branches often show large commit counts only because their history was rewritten.
3. If it is new and useful, merge it into `Development`, build and run the affected tests, then push. Advance the parent gitlink and `Docker/oasis-dependency-versions.env` together when a submodule moves, and run `python Scripts/validate_railway_dependency_manifest.py --require-gitlinks`.
4. If it is superseded, stubbed or unwanted, keep its history and delete the branch:
   ```bash
   git tag archive/<branch> origin/<branch>
   git push origin archive/<branch>
   git push origin --delete <branch>
   ```
5. Re-run the dry run until it exits `0`.

Whole projects that belong in their own repository: create a private repo from the folder snapshot (`git archive <ref>:<folder>`), or push an import branch to an existing repo, before deleting the branch that held them.

## Rules that apply while doing this

- Merge into `Development` first; production reaches `master` only through the promotion process (`DEVELOPMENT_TO_MASTER_PROMOTION.md`).
- Never force-push and never delete a branch another agent is working on; check `git worktree list` and recent activity first.
- Other contributors' uncommitted edits stay untouched; commit only the files you changed.
- Report what was done in `Docs/WIP/` (example: `Docs/WIP/BRANCH_CLEANUP_AND_PROVIDER_KEY_FIXES_2026-10-20.md`).
