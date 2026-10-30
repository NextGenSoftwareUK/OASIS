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

Provider/unit cached restores and bounded builds pass. **34 non-native tests pass**,
zero failed/skipped. Explicit native storage conformance cases were excluded;
this count does not certify them. [CQL tests](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests/FirebaseCqlTests.cs).
Evidence: `C:/Source/OASIS-cosmic/TestResults/FirebaseCqlCurrent/firebase-controls.trx`.
SHA256 `9bb5f71613add91d3fdab098f049a13505cc13bde37dd699982cef53fa866206`.
[Native REST query semantics](https://firebase.google.com/docs/database/rest/retrieve-data).

## Exact next step

Fix canonical holon save ownership/acknowledgement and soft deletion, with focused
tests. Existing save mutates caller ID/key before dispatch and lacks saved flags;
soft deletion ignores PATCH acknowledgement and writes a derived isDeleted flag
rather than the canonical DeletedDate. Common point/list reads also need a single
key/payload identity invariant. Avatar/detail and full child/history parity remain
separate gaps. No native scope or global precision guarantee is claimed.

Then reuse the cached RTDB emulator4.11.2 JAR at
`C:/Users/david/.cache/firebase/emulators/firebase-database-emulator-v4.11.2.jar`
(34,495,935B; historical owner/provenance not yet independently admitted).
Inspect reusable Java/runtime/config/state first. Do not download an emulator or
use Firestore's different cached JAR. Admit one retained RTDB namespace/schema,
record original actor/IDs and write intents outside TestResults, verify native
filtered multi-record queries and retained restart data, then Mongo-paired
C#/HTTP/browser using the shared teaching host. No hosted account or dataset
has been created for this CQL milestone. Leave inventory red until full proof.
