# Subscription usage implementation validation — 2026-09-22

This is a tested coordinated release candidate, not a completed production rollout. The [protocol](WEB4_SUBSCRIPTION_USAGE_LEDGER.md) and [operations runbook](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md) define the implementation and the remaining external acceptance gates.

## Recorded source graph

| Component | Tested feature revision |
|---|---|
| OASIS implementation | 08351e61c; concurrent Development changes integrated in 26bb4bd81 |
| API Core | 268891d061a39f82057f842f4863b3a43be424e0 |
| STAR ODK | 45c33bd9ebabd3a6bd79c9cb0f25d835f6e5c206 |
| WEB6 | e7f0c9319a8f5ceb8065afb611f88abd59404340 |

The parent gitlinks and Docker/oasis-dependency-versions.env agree. Other deployed dependencies retain the manifest's existing pins. The parent merge preserves Development's Mongo single-ID regression and development URL diagnostics, adapted to the new bucket model and usage protocol, plus the concurrent quest fixture documentation.

Core Development subsequently introduced ProviderCategories → ProviderCapabilities changes. The ledger Core branch now contains those commits, and the parent provider implementations use the renamed capability contract directly. No compatibility shim was added. The updated coordinated graph must pass the protocol workflow before changing a deployment branch.

## Local results

Environment: Windows, .NET SDK 10.0.401, isolated MongoDB 8.0.15 replica set with test-command fault injection. Mongo tests used an explicit connection URI and generated disposable databases. No production data, provider credentials or Stripe credentials were used.

| Check | Result |
|---|---|
| WEB4 Mongo protocol, billing transactions, migration, JWT and failure injection | 74 passed; no skips |
| Full ONODE WebAPI.UnitTests project | 92 passed; no skips (includes DNA/default database, empty-installation and existing Railway Stripe override checks) |
| API Core usage SDK/outbox/recovery/HTTP lifecycle | 63 passed |
| Independent reconciliation and Mongo discovery/corrections | 46 passed; no skips |
| Private outbox administrator authentication and Mongo requeue | 17 passed |
| WEB5 and WEB7–WEB10 transport policies | 25 passed |
| Existing WEB6 UsageMeteringTests | 10 passed |
| WEB6 pricing, execution scope and audio verification | 26 executable assertions passed |
| WEB6 provider recovery and injected failures | 45 executable assertions passed |
| Live-runner offline transport security | 5 passed |
| WEB4, WEB5, WEB6, WEB7, WEB8, WEB9, WEB10 Release publishes | All seven passed |
| Private operator Release publish and published command execution | Passed |
| Opening-manifest signer build | Passed |
| Authority/cutover structural validators | Passed |
| Railway manifest, seven Dockerfiles and committed gitlinks | Passed |
| Workflow/collector/alert YAML syntax; generated Postman JSON | Parsed successfully |
| Diff whitespace check | Passed |

Release publishes used -c Release -m:1 /p:UseAppHost=false. The new subscription-usage-protocol workflow runs the protocol, application, SDK, reconciliation, operator and service-policy suites, both WEB6 executables, transport-security tests and all seven publishes against a disposable replica set.

The WEB4 Stripe dependency now explicitly selects [Stripe.NET 47.0.0](https://www.nuget.org/packages/Stripe.net/47.0.0), which was already the resolved assembly used by the successful tests. The former nonexistent 46.3.0 reference relied on NuGet substitution. Mongo and Microsoft.Extensions references touched by the new shared dependency graph are aligned explicitly.

## Unresolved repository and release gates

- The repository-wide solution-coverage script still fails for 51 projects already present in the original pinned source graph; zero newly added projects are uncovered. This is not a successful full-solution build. Existing NuGet/compiler warnings remain, including SharpCompress/Snappier advisories and older dependency constraints.
- Credentialed staging protocol and real business endpoints, provider/model/modality receipt comparisons, paid failure/cancellation, process-crash and network-fault matrices, and Stripe lifecycle/invoice comparison are NOT RUN. The supplied offline/fake-provider tests do not substitute for them.
- The operator confirmed this is a new installation with no users, so historical import is not part of this rollout. Set `SUBSCRIPTION_LEDGER_INITIAL_STATE=empty-new-installation` for the first WEB4 deployment; its transactional guard refuses any nonempty ledger, billing or legacy source collection. Remove the setting after the durable initialization marker is created. Existing installations still require the signed migration path.
- Reviewed production provider prices, service/admin credentials, actual Mongo roles/capacity, telemetry ingestion and alert routing remain deployment prerequisites. No production price catalogue was invented.
- Specialized WEB6 adapters without measurement/recovery contracts remain gated before network execution. See WEB6/docs/SUBSCRIPTION_USAGE_PROTOCOL.md for the precise supported and rejected paths. HTTP SSE output is currently buffered until settlement acknowledgement; incremental HTTP streaming remains incomplete.
- Stripe reconciliation currently covers the explicit ledger-cost-usd-v1 usage-only cost basis. Other retail markups, discounts, credits, taxes or whole-plan invoice comparisons require a defined billing projection before use.
- Docker/Railway deployment and hosted validation are NOT RUN. Feature branches and draft PRs do not complete Development-to-production promotion. Follow the source-graph promotion policy after the remaining gates pass.

The next required inputs are the protected configuration locations for staging/provider/Stripe access, the approved provider price catalogue, and retained historical billing evidence. Keep credentials out of chat, PRs, manifests and result artifacts.
