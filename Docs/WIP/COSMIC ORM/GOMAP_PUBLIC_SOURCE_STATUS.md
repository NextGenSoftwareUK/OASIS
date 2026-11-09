# GOMap public query source review — 8 November 2026

Status: bounded real HTTP routing client controlled-qualified (28/28); no CQL source admitted, no native route request made, no coverage increment.

The current GOMap project targets net10.0. Its existing OSRMDirectionsProvider uses an injected HttpClient or one shared client, identifies itself with an OASIS user agent, and calls the route HTTP API with full GeoJSON geometry. This is a genuine integration. The README netstandard2.1 description does not describe the current project target. Unity rendering, placement, routing and persistence require separate evidence.

Primary policy reviewed directly on 8 November:

- [OSRM official demo-server policy](https://github.com/Project-OSRM/osrm-backend/wiki/Demo-server): reasonable non-commercial use only, maximum one request per second, no availability or freshness guarantee.
- [FOSSGIS operator policy summary](https://routing.openstreetmap.de/about.html): required attribution and fix-the-map link, valid user agent/referrer, maximum one request per second, no scraping/heavy usage; requests are logged. Profile configuration can differ from stock OSRM.

The current client does not serialize requests or enforce a shared rate limit, reads the entire response without a source byte limit, has no cancellation parameter, accepts malformed coordinate entries by skipping them, and returns an empty list on failures with LastError as side-channel evidence. Do not expose this unchanged as a bounded CQL catalogue or infer that an empty list is a successful native read. Existing routing behavior has not been edited by this review.

Next engineering gates: choose a supported hosting/use contract for the public sandbox; preserve the real routing API while defining strict bounded response/schema/error/cancellation behavior at the responsible layer; establish fixed public teaching coordinates and stable identity without exposing user locations; retain full route data and attribution; qualify controlled and native paths in the reusable cloud environment, then WEB4 source registration, owner scoping, presets, federation, privacy and HyperDrive replicas. No unreviewed alternate endpoint, local OSRM dataset/container/install, account request or live enrollment was introduced.


Bounded client implementation prepared (controlled cloud compilation pending): the existing directions interface delegates to GetRouteAsync, which returns OASISResult<OSRMRouteObservation>. This is one request/validation path, with process-wide one-second dispatch spacing, cancellation before dispatch and during wait/stream reads, 256KiB content bound, strict UTF8/depth32/duplicate-property checks, exact Ok/non-alternative LineString/2-4096 WGS84 coordinate pairs/two snapped endpoints, finite nonnegative metrics, complete raw JSON/hash/observation time. HTTP errors do not retry; redirects are disabled on the owned client; injected transports cannot turn a redirected response into an admitted observation. HTTPS origins exclude credentials/path/query. The legacy interface retains explicit LastError on its empty error projection.

Twenty-six declared controlled cases and a cloud-only leased qualification workflow cover real request/profile construction, complete observation, malformed payloads/coordinates/metrics, HTTP401/403/429/503 without retry, input rejection without dispatch, cancellation preflight and legacy error projection. These declarations are not a passing-test claim. No local .NET build/runtime/install, real routing request, CQL adapter, Core admission, website card, live setting or scope-count change. This work remains outside the prepared four-provider candidate. Public hosting/use-contract/native-rate/attribution and CQL/HTTP/replica/browser gates remain open.

First source5d3a6960362b1bf8ae9bd29c11d2dc8679c34243 cloud42476678025/job128719482090 failed before test execution: existing GOMapOASISProviderTests lacked the Contracts.Enums import for MapProviderType (CS0103). Client compilation completed; no TRX was produced and no tests/native requests/coverage are claimed. Corrected that namespace import without removing tests. Client now adds a linked30second end-to-end deadline including gate/response streaming, plus real gate-wait cancellation and cross-instance request-spacing controls (28declared cases). Actual corrected cloud run required.

Corrected source32a252125b56adeaa63e00a3b49699fea92e5871 run42476814718/job128719902348 executed28cases:27pass/1fail/no skips. Only failure: MSTest exact-type assertion expected OperationCanceledException while the actual canceled HTTP task correctly propagated its TaskCanceledException subclass. Updated the test helper to require cancellation across that defined exception hierarchy; zero-second-dispatch assertion remains. No production request/parser change or native route call. Successful corrected run/artifact required before qualification.


Cloud sourcef393451ad6da967aa0222fe5b6bf2a73c9498a4d run42476991119/job128720435514 SUCCESS. Actual TRX artifact13616200194/4667bytes SHA25668e1466097f145e7c0baefd77edb110a5b8ab8c473da268bd47dec147786f820 inspected wholly in memory:28executed/28passed/0failed/0skipped. This qualifies only the selected controlled HTTP/parser/cancellation/shared-rate cases, not all Unity/provider behavior, a real OSRM request, hosting eligibility, CQL/WEB4/replicas/browser or full ORM parity. Original compile/test failures remain above; no assertions removed. No local image/build/runtime/environment installed.

Optional native developer qualification now prepared: manual native_route=true only, following all28controlled cases in the same leased cloud build. Three sequential driving requests use only the official API example Berlin points; actual raw responses/hashes/metrics retained in an explicit owned evidence path with OpenStreetMap attribution/fix-map links. Actual native check has NOT run. No retry, user locations, live public sandbox hosting decision, CQL/WEB4/replica claim or four-provider candidate change. Production/commercial eligibility remains distinct from this bounded developer check.

Native developer source24d53fc823f2f998fe38fc78ab02bac92f6a0344 run42477332751 SUCCESS:28controlled+1native case, three actual fixed Berlin routes, zero retries. Artifact13615454829/10604bytes SHA25627684722d41071270a8aebfb90b4e459c3afadc57f053479be9ff04451f21b97 inspected wholly in memory: source/endpoint/public-coordinate declaration, all three raw-response hashes/LineStrings/geometry counts/two snapped endpoints/attribution and both TRX counters verified. This developer-native proof does not authorize commercial/public sandbox hosting, CQL/storage/WEB4/replicas/browser or claim full GOMap parity. No local archive/runtime/build installed.


Bounded CQL source now prepared, not yet qualified: GOMapRouteQuerySource delegates every transport/parse to the already qualified OSRMDirectionsProvider. Three exact route keys (west-north/north-east/east-west) use the same fixed public Berlin points/driving profile, stable owner-scoped native IDs and public read-only mapped holons. Complete original JSON/hash, metrics, geometry count, observation time and attribution/fix-map links are preserved. Shared bounded-key CQL compiler/reader handles equality/membership, selection preflight, cancellation, no-partial-results and paging; no visitor coordinates, scans or source writes. Twelve controlled cases and one cloud-only leased workflow prepared; no native CQL/manager/WEB4/tariff admission, live deployment or coverage increment yet. Existing four-provider candidate remains unchanged.

CQL controlled source4dfc9d2d48d3ce61bbf16b7087cf1bbd9e512272 run42479732136/job128728848944 SUCCESS:12/12/no skips. Artifact13616694124/2748bytes SHA256b419b1ef651e9f3e6fcab46174967463e8ce2439c69e0697678e5fbd773e05cd verified in memory. Native CQL remains unproven. Added separately opt-in native_query workflow case using actual fixed-route adapter and common planner: full3/subset2, IDs/raw hashes/attribution retained; activation/full/subset schedule only, no source writes. Controlled filters explicitly exclude that native category; actual native dispatch still required.

Native CQL source525434ce2fc79bffc4c097c6d00d1a9bfc059062 run42480043896/job128729794809 SUCCESS:12controlled+1native case/no skips, actual full3/subset2 CQL results. Artifact13616947227/8168bytes SHA256151a673e50db17ae14370350e9b5d17faee293eb20481cca7b72e009f6aac9ec inspected wholly in memory: owner/provider/counts, exact fixed route keys, subset ID inclusion, all five complete native response hashes/LineString/two-snapped-endpoint payloads and attribution/fix-map links verified. No source writes, native alternate endpoints or retries. This qualifies the adapter/common planner/native CQL scope only; shared manager/WEB4/Core query tariff admission, Mongo federation/privacy/replicas, live hosting/browser remain open. Coverage checkpoint unchanged until integration is proven.

Core/WEB4 integration prepared: Core8b085f846ed91cd883ed591ba569cfb68e31fe01 adds only GOMap to the explicit query-tariff set (unowned ProviderType edits preserved). API registers an inactive fixed-route source only for canonical CQL_GOMAP_ROUTE_QUERY_SOURCE_OWNER; plain provider selection remains rejected. Eight boundary declarations enforce absent/invalid config, explicit tariff, no write tariff and exact native/federation preset planning. Actual loopback HTTP/Mongo native case prepared, with owned ephemeral replica helper requiring cloud context/labels and verified container/anonymous-volume removal. Producer build uses one lease/m2; native test consumes frozen private outputs with no build. Core gitlink/manifest advanced together in task commit; exact cloud integration remains pending. No live setting/source-owner/candidate/cohort change.

Future exact-key preset tooling now recognizes GOMap through the existing fixed-public catalogue mechanism (Actor only; exact source namespace/three keys). Python IDs are locked to the actual native CQL fixture f2c0c826/ba0c3bc1/d25cf63e and the C# catalogue regression carries the same full IDs. All39shared configuration test methods pass with controlled temporary fixtures, preserving existing providers/refusing replacement. No real descriptor/owner/settings candidate was created and no settings applied. Original WEB4 run42480693015 remains active; source/fixture/tooling qualification must stay distinct.

WEB4 integration sourcea1417375e0e4a1b0e1242d9137f25fa53b33f9c1/Core8b085f846ed91cd883ed591ba569cfb68e31fe01 run42480693015/job128731756954 SUCCESS. Artifact13617567136/5132bytes SHA2566fb05c12d16978512c743dbc79f93f8719c7f59507b46aa59c32764f2e004a47 verified in memory:8boundary+1real HTTP/Mongo case/no skips; native3/2, paired5/4, independent private0/public3,2SDK saves/9receipts and owned database absent. Separate cleanup artifact13617866303/237bytes SHA2568422b2d02ec55b82aa5bbed7996aea6ef1ad761b40091c0a0cac9755a0faa822 proves exact run owner/container and anonymous volumes absent. This adds one scoped integrated local/cloud CQL type (checkpoint129), not live HTTP/browser/replica or full GOMap/Unity certification. Further GOMap rollout is behind the user-priority chain queue; no new feature/native schedule started.

9 November preflight. The original four-provider acceptance seals
and active 270-setting Development baseline matched. Prepared the
GOMap public route descriptor, three logical IDs, and protected
before/candidate snapshots under the gomap-public prefix. The
candidate changes exactly four settings. The descriptor and owner
are retained in HostedGOMapPublicSourceCurrent. No live settings
or provider data were changed. Scoped candidate integration and
exact build qualification remain required.

9 November deployment handoff. The code-only WEB4 Development deployment
76806353-8f77-4052-ba45-5c1679418f01 at e2286e442013a2f9137ab694a61349e2e5856e02
passed running-package inspection. The GOMap reader is present, the complete
settings matched the protected baseline, and all four prior enrollments remained
enabled. GOMap is still disabled. The inspected image digest is
sha256:381096f87837bb2202a02a9825384b4a7971111ab9f7a22f19833ef08cd61bfc.

David has put Development pushes on hold until he explicitly lifts the hold.
Keep GOMap settings and activation on hold too. On release of the hold, recheck
the active deployment and full settings against the saved baseline before
applying the four-setting candidate. Then verify the running settings, fixed
native routes, federation, private-save isolation and replicas. Browser
verification is a separate gate.

Offline replay passed against the original protected snapshots. The saved route
descriptor SHA-256 matched the source plan, the reviewed catalogue rebuilt the
candidate byte-for-value, and the exact four planned keys accounted for every
difference between the 270-setting baseline and 272-setting candidate. This
check issued no provider requests and wrote no Development settings.

Before public enrollment, close the routing endpoint use gate above. The
three-route cloud qualification establishes developer use, and the protected
settings replay establishes configuration integrity. Public sandbox use still
requires a documented endpoint contract covering expected traffic, concurrency
across WEB4 instances and required attribution. Keep this gate in the rollout
checklist when the Development hold is lifted.

Task commit 7181193d2 requires a dedicated HTTPS OSRM origin for hosted GOMap
registration and rejects the two public demo hostnames. The historical
four-setting candidate remains evidence of the previous preflight only.
A future candidate must include CQL_GOMAP_ROUTE_QUERY_SOURCE_ORIGIN and be
reconstructed from the then-active baseline with the reviewed endpoint.
Qualify the new package and inspect the effective settings before live acceptance.
Task CI run 42625727494 covers the hosted registration change.
