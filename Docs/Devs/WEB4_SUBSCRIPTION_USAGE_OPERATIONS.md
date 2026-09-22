# Subscription usage deployment and operations

This runbook accompanies the [protocol and sequence diagram](WEB4_SUBSCRIPTION_USAGE_LEDGER.md). Code/build success does not replace the credential-dependent live release gates below. Never restore the retired authorize-request counter to work around a rollout failure.

For the concise service-by-service variable list, use [Railway subscription configuration](SUBSCRIPTION_RAILWAY_CONFIGURATION.md).

See the [dated validation record](WEB4_SUBSCRIPTION_USAGE_VALIDATION.md) for the tested revisions, exact local results and outstanding production gates.

## Configuration ownership

| Variable | Where | Requirement |
|---|---|---|
| SUBSCRIPTION_MONGODB_CONNECTION_STRING | WEB4 | Optional protected override. When absent, WEB4 reuses `OASIS.StorageProviders.MongoDBOASIS.ConnectionString` from the deployed OASIS DNA. The resolved deployment must support snapshot transactions and journaled majority writes. |
| SUBSCRIPTION_MONGODB_DATABASE | WEB4 | Optional override; defaults to dedicated `oasis_subscription_accounting` |
| SUBSCRIPTION_LEDGER_INITIAL_STATE | WEB4 | For a verified new installation with no users or accounting records, set exactly `empty-new-installation`. WEB4 atomically refuses initialization if any ledger, billing or legacy source collection contains data. Omit for migrated installations. |
| SUBSCRIPTION_SERVICE_KEY_WEB5 through WEB10 | WEB4 | Distinct randomly generated secrets, at least 32 bytes each |
| SUBSCRIPTION_SERVICE_KEY_WEBn | Corresponding consumer only | Its own matching service credential |
| WEB4_API_BASE_URL | WEB5–WEB10 | HTTPS authority URL; loopback HTTP is permitted for local tests |
| SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING | WEB5–WEB10 | Optional override; defaults to the deployed OASIS DNA MongoDB connection |
| SUBSCRIPTION_OUTBOX_DATABASE | WEB5–WEB10 | Optional override; defaults to `oasis_webN_subscription_outbox` for the consuming service |
| SUBSCRIPTION_ADMIN_AVATAR_IDS | WEB4 | Comma-separated canonical avatar UUID allowlist; administrator JWT claim is also required |
| SUBSCRIPTION_MIGRATION_APPROVAL_KEY | WEB4 and offline manifest signer | Required only when importing existing accounting state; at least 32 bytes and never given to consuming services |
| SUBSCRIPTION_OTLP_METRICS_ENDPOINT | WEB4–WEB10 | Full operator-controlled HTTPS HTTP/protobuf metrics endpoint, e.g. collector /v1/metrics; HTTP only for loopback |
| SUBSCRIPTION_OTLP_HEADERS | WEB4–WEB10 | Optional collector authentication headers, supplied as deployment secrets |
| STRIPE_SECRET_KEY | WEB4 reconciliation | Optional protected override; otherwise reuses `OASIS.SubscriptionConfig.Stripe.SecretKey` |

The telemetry exporter sends only the registered subscription meters. Accounting identity/operation IDs belong in restricted logs and audit queries, not high-cardinality metric labels. Configure the collector and alert routing before opening paid traffic. [Collector example](../../Docker/monitoring/subscription-otel-collector.yaml) and [Prometheus rules](../../Docker/monitoring/subscription-alerts.yaml) are checked-in operational assets; set their protected listening interfaces for your environment.

WEB6 provider/model prices live in `NextGenSoftware.OASIS.Web6.Core/Configuration/web6-model-catalogue.json`; Railway does not carry a second copy. `ModelCatalogueManager` validates and loads that file. `Web6UsagePricing` validates uniqueness and nonnegative decimal rates, derives a reservation that covers every permitted provider call at the configured input/output limits, and persists the catalogue version. Bump that version with every rate change and retain old versions for audit. Unit/media adapters still require reviewed rates and supported receipt contracts before their execution gates can be removed.

### MongoDB deployment layout

The ledger does not require another MongoDB server. WEB4 may reuse the live MongoDB deployment already configured for `MongoDBOASIS`; it uses the official MongoDB driver against the separate database named by `SUBSCRIPTION_MONGODB_DATABASE`. Dedicated collections and roles are required because accounting needs cross-document transactions, purpose-specific unique indexes, Decimal128 money, immutable audit/evidence permissions and indexed reconciliation queries that the shared `holons` collection cannot enforce.

Each consuming service uses a separate outbox database on the same deployment or another persistent transaction-capable deployment. Give WEB4 and every outbox a distinct least-privilege database user. Sharing the cluster is supported; sharing the accounting database, outbox databases or broad application roles is not.

## Cutover and release order

1. Stop admission of billable work on any old consuming-service versions and drain in-flight calls.
2. Choose exactly one initialization path:
   - **New installation with no users:** set `SUBSCRIPTION_LEDGER_INITIAL_STATE=empty-new-installation`. The first transactional write checks every ledger, billing and legacy source collection and records an immutable initialization marker only when all are empty. Any existing record fails closed.
   - **Installation with existing users or accounting records:** inventory WEB4 legacy aggregates, old WEB6 usage keys, Holon subscription/order records, provider evidence and Stripe records, then run the reviewed signed opening-balance import. Never set the empty-installation value on this path.
3. Verify the initialization marker and remove `SUBSCRIPTION_LEDGER_INITIAL_STATE` after the first successful deployment. The stored marker remains the durable fence.
4. Provision required service credentials, durable outboxes, Mongo indexes/permissions, reviewed pricing and the OTLP collector. Test transactions against the actual replica set.
5. Build the parent repository with the exact API Core, STAR ODK and WEB6 gitlinks recorded in `Docker/oasis-dependency-versions.env`. Change each affected submodule gitlink and manifest SHA together. Do not clone moving branch tips.
6. Run all focused suites, all seven Release publishes and the dependency/source validators. Apply the documented Development → component main → parent master release workflow; feature branch tests alone do not authorize a production source-graph substitution.
7. Deploy WEB4 and the matching consumers as a coordinated cutover. Mixed old/new billing clients are unsupported; authorize-request returns 410 and does not increment anything.
8. Execute the live protocol, endpoint, provider, outage/recovery and reconciliation matrices in staging. Compare recorded provider calls, ledger totals and Stripe test invoices. Then enable the intended environment and verify every Railway service plus hosted functional routes.

For an incident, stop new provider admission while preserving workers, ledger and outboxes. Deploy a tested forward fix. Do not roll back to an old ledger writer, delete reservations, reset balances, or regenerate client idempotency keys.

## Local automated verification

Use .NET 10 and an isolated MongoDB replica set with test commands enabled for failure injection. Never enable test commands on production. The local test databases have generated names and are removed by their fixtures.

```powershell
$env:SUBSCRIPTION_TEST_MONGODB_URI = 'mongodb://127.0.0.1:27017/?replicaSet=ledger-test'
dotnet test ONODE/Tests/SubscriptionLedger.ProtocolTests/SubscriptionLedger.ProtocolTests.csproj -m:1
dotnet test "OASIS Architecture/NextGenSoftware.OASIS.API.Core/Tests/SubscriptionUsage/SubscriptionUsage.Tests.csproj" -m:1
dotnet test ONODE/SubscriptionReconciliation.Tests/SubscriptionReconciliation.Tests.csproj -m:1
dotnet test Tests/SubscriptionServiceAdapters/SubscriptionServiceAdapters.csproj -m:1
dotnet test Scripts/SubscriptionLedger.OutboxAdmin.Tests/SubscriptionLedger.OutboxAdmin.Tests.csproj -m:1
dotnet run --project WEB6/NextGenSoftware.OASIS.Web6.UsageProtocol.Tests/NextGenSoftware.OASIS.Web6.UsageProtocol.Tests.csproj
dotnet run --project WEB6/NextGenSoftware.OASIS.Web6.UsageRecovery.Tests/NextGenSoftware.OASIS.Web6.UsageRecovery.Tests.csproj
python Scripts/validate_web4_subscription_authority.py
pwsh -File Scripts/validate_web6_web4_metering_cutover.ps1
python Scripts/validate_railway_dependency_manifest.py --require-gitlinks
python -m unittest discover -s Scripts -p test_subscription_usage_live_runner.py
```

Authority tests require the URI. Reconciliation tests explicitly report skipped Mongo cases when it is absent; such a run is not a release gate. Full application test projects may contain unrelated historical failures; record their exact failures rather than disguising focused results as a successful full suite.

The protocol suites exercise concurrent quota admission, parallel identical IDs, changed-payload and ownership conflicts, exact decimal costs, all six consuming-service identities, actual Mongo rollback/commit behavior, time-boundary settlement, start/expiry races, durable outbox re-instantiation, duplicate receipts, HTTP start/flush behavior and missing evidence. WEB6 measurement tests exercise reviewed catalogue arithmetic and provider execution bounds. Provider integration still needs the external credentials below.

## Deployed matrix prerequisites

Use dedicated test subscribers with ample quota and no concurrent unrelated traffic. The matrix changes their test usage. Retain reports and operation IDs. Do not put secrets in command arguments, case manifests, Postman exports or result artifacts.

Required environment variables for the protocol matrix:

- WEB4_TEST_BASE_URL: staging WEB4 HTTPS URL.
- WEB4_TEST_BEARER: a validated, active test subscriber bearer.
- WEB4_SECOND_TEST_BEARER: a different subscriber, used for ownership-denial tests.
- SUBSCRIPTION_SERVICE_KEY_WEB5 through WEB10: staging service credentials.

```powershell
python Scripts/test_subscription_usage_live.py --concurrency 4 --report usage-live-results.json
```

This runs authorize/start/settle, retries/conflicts, service/owner security, failed/cancelled outcomes and concurrent reservations for every consuming service. It settles all operations in a passing run. If interrupted, inspect the recorded account's events and let expiry/reconciliation resolve remaining work; do not rerun providers.

To test actual service endpoints, supply `--service-cases approved-staging-cases.json`. Its array must cover WEB5–WEB10. Each entry contains service, baseUrlEnvironment, method, path, optional body and expectedStatus. Example shape:

```json
[{"service":"WEB6","baseUrlEnvironment":"WEB6_TEST_BASE_URL","method":"POST","path":"/v1/chat/completions","body":{"model":"YOUR_APPROVED_TEST_MODEL","messages":[{"role":"user","content":"Reply with OK"}],"max_tokens":8},"expectedStatus":200}]
```

The example is one entry, not a complete all-service manifest. Select valid test resources for the other five services. The runner requires an operation ID response and rejects duplicate provider execution on replay. It explicitly reports when endpoint tests were not requested. Provider credentials are required for paid endpoint cases; pricing comes from the versioned WEB6 model catalogue.

## Required provider and failure-injection matrix

| Scenario | Setup and evidence required | Passing invariant |
|---|---|---|
| Each deployed provider/model/modality | Provider sandbox credentials, reviewed prices, input fixture, provider receipt lookup/export | Measured units/cost and receipt IDs equal settlement |
| Failed provider call with charge | Sandbox fixture that incurs a documented charge then fails | Failed outcome retains observed charge |
| Cancellation/disconnect | Abort REST, SSE and WebSocket consumers after provider work begins | Original charge persists; no false terminal success |
| Batch/fan-out/job | Stable per-operation and per-provider intent/job IDs | Every child receipt exactly once; no hidden child work |
| Cache hit | Repeat business request with a new operation key and known cache state | Explicit internal/cache measurement; no phantom provider bill |
| WEB4 unavailable after provider success | Staging network fault between consumer and WEB4, then restore it | Visible pending response; durable original settlement eventually acknowledged once |
| Process crash after provider accepts | Kill only a staging consumer at each persisted intent/ack/receipt boundary | Resolver reads original job/evidence; never re-executes provider |
| Lease expiry vs late success | Short test lease and provider fixture longer than lease | Unstarted reservation releases; executing exposure stays held until definitive settle |
| Worker restart/replica race | Restart consumers and WEB4 while retries run | No duplicate provider execution, settlement or financial correction |
| Exhaustion under concurrency | Isolated subscriber at exact token/request/USD limit; multiple replicas | Only fitting reservations admitted; no lost increment |
| Stripe lifecycle | Stripe sandbox upgrade/downgrade/cancel/past-due/renewal and webhook replay | WEB4 entitlement follows verified lifecycle; no repeated order/billing effects |
| Reconciliation drift | Import mismatching test receipts and a usage-only Stripe test invoice | Persistent findings and operational alert; no silent balance mutation |
| Admin abuse | Ordinary JWT, forged/missing claim, disallowed admin UUID, wrong service key | Denied before accounting mutation |

These external checks must be marked NOT RUN when credentials, approved fixtures, receipt lookup or fault-control access are unavailable. A structurally registered endpoint is not proof that every provider adapter has a recovery contract.

## Operator actions

**Settlement pending:** find operation ID in the consumer outbox and WEB4 event projection. If the immutable settlement exists, restore authority connectivity/service credentials and let the delivery worker retry. Do not regenerate the settlement from a new provider call. A changed-payload conflict requires inspecting the original receipt/settlement before any correction.

**Deadletter after a repaired delivery fault:** use the [private outbox administrator command](../../Scripts/SubscriptionLedger.OutboxAdmin/README.md). It validates the deployed WEB4 JWT signature, administrator claim and allowlist, derives the actor from that identity, and atomically requeues the original terminal settlement with an action UUID and audit reason. It cannot resolve unknown provider work or rewrite receipts. Preserve the same action UUID when acknowledgement is uncertain.

**Recovery required:** inspect durable provider intents/job acknowledgements and query the provider by their original IDs. Use the registered read-only resolver or secure evidence workflow. No evidence means exposure remains held and the operation remains unresolved. Never turn an unknown outcome into a failed zero-cost settlement.

**Independent provider evidence:** POST `/api/subscription/usage/admin/reconciliation/provider-receipts` with provider source, avatar/month, operation/service/provider request ID, external receipt ID, USD amount and SHA-256 of retained evidence. Actor/time are assigned by WEB4. Repeated identical receipts are idempotent; changed evidence under the same ID conflicts.

**Stripe evidence:** POST `/api/subscription/usage/admin/reconciliation/stripe-invoices` with userId, month and invoiceId. WEB4 fetches Stripe directly and checks its subscription customer binding. Invoices must be finalized USD usage-only invoices carrying `oasis_avatar_id`, `oasis_usage_month` and `oasis_usage_cost_basis=ledger-cost-usd-v1`. The adapter uses subtotal, excluding tax. It does not compare a whole subscription invoice or silently assume provider cost equals an arbitrary retail price. Other commercial pricing models require an explicit versioned billing projection and matching adapter before rollout.

Stripe can deliver events out of order or more than once, and different events can share a creation timestamp. The billing transaction fetches canonical subscription state and deduplicates event and invoice IDs; it does not order lifecycle events using their timestamp alone. See [Stripe event delivery](https://docs.stripe.com/webhooks#event-ordering). The invoice.paid event also covers invoices marked paid outside Stripe, while invoice.payment_succeeded covers successful payment attempts; see [Stripe event types](https://docs.stripe.com/api/events/types).

**Run reconciliation:** POST `/api/subscription/usage/admin/reconciliation/run` with userId/month; GET `/api/subscription/usage/admin/reconciliation/reports` with those query fields. Reports identify missing provider/Stripe evidence, orphan receipts, duplicate/mismatched evidence, unsettled exposure, missing buckets and projection drift. HTTP success means the check ran; inspect Balanced/Findings. The continuous worker scans the distinct account-month keys from indexed audit, bucket and external evidence collections, so a missing bucket or orphan receipt remains discoverable. It logs failures or actionable reports.

Reports are point-in-time checks identified by CheckedAtUtc. Every production check refreshes Stripe evidence first; an invoice that has become void or fails its identity/cost-basis contract makes that check fail visibly. While the UTC month remains open, AwaitingPeriodClose is true and Stripe evidence remains incomplete; missing invoice and invoice-total comparisons wait until period close. An unsettled operation within its persisted execution lease remains pending. Once its lease expires, or if its expiry is missing/invalid, it requires action. Pending reports are never Balanced, but ActionRequired is true only for concrete findings, so ordinary active work and an open billing period do not trigger drift alerts. No invoice grace period is assumed.

**Correct accounting:** use the authenticated correction endpoint with a new correction UUID, original operation ID, explicit CorrectionKind, signed quantity/cost delta, reason and evidence reference. A provider-measurement correction must name Provider and ProviderRequestId already owned by the original operation; it adjusts the expected cost for only that receipt during independent reconciliation. A billing-adjustment has no provider binding and changes billing totals without claiming that the provider measurement changed. A replay must preserve all fields. Negative aggregate balances are rejected. Do not update existing audit/receipt documents or operation measurements. Rerun reconciliation and retain both the original finding and the correction.

**Historical opening balances:** fetch `/api/subscription/usage/admin/migration/inventory` and retain SourceDigest with independent Holon/provider/Stripe evidence. Prepare `UsageOpeningBalanceManifest` JSON using the production model (MigrationId, ApprovedBy, SourceDigest, EvidenceSha256, BatchIndex, BatchCount, Balances, Subscriptions, Orders). Each balance specifies canonical UserId, UTC Day, requests, reserved/settled tokens and USD costs. Subscriptions carries the reviewed Holon subscription snapshot; Stripe subscriptions also require StripeSubscriptionCreatedAtUtc to preserve ordering when an old subscription event arrives. Orders contains actual Stripe invoice IDs, owners, amounts and currencies; historical synthetic checkout orders require reconciliation against Stripe evidence before import. Do not manufacture invoice IDs for them.

Sign using `dotnet run --project Scripts/SubscriptionLedger.Migration -- sign reviewed-manifest.json signed-manifest.json`; the key comes only from SUBSCRIPTION_MIGRATION_APPROVAL_KEY. POST the signed file to `/api/subscription/usage/admin/migration/opening-balance` with the approved administrator bearer. Each batch has at most 1,000 entries across all three arrays, the same migration/source/evidence identity, and a unique zero-based batch index. Completion is marked only after all signed batches and legacy coverage checks pass; ordinary accounting remains blocked until then. Even a genuinely empty new store needs a reviewed empty bootstrap manifest so unknown historical Holon usage cannot be silently ignored. Preserve historical sources after migration.

An opening balance establishes reviewed aggregate totals; it does not manufacture missing provider measurements. Migrated settled operations with no per-provider receipts produce HISTORICAL_PROVIDER_EVIDENCE_REQUIRED and retain the original source hash in the report reference. They remain unverified until an explicit independent historical evidence contract establishes those measurements. A signed opening balance alone cannot produce a Balanced provider report.

## Retention, privileges and capacity

Give WEB4 transaction access to operation/bucket/subscription projections and insert/read access to audit, invoice, provider-dedup and evidence collections. Deny application update/delete on immutable collections. Current hosts call CreateIndexes on startup, so the runtime role also needs createIndex on the named collections, even when deployment already provisioned them. Verify the exact restricted role in staging; do not assume an unrestricted local Mongo test proves production permissions.

Use point-in-time backups and periodically restore them into an isolated environment, then replay/reconcile a closed period. Retention is an explicit operator policy. Do not enable TTL on unresolved reservations, audit history, provider dedup IDs or external evidence. Archive closed periods with account/month counts, exact decimal totals and SHA-256 manifests; verify the restored archive before deleting any eligible hot projection. Retain deduplication tombstones throughout the declared retry/support horizon.

Watch transaction latency/contention, oldest unresolved execution, outbox attempts/deadletters, quota denials, reservation overruns, duplicate spikes, reconciliation drift and telemetry delivery itself. Test alert routing using staging fixtures. Reconciliation errors must remain visible; increasing retry counts or suppressing alerts is not a root-cause fix.
