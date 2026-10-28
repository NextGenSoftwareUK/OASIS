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
