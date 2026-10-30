# Firebase Realtime Database CQL MVP — controls only

Updated 29 October 2026. Native/restart/paired/browser proof remains red.
Firebase Realtime Database is distinct from the already-proved Firestore adapter.

## Implemented query controls

- Explicit Firebase authority, shared bounded GUID identity compiler and trusted
  source catalogue; equality/membership/boolean composition and paging, at most100IDs.
- RTDB has no native IN. Execution is one bounded union of indexed `$key` equality
  REST queries, at most100 requests. Each response must contain only the exact key
  and matching payload identity. No collection scan or managed business predicate.
- Strict object/null result shape, no foreign/multiple rows, authorization check,
  missing/deleted exclusion before paging, cancellation and aggregate4MiB boundary.
- Inactive source, failed later read or result-budget overflow returns an error
  without earlier rows. Reads can observe different moments; no shared snapshot.
- Activation checks one reserved empty-GUID node instead of the SDK-only connected
  status path; provider activated state follows successful activation/deactivation.

Provider/unit cached restores and bounded builds pass. **49 non-native tests pass**,
zero failed/skipped. Explicit native storage conformance cases were excluded;
this count does not certify them. [CQL tests](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests/FirebaseCqlTests.cs).
Evidence: `C:/Source/OASIS-cosmic/TestResults/FirebaseCqlCurrent/firebase-controls.trx`.
SHA256 `becc537dcc8b11e05e00e9d296e4c0b011c3c063f19b354aa025fc3219f62a44`.
[Native REST query semantics](https://firebase.google.com/docs/database/rest/retrieve-data).

## Exact next step

Canonical holon ownership/read/delete controls now pass:
- Freeze runtime-derived payload/proposed ID/key before dispatch; publish caller
  identity/key/saved count only after exact native acknowledgement. Failed/unknown
  dispatched results carry `INativeMutationReconciliationRequired`; no replay.
- One key/payload reader validates point/list/parent/CQL identities. Soft delete
  writes canonical DeletedDate through the same acknowledged save, not a patch
  of the derived IsDeleted flag. Avatar/detail ownership remains separate.
- RTDB removes null/empty nodes. The declared wire projection removes them before
  dispatch without changing the caller; acknowledgement is checked against that
  projection. No alternate storage envelope or acknowledgement repair path.
- Values outside the supported JSON numeric roundtrip range fail before dispatch
  with FIREBASE_REPRESENTATION_UNSUPPORTED. This rejects unsafe large integers and
  high-precision decimals instead of certifying a lossy save. Full generic numeric
  range, arrays/graph/default-value roundtrips require broader native acceptance.
- Write acknowledgement uses strict UTF8 and4MiB streamed boundary; overflow is an
  uncertain dispatched write, not success. Manager still owns tracked baselines.

[Ownership tests](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests/FirebaseOwnershipTests.cs).
Native storage conformance and emulator verification remain outstanding. These
synthetic tests do not establish durable Firebase behavior or full graph parity.

Next reuse the cached RTDB emulator4.11.2 JAR at
`C:/Users/david/.cache/firebase/emulators/firebase-database-emulator-v4.11.2.jar`
(34,495,935B; historical owner/provenance not yet independently admitted).
Inspect reusable Java/runtime/config/state first. Do not download an emulator or
use Firestore's different cached JAR. Admit one retained RTDB namespace/schema,
record original actor/IDs and write intents outside TestResults, verify native
filtered multi-record queries and retained restart data, then Mongo-paired
C#/HTTP/browser using the shared teaching host. No hosted account or dataset
has been created for this CQL milestone. Leave inventory red until full proof.
