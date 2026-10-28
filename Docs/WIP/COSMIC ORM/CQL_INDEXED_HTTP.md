# Indexed WEB4 HTTP execution milestone

27 October 2026. Stock middleware and native row/projection HTTP acceptance verified.
Indexed cardinality HTTP passes controlled and live-source transport gates;
production identity/durable settlement acceptance remain open.

## Behavior

POST `/api/data/query-cql/indexed` accepts an explicit server Alias and bounded
canonical row Query. The host resolves/authenticates/authorizes the alias once,
prepares the immutable plan/grant/freshness/tariffs and reserves its maximum
service cost **and units** before dispatch. The request binds to that exact usage
handle and captured tariff object once. Price bound and attempt/unit ceilings
are checked again; changed wire intent or duplicate consumption cannot execute.

The endpoint executes the captured plan through typed HolonManager. It returns
bounded public rows plus logical provider, physical execution provider, complete
checkpoint, independently observed head and aligned original source receipts.
Provider keys/revisions/runtime objects remain excluded. Slot/coverage values
are strings to preserve values beyond 2^53. Envelope LoadedCount matches rows.
Assurance is explicitly `trusted-source-observation`, not inclusion proof.

The existing 1 MiB response / 32 KiB request contracts and five-second linked query
deadline apply. Unsupported shapes reject during preparation, not client-side
reduction. Pre-dispatch errors retain affirmative no-provider-execution evidence.
Dispatching a reader is possible execution even if it omits instrumentation;
missing capture cannot become false zero-execution evidence. Uncertain begin/
receipt persistence returns its classified code with no partial result/retry.

Indexed physical-read policies now cap at 512, matching the global usage ledger.
The same Core owner computes the maximum tariff cost and checks measured cost,
reserved units and physical-attempt budget in SDK prepared execution too.
Standalone unprepared SDK calls retain their existing contract.

## Host sequence (explicit host enrollment required)

1. Authenticate the actor; capture bounded indexed DTO and resolve its server alias.
2. Prepare once; use MaximumServiceCostUsd, MaximumUnits and the tariff read budget
   when authorizing/reserving through the existing usage system.
3. Bind the exact admitted handle in Web4UsageMeasurement.ExecutionKey, then call
   CqlIndexedV1RequestContext.Bind(http, preparation, handle). Honor its result.
4. Run the action under PreparedStorageExecution with the same captured tariff
   object. Do not resolve/compile the alias again or substitute a new policy.
5. Existing usage middleware must durably observe/settle the actual receipts;
   uncertainty remains pending for reconciliation, not a query retry.

The stock preparation/admission hooks bind the raw HTTP reservation fingerprint
and semantic route-domain fingerprint as distinct identities. Register the alias
registry and AddCqlIndexedReadAdmission, and explicitly enroll each measured route.
No host route, signer or credential is installed automatically. Production
JWT/Mongo-outbox/authority/settlement acceptance remains open.

## Request example

This example assumes the server alias permits at least 10 slots of lag and a
30-second head age. Omit those optional fields to use its configured baseline.

```json
{
  "alias": "solana-coffee",
  "maximumLagSlots": 10,
  "maximumHeadAgeSeconds": 30,
  "query": {
    "filters": [
      { "field": "Version", "scalar": "Integer", "operator": "GreaterThanOrEqual", "value": 1 }
    ],
    "orderBy": "Name",
    "take": 5
  }
}
```

Alias owns the provider/network/index/grant and minimum finalized position.
Caller lag/head-age may only tighten the server baseline. No Provider/Graph,
endpoint, keys, prices or fabricated checkpoint/head fields are accepted.

## Verification

- Full Core: 4,128 passed / 0 failed / 0 skipped, including actual controller/Kestrel 12-request
  cohort: anonymous, missing preparation, insufficient dollars/units/attempts,
  wrong fingerprint, changed intent, unknown field, rows, empty, large-slot and
  uncertain receipt. Two cases prove single-use/exact-policy binding.
- Slot 9007199254740993 is retained as a string. SDK budget failures dispatch
  zero reads; uninstrumented reader dispatch remains possible execution.
- SQLite: 31 actual indexed/store checks pass; Solana: 789 unit cases pass, including
  seven actual-SQL/canonical-decoder/controlled-RPC/outbox accounting cases.
- Shipping WEB4/net10.0 and Cosmos/ActivityPub/IPFS/SOLID/Neo4j controller consumers
  build. Missing shipping outputs were regenerated for 61 in-worktree projects from
  cached dependencies. Eight sibling projects were kept read-only. ONET / net8.0 was
  built after detecting its consumer requires that runtime target.
- Full Core's prior missing shipping-host prerequisite failures are resolved.
  Owned Kestrel listener 127.0.0.1:50259 stopped; fixed test DB directories empty.
- Runtime/package cache reused, no requested runtime/package installation or new
  native service. Inventory contains 1,770,881,866 observed build bytes, including
  inherited/reused outputs; do not attribute all bytes to this milestone.

Current HTTP transport/identity/outbox fixtures are controlled. Actual native
source coverage, original Solana recovery, real-backend HTTP, production host deployment,
indexed scalar/projection/metadata/graph capabilities and full parity remain red.

- `TestResults/ProtocolCqlCurrent/index-http-full-core.trx` SHA256 `07C2BF888BC1EA14C63EE286E7EF246547F49F62FDD08AE1C024DBA65243332D`.
- `TestResults/ProtocolCqlCurrent/index-http-solana.trx` SHA256 `D9ED888F31D43617E526592ED4678180F2FF0AE2C1079E98EB2EB9EB0C4A6216`.
- `TestResults/ProtocolCqlCurrent/index-http-sqlite.trx` SHA256 `F7948272B3A8C8C96FC63A11ED72DF41162BA2EDA54219A81F278E2E68B7A4FF`.
- `TestResults/ProtocolCqlCurrent/index-http-build-resource-inventory.json` SHA256 `66C1E2019C67473B29DF31788FD3FCB2CDEA097FBDEA3207FF432D8BDA6BF90C`.


## Stock middleware milestone (27 October)

The existing Web4UsageLedgerMiddleware now supports optional trusted
IUsageRequestPreparation after authenticated bounded buffering/raw hashing and
before reservation. Indexed admission captures the server alias once, validates
intent before provider I/O, and reserves its physical-read unit/dollar ceiling.
Raw HTTP reservation identity and semantic query identity are both checked.
No second ledger pipeline or client-controlled tariff is introduced.

Register the server-owned ICqlIndexedV1Bindings and existing singleton native
IPreparedStorageAdmissionFactory first, then call
`services.AddCqlIndexedReadAdmission()`. Existing explicit route configuration
must enroll POST `/api/data/query-cql/indexed` with provider measurement enabled,
zero fixed/native fees and an adequate MaximumProviderAttempts ceiling. Keep the
existing usage middleware before PreparedStorageAdmissionMiddleware and MVC.
Unrelated routes retain their captured native factory; duplicate preparation
owners reject during registration. Registration does not enable routes by itself.

A single UsageExecutionHandle now owns the aggregate indexed attempt/cost budget,
including newly created prepared wrappers. Repeated reads cannot reset the
reservation. Unknown attempt-start outcomes conservatively retain the claim.

Full Core: 4,140 passed, zero failed/skipped. New 12-case stock suite includes
real Kestrel/controller/middleware rows, empty result, replay, invalid request,
unsupported graph, route ceiling, receipt uncertainty, native-factory composition,
aggregate unit/cost ceilings and fail-closed preparation hooks. Authority/outbox,
identity and source transports remain controlled; this does not certify production
JWT, durable settlement or real-backend HTTP acceptance.

Evidence: `TestResults/ProtocolCqlCurrent/index-stock-full-core.trx`; shipping
WEB4 build: `index-stock-web4-build.log`; shared-controller builds:
`index-stock-*-build.log`. Raw evidence stays outside Docs.


## Actual SQLite and canonical decoder over HTTP

Four additional cases now execute the shipping controller assembly and stock usage
and storage middleware, with actual SQLite history/projection/checkpoint reads,
independent Solana canonical memo decoding and typed manager execution. Rows and
empty pages pass with exactly seven/four billed physical reads respectively.
Returned rows carry the current source commit and string checkpoint position.
Replay adds no SQL/RPC execution or settlement.

After publishing a valid index, removing the source transaction or changing the
observed network rejects the HTTP page with 503 and no rows. Completed SQL/RPC
attempts still settle with their actual units/cost; neither error is mislabeled
as no-provider-execution. Tests issue no chain writes. Identity, RPC responses and
ledger storage/authority are controlled. A live source, production JWT and durable
settlement remain separate red acceptance gates.

Full Solana suite: 793 passed / zero failed / zero skipped. Proving test:
`Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests/SolanaChainIndexedHttpTests.cs`,
called by `SolanaChainIndexedBillingTests` http-* cases. Evidence:
`TestResults/ProtocolCqlCurrent/index-native-http-full.trx`. The test links the
shipping WEB4 project directly, rather than a second controller implementation.
The fixed fixture DB is removed and key bytes zeroed by its existing finally;
loopback listeners stop in finally. No persistent environment was allocated.


The older minimum-position identity bug is now reproduced and fixed in shared
checkpoint admission; see [finalized anchor contract](CQL_CHAIN_QUERY_CONTRACT.md#finalized-anchor-identity-correction-27-october).
Correct older anchors pass for rows and empty pages; conflicting or unavailable
anchors reject with no partial page. Additional finalized block RPCs retain their
own billed attempts/receipts. Ingestion also rejects a conflicting minimum before
SQLite publication. Reproduction evidence is preserved separately as
TestResults/ProtocolCqlCurrent/index-anchor-repro.trx.


## Live Solana source through shipping HTTP (27 October)

One native schedule now passes five HTTP requests against shipping MVC, stock
Web4UsageLedgerMiddleware/PreparedStorageAdmissionMiddleware, the actual reopened
SQLite index and retained live Agave4.3.0 validator. Matching rows and empty pages
return correct source/execution identities, declared coverage and source commit.
Two identical-key replays return409 without additional read attempts. An unsupported
graph shape returns400 before another reservation/provider execution.

Rows/empty complete9/6 physical reads in this run, each with separate captured
attempt/receipt. Exact units and service cost match receipts; the full100-read/$0.10
ceiling is reserved before execution. The retained seed is reused and no source
write occurs. Older finalized anchors account for their own actual source reads.
Counts can vary with source checkpoint/head equality; the test asserts actual
receipts and settlement, not a fabricated fixed read total.

Identity and ledger/authority are controlled fixtures. RPC, canonical decoder,
SQLite and controller/middleware execution are real. This does not certify
production JWT, durable Mongo settlement, hosted deployment, original-record
recovery or full indexed scalar/projection/metadata capability.

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-solana-http -Operation {
    & Scripts/TestHosts/Run-ReusableSolanaTests.ps1 -IndexedHttpDemo
}
```

The explicit SDK seed gate must have completed first. This mode refuses to create
another seed and reuses its persistent identity/index. Preserve prior native TRX
and JSON before rerunning; preserve pending/uncertain state for reconciliation.
The runner starts/stops the owned retained validator, and the test starts/stops
its loopback MVC host. It does not expose a persistent interactive HTTP server.
Evidence: TestResults/SolanaCurrent/solana-indexed-http-native.trx/json;
TestResults/ProtocolCqlCurrent/index-live-http-solana.trx (full799 regression gate).
The strict ledger fixture is shared with controlled-RPC tests, not a second
billing implementation or a production persistence substitute.

Native HTTP TRX SHA256: F0C49FC0E6EAFDADD4FF0E233B000409CEBDF7F416763D53DCCD3F9E2C1F3690; JSON: 6503A37C13A53F625D80F5B78FB843DDF3F841E540A721B72ECAED7E820D1EAA; Solana799 TRX: B221F764234D3D3BA13260653275D7B9F8A8B2C490DFCBAD3FB0E856B55E334D. Raw files remain under TestResults; see cleanup ledger for sizes and teardown.


## Optional native selected columns on /indexed (27 October)

POST /api/data/query-cql/indexed now accepts an optional columns array. Omit it
(or use null) for the existing entity-row profile. Provide1-32 explicit columns
for an indexed-projection result. The alias must advertise the separate native
selected-column contract. Unsupported metadata/field/domain shapes reject during
preparation, before authority reservation or provider execution. Empty/duplicate
columns and entity includeMetadata combined with columns are invalid.

Column descriptors are deep-captured before the alias resolver callback and bound
into a projection-specific semantic fingerprint. The exact admitted raw HTTP
fingerprint, handle, tariff policy and frozen semantic intent remain separately
checked. Changing columns cannot turn a row admission into a projection or change
a selected result. This uses the existing route/reservation/physical-read ledger.

Execution uses HolonManager's shared native indexed projection owner: one SQL
snapshot, independent source proofs, visibility/checkpoint/anchor validation,
canonical cell comparison and detached construction. Output reuses the existing
public selected-column scalar encoder. Mode is indexed-projection, with columns
and scalar-cell rows plus the same source/execution/checkpoint/head/receipts.
Slots remain strings; no endpoint, price, checkpoint claim or actor grant is
accepted from the caller. Canonical Id/Name/Version/VersionId/CreatedByAvatarId/
IsPublic/IsDeleted selections are enrolled; metadata and aggregate shapes are not.

```json
{
  "alias": "solana-native-demo",
  "query": { "orderBy": "Name", "take": 1 },
  "columns": [
    { "alias": "identity", "field": "Id" },
    { "alias": "label", "field": "Name" },
    { "alias": "revision", "field": "Version" }
  ]
}
```

The selected rows contain cells in columns order, e.g. [["<id>","Coffee",1]].
Use the existing AddCqlIndexedReadAdmission registration and explicit measured
indexed-route policy. The native test host's alias is not auto-enrolled in a live
deployment. The current runner stops its test host after acceptance.

Verification: fullCore4165pass;11new projection cases cover rows/empty, unsupported
or malformed columns, row-only alias rejection, contradictory metadata envelope,
uncertain receipt, forged cells and column capture/semantic-intent separation.
The retained live Solana/SQLite shipping HTTP gate now passes9requests: entity and
projected rows/empty,4replays,1unsupported shape. Completed physical reads are
9/6/9/6 in this run, with exact reservation/actual settlement assertions and zero
new source submissions. Controlled identity/ledger remain explicit; production
JWT/durable settlement, deployment, original recovery and full parity remain red.
Evidence: TestResults/ProtocolCqlCurrent/index-http-projection-core-full.trx;
TestResults/SolanaCurrent/solana-indexed-http-native.trx/json.

Projection milestone hashes: Core TRX 7C3A2FBEB945E19597D30D7A550AF00F875E326607063F051AA47FD14372C34C; Solana799 TRX 05842494F8ADFDBD56401437B73B82FC47EFE967E6FC6B141E4348DC5A069214; native9request TRX 7F00801B6AA4412CB0CA08DD485BE133295276D8B8A1115B605A4E1F1EAAE010; native JSON A82683E2CF9A2D8E704CDD7F78D9C6C8ED708CA2C9F5A5AFC01C5B2D0DA15432. Paths above; teardown/resources in the single cleanup ledger.

## Indexed cardinality HTTP

POST `/api/data/query-cql/indexed/scalar` uses the same alias registry, frozen
actor grant, freshness baseline and stock reservation/storage pipeline as rows.
Enroll this exact route in the host's measured read policy before using it.

```json
{
  "alias": "solana-demo",
  "query": {
    "operation": "LongCount",
    "orderBy": "Id"
  }
}
```

Use `Count`, `LongCount` or `Any`. Omit `take` to include the entire eligible
source; `take: 0` means an empty source window. Explicit windows accept 0–100,
subject to the alias result ceiling; skip accepts 0–1000. Filters and actor access
are applied in native indexed SQL. This route does not fetch a row page to count.
Selections, provider overrides and graph hydration reject before reservation;
other unsupported predicates reject against the actual indexed capabilities.

The response includes `mode: "indexed-scalar"`, operation, valueType, value,
logical/physical providers, checkpoint and independently observed finalized head.
LongCount values use exact decimal strings; Count and Any use JSON integer and
Boolean values. Assurance is `authenticated-index-snapshot`: it authenticates the
index snapshot and freshness, without asserting current per-record RPC readability.
It returns neither fabricated record receipts nor global manifest counts/hashes.

Verification: 9 controlled loopback cases through the shipping controller and
stock usage/storage middleware cover successful Count/LongCount/Any, forbidden
selection/graph/provider, missing scalar capability, changed post-reservation
intent and uncertain receipt acknowledgement. Successful fixtures capture four
physical reads (SQL, genesis, slot, block), reserve 100 units and settle four.
Three further tests prove operation/window capture before resolver callbacks.
The live Solana/SQLite HTTP gate also passes:27requests,13accepted,13replays
rejected and1unsupported before execution. Nine scalar cases cover all three
operations across whole-source, filtered-empty and zero-length windows. Each
settles six physical reads (including independent older anchors). No new seed or
source writes; retained index reopens and validator stops after the gate.
Production JWT/durable settlement remains pending.

## Explicit schema3 metadata profile

The live Solana/SQLite schema3 gate now passes42requests (19accepted/19replays/
4unsupported), including six metadata row/scalar/canonical-projection cases and
three forbidden-root rejections before reservation. Query filters use metadataPath,
Scalar String/Integer and native SQL; no source substitution or client filtering.
The server alias owns the selected queryable-root policy; callers cannot enroll
keys or migrate a schema2 index. The first journaled metadata seed was acknowledged
once, then reused read-only. See [profile, commands and limits](CQL_INDEXED_METADATA.md).
