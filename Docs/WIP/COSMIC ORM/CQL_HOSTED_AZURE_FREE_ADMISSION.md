# Hosted Azure free-only demo admission

28October2026. User priority:hosted Cosmos before deeper parity; strict zero-cost constraint.
Status:not provisioned/not verified. No Azure account/resource/paid subscription created.

## Current evidence

- No Azure connector, standard CLI installation, .azure profile/config, Az PowerShell module or Azure/Cosmos environment names found. No secret contents read or printed.
- Official pricing documents a30day free hosted sandbox without an Azure subscription, alongside a separate lifetime free-tier offer.
- The legacy official trycosmosdb.azure.com link fails DNS here; owned IAB tab3 is a failed/error page, not provisioning evidence.
- Current cosmos.azure.com/try/ opens in owned IAB tab4 and displays “Under Construction”:“We are working on improvements for the Try Azure Cosmos DB service.” Only Sign In and an Azure Free Account link are offered.
- Lifetime free tier includes1000RU/s/25GB but can bill beyond limits, requires opt-in at account creation and is not serverless. Do not treat this as an unconditional zero-cost subscription.
- Azure free-account documentation describes spending protection until upgrade. User confirmed no existing free subscription. Free-account setup/authentication remains an external prerequisite; no paid upgrade is authorized.

## Acceptance still required

1. User-owned authenticated free account/subscription or available free sandbox, with verified zero-cost conditions. Never silently choose a paid account, serverless plan, upgrade or paid feature.
2. Dedicated journaled test scope, minimal owned fixture, real hosted endpoint (not emulator), lifecycle/resource/expiry inventory without secrets in docs.
3. Live rows/filter/projection/cardinality through shared CQL/manager/HTTP/browser, plus an explicit Mongo-paired federation preset showing both native providers/provenance.
4. Exact fixture cleanup; preserve pending/unknown outcomes and record any retained account/resource and safe teardown.

## Sources

- https://azure.microsoft.com/en-us/pricing/details/cosmos-db/standard-provisioned/
- https://learn.microsoft.com/en-us/azure/cosmos-db/free-tier
- https://learn.microsoft.com/en-us/azure/cosmos-db/optimize-dev-test
- https://cosmos.azure.com/try/ (live UI inspected28October2026)

This gate remains open. Emulator proof and under-construction pages do not satisfy hosted acceptance. Continue multi-record IPFS/SOLID/ActivityPub and Solana browser work while Azure prerequisites remain unavailable.


## Local federation preparation completed

Mongo+retained local Cosmos C#/HTTP/browser now passes5physical rows/4subset,
source provenance, policy rejection and exact cleanup on mergedCorea2e9b6c.
[Proof](CQL_MONGO_COSMOS_VERIFICATION.md). This removes the local federation
implementation gap; it does not satisfy any hosted endpoint/account/zero-cost gate.
No Azure resource/subscription/account was created and no billing terms accepted.


## Revalidated signup prerequisite

Official sandbox rechecked28October:still Under Construction. Its free-account link
opens [Azure account comparison](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account).
The free option advertises spending protection and requires moving to pay-as-you-go
to continue after30days/credit; footnote discloses a temporary$1 verification hold.
Followed only **Try Azure for free** to Microsoft's blank sign-in page and marked
that tab for user handoff. No identifiers/passwords/card data supplied, terms
accepted, subscription/account/resource created or paid option selected.
User asked whether they will complete signup or leave hosted Azure pending.
OAuth state/nonce URLs and credentials must never be copied into repository docs.
The next acceptance gates above remain unverified.


## Provider resource-admission prerequisite implemented

`CosmosResourceInitializationMode.ExistingResourcesOnly` now explicitly binds
pre-provisioned resources. Activation reads database/container schema and requires
matching container identity with exactly one `/id` partition path; missing or
incompatible resources fail with structured errors. There is no create/throughput
or provisioning fallback. Original public constructor signatures/default provisioning
behavior are preserved. [Usage](../../../Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/README.md).

Six new controlled cases prove existing3container success, missing database/container,
wrong identity/partition and invalid mode before client access. Strict mocks verify
no other client/database/container calls even when exceptions are caught.
All52affected non-native CQL cases pass/no skips; provider/consumer builds pass.
TRX TestResults/CosmosCqlCurrent/hosted-resource-cql-regression.trx SHA256
92872A8A41FDD071FE579928F4400113FD5EF7C9D4F960B05533AC7F46685413.
No native/hosted network requests were made for this change. Actual hosted schema,
account/free conditions and hosted SDK/HTTP/browser/Mongo acceptance remain red.
The mode controls infrastructure creation, not data permissions or Azure billing.


## Prepared hosted read entry point (not yet live-verified)

`Scripts/TestHosts/Run-HostedCosmosCqlRead.ps1 -ConfigurationPath <existing-json-path>`
builds the existing provider/consumer under one lease and invokes only the opt-in
hosted test. It never starts the emulator, provisions resources or substitutes
another provider. Supply the auth key through protected process environment
`OASIS_HOSTED_COSMOS_AUTH_KEY`; never command-line/config/repository/chat contents.

Configuration is non-secret, strict PascalCase JSON (unknown fields reject):

```json
{
  "Endpoint": "https://your-account.documents.azure.com/",
  "DatabaseName": "your-existing-owned-database",
  "Actor": "11111111-1111-4111-8111-111111111111",
  "ReadableIds": [
    "22222222-2222-4222-8222-222222222222",
    "33333333-3333-4333-8333-333333333333",
    "44444444-4444-4444-8444-444444444444"
  ],
  "FreeAccountSpendingProtectionConfirmed": false
}
```

Replace every example identifier with actual owned synthetic fixtures; do not run
unchanged. Only the operator may confirm free-account spending protection after
checking it; this Boolean is an assertion, not independent Azure billing proof.
The profile admits commercial Azure's canonical HTTPS documents.azure.com account
endpoint only, no localhost/HTTP/alternate port/path/userinfo/query/fragment/lookalike
host. It freezes3..8distinct nonempty readable IDs. Existing database and all3OASIS
containers with single `/id` partition keys are required; no auto-creation fallback.
Sovereign/custom/private endpoints require separate explicit admission.

The prepared live test performs shared HolonManager/LINQ ID filtering,2record subset,
Id/Name projection and exact LongCount. It creates no data or infrastructure and
leaves caller-owned records unchanged. Existing resources/credentials remain owned
by the operator. Evidence is TestResults/HostedCosmosCurrent/read-only-report.json
and hosted-read-current.trx; prior/unknown evidence is never overwritten.

14new admission/parser cases and66affected controlled CQL cases pass/no skips;
endpoint/lookalike/credential-field rejection, catalogue bounds/freezing and missing
confirmation are covered. Runner syntax/build pass. TRX
TestResults/CosmosCqlCurrent/hosted-entry-controlled-pass.trx SHA256
0564EC93CE5E1C6EDE8B90572B82B746B9C23122CCBA88431EAFE4ADEF04AB0F.
Initial broad controlled filter accidentally selected the hosted test because its
method name containedCql; its no-config guard failed before any client/network.
Renamed the separate native method and corrected exact runner dispatch; no skip or
mocked hosted pass. Original failed TRX retained as hosted-entry-controlled.trx.

Live hosted SDK/HTTP/browser/Mongo pairing, account/free verification and owned
hosted write/cleanup story remain red. This prepared read-only stage does not
replace those gates or the broader objective.


## Hosted HTTP/Mongo wiring prepared; external prerequisite remains

Add `-Mongo` to the hosted runner for the shared SDK and HTTP pairing acceptance;
add `-Serve` as well for http://127.0.0.1:5092/ . Serve requires explicit Mongo.
The cloud host is labelled **hosted Azure Cosmos DB** only for the explicitly
admitted hosted path; emulator runs retain their local label. Controlled HTTP
page tests verify both labels without invoking any provider.

The hosted path reads existing cloud fixtures only. It journals3synthetic Mongo
copies in the existing local test environment, proves5/4physical rows, rejects
forged actor/unknown preset input, and verifies exact local Mongo cleanup. Cloud
records/resources remain operator-owned and unchanged. Its structured report
separates hostedDataWrites0 from local Mongo fixture writes/cleanup and records
interactive-requested status without claiming browser clicks were verified.

68controlled CQL cases pass/no skips, runner syntax/build pass. Local native
Mongo+Cosmos SDK/HTTP regression1/1passes after this shared-host change; exact
4Cosmos/3Mongo cleanup/journals absent, services stopped. These are not hosted proof.
Controlled TRX SHA25683FCB9F5A3470FAD894ABD8629861BBB5CC17FA806ABF3EDA26D08B47F0C4528;
local native TRX SHA25668862E3DAC1DFDB8A810041DC5ED382862DFCFEB19A3B8498F9CFD995CDA539A,
both under TestResults/CosmosCqlCurrent.

Final prerequisite audit:hosted auth key absent and hosted configuration path absent
(only presence Booleans inspected; no credential contents read). Free signup remains
unanswered/pending; no authenticated hosted endpoint can run. The same external
condition has persisted across the hosted preparation milestones. All actual hosted
acceptance stays red. Deferred parity/unit-of-work/migration programmes remain
explicitly deferred by user priority; do not manufacture green-feature hardening
while waiting. Resume by completing user free-account signup/verification, binding
actual existing hosted fixtures and running this prepared acceptance, then verify
real browser/native evidence and remaining full scope. Never mark the goal complete.


User decision28October2026:leave hosted Azure pending. No further signup prompts,
provisioning, paid alternatives or credential requests. Prepared acceptance remains
available for a later explicit resume; hosted success is still unverified.
