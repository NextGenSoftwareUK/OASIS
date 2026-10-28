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

Fauna service/lifecycle review next, then remaining document-family baselines. Convex companion implementation/deployment and all native shared-suite gates below remain open. Do not copy Firestore acceptance onto Firebase Realtime Database; its separate binding/runtime gate is in FIREBASE_STORAGE_BASELINE.md.

## PocketBase

- Current official API uses `/api/collections/_superusers/auth-with-password` and the raw token in Authorization. Replaced obsolete `/api/admins` route; no automatic legacy fallback. Servers predating the superuser collection are not certified by this change.
- Read HTTP failures retain HttpRequestException/status through existing OASISResult handling rather than becoming missing records/empty collections.
- Collection reads now follow page numbers with `sort=id` and explicit totals, rather than silently stopping at500 records. Validate page/perPage/totalItems/totalPages/items, completeness, duplicate native identities and consistent continuation metadata. Failed later pages never return partial success. Changing totals during concurrent writes cause an explicit error; this is not snapshot isolation.
- Authentication validates a nonempty token before caching it. An invalid token cannot poison later activation into false success. Caller-owned HttpClient overload keeps original constructor available.
- Fifteen targeted regression cases observed red (14read/page cases,1empty-token case). Final24/24 unit cases pass:19new plus5existing identity checks. Includes valid empty collections and continuation failure controls.
- Cached builds passed (producer6warnings, consumer2warnings,0errors); no native service, database, SDK download or runtime install. Native canonical52 suite, actual deployment auth/rules, full identity/child/version/lifecycle contracts, selector escaping and shared record-policy base consolidation remain open.

Official [record pagination](https://pocketbase.io/docs/api-records/) and [authentication](https://pocketbase.io/docs/authentication/) references. Reproduction follows the Appwrite bounded cached-build recipe above, using PocketBaseOASIS and its unit project. Existing NuGet cache/installed library-packs only; fixed reusable output paths recorded in cleanup ledger.

## Convex

- Functions HTTP API returns a status/value envelope. The adapter previously treated successful envelopes as empty lists and accepted function-error envelopes as successful writes. Now require valid status, throw on function error, extract explicit success value and validate list/record shapes. Remote error/log payloads are not copied to caller diagnostics.
- Deployment admin keys now use the Convex authorization scheme, matching the official JS client; requests explicitly select JSON format. Non-success HTTP status remains available as HttpRequestException. Existing constructor retained, injected HttpClient stays caller-owned.
- Fifteen new regressions observed red before fixes; final20/20 unit cases pass (15new+5existing). Cached producer/consumer builds passed with2warnings each,0errors; no native deployment/service or download.
- Source claimed to ship companion code, but no oasis/avatars or oasis/holons modules exist in this provider directory. Corrected the claim; real companion implementation/provisioning remains required. This checkpoint does not implement those modules or certify any deployment.
- Native52 suite, all relevant interfaces, action return acknowledgements, identity/detail/child/version lifecycle, encrypted shared-record-policy base and deployment-auth acceptance remain open. The existing /version activation probe is readiness only, not proof that required functions or credentials work.

Protocol sources: [Functions HTTP API](https://docs.convex.dev/http-api/), [official JS HTTP client](https://github.com/get-convex/convex-js/blob/main/src/browser/http_client.ts). This is distinct from user-defined HTTP Actions on convex.site. Reproduce via the same bounded cached-build recipe above for ConvexOASIS and its unit project.
