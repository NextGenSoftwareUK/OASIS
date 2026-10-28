# Document-family baseline work

28 October 2026. Shipping REST clients are real APIs; controlled HTTP tests do not certify a live deployment. Reuse the canonical core-storage-v2 suite for native storage; do not replace it with these regression tests.

## Appwrite

- Fixed denied/throttled/server-error reads being interpreted as absent records or empty collections. HttpRequestException retains native status through existing OASISResult error handling.
- Later-page HTTP errors now fail the collection instead of returning partial success. Missing/null envelopes, missing documents/total and premature empty pages fail visibly.
- Boolean equality query had an unclosed value array; fixed the missing bracket. A regression parses the value array as JSON, rather than comparing an implementation string.
- Existing constructor retained; injectable caller-owned HttpClient supports controlled transport tests without a network/database.
- Thirteen targeted regressions reproduced red before fixes:12read/envelope cases plus1boolean query case. Final maintained suite19/19 passes:14new cases plus5existing identity checks. Cached producer build6warnings, consumer2warnings,0errors.
- No native Appwrite server was contacted. Full query/pagination compatibility for the deployed API version, source-side ordering, concurrency, all acknowledgement/identity/detail/child/version contracts and shared52-case native suite remain open. Top-level pagination parameters and old response-format header were not certified by these tests.
- Provider remains on its existing collection schema/data layout. No Core/Shared public API or storage-family base changed in this checkpoint; shared record-policy consolidation remains a family implementation gate.

Official [REST API reference](https://appwrite.io/docs/references/cloud/server-rest/databases) is the protocol source; current reference marks legacy Databases endpoints deprecated. Deployment/version acceptance must be tested before claiming support for their replacement.

Reproduce under Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1: bounded cached build of AppwriteOASIS then AppwriteOASIS.UnitTests using --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false, then dotnet test the unit project --no-build --no-restore. Initial nonrecursive restore used existing NuGet cache/installed library-packs only; no downloads/runtime/service created.

## Next

PocketBase shows the same non-success-to-empty pattern and an unpaged500-item read. Review native pagination/auth contracts and fix them with regression coverage. Convex already throws for non-success HTTP responses; its envelope/function provisioning and shared-suite gates still need review. Do not copy Firestore acceptance onto Firebase Realtime Database; its separate binding/runtime gate is in FIREBASE_STORAGE_BASELINE.md.
