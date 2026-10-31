# Hosted sandbox integration — active priority

31 October2026. User clarified that all demonstrated providers must run through
the hosted sandbox, like MongoDB/SQLite/Neo4j. Local launchers are development
evidence, not completion of hosted demo coverage. Pause additional local catalogue
expansion (including Milvus) until hosted integration makes progress.

## Current truth

-3 providers verified through deployed authenticated API: MongoDB, SQLite, Neo4j.
-41 additional providers have scoped native/local or temporary hosted SDK/HTTP
 demo evidence and public catalogue links. They are not41 hosted sandbox providers.
- Public headings39 are neither provider count nor hosted coverage count.
- Compiler read-profile admission additionally names Cosmos DB/IPFS/SOLID/
 ActivityPub, but source/config/policy/deployment/live acceptance is not proven by
 that list. Federation and public selector still admit only the original three.
- Azure account remains pending/free-only; no paid signup or assumed credentials.

## Required work

1. Inspect deployed provider registrations/configuration and available persistent
 hosted runtimes. Do not point public services at developer loopback addresses or
 forward browser JWTs to third-party providers. No unapproved paid resources.
2. Enrol actual configured sources through host-owned provider/read-policy registry.
 Discovery exposes only configured/eligible profiles and supported query presets.
 Fix hard-coded admission at its owning boundary; do not enable every enum blindly.
3. Retain per-avatar visibility, authoritative catalogue ownership, response budgets,
 native error propagation and capability-aware rejection. An arbitrary submitted
 ID is not an authorization grant. No local proxy/fixture rows disguised as hosted.
4. Use the existing CQL tab's query/request/result flow for newly enrolled providers;
 show service location and native/emulator status. Keep optional local teaching
 launchers separate. Unsupported metadata/text/etc queries must reject honestly.
5. For each provider verify deployed health/config identity, authenticated full and
 filtered multi-record query, Mongo federation, provenance, tenant isolation,
 capability rejection, native-source observation and cleanup/retention contract.
 Keep evidence counts/hashes/paths and never mark the provider hosted-green from
 compiler tests, a catalogue card or local-only acceptance.
6. Prioritise the requested demo providers (Oracle, IPFS, SOLID, ActivityPub,
 Solana, AWS/Azure as available) before remaining breadth; preserve existing
 Mongo/SQLite/Neo behaviour. Batch verified deployment once/twice daily.

HyperDrive failure matrix/full ORM parity remain open. Hosted read/query evidence
does not prove replication/CAS/transactions or100% uptime. Deep LINQ remains paused.

Exact next step: inspect deployed eligible sources and existing server-side CQL
read-access policy/registration, then implement and verify the first additional
provider in the hosted request/result flow. No further local-only card milestone
is a substitute for this requirement.

## First registration fix verified —31 October2026

ActivityPub was absent from the BootLoader registration switch. It now uses the same provider configuration factory as tests, with a valid instance-origin DNA default, explicit EnableCqlStatusKeySets and optional AccessTokenEnvironmentVariable. Overrides cannot rebind credentials to another origin; missing configured tokens reject, never downgrade to anonymous. Construction does no I/O and does not change the node default. DNA/provider/BootLoader builds and10 host configuration tests pass. TRX TestResults/HostedCqlIntegrationCurrent/activitypub-host-configuration.trx SHA256914658BF4790127FDE4F5E2705DA3053236CC05039D18CA79F64C7D496CCD431. Not deployed; no fourth hosted provider claimed.

Next root cause: current ordinary-provider admission requires write tariffs for every ordinary provider whenever write routes exist. Query-only social sources need explicit read-only enrollment and an enforced write prohibition, not fabricated write grants. Then enrol actual hosted source/discovery/presets and verify authenticated live queries/Mongo pairing. Existing local launch links remain.

## Query-only admission verified —31 October2026

Server configuration now supports explicit QueryOnlyProviders separate from ordinary writable storage. Both preflight and direct physical dispatch prohibit non-CQL calls before provider/outbox I/O, including SDK policies with optional storage pricing disabled. Explicit query tariff required; duplicate/overlapping/native/ambiguous grants rejected and caller array mutation cannot change captured permission. Existing ordinary storage unchanged.12 new controls pass and52 existing configured-admission regressions pass (64total/no skips). TestResults/HostedCqlIntegrationCurrent/query-only-configured-regression.trx SHA25637BB48BF44B86578BED456EDD9F50817E458130964867FBEE9F981B9687D9C76. Initial12-case run had1 invalid test fixture (leaf reserved4 attempts instead of1); corrected fixture, original failed TRX preserved. No production contract weakened. Next is actual hosted source activation/discovery, then presets/federation and deployed/browser verification. Still only3 hosted providers verified. Guide Docs/Devs/CQL_QUERY_ONLY_ADMISSION.md.

## Hosted registration/discovery and read-only activation verified

Background enrollment now uses only the admission factory's captured query-only providers, leaves default/routing lists unchanged, and requires explicit ICosmicReadOnlyQueryActivation (no generic provisioning activation fallback). ActivityPub verifies one cancellable/1MiB-bounded native instance GET. Hosted deadline30s awaits the owning request. Authenticated GET /api/data/query-cql/providers describes configured tariff-backed sources; query-only readiness requires completed verification, not just an active flag. Native failures are explicit/redacted and never switch sources. Missing factory exposes no fabricated sources. Native feature descriptions are not full API/query parity guarantees; protocol federation remains unavailable until integrated.

100 affected controls pass/no skips:64 Core admission/snapshot,21 API discovery/policy/sandbox and15 ActivityPub activation/configuration. Paths TestResults/HostedCqlIntegrationCurrent: hosted-source-core-regression.trx SHA25615C858164C7158B10FD6A529F69C138D3B5D7CA5DE67A6360EA1746422B42BF6; hosted-source-discovery-verified.trx SHA256621F150A12876B1447A1C30C732C4EEAE89F4D285B81D9DFE9381CE11F127E28; activitypub-read-only-host-activation.trx SHA256694715C916E354B886C3A9237BCA12FEF5E29E1422D8DD4D0E6BC38ADCDC445B. Injected/in-process controls, not deployed native acceptance. API producer build passes. Earlier missing ILogger import build failure preserved/corrected. Hosted verified count3 unchanged.

Next concrete gap: HTTP filter operators lack In membership, while native protocol multi-record profiles require one membership AST; OR of point predicates is not equivalent admission. Implement bounded typed In without alternate native reads, then public source presets/browser discovery and live deployment. Keep original local launcher links. Hosted Azure remains pending/free-only.

## HTTP In membership verified —31 October2026

Hosted JSON CQL now compiles In arrays1–100 into one membership AST, sharing the scalar comparison literal boundary. Native key-set caps remain independent (ActivityPub40); point-only/oversized/lossy/mixed/null/nested inputs reject without fan-out.25 new compiler/plan/precision controls +21 existing source/admission controls pass (46/no skips). TestResults/HostedCqlIntegrationCurrent/hosted-membership-canonical-controls.trx SHA256B3BB787CE4BCED9C270661971A4B02F9B3BE38D4798FF4B79A9F0FF04D88DAB4. Initial2 failures were test assumptions about Core's existing canonical UInt64->Decimal/Single->Double AST representation; corrected tests assert both retained path type and exact value, no Core semantics changed. Original failed report preserved.

Bounded read-only SSH from actual dev WEB4 reached mastodon.social v5.0.0-nightly.2026-10-31-security at2026-10-31T06:22:35.994Z, official account13179 and3 public/unlisted status IDs117466985005297414/117466983307949301/117411420916699261. This proves source reachability/discovery, not hosted CQL execution; count remains3. Ready JSON example in Docs/Devs/CQL_HTTP_MEMBERSHIP.md. Next: browser source discovery and native-key presets, configured query-only source/tariff rollout, deployed full/filter query, Mongo pairing/isolation. Azure still pending/free-only; local links retained.

## Generic server examples and browser discovery verified

CQL_HOSTED_QUERY_EXAMPLES_JSON owns bounded public row templates; duplicate/misowned/unknown input rejects and actual native capability plans must admit each example. Scripts/Sandbox/cql-activitypub-public-examples.json contains public3/2 selections. API discovery publishes canonical JSON bodies, no data read or authorization grant. Browser loads only server-discovered sources, uses the fixed WEB4 endpoint and captured session, disables on unavailable/invalid discovery, discards late sign-out/refresh data and retains separate local launch links. No frontend provider case is required for an enrolled source with examples. API60controls pass including25membership/13example-related/source/policy and shipping schema; Node25client controls pass. Evidence TestResults/HostedCqlIntegrationCurrent/hosted-examples-wire-shipping.trx SHA2562EB03927FE9B152C67E5326677E362D7F28AB48F2CEA758CAEC15D0E014E8621; hosted-client-discovery.log SHA2568AC51CA822FE1BF286BE7DB1D6A6CCE550BA9801D957CBB5D922FD28FDBF3373. Controls/in-process proof only; source main d8afd91 remains public until matching API rollout.

Actual dev-node public native batch returned3selected statuses/19371B/274ms, payload SHA256566743c3644452212ebd261afe862e0053e42943c685fb848a0832df877edfa6. Still not a fourth hosted CQL provider. Next: protected configuration preparation and verified consolidated release, then actual CQL/browser full/subset and Mongo pairing/isolation. Inspect shared branch changes first; no unverified changes swept in. Hosted Azure pending/free-only; no new paid source provisioned.

## Protected rollout preparation

Original dev WEB4 variables captured create-only with Windows DPAPI at LocalState/COSMIC/HostedSourceRolloutCurrent/original-variables.dpapi (36902B); candidate-variables.dpapi (34406B) preserves all existing configuration except the explicit ActivityPub origin/key-set enrollment, zero read tariff and public examples. Existing ordinary3, write routes/tariffs/default/routing settings unchanged. No live variable mutation or deployment. Private values never enter docs/TestResults. Task commits parent51010455d and frontend16553be are remote-verified. Next: verify consolidated release scope, apply matching backend/configuration, publish frontend, then prove actual authenticated full/subset queries/browser/Mongo pairing and isolation. Hosted count remains3.

## Scoped release ownership

Broad task237403a75 preserved remotely. Parent work/cosmic-cql-hosted-release uses the same existing worktree and starts at fetched Development51f3c4c3;32 named files select ActivityPub native key sets/configuration, HTTP membership, host registry/example discovery, protected verifier, related tests/docs and matching Core pin6963c50. Other local-only provider adaptations are excluded. Core14-file delta includes existing native key-set/mixed-federation ownership and query-only fences; its targeted controls were verified earlier. Full API assembly before branch selection:415pass/0fail/12environment-dependent skips (not native acceptance), TRX TestResults/HostedCqlIntegrationCurrent/hosted-release-api-suite.trx SHA256D0DCE7B5541223696207320861DFC066A6051D234F04015E4BDAE3743A5DDC14. Eight protected-verifier controls pass. Full transitive producer build is running under its live exec handle; fresh selected consumer tests follow. No deployment/configuration mutation yet.

Scoped release verification complete: full transitive API producer build succeeded (0errors/2537existing warnings); fresh API415pass/0fail/12environment-dependent skips, ActivityPub344pass/no skips, protected verifier8pass. Local demo compile exposed missing explicit demo dependencies and production BootLoader coupling in wildcard-linked CQL code. Included NativeKeySetWalkthrough/ProtocolPointApiHost/native test support and moved the concrete registry into the existing production-only CqlV1Providers composition file, preserving one registration owner and no alternate activation path. Native integration compile now passes; final API415pass after correction.36named parent files total. Final evidence TestResults/HostedCqlIntegrationCurrent/scoped-composition-api.trx SHA2564bb23921ef52556451b7e1e9253188711b5a8ccf51bdd4616814a3f439bae781; scoped-release-activitypub.trx SHA2563f9a9094ea6003da52bf2cecf4713da7731db9e400c367e44eb055240d45daad. Original failed compile evidence preserved. No additional native reads/writes/deployment inferred from these tests.

## Live acceptance exposed and fixed request capture gap

Deployment e71d76cc-daaf-465e-8923-f9b4b19b1589 succeeded at parentfd4c1e033/Core6963c50. Fresh normal free session discovery200 lists3existing plus ActivityPub ready/2examples. First actual AP query400 CQL_REQUEST_INVALID (operation a3e18ac8-9c55-ef15-e30c-9e7c09aa1dcc) proved compiler-only membership checks missed the owning HTTP snapshot, which rejected every array. CqlRequestCapture now freezes bounded1–100 scalar members only for In, rejects nested/null/oversized values, and retains scalar-operator restrictions. Example enrollment traverses the same snapshot boundary before publication.12new controls cover ownership/document disposal/bounds/scalar restrictions/exact shipped presets; full API427pass/0fail/12environment-dependent skips. Evidence TestResults/HostedCqlIntegrationCurrent/membership-capture-api.trx SHA256e3285fa69888817e1bab41b644edb732bc1ffea75f9b99db15000746d855b601. Failure intent/report preserved; no blind retry. Hosted verified count remains3 until actual fixed request/browser/Mongo acceptance. Frontend main remains held.

Correction6735c4d461614be5ce9010d528b26cb870ae8fff is remote-verified on parent Development and task release branch. Matching Railway94623078-50c9-4129-9df9-9ee2e61dd834 is confirmed BUILDING; latest log07:21:29UTC reached dotnet publish. Observe this exact job, no replacement build. Core remains6963c50. Frontend16553be/main d8afd91 still held until actual corrected native read acceptance. Original retained session expired according to JWT exp; fresh rollout session identity/DPAPI are under LocalState/COSMIC/HostedSourceRolloutCurrent, check expiry before using and never impersonate expired owners. First failed public-posts query remains definitive400/pre-dispatch; explicitly corrected verification must retain separate intent/ACK/evidence. Source4ready is not four-provider acceptance; Mongo pairing/isolation remain required.
