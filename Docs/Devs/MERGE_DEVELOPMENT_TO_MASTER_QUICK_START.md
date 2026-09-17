# Merge Development into master: quick start

For a normal release, use the **Promote Development to master** GitHub Actions workflow. Do not open a direct `Development` to `master` pull request: a direct merge can copy Development submodule pointers and Development Railway pins into production.

## A. Start the promotion

1. Open the OASIS repository on GitHub.
2. Select **Actions**.
3. Select **Promote Development to master**.
4. Select **Run workflow**, leave **Create or update promotion pull requests** selected, and run it.

The workflow checks every submodule used by Development. If a required change has not reached that submodule's `main` branch, it opens a `Development` to `main` pull request in that repository and stops before changing OASIS production.

## B. Merge any submodule pull requests

1. Open the pull requests linked in the workflow summary.
2. Wait for their checks and review them.
3. Merge them into each submodule's `main` branch.
4. Run **Promote Development to master** again.

If no submodule promotion is required, the first run proceeds directly to step C.

## C. Merge the generated OASIS promotion pull request

The successful workflow run opens or updates one pull request named **Promote Development to master**. It has already:

- merged the parent Development changes onto a branch based on `master`;
- kept production submodules on `main`;
- regenerated the Railway manifest from those exact commits; and
- run the branch and manifest policy checks.

Wait for the pull request checks, review it, and merge it. Railway then deploys `master`. Confirm the production deployment and affected hosted endpoints are healthy.

That is the normal process: **run workflow → merge linked submodule PRs if shown → rerun workflow → merge the generated OASIS PR**.

For conflict resolution, manual commands, rollback, and the exact invariants enforced by the workflow, see [Promoting OASIS from Development to master](./DEVELOPMENT_TO_MASTER_PROMOTION.md).
