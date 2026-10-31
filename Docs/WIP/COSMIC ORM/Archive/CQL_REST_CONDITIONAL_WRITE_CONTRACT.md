> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

# V2 REST conditional graph writes

Status: controller/service migration, controlled MVC gates and joined signed
shipping DataController HTTP with MongoDB/SQLite/Neo4j and durable usage settlement
are verified. Full deployment startup/alias/concurrent configuration and remaining
transports stay open. This is not a production-completeness claim.

Latest gate: Core3,342 and normal WEB4221 passing, zero skips;36 signed shipping
REST requests/33 settled REST operations across all three stores, plus the existing
138SDK/138CQL walkthrough. Complete serialized child graphs, full recursive child
updates, depth selection, foreign denial, replay/stale conflicts and atomic stale
batches pass with exact service receipts. See [joined evidence and limitations](CQL_STORAGE_WRITE_ACCOUNTING.md).

The existing `POST /api/data/save-holon` and `POST /api/data/save-holons` actions
use the generic Core owned graph APIs in HyperDrive V2. Existing single-save
aliases delegate to the same action. Legacy behavior is unchanged and its gaps
remain documented in the [owned graph contract](CQL_OWNED_GRAPH_WRITE_CONTRACT.md).

## Request intent

```json
{
  "operationId": "11111111-1111-4111-8111-111111111111",
  "providerType": "MongoDBOASIS",
  "saveChildren": true,
  "recursive": true,
  "maxChildDepth": 4,
  "expectedRevisions": {
    "22222222-2222-4222-8222-222222222222": "33333333-3333-4333-8333-333333333333"
  },
  "holon": {
    "id": "22222222-2222-4222-8222-222222222222",
    "name": "Edited by its creator"
  }
}
```

Use `holons: [...]` for the bulk action. Supply all required selected-node
conditions, including children. Revision values are opaque provider revisions,
not logical `VersionId` values. A null condition means create-only. The empty
GUID is an actual legacy revision, not an absent record. Creator writes may
instead preserve provider revision metadata from their loaded holons; fresh
ownership reads never replace a stale caller revision.

An operation ID is required and must remain stable when reconciling an unknown
outcome. It can appear in the body or one `X-COSMIC-Operation-Id` header. The
header accepts a nonempty D-format GUID; coalesced duplicate values and
contradictions with body intent are rejected. This supports the existing
single-save aliases whose bodies contain a holon rather than the request DTO.
It is distinct from the host usage reservation's `Idempotency-Key` contract;
neither key replaces the other when admission is enabled.

Actor identity and Wizard status come from the server-authenticated avatar,
captured before configuration or an await. Payload creator fields and role
labels grant no access. Ordinary callers authorize every selected existing node
at the exact write authority before mutation. A later foreign root/child or an
inconclusive lookup prevents every batch mutation. One conditional transaction
commits the selected batch; `ContinueOnError` cannot request partial commits.
Selection still uses `SaveChildren`, `Recursive` and `MaxChildDepth`.

The existing trusted server Wizard policy is retained, using conditional Core
saves rather than an unconditional bypass. Wizards must provide valid caller
revision intent for existing selected records. This is not a new tenant-role
authorization model or permission granted by a request-body field.

## Responses and settings

The V2 controller sets the HTTP status and response envelope consistently.
Error-only bulk responses copy result flags without invoking a payload mapper:

| Result | HTTP status |
| --- | --- |
| Acknowledged save or valid no-write result | 200 |
| Missing authenticated actor | 401 |
| Foreign selected record | 403 |
| Stale provider revision | 409 |
| Invalid operation/configuration intent or missing required revision | 400 |
| Unknown or unavailable engine mode | 503 |
| Inconclusive authority/admission/provider failure | 503 |

The `OASISResult` error code remains in the envelope. A 503 with an unknown
mutation outcome is not permission to replay using a new operation ID. V2 bulk
settings are reset in a finally block on ownership denial, lookup exceptions
and failed writes, subject to the existing explicit `SetGlobally` contract.

## Admission and configuration ownership

Save routing requires an explicit supported engine mode before configuration.
Missing DNA, a blank mode or an unsupported mode returns
`COSMIC_WRITE_ENGINE_MODE_REQUIRED`/503 with no provider calls. The boundary
never interprets an unknown mode as permission to run an unconditional write.
Explicit `Legacy` mode retains its existing execution path. Server startup must
load its engine configuration before this save boundary can execute.

V2 configuration validation continues to use the existing configuration owner.
Invalid replication/failover/load-balance modes or provider-list input returns
`COSMIC_WRITE_CONFIGURATION_INVALID`/400 with `IsError=true`; it cannot be
reported as successful HTTP200 merely because validation returned an envelope.

V2 save actions validate actor, input shape and operation intent before engine
configuration or manager acquisition. Invalid requests therefore cannot apply
`SetGlobally` changes. If the host registered `StorageAdmissionRoutes`, a missing
inherited admission returns `COSMIC_WRITE_ADMISSION_REQUIRED`/503 before any
configuration or storage dispatch. An alias not enrolled in the exact registry
cannot silently use unconfigured execution. Explicitly unconfigured deployments
retain their documented mode.

Under an inherited prepared admission, both synchronous and asynchronous base
configuration methods defer provider activation to HyperDrive's physical attempt
owner. The real MVC activation control observes the durable-attempt callback
before provider activation. No query price, native invoice or zero write charge
is manufactured by this guard.

## Verification and remaining gates

The linked tests compile the actual shipping service and DTOs. Eleven new
cases cover stable intent, exact header handling, forged child/bulk ownership,
conditional Wizard behavior and null-versus-empty revision round trips.
Full Core3,302/3,302 and shipping WEB4 build pass. Native service verification
against MongoDB, SQLite and Neo4j passes all three providers: foreign bulk roots
are denied, header-based writes succeed and stale writes conflict. The existing
138SDK/138signedCQL HTTP examples also pass,345settled operations. Source and
six producer/consumer binary identities match; owned fixtures are cleaned.
Evidence: `TestResults/CosmicCqlDemo/hosted-write-evidence.json`.
This verification is separate from actual
DataController HTTP verification and from the signed CQL query walkthrough.

Nine actual shipping MVC/Kestrel controls pass, exercising the real custom
authorization filter, model binding, DataController, envelope and HTTP status.
Storage and the authenticated avatar are controlled fixtures; this is not the
full deployed startup/JWT/real-store/settlement chain. Five functional baseline
failures were reproduced first: an unenrolled alias executed, invalid single
intent changed global settings, and three bulk error cases returned500. The
corrected controls also cover forbidden/outage finally restoration, actual409
and admitted activation ordering. The normal WEB4 unit gate passes200/200,
excluding its separately documented operator Mongo soak and external Stripe
integration cases. Full Core remains3,302/3,302, zero skips in those gates. The final separate
three-provider native gate also passes after these changes; source freeze and
six binary pairs match, and shipping WEB4 matches its HTTP consumer5D3049F6.
Owned native services/fixtures are cleaned. Evidence:
`TestResults/CosmicCqlDemo/hosted-http-evidence.json`.

Before declaring hosted writes complete:

- Canonical single/batch DataController HTTP now passes the joined native
  admission/outbox/settlement gate, including acknowledgements and child selection.
  Extend it to unknown-outcome recovery and the full deployment/startup matrix;
  the verified fixture authority does not certify external billing services.
- Expand the option/alias and startup certification matrix as hosted native
  integration grows. Controlled HTTP tests now cover absent/blank/unknown mode
  and invalid mode/provider-list options, alongside the existing supported-V2
  request controls; they do not prove the complete deployed startup chain.
- Keep canonical and alias enrollment explicit; guards prevent missing-policy
  execution but do not invent deployment route registrations or tariff budgets.
- Actual ownership reads and conditional writes now use explicit server operation
  tariffs with joined native settlement proof. Keep catalogue approval and exact
  deployment enrollment explicit. Service prices do not establish native invoices.
- Finish gRPC save migration and authenticated GraphQL actor binding before
  enrolling authoritative recipient/tenant CQL access.


Latest mode/options gate: all14new HTTP regressions failed before the fix.
The final normal WEB4 gate passes214/214 including23actual MVC cases; Core
passes3,302/3,302 and shipping WEB4 builds. The two explicit external/operator
integration exclusions above remain unchanged. DLL20F4E5F1 matches the HTTP
consumer. See `TestResults/CosmicCqlDemo/mode-options-evidence.json` for identities,
filters and counts. This new gate covers HTTP mode/configuration ownership and
does not claim a new native-provider/billing run.


Ordinary read/single-save/batch-save attempts now support explicit server-owned
service tariffs in the existing prepared ledger. Query tariffs do not price
writes; configured save routes require bounded complete profiles. See
[storage write accounting](CQL_STORAGE_WRITE_ACCOUNTING.md) for the JSON profile,
price-versus-native-invoice distinction and remaining joined settlement proof.
