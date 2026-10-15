# Railway staging and production approval gate

## Implementation and live configuration status

The new implementation is `.github/workflows/railway-staged-release.yml` and `Scripts/railway_release_gate.py`. It is not yet declared end-to-end proven. The live cutover has disabled Railway push autodeploys for the existing seven staging and seven production services. Dev autodeploys are unchanged. Running deployments were not stopped.

GitHub environment `oasis-production-gated` is configured with required reviewer **dellams**, administrator bypass disabled and only the master branch allowed. The owner explicitly selected a single approver; self-review remains allowed so dellams can approve a release he initiated. This is an explicit human approval boundary, not independent two-person authorization. Automation must never approve its own production deployment.

GitHub environment `oasis-staging-gated` permits master only. There are no new Git branches, Railway environments or build runtimes.

## Release sequence

1. Promote components and parent through the existing reviewed branch/pin policy. Merge to master does **not** directly deploy staging/production anymore.
2. A successful master `OASIS CI/CD Pipeline` run triggers `Gated Railway release`; alternatively dispatch it from master with a full SHA already reachable from master. The latest exact-master CI result must be successful.
3. Freeze candidate SHA, source tree, dependency-manifest hash, gate-policy hash, service inventory and workflow run ID into an immutable same-run artifact. Workflow code comes from trusted master, not from a fork or an arbitrary candidate checkout.
4. Verify environment-scoped credentials, disabled push autodeploys and approved staging database/test-payment configuration. Missing/incorrect isolation fails before staging deployment.
5. Deploy all seven services using Railway's explicit `commitSha` API. Retain every deployment ID. Bounded polling fails on failed/crashed/removed/skipped deployments or timeout; mutation requests are never blindly retried.
6. Check deployment SHA, service/environment identity and that each tested deployment is still active/current. Run ten read-only TLS/HTTP200/nonempty JSON contract checks: seven valid OpenAPI documents plus WEB6 health, MCP and A2A discovery. Paid provider operations and physical-device acceptance are outside this suite; applicable functional tests remain covered by candidate CI and separately scoped acceptance evidence.
7. Pause at GitHub's native production environment approval. The staging summary identifies the exact SHA/tree and links to retained evidence. Rejecting or withholding approval must leave production unchanged.
8. After dellams approves, revalidate staging evidence (same run/candidate/policy, all seven services, ten checks, no more than six hours old), recheck staging deployment identity and HTTPS contracts, and verify database separation against production before any production write.
9. Deploy **the identical candidate SHA and dependency graph** to production. Verify all seven exact-source deployments and repeat the ten read-only acceptance checks. Retain production evidence for 30 days.

The global workflow concurrency lock spans staging, the human approval wait and production, preventing another workflow candidate from replacing staged evidence mid-approval. External/manual drift is rejected by deployment-ID/current-active checks. Cancelled/failed runs release the lock; a newer run stages its own candidate and requires its own approval.

## Source identity versus artifact identity

This implementation uses Railway Git-source deployments: staging and production build from the same full source SHA and pinned dependency graph. It does **not** claim byte-identical image promotion. Railway can produce different image digests for separate builds (including timestamps/base-image resolution). A future build-once/digest-promotion design needs a controlled OCI registry/build pipeline and associated credential/storage policy; do not relabel two source builds as the same tested binary. This limitation is explicit in approval evidence.

## Owner setup required before execution

The current Railway authentication can manage service autodeploy settings but `projectTokenCreate` returns **Not Authorized**. No project token was created or leaked, and no personal token was copied into GitHub.

In Railway project **OASIS → Settings → Tokens**, create separate environment-scoped project tokens for staging and production. Store through GitHub **Settings → Environments** (never paste into chat):

| GitHub environment | Secret | Value source |
|---|---|---|
| `oasis-staging-gated` | `RAILWAY_TOKEN` | Staging-scoped Railway project token |
| `oasis-production-gated` | `RAILWAY_TOKEN` | Production-scoped Railway project token |
| `oasis-production-gated` | `RAILWAY_STAGING_TOKEN` | Same staging-scoped token, for post-approval drift checks |

Do not store the production token at repository scope or in the staging environment. GitHub releases environment secrets only after its protection rules pass.

### Isolation blocker discovered during setup

Read-only comparison found staging WEB4's `OASIS_DNA_JSON`, Stripe secret key and webhook credentials equal to production's. The staging Stripe key has a live-mode prefix. No values were printed. Shared Mongo settings therefore cannot be treated as isolated acceptance data, and a staging deployment may write during startup even if probes are read-only.

The owner approved dedicated empty staging databases on the existing Mongo service, without a new paid plan. Setup has now created `oasis_staging` (application) and `oasis_subscription_accounting_staging` (accounting), each with an empty `_staging_isolation` collection and zero records. No production data was copied. All seven staging services' DNA DBName settings now select the staging application database; WEB4 explicitly selects the staging accounting database and empty-new-installation ledger policy. Updates used secret-safe stdin and `--skip-deploys`; each readback passed and production DNA remained unchanged. **The new settings are not yet activated in running services by a deployment.**

The existing Mongo login cannot create users or grant roles. A database owner must create a staging-only user with `readWrite` restricted to these two staging databases, install its connection URI securely in all seven staging DNA settings (and WEB4's explicit accounting URI if configured), and set its nonsecret reviewed username in `Scripts/railway_release_gate.json`. `stagingIsolation.mongoUsername` remains null, deliberately blocking deployment until that scope is configured. The gate checks both application and accounting DB names/identities; changing only app DBName must not leave accounting on the production default.

Stripe test credentials and a staging-specific test webhook secret still need secure configuration; no payment credentials have been copied or changed. An existing dev test key was found, but sharing its account/webhook routing with staging was not silently assumed. Never clone production data or rotate/copy live payment credentials implicitly. Existing explicitly approved shared offline-grant signing keys remain unchanged; sharing that trust identity is distinct from sharing billing/data resources.

`Scripts/Initialize-RailwayStagingIsolation.ps1` records the owned setup path. It reuses installed .NET/MongoDB assemblies, refuses nonempty/production target namespaces, and performs no deployments or deletion. Do not rerun empty-installation initialization after staging holds test records.

## Proof checklist (not yet complete)

- Local positive/negative contract tests pass, including missing acceptance, SHA/environment/service mismatch, stale evidence, superseded deployments, autodeploy bypass, wrong token scope and live Stripe/wrong database rejection.
- Hosted contract tests must pass on the exact implementation commit and promotion PR.
- Live readback must confirm all fourteen push autodeploys are disabled and the production reviewer/branch/bypass policy is enforced.
- After credentials/isolation are ready, execute one candidate: record seven staging deployment IDs, acceptance JSON and a native pending production approval while production remains unchanged.
- Dellams approves in GitHub, not via an agent. Record the approved run, production deployment IDs, exact candidate match and all acceptance results.
- Reject/failure paths must show no production mutation. Do not manufacture a passing result when owner setup or approval is missing.

## Failure and rollback

Seven services do not deploy atomically. If a deployment or post-deploy contract fails, the run fails and retains IDs/evidence; investigate the affected service and migration compatibility. Do not automatically roll back databases or mix moving branch tips. A rollback is an explicit approved candidate release: select a known-good full SHA, verify compatibility with current data, stage/test it, obtain production approval and promote that same SHA. Do not re-enable push autodeploys as a recovery shortcut.

Manual Railway administrators can still change the platform; GitHub gating does not remove workspace-administrator powers. Restrict those roles operationally, audit emergency changes and keep deployment credentials scoped. The pipeline detects effective autodeploy/drift but is not a substitute for organization RBAC.

## Official guidance used

- [GitHub environment protection and review](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/review-deployments): native approval, environment-secret release and administrator bypass controls.
- [Railway GitHub autodeploy controls](https://docs.railway.com/deployments/github-autodeploys): disable parallel push-driven deployment paths.
- [Railway public API](https://docs.railway.com/integrations/api): environment-scoped project tokens, GraphQL error handling and mutation reconciliation.

The selected design combines these controls with immutable source identity, serialization, bounded deployment reconciliation and retained acceptance evidence. It does not claim a universal industry certification or coverage beyond the explicit contracts above.
