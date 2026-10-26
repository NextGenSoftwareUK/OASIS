# COSMIC storage write accounting

Status: operation pricing kernel and configured admission checks implemented;
joined shipping HTTP/native-store/outbox write certification remains open.

Creator-owned graph saves perform physical ownership reads before one conditional
write. Those reads are not free or query attempts by implication. The existing
prepared storage execution now accepts explicit server-owned
`StorageAttemptTariff` rows for `LoadHolon`, `SaveHolon` and `SaveHolons` on
MongoDB, SQLite and Neo4j. Each completed physical invocation records its own
price under the original admitted attempt identity.

This is a service API tariff. It does not claim a database/cloud invoice, gas
price or native-token valuation. Existing native fee/resource evidence owners
remain in place. No request DTO may supply these prices or signers.

## Deployment profile

Example values below are fixture prices for explaining the contract, not a
recommended production price list. Supply approved values from your catalogue.

```json
{
  "nativeProviders": [],
  "ordinaryProviders": ["SQLLiteDBOASIS"],
  "storageTariffs": [
    {
      "provider": "SQLLiteDBOASIS",
      "operation": "LoadHolon",
      "costUsd": 0.001,
      "catalogueVersion": "fixture-storage-v1"
    },
    {
      "provider": "SQLLiteDBOASIS",
      "operation": "SaveHolon",
      "costUsd": 0.003,
      "catalogueVersion": "fixture-storage-v1"
    },
    {
      "provider": "SQLLiteDBOASIS",
      "operation": "SaveHolons",
      "costUsd": 0.003,
      "catalogueVersion": "fixture-storage-v1"
    }
  ],
  "routes": [
    {
      "method": "POST",
      "path": "/api/data/save-holon",
      "policy": {
        "meterCategory": "fixture-storage-write",
        "reservedUnits": 10,
        "estimatedCostUsd": 0.03,
        "fixedCostUsd": 0,
        "pricingCatalogueVersion": "fixture-storage-v1",
        "requiresProviderMeasurement": true,
        "maximumProviderAttempts": 10,
        "maximumNativeFeeCostUsd": 0
      }
    }
  ]
}
```

Store this server configuration under `PREPARED_STORAGE_ADMISSION_JSON` alongside
the existing usage-authority/outbox configuration. The existing usage and
prepared-admission middleware remain the single reservation/attempt/settlement
pipeline. `storageTariffs` permits ordinary-only profiles without a native quote
source. It does not disable required native quotes for signed native bindings.

Provide all three operation rows for every permitted ordinary provider when
enrolling a holon save route. Rows must be unique, nonnegative, from an approved
catalogue and within the reserved budget. The conservative service ceiling is
the highest configured operation price times `MaximumProviderAttempts`; it must
fit the estimated reservation after any native fee maximum. The example fits
0.003 ×10 =0.03 with no native fee exposure.

An existing graph containing N selected stored records ordinarily needs N
ownership reads plus one write. Generated identities need no ownership read.
The route's attempt and unit reservation must cover its intended graph sizes.
An exhausted attempt budget prevents the write; it does not authorize partial
graph commits. The SDK node limit is separate from the hosted attempt budget.

Enrollment remains exact method/path selection. Write classification recognizes
the existing single-save aliases only after their concrete path was selected by
the registry. It never grants a prefix enrollment. Query tariffs cannot price a
write. An unpriced hosted ordinary write is rejected before its physical attempt
or provider activation, including normalized method/path variants.

## Attempt and outcome semantics

HyperDrive passes the actual storage operation name to the existing prepared
owner in both synchronous and asynchronous routing. It waits for durable
attempt persistence before invoking the provider. A completed invocation,
including an explicit provider error, records one service receipt with
`COSMIC-storage-service-attempt-tariff` as its cost source. Empty/not-found reads
still represent completed attempts.

The receipt retains the exact provider and client/provider request identity;
receipt persistence is awaited before returning. A thrown or indeterminate
invocation produces no invented completed receipt. An uncertain write outcome
must be reconciled with the original operation identities. The service tariff
does not prove a data commit or replace COSMIC's conditional write receipt.

Original public five-argument `PreparedStorageExecution` and four-argument
`PricedStorageAdmissionFactory` constructors remain available. Their existing SDK
profiles retain their behavior. Configured save factories explicitly require the
storage operation profile; an unmeasured SDK scope is not proof of hosted billing.

## Verification and remaining work

Fourteen functional baseline failures were reproduced before enabling receipt
and rejection behavior. The focused tests cover all three operations/providers,
distinct read/write pricing through an actual owned-save manager route, durable
ordering, missing prices, completed errors and thrown outcomes. Additional
controls cover catalogue/budget/shape validation, immutable snapshots, normalized
route matching and original constructor signatures.

Full Core3,332/3,332 and normal WEB4 units214/214 pass, plus dual Core targets
and the shipping WEB4 build. The WEB4 gate excludes its separately documented
operator Mongo soak and external Stripe integration cases. Native CQL/service
regression passes all three providers,138SDK/138signedHTTP examples and345
settled CQL operations. Source freeze and six binary pairs match; Core consumer
copies matchBE5C06BB. Owned services/fixtures are cleaned. Evidence:
`TestResults/CosmicCqlDemo/storage-pricing-evidence.json`. Published Core:
`29a7725247c0b4c749b83452b58415ac43ee764f`.

Next is the joined proof: actual shipping DataController, authenticated HTTP,
real MongoDB/SQLite/Neo4j stores and the durable Mongo usage outbox under this
operation profile, followed by settlement and exact fixture cleanup. Existing
separate MVC and native-service tests do not establish that joined result.
