# Merge Development into master: quick start

For a normal release, use the **Promote Development to master** GitHub Actions workflow. Do not open a direct `Development` to `master` pull request: a direct merge can copy Development submodule pointers and Development Railway pins into production.

The purpose and output of every workflow are documented in [OASIS CI/CD, Tests, Artifacts, and Releases](./CI_CD_WORKFLOWS_AND_RELEASES.md).

Staging does not require another branch in each repository. The planned immutable-candidate staging gate,
environment-isolation rules and remaining automation work are documented in
[Staging release-candidate process](./DEVELOPMENT_TO_MASTER_PROMOTION.md#staging-release-candidate-process-planned-enhancement).
This is a follow-up design, not a gate already enforced by the current workflow.

## 0. Cut a release branch first (default)

Other people and agents keep pushing to `Development` while a release is being checked, and the promotion takes whatever `Development` is at the moment it runs. That moves the target under the checks, opens new submodule pull requests and forces rebuilds. So **promote a fixed snapshot, not the moving branch**:

1. Pick the `Development` commit you want to ship (normally the tip once CI is green).
2. Check every submodule commit it pins is already on that submodule's `main` (otherwise do step B first for those submodules).
3. Cut and tag the snapshot:

   ```bash
   git fetch origin Development
   git push origin origin/Development:refs/heads/release/YYYY-MM-DD
   git tag release-YYYY-MM-DD origin/Development && git push origin release-YYYY-MM-DD
   ```

4. From here on, only fixes needed to ship go on `release/YYYY-MM-DD`. Merge them back into `Development` afterwards. Everything else stays on `Development` for the next release.

Full rationale and rules: [Release branch snapshot](./DEVELOPMENT_TO_MASTER_PROMOTION.md#release-branch-snapshot-default).

## A. Start the promotion

1. Open the OASIS repository on GitHub.
2. Select **Actions**.
3. Select **Promote Development to master**.
4. Select **Run workflow**.
5. For **Use workflow from**, choose `master` (the trusted production copy of the workflow). If master's copy does not yet have the **Branch to promote** field, choose the `release/YYYY-MM-DD` branch instead.
6. Set **Branch to promote** to `release/YYYY-MM-DD`. Leave it as `Development` only if you deliberately want the moving tip.
7. Leave **Create or update promotion pull requests** selected and run it.

**Use workflow from** chooses which branch's workflow definition runs. **Branch to promote** (`source_ref`) chooses what is merged into the production branch. They are separate fields.

The workflow checks every submodule used by Development. If a required change has not reached that submodule's `main` branch, it opens a `Development` to `main` pull request in that repository and stops before changing OASIS production.

## B. Merge any submodule pull requests

1. Open the pull requests linked in the workflow summary.
2. Wait for their checks and review them.
3. Merge them into each submodule's `main` branch.
4. Run **Promote Development to master** again, with the same **Branch to promote**.

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
