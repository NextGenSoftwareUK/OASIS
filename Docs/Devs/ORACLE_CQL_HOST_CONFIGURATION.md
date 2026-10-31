# Oracle CQL host configuration

Oracle reads canonical OASIS records from an existing Oracle Database. Its query
profile supports bounded ID equality/membership, name/description equality or
substring matching, string metadata equality, Boolean combinations and paging.
Use `orderBy: "None"`; sorting, graph loading and full EF parity are not enrolled.

Current implementation and deployment evidence:
[CURRENT_STATUS](../WIP/COSMIC%20ORM/CURRENT_STATUS.md). Oracle is not yet verified
in the public sandbox. Do not equate local activation or planning tests with
hosted connectivity, live query results or a completed Mongo-paired demo.

## Existing database and read-only activation

Configure the existing Oracle provider through its structured DNA settings and
retained secret/wallet mechanism. Do not put connection credentials into examples
or reports. The host must actually reach that database with a valid native client
configuration; this guide does not provision an OCI account or database.

The hosted source registry calls `ICosmicReadOnlyQueryActivation`, which validates
the existing canonical schema without provisioning or repairing objects. Missing
or incompatible schemas and cancellation produce explicit errors. Ordinary
storage setup remains a separate administrative operation.

Enroll Oracle as query-only with an explicit query tariff in the host's prepared
storage admission configuration. This does not authorize writable replication
routes. A zero tariff is an explicit API pricing choice, not a guarantee about
the database hosting account's costs. Enrollment alone does not imply readiness.

## Complete server-side read catalogue

The native Oracle compiler requires an authoritative record catalogue. For a
reviewed synthetic public demo source, configure `CQL_PUBLIC_READ_CATALOGUES_JSON`
as a provider-to-ID-array object, for example:

```json
{
  "OracleDBOASIS": [
    "11111111-1111-4111-8111-111111111111",
    "22222222-2222-4222-8222-222222222222"
  ]
}
```

Replace these illustrative IDs with the complete reviewed source selection.
**This is a host grant to every authenticated caller**, including demo avatars.
It is not inferred from a source record's public flag. Only list records whose
entire returned content the host intends to expose. Never populate it from query
body IDs, client metadata hints or an unreviewed search result.

Each catalogue is immutable after startup and accepts at most 128 unique,
nonempty GUIDs. Empty arrays grant no records. Invalid/duplicate provider names,
invalid/duplicate IDs and oversized configurations fail configuration loading.
Public or owned records outside the complete catalogue remain inaccessible.

The same policy instance is registered through standard keyed dependency
injection for its concrete provider and used for public example planning. The
request's authenticated actor is bound at authorization time. Other providers
retain owner/public visibility unless they have their own policy. No Oracle grant
crosses into MongoDB, SQLite or Neo4j during federation.

A registered global `ICqlReadAccessPolicy` takes precedence. Its rejection or
failure cannot fall through to a public grant. A registered policy returning null
is an error. The existing `CQL_REQUIRE_AUTHORITATIVE_READ_POLICY=true` requirement
still requires a global application policy; provider-scoped public catalogues do
not silently satisfy or weaken that requirement.

## Queries and examples

Authenticate normally, then POST a CQL request to `/api/data/query-cql` with
`provider: "OracleDBOASIS"`, `orderBy: "None"` and `take` between 1 and 100.
Use an ID `In` filter with scalar `Guid`, a name/description string filter, or a
string metadata equality filter. The host catalogue is added independently before
native selection and paging; values are bound as Oracle parameters.

Configure public presets through `CQL_HOSTED_QUERY_EXAMPLES_JSON`. Discovery uses
the same captured public catalogue when checking the adapter's real capabilities.
A source without the necessary policy must not advertise unsupported examples as
ready. Actual requests are authorized again for their real actor.

Oracle/Mongo federation authorization and planning are implemented. The current
browser snapshot helper supports metadata-key membership; Oracle's ID-membership
selection still needs its preparation wiring and end-to-end acceptance. Do not
advertise fixed five-row/four-row pairing until that path and actual hosted SQL
execution, isolation and replica checks pass.

## Verification

`Scripts/TestHosts/Test-OracleQueryActivation.ps1` reuses the retained local
Oracle runtime/source and verifies unchanged schema/four records around query
activation. It requires existing builds and the provider build lease; it stops
only the container it started. No source records or users are created.

`CqlProviderReadCatalogueTests` covers host capture, provider isolation, policy
failure and federation. `CqlOracleHostedAdmissionTests` covers the nine-provider
admission catalogue and real Oracle SQL compilation from HTTP descriptions.
`CqlHostedSourcesTests` checks discovery with the configured catalogue. These
controls do not replace the outstanding native hosted/browser acceptance gates.
