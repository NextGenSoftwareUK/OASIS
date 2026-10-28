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
