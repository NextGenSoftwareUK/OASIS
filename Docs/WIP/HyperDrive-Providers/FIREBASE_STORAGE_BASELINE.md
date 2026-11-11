# Firebase Realtime Database checkpoint

## Current shared storage qualification

11 November 2026. [CI run 43090501206](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43090501206) uses the shipping Realtime Database REST adapter with the checksum-pinned official RTDB 4.11.2 emulator. It creates a disposable demo namespace with the indexes used by the provider, runs both existing storage modes, and checks native namespace emptiness after cleanup. [Firebase run 43090501206](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43090501206) compiled both test projects and passed all four activation-response cases. Official RTDB 4.11.2 passed the first nine asynchronous lifecycle checks and stopped at graph-save acknowledgement/count. Sync was fenced by the incomplete checkpoint. That tested revision wrote only the root. The subsequent atomic graph write and indexed graph read candidates await [native verification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43097713248). Revision history and hosted acceptance remain open. The CI emulator and reports were removed. Hosted access and production security rules require their own verification.

The CI integration entry point uses `OASIS_FIREBASE_STORAGE_CONFORMANCE=1` and `FIREBASEOASIS_DATABASEURL` pointing to the root of the loopback HTTP emulator. Its filter selects `CoreStorageAgainstRealtimeDatabase` and `HolonGraphReadAgainstRealtimeDatabase`. The existing interrupted Windows checkpoint and COSMIC query fixture are preserved.

The same job compiles the hosted and emulator entry points and runs the focused activation-response regression cases. It supersedes the earlier compile-only and emulator jobs, which were cancelled while queued.

The hosted entry point shares the exact holon cleanup routine with the emulator suite. It requires a protected test credential, checks the tombstone identity and ETag before deletion, and verifies an empty root before recording success. An incomplete checkpoint fences the other mode. Hosted execution requires the dedicated database and request budget in [Actions for David](../Release%20and%20Status/ACTIONS-FOR-DAVID.md#firebase-realtime-database-hosted-fixture).

## 28 October implementation checkpoint

- Shipping API transport: System.Net.Http calling Firebase Realtime Database REST, not Firestore.
- Existing Firestore1.22.0 cannot verify Realtime Database operations. One distinct RTDB4.11.2 JAR was subsequently cached after inspection:34,495,935 B, SHA256b70d99344caf17c98b6f910fa8f6edf32a7c016cb1035e8915f70d38901eb97f. No SDK/CLI installed.
- Fixed point/query/list reads that discarded non-success HTTP responses. HTTP failures now reach the existing OASISResult error path with HttpRequestException and native status intact.
- Collection responses accept documented JSON null/objects; empty bodies, arrays, scalar records and malformed JSON fail visibly instead of returning empty success.
- Query equality strings are JSON-serialized before URL encoding, preserving quotes, backslashes, Unicode and reserved URI characters.
- Added injectable caller-owned HttpClient overload; original constructor signature remains available. No persisted data layout changed.
- Before fix:15/15 targeted regression cases failed. After fix:23/23 maintained unit cases pass, including18 new cases. These use synthetic HTTP responses, not a native Firebase server.
- Cached producer/consumer builds passed (2 warnings each). No source/interface work in Core or Shared, no routing/depth work.

Canonical binding now builds with IncludeCoreStorageConformance=true:23controls pass,2native cases skipped without configuration. Set OASIS_FIREBASE_STORAGE_CONFORMANCE=1 and OASIS_FIREBASE_CONFORMANCE_DATABASE_URL (HTTPS origin or explicit127.0.0.1 loopback); optional OASIS_FIREBASE_CONFORMANCE_AUTH_TOKEN stays outside reports. Hosted testing additionally needs OASIS_FIREBASE_CONFORMANCE_ALLOW_BILLABLE_REQUESTS=1. The binding refuses occupied database roots, old checkpoints and redirects. Reports use fixed TestResults/ProviderConformanceCurrent/FirebaseOASIS/{async,sync} paths.

Next: native execution. Execution policy rejected launching the pinned emulator on loopback19000, stating only blocked by policy. No retry/bypass; no owner file or listener appeared. Save/read acknowledgements, activation state, child/version handling, identity/detail lifecycle and full common-interface coverage remain unproven. Do not certify Firebase from Firestore results.

Official protocol references: [read API](https://firebase.google.com/docs/database/rest/retrieve-data), [write API](https://firebase.google.com/docs/database/rest/save-data). Regression tests preserve transport errors and validate protocol payload shapes, not hosted access/security rules.

Reproduce with the existing cached SDK under Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1: bounded build of FirebaseOASIS then FirebaseOASIS.UnitTests, --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false; dotnet test the unit project --no-build --no-restore. Initial restore used only the existing NuGet package directory and installed .NET library-packs, with RestoreRecursive=false and NuGetAudit=false.

The interrupted native RTDB async run reached 1/52 checks and stopped at avatar save acknowledgement. Sync remained fenced. Commit 8ad9ea382b requires exact write acknowledgement for both avatar and avatar-detail saves before publishing generated identity to the caller. The provider build completed with four warnings and zero errors. The fix still needs a focused native write and readback check on an authorized RTDB fixture. Keep the interrupted report as evidence of the original failure.

The integration project builds the focused AvatarWriteAcknowledgementAgainstRealtimeDatabase test (15 warnings, zero errors). Run this test alone with OASIS_FIREBASE_ACKNOWLEDGEMENT=1 and FIREBASEOASIS_DATABASEURL set to an isolated loopback emulator. It creates a unique demo namespace and verifies generated identity, provider readback, hard deletion and namespace emptiness. No native execution yet.
