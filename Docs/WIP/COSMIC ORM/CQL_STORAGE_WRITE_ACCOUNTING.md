# COSMIC storage write accounting

Status: operation pricing, configured admission and the joined shipping
HTTP/native-store/outbox write gate pass on MongoDB, SQLite and Neo4j.
The focused signed GraphQL/gRPC save gate also passes on all three stores:
24 transport operations join 33 REST and 345 CQL operations for 402 settled
operations /436 physical attempts. Required proof is retained in
`Verification/signed-transports-final*`; see
[transport contract and limits](CQL_TRANSPORT_WRITE_AUTHORITY.md).
Broader deployed-startup, other transport methods, deletion and full CQL/ORM
certification remain open.

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

### Joined boundary verified — 26 October 2026

The shipping MVC serializer now creates ordinary `Holon` instances for nested
`IHolon` children. Payloads cannot select CLR types. The normal serializer owns
reference handling and depth limits, including during recursive binding. Three
previously failing HTTP cases now verify `SaveChildren=false`, `MaxChildDepth=1`
and `Recursive=false` without authorizing excluded foreign descendants.

Prepared admission now records affirmative zero-execution evidence when model
binding or action preflight ends before any provider preparation. Once preparation
could have started, the scope never invents zero-execution evidence, even if
durable preparation fails. Existing acknowledged CQL evidence is reused rather
than replaced with a different reason. The durable outbox still rejects this
claim if attempts or receipts already exist.

An additional root cause was reproduced during native integration: conditional
save planning applied ordinary process-local hydration rules to complete serialized
graphs. Explicit conditional plans now admit exact materialized stored membership
without granting ordinary-save observation authority. Known-partial and mismatched
serialized graphs still fail before storage. Child interface JSON also retains
`ChildRoleManifestJson`, so nested membership observations survive transport.
Four ownership/CAS baseline cases and one child-wire case failed before these fixes.

Current full gates: Core **3,342** and normal WEB4 **221** passing, zero skips.
Evidence is `TestResults/CoreCurrent/serialized-full.trx` and `serialized-web4.trx`.
The new joined test references the actual shipping WEB4 assembly and uses signed
HTTP, server-configured operation tariffs, native stores and the Mongo usage
outbox. It checks two clients, replay rejection, stale writes, denied foreign
children, atomic batches and depth selection, including exact settlement costs.
The first native run is preserved as `joined-native-red.trx` and
`joined-native-red-manifest.json`. Its foreign-child request contradicted the
parent's stored membership before ownership lookup; the corrected ownership
control uses a new parent, while separate incomplete-view controls retain that
structural rejection. Both retained failure fixtures passed exact-digest
terminal-state/ownership reconciliation before cleanup. The second assertion
failure was a test-helper value projection that deliberately omitted navigation;
independent native loading proved child retention before that helper was corrected.
No original operation was replayed.

The final live gate passes **36 signed shipping REST requests across three
providers**, including recursive child updates, **33 settled REST operations**,
the existing **138 SDK /138 signed CQL examples**, **345 settled CQL operations**
and **403 total physical attempts**. All **378** usage operations settled.
Every REST case checks exact attempt receipts, original request identities,
fixture service prices and settlement totals against the durable Mongo outbox.
Duplicate requests do not start another execution; stale batches preserve every
unchanged member. Owned fixtures and services are cleaned after terminal success.

Source snapshots are unchanged; seven producer/consumer binary pairs match,
including the actual shipping WEB4 assembly. Core unit/WEB4/unit-consumer copies
also match `6B453166`. Final evidence:
`TestResults/CosmicCqlDemo/joined-final-evidence.json`; native log
`joined-native-final.log`. Core implementation is published at
`8a1d42f9b6ce6e43539071b8bb0be85bdd898bf8`.

The reservation authority and avatar-directory binding are controlled fixture
services; JWT signature validation, shipping controller/middleware, native stores
and durable outbox are real. This does not certify external payments, deployment
configuration, service availability or the remaining gRPC/GraphQL paths.

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

The separate pricing checkpoint above is historical. The joined proof is now
recorded in the newer section; remaining acceptance includes full startup/alias
and concurrent-request configuration certification, gRPC/GraphQL authority and
authoritative recipient/tenant CQL enrollment.
