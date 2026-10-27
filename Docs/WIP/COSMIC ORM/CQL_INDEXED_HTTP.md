# Indexed WEB4 HTTP execution milestone

27 October 2026. Controller/context execution verified; stock middleware deployment
integration, real-backend HTTP/native acceptance and full ORM parity remain open.

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

## Host sequence (not yet automatic stock deployment wiring)

1. Authenticate the actor; capture bounded indexed DTO and resolve its server alias.
2. Prepare once; use MaximumServiceCostUsd, MaximumUnits and the tariff read budget
   when authorizing/reserving through the existing usage system.
3. Bind the exact admitted handle in Web4UsageMeasurement.ExecutionKey, then call
   CqlIndexedV1RequestContext.Bind(http, preparation, handle). Honor its result.
4. Run the action under PreparedStorageExecution with the same captured tariff
   object. Do not resolve/compile the alias again or substitute a new policy.
5. Existing usage middleware must durably observe/settle the actual receipts;
   uncertainty remains pending for reconciliation, not a query retry.

The generic usage middleware currently has its own raw HTTP fingerprint; indexed
preparation currently uses a semantic route-domain fingerprint. Next integrate
these as distinct bound identities in the stock preparation/admission hooks,
without bypassing the ledger or adding a parallel billing pipeline. The route
fails closed when the host has not installed its explicit binding. This milestone
is not production JWT/Mongo-outbox/authority/settlement acceptance.

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
source coverage, original Solana recovery, real-backend HTTP, stock host wiring,
indexed scalar/projection/metadata/graph capabilities and full parity remain red.

- `TestResults/ProtocolCqlCurrent/index-http-full-core.trx` SHA256 `07C2BF888BC1EA14C63EE286E7EF246547F49F62FDD08AE1C024DBA65243332D`.
- `TestResults/ProtocolCqlCurrent/index-http-solana.trx` SHA256 `D9ED888F31D43617E526592ED4678180F2FF0AE2C1079E98EB2EB9EB0C4A6216`.
- `TestResults/ProtocolCqlCurrent/index-http-sqlite.trx` SHA256 `F7948272B3A8C8C96FC63A11ED72DF41162BA2EDA54219A81F278E2E68B7A4FF`.
- `TestResults/ProtocolCqlCurrent/index-http-build-resource-inventory.json` SHA256 `66C1E2019C67473B29DF31788FD3FCB2CDEA097FBDEA3207FF432D8BDA6BF90C`.
