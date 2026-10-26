# CQL federation HTTP v1

26 October 2026. Implemented and unit verified; signed real-provider federation
HTTP/billing acceptance is still pending. The last fourteen real-provider SDK
scenarios predate hosted authorization and typed materialization extensions.

`POST /api/data/query-cql/federation` requires an authenticated WEB4 actor,
HyperDrive V2, the route's configured storage admission and explicit ordinary
query tariffs. It shares the existing CQL resource filter and usage attempt owner.
The request is at most 32 KiB. Unknown fields are rejected; clients cannot supply
grants, actors, tariffs, capabilities or executable query text.

## Scope and access

| Scope | Providers | Meaning |
|---|---|---|
| CurrentDefault (default) | Empty | Resolve and capture the existing default once. |
| Named | One to three unique concrete names | Every requested target must be eligible and available. |
| AllEligible | Empty | Select tariff-admitted MongoDB, SQLite and Neo4j targets; a required unavailable target fails. |

Concrete names are MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS. There is no
activation, implicit failover, retry or silent omission. The application-owned
ICqlReadAccessPolicy is captured per target before any native dispatch. Every
policy must bind the captured actor and that provider's authority. A later denial
rejects the entire request before earlier authorized targets can execute.
Unenrolled hosts retain the explicit owner/public contract; required-policy hosts
fail closed when their authorizer is missing or invalid. Metadata labels do not
grant access.

## Independent source and global windows

`source` is the existing bounded CqlV1Request filter/order description. Its
Provider must be omitted and Graph must be null. Source Take is 1–100 (default25),
with the existing source Skip bound. These windows apply independently at each
provider, after its mandatory access predicate. Global Skip/Take apply after
identity selection and global ordering. A source window deliberately limits the
participating source; results outside that window are not part of this intent.

IdentityMode is PreserveCopies (default) or ProviderPriority. The latter requires
ProviderPriority to name every resolved target exactly once. Priority selects a
deterministic provider before the global page; it does not mean newest. Observed
accessible copies are retained. CausalAgreementEstablished is false. Repeatable
snapshots, causal agreement, continuation cursors and distributed joins are not
part of this HTTP version.

## Budgets

| Property | Default | Admitted range |
|---|---:|---:|
| Skip | 0 | 0–1000 |
| Take | 25 | 1–100 |
| MaximumInputRows | 300 | 1–300 |
| MaximumRowsPerProvider | 100 | 1–100 |
| MaximumInputBytes | 4194304 | 1–16777216 |
| MaximumOutputBytes | 1048576 | 1–1048576 |
| MaximumWork | 100000 | 1–10000000 |
| MaximumProviderAttempts | 3 | 1–3, at least the selected target count |
| MaximumDurationMilliseconds | 5000 | 1–5000 |

The same cumulative budget spans complete native inputs, reconciliation,
ordering, typed mapping/decryption and canonical output. The successful public
JSON body must also fit MaximumOutputBytes and the one MiB hard response limit.
Errors return no partial rows. Native attempts already performed retain their
receipts when a later target or output check fails. Input truncation is never
reported as a complete source.

## Example

Use the application's WEB4 bearer token and its existing admitted route setup.

```json
{
  "scope": "Named",
  "providers": ["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"],
  "source": {
    "filters": [
      {
        "metadataPath": ["category"],
        "scalar": "String",
        "operator": "Equal",
        "value": "coffee"
      }
    ],
    "orderBy": "Name",
    "skip": 0,
    "take": 100,
    "includeMetadata": false
  },
  "identityMode": "ProviderPriority",
  "providerPriority": ["SQLLiteDBOASIS", "MongoDBOASIS", "Neo4jOASIS"],
  "skip": 0,
  "take": 10
}
```

The OASISResult result contains version cql-federation-v1, captured providers,
MovingProviderReads consistency, ObservedInputRows, ResolvedRows and rows. Each
row has selected/copies, each with provider and the existing public CQL leaf row
DTO; provider keys, opaque revisions and runtime objects are not exposed.
SelectionReason and CausalAgreementEstablished describe the explicit choice.
Counts include only authorized participating source rows. The selected row and
each observed copy are projected using their own target policy.

Authentication/admission failures are rejected before lookup or execution.
Malformed intent returns400; authorization denial403; missing/invalid required
policy503; an expired cumulative deadline504; canonical/public output overflow413.
Other execution failures preserve their explicit error code. Error responses may
contain the error envelope even when a caller selected a very small output limit.

## Verification and remaining gates

Core dual-target and shipping WEB4 builds passed. Full Core3,636 tests passed,
including nineteen hosted federation cases covering three target policies,
source/global paging, exact per-target tariff receipts, captured actor/default,
late denial before dispatch, invalid descriptions, authentication/admission,
unknown grant/tariff fields and actual JSON wire size. The existing shipping WEB4
suite passed320 with one existing Stripe sandbox skip.

Retained proof: Verification/federation-http-{core,web4,initial-core}.trx.gz and
federation-http-identities.json. Signed actual-controller tests on real MongoDB,
SQLite and Neo4j, exact durable federation billing, native policy separation,
typed/encrypted acceptance and annotated HTTP demo enrollment remain required.
See CQL_FEDERATION_CONTRACT.md and CQL_COMPLETION_ROADMAP.md.
