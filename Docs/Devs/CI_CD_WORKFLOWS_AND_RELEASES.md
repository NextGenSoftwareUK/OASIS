# OASIS CI/CD, Tests, Artifacts, and Releases

The repeatable monthly process for releasing all NuGet packages, runtimes, OGEngineClient, Native Endpoint and MCP distributions is documented in [GLOBAL_RELEASE_AUTOMATION.md](GLOBAL_RELEASE_AUTOMATION.md).

That guide also defines the required standalone component workflows, automatic and explicit SemVer modes, non-publishing previews, release-preparation PRs and publish-time drift checks. Existing and planned workflows must be described accurately; unavailable game binary pipelines must remain explicit blockers.

This is the source-of-truth map for the workflows in `.github/workflows`. It explains what each workflow proves, what it produces, where releases appear, and which credentials it needs. Update this document whenever a workflow, test suite, release target, or required secret changes.

## Branch and release model

`Development` is the integration branch. Normal test workflows run there before a release. Private submodules use their own `Development` branches during integration. Production promotion merges each submodule's `Development` branch into its `main` branch, updates the parent gitlinks and `Docker/oasis-dependency-versions.env` atomically, then opens the parent `Development` to `master` pull request. No promotion workflow deletes branches.

`master` is the production source graph. A green Development run does not publish a package by itself. Publishing is explicit through a versioned release workflow or a release tag.

## Workflow catalogue

| Workflow | Triggers | What it validates or deploys | Artifacts and release output |
|---|---|---|---|
| `ci-cd.yml` — **OASIS CI/CD Pipeline** | Push and pull request for `main`, `master`, or `Development` | Validates solution coverage, forbidden project references, Railway dependency pins, the provider integration matrix, OASIS Architecture tests, ONODE tests, STAR ODK tests, cross-project integration tests, OGEngineClient tests, security checks, code quality, coverage, and a production solution build | TRX/test evidence, coverage reports, OGEngine test evidence, and `oasispackages` NuGet build artifacts retained by the Actions run. It builds packages but does not publish them. |
| `onet-onode-tests.yml` — **ONET/ONODE Tests** | Relevant pushes to `Development`/`master`; relevant PRs to `master` | Builds and runs ONODE Core unit tests, ONODE Core integration tests, and ONODE WebAPI unit tests on .NET 10 | `unit-test-results`, `integration-test-results`, and `webapi-unit-test-results` TRX artifacts |
| `edge-runtime-validation.yml` — **Edge Runtime validation** | Relevant edge/runtime changes on PRs and pushes to `Development`/`master`; manual | Builds and tests the pinned HoloOASIS hApp; starts a real three-member MongoDB replica set; proves transactional rollback/retry and acknowledged replay across abrupt primary loss; validates the Windows edge release with pinned Unity 2022.3.62f3 and Android modules | `verified-holooasis-happ`, `hosted-mongo-sync-evidence`, and `edge-runtime-release-evidence` |
| `subscription-usage-protocol.yml` — **Subscription usage protocol** | PRs to `Development`/`master`, pushes to `Development`, manual | Proves the WEB4 subscription authority and exact dependency graph; runs transactional ledger, WEB4, shared SDK/outbox, Stripe reconciliation, WEB6 measurement/recovery, WEB5 and WEB7-WEB10 transport, live-runner security, and operator authentication tests; publishes all WEB4-WEB10 projects as a build check | `subscription-protocol-test-results` |
| `global-release.yml` — **Global OASIS Release** | Manual, normally once or twice per month from `master` | Creates one registry-aware version plan; validates metadata for every first-party NuGet package; generates package/component/version-specific notes from Git history; builds selected OASIS Runtime, STAR Runtime, OGEngineClient, Native Endpoint and MCP outputs. API version/history preparation is performed first by the same local release script and committed for review. | Release plan, all selected `.nupkg` files with version-specific notes, runtime archives, component-specific GitHub releases, and delegated MCP GitHub/NuGet/npm releases |
| `release-web4-api.yml`, `release-web5-api.yml`, `release-web6-api.yml` | Manual Preview/Publish | Preview applies Automatic/Patch/Minor/Major/Manual SemVer to only the selected API, generates its Swagger history and opens reviewed parent/dependency PRs. Publish verifies merged `master`, builds the API and creates its versioned release. | Exact preview plan/diff artifact, preparation PRs, selected API build and `WEB4-v*`, `WEB5-v*`, or `WEB6-v*` release |
| `release-oasis-runtime.yml`, `release-star-runtime.yml`, `release-ogengine-client.yml`, `release-native-endpoint.yml`, `release-mcp-server.yml` | Manual Preview/Publish | Standalone entry points that select one component and dispatch the shared global planner with Automatic/Patch/Minor/Major/Manual versioning | Global plan/notes in Preview; the component's normal tested assets and destinations in Publish |
| `release-our-world.yml`, `release-odoom.yml`, `release-oquake.yml`, `release-oide.yml`, `release-onode-manager-entry.yml`, `release-hyperdrive-client.yml` | Manual Preview/Publish | Standalone external/application entry points using the same planner. Games report a publication blocker until their owning repositories expose canonical binary workflows. | Version plan and, where supported, dispatch of the owning tested release workflow |
| `reusable-api-release.yml` | Called by the three API entry points | Shared API preparation, submodule ownership, dependency pin, build and release invariant | Not run directly |
| `publish-mcp.yml` — **Build and Publish MCP Server** | MCP-related PRs to `Development`/`master`, pushes to `Development`, manual versioned release | Builds self-contained MCP binaries for `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`; runs packaged JSON-RPC/MCP smoke tests; validates the npm tarball. A manual run with `publish=true` creates all three package channels | GitHub release with five platform archives, `NextGenSoftware.OASIS.MCP.Server` on NuGet, and `@oasisomniverse/mcp-server` on npm |
| `publish-nuget.yml` — **Publish NuGet Packages** | Manual | Packs the configured OASIS and WEB6 NuGet projects from the exact source graph and pushes all resulting packages | NuGet.org packages |
| `release-onode-manager.yml` — **Publish — ONODE Manager Binaries** | `onode-manager-v*` tag or dispatch from the standalone planner | Runs manager tests; publishes Manager and Service for Windows, macOS, and Linux; packages with Velopack | Windows installer/update files, macOS DMG/update files, Linux AppImage/update files, and a GitHub release |
| `master_oasisapionode.yml` — **Build and deploy ASP.Net Core app to Azure Web App - OASISAPIONODE** | Configured production branch push or manual | Builds and publishes the ONODE WebAPI and authenticates to Azure with OIDC | Workflow deployment artifact and Azure Web App deployment |
| `deploy-oasisweb4.yml` — **Deploy OASIS Web4 Site** | Changes to `oasisweb4 site/**` on `max-build`, or manual | Copies the static Web4 site, removes oversized media, uploads the official Pages artifact, and deploys through GitHub Pages OIDC | GitHub Pages deployment at the repository Pages URL |
| `render-mermaid.yml` — **Render Mermaid Diagrams** | Changes to Mermaid source diagrams | Installs the Mermaid CLI, renders SVGs, and commits changed generated diagrams | Versioned SVG diagrams in the repository |
| `submodule-sync.yml` — **Submodule Sync** | Scheduled or manual | For both `Development` and `master`, moves every gitlink to the branch declared in `.gitmodules`, regenerates the Railway dependency manifest, validates equality, and opens or updates a review PR | A `sync-submodules-*` pull request; no branch deletion |
| `promote-development-to-master.yml` — **Promote Development to master** | Manual | Opens missing submodule Development-to-main PRs, verifies each production submodule SHA, regenerates/validates Railway pins, and opens or updates the parent promotion PR | Promotion PRs in submodule repositories and the parent OASIS repository; no branch deletion |

## Main CI test detail

The main pipeline's provider matrix exercises Redis, KeyDB, Dragonfly, Valkey, Garnet, Memcached, MongoDB, CouchDB, ArangoDB, ArcadeDB, SurrealDB, OpenSearch, Elasticsearch, Solr, Meilisearch, Typesense, Qdrant, Weaviate, Chroma, PgVector, ClickHouse, QuestDB, InfluxDB, and MinIO against real service containers where the workflow defines them.

The remaining jobs cover:

- solution and deployment-source integrity, including exact Railway manifest/gitlink equality on deployment branches;
- OASIS Architecture unit projects and aggregate integration tests;
- ONODE Core, WebAPI, and ONET behavior;
- STAR ODK and OGEngineClient tests;
- dependency/security scanning, formatting and code-quality checks, and coverage collection;
- the production `The OASIS - NoTests.sln` build and NuGet packing.

Unity edge release validation lives in `edge-runtime-validation.yml`, where the exact Unity editor is installed and the full release inspector runs. The placeholder `unity-tests` job in the broad pipeline is disabled and is not release evidence.

## Release locations

- GitHub releases: <https://github.com/NextGenSoftwareUK/OASIS/releases>
- MCP NuGet: <https://www.nuget.org/packages/NextGenSoftware.OASIS.MCP.Server>
- MCP npm: <https://www.npmjs.com/package/@oasisomniverse/mcp-server>
- GitHub Actions runs and retained artifacts: <https://github.com/NextGenSoftwareUK/OASIS/actions>
- GitHub Pages Web4 site: <https://nextgensoftwareuk.github.io/OASIS/>
- Railway deployment SHA source: `Docker/oasis-dependency-versions.env`

A GitHub release is not proof that every registry accepted a publication. Check the individual `release`, `publish-nuget`, and `publish-npm` jobs. npm trusted publishing must authorize repository `NextGenSoftwareUK/OASIS` and workflow `publish-mcp.yml`; a registry authorization rejection cannot be repaired by retry logic in the repository.

## Credentials and repository settings

| Name or permission | Used for |
|---|---|
| `PRIVATE_SUBMODULE_PAT` | Reading all private submodules during checkout and promotion/sync operations |
| `NUGET_API_KEY` | NuGet publishing in workflows that do not use registry trusted publishing |
| GitHub `id-token: write` | npm trusted publishing, Azure OIDC, and GitHub Pages deployment where configured |
| GitHub `contents: write` | Creating releases, committing generated Mermaid SVGs, and updating synchronization branches |
| GitHub `pull-requests: write` | Opening/updating promotion and submodule synchronization PRs |
| GitHub Pages source = **GitHub Actions** | Required for `deploy-oasisweb4.yml`; legacy branch builds cannot authenticate private submodules |

Do not put credentials in workflow files, DNA files, scripts, or documentation. Repository/environment secrets and registry trusted-publisher configuration are the owning layers.

## Reading a run correctly

A workflow is green only when every required job completes successfully. Skipped publishing jobs are expected on pull requests and ordinary Development pushes. They are not evidence of a release. For a production release, verify the platform build matrix, protocol smoke tests, registry-specific publish jobs, resulting GitHub release assets, and registry pages.

For the release sequence and dependency-pin invariant, also read:

- `Docs/Devs/MERGE_DEVELOPMENT_TO_MASTER_QUICK_START.md`
- `Docs/Devs/DEVELOPMENT_TO_MASTER_PROMOTION.md`
- `Docs/Devs/RAILWAY_DEPENDENCY_PINS.md`
