# Cosmos DB CQL implementation gates

Source audit: 27 October 2026. CQL enrollment remains unimplemented/unverified.
This is the next cloud-demo work item, not evidence of a passing cloud demo.

## Existing ownership and stored schema

- Provider: `Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/`.
- `Infrastructure/HolonRepository.cs` uses `holonItems`, canonical `Holon` projection,
  and the logical GUID as its partition value. Avatar/detail containers are separate.
- `CosmosDbRepository.DeserializeEntity` validates native `id`, logical `Id`, expected
  identity and duplicate identities before canonical hydration. Preserve this owner.
- `CosmosDbClient.History.cs` owns writes, observed native ETags, conditional replacement,
  exact acknowledged-body validation and optional embedded history. CQL must select
  current records without treating embedded historical payloads as additional holons.
- `CosmosDbClient.cs` has parameterized native field queries and SDK stream iterators,
  but its all-document methods collect the full container. They are not a CQL executor.
- `ICosmosDbClient` currently has no bounded, cancellation-aware CQL command contract.
  Introduce one native command/read owner rather than managed filtering over ReadAll.

## Precision is a query concern

`Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/Binary64JsonNumberCodec.cs`
encodes non-roundtrippable JSON numbers as native strings. `_oasis_exact_numbers`
binds structural paths to literals using schema `binary64-v1`; canonical decoding
restores the numeric values. Ordinary JSON strings remain strings.

Therefore raw native comparison/order/aggregation of a metadata path cannot assume
that every persisted numeric value is a Cosmos JSON number. String sorting or a
lossy conversion is not canonical CQL numeric semantics. Planning must reject
unsupported domains, or a deliberately specified native representation must prove
exact semantics. Checking only the query constant is insufficient: stored operands
also matter. Projection must retain enough envelope information to validate/decode
selected cells without silently retyping unrelated strings.

Existing evidence source:
`Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/CosmosQueryScalarNativeTests.cs`
contains independent point-read/wire/canonical assertions for a large fractional
number and date-shaped string. Its metadata selector assertion does not prove CQL
native numeric predicates. No tests were rerun for this source-audit checkpoint.

## Required implementation and proving tests

| Gate | Required proof | Status |
|---|---|---|
| Capability admission | Explicit supported fields/operators/types; unsupported shapes rejected before SDK I/O | Open |
| Native command boundary | Parameterized predicates, bounded page size/output, cancellation/deadline; no ReadAll reduction | Open |
| Visibility | Actor/deletion/protected metadata rules identical to admitted CQL scope | Open |
| Types | Missing/null/string/bool/numeric distinctions; exact-number envelopes cannot change comparison semantics | Open |
| Ordering/paging | Deterministic identity tie-breaks and required Cosmos index policy; no hidden index creation | Open |
| Projection/count | Native execution, exact cell validation and DTO construction ordering; unsupported shapes explicit | Open |
| Read consistency | Declare actual cross-partition/page guarantees; no assumed container-wide snapshot | Open |
| Failure | SDK rejection, malformed response, cancellation, budgets and unsupported shapes preserve errors | Open |
| Demo lifecycle | Same synthetic records, acknowledged revision/delta, exact owned-fixture cleanup | Open |
| Hosted acceptance | Hosted endpoint execution, separately labeled from retained emulator evidence | Open |

Reuse `Scripts/TestHosts/Run-ReusableCosmosDBTests.ps1` and its documented retained
resources after inspecting their current ownership/configuration. Do not start a
replacement emulator or replay pending fixture journals. Existing test code that
changes database ownership after failure must be understood before adding fixtures.

Next: inspect the Core row-query capability/AST contract and Cosmos runner's resource
inventory; define the first native translator profile and focused tests against the
stored representation above. Keep all gates open until their proving evidence exists.
