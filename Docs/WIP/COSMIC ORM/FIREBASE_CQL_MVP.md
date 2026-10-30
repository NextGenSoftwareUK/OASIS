# Firebase Realtime Database CQL MVP — native verified, paired pending

Updated 29 October 2026. Native and explicit emulator checkpoint restore pass; paired/browser remain red.
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

Provider/unit cached restores and bounded builds pass. **50 non-native tests pass**,
zero failed/skipped. Explicit native storage conformance cases were excluded;
this count does not certify them. [CQL tests](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests/FirebaseCqlTests.cs).
Evidence: `C:/Source/OASIS-cosmic/TestResults/FirebaseCqlCurrent/firebase-controls.trx`.
SHA256 `6d69139f48d5428c0de6f3d77dad1fc82093b9ef71ee214e3be88b4ea7b3b3d8`.
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
The separate full native storage conformance matrix remains outstanding. These
controls do not establish full graph or production Firebase durability.

## Native emulator and checkpoint restore

Reused cached4.11.2JAR (34,495,935B) at
`C:/Users/david/.cache/firebase/emulators/firebase-database-emulator-v4.11.2.jar`.
SHA256 `b70d99344caf17c98b6f910fa8f6edf32a7c016cb1035e8915f70d38901eb97f`.
Reused Microsoft JDK21.0.12 at
`C:/Program Files/Microsoft/jdk-21.0.12.101-hotspot/bin/java.exe`.
No SDK/runtime/emulator download. Version command and actual native REST verified.
JVM bounded to256MiB/two CPUs; binds loopback19091, namespace `cosmic-cql-demo`.
Optional explicit databaseNamespace is encoded on every provider read/write/query;
normal production URLs retain their existing namespace behavior.

Fixture: `C:/Source/OASIS-cosmic/LocalState/COSMIC/FirebaseCql` (9,823B), retained
original actor/fourIDs/write+delete acknowledgements/checkpoint/hash. Emulator-owner
header is confined to this local teaching fixture; public read rules deny access.
Four saves/one soft delete produce3live/1deleted. Missing/deleted catalogue yields
3live, native-key membership union yields2, paged query1. Unsupported name rejects.
[Native test](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.IntegrationTests/FirebaseNativeCqlTests.cs).

RTDB emulator is memory-only. The runner imports the exact original checkpoint
into an explicitly owned empty process, matching the [Firebase CLI import protocol](https://github.com/firebase/firebase-tools/blob/master/src/emulator/databaseEmulator.ts).
It does not reseed, change IDs or replay business saves. Native1+export-restore1
passed, then reuse1+restore1 passed without new seed writes. This proves original
logical data recovery from an export, **not** native disk/crash durability.
Complete original source hash stayed
`6222b4436a54a485f172e2d34413a248c0ed67c7e8b738dc20271c0101fea343`.

Evidence under `TestResults/FirebaseCqlCurrent`:
- `native.trx`: `78cff8acda7b2a6cd0eb1708078cdc81acc41c945af07795ef798d57c28d2d39`.
- `restart.trx`: `2898a0f896fe8e927576e6458a2726f68d66eb0b9978e69340c341a22699b5d3`.

After leased bounded provider/integration builds:
```powershell
python Scripts/TestHosts/run-cosmic-firebase-native.py
```
Runner refuses pending outcomes, occupied ports or missing original checkpoint.
A failed uncheckpointed volatile source is preserved live with its PID receipt.
Successful sources stop only after exact checkpoint verification (Windows process
termination; no clean native disk-flush claim). No19091listener remains.

## Exact next step

Reuse this original fixture/checkpoint for one Mongo-paired C#/HTTP/browser preset.
Do not count Firebase demo-ready until paired/browser checks pass. Hosted Firebase
credentials/rules, atomic conditional concurrency, full numeric range, arrays,
child/history and broader graph/storage conformance remain separate open gates.
