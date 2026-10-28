> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

# Transport write authority implementation gates

Updated 26 October 2026. The focused signed GraphQL/gRPC save subgate now passes
on MongoDB, SQLite and Neo4j, through configured admission and durable settlement.
Broader transport/startup/read/delete gates remain open.

## Signed deletion checkpoint - 26 October 2026

The focused shipping GraphQL DeleteHolon and gRPC DeleteHolon methods now have
signed three-provider native acceptance. Twenty-four deletion requests join the
existing24 transport saves, yielding426 settled operations and466 physical
attempts across the complete native cohort. Persisted soft deletion, physical
hard deletion, committed gRPC deltas, missing intent, foreign ownership, stale
conditions and per-attempt settlement are verified. Sources remained unchanged;
seven producer/consumer pairs matched; owned services/fixtures were cleaned.
See [provider-key/delete acceptance](PROVIDER_KEY_DELETE_ACCEPTANCE.md).

REST provider-key/logical deletion has SDK/MVC acceptance and a prepared signed
native matrix; that matrix is not yet executed. Full deployed GraphQL schema,
startup, authority-failure/concurrent scope and unknown-outcome coverage remain
open. Historical findings below retain their original checkpoint context.

## Current authoritative findings

| Surface | Current source | Required correction |
|---|---|---|
| gRPC `DataGrpcService.SaveHolon` | V2 uses authenticated request identity and the shared conditional owned-save boundary | Broader alias/startup and unknown-outcome acceptance |
| gRPC `SaveHolonGrpcRequest` | Added operation ID, revision map and recursive JSON envelope without renumbering old fields | Extend remaining read/delete surfaces consistently |
| gRPC `HolonMessage` | Flat Legacy representation; V2 uses the shared recursive `holon_json` contract | Read routing/access migration; keep wire versions explicit |
| gRPC `HolonResponse` | Error code/count, acknowledged state, recursive record, delta and metadata | Broader read/delete/unknown-outcome enrollment |
| GraphQL `Mutation.SaveHolon` | Authenticated conditional create with operation/provider and optional create-only ID | Graph-update/delta envelope and full deployed schema acceptance remain open |
| gRPC/GraphQL sharing | Shared helper pins authority and preserves provider error evidence | Manager acquisition and transport-wide admitted lifecycle still need verification |

Sources are `GrpcServices/DataGrpcService.cs`, `Protos/data.proto`,
`GraphQL/Mutation.cs` and `Helpers/HolonAccess.cs` under the shipping WEB4 project.
The existing gRPC mapping accepts an ID but discards child graphs because the
wire message has no children. A graph-capable contract needs actual round-trip
tests; adding a save-depth flag alone cannot establish graph support.

## Shared invariants and acceptance

1. Reject missing/empty authenticated actor before manager acquisition, provider
   activation or I/O. Never trust a body actor, singleton logged-in avatar or
   client-supplied Wizard flag. Capture host-authenticated role before awaits.
2. Use `HolonWriteEndpoint` and Core owned-save planning for selected-node
   ownership/revision validation. Stable operation IDs are caller intent; do not
   generate a new ID when replaying an uncertain result.
3. Preserve explicit write authority and request routing snapshot through
   observation, ownership checks and conditional write. A conflict never enables
   retry against a replica or unconditional save.
4. Enroll transport paths in existing reservation/admission/outbox/pricing owners.
   Verify real physical attempts and settled receipts. A direct service unit test
   cannot establish deployment admission, JWT middleware or HTTP/2 behavior.
5. Preserve original `OASISResult` failure metadata, counts, diagnostic details and
   exceptions through generic projections. Do not collapse multi-error provider
   evidence into a newly constructed single-error result.
6. Run actual signed GraphQL and HTTP/2 gRPC requests against shipping transport
   registration for MongoDB, SQLite and Neo4j. Include foreign selected children,
   stale revisions, explicit provider/default races, invalid input, no-op, replay
   and unknown outcomes. Verify no I/O on pre-admission rejection.

## Immediate regression gate

`CosmicAuthorityFailureEvidenceTests.cs` exercises a provider failure containing
three errors, detailed diagnostic, exception and metadata through direct pinned
authority observation and the shared sharing helper. Both original controls
genuinely failed (`ErrorCount`: expected three, actual one). The correction uses
the existing generic result-copy owner to retain the complete provider evidence,
then rejects warnings/missing/mismatched observations without publishing a record.
It preserves existing error codes and the sharing boundary's invalid-read code.

Both original controls now pass. Two further controls verify conditional grant
and revoke failure evidence, one physical write attempt and no successful
acknowledgement. Full Core passes 3,372 tests without skips. Compressed original
red and full Core reports are in `../Evidence/Verification/authority-evidence-*.trx.gz`.
Final WEB4/portable gates are still being checked; this does not complete the
GraphQL/gRPC transport migrations above. Legacy behavior remains unchanged.

Legacy paths are documented only. This work targets HyperDrive V2.

## Remaining delete boundary (source audit)

The original audit found GraphQL/gRPC ordinary deletion with an empty actor.
Their V2 logical-ID paths and canonical REST aliases now use authenticated,
revision-bound conditional deletion (see the checkpoint below). The old calls
remain only in unchanged Legacy branches.

REST provider-key deletion still needs a pinned native-key observation and logical
identity/revision binding; its earlier ordinary read/delete flow is not enrolled
by the new logical-ID paths. Native deletion/admission proof is also still open.
Do not enroll recipient/tenant query policy on the assumption that migrating
saves or adding an SDK facade secured the complete delete matrix.

## GraphQL conditional-create migration

The V2 `saveHolon` resolver now accepts `operationId` and `providerType` in
addition to its existing name/description/type fields. It captures the caller
from the authenticated HTTP context and delegates to `HostedHolonWriteEndpoint`,
which validates intent before lazy manager acquisition, creates a request-owned
routing context, then invokes the same owned conditional-save boundary as REST.
An `X-COSMIC-Operation-Id` header may supply the stable operation identity under
the existing agreement rules. Unknown engine modes and non-concrete holon types
reject. Provider errors become GraphQL errors with the original error code.

```graphql
mutation {
  saveHolon(
    name: "Coffee order"
    description: "Conditional create"
    holonType: "Quest"
    operationId: "49751220-5f5a-4b31-8b20-678bd1ef2ddc"
    providerType: "SQLLiteDBOASIS"
  ) {
    id
    name
  }
}
```

Use a new operation identity for a new logical create and retain it when
reconciling that same intent. Core already derives new record identities from
conditional operation identity; the resolver does not invent another identity
scheme. This method remains create-only; it does not accept an existing graph.

Real schema execution exposed implicit `IHolon` member discovery, including
mutation methods and unsupported celestial input graphs. `GraphQL.Types.HolonType`
now binds its declared fields explicitly. The original schema failures are
retained in `../Evidence/Verification/graphql-schema-discovery-red.trx.gz`.

Current controls cover actor/missing intent/invalid provider/unknown engine before
manager acquisition, one actor/operation/provider-bound conditional create, and
shipping resolver execution through a focused GraphQL schema. Full signed hosted
GraphQL/native acceptance, configured transport admission/settlement, explicit
delta responses and gRPC migration remain open. Configured hosts without admitted
execution reject rather than bypass reservation policy. This example therefore
does not claim deployment enrollment on such a host.

## gRPC conditional-write envelope (V2)

`DataService.SaveHolon` now selects the explicit engine mode before activation.
V2 validates and uses the shared hosted conditional-write boundary; the original
Legacy branch remains separate. New protobuf fields retain the original field
numbers and add:

| Field | Semantics |
|---|---|
| `SaveHolonGrpcRequest.operation_id` (9) | Stable nonempty canonical GUID, or omitted when supplied by the agreeing operation header |
| `expected_revisions` (10) | Canonical record GUID to opaque provider revision GUID; empty value means create-only; all-zero GUID is an actual legacy token |
| `holon_json` (11) | Required recursive REST-compatible holon representation for V2; omit the flat Legacy `holon` field |
| `HolonResponse.error_code` / `error_count` | Original machine-readable failure evidence |
| `is_saved` / `committed_delta_json` | Actual acknowledged save state and committed delta, when supplied by the write owner |
| `holon_json` / `result_metadata` | Recursive returned record and provider/result evidence |

The canonical recursive JSON is deliberate: it reuses `HolonJsonContracts` for
interfaces, child-role observations and exact metadata numbers, rather than
maintaining a second lossy holon mapper. It accepts no CLR type hint. Sending
both flat and recursive input rejects instead of guessing precedence. Malformed
record IDs and revisions reject instead of turning an intended update into a
create. The serializer retains the ordinary bounded JSON depth handling; the
revision map is bounded to 10,000 entries before materialization.

For example, a new logical create supplies this protobuf envelope (shown in
protobuf JSON notation):

```json
{
  "operationId": "0da8c72f-a1d5-4dd1-ae11-c822389b1510",
  "providerType": "MongoDBOASIS",
  "holonJson": "{\"name\":\"Coffee order\",\"description\":\"Conditional create\"}",
  "saveChildren": false
}
```

For an existing record, retain its loaded recursive representation and submit its
original provider revision in `expectedRevisions`. Selected children require their
own original conditions and complete stored role observations. `saveChildren`,
`recursive` and `maxChildDepth` pass to the generic planner. The old
`saveChildrenForAllProviders=true` flag rejects for V2 conditional writes;
cross-provider propagation requires explicit replication enrollment and cannot
change the single write authority. `setGlobally` publishes the selected routing
preference through the existing atomic owner after intent validation.

V2 `LoadHolon` now also returns the recursive representation in `holon_json` so
clients need not reconstruct a graph from the old flat root. Its existing read
routing/activation flow still needs migration; this output change does not prove
read authority or access-policy completion.

Tests cover protobuf round trips, recursive children and role observations, exact
large integer/fraction metadata, create-only versus all-zero revision tokens,
duplicate/malformed conditions, ambiguous payloads and result deltas. Real
loopback HTTP/2 requests execute the shipping gRPC service and verify anonymous
and missing-intent rejection. Their authentication boundary is controlled by the
fixture, not a signed JWT certification. Signed native acceptance and configured
transport admission/settlement remain open on all three providers.

Final evidence checkpoint: gate40497terminal0; portable Core and shipping WEB4 builds pass, WEB4231/231PASS0skip. Core289fa01 published. No new native cohort was run for this failure-projection change; the preceding routing native cohort is separately retained and is not relabelled as this revision's evidence. GraphQL/gRPC migration and native transport enrollment remain open.

## Signed native transport acceptance in progress

The existing retained three-provider fixture now journals a separate transport
actor and three exact record IDs before any transport seed/write. It adds 24
signed HTTP/2 requests through the shipping GraphQL save resolver and gRPC
service, the real JWT validator, and the existing configured reservation,
admission, outbox, tariff and settlement owners. Controls cover GraphQL create,
create-only conflict and missing intent; gRPC selected-child update, stale
revision, forged stored ownership, missing intent and malformed JSON. Exact
physical attempt counts, receipt counts, costs and succeeded/failed outcomes are
asserted rather than inferred from transport status. Execution is still pending;
full WEB4 250 tests and shipping/native builds passed before launch.

The GraphQL create resolver also accepts an optional nonempty client `id`, bound
to an explicit create-only revision condition. This enables stable, pre-journalled
migration identities without turning the resolver into an update API. Omitting it
retains Core's operation-derived new identity behavior.

A gRPC domain error can legitimately have HTTP 200 and grpc-status 0. The response
owner now marks that failed domain outcome explicitly for shared usage settlement;
the ledger must not infer application success from those transport statuses.
V2 read/write response projections use this same explicit outcome mark.

The native runner now freezes GraphQL, gRPC, protobuf and shared hosted-boundary
sources too, and requires all 24 transport operations to settle across all three
providers. Any failure preserves its manifest, exact operations and data for
same-original inspection; it must not be reset or retried as a fresh fixture.

### First signed cohort: preserved failure

The first expanded cohort stopped after all existing CQL/REST checks and three
signed MongoDB GraphQL requests passed. Its first gRPC selected-graph update
returned before provider execution: zero attempts instead of the expected three.
The original failed TRX is retained as
`../Evidence/Verification/signed-transports-native-first-red.trx.gz`; the fixture manifest
has SHA256 `4859E1760E50FF1D9F7425A866BCF0145F0D8520F5BB58F04DFDD5D77E120858`.
No successful native gRPC claim is made.

A matching unit input now includes REST-style string `holonType` values. The
gRPC serializer lacked the string-enum converter used by the producing REST
serializer. After the peer released its live offline Rust build lease, that unit
control genuinely reproduced JSON rejection. Adding the existing string-enum
converter at the gRPC options owner fixes the mismatch. Shipping build and all
250 WEB4 tests now pass; the original unit failure is retained as
`../Evidence/Verification/grpc-native-wire-red.trx.gz`.

`ReviewedSignedTransportEnumFixturePreservesOriginalsBeforeExactCleanup` is
prepared to inspect only this exact manifest, all 382 terminal operations, the
four original transport requests and their costs/outcomes, provider record
identity/ownership/fixture tags, untouched gRPC graph and conditional receipts.
It writes compact reviewed proof before allowing exact owned cleanup. It does
not replay any request. The existing reconciliation runner selects it only for
the reviewed manifest hash. Reconciliation passed (one test, four seconds): all
382 originals were terminal, the four transport rows matched, the rejected gRPC
graph was unchanged, and exact identity/ownership/tag/receipt checks passed.
Reviewed proof was written before removing the exact owned fixture. No request
was replayed. Compact proof and the original reconciliation TRX are retained in
`../Evidence/Verification/signed-transport-original-*`. Fresh native acceptance after the
decoder correction remains pending.

### Buffered-response root cause found by fresh native execution

Fresh cohort 57839 passed decoding and committed/settled its first signed MongoDB
selected-child gRPC graph with three physical attempts and the expected cost.
The HTTP/2 response body was empty. The fixture is preserved with manifest SHA256
`E722AC607FAB11F2CC6BFD2DC140C081FFC0683FCCB41093DBEF1D62F05273DE`;
its original failed TRX is `../Evidence/Verification/signed-transports-bodywriter-red.trx.gz`.
Do not replay that committed operation.

Two real-Mongo usage tests reproduced lost unflushed `HttpResponse.BodyWriter`
output, including a GraphQL error envelope. The shared usage middleware completed
its response capture before the pipe buffer had been drained. The correction is
at that owner: complete the buffered response feature before envelope inspection,
settlement and copying the captured response to the network. It is not a manual
gRPC-only flush. All 387 usage-protocol tests pass against the rebuilt Core,
including real-server publication/settlement controls. Final runner verification
is still in progress.

That broader gate also exposed six stale fixture assumptions: three reflected an
ambiguous newly overloaded internal method, and three scope-only probes claimed
success despite affirmative no-provider-execution evidence. The tests now select
the intended two-argument method and verify the existing conflict/failed-settlement
contract without inventing provider work. The original eight-failure baseline is
retained in `../Evidence/Verification/usage-bodywriter-baseline.trx.gz`.

The reusable usage runner now builds its Core producer before consumers in every
mode, and expects the actual 387 cases. An earlier attempt had compiled the tests
against the old Core binary because the default mode skipped that producer build;
that attempt was not evidence against the correction.

A second exact-hash reconciliation gate is prepared for the committed original.
It must verify all 382 terminal rows, seven transport attempts/receipts, the
acknowledged stored graph and exact owned footprint before cleanup. No request
may be replayed. Fresh three-provider acceptance is still open.

Buffered-response checkpoint: Core `361f3d6` is published. The corrected reusable
runner passes all 387 real-Mongo usage cases. Both Core targets and shipping WEB4
build; full Core 3,372 and WEB4 250 tests pass without skips. The exact committed
fixture reconciliation passed: 382 terminal originals, seven attempts/receipts,
acknowledged graph state and exact owned footprint verified before cleanup.
No request was replayed. Required proof is retained in
`../Evidence/Verification/signed-transport-committed-*`. Fresh full signed native acceptance
remains pending after this correction; this is not a completion claim.

## Signed save gate — passed

Native run 43815 passes on MongoDB, SQLite and Neo4j: 24 signed GraphQL/gRPC
requests through real JWT validation, configured storage admission and the shared
reservation/outbox/settlement pipeline. It verifies creates, create-only conflicts,
selected-child updates, stale revisions, forged stored ownership, missing intent,
malformed payloads, deltas and zero-execution evidence. Combined with the existing
CQL/REST suite: 402 settled operations and 436 physical attempts/receipts.

Sources stay unchanged; seven producer/native-consumer pairs match. Refreshed
Core3,372 /WEB4250 tests pass with zero skips, and both unit hosts' Core copies
plus the WEB4 unit host's shipping assembly match that exact native cohort.
Required safe result/binary/unit summaries, compressed source identities and
original TRXs are in `../Evidence/Verification/signed-transports-final*`. Exact owned fixtures
and services are removed. The earlier usage387 gate and portable/shipping builds
also passed; historical failed originals were reconciled without replay.

This closes the focused signed save-transport gate. Full startup/schema/alias,
other methods, authenticated conditional delete, authoritative recipient/tenant
queries, federation and the broader ORM roadmap remain open.

## Generic owned-delete SDK checkpoint in progress

`DeleteOwnedHolonAsync(id, actor, options, softDelete, provider)` now provides the
missing common permission boundary over existing conditional tracked deletion.
It requires V2, nonempty actor/record/intent and one original non-create-only
revision before touching providers. A pinned authoritative read supplies stored
ownership; the original caller condition and observed authority then pass to the
existing deletion engine. Provider failure counts/metadata remain intact.

Thirteen focused controls pass: soft/hard acknowledgement, foreign-owner denial,
stale token retained across a provider-default change, identity/actor/condition
rejection before I/O, legacy rejection, no failed mutation acknowledgement and
provider failure evidence preservation. Full/portable/downstream gates are in
progress. This SDK addition does not yet secure REST/GraphQL/gRPC delete routes;
those adapters and signed native acceptance remain the next gate.

Owned-delete SDK gate 55301 passed: portable/shipping builds, full Core **3,385**
and WEB4 **250** tests, with no skips. Core `5ec9ccc` is published. The 13 focused
controls are included in the retained full Core report
`../Evidence/Verification/owned-delete-core-full.trx.gz`. This checkpoint is SDK-only;
transport adapters and native owned-delete acceptance remain open. The earlier
complete signed-save proof remains pinned to Core `361f3d6`; it is separate from
the new deletion work.

## Transport conditional deletion checkpoint in progress

Canonical REST deletion and all five logical-ID route shapes now use the shared
write-intent validator, immutable request routing and Core owned deletion. The
request keeps its existing `BaseLoadHolonRequest` inheritance; a common intent
interface adds conditional semantics without changing that public base type.

Supply `operationId` and `expectedRevisions` in the canonical delete body, or use
`X-COSMIC-Operation-Id` and `X-COSMIC-Expected-Revision` for URL aliases. Headers
must agree with supplied body conditions. The revision header is one canonical
GUID; an all-zero GUID is a real revision. Missing/create-only revisions reject
before storage. Identity, role and soft/hard choice are captured before awaits.

GraphQL `deleteHolon` now accepts `operationId`, `expectedRevision` and
`providerType`, binds the HTTP actor and retains its boolean acknowledgement
surface. gRPC adds `operation_id` (field5) and `expected_revision` (field6) without
renumbering old fields. Its `OASISDataResponse` now carries `error_code`,
`is_deleted`, `committed_delta_json`, `error_count` and `result_metadata` in
fields3–7. Domain failures explicitly mark the shared usage outcome as failed.

The existing server-authenticated Wizard policy remains conditional and pinned;
client-submitted actor/role flags cannot select it. Ordinary callers use the
creator-owned Core facade. Existing Legacy engine branches remain unchanged.

Sixteen focused controls pass through real MVC aliases, the shipping GraphQL
schema and real HTTP/2 gRPC calls. They include forged identity/role and
missing/disagreeing revision rejection. Two additional wire controls preserve
zero versus absent revision tokens and deletion delta acknowledgements. Full
WEB4 gates pass all **264** tests without skips; the shipping build passes.
Required full evidence is `../Evidence/Verification/transport-delete-full.trx.gz`.
Provider-key deletion is not yet migrated, and
signed native deletion/admission acceptance is still open; this does not close
the entire transport delete matrix.
