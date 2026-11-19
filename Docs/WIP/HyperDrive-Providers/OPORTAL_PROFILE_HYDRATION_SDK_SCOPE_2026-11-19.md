# Profile hydration SDK scope — 19 November 2026

Status: source and generated delivery verified; real browser/API/backend qualification still required. OPORTAL remains incomplete.

The dashboard calls `hydrateAvatarProfile` to refresh profile details. Its request owner used the JWT cached inside the avatar record while the actual SDK sends its current bearer. Missing cached credentials could suppress a valid SDK read; an old in-flight request could absorb a new bearer/client intent.

Portal source13e15141d7a6a9ee6d4c06aa2e6e47983dfe46ab makes the SDK token store authoritative for this read and includes client identity in pending-read coalescing. Existing ID/client/token checks suppress late publication. No request, cache, timer, retry, fallback, new parser or backend write added. The existing legacy state-test setup was aligned with the required token-store contract; it is not real-backend evidence and was not selected as acceptance.

Bundle44603511203/job135840944006 SUCCESS generated7f01ee9fa20eb2853561f06f587e3758b3146717. Existing mandatory delivery gates and owned install/cache cleanup passed. Recorded input exactly13e15141, SDKd7aa569, avatar-modal SHA25621ad1906c55da6d75af5bcbc41cd17815ebad048c4a5363a09485918944a60ec. JavaScript syntax checked through stdin with the existing Node runtime; no install or temporary source files. An initial command-line-length failure happened before source publication; stdin parsing corrected the tool invocation, not product behavior.

## Next real qualification

Use the reusable subscription HTTP host with the actual `AvatarProfileController` route `/api/avatar/get-avatar-detail-by-id/{id}` and existing isolated Program avatar manager/native Mongo. Explicitly disable the optional test-data setting in the owned host; use real stored actors/detail records, actual SDK, Chromium and component code. Add a NEW profile-hydration-only mode; do not rerun prior profile edit or dashboard subscription acceptance.

Require: cached expired JWT with valid SDK bearer; missing cached JWT; coalesced same-client/same-bearer read; changed bearer starts a new read and old actual reply cannot publish; changed client with identical bearer does not reuse old pending work; account change; expired SDK401 and successful recovery. Check actual IDs/karma values, no business writes, unchanged native avatar/detail and financial state, explicit errors and all owned cleanup. Held transport must continue original requests, never replace business responses. Production Startup, refresh endpoint, portrait/edit acceptance, all dashboard cards and hosted speed remain separate.

## Real qualification prepared

NEW `ProfileHydrationUsesCurrentSdkBearerClientAndActualNativeController` uses the existing isolated host with actual AvatarProfileController and native Program avatar manager; optional test-data setting explicitly false. Frozen Portal7f01ee9f/SDKd7aa569; ten exact real GETs and six published updates across three stored actors, held original requests continue. Covers cached-expired/current SDK, no cached JWT, duplicate pending coalescing, bearer replacement, client replacement with identical bearer, account change, expired SDK401 and recovery. All old replies must resolve without publication. Native profile plus eight financial collections unchanged, zero business writes/page errors/external calls, provider/database/process and browser/runtime/cache cleanup required. Source syntax/diff checks passed; no actual build/browser/native acceptance yet. Prior accepted tests not selected.
