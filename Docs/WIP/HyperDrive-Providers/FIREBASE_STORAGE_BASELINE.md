# Firebase Realtime Database checkpoint

28 October 2026. This is an implementation checkpoint, not native conformance certification.

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
