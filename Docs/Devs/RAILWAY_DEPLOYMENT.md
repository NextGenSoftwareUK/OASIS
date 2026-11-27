# Railway deployment: operations guide

State as of 26-27 November 2026. Railway settings live in Railway, not in git, so check the dashboard before relying on a value here.

Related: [host profiles and provider hosting](ONODE_HOST_PROFILES_AND_PROVIDER_HOSTING.md), [dependency pins](RAILWAY_DEPENDENCY_PINS.md), [promotion to master](DEVELOPMENT_TO_MASTER_PROMOTION.md), [Railway and DNA variables](../Configuration/RAILWAY_AND_DNA_VARIABLES.md).

## Layout

Railway project **OASIS**, environments `production`, `dev` and `staging`.

| Service | Notes |
|---|---|
| WEB4 OASIS API | |
| WEB5 STAR API | |
| WEB6 AI API | |
| WEB7 SYMBIOTIC API | |
| WEB8 IGN API | |
| WEB9 SINGULARITIY API | Spelled this way in Railway |
| WEB10 THE SOURCE API | |
| Neo4j | Stopped |

`function-bun` was a Railway sample service and has been deleted.

| Environment | Source |
|---|---|
| `dev` | The `Development` branch, deployed on push |
| `production` | The `master` branch |
| `staging` | No branch of its own. It takes an exact `master` candidate through `.github/workflows/railway-staged-release.yml`, with approval before production |

The promotion doc records that push auto-deploys for staging and production were disabled for that gated workflow, which was still being proven. Check each service's source settings before assuming a push to `master` deploys by itself.

## Configuration

- Each service in each environment has its own `OASIS_DNA_JSON` variable. `Docker/entrypoint.onode.sh` writes it to `/app/OASIS_DNA.json` at start-up. It is the live DNA.
- When DNA changes, change every copy in the same piece of work: the typed C# default, every `OASIS_DNA*.json` (including test-harness copies and the STAR ODK templates) and Railway's `OASIS_DNA_JSON`.

### Setting OASIS_DNA_JSON safely

- Never put the value on a Windows command line. It is over 30,000 characters (Windows allows 32,767), and Git Bash rewrites Linux paths such as `/data/...` into `C:/Program Files/Git/data/...`.
- Use a Python script that reads the JSON from a file and calls the Railway GraphQL API (`variableUpsert`) with a file-based body. If Git Bash is involved at all, set `MSYS_NO_PATHCONV=1`.
- Read the current value immediately before editing: other sessions may write the same variable.
- Read it back afterwards and check your keys and any paths.
- Redeploy the service so the running container picks up the new value, then check the start-up log.
- Keep the file out of git (it holds secrets) and delete it when done.

## Cost settings

Memory is about 90% of the bill.

| Setting | Value (WEB4 to WEB10) |
|---|---|
| `DOTNET_gcServer` | `0` |
| `DOTNET_GCConserveMemory` | `5` |
| App sleeping | On. Expect cold starts of a few seconds; warm the services before a demo |
| Watch Paths | `["**","!Docs/**","!**/*.md","!.github/**"]` |
| Memory limits | WEB4 3 GB; WEB5 and WEB6 1 GB; WEB7 to WEB10 0.5 GB |

Production memory went from about 4.6 GB to about 2.5 GB.

## WEB4 sidecars

Sidecars are helper processes inside the WEB4 container that serve CQL sandbox sources (for example NATS JetStream). On demand, a sidecar starts on the first query for its source and stops after the idle period. Start-up launches none. This is on `Development` and `master`.

DNA `OASIS.Sandbox.Sidecars`: `OnDemand` (default `false`), `IdleMinutes` (default 10, minimum 2; 2 was used while testing on dev), `AlwaysOn` (comma-separated source providers).

Search the WEB4 logs in Railway for `SIDECAR`:

| Line | Meaning |
|---|---|
| `SIDECAR start name=… reason=…` | Starting (`first-use`, `crash` or `always-on`) |
| `SIDECAR ready … startupMs=… memoryUsedMb=…` | Ready |
| `SIDECAR stop … reason=idle\|shutdown memoryFreedMb=…` | Stopped |
| `SIDECAR crash name=…` | It had exited and is being restarted |
| `SIDECAR failure … error=…` | It did not start; the query fails |

Status, Wizard avatars only: `GET /api/admin/cql/sidecars` (state, active requests, last used, uptime, memory).

Remove a source's `OASIS_SANDBOX_*` flag only together with its other settings: turning flags off while settings remain stopped WEB4 from starting.

## Neo4j

The Railway deployments are stopped and the volume is kept. `Neo4jOASIS` stays in the HyperDrive lists, so fail-over routes around it.

- Bring it back: redeploy the Neo4j service, then check health and replication.
- Or move to Neo4j AuraDB Free: create the instance, migrate the data and point `Neo4jOASIS` at it in each `OASIS_DNA_JSON`. See the [host-profiles doc](ONODE_HOST_PROFILES_AND_PROVIDER_HOSTING.md).
- To stop it again, remove the deployment (`deploymentRemove`). `deploymentStop` did not stop it.

## Monitoring and cost

- Memory per service: open the service, then Metrics (choose the environment first).
- Month-to-date bill: the workspace Usage page.
- Hard usage limit: Usage, then Update limits. A hard limit takes services offline when reached, so leave headroom.
- A service at its memory limit restarts repeatedly. Check Metrics and the logs just before each restart; raise the limit if the usage is legitimate, otherwise find the growth.

## Deploy safety

- Railway builds the pins in `Docker/oasis-dependency-versions.env`, not the parent gitlinks. They move together: run `python3 Scripts/sync_railway_dependency_manifest.py` after moving a gitlink and commit both.
- Enable the pre-push check once per clone: `git config core.hooksPath .githooks`.
- Batch pushes. Docs-only commits use `[skip ci]`.
- Deploy to dev first, verify, then production, with a rollback ready.
- Production should have the `/api/health` deploy health check applied so a crashing deployment does not replace a working one. `railway.json` declares it, but it was not applied during the outage below; confirm it in the dashboard.

## Incidents and fixes

| Problem | Fix |
|---|---|
| Memory was about $65 of a $72 bill | Neo4j stopped, .NET GC settings, app sleeping, memory limits, on-demand sidecars (dev WEB4 1,481 MB to 974 MB; production not yet measured) |
| Docs-only pushes redeployed WEB4 to WEB10, flooded the deploy queue and kept restarting the dev sandbox | Watch Paths; claims moved to the non-deploying `claims` branch with `[skip ci]` |
| The IPFS `CatalogueRootPointerPath` was saved as `C:/Program Files/Git/data/IPFS/catalogue-root` (Git Bash rewrote the path) | Re-set with `MSYS_NO_PATHCONV=1` from a file; the write now refuses any other value; read back |
| `OASIS_DNA_JSON` had concurrent writers (an `AccessLog` block appeared during an edit) | Always read back after writing; DNA copies synced |
| The pre-push pin check blocked all pushes to `master` after an ONODEManager gitlink moved without its manifest | `Scripts/sync_railway_dependency_manifest.py` run on `master` (`cd4ace17`) |
| WEB4 production returned 502 for a few minutes while sidecar work was in progress: sandbox flags were set to `false` while their settings remained | Flags restored and deployment rolled back. Rule: dev first, then production behind the `/api/health` check, rollback ready |
| Stopping Neo4j with `deploymentStop` did not stop it | `deploymentRemove` (volume kept) |
| Variables set through the API did not take effect | Redeploy the service |
| GitHub Actions queue clogged, including 22 stale "queued" runs that cannot be cancelled | Count only runs from the last 12 hours; cancel superseded runs; batch pushes; push a batch only under 35 queued, extra runs under 20 |
| Boot source checks started every sidecar | Boot now starts none; a sidecar starts only for a real query |
