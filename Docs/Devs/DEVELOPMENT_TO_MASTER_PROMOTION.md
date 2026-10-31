# Promoting OASIS from Development to master

This is the release process for promoting the OASIS parent repository and its submodules from development into production. It keeps the source graph tested locally, built by Railway, and recorded by the parent repository identical.

For the complete workflow, test, artifact, credential, and release-channel catalogue, see [OASIS CI/CD, Tests, Artifacts, and Releases](./CI_CD_WORKFLOWS_AND_RELEASES.md).

For the normal GitHub process, start with [Merge Development into master: quick start](./MERGE_DEVELOPMENT_TO_MASTER_QUICK_START.md). Promote a **release branch snapshot** (below), not the moving `Development` tip. The remainder of this guide explains the implementation, manual equivalent, failure handling, and recovery process.

## Release invariant

| Parent branch | Submodule branch | Railway manifest |
|---|---|---|
| `Development` | `Development` | Must equal the parent branch's private submodule gitlinks. |
| `master` | `main` | Must equal the parent branch's private submodule gitlinks. |

Production must never point at a submodule's `Development` commit. Each changed submodule is promoted to its `main` branch first. The parent `master` pointer and `Docker/oasis-dependency-versions.env` then advance together in one reviewed pull request.

## Release branch snapshot (default)

**Rule:** a release promotes `release/YYYY-MM-DD`, a branch cut from one chosen `Development` commit and tagged `release-YYYY-MM-DD`. It does not promote the live `Development` branch.

**Why.** `Development` is shared by many contributors and automated agents who push continuously. The promotion workflow merges whatever `Development` is when it runs, so while a release was being checked each push changed the candidate: required checks restarted, new `Development` to `main` pull requests appeared in submodules, generated manifests went stale, and one contributor's half-finished work (a compile error in a new controller) blocked everyone's release. A fixed snapshot makes the candidate immutable, so the checks you read are the checks of what ships.

**How.**

1. Choose the commit. It should be a `Development` tip whose CI is green, or whose remaining failures you have decided to accept and recorded.
2. Confirm every submodule gitlink in that commit is an ancestor of that submodule's `main`. If not, merge those submodules' `Development` to `main` pull requests first. Merge only what that commit already contains: submodule `Development` may be ahead of the pin, and the pin is what counts.
3. Cut and tag:

   ```bash
   git fetch origin Development
   git push origin <commit>:refs/heads/release/YYYY-MM-DD
   git tag release-YYYY-MM-DD <commit> && git push origin release-YYYY-MM-DD
   ```

4. Run **Promote Development to master** with **Branch to promote** (`source_ref`) set to `release/YYYY-MM-DD`. The workflow merges that branch, keeps production submodules on `main`, regenerates the Railway manifest and opens or updates the **Promote Development to master** pull request.
5. Checks run on that pull request. Fix failures **on the release branch** (the smallest change that fixes them), rerun the workflow with the same `source_ref`, and merge the pull request when green. That merge is the production release.
6. Afterwards, merge the release branch back into `Development` so fixes are not lost, and let normal work continue. Anything pushed to `Development` after the cut ships in the next release.

**Rules.**

- Never move a `release/*` branch or its tag. A corrected release is a new branch, for example `release/YYYY-MM-DD-2`.
- Only release-blocking fixes go on a release branch. New features stay on `Development`.
- The workflow's `source_ref` defaults to `Development`, which keeps the old behaviour. Use that only for a deliberate promotion of the moving tip.
- The generated manifests that CI verifies (HyperDrive provider I/O, WEB4 routes) and the dependency pins are part of the snapshot. Regenerate them on the release branch if the snapshot's Core or controllers changed, never against a newer Core.

## Underlying promotion sequence

The **Promote Development to master** workflow performs this sequence. These details are provided for maintainers and recovery; normal developers should use the quick-start workflow rather than execute them manually.

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

The manifest, not the gitlinks, decides what Railway builds, so never skip the `sync_railway_dependency_manifest.py` step above. Enable the local guard once per clone with `git config core.hooksPath .githooks` (`.githooks/pre-push` refuses a push whose pins differ from the gitlinks).

## Staging release-candidate process (gate implementation in progress)

The owner has now requested enforcement. See [Railway staging gate implementation and proof checklist](<../WIP/Release and Status/RAILWAY_STAGING_GATE.md>). Live staging/production push autodeploys have been disabled; dev is unchanged. The new gated workflow freezes an exact CI-tested master candidate, stages it, requires native GitHub approval by dellams, rechecks staging and promotes the same source SHA. It remains blocked from live proof by scoped-token provisioning and staging data/payment isolation; do not claim end-to-end completion. The numbered intended sequence below is the target contract, not a claim that every acceptance item has passed.

Staging is a Railway **environment**, not an additional long-lived Git branch. Do not create `staging` branches
in the parent or every component repository. Keep the existing `Development → master` parent and
`Development → main` component mapping. A branch is needed only if a future, explicitly reviewed branching
policy introduces a separate maintained release line; environment separation alone does not require one.

The intended deployment sequence is:

1. Complete Development CI and development hosted-contract checks.
2. Complete checked component promotion PRs and prepare the checked parent promotion PR using the existing workflow.
3. Freeze its reviewed parent candidate SHA, gitlinks and dependency manifest. Deploy that exact source graph to
   Railway staging before production deployment; do not deploy moving Development or component branch tips.
4. Verify staging startup, HTTPS/domain certificates, health, Swagger and affected functional contracts across
   WEB4-WEB10, including authentication, offline grants and synchronization where changed. Record candidate SHA,
   deployment IDs and compact test evidence. Build success or Swagger alone is not functional acceptance.
5. Require staging acceptance and production approval. Promote the same tested source graph to production;
   if merging changes the candidate's source graph, restage and retest it. Production must not build a different
   dependency revision. Where supported, promote the same immutable build artifact rather than rebuilding it.
6. Verify production endpoints, then publish the requested GitHub/NuGet/NPM releases using the existing global
   release workflow. Record the deployed source graph and publication results.
7. On failure, stop promotion and diagnose the owning code or environment. Roll back to the previous reviewed
   deployable artifact/source graph where compatible with data migrations; do not bypass gates or silently fall back.

Staging mirrors production's runtime versions, configuration **schema**, feature flags, deployment topology and
dependency pins. Use separate staging databases, storage, queues, service credentials and external-service sandbox
accounts where possible. Copying production settings verbatim can expose live data or send real payments/messages.
Signing identities and client public-key pins need an explicit policy; never rotate or copy them implicitly.
The owner explicitly selected matching staging/production offline-grant signing keys on 2026-10-08; dev remains
separate. These two environments therefore share that trust identity and are not cryptographically isolated by
signing keys. Keep their public pins, scopes and lifetimes identical, and coordinate future rotation and client
updates together. Other isolation recommendations above remain recommendations rather than permission to
change the owner's environment settings. Review configuration parity without printing secret values, and validate migrations against representative
sanitized data with a documented rollback/forward-recovery plan.

**Transition status:** the old merge-then-parallel-deploy triggers are now disabled for staging/production. The gate implementation and owner setup/proof requirements are recorded in the linked runbook. Source-SHA promotion is not byte-identical OCI artifact promotion. No running deployment was stopped, no production data/payment credentials were changed, and no owner approval was simulated.

## Existing automation

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
| `.github/workflows/promote-development-to-master.yml` | Opens missing submodule promotion PRs and prepares the validated parent production PR. Takes a `source_ref` input: the release branch to promote (default `Development`). |
| `.github/workflows/ci-cd.yml` | Blocks pull requests that violate the source-graph invariants. |
| `Docker/oasis-dependency-versions.env` | Records the immutable revisions Railway clones. |
| `Scripts/sync_railway_dependency_manifest.py` | Generates private manifest pins from checked-out submodule commits. |
| `Scripts/check_submodule_branches.py` | Confirms parent gitlinks and `.gitmodules` follow the branch policy. |
| `Scripts/validate_railway_dependency_manifest.py` | Confirms all seven Dockerfiles share the manifest and its private pins equal committed gitlinks. |
| `Docs/Devs/RAILWAY_DEPENDENCY_PINS.md` | Documents dependency pinning, local build checks, Railway verification, and failure diagnosis. |
| `Docs/Devs/MERGE_DEVELOPMENT_TO_MASTER_QUICK_START.md` | Gives normal developers the short GitHub-only release procedure. |

## Failure handling

| Failure | Action |
|---|---|
| A required commit is missing from submodule `main` | Stop the parent promotion and merge the owning submodule PR first. |
| Checks fail on the promotion pull request | Fix on the release branch and rerun with the same `source_ref`. Do not switch to the moving `Development` tip to make a check pass. |
| Promotion PR reports a manifest/gitlink mismatch | Rerun the sync workflow or run the generator; do not hand-select a different SHA. |
| A submodule merge introduces an incompatible shared contract | Fix and test the contract in its owning repositories before advancing parent `master`. |
| Railway fails after local builds pass | Diagnose the failing environment or service log while keeping the committed source graph fixed. |
| Production verification fails | Revert the parent promotion PR to restore the preceding recorded source graph, then fix forward through the same process. |
