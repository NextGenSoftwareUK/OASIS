# Firebase Realtime Database checkpoint

28 October 2026. This is an implementation checkpoint, not native conformance certification.

- Shipping API transport: System.Net.Http calling Firebase Realtime Database REST, not Firestore.
- Existing cached emulator is Firestore1.22.0; it cannot verify Realtime Database operations. No new emulator, runtime or SDK downloaded.
- Fixed point/query/list reads that discarded non-success HTTP responses. HTTP failures now reach the existing OASISResult error path with HttpRequestException and native status intact.
- Collection responses accept documented JSON null/objects; empty bodies, arrays, scalar records and malformed JSON fail visibly instead of returning empty success.
- Query equality strings are JSON-serialized before URL encoding, preserving quotes, backslashes, Unicode and reserved URI characters.
- Added injectable caller-owned HttpClient overload; original constructor signature remains available. No persisted data layout changed.
- Before fix:15/15 targeted regression cases failed. After fix:23/23 maintained unit cases pass, including18 new cases. These use synthetic HTTP responses, not a native Firebase server.
- Cached producer/consumer builds passed (2 warnings each). No source/interface work in Core or Shared, no routing/depth work.

Next: bind the unchanged canonical core-storage-v2 suite to this provider using an explicit isolated Realtime Database service. Save/read acknowledgement counts, activation state, child/version handling, identity/detail lifecycle and complete common interface coverage remain unproven. Do not certify Firebase from GoogleCloudOASIS Firestore results.

Official protocol references: [read API](https://firebase.google.com/docs/database/rest/retrieve-data), [write API](https://firebase.google.com/docs/database/rest/save-data). Regression tests preserve transport errors and validate protocol payload shapes, not hosted access/security rules.

Reproduce with the existing cached SDK under Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1: bounded build of FirebaseOASIS then FirebaseOASIS.UnitTests, --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false; dotnet test the unit project --no-build --no-restore. Initial restore used only the existing NuGet package directory and installed .NET library-packs, with RestoreRecursive=false and NuGetAudit=false.
