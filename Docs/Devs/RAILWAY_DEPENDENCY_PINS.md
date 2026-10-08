# Railway dependency pins for WEB4-WEB10

## Subscription protocol update (2026-09-22)

WEB5–WEB10 now share WEB4's operation-ID reserve → execute → settle protocol. Billable calls require a validated bearer and stable `Idempotency-Key`; consuming services use distinct service credentials and durable settlement outboxes. The old `authorize-request` counter is retired (410). See the [sequence, accounting and recovery contract](WEB4_SUBSCRIPTION_USAGE_LEDGER.md) and [configuration, historical migration, live tests and operational runbook](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md). Provider measurements and reviewed price catalogues must be configured before enabling paid execution.


Railway builds WEB4 through WEB10 from the OASIS parent repository. Several required repositories are private submodules, and Railway's source checkout does not populate them. The Docker build therefore clones those repositories explicitly.

## The invariant

[`Docker/oasis-dependency-versions.env`](../../Docker/oasis-dependency-versions.env) is the only authoritative set of dependency revisions for all seven Railway services. Every revision is a full commit SHA so one deployment always builds the same source graph.

The WEB4-WEB10 Dockerfiles must:

1. Copy the shared manifest.
2. Source it when cloning NextGenSoftware-Libraries and holochain-client-csharp.
3. Invoke `Docker/clone-pinned-oasis-dependencies.sh` for the private OASIS repositories.
4. Contain no dependency commit SHAs of their own.

On `Development` and `master`, the seven private repository values in that branch's manifest must exactly match the parent repository's gitlinks. This makes the locally tested checkout and the Railway checkout identical without allowing Development revisions to leak into production.

## Files and responsibilities

| File | Responsibility |
|---|---|
| [`Docker/oasis-dependency-versions.env`](../../Docker/oasis-dependency-versions.env) | The single authoritative dependency SHA set for the current parent branch. |
| [`Docker/clone-pinned-oasis-dependencies.sh`](../../Docker/clone-pinned-oasis-dependencies.sh) | Reads the manifest and checks out the private repositories at those exact revisions. |
| [`Scripts/validate_railway_dependency_manifest.py`](../../Scripts/validate_railway_dependency_manifest.py) | Rejects missing or malformed pins, Dockerfile-local pins, Dockerfiles that bypass the manifest, and manifest/gitlink mismatches. |
| [`Scripts/sync_railway_dependency_manifest.py`](../../Scripts/sync_railway_dependency_manifest.py) | Regenerates the seven private pins from the currently checked-out submodule commits. |
| [`Scripts/check_submodule_branches.py`](../../Scripts/check_submodule_branches.py) | Enforces `Development → Development` and `master → main`, including the branch declarations in `.gitmodules`. |
| [`.github/workflows/ci-cd.yml`](../../.github/workflows/ci-cd.yml) | Runs both policy checks on pushes and pull requests and blocks drift from merging. |
| [`.github/workflows/submodule-sync.yml`](../../.github/workflows/submodule-sync.yml) | Detects new submodule branch tips and opens a reviewed pointer-update pull request. |
| [`.github/workflows/promote-development-to-master.yml`](../../.github/workflows/promote-development-to-master.yml) | Opens missing component promotion PRs and prepares a validated Development-to-master parent PR. |
| [`.gitmodules`](../../.gitmodules) | Declares which branch each parent branch follows for every submodule. |
| [`AGENTS.md`](../../AGENTS.md) | Gives this policy to repository-aware coding agents before they modify deployment dependencies. |
| [`Docs/Devs/DEVELOPER_DOCUMENTATION_INDEX.md`](./DEVELOPER_DOCUMENTATION_INDEX.md) | Makes this guide discoverable from the developer documentation index. |
| [`Docs/Devs/DEVELOPMENT_TO_MASTER_PROMOTION.md`](./DEVELOPMENT_TO_MASTER_PROMOTION.md) | Defines the complete reviewed Development-to-production promotion sequence and its automation boundary. |
| [`Docs/Devs/MERGE_DEVELOPMENT_TO_MASTER_QUICK_START.md`](./MERGE_DEVELOPMENT_TO_MASTER_QUICK_START.md) | Provides the short GitHub-only release checklist for normal developers. |
| [`Docker/Dockerfile.web4`](../../Docker/Dockerfile.web4) | WEB4 Railway build; consumes the shared manifest. |
| [`Docker/Dockerfile.web5`](../../Docker/Dockerfile.web5) | WEB5 Railway build; consumes the shared manifest. |
| [`Docker/Dockerfile.web6`](../../Docker/Dockerfile.web6) | WEB6 Railway build; consumes the shared manifest. |
| [`Docker/Dockerfile.web7`](../../Docker/Dockerfile.web7) | WEB7 Railway build; consumes the shared manifest. |
| [`Docker/Dockerfile.web8`](../../Docker/Dockerfile.web8) | WEB8 Railway build; consumes the shared manifest. |
| [`Docker/Dockerfile.web9`](../../Docker/Dockerfile.web9) | WEB9 Railway build; consumes the shared manifest. |
| [`Docker/Dockerfile.web10`](../../Docker/Dockerfile.web10) | WEB10 Railway build; consumes the shared manifest. |

The policy has two complementary checks. `check_submodule_branches.py` proves that the parent gitlinks point to the correct branch tips. `validate_railway_dependency_manifest.py --require-gitlinks` proves that Railway will build those exact same gitlink revisions.

## Branch mapping

| OASIS parent branch | Required submodule branch | Manifest purpose |
|---|---|---|
| `Development` | `Development` | Development and Railway dev deployments. |
| `master` | `main` | Staging and production deployments. Development commits must first be promoted into each affected submodule's `main`. |
| `main` | Not applicable | Legacy vendored layout without the WEB4-WEB10 Dockerfiles. |

Never solve a production incompatibility by pointing `master` at a submodule's `Development` commit. Promote the compatible change into the submodule's `main`, advance the parent gitlink, and update the `master` manifest together.

## Updating a dependency

### Private checkout credentials are environment-specific

Each Railway WEB4-WEB10 service must receive a non-empty `GITHUB_PAT` build variable with read access to the
private repositories named by the manifest. GitHub Actions' `PRIVATE_SUBMODULE_PAT` secret does not automatically
configure Railway. A successful dev or production build does not prove that staging has this variable.
Configure it through Railway's secret-variable controls (or an explicitly approved shared-variable reference);
never put its value in Git, documentation, logs, or a command-line argument. Keep the pinned clone's fail-fast
credential requirement; do not replace it with a public clone, moving branch, or cached dependency fallback.

On 2026-10-08, staging WEB5-WEB10 had no non-empty `GITHUB_PAT`. WEB5 deployment
`c16f92e3-717d-4c77-a6e9-96675403446a` and WEB6 deployment `838bc194-77b5-490d-be38-df98d022e851`
failed in `clone-pinned-oasis-dependencies.sh` with `GITHUB_PAT: parameter not set`, before .NET compilation.
After configuring the missing variables, redeploy the reviewed staging commit and inspect every affected service's
terminal result and hosted endpoint. This is a configuration repair, not evidence that the source pins are wrong.

The missing staging variables were subsequently configured using the existing credential with explicit user approval,
without writing the credential to files or command-line arguments. Redeployments retained reviewed master commit
`cfda48d9af175498522d320d39bf70ce9ed8caff`. WEB5, WEB6, WEB7 and WEB8 hosted Swagger endpoints returned HTTP 200
after their deployments succeeded (WEB6 uses `/swagger/v2/swagger.json`; the others use `/swagger/v1/swagger.json`).
WEB10 deployment `40927e11-4f69-4767-a0d3-3c0fcf06a7c4` succeeded, but its custom domain initially failed TLS
validation. Railway reported propagated, verified DNS and `CERTIFICATE_ERROR_TYPE_INTERNAL` during certificate
issuance. One Railway certificate-issuance retry restored a valid certificate; the HTTPS Swagger endpoint then
returned HTTP 200. TLS validation was never disabled. WEB9 and subsequently queued replacement deployments still
require terminal-result and hosted-endpoint verification; this evidence does not certify a new release or promotion.

When a deployed change lands in a submodule:

1. Advance the submodule pointer in the OASIS parent repository to the tested commit.
2. Change the matching value once in `Docker/oasis-dependency-versions.env`.
3. If NextGenSoftware-Libraries or holochain-client-csharp changes, update its manifest value. Update both together when their transport contract changes.
4. Run:

   ```bash
   python3 Scripts/validate_railway_dependency_manifest.py --require-gitlinks
   ```

5. Publish all affected services locally. For a shared API or transport change, publish WEB4-WEB10.
6. Commit the submodule pointer and manifest change together.
7. After pushing the applicable deployment branch (`Development` or `master`), wait for every Railway service to reach a terminal state and verify the hosted health or Swagger endpoint.

Do not point a Docker build at a moving branch, use `git clone --depth 1` without a checkout SHA, or add a service-specific fallback revision. Those recreate the version-skew failure this policy prevents.

## Enforcement

`Scripts/validate_railway_dependency_manifest.py` checks that:

- all required values exist and use full lowercase commit SHAs;
- all seven Dockerfiles consume the shared manifest and clone script;
- no Dockerfile contains a duplicated commit pin;
- with `--require-gitlinks`, every private pin equals the parent gitlink.

The `solution-integrity` CI job runs the structural check on every branch that contains WEB4-WEB10. It additionally enforces gitlink equality on `Development` and `master` pushes and pull requests targeting either branch. A submodule-only update therefore cannot merge into a deployment branch until its manifest is updated in the same change.

The legacy parent `main` branch uses a vendored directory layout and does not contain the WEB4-WEB10 Dockerfiles, so this manifest policy does not apply there. Do not copy a Development manifest into `master`; each deployment branch records its own tested source graph.

The `submodule-sync` workflow opens a pointer-update pull request when a tracked submodule branch advances. It regenerates the private manifest pins from the checked-out submodules and commits them with the gitlinks, then validates the committed state. The pull request remains an explicit reviewed release action. See [Promoting OASIS from Development to master](./DEVELOPMENT_TO_MASTER_PROMOTION.md).

## Required verification

For a manifest-only or Docker orchestration change:

```bash
python3 Scripts/validate_railway_dependency_manifest.py --require-gitlinks
bash -n Docker/clone-pinned-oasis-dependencies.sh
```

For a shared API, model, provider, transport, inventory, quest, or persistence change, also publish all seven web APIs from the parent checkout:

```bash
dotnet publish ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj -c Release
dotnet publish "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release
dotnet publish WEB6/NextGenSoftware.OASIS.Web6.WebAPI/NextGenSoftware.OASIS.Web6.WebAPI.csproj -c Release
dotnet publish WEB7/NextGenSoftware.OASIS.Web7.WebAPI/NextGenSoftware.OASIS.Web7.WebAPI.csproj -c Release
dotnet publish WEB8/NextGenSoftware.OASIS.Web8.WebAPI/NextGenSoftware.OASIS.Web8.WebAPI.csproj -c Release
dotnet publish WEB9/NextGenSoftware.OASIS.Web9.WebAPI/NextGenSoftware.OASIS.Web9.WebAPI.csproj -c Release
dotnet publish WEB10/NextGenSoftware.OASIS.Web10.WebAPI/NextGenSoftware.OASIS.Web10.WebAPI.csproj -c Release
```

A local success is necessary but does not complete deployment verification. After pushing, confirm the Railway aggregate deployment and individual WEB4-WEB10 statuses. Then call the hosted health or Swagger endpoints that exercise the changed contract.

## Expected CI failures

| Failure | Meaning | Correct action |
|---|---|---|
| Parent gitlink is behind the required submodule branch tip | The parent branch has not promoted the newest required submodule revision. | Review the submodule change, update the parent gitlink, update the manifest, and rebuild. |
| Manifest SHA differs from its parent gitlink | Railway would build different code from the parent checkout. | Change the manifest and gitlink together; do not add a fallback SHA. |
| Dockerfile contains `ARG ..._COMMIT=` | A service has reintroduced a private pin and can drift from the others. | Remove it and consume the shared manifest. |
| Dockerfile does not copy the manifest or invoke the clone script | That service bypasses the deterministic dependency graph. | Restore the standard shared-manifest block. |
| SHA is missing, abbreviated, uppercase, or malformed | The deployment is not pinned to an immutable commit. | Use the complete lowercase 40-character commit SHA. |
| Railway fails while all local publishes pass | The source graph is coherent, but the Railway environment or service configuration differs. | Inspect the failing environment's build/runtime log; do not change dependency pins without evidence. |

## Review checklist

- The parent branch and every changed submodule follow the branch mapping above.
- Every changed submodule revision is present on the required remote branch.
- The manifest and parent gitlinks contain identical private dependency SHAs.
- NextGenSoftware-Libraries and holochain-client-csharp pins are compatible when their transport contract changes.
- No WEB4-WEB10 Dockerfile contains a commit SHA or moving-branch clone.
- The validator and shell syntax checks pass.
- Every affected web API publishes locally; shared changes require WEB4-WEB10.
- Railway reaches a terminal success state in each intended environment.
- Hosted endpoints expose and execute the expected contract.
