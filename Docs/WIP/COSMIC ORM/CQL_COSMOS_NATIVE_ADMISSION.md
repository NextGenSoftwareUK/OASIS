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

## Nested metadata row milestone

Native predicates now cover nested string, bool/bool? equality/inequality, explicit nullable null, Exists and IsNull. Ancestor object/terminal existence checks distinguish missing/wrong-type values; string comparisons explicitly exclude exact-number framed paths. Names are JSON-escaped structural identifiers and values/frame-path components are parameters. No decrypting/resolving secrets or changing reserved/queryable encryption policy occurs; encrypted/missing values do not become plaintext query data. Numeric metadata remains rejected pending exact native semantics.

27 focused tests pass, zero failed/skipped: TestResults/CosmosCqlCurrent/cosmos-cql-metadata-unit.trx SHA256 5A5D5B31827C377B5F5E32CB162915500A66F780FBDB827F405E8915CD3947DA. Public-provider native gate now passes14 cases (prior5 plus9 metadata cases): nested path, exact-number-vs-string distinction, existence, explicit null, nullable bool, nullable inequality, wrong type and bool equality. TestResults/CosmosCqlCurrent/cosmos-cql-metadata-native.trx SHA256 287A3BC148CAEF3C0D6034F4FE5D3460961C57FAA1A474662C8D05865D8A306A. Emulator proof only. Four journaled originals were exact ETag-deleted, COUNT0 confirmed; pending journal absent and emulator inspected exited. No replay or replacement environment.

Next: projection/count and remaining numeric/index/manager demo scope. Earlier canonical-only/internal-only statements above are historical checkpoints, superseded by this section.

## Native cardinality milestone

Count/LongCount/Any now use ICosmicQueryScalarStorageProvider and independent scalar admission. One native SELECT COUNT(1) AS count reduces the admitted source predicate; exact integer cardinality arithmetic applies its immutable offset/limit. No entity/DTO construction, row fetch/filtering or error-triggered alternate path. Post-window predicate terminals, All and selected aggregates remain rejected. The acknowledged count must be one nonnegative exact JSON integer within9007199254740991; Count uses the shared checked Int32 conversion.

Root cause found by native test: Cosmos rejects composing aggregates with IIF/arithmetic in SELECT. Removed that unsupported SQL entirely; the sole count path now uses the supported aggregate and equivalent scalar window algebra. Initial failure TRX TestResults/CosmosCqlCurrent/cosmos-cql-count-native.trx SHA256 14410C6CC771772EB47B04BE88FC86065F876DEAE09EE9DE49CD1A0F4805CAC0 remains preserved. Initial decoder test also exposed canonical JRaw numeric representation; scalar parsing now uses exact JSON integer parsing without coercion.

34 focused tests pass: TestResults/CosmosCqlCurrent/cosmos-cql-native-algebra-unit.trx SHA256 BAE608A9797E98759DC36C9D96D873D1785EFE5FBEC588ED8EB2C5EBAEFD3097. Native public-provider gate passes23 cases, including9 new cardinality cases: Count/LongCount/Any, paged count, empty Any, beyond-offset/zero-limit count, framed-string-domain metadata count and a source constructor that throws if hydrated. Evidence TestResults/CosmosCqlCurrent/cosmos-cql-count-reviewed-native.trx SHA256 B2E758BC1820C932B481714F5E79A7AC4A32187B946AC8710EC2BF2D0ED0768F.

Verification resumed the original four records under journal SHA256 ACEC97647EBBF2041FD0241FEF38A41A28FE3BCB63315081DEDCF80704327470, with explicit reviewed mode pinned to that journal. No saves/creates were replayed; original IDs/fields were point-read checked, then the complete gate passed. Exact ETag cleanup/count0 cleared the journal; emulator inspected exited. Reviewed mode must not run now that its original journal is absent. Next: projection and remaining numeric/index/manager/hosted demo acceptance.

## Projection milestone

Cosmos now implements ICosmicQueryProjectionStorageProvider. Native SELECT VALUE emits scalar cells with explicit missing/null/valid/invalid states and a minimal identity/visibility source proof, using the same predicate/order/window command owner as rows. No full entity hydration or DTO construction occurs in the provider. String metadata selection reuses exact-number frame exclusion. Canonical string/bool/Int32/Guid and string/bool/bool? metadata domains are independently admitted. Wrong domain, invalid/duplicate source identity, visibility violation, missing non-nullable data and payload overflow reject complete results. Shared CQL validation/materialization owns DTO construction. Selected-result/entity-stage composition remain unsupported.

39 focused tests pass: TestResults/CosmosCqlCurrent/cosmos-cql-projection-fixed-unit.trx SHA256 5184BB2681B68992345B7A159F42186502AA9DD667AE310590A7927C68CE0FC4. Native gate passes31 cases, including8 projection cases: canonical identity/version/name; windowed scalar; nested nullable metadata; absent nullable cell; framed-number string rejection; wrong type rejection; unconstructible source type; payload rejection. TestResults/CosmosCqlCurrent/cosmos-cql-projection-reviewed-native.trx SHA256 FAC37A762DADD9980360B51B9D0E803BB0FD89759DB96C8466CD79FDBCE6C79C.

The first projection run failed in test composition after three projection cases: an indexed closure array was used inside Where, which CQL intentionally rejects. Fixed the test by capturing scalar IDs before compilation; no application expression evaluation was added. Original failed journal6BA5D0DDC1C2D5D90A4D9F2BBCDCCA6682E817381B18865E718C7DCBD4B01492 and failure TRX A3EE2C0B25BA5367DA04AB419D9FF7124343F1F7082D67C08AF6CEDA90EBFA04 were preserved during review. Corrected gate resumed those originals with no replay, validated/cleaned exact4 IDs via ETags/count0, removed journal and stopped emulator. Reviewed projection mode must not run now its original journal is absent.

Next: compound ordering/index profile, numeric metadata and manager/usage/demo/hosted acceptance. This remains emulator-level partial demo coverage, not full Cosmos/cloud/ORM parity.

## Shared manager / LINQ demo milestone

Examples/CosmicCqlDemo/CloudRowsWalkthrough.cs now demonstrates explicit scope, trusted actor visibility, string fixture metadata, deterministic bounded entity reads, nested DTO projection and standard Count/LongCount/Any through HolonManager V2 and CosmicLinq. Hosts supply existing activated adapters/fixtures; the component creates no database and introduces no alternate query implementation. The native integration project compiles/executes this exact demo file.

Native gate passes prior31 cases plus5 manager/LINQ operations (rows, projection, Count, LongCount, Any), verifying the expected actor-visible two-record result. Evidence TestResults/CosmosCqlCurrent/cosmos-cql-manager-demo-native.trx SHA256 0EF256609221FF4721BDD643D07FB85F5E2960657EE268EFF2A203D5FE8912C5.39 focused tests remain the last applicable unit cohort; this step adds demo/native integration only. Build passed with bounded workers/cache-only existing outputs. No prepared subscription/usage admission, HTTP host or hosted Azure proof is claimed by this local trusted SDK run.

Four originals were exact ETag-cleaned/count0 confirmed, journal absent, emulator stopped/inspected exited. The component is callable from a host, not a new existing console/Swagger switch. Full common demo still needs save-state/concurrency/delta integration, compound index/profile, numeric metadata and hosted/HTTP acceptance. Next: explicit compound ordering index profile; no implicit index changes inside query execution.

## Explicit canonical index profile milestone

GetCqlCompositeIndexDefinitions returns detached SDK definitions (/Version ASC,/Id ASC) and (/Version DESC,/Id ASC). Reverse scans cover opposite directions. No query/helper mutates live policy. Owned native fixture setup explicitly merges absent definitions while preserving all other settings/indexes and uses the observed container ETag. Production deployments must explicitly provision/verify their own policy/readiness.

Row/projection ordering now removes repeated keys and stops after canonical unique Id, avoiding redundant duplicate index paths without changing sequence semantics.41 focused tests pass: TestResults/CosmosCqlCurrent/cosmos-cql-index-unit.trx SHA256 17AF6FB78B7D8A7B549D23E62122BDA00788153204FF3AA7D86DAA688143A929. Native gate passes38 cases plus5 manager/LINQ operations: TestResults/CosmosCqlCurrent/cosmos-cql-index-native.trx SHA256 576CD10C9A88ECED18035A77F777CA29C8FE8F24812D0590A5536E80799E9DA3. Seven new cases prove Version ties, four Version/Id direction combinations, duplicate/trailing key semantics and compound-order selected paging. This closes the declared canonical ordering profile, not arbitrary metadata ordering.

Exact four-ID ETag cleanup/count0 cleared the journal, emulator stopped/inspected exited; the two explicit indexes remain in the reusable empty owned container. No reset/replay/download. Next: audit conditional save-state/concurrency/delta lifecycle for the cloud demo, then numeric metadata and HTTP/hosted acceptance. Hosted index behavior/performance and production account configuration remain unverified.

## Conditional save-state audit

Authoritative source: Infrastructure/CosmosDbClient.History.cs compares an observed ETag and performs a conditional native replacement, validating the acknowledged payload. Infrastructure/CosmosDbRepository.cs exposes ReplaceIfUnchangedAsync. Neither implements IConditionalHolonStorageProvider, IConditionalHolonDeltaStorageProvider or durable COSMIC operation receipts/preparation recovery. CosmicConcurrency's provider Guid revision observation is not published by the current Cosmos decoder. Existing history retention is not a replacement for those contracts.

The current native schema partitions holonItems by /id (CosmosDbClientFactory/HolonRepository). Different holon identities therefore do not share the current transactional-batch scope. COSMIC's conditional interface requires graph preconditions, mutation and idempotency receipt to commit atomically; delta enrollment additionally requires durable acknowledged delta evidence. Sequential item ETag writes, compensating writes, a separately saved receipt or ordinary CRUD fallback would not satisfy that invariant.

Two new sync/async tests prove explicit CosmicWriteOptions saves return COSMIC_CONDITIONAL_WRITE_UNSUPPORTED before activation, even with automatic failover/replication enabled, and preserve caller identity/version. All43 focused tests pass: TestResults/CosmosCqlCurrent/cosmos-cql-conditional-admission-unit.trx SHA256 D9D1753F6FCDBE0F62BF771A1DEC6B0D8DEE3DB9B43DCF4BFC292620D4EAC5D6. No native service started for this audit; latest native38 cases/5 manager operations remain applicable query evidence, not conditional lifecycle proof.

Implementation requirements remain open: choose/admit a real transaction owner for the selected graph and receipt, bind operation ID to immutable request content, preserve native revision observations, retain preparation/receipts/deltas through ordinary CRUD/history, and prove stale clients, operation replay/reuse, uncertain acknowledgement recovery and size/partition rejection. Do not advertise a one-item patch as full graph/receipt support. The demo may explicitly show unsupported conditional capability, per its bounded acceptance contract, but cannot claim the full cloud save/delta workflow.

Platform verification27October: Microsoft's [transactional batch documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/transactional-batch) scopes standard batches to one logical partition. New [distributed transactions documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-configure-and-use-distributed-transactions) describes a public preview requiring account enrollment and .NET SDK3.64.0-preview.0, with100 operations/2MB per transaction and no preview SLA. This is a possible explicit future implementation route, not evidence that current SDK3.35.4/emulator supports it. No preview SDK was installed or account enabled. Do not claim Cosmos generally cannot transact across partitions; the gap belongs to this current implementation/environment. Full roadmap scope remains open.

## WEB4 read-profile enrollment milestone

CqlV1Compiler and row/scalar/projection endpoints now explicitly admit AzureCosmosDBOASIS's read profile. Provider capture/read-policy resolution carry that route-specific enrollment through compilation; other routes retain their existing three-database scope. No federation/cursor code changed. Existing active-provider, authenticated actor, query-tariff and trusted read-policy requirements remain. Cosmos source operators still undergo the actual adapter's independent admission.

Seven new tests prove cloud profile selection, original route isolation, concrete/case-sensitive identity restrictions and actor policy bound before source paging. TestResults/CoreCurrent/cql-cosmos-web4-fixed-admission.trx SHA256 93555FFD45965C53BF1747CDF5AA95DCFAA5DB2110820DF106A761F0DB540D99. The affected existing WEB4 regression cohort plus these tests passes372 total, no skips: TestResults/CoreCurrent/cql-cosmos-web4-regression.trx SHA256 F5FDC7E119AE69CF5611E00707EEC22426709F7642E04861A73312019DFA119E. Tests compile linked production endpoint/compiler source; no full WEB4 host build or HTTP-to-native/hosted Cosmos run is claimed. Initial test exposed a second source-compiler provider check; the explicit route profile is now propagated there rather than globally expanding every route.

Remaining HTTP gates: complete prepared-usage/authenticated host execution, canonical membership for authoritative record catalogues (must currently reject, never downgrade), native/hosted HTTP proof and demo host integration. Shared wire default Name ordering is unsupported in Cosmos; request Id or declared Version ordering explicitly. No COSMOS/AZURE environment variable names were present in this shell; hosted configuration remains unspecified and an optional user question asks only its location/resource identity. No keys were requested/read/output. Next: authoritative catalogue membership and native HTTP integration, then numeric demo scope.

## Authoritative catalogue membership milestone

Canonical Guid/string/bool/Int32 membership now translates to one parameterized native ARRAY_CONTAINS predicate with native domain guards. Nullable canonical strings retain absent/null membership semantics. Metadata membership remains independently rejected. The shared canonical whitelist/type owner is used by comparisons and membership, rather than duplicating field/domain admission.

AuthoritativeRecords now works natively across rows, count and projection. Nine new native cases prove: private catalogue member returned; owner/public outsiders excluded; same catalogue count/projection; empty catalogue rows/Any remain empty; string/bool/Int32/null canonical membership. Latest gate passes47 native cases plus5 manager demo operations. TestResults/CosmosCqlCurrent/cosmos-cql-membership-native.trx SHA256 589B7BDA8DCEC54DF4A6FB48D53080D2EE78288E01CBBF614B1E8819BD3C5D09.46 provider-focused tests pass: TestResults/CosmosCqlCurrent/cosmos-cql-membership-unit.trx SHA256 2B039F0C57B668D64B5D63E4956A1BAC773B0E9DC0FC31A1FA2CBB2AB6B1133E. Earlier statements that Cosmos lacks canonical catalogue membership are superseded. Live authenticated HTTP/prepared usage remains unverified.

Four owned originals exact ETag-cleaned/count0 confirmed; pending journal absent and emulator inspected exited. No new environment, dependency or raw committed evidence. Next: native HTTP/prepared usage integration and numeric demo scope; conditional lifecycle/hosted Azure remain open.
