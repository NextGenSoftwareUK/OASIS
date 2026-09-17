# Promoting OASIS from Development to master

This is the release process for promoting the OASIS parent repository and its submodules from development into production. It keeps the source graph tested locally, built by Railway, and recorded by the parent repository identical.

## Release invariant

| Parent branch | Submodule branch | Railway manifest |
|---|---|---|
| `Development` | `Development` | Must equal the parent branch's private submodule gitlinks. |
| `master` | `main` | Must equal the parent branch's private submodule gitlinks. |

Production must never point at a submodule's `Development` commit. Each changed submodule is promoted to its `main` branch first. The parent `master` pointer and `Docker/oasis-dependency-versions.env` then advance together in one reviewed pull request.

## Promotion sequence

1. Confirm the OASIS `Development` branch is green, its Railway development deployment succeeded, and the changed hosted contracts work.
2. Identify submodules changed since production:

   ```bash
   git diff --submodule=log origin/master...origin/Development
   ```

3. For every changed submodule, open and merge a reviewed pull request from `Development` to `main`. Resolve and test contract changes in the owning submodule. Do not advance the parent `master` branch yet.
4. Wait until every required submodule change exists on its remote `main` branch.
5. Run the **Submodule Sync** workflow for `master`, or wait for its scheduled run. It:
   - checks out `master` and all submodules;
   - advances each gitlink to the `main` tip declared by `.gitmodules`;
   - regenerates the seven private pins in `Docker/oasis-dependency-versions.env` from those checked-out commits;
   - commits the pointers and manifest together;
   - validates the committed branch mapping under the target branch's rule and validates manifest/gitlink equality; and
   - opens or updates a `submodule-sync/master` pull request.
6. Review that parent pull request. CI must pass the branch-policy checks, manifest checks, and applicable builds before merging.
7. Merge the parent pull request into `master`. This is the production release action.
8. Verify Railway reaches a terminal success state for staging and production, then call the affected hosted health, Swagger, or functional endpoints.

The same workflow can synchronize `Development` to the latest submodule `Development` tips. Its PR is still reviewed because it changes the complete source graph deployed by Railway.

## Manual equivalent

Use this only when diagnosing or reproducing the workflow locally:

```bash
git checkout master
git pull --ff-only origin master
git submodule sync --recursive
git submodule update --init --remote --recursive
python3 Scripts/sync_railway_dependency_manifest.py
git add --all
git commit -m "chore(submodules): bump master pointers to their branch tips"
python3 Scripts/check_submodule_branches.py
python3 Scripts/validate_railway_dependency_manifest.py --require-gitlinks
```

Run the required WEB4-WEB10 publishes described in [Railway dependency pins](./RAILWAY_DEPENDENCY_PINS.md) before opening the parent pull request.

## What is automated

- Scheduled and manually dispatched discovery of new submodule branch tips.
- Updating parent gitlinks according to `.gitmodules`.
- Regenerating the private Railway pins from the checked-out submodule commits.
- Committing the gitlinks and manifest atomically in the promotion branch.
- Branch-tip, manifest structure, Dockerfile-consumption, and manifest/gitlink validation.
- Opening or updating the promotion pull request and running CI.

## Required review gates

- Each submodule's `Development` to `main` merge is reviewed because it publishes that component's production history.
- The parent `master` pull request is reviewed because it selects the exact production source graph.
- Railway deployment and hosted endpoint verification happen after merge.

These gates are intentional. Automation prepares and proves a concrete release candidate; it does not silently publish changing branch tips to production.

## Enforcement files

| File | Role |
|---|---|
| `.gitmodules` | Declares `Development` or `main` as the required submodule branch for the current parent branch. |
| `.github/workflows/submodule-sync.yml` | Updates branch tips, regenerates the manifest, validates the committed result, and opens the PR. |
| `.github/workflows/ci-cd.yml` | Blocks pull requests that violate the source-graph invariants. |
| `Docker/oasis-dependency-versions.env` | Records the immutable revisions Railway clones. |
| `Scripts/sync_railway_dependency_manifest.py` | Generates private manifest pins from checked-out submodule commits. |
| `Scripts/check_submodule_branches.py` | Confirms parent gitlinks and `.gitmodules` follow the branch policy. |
| `Scripts/validate_railway_dependency_manifest.py` | Confirms all seven Dockerfiles share the manifest and its private pins equal committed gitlinks. |
| `Docs/Devs/RAILWAY_DEPENDENCY_PINS.md` | Documents dependency pinning, local build checks, Railway verification, and failure diagnosis. |

## Failure handling

| Failure | Action |
|---|---|
| A required commit is missing from submodule `main` | Stop the parent promotion and merge the owning submodule PR first. |
| Promotion PR reports a manifest/gitlink mismatch | Rerun the sync workflow or run the generator; do not hand-select a different SHA. |
| A submodule merge introduces an incompatible shared contract | Fix and test the contract in its owning repositories before advancing parent `master`. |
| Railway fails after local builds pass | Diagnose the failing environment or service log while keeping the committed source graph fixed. |
| Production verification fails | Revert the parent promotion PR to restore the preceding recorded source graph, then fix forward through the same process. |
