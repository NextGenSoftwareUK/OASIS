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

## ActivityPub live HTTP and browser milestone

Correction94623078 SUCCESS at6735c4d46. Actual authenticated HTTP full/subset200 return3/2rows, exact provider ActivityPubOASIS, reported count matches and subset projected identities belong to full selection. Evidence TestResults/HostedCqlIntegrationCurrent/activitypub-hosted-wire-acceptance.json SHA256BE5F7275720087C7E605C661B9705C21E6EB3C99496FA99BC37A56E0518F0D57. Earlier post-fix200 false0 was a verifier envelope error, not native empty data: identical full-response hash8871c9f9e7b11aa86807c6ce707d7cf288e99fbcf846f50958c226c9dd445ae8 reconciles it. Original report/intent kept, separate reconciliation record under protected rollout state; no original query resubmission.

Frontend16553be is main/Production7455754340SUCCESS, public JS SHA2569d90ea5fb1aaefd4afd9d063a06785fe6ca8e1c3f504739deabdd7404b50ae90. Fresh normal browser visitor sandbox-5644a572 sees4available sources, auto-selected public AP example and actual3then2rows, with native status metadata/identities. Local catalogue links remain separate. Screenshots TestResults/HostedCqlIntegrationCurrent/activitypub-sandbox-three-posts.png SHA2564077C7C385FF01D3BD65A7751D9A918DF718D507D1A2EDCC22EC13765C0BB658 and -two-posts.png SHA2566735A67D5B43FB66D13E73B5614D10C9EA24ECE6DEAD0E72BD75614FCE118B92. Owned tab closed; token only page memory, existing free-avatar retirement lifecycle applies.

Reproduced browser gap: X-OASIS-Operation-Id exists in direct HTTP but CORS did not expose it. Actual Startup CORS policy now exposes that one response header while preserving existing origins/methods/request headers and no cookies.3framework evaluation/application/preflight controls pass, browser-operation-header.trx SHA2569c3414b91deb01e9347be38a4d158cb40a3a35368c6997cb2dfa9c67efd46ac7. Code is task-branch only for next backend batch; do not claim live header correction yet. Four providers have hosted query/browser evidence; AP Mongo federation/pairing and broader HyperDrive/full ORM acceptance remain red. Next: named protocol federation admission/unsorted source,2acknowledged owned Mongo copies and ready5/4preset; preserve metadata encryption policy.

## Protocol federation admission implemented, deployment pending

Named/CurrentDefault federation accepts the explicit supported read profiles; AllEligible enumerates the same stable profile list filtered by captured query tariffs, preserving original3order and no silent fourth-source omission. Explicit unordered source permits native membership with per-target authorization/capability planning. Ordinary federation remains bounded8targets/800input rows/8attempts; original defaults remain3attempts. Additional read profiles and unordered sources explicitly lack continuation support, preserving the separate original cursor contract. Query-only storage-write fences remain intact.

New captured-route reproduction exposed caller4attempts overriding a server3ceiling in controlled execution. Test failed with4native reads, retained protocol-federation-budget-reproduction.trx SHA2564B0C69E8F8EB6A3BDA9CC46ED7164C3F37B628752DB6B7239692A529A4472071. UsageExecutionHandle now exposes the captured ceiling; endpoint rejects the complete insufficient set before resolution/reads, without treating it as new permission/reservation. Private Core249a223 pushed/remote-verified and parent manifest/gitlink aligned. Per-attempt durable admission remains its existing owner.

Final32Core controls pass/no skips (12new +20existing):3protocol scope/actor/receipt controls, complete fourth-source eligibility, tariff/capability/request-budget/authorization/descending rejection, server-ceiling denial and foreign-private result refusal from either adapter.61affected API discovery/example/membership/CORS controls pass/no skips. Evidence TestResults/HostedCqlIntegrationCurrent/protocol-federation-visibility.trx SHA2565DD2BE766C8127ED95C68360811F5E926AD9EE50648440DC4047BA4102903B1E; protocol-federation-final-api.trx SHA256B3DCF5025126EC746AB6C37DAB510522BB1678EB90B2F5BE71DB526BFBDF8FAD. Test fixtures use controlled adapters/capabilities, not real native records. Core.UnitTests explicitly links the new host descriptions/examples required by its linked production controller; no alternate runtime path.

No new deployment/configuration/native writes. Live API remains6735c4d46/Core6963c50 and row-only AP3/2 browser acceptance. Next: extend server/client example operation contract, prepare2durably acknowledged owner-scoped Mongo records, and prove ready5/4federation/isolation; bundle with pending CORS fix in next verified deployment batch. Existing protected DNA inspection showed legacy HolonDataEncryption flagsfalse and AdditionalQueryableKeysempty, but effective authenticated V2 metadata policy still needs checking. No security setting changed or encrypted-storage claim made.

## Versioned federation examples implemented, data preparation pending

Server templates now declare Rows(default) or Federation and retain strict request ownership/bounds. Bounded request capture/validation is shared with the actual federation endpoint. Named federation template must contain its owning profile, unique enrolled read targets, PreserveCopies and no continuation/leaf override; no arbitrary URLs/write operations. Native capability planning covers each actual target. Dependency failure disables only that example, not independent public source reads; no replacement query/fan-out. Discovery version1 keeps leaf-only templates for existing clients, explicit examplesVersion=2 adds federation; unsupported versions400. Client selects only two fixed WEB4 endpoints, validates scope, freezes bodies, disables dependency-blocked choices and retains bearer only on WEB4.

77affected APIcontrols pass/no skips (16new relative to61),32federation controls pass/no skips,29client controls pass/no skips (4new). Exact shipped federation-only operational fragment Scripts/Sandbox/cql-activitypub-federation-examples.json is compiler/planner-tested; existing leaf config unchanged. Fragment has NOT been merged into live variables and creates no records. Controlled adapter tests are not native5/4acceptance. Final evidence TestResults/HostedCqlIntegrationCurrent/hosted-example-version-api.trx SHA2563DD8BB8D8133FD6B3B8B091FC9D2C6633E76A67323FFA942F3FF94FF68EF9EC3; hosted-example-version-core.trx SHA256A587E0FFFB96A5C62BE3162B596CD43045D9537849CE3393437A319E174AD87A; hosted-federation-client.log SHA256625C85D63A5AC5152B09F7C1F329D3DC3DF7A645E084A66831048945BA824A32.

Next: use authenticated preparation metadata with existing Core native-record stable identity (avatar/provider/dedicated demo source/key), then normal admitted load/save per record with preserved intent/ACK, privacy and no overwriting foreign/changed records. Browser currently receives only getToken/getBase, so server-derived identities avoid a duplicate client hash algorithm. Effective metadata protection/query policy must be respected. Public live remains4hosted row-query providers/AP3then2, no new deployment. Bundle tested federation, CORS and actual ready5/4pairing in next verified batch.


### Visitor preparation controls — 31 October 2026

Implemented owner-scoped native identities and browser preparation through normal
create-only saves. Eight helper tests and two UI tests cover reuse, edited/foreign
records, lost acknowledgements, storage failure, session changes and concurrent
intents. API86/Core383/client39 pass, zero skipped. Initial test compilation found
a test-namespace Avatar stub collision; fully qualified Core Avatar fixes the test.
Evidence: TestResults/HostedCqlIntegrationCurrent/demo-preparation-api.trx,
demo-preparation-auth-core.trx and demo-preparation-client-final.log.
No hosted preparation/configuration writes occurred. Next: publish the coherent
batch and verify actual5/4ActivityPub+Mongo rows and tenant isolation in-browser.

- demo-preparation-api.trx SHA256 4f34dc55673a3b83b93f4b2050bf74165a6bfa61a6ec0312809a58ee60f7c29e.
- demo-preparation-auth-core.trx SHA256 24c971f16fc12ebba0d359e809550cec184c236383b766f914f46676e18ef732.
- demo-preparation-client-final.log SHA256 d927ab07712a8180a11024792417c675efd75092a9da6c42c7475f083fde2e88.

### Hosted browser federation and preparation acceptance — 31 October 2026

Core249a223 and parent51fc64f5e are fast-forwarded/pushed/remote-verified on
Development; no concurrent changes were overwritten. Railway deployment
14b4e5e7-6646-4e20-b05f-a3025b3819d1 SUCCESS, image
sha256:45f5ff0ab28241eadb622888f96aef01fa11987d4ddbdc71e4f1ddf56f3d8cb6.
Frontend main2b46ded, production7456696386 success. Published client bytes match
both tested scripts exactly. Four configured AP examples are observed, only that
configuration variable changed, protected prior/candidate values and ACK retained.

Normal browser visitor sandbox-60b2bb72 acknowledged2private Mongo saves. Full
federation returns5actual rows (AP3/Mongo2), subset4(AP2/Mongo2). Repeated setup
returns0saved/2existing without new writes. Independent sandbox-46616f4f sees only
3public AP rows; exact foreign Mongo IDs return0. SQLite and Neo4j each return
2matching private replicas (ID/name/description/version/owner/metadata). No native
AP writes, no manual replica repair or business replay. Operation IDs are visible
in the browser, proving the deployed CORS correction. Owned tabs closed; sample
avatars/data remain subject to the normal sandbox expiry/cleanup policy, not
claimed already removed. This is not universal failover/load-balance acceptance.

Raw evidence lives only in TestResults/HostedCqlIntegrationCurrent. Durable
configuration and acceptance ACK live in LocalState/COSMIC/HostedSourceRolloutCurrent.

- browser-federation-acceptance.json (915B), SHA256 feaa8658d7275aa5158230ca1c144b2648550972455731e56e2359a0f89d431a.
- browser-preparation-ack.json (256B), SHA256 b65f8c64c6e1f8f1e4cd51edc31fc9581d818d1512422129a1399e0166fd67c7.
- browser-preparation-repeat.json (148B), SHA256 ca272845d713bdc1d032e4171a9d710a1fb1f003c5952cfd4448b1f88b981832.
- browser-activitypub-mongo-five.json (15026B), SHA256 e86456888d13346f1249ba0a7fd7128907cd3a48fae814128c113729b228ff09.
- browser-activitypub-mongo-four.json (11524B), SHA256 8527341c12fc6d77ae4f71327b6cbd47a7d568dc387b0b83a7123c9e0ce38c6f.
- browser-activitypub-mongo-other-visitor.json (10718B), SHA256 4f5c1f81039c5c58ba9ceffe4d7f3fe9aa9bb1b9e1fd14c3d0cd49b297372f47.
- browser-private-mongo-isolation.json (468B), SHA256 f25ce206d8d8e73e346dec10da030881b2cc2b1d90c3f56e7c12a18cad3bd171.
- browser-preparation-sqlite-replicas.json (2144B), SHA256 7a85da2ebc3f0f10838b29e0fbf0f9f659b51b61e544700797d794e9c596a743.
- browser-preparation-neo4j-replicas.json (2140B), SHA256 f48e587d8891c6154f5850e4639448b435fd318fb7bc394213c3187a0c68d7e5.
- browser-activitypub-mongo-four.png (86492B), SHA256 876e08bafc765ec5a7c76609d62083392d0efcc3939ede2f66b27111aef7fba6.