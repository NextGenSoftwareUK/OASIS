# Cosmos DB CQL implementation gates

Source audit: 27 October 2026. Canonical row enrollment is implemented; wider demo acceptance remains incomplete.
The native row gate uses an emulator; hosted-cloud acceptance remains unverified.

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

## Bounded transport checkpoint

CosmosDbClient.CqlRead.cs now owns parameterized SDK stream iteration with explicit cumulative response-byte, page and row limits plus cancellation. It publishes only after every page validates and uses canonical exact-number decoding. No CQL capability is enrolled yet.

CqlBoundedReadTests: six passed, zero failed/skipped; provider build passed. These simulated SDK pages prove transport behavior, not hosted/native SQL semantics. Evidence: TestResults/CosmosCqlCurrent/cosmos-cql-bounded-read.trx; SHA256 C76FFC3EBA3E17211454D1127189C61B8B62734187AC5DB3F4C9ADA8F74CDF45. Translator, visibility, numeric-domain admission and native/cloud gates remain open.

## Canonical predicate compiler checkpoint

CosmosCqlPredicateSql now translates Boolean composition and whitelisted Name/Description, IsActive/IsPublic and Int32 Version comparisons with SDK parameters. String null/missing and inequality normalization are explicit. Unsupported fields, metadata and mixed numeric comparisons reject before command execution. This is an internal compiler foundation, not provider enrollment or native semantic proof; metadata/string/numeric admission and record-domain validation remain open.

Provider build passes; ten focused tests pass (six transport plus four compiler tests), no skips. Compiler tests cover parameter isolation, string missing/null normalization and numeric metadata rejection. Evidence TestResults/CosmosCqlCurrent/cosmos-cql-predicate.trx; SHA256 DD67ABF3B05155FB579139C187020C33D6A82ABC7A0DC271365E42C174FC528A. Next: complete query/visibility/record-validation owner and native acceptance before exposing capabilities.

## Row-command checkpoint

CosmosCqlRowCommand now re-admits one exact Cosmos target against an internal canonical profile, binds owner/public predicates before the native source window, uses parameterized OFFSET/LIMIT and an overflow sentinel, and appends Id ordering where needed. Guid literals use canonical D strings. Version plus Id ordering requires an appropriate declared Cosmos composite index; no index is created here. No provider enrollment yet: native record/domain validation, metadata, projection/count and execution proofs remain open.

Provider build passed; fourteen focused tests passed, zero failed/skipped, including visibility/window parameter placement, overflow sentinel, foreign target rejection and re-admission of a permissive incoming plan. Evidence TestResults/CosmosCqlCurrent/cosmos-cql-row-command.trx; SHA256 F07A78C8834DAAF695F82345241C840A4C74FFF727005FDAB926718916763917. These tests inspect commands and simulated responses, not native semantic execution.

Read-only podman inspection confirmed retained oasis-cosmos is exited with image b4edfeff93d70f2438ca55dbb14cdb8c6f111208d69c525baf62b7269ce44b56. No service was started. This worktree lacks TestResults/CosmosDBCurrent; do not infer original pending fixtures were reconciled from that absence. Inspect retained database state and existing recovery inventories before allocating native CQL fixtures.

## Canonical read-owner checkpoint

CosmosDbRepository now connects bounded SDK results to its existing DeserializeEntity owner for current records. It rejects duplicate/mismatched identities and decoded row overflow, respects cancellation (including an empty result), and publishes only a complete validated list. Storage-only injected clients explicitly reject bounded querying rather than substituting ReadAll.

Provider build passes; nineteen focused tests pass, zero failed/skipped (five canonical read tests plus the prior fourteen). Evidence TestResults/CosmosCqlCurrent/cosmos-cql-canonical-read.trx; SHA256 091F04FAE40DE0CB11F052EE49CE3C0A52CAC03CCCA76A620CDB0D720391F3BA. These are decoder/compiler/transport tests; native SQL and hosted acceptance remain unverified. Next: connect the admitted row command to provider execution with visibility defense and leaf-shape enforcement, then run journaled native cases against inspected retained state.

## First native row milestone

HolonRepository.Cql.cs now executes the compiled command through bounded SDK reading and the shared canonical decoder. Visibility violations reject the complete result; no managed visibility filtering. Leaf queries clear embedded Children and do not imply graph expansion.

Provider/test builds pass.21 focused compiler/transport/canonical/leaf tests pass; evidence TestResults/CosmosCqlCurrent/cosmos-cql-leaf-read.trx SHA256 116E46C0B0A28FB3D1D184BD07E1F9C601A4B13F39CD67F14E58E26CF762179B. One native test passes with five cases: owner/public plus active/range filtering, deterministic Id-window paging, absent-name empty result, canonical null-string selection, and overflow rejection. Evidence TestResults/CosmosCqlCurrent/cosmos-cql-native-rows.trx SHA256 CF102A11E5032942CABD13E1A6CFFE046EE92A8D5C5AFED959E2ABB9427F281A. Native fixture used the existing emulator, not hosted Azure.

Owned fixed reusable database oasis-cql-demo/holonItems was initialized independently of prior provider fixtures. Four original journaled IDs were saved through canonical repositories, then point-read identity/name checked and deleted with observed native ETags; independent native COUNT confirmed0. The durable rows.pending.json was removed only after successful cleanup. Database/container remain reusable empty. Original oasis-cosmos-crud was not accessed. Emulator stopped and independently inspected exited; no pending CQL journal remains.

Enrollment stays internal: Version composite ordering/index policy, wider operand-domain/corruption cases, metadata precision/protection, projection/count, provider lifecycle/manager integration, save-state concurrency and hosted proof remain unverified. Next: complete native canonical profile cases and provider boundary, then progress common demo features without claiming full parity.

## Standard provider boundary milestone

AzureCosmosDBOASIS now implements ICosmicQueryStorageProvider for the canonical row profile. It translates/re-admits before obtaining a client, requires explicit activation, serializes with activation/disposal using the existing lifecycle gate, and returns structured OASISResult errors. Query calls never activate/create storage. The repository executes the same compiled command without another translation path.

24 focused tests pass (three new provider-boundary tests); evidence TestResults/CosmosCqlCurrent/cosmos-cql-provider-boundary.trx SHA256 25CE816569BA90A696B6C50FC2C25915D37B14423CC8FD3FE8079F67440F02A8. The native five-case gate now runs through the public provider boundary, verifies completion counts, and passes: TestResults/CosmosCqlCurrent/cosmos-cql-public-native.trx SHA256 F9F1FF31D8D47C177ADDF0A11993AE79C7F8CC46882B3E7E4D02A4F2BDBA0D18. Exact four-ID cleanup/count0 and absent pending journal verified; emulator inspected exited. Provider activation initialized reusable empty avatarItems/avatarDetailItems in the same owned oasis-cql-demo database.

Canonical rows are enrolled; prior internal-only statements above describe earlier checkpoints. Metadata, graph/projection/count and full demo acceptance remain open. Id ordering is native-proven; compound Version/Id index configuration and broader operator/domain tests remain unverified. No hosted-cloud or full manager/usage-owner acceptance is claimed.
