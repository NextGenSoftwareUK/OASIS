# Hosted sandbox integration — active priority

31 October2026. User clarified that all demonstrated providers must run through
the hosted sandbox, like MongoDB/SQLite/Neo4j. Local launchers are development
evidence, not completion of hosted demo coverage. Pause additional local catalogue
expansion (including Milvus) until hosted integration makes progress.

## Current truth

- Five providers are verified in the public sandbox: MongoDB, SQLite, Neo4j,
  ActivityPub and IPFS. See CURRENT_STATUS.md for current release and evidence.
- Additional native/local demonstrations and public catalogue links do not count
  as hosted coverage. SOLID host integration is next; Azure remains pending,
  free-only. Other providers require real hosted sources and acceptance evidence.
- Provider count, catalogue heading count and local demonstration count are
  separate measures. Never label local-only entries as ready hosted providers.

## Visitor experience required for every provider

- Use the existing live CQL tab: select a ready provider, choose a populated
  example, run it immediately and inspect actual rows and provider provenance.
- Supply a small multi-record source, a filtered subset and one Mongo-paired
  federation preset. Keep sample preparation within the normal visitor flow;
  do not require an SDK, local service or manually supplied credentials.
- Link the corresponding local C# and HTTP demos alongside the live examples.
  Verify links and runnable instructions against the released source revision.
- Show whether a source is hosted, local-only or pending. State bounded membership
  and other query limitations beside examples; do not imply full ORM parity.
- Completion requires the deployed browser-to-native-provider round trip,
  isolation checks and retained evidence, not just a launcher or passing unit test.

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

Next-provider feasibility inspection: protected prior variables contain no Oracle/AWS/SOLID provider entries. IPFS connection is localhost:5001 and a catalogue is configured, but daemon/source availability is unverified. Scoped branch has CID equality; preserved broad branch has the native bounded key-set implementation (50line delta). Production host registry requires ICosmicReadOnlyQueryActivation; do not enroll IPFS by bypassing it or claiming local acceptance as hosted. Next inspect reusable hosted/Linux native resources, then explicitly provision source/configuration and test the live flow. No runtime/service/account was created in this inspection.


### Hosted IPFS source preparation — 31 October 2026

Restored only the preserved bounded CID membership implementation and four
controls; explicit DNA EnableCqlCidKeySets reaches the actual BootLoader provider.
Read-only activation now shares the native health owner with normal activation,
propagates cancellation and performs no catalogue/data mutation. Six new controls
cover native readiness/errors/cancellation. Initial23-case run reproduced one
blank-version readiness defect; fixed at the shared activation owner. Final23pass,
zero skips. Full transitive API build passes after the fix.

One new native source case creates/retains a separate public3-record demo cohort
on the existing Kubo0.43.1 repository/key. Actual CID membership returns3/2matching
IDs/names/CIDs; a second run verifies the same acknowledged source without saves.
Four CAR exports retain the catalogue and selected records for hosted transfer;
raw source data stays LocalState, not docs or TestResults. No historical fixture
was restored/replayed. This does not establish hosted IPFS coverage (still4live
providers).

Runner testing found cross-script process-handle scope loss in its first readiness
failure. Exact owned PID48452 was verified by executable/arguments, then shut down
via native RPC; listener absence confirmed. One explicit shared owner object now
survives the build lease scope, and normal native runs stop the owned daemon in
finally. Use the same explicit non-browser native User-Agent as existing runners.
Original failed readiness/activation evidence preserved. No source save occurred
before that readiness failure.

Hosted SSH inspection: Linux x64/Node24.19.0 and existing volume,4,818,817,024Bfree,
no IPFS runtime or repository at the intended owned paths. Official distribution
checksum metadata timed out; no archive downloaded/installed. Official OCI
ipfs/kubo:v0.43.1 index digest
sha256:b293923d66e490e70ced64df42ea7a6cf7eac2740e3fb29101df18070fa7be48,
linux-amd64 digest
sha256:97ece99cfccea95613ab3634f761d0200e9e8b1f49294b5428e19b5b9056759d,
compressed image layers59,145,329B. Metadata only; no image pulled. Next package
that pinned Linux runtime and own its lifecycle on the existing sandbox volume,
import/verify the retained source with protected key/configuration, then enroll
and prove actual hosted leaf3/2and Mongo5/4/isolation in the existing generic UI.

- ipfs-host-initial.trx SHA256 d299d36723a2a7cdbc98413d557fad57e023141466210731206005bc462b2eb2.
- ipfs-host-final.trx SHA256 215cc87ceef14e700c1b6cf2f7e1bf219faf9b283b0903dd3f9f42fbd0089021.
- ipfs-host-source.trx SHA256 baaa16301d8e6e5e00f88f12e02fab04816069b8da9dfe2a9bda62fc7646d27a.
- ipfs-host-source-summary.json SHA256 9aad480517eae883ae77d6140e61bb789196e35eb1b676d66260dc4e5d1d5a75.
- ipfs-runtime-manifest.json SHA256 5fec6634543b1618b5d4198e3db2141c3a6da59995c7ffe10af70e9049883c10.
- ipfs-host-resources.json SHA256 7971b34f94f39a7d21bd0bfbf8061d6caf6da8f2c85f77841575ea3e0ca21c45.

### Owned hosted IPFS runtime — prepared release

Pinned official multi-platform Kubo0.43.1stage in WEB4 Dockerfile, no Windows image
or archive download. Explicit sandbox entrypoint uses authoritative DNA Sandbox.Enabled,
not the obsolete environment flag (actual service has DNA true/env absent). Bootstrap
captures a bounded canonical hash-checked encrypted CAR bundle, persists intent,
initializes/imports once and refuses interrupted/changed state. Native health binds
version and peer identity; process owner awaits readiness and owns termination of
Kubo/API. RPC loopback5001 only, no gateway/swarm, offline mode disclosed.

Ten Node controls pass/no skips, including real cached-runtime initialization/import,
repeat identity/config/ACK preservation, interrupted-state rejection and explicit
source migration rejection. The sole disposable native probe under
TestResults/HostedIpfsBootstrapCurrent was removed only after assertions and awaited
CLI completion; zero ports/daemon processes. Two new API configuration cases plus
existing public/federation example controls pass (counts in TRX). Native source/key
are unchanged. No remote bootstrap/configuration mutation yet; protected before and
candidate variables stored under LocalState/COSMIC/HostedSourceRolloutCurrent.

- ipfs-runtime-tests-final.log SHA256 f6e457073e69898b983f6f8aa9d0d0611a81352b453feb472349b0589f211ec0.
- ipfs-runtime-native-proof.json SHA256 404f460092f729e04caae8ddbe3b7191a47fb9a6d0ea05fd66d70ac8e6302efc.
- ipfs-hosted-examples.trx SHA256 f45e5eef5567e61939bf7a4749075b66fd11fb37c2c2f1d0b91ef0eb67e5411f.

Hosted4543d364-5b9f-4a04-8b98-8f94a9dc4912 SUCCESS at1b1105ba4, image sha256:317536597c04b52b9dc26a3ee07390202dad115b0c03153fed70655b328663fc; protected six-variable update independently observed/ACK, existing source directory absent before bootstrap. Fresh browser sandbox-1b7b4523 discovers IPFS but valid3-CID query fails503 CQL_INVALID_SOURCE_KEY, operation32200696-507f-91a0-67d4-1bf391fb4618, requestf0e1b7ca-181f-4f6b-afcf-0b332467c3a1. No user save/preparation issued. Shipping APIUnit reproduces actual cause: Ipfs.Core0.55 expects SimpleBase.Base58.Bitcoin field from1.3.1, full API SolSharp4.0.0 selects5.6.4 property. Actual native-only tests had1.3.1 and missed shipping unification; original failing TRX ipfs-shipping-cid-reproduction.trx and browser ipfs-live-initial-failure.json retained. Next rebuild exact upstream core source against unified current dependency, no fallback parser/global downgrade/assembly swapping. Cached package has PDBs but no source; original NuGet nuspec and v0.55.0 tag agree commit07fbb4392cd57a8ba771d2e5c3279a15d5ffbeae. Existing library source not found in current vendored trees.


### Shipping ABI correction — verified before deployment

Vendored only exact67upstream src files +MIT license (306,860B), Git blob hashes
verified at07fbb4392cd57a8ba771d2e5c3279a15d5ffbeae; archive89,333B memory-only,
SHA25627adad4c262cd585b8b61bd51f03f770ebd74fc6ba126b97bcb179065d60b9a3.
No new runtime/environment/clone. Cache-only restores use existing dependencies.
Modern core target binds SimpleBase5.6.4; standard2.0 retains1.3.1target compatibility.
Small Base16 API adaptations preserve canonical encodings via existing HexString
codec/array contract; hex/base32/CIDv0/v1 vectors pass, including independent Kubo
CIDv1 and Python RFC4648 outputs. No codec fallback/assembly rewrite/global downgrade.

First source rebuild still failed shipping test because the caller restored the
old NuGet core through transitive dependencies despite a library-private source
reference. Explicit executable/test SDK composition in Directory.Build.targets
now makes Ipfs.Core0.55a project in the real API graph. Patched file version records
the new private binary; file-version change alone did not fix graph ownership and
its failed evidence is preserved. Same DLL bytes in source/API/test/package.

Full API build passes;28shipping API/schema +27provider/codec controls +1retained
native source verification pass. Provider package169,159B embeds matching core/HTTP
DLLs and upstream license, without old/unpublished SDK NuGet dependencies. Unused
Ipfs.Engine removed (only a commented field referred to it); shared runtime
references own dependencies for source and package. Complete/missing-core Docker
context guards pass. LocalState excluded from local Docker context; it was already
Git-ignored and absent from the remote build clone.

Original deployed source/peer/config/ACK preserved before correction; native owned
volume folder26,189B, image Kubo binary124,384,728B, volume free4,818,628,608B.
Only deployment SDK correction is next; no source re-publication or Mongo saves.

- ipfs-explicit-api.trx SHA256 f617c1e34ffdd5f32f1f24c2eeeebfcb74af485dce36c4e0ef76fb449b91d984.
- ipfs-abi-provider-final.trx SHA256 425fc8e4da2951655fb1e2adb9d0a3d4a6c08530ca969fb3aa22f885f97b17d4.
- ipfs-host-source.trx SHA256 29e457968a08e8fb664ad5d8ad8125f98b3e0bace1477211dfac3679c1bd0420.
- ipfs-abi-artifact-proof.json SHA256 d67abea727760f7d6fdea19e88cbd968c5b98af8dce6ade64a7127b3f134b4c7.
- ipfs-build-input-proof.json SHA256 a6eb706ce34179f30644d414ff037f29f2893a58f3908b2b5797461e70c1ca8a.
- ipfs-server-state-before-sdk-fix.json SHA256 fd819dba1e442a94e97e2beb24a09e020d80df58c68bf8509344db0e448470a9.

### Hosted IPFS acceptance — 31 October 2026

Correction913b9d0bf (SDK patchf60758a1e plus preserved concurrent f0b72a55e
provider-I/O manifest) deployed64bbc646-5d06-412a-a5bd-eb62ca9ae737 SUCCESS,
image sha256:9410328bb615830561ebd001eb4ebb0e6c851ce68dea0f091b75239fb040ef2f.
No configuration resubmission/source re-publication. Public frontend unchanged
2b46ded: generic discovery/operation/preparation contracts needed no provider case.

Fresh normal visitor sandbox-6998fcb9 reads native IPFS3records and2subset, exact
original IDs/CIDs/names. Acknowledged2private Mongo snapshots, full federation5
(IPFS3/Mongo2), subset4(IPFS2/Mongo2). Repeated preparation0saved/2existing.
Independent sandbox-869466e8 sees3public IPFS rows only; exact foreign Mongo IDs0.
SQLite and Neo4j each return2replicas with matching ID/name/description/version/
owner/private flag/metadata. No user IPFS writes or manual replica repair.
Five providers now have actual hosted query evidence; this is bounded CID lookup
on an isolated offline Kubo node, not general IPFS search/public-network resilience
or full ORM/provider parity. Original failed request/SDK reproduction retained.

Owned tabs7/8closed. Tokens stayed in page memory. Demo avatars/private snapshots
remain under normal sandbox expiry/cleanup; retained IPFS source remains product
fixture. Post-correction SSH read-only probe timed out twice on port22, so physical
peer/config/ACK persistence was not independently re-observed; do not claim that
post-redeploy measurement. Before-state hashes and native repeat-bootstrap tests
are retained. Public CQL source IDs/CIDs remain identical and native results prove
actual hosted data access despite the unavailable SSH observation.

- ipfs-live-acceptance.json (961B), SHA256 05179c2e6aa22f02a38f3e6f4708f9e2d6465be66197e8b7f0ae8a6b2700661c.
- ipfs-live-three.json (2091B), SHA256 c6d00c2e9886a2a7e90b55b16284ec6e7460b252ed2729071da8b2b962d91945.
- ipfs-live-two.json (1558B), SHA256 2264eb874a1c4134dc44a65fce1e5cd9db0d27455b807dfcb978f6247909cba8.
- ipfs-live-preparation.json (257B), SHA256 718a85cad8543b4a5512807fdd31296fa4ae2e88e3fa12bbfa41edb5e7be12d9.
- ipfs-live-repeat.json (148B), SHA256 ca272845d713bdc1d032e4171a9d710a1fb1f003c5952cfd4448b1f88b981832.
- ipfs-live-mongo-five.json (9663B), SHA256 07c5a741ef1b6087cfab2b4b9e43b3d2c513d439fa38f499f822cbc4f5ca60fa.
- ipfs-live-mongo-four.json (8179B), SHA256 12ae0ab71f8072fd9691e81798608eb117f5fff372902da7568ddfbf5809ed1d.
- ipfs-live-other-visitor.json (5117B), SHA256 5d7000d9136e470a86f06f53df5a205e5d83e05c5076deb071cc684c09747be7.
- ipfs-live-private-isolation.json (468B), SHA256 f25ce206d8d8e73e346dec10da030881b2cc2b1d90c3f56e7c12a18cad3bd171.
- ipfs-live-sqlite-replicas.json (2263B), SHA256 f2e84970d9cc8a09938e2e8f39fc262e6d71b0d4631fb6a1910f1f47371eaff0.
- ipfs-live-neo4j-replicas.json (2259B), SHA256 ad039b57030fb44d6059c6c98b2e9087d925f46c6055ae0fe06c33f2b2e7d38c.
- ipfs-live-mongo-four.png (87513B), SHA256 2f7dfd93dde494cd66f6e1aee09bcf607d6cd993e82a51a04ca6b0a6ba997658.
- ipfs-live-initial-failure.json (456B), SHA256 8eae5123e0e3bd2afec7fb7def0bccd922a289cc7b21069ae38f82a5a5282f39.

### SOLID host adapter preparation

Reused only preserved native resource-key membership implementation and9controls.
Added explicit DNA EnableCqlRecordKeySets at the actual SolidProviderFactory,
with the existing default point-only behavior unchanged. ICosmicReadOnlyQueryActivation
shares one root health owner with ordinary activation, propagates cancellation,
requests headers only (never buffers an entire Pod listing), rejects errors/redirects
and resets activation state on failure. It creates no container/access rule/record.
Six new controls cover ready/401/503/redirect/pre-cancel/in-flight cancel and assert
only the exact scoped root GET occurs, with response content never consumed.

37combined activation/membership/point controls pass/no skips; full transitive API
producer builds. Not yet hosted/enrolled. Existing Ubuntu/rootful Podman image
58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a inspected
read-only: CSS7.1.8, upstream642983926c79dd9b91d5b2035a8accc6bc2c8bbc,
336,257,823B. No image/container/volume created or started. Existing identity/key
remain protected under LocalState/COSMIC/SolidCurrent. Actual record encryption
context includes resource URI, so hosting needs deliberate source scope preservation
or a separately journalled new owned source, not blind ciphertext relocation.

Cloud metadata inspection only: no local AWS CLI/standard config or credentials
files/access-key/web-identity configuration; protected WEB4 variable snapshot has
no AWS_/OASIS_AWS_ or Oracle variable names. This does not prove no account exists;
it means a hosted connection is not available in the checked configuration. No
cloud account, permission, secret or paid infrastructure was created.

- solid-host-controls.trx SHA256 dfdb1739785aaee009fe4803f687c530a69b48bc67ff879d2f43b8d8fa708962.
- cloud-hosted-readiness-metadata.json SHA256 ad3b249da07bd4fe42c7373b3faaf9ffa565bed678c844fa09cf75e3c5d372a1.
## 31 October — SOLID shipping example gate

Added shipping-API checks using the actual SOLID adapter capabilities and the
production HTTP example compiler. Four presets (3-resource selection, 2-resource
subset, each paired with Mongo) compile only when native membership is enabled.
With Mongo unavailable, native examples remain available and federation examples
carry CQL_DEMO_TARGET_UNAVAILABLE. Discovery planning performs no native I/O.
Two tests passed, zero failures/skips; consumer build passed without restores.
Evidence: TestResults/HostedCqlIntegrationCurrent/solid-shipping-examples.trx,
SHA256 34EF67E9899295BF8BDC9AE059C88C863129F0637BD75C7642B6269A18E27C5D.
These tests do not establish a hosted Pod or live six-provider coverage.

The retained-container read-only inspection command was rejected by automatic
approval review before execution, with no detailed reason. No container started,
no readiness report was produced and no Pod/identity mutation ran. Existing
retained CSS resources remain reusable; this is not a provider failure result.

Upstream pinned-package configuration documentation distinguishes ordinary
file.json (no root writes) from file-root.json (root LDP storage, initially broad
permissions). Hosted provisioning must explicitly choose and restrict the owned
loopback-only source, rather than assuming a default file.json root accepts seed
writes. Reference: https://app.unpkg.com/%40solid/community-server%407.1.8/files/config/README.md.
Next: package pinned CSS with deliberate lifecycle ownership, persist the native
source on the existing volume, provision three public encrypted sample records
for its exact URI, then enroll and verify public browser queries/federation.
## 31 October — SOLID runtime packaging and retained encrypted source

Implemented an opt-in CSS7.1.8 native runtime in the existing WEB4 image, pinned to
the retained image digest125727897fde6e2225239a0748bf47d8fa559cac065d5694dc10f6428799e9f2.
No image was pulled/built locally and this batch is not deployed. CSS source was
inspected read-only from the stopped cached image, with tar streams held only in
memory. Its supported StaticFolderGenerator/root initializer creates the native
source from a custom template. The template grants public Read only from initial
creation; it never starts with the upstream unrestricted root ACL. CSS itself
binds its configured port on container interfaces; the base URI is not a binding
restriction. Only WEB4's configured public HTTP port should be exposed by hosting.

Native setup persists intent before initialization, refuses unacknowledged state
and changed bundles, and acknowledges only after native records and read-only
permissions verify. Native RDF comparison uses CSS's shipped N3 parser rather
than text formatting equality. IPFS and CSS share one process owner: API starts
after both sources verify; either source exit stops the API and other native
child; later startup work is cancelled and observed if an earlier source exits.
Native children receive a minimal environment without WEB4 credentials or keys.
The existing IPFS enabled configuration continues to select its original runtime;
SOLID remains off until explicitly configured.

Runtime checks14passed/0failed/0skipped, including existing real offline Kubo import
and lifecycle checks because the shared process owner changed. SOLID boundary
checks use controlled HTTP/parser/package inputs and are not a native CSS result.
Evidence TestResults/HostedCqlIntegrationCurrent/solid-runtime-controls.log,
SHA256 E6E9406AF1CFCA7905C74F0EE2686847851F21DF5B8597907C10F5DB520DCC7A.
Node syntax and entrypoint shell syntax checks passed.

Prepare-HostedSolidSource.ps1 now exports exactly3new public synthetic leaf records
using the retained original protected encryption key, for http://localhost:13000/.
The old private Pod/data are never read or mutated. Export intent precedes encoding;
ACK follows decode/hash/owner/URI checks. A different Pod URI rejects each envelope.
Export and repeat each passed1case with no skips. Source files total13,817B at
LocalState/COSMIC/HostedSolidSourceCurrent; raw bundle never committed to docs.
Bundle SHA256 CD2BB473EE916256582BE7A4947D675D594AA2EB302DB000312558BEB7DD9EDA;
canonical runtime digest345a047e2c35e600ae629f56c661ac8a5fd1418af3cf823b307c7c118769237a.
Repeat preserved bundle, export intent and ACK bytes. Evidence
TestResults/HostedCqlIntegrationCurrent/solid-host-export-proof.json and
solid-host-export.trx (repeat SHA25666A6CD43D7307B88569618305B230CB35322A5020C47E30B8CA884DBFAE84697).

The first integration build reproduced an existing composition defect: wildcard
linked API services included production-only DnaCqlSharedReads, requiring the full
BootLoader and CqlV1Providers deliberately absent from the loopback host. Excluded
that production composition alongside CqlV1Providers in this integration project;
then consumer build/export passed. No substitute BootLoader or fallback added.

Still required before live enrollment: native CSS startup/initialization against
the retained export, actual N3 response and WAC permission checks, same-volume
restart, memory/headroom observation, host config/admission/example publication,
and public visitor query/federation/isolation/replication acceptance. Five hosted
providers remain the current verified coverage.
## 31 October — native SOLID initialization and restart verified

A safer probe reused the exact cached CSS image with network=none, no published
ports and no key/credential mounts. Only the new public encrypted bundle, runtime
scripts and a separate owned probe-data directory were mounted; the original
private Pod/container/volume were never started or altered.

The first native run caught a real configuration error: directly adding
RootFolderGenerator.templateFolder imports two scalar values. Components.js
rejected it before app initialization. The probe container was terminal/removed
and its native data directory absent. Explicit reconciliation preserved its
13KB setup files under initial-config-failure before a corrected new probe.
Fixed the owning config with supported Override/overrideParameters semantics:
https://componentsjs.readthedocs.io/en/latest/configuration/configurations/overrides/.
No upstream patch, duplicate configuration path or blind failed seed replay.

Corrected native startup passed: CSS7.1.8 served3actual encrypted RDF envelopes,
compared through its real shipped N3 parser, and root WAC-Allow reported Read only.
A second isolated process against the same data passed with fresh=false and
unchanged bootstrap ACK. Native CSS image uses Node18.20.8; the final WEB4 image
uses Node24.19.0 and still needs its own build/runtime compatibility verification.
No public sandbox acceptance or full final-image proof is claimed here.

Native evidence TestResults/HostedCqlIntegrationCurrent/solid-native-runtime-proof.json,
solid-native-runtime-{first,corrected,repeat}.log. First failed log SHA256
3E131FA2013E005864F707F6BDB12F558FE74B8278F1F788AB1E855388F768E2;
corrected B7C45FF4BFDC918F35CE0CBD2E3A6037653C49AED176076E503915AD3B4A2E3B;
repeat 7D2A02BB46D5D4C1AE3B4B2801DED50A146B046EB8300B33F433ABA1C2415464.
ACK SHA256 B4025B3F96A62AC634316B5FCBE8EABBDE0B8978B3EA2160A0107127B7A6ECB5.
Compact proof also retained at LocalState/COSMIC/HostedSolidSourceCurrent/native-runtime-proof.json.

After the config fix14runtime controls pass/no skips again; latest log SHA256
BEDF95E7E20A27B91283EEE240A3E71F7F1EED307645B58D1A6A4E5433EAF88C.
Probe container removal is verified, original oasis-solid remains exited. The
31,405B disposable NativeProofCurrent directory is retained: automatic approval
review rejected the validated cleanup command before execution, with no specific
reason. No alternate deletion attempted. This supersedes the planned cleanup in
the prior resource entry; it is not an active service or large disk allocation.

Task branch has merged the latest Development CI changes085f6b705/4a5450012;
manifest/gitlink validation passes. These preparation changes remain off deployed
Development until final-image/configuration acceptance is ready.
## 31 October — hosted SOLID configuration acknowledged and release prepared

Existing service capacity observed without a plan/service change:580.294541312MB
current memory,686.657896448MB maximum1h,8,192MB configured limit; /data volume
136.809807872MB of5,000MB. Report solid-host-capacity-before.json under the fixed
TestResults/HostedCqlIntegrationCurrent directory. This is observed headroom, not
an ongoing capacity guarantee.

Prepared/protected and then applied exactly six SOLID configuration values with
skip-deploys. Every value had a durable intent before the CLI request and a separate
full-map observation before ACK. OASIS_DNA_JSON adds only the deliberate new
anonymous/public Pod scope and membership capability; admission adds query-only
SOLID/explicit tariff; four examples cover3/2native rows and Mongo federation.
The original encryption key is reused via protected configuration, never printed.
Default writer/failover/replication settings and existing provider entries remain
unchanged. Script: Scripts/Sandbox/Publish-HostedSolidConfiguration.py; snapshots,
intents and ACKs retained under LocalState/COSMIC/HostedSourceRolloutCurrent.
No configuration action itself triggered a deployment.

Retained exact native bootstrap ACK alongside the public source export at
HostedSolidSourceCurrent/native-bootstrap.acknowledged.json. Its hash and digest
bind the native proof to the candidate bundle, not merely to readiness booleans.
Confirmed candidate digest345a047e2c35e600ae629f56c661ac8a5fd1418af3cf823b307c7c118769237a.
A stale/different source fails preparation; existing intents prevent apply replay.

Full transitive WEB4 API build passed with0errors (existing2,566warnings), no
restore, bounded workers/lease; log solid-release-api-build.log. Added spaced,
commented SolidPointWalkthrough.RunRecordSetAsync for C#3/2selection through
HolonManager/planner/actual adapter. Export control compiles and executes the
published example over real retained encrypted envelopes using controlled GET
transport, exactly5selected resource reads. Native CSS/N3 proof remains separate;
this C# control is not a hosted network claim. Export control1passed/no skips;
source ciphertext/identity unchanged. Added maintained SOLID_CQL_HOST_CONFIGURATION
and linked C# and HTTP guidance from demo README/manual.

Waited for the already-running shared deployment0196bd82-cf5a-4bf6-8c06-68c264a20db9
at20a981f8d to reach SUCCESS before preparing the next deployment batch. That
concurrent commit adds solution IpfsCore entries and updates route manifest;
preserve it when integrating. Public SOLID/browser verification still pending.
## 31 October — release published, existing Railway queue under observation

SOLID batchd41d4ec8383ca27b9d079cb1d17178b1b80ee1f9 is merged/pushed to
Development and task branch, independently remote-observed. Release intent/ACK
retained under HostedSourceRolloutCurrent. Subsequent shared CI-only9962c4e46
preserves that commit (merge-base verified); it changes only the operation-contract
check script. Do not undo or redeploy around concurrent CI changes.

Specific queued jobs, re-observed after a bounded45second wait:
73a8848d-04e7-464d-b371-3d3702c2d91a atd41d4ec83 and
2fcd7075-ae8c-44ac-81cf-c0558429746d at9962c4e46. Both QUEUED, neither terminal;
no cancellation/restart/resubmission sent. Previous live0196bd82-cf5a-4bf6-8c06-68c264a20db9
succeeded at20a981f8d. These are live deployment handles, not an inferred blocker.
Next action is poll these same jobs and inspect final-image results before browser
acceptance. Evidence solid-release-deployments.json in fixed TestResults folder.

Frontend main/task1f7335121fbb1340439165d699e31962aff1119c pushed and Production
7458236043 observed success. CQL tab now links directly to released SOLID C# and
HTTP/host examples and general CQL README. Corrected a reproducible local-doc gap:
Run-ReusableProtocolMongoTests.ps1 is absent from scoped Development but exists at
preserved237403a75b6983e013141cf7ba44bf66858abafd. Local launcher text links that
exact source revision and distinguishes required local services from the ready
hosted playground; it no longer instructs users to run the missing scoped script.

Public https://web4.oasisomniverse.one/web4-sandbox.html contains all three links
and exactly matches its committed Git blob SHA256
920bd14e23406cbac5e1a7af8c5d17ffab28caa1e80a4a1ae5ca23872982deb6.
Initial comparison with Windows worktree bytes differed due checkout line endings;
comparison with the actual committed blob passed. Frontend39controls pass/no skips;
TestResults/CqlCurrent/solid-links-public-proof.json and solid-links-client-tests.log
in C:/Source/Web4Site. SOLID is not counted as verified hosted coverage yet.
## 31 October — SOLID deployed; approved public AWS source implemented

Railway2fcd7075-ae8c-44ac-81cf-c0558429746d reached SUCCESS at9962c4e46,
which preserves the scoped SOLID batch.73a8848d-04e7-464d-b371-3d3702c2d91a was
removed/superseded, not failed/restarted. Native runtime and API started, but the
actual SOLID HTTP/browser query/federation/isolation gates remain outstanding.
Browser tool cua_repl failed Transport closed. Existing @oai/sky was reused;
Edge launched at its verified existing path, returned window327700/New tab.
State capture then stopped Computer Use because it could not confidently identify
the current URL. No navigation/session/data mutation occurred. No bypass attempted;
Edge window remains for user navigation. User asked to open the sandbox URL while
independent API/source checks continue. Do not infer a successful browser test.

User explicitly selected a public S3 dataset for the AWS demo, distinct from owned
AWS persistence. Selected3NOAA GOES-16 objects from a bounded native ListObjectsV2
preflight; anonymous HEAD200each, no object-body downloads. Source documentation:
https://registry.opendata.aws/noaa-goes/. Retained keys/region/bucket/stable source
owner at LocalState/COSMIC/HostedAwsS3PublicSourceCurrent/source.json. Public
preflight report TestResults/HostedCqlIntegrationCurrent/aws-s3-public-preflight.json.

Implemented separate AwsS3PublicMetadataSource over the existing shared storage
boundary, with Core-native mapping and official AWSSDK.S3 3.7.502. Explicit DNA
PublicObjectMetadataOnly selects this anonymous read-only source; the existing
private encrypted S3 provider remains its own mode. No fake storage key, default
credential inference, object body GET, native bucket scan during queries, automatic
retry, account creation or new service. Activation HEADs one configured object;
point/membership selects at most8known keys of128characters, validates all selected
metadata before paging, handles cancellation/404, and denies unknown keys before
I/O. Backend mutations explicitly reject; host admission must remain query-only.
The profile is included in the CQL HTTP compiler only, not admitted by configuration.

Verification:11controlled SDK tests pass/no skips; native anonymous SDK test1passes,
reading3objects and2subset through actual adapter/planner and live AWS (not emulator).
Full transitive API build0errors/existing2,040warnings, no restore; shipping public
S3 preset/dependency test plus GraphQL shipping schema2pass/no skips. Four presets
compile against actual adapter capabilities; missing Mongo disables only paired
examples. Raw evidence under TestResults/HostedCqlIntegrationCurrent:
aws-s3-public-controls.trx SHA2568A70985B86C093A8C9FF20578FAE315A1A00C9507FE842450F466B7A38ACEEBC;
aws-s3-public-native.trx SHA256B0ADF714AD4533AA3B2D8E71ABC47C074E784402A5C093EBC988EFC51C06BB72;
aws-s3-public-shipping.trx SHA256F35DF5C0FCEEFD6E0E2C446AC65DFEA8D5618AD8F3735E386E1DA6336D3E7BD2.
Native AWS is not yet configured/deployed to the sandbox; no increase in hosted count.

OracleCql.cs/OracleCqlCompiler.cs/OracleCqlTests.cs were restored unchanged from
preserved237403a75b6983e013141cf7ba44bf66858abafd into the scoped task branch.
Producer/consumer builds pass;13compiler/authority/binding/unsupported-shape tests
pass/no skips. Evidence oracle-scoped-cql.trx SHA256
C5445FDDE7F06EB0139F22FDB963CA3D9DB62609CE5F8551404523255FF6A4C5.
No Oracle connection/schema/data/process operation ran. This adapter requires a
complete server-authorized Oracle catalogue; read-only host activation, host access
policy and real hosted connectivity remain pending. Do not enroll it prematurely.
## 31 October — SOLID API proof, verifier correction and demo-budget root cause

Normal visitor sandbox-2ab66e15 /97668a7f-0189-4cf4-a21f-aba6c8a5d0ce
read SOLID3native rows and2subset through deployed HTTP/discovery. Proof
TestResults/HostedCqlIntegrationCurrent/SOLIDOASIS-native-http.json. This raises
native hosted HTTP breadth to6; browser-verified breadth remains5. User opened
Edge, but fresh native window inventory exposed no Edge/browser target; no unrelated
window was controlled. Browser acceptance still outstanding, not inferred from HTTP.

A new generic HTTP verifier executes the published frontend preparation module.
Its first caller missed the standard data-endpoint result wrapper, causing an
incomplete-ACK assertion after a real save. The web app already unwraps correctly;
fixed only the verifier caller by reusing exported CqlPlayground.unwrap. No product
save fallback, provider patch, manual native write or replay was introduced.

Original request5b69c436-34f2-42f6-8be8-dd37529a2199 has no retained raw ACK;
record39e5961a-d295-8ee0-b3a1-22378b4f555b independently observed private/V1,
owner97668a7f..., matching exact source name/description and mapping metadata.
Archived identifiers-only client intent and materialized-state reconciliation under
HostedQueryAcceptance-SOLIDOASIS. This explicitly does not assert the missing
operation acknowledgement. Original operation never resubmitted; subsequent setup
observed that record and created only the missing second snapshot.
Second requestfe8ce7f4-6310-4d7c-83ac-188141a55969 ACKed record
ac4d653b-7af1-838a-b8e2-408c3c8ddeab/V1/private/owner97668a7f...,
server operation41b50624-c8b4-5bed-d73b-77ae4d382dd1. Thin ACK persisted before
client intent removal. Full flow then stopped on HTTP429 during repeated reads;
no save retry. Raw intermediate federation results were not persisted by the first
verifier version, so no complete federation/replica acceptance is claimed from it.

Authoritative usage/current confirmed20dailyCalls/20limit/0remaining, planfree,
monthlySpend0. JWT expiry12:48:44Z separately confirmed; old cohort is preserved,
not automatically reauthenticated/replaced. Generic verifier now records progress,
uses durable identifiers-only save intents, retains thin ACKs, rejects expired
sessions and supports only an explicit fixed-client replacement test cohort.
Replication observation is bounded3reads/target within5seconds; no repeated writes.

User approved200daily calls exclusively for temporary demo avatars. Implemented
SandboxSubscriptionRules as the single owner of existing issued-free classification
(reused by issuance/cleanup) plus active lifetime eligibility. Authoritative Mongo
reservation and usage summary resolve the same200-call policy; normal plans remain
unchanged. Monthly request/token/money caps, subscription lifetime, usage counters,
pay-as-you-go and captured prior operations are untouched. No quota reset or tier
upgrade. Admission captures one time for expiry and policy evaluation. Summary/
authorization use injected TimeProvider; defaults remain system time.

Final API build0errors;63targeted allowance/lifecycle/ordinary-policy/S3/schema tests
pass/no skips. Evidence sandbox-allowance.trx SHA256
1FE44A8D58B26EB7AC5ADA07AEA60DC3B2895C1B31B7823D6FA3E6922F0D4C3E.
Shared operator mutation owner3in-memory tests prove observed success, unknown
outcome replay refusal and concurrent-change preservation; log SHA256
383A12FBA9B5B327054A24CE7E5739569C4F0B6621FACD981F2D6A4271063B3C.
S3 publisher reuses that owner, not a parallel environment update implementation.

Public S3 candidate review proved only3values changed; other DNA/routing, admission
and provider examples preserved. Anonymous metadata mode/3objects/four presets;
query tariff0USD/catalogue sandbox-demo-free-v2. Applied all3with skip-deploys and
independent full-map observations/ACKs. No source account, bucket, native object,
signing credential or new paid service created. Await one combined code release
before testing new demo allowance and AWS live. Concurrent Development073edc99f
CI/docs/AGENTS release-snapshot updates must be preserved; no master promotion planned.

## 31 October: S3 startup admission regression

Railway833256b2-5008-47b7-9662-c4d60452e2e7 atd300e8254 reported SUCCESS, but runtime exited before API startup: StorageQueryTariff.Capture omitted AWSS3 and allowed at most7tariffs. Actual SOLID fixed-client session POST returned502; only owner.intent.json exists, no bearer/ACK. Preserve unknown creation intent; never replay. API readiness must be observed separately from platform deployment status.

Core bf098a4fbaf8ec577459af1a53fa16bdc59c45ad explicitly adds S3, raises supported catalogue bound to8 and samples9to detect overflow. Permission, uniqueness, price/version checks retained; no generic provider enrollment or writable routes. Shipping test builds full8-provider PricedStorageAdmissionFactory and verifies absent permission/overflow/negative-price rejection. Full API and test consumer builds pass;30tests/no skips cover admission, discovery, demo allowance and GraphQL shipping schema. TestResults/HostedCqlIntegrationCurrent/s3-admission.trx SHA2566F8618D8B3E8102FDC14F121908CFF13ABAEBF43E1D0C4348AF54DAE3D9EC9EB. Core branch/Development pushes independently verified with ls-remote. Next parent pin rollout, then explicitly planned post-startup-fix SOLID cohort; original fixed-client and expired cohorts remain preserved.


## 31 October: SOLID and public S3 hosted HTTP acceptance passed

Development b3644ed3173e63769e130a6325d57b7ed5def31a / Core bf098a4, deployment5df7cbfd-c47f-4031-9c1e-9273d86e2a5c SUCCESS. Image sha256:b9b436b322bcb775701ade836d38951cb86d835bcde3fe2962978c3f0f00ceed. Unauthenticated discovery401 plus actual authenticated sources establish API readiness beyond platform status. Both sources: native3/2, preparation2saved, federation5/4, repeat0saved/2existing, exact SQLite/Neo4j2replicas each, independent visitor3public/0foreign-private IDs. Published client preparation/unwrap modules used over real HTTP; browserAcceptance=false. No browser UI proof inferred. No source object/Pod writes occurred. Both usage summaries show14daily calls/200limit/186remaining,0tokens/50klimit,0spend/$1cap. Normal Free20 remains unit-tested, not a new live ordinary-account claim.

- `TestResults/HostedCqlIntegrationCurrent/SOLIDOASIS-post-startup-fix-native-http.json` SHA256 `c9ea78606fd40f770d79e581da132d30babe692ef3aa0b220377ebc908ace80b`.
- `TestResults/HostedCqlIntegrationCurrent/SOLIDOASIS-post-startup-fix-client-http-flow.json` SHA256 `c42fe7cad8ba88294f701c2d74df0c5a3c25c1f0e71f7d79f467239f788d5835`.
- `TestResults/HostedCqlIntegrationCurrent/SOLIDOASIS-post-startup-fix-isolation-http.json` SHA256 `4dafb6ffe99ff4f768c7427caf5bebb129720f41a05b2909470c02ea72a3425a`.
- `TestResults/HostedCqlIntegrationCurrent/SOLIDOASIS-post-startup-fix-usage-http.json` SHA256 `ae40c9830a733599cdf1a7fe4150a351e09f1cb09e8806ceaa7e8d3ad2ef4600`.
- `TestResults/HostedCqlIntegrationCurrent/AWSS3OASIS-fixed-client-native-http.json` SHA256 `ba6d9c05d7b7d9fab985471a24d1c936bed7ab709ef9dfdc30fc4599a814a02a`.
- `TestResults/HostedCqlIntegrationCurrent/AWSS3OASIS-fixed-client-client-http-flow.json` SHA256 `d869d807892d505bb758bd27dc01d6aaf747f5a8e4d3296efdf1df0e62282eb8`.
- `TestResults/HostedCqlIntegrationCurrent/AWSS3OASIS-fixed-client-isolation-http.json` SHA256 `6bcb2e03e5e694308d9c4357e78b8e68f97962a94facdf121a6acb8c4e19f8f1`.
- `TestResults/HostedCqlIntegrationCurrent/AWSS3OASIS-fixed-client-usage-http.json` SHA256 `093d0448b31a0a93655340d5efefc6027716414badcd700d500d7f318e2afb2f`.

Next browser SOLID/S3 acceptance when control surface is available, then Oracle native hosted activation/access catalogue and Solana view; Azure explicitly pending/free-only. Deep parity remains paused. Old unknown502 SOLID fixed-client intent preserved, no replay.


## 31 October: Oracle read-only activation contract

Oracle now implements ICosmicReadOnlyQueryActivation separately from explicit storage schema setup. It reuses existing canonical schema validation (read-only USER_* queries), propagates cancellation to SDK open/read operations, never calls provisioning and clears stale activation on error/cancellation. No host enrollment or record authorization change. Provider/unit/integration builds use existing assets/no restore and pass.15controls (13Oracle CQL +2activation) pass; retained native source activation passes1test, provisioning override observed0calls, before/after schema identities/DDL times and4original record hashes identical. Oracle original container e8861e296e101147681c4e2f0a3115e870cafffbce5f92848eb75d8cf25d0982 observed exited after runner-owned start/stop.

Evidence: TestResults/HostedCqlIntegrationCurrent/oracle-query-activation-controls.trx SHA2567BFAE76807A4066658C15B887822BB86FF4DAEFD571AD8B4F92121E9AA37AF43; oracle-query-activation-native.trx SHA2565F4FE9E3DB8D12048CC0D416A7C02BADB9D42F59E3E2B87BC8A74430E42A6654. Reusable source runner Scripts/TestHosts/Test-OracleQueryActivation.ps1; no new database/user/records/schema objects. Next actual server-authorized Oracle read policy/query integration and hosted connection; no public readiness claim.


## 31 October: Oracle provider-scoped authorization and discovery

Task-only Core e681b8d5c37beda31f33b599bc8e2cc7f86e046a explicitly admits Oracle read tariffs as the ninth host catalogue entry (request fan-out limits unchanged). Root host adds Oracle row profile and unordered query capture; complete immutable host-public catalogues are registered as provider-keyed ICqlReadAccessPolicy. Global application policy retains precedence; errors/null factories/rejections never fall through to another grant. Unenrolled providers retain owner/public, resolved once per request. Federation keeps Oracle catalogue and Mongo actor visibility separate. Host configuration is an explicit full-record grant to every authenticated caller, not inference from request IDs or native public flags.

Discovery uses the same captured public catalogue instance to plan native and paired presets; missing authority keeps unsupported Oracle examples unavailable. No fake-actor callback to arbitrary application authorizers and no extra native reads during planning. Full API/Core consumer/API consumer builds pass without restore;401endpoint/policy controls and21shipping API/discovery/native-compiler controls pass/no skips. Raw oracle-read-catalogue-core.trx SHA25652CF5414C0F2EF33FB85FE85BBBABD15E53647F686756C8220157C23342390A1; oracle-read-catalogue-api.trx SHA256DCAAA48D94F87FDB8EF07FEC720D4F69485506691A12F5AF2E28D02E35808264, both under TestResults/HostedCqlIntegrationCurrent. These are planning/authorization/SQL compiler controls, not new native hosted execution proof.

Next concrete gap: cql-demo-preparation.js accepts metadata-key membership; Oracle uses native Id membership. Add explicit ID-family preparation using Core owner-mapped snapshot IDs and compose source/native plus mapped IDs for paired selection; preserve existing protocol family behavior and per-avatar visibility. Then prove actual native Oracle HTTP leaf3/2, preparation2, paired5/4, reuse/isolation/replicas. Existing source4rows/schema must remain unchanged. No new cloud database/credentials/routing/host variable mutation. OCI executable, C:/Users/david/.oci/config, C:/Users/david/.oracle and Oracle/OCI environment variable names were absent in inspected local state; asks paths only for an existing hosted configuration, while independent demo integration continues. Guide Docs/Devs/ORACLE_CQL_HOST_CONFIGURATION.md describes explicit grant and limits.


## 31 October: Native Oracle/Mongo HTTP read gate and ID compositor

One explicit native case passes/no skips against retained Oracle11521 and Mongo27029. Shipping CqlV1Endpoint capture/planning/execution over loopback HTTP returns Oracle3/2 and paired5/4; second actor gets0private Mongo copies and3configured Oracle grant rows; fourth Oracle record outside catalogue returns0. Two private Core-mapped records were seeded via V2 HolonManager conditional SDK saves (create-only, stable journalled operation IDs), version1/owner checked.9physical read receipts observed in controlled in-memory usage admission. This is not production signup/billing, native JS REST preparation, replication or browser proof. Original4Oracle records/modified timestamps unchanged: source hash68A2F56066F2683CB7FD060AFEC927167B0FE24D9CBF30598C56C876CC3DCF04.

Owned fixture cosmic_cql_oracle_host_id_demo removed only after successful queries, HTTP stop, exact marker/2holon checks and observed DBabsence. Persistent intent/2save ACKs/cleanedACK retained under LocalState/COSMIC/OracleHostedIdHttpCurrent (870B). Original native sources/credentials untouched. Both owned started services stopped. Native report TestResults/HostedCqlIntegrationCurrent/oracle-mongo-id-native.trx SHA256A06D660D4871C673CC46760F0A4AE80806F03139B7202D7AE21F09FC550CC68C; compact native summary SHA2564A71431B3D9213A71193D35A9EA621B6F760A4F84C297BE59C0BD63CE60991FE. Final test consumer build passes without restore.

Frontend7c0f71e shares composeIdPair between preparation and verifier; publishes actor-specific ID union only after acknowledged/verified copies.45client controls pass;2memory-only verifier controls exercise actual client modules, full/subset IDs, zero-repeat writes, replica comparisons and unchanged protocol request order. They are controls, not native provider certification. oracle-id-verifier-controls.log SHA256755AA9E6B72687C50361E4815110FFCBCA603F708B93F4280DAC8195A2493611. All raw evidence stays in worktree TestResults. Browser tab retry still Transport closed, no new user action requested.

Hosted Oracle/Azure remain explicitly deferred by David; see central ACTIONS-FOR-DAVID Part E. Continue available public demo breadth, with Solana source feasibility next and original chain history preserved. Full ORM/HyperDrive fault matrix/EF parity remains incomplete.


## Hosted avatar-scoped read failover — 31 October 2026

Shipping Development498e3fd7f/Core759b49a, Railway4dbe69d0 SUCCESS. Reused acknowledged Solana-demo snapshots and existing owner/foreign sessions; no new accounts/saves/settings changes. Runner: `Scripts/Sandbox/Verify-HostedHyperdriveFailover.py`.

All3cases (MongoDB/SQLite/Neo4j) passed: exact native replica baseline, avatar-only30second fault, failover-OFF `HYPERDRIVE_PROVIDER_OFFLINE`, failover-ON correct ID/name/version/owner, two independently observed refusal increments, unaffected foreign fault scope and zero foreign private rows. Every fault was restored in finally and absence independently read back. Durable fault/session journals remain under `LocalState/COSMIC/HostedQueryAcceptance-SolanaOASIS`; rerunning a completed or uncertain gate is forbidden. Public/browser rendering and full routing behavior are not inferred from these HTTP checks.

Regenerable report `TestResults/HostedCqlIntegrationCurrent/hyperdrive-failover-http.json`, SHA256 `8EA18987E76DFFF2B36DD6A05AB4D1E6033090B9FB80288F69441AD07204EB53`. X10 remains red: automatic load balancing, write faults and late-enrollment/reconciliation need their own acceptance. Existing successful query/replication scopes stay frozen absent a reproduced bug.


## Per-request routing evidence — next release batch

A live default-provider read with auto-load-balancing ON succeeded, but its result had empty route metadata. Shared `hyperdrive/status.lastProviderSelection` cannot prove that read's provider under concurrent visitors. The owner usage projection did not contain a corresponding load receipt either. This is an observability gap; it does not establish a selection-policy failure.

Core task `6bbd015c5b35f6b401d289676576ccd781984ab9` now attaches `metaData.hyperDriveRoute` JSON using operation-local values: `RequestedProvider`, `InitialProvider`, `ServingProvider`, `RequestedStrategy`, `Succeeded`. Failed/missing results do not claim a serving provider. Sync and async paths use the same metadata owner. No algorithm, candidate list, signing permission or canonical provider ownership changes.

552 HyperDrive routing/selection regression cases pass, including four new cases: sync/async explicit vs automatic reads, failed result ownership, and a held overlapping read that retains Mongo's identity after another operation changes shared selection to SQLite. Regenerable `TestResults/HostedCqlIntegrationCurrent/hyperdrive-route-regressions.trx`, SHA256 `38A8A4573CA0391019AD6E48221DCB02E3EFBCB63179B7C86FD7BA726056B9E9`.

Frontend task `ded5ab0` renders only the load response's `hyperDriveRoute` and existing `hyperDriveFailoverDiagnostic`; it no longer fetches shared status to label that request.47client tests pass, including response/missing-evidence controls. This batch is pushed on task branches, not yet deployed. Live automatic-balancing/provider-choice acceptance remains red until the response-owned facts are available on the host. No additional Railway deployment was triggered in this step.


## Load-balancing verifier and partial write-fault gate

`Scripts/Sandbox/Verify-HostedHyperdriveLoadBalancing.py` now requires each response's captured route, verifies three explicit reads plus six automatic reads of the acknowledged snapshot, rejects unenrolled/foreign/missing/shared evidence, and records observed provider variation. It makes no saves/faults/settings changes. Live execution waits for the next response-evidence release; it has not passed on the current deployed version.

`Scripts/Sandbox/Verify-HostedHyperdriveWriteFault.py` journals unique negative-test IDs/operation keys before one create-only save per faulted authority with failover/load-balancing/replication ON. It restores owned faults in finally; uncertain sends never retry. Safe acknowledged rejections can be explicitly reviewed/resumed, not automatically replayed. Four pure verifier controls pass (including rejection subcases and exactly one transport send with the original operation key).

The first live Mongo case returned503 `COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED`, IsError=true/IsSaved=false/nullresult. This is the mandatory ownership read failing before conditional write routing, not a conditional-commit proof. Its fault restoration is acknowledged. The original test expected the later offline routing code; that expectation was corrected to distinguish both legitimate stages. The retained save was not resent. The owner session then expired (verified JWT expiry plus401), preventing remaining SQLite/Neo cases and all-store absence reconciliation. No storage absence, alternate-store non-commit or uncertain-commit recovery acceptance is claimed. All these scopes remain red.

Original cohort intents/responses/restoration remain under `LocalState/COSMIC/HostedQueryAcceptance-SolanaOASIS`. Partial report `TestResults/HostedCqlIntegrationCurrent/hyperdrive-write-fault-http.json`, SHA256 `9A2CDA778FF3A175828519F9461C0DE8A66F7A873FED740DF9E50BED9221C1EA`. Further testing needs an explicitly journalled fresh negative-test cohort and appropriate native reconciliation of the original IDs; do not replace the original session or replay its save. This is agent-owned test work, not a new action for David.


## Offline write-authority preflight — distinct negative cohort

Explicit `--cohort negative-v1` uses fixed `LocalState/COSMIC/HostedHyperdriveWriteFaultCurrent`, separate from the expired Solana cohort. One new journalled temporary owner, three new IDs/operation keys; original expired intents were neither replaced nor replayed.

All three live authorities (Mongo/SQLite/Neo4j) returned `COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED`, IsError=true/IsSaved=false/nullresult while their avatar-only fault was active, despite failover/load-balancing/replication ON. All faults were restored and independently read back. Before/after queries over all three IDs on all three databases returned zero owner-visible rows. This proves the early ownership rejection path and no observed cross-database creation; it does not exercise post-preflight conditional dispatch, ambiguous commit recovery, durable outbox reconciliation or write-authority migration. X10 remains red for those scopes.

Runner: `Scripts/Sandbox/Verify-HostedHyperdriveWriteFault.py --cohort negative-v1`. Successful cohort acknowledgement prevents replay. Four pure verifier tests pass. Completed durable cohort17files/2,444B; account expires normally, no successful holon save observed. Original expired-cohort absence reconciliation remains separate/unverified.

Regenerable final report `TestResults/HostedCqlIntegrationCurrent/hyperdrive-write-fault-negative-v1-http.json`, SHA256 `7B89363EC44A91ACBE1B57F909230EEA4D8661BBBEFCE6D34520F17D22121E4C`. Final absence/restoration summary is backed by the retained native-run acknowledgement (the initial report writer recorded per-case outcomes; its final summary was enriched from that acknowledgement without repeating requests).


## Recovery evidence reconciliation — 31 October 2026

Read preserved repair commands and source inspection, not native services. Sequence6command acknowledged success; reviewed sequence8command acknowledged success. Follow-up2026-10-30T21:32:05.5938103Z shows6selected obligations Completed, all error codes null, SQLite held headnull and mutationCommands0. Its explicit scope is source/outbox only, not destination contents/query acceptance. Those distinctions are now reflected in REPLICA_BASELINE_AND_RECOVERY and X10a/X10b. The prior text incorrectly described these exact repairs as still pending; repeating them would be wrong. Current automatic cutover, general operator/API authorization/isolation, larger-store admission and broader source/target routes remain unverified.

Hashes: `hosted-repair-command.log` c0a5cc31358e009154ef51281cfe11788897fe36ce34048b9855e46131912ec7; `hosted-repair-8-reviewed-command.log`9211fe9a24c19dd43454b11e91c2fa5369d28bf51f0439820baa576ffb0b0c5b; `hosted-repair-after-8.log`4679f2115b557eddc3a64ed96a4f67bb2b9eeb13bb95ffeca78082534d698662, all under worktree TestResults/Web4SandboxCqlCurrent. No repair call, replay, source write or retest of a green scope occurred during this evidence audit.

## Read-only routing cohort ownership — 31 October 2026

The pending load-balancing verifier no longer uses disposable TestResults as its durable expected-record source. Each selected cohort requires owner.acknowledged.json, owner.dpapi and routing-record.acknowledged.json together under LocalState/COSMIC. The latter has schema1, sourceEvidenceSha256 and expected (id/name/version/createdByAvatarId); canonical nonempty IDs, actual integer version and exact acknowledged owner are required before HTTP. It is an expectation, not a substitute for native replica reads or server authentication.

Original Solana expected record was retained once from its existing successful proof SHA256429991c47b4bc0ee56f7a84828cc31d5fd52b445f368c2374267efe53f62f779, checking the original owner. Durable file310B. Actual default-gate invocation refuses the expired session before HTTP; no account/save/request replacement. No green source query was repeated.

A future explicitly planned fresh cohort uses LocalState/COSMIC/HostedHyperdriveRoutingCurrent, with its own ordinary create-only operation intent/ACK, exact independent replica baseline and owner-bound record evidence. Only then run `python Scripts/Sandbox/Verify-HostedHyperdriveLoadBalancing.py --cohort routing-v1` after response-owned backend metadata is deployed. This read-only command cannot provision sessions/fixtures. Never copy old record IDs into a new owner cohort. Original cohort and partial outcomes stay preserved.

Seven controlled routing/session tests and eight existing hosted-demo controls pass. The new tests exercise missing acknowledgement and expired bearer refusal without requests/writes, and foreign-owner/malformed evidence rejection. These are verifier controls; live balancing remains unverified.

## Distinct routing-v1 fixture setup — 31 October 2026

Implemented `python Scripts/Sandbox/Prepare-HostedHyperdriveRouting.py`, to run only after publishing response-owned routing facts. It is not run live yet. Fixed durable state: LocalState/COSMIC/HostedHyperdriveRoutingCurrent. Creates at most one acknowledged temporary owner and one private version1Mongo record using an exclusive flushed original intent, create-only expected revision, pinned Mongo authority, failover/balancingOFF and replicationON. The same operation ID is in body and request header. An uncertain response or incomplete/foreign/public save acknowledgement leaves the original intent and stops; no retry/new ID. An existing save intent without its original owner acknowledgement prevents replacement session creation.

The durable save ACK binds owner, operation and complete intent hash. After acknowledgement, only exact explicit no-failover/no-balancing reads inspect all three native providers, matching ID/name/version/owner/description/private flag. A missing replica stops setup without deleting data or resaving; rerunning the same setup resumes reads only. There is no polling loop or hidden delay/retry. After all three match, a durable baseline ACK is flushed, then routing-record.acknowledged.json binds its exact SHA256 and expected record. A completed fixture refuses setup repetition. Existing expired cohort sessions still refuse replacement.

Eight controlled setup cases pass, including interrupted replica-check resume with exactly one save total, uncertain save refusal, intent/owner change, incomplete ACK and private-replica mismatches. Seven existing routing controls also pass. These are offline protocol checks, not actual new hosted records, replication or balancing acceptance. One initial resume test lacked the owner ACK that the real session writes; its fixture was corrected and the full suite rerun successfully. All temporary test directories were automatically removed and absence checked.

## Consolidated release preflight — 31 October 2026

Fetched root origin/Development0e688ac9c2db452dd866a119fb84ecb7ce971fd9 and Core Development759b49a; root merge dfa4c6936 preserves the incoming prepared-storage receipt-ID assertions and CI WEB4 artifact build prerequisite. No conflict, manual reset, competing checkout or Development push. Core task6bbd015c5b35f6b401d289676576ccd781984ab9 remains published/ls-remote verified, with matching root gitlink/manifest. Scope versus Development is43files: tested routing metadata, strict reviewed shell recovery/source completions and their native/control evidence, guides/demo/verifiers/docs. The separate540-file provider backlog stays excluded.

Reused Debug outputs and no-restore bounded builds under one lease: shipping WEB4 and Core unit test projects build successfully.1009selected HyperDrive/admission contracts pass, zero failures/skips, including actual WEB4 controller-artifact loading and the merged operation-bound receipt-ID assertions. TestResults/HostedCqlIntegrationCurrent/release-hyperdrive-admission.trx SHA256DA95B0D0181315ED03A16FF819787E6674E7D81CBF1C7EDF8E31C382802A53DF. No extra Release output or native environment allocated. These are integration contracts, not fresh hosted/native query proof.

Static provider-I/O manifest:900public manager methods/729classified; route manifest:730controller actions/621classified; V2 inventory47paired operations. All3scripts pass. Static coverage is not runtime provider/option acceptance. Committed manifest/seven Dockerfiles/gitlinks validate.

Read-only Railway CLI observation: latest WEB4dev deployment68fb8cc4-c603-4d16-a97a-0e5a30e364c2, parent0e688ac9c, SUCCESS. Original full eight-provider HTTP proofs remain from4dbe69d0/498e3fd7f; no green scopes or expired sessions were replayed. Pending batch must advance Core Development and verify ls-remote before the matching parent Development/pin batch. Fetch again immediately before publishing, preserve newer contributors and normal once/twice-daily cadence; no force push or extra small Railway deployment. Then observe the one resulting image, prepare the new routing cohort and run response-owned live balancing. Browser transport remains closed; do not restart Windows control after David's Escape stop.


## Seven-source prepared configuration review — 31 October 2026

Browser IAB getTab revalidated: Transportclosed. Windows control remains stopped after Escape; do not restart or interfere with games. No new David action requested. His central list still records pending extra-publication timing and deferred Azure/Oracle hosting. Latest read-only Railway observation:68fb8cc4-c603-4d16-a97a-0e5a30e364c2 SUCCESS at0e688ac9c2db452dd866a119fb84ecb7ce971fd9; no new profiles/routing release inferred. Earlier498e3fd7f deployment is now REMOVED; its acceptance is historical, not a fresh live result.

Added a public-only export of the prepared seven-source candidate: no DNA, admission, tokens or credentials. New shipping `CqlPreparedPublicBatchTests` instantiates actual read adapters without activation and plans all28actual publisher presets against their real capabilities and static Mongo capabilities.1test passed/0skips, seven sources/four presets each, including14Mongo federations. No native calls, provider writes or deployment. Three pure export controls check omission of private settings and rejection of credential-bearing source fields/URLs. One test compile ambiguity with MVC File was fixed by qualifying System.IO.File.

The checked review export was regenerated byte-for-byte by the committed helper. Reproduce using `python Scripts/Sandbox/Review-PreparedPublicBatch.py`, set `COSMIC_CQL_PUBLIC_REVIEW` to the generated worktree TestResults/HostedCqlIntegrationCurrent/seven-source-public-review.json, and run the existing API UnitTests filtered to CqlPreparedPublicBatchTests under one provider build lease with bounded build flags. Clear that environment variable afterward. It reuses the preserved protected candidate and never observes/modifies hosted variables.

This closes the actual staged-preset planning gate only. Matching code/config/frontend publication, hosted source availability, private visitor preparation/reuse/isolation, exact replica checks and browser acceptance remain open. Never apply this candidate to frozen6bb or relabel offline planning as a live query.

- `TestResults/HostedCqlIntegrationCurrent/release-recheck.json`: 443 B; SHA256 `58229c060c3a4cd689f2b9f1e0d0bc9c07631ffa4cbfefea855bce6a394f02d7`.

- `TestResults/HostedCqlIntegrationCurrent/seven-source-public-review.json`: 27764 B; SHA256 `ed04d6c40ea1f0b12e77162c132fcf3dcf8f39d8f0d1f829f4db462dc6aa5e04`.

- `TestResults/HostedCqlIntegrationCurrent/seven-source-public-review.trx`: 3377 B; SHA256 `d21868d61f96bf5b57b8dd2225c9bef24238075a864e35c501170fa3c4fe739f`.

- `TestResults/HostedCqlIntegrationCurrent/public-review-export-controls.log`: 106 B; SHA256 `383a12fba9b5b327054a24ce7e5739569c4f0b6621facd981f2d6a4271063b3c`.

### Ten-source read-only hosted reconciliation — 31 October 2026
Reconcile-TenSourceHostedConfiguration.py observed current variables once and compared them in memory against the preserved ten-source before snapshot. Exact match;0changed keys,12planned settings (10source configs plus shared admission/examples). No values printed/persisted in plaintext, no mutation/deployment. Pure drift-control1test passes. Evidence: TestResults/HostedCqlIntegrationCurrent/ten-source-host-reconciliation.json and ten-source-reconciliation-controls.log. Offline40preset proof remains separate from hosted acceptance. Extra daily Development-publication decision remains pending in David list; no repeat ask or inferred permission. Matching rollout must include current Core3d5be79/application public profiles, not frozen6bb.

### Three-source client preparation readiness
New Base/Avalanche/BNB client tests pass3/0skips on website taskb6c6854. Each uses actual recipe/prepare modules: native3key preset retained, bounded2key read, server-derived IDs,2Mongo-only private saves/provenance and repeated preparation0new/2existing. Earlier green client scopes not rerun. Hosted Verify-HostedQuerySource.py now accepts these3profiles through the existing journalled flow. CLI help validated without creating sessions or sending requests. Do not run until matching hosted sources are ready. Evidence: TestResults/HostedCqlIntegrationCurrent/three-source-client-preparation.log. Browser retry still Transport closed; Windows control not restarted. Rendered SOLID/S3/Solana acceptance remains open.

### Ten-source application entry point
`python Scripts/Sandbox/Prepare-TenSourcePublicBatch.py apply-reviewed` is prepared, NOT EXECUTED. Use only after publication timing and matching application/Core release are verified; frozen6bb is incompatible. It validates exactly12changed settings with no deleted/unexpected/existing-source settings, writes10source configurations before shared admission/examples, and delegates all mutation intents/concurrent checks/observed ACKs to existing apply_values. Every set uses skip-deploys; no automatic retries/resume after uncertain outcomes. Failure requires original-intent reconciliation.2scope controls pass; evidence TestResults/HostedCqlIntegrationCurrent/ten-source-application-controls.log. Before application rerun read-only host reconciliation. No settings/deployment changed in preparation.

### Optional Optimism extension — 31 October 2026
`Prepare-TenSourcePublicBatch.py with-optimism` prepares separate eleven-source-public-{before,candidate}-variables.dpapi67,094/95,094B. Original ten-source protected files preserved. Public-only44preset export plus focused actual4Optimism presets generated from that candidate. Shipping planner1test/0skips proves the new4presets with real inactiveOptimism/nativeMongo capabilities; earlier40presets not rerun.3application controls pass, including explicit optional scope. No hostread/apply/deployment. The explicit apply-reviewed-with-optimism command uses exact13settings and the existing journalled publisher; only run after current-host reconciliation and permitted matching application/Coreccc3edb release, never frozen6bb. Ten-source commands remain available for the earlier scope. Evidence TestResults/HostedCqlIntegrationCurrent/{optimism-prepared-review.trx,eleven-source-application-controls.log,eleven-source-public-review.json}.

### Optional Linea/Scroll extension
`Prepare-TenSourcePublicBatch.py with-linea-scroll` composes all13public profiles into a separate candidate; optional Optimism required. Before/candidate protected files67,094/99,990B. Existing10/11protected candidates preserved. Exactly15settings admitted by explicit apply-reviewed-with-linea-scroll; shared journalled write owner unchanged, no application run.4scope controls pass. Initial preparation correctly refused copied Linea/Scroll endpoint URLs in builders/tests before protected output; fixed both to retained native-proven rpc.linea.build/rpc.scroll.io, corrected6configuration controls pass. Repreparation succeeded using actual retained catalogues. Actual8newpresets pass shippingplanner with realinactiveadapters/nativeMongocapabilities (1test/0skips); no freshhostread/configmutation/deployment.

### Optional zkSync/Abstract extension — fifteen sources
`Prepare-TenSourcePublicBatch.py with-zksync-abstract` creates separate fifteen-source-public-{before,candidate}-variables.dpapi67,094/104,934B; earlier10/11/13candidates preserved. Public-only60preset export and focused actual8newpresets generated from samecandidate. Shipping planner1test/0skips passes with realinactivezkSync/Abstract/nativeMongocapabilities; prior52presets not rerun.5scope controls pass, exact17settings through explicit apply-reviewed-with-zksync-abstract, journalledpublisher/noautodeploy unchanged. `Reconcile-TenSourceHostedConfiguration.py --scope fifteen` freshhostread exactmatchesoriginalsnapshot,0drift/17plannedsettings; nomutation/deployment. Matchingapplication/Corefb43eaa release and pendingdailypublicationtiming must be resolved before applying; frozen6bb incompatible. Evidence: TestResults/HostedCqlIntegrationCurrent/{zksync-abstract-prepared-review.trx,fifteen-source-application-controls.log,fifteen-source-host-reconciliation.json,fifteen-source-public-review.json}.

## Approved consolidated publication — 31 October 2026
David explicitly approved “Publish one consolidated batch now”. Scope is frozen correctness/routing parent79fe887e3/Core6bbd015, excluding newer publicprofiles. Refetched Development parent0e688ac9c/Core759b49a; both ancestors of release branches,no conflict/drift. Original1009passed/0skips report SHA256DA95B0D0181315ED03A16FF819787E6674E7D81CBF1C7EDF8E31C382802A53DF verified.3static inventories and parentpins pass. Publish CoreDevelopment first/ls-remote, then one parentDevelopment push; observe resultingRailwaydeployment before newrouting-v1setup. No master/newhostspend/sourceconfigurationapplication authorized by this batch. Newer tasksource preserved on work/cosmic-cql-google-public.

PublishedDevelopment57174def3/Core6bbd015remoteverified; samejob e107f7e5-fb8b-4cce-854b-46ebad7c8600QUEUED Waiting for build slot. TaskbranchmergedDevelopment preservingdocchronologies; routing-v1stateabsent, waitterminalsuccess.

### Optional Monad extension — sixteen-source candidate
`Prepare-TenSourcePublicBatch.py with-monad` prepares separate sixteen-source-public-{before,candidate}-variables.dpapi67,094/107,366B; earliercandidatespreserved. Public64presetexport/focused4actualMonadpresets generated. Shippingplanner1test/0skips passesnew4presets withrealinactiveMonad/nativeMongocapabilities; earlier60notrerun.6scopecontrolspass; exact18settings requireexplicit apply-reviewed-with-monad throughsamejournalledpublisher,notexecuted. Readonlyreconciliationtool supports --scope sixteen forfuturepreapplycheck. Matchingapplication/Core6d26291 required, neverapprovedrouting-only6bbimage. Existingapprovedjob e107f7e5 stillBUILDING exact57174def3; noextradevpush/configmutation. Raw HostedCqlIntegrationCurrent/{monad-prepared-review.trx,sixteen-source-application-controls.log,sixteen-source-public-review.json}.

Approveddeployment e107f7e5 reachedSUCCESS exact57174def3. Routing-v1setupcompleted:1owner/1acknowledgedprivateversion1Mongo record, exact3nativecopiesverified. Readgatepassed3explicitproviders thenfailed Default routeevidence:RequestedProvider=MongoDBOASIS,notDefault. Diagnosticreadonlysameoriginalrecordconfirms; noresave/newowner. Rootinspection:DataController ordinary non-admittedV2acquisitionusesprovider-bound legacyconstruction,bypassingDefaultbalancing. Fix/controllerregressionwork next onscopedreleasebranch; noadditionalDevelopmentpushyet. Raw routing-v1-{setup,balancing}.log,hyperdrive-load-balancing-routing-v1-http.json,routing-v1-default-diagnostic.json.

Publication result:CoreDevelopment6bbd015c5b35f6b401d289676576ccd781984ab9 and parentDevelopment57174def3ec008b360f8a9ecad585340d066b800 fast-forwardpushed/ls-remoteverified. ExactlyoneparentDevelopmentpush. Railway e107f7e5-fb8b-4cce-854b-46ebad7c8600 created2026-10-31T22:48:38.542Z, exactparent57174def3, observedQUEUED. Track samejob to terminal; no redeploy/restart on observationtimeout. Current priorlive68fb8cc4 remainsSUCCESS; newrouting-v1state doesnotexist, no session/save yet. Sanitizedevidence TestResults/HostedCqlIntegrationCurrent/approved-release-{deployments,current}.json.

Sameapprovedjob reobservedQUEUED with Railway queuedReason="Waiting for build slot". This is a confirmed externaldeploymentwait, not terminalfailure or permissionblock. Continueobserving e107f7e5-fb8b-4cce-854b-46ebad7c8600; do not redeploy/cancelothers or create routingfixture against priorimage. Sanitizedreason added to approved-release-current.json. No localservices/newresources or hostsettings changed.

## Live default-routing root cause and scoped fix — 31 October 2026
ApprovedRailway e107f7e5SUCCESS exactDevelopment57174def3/Core6bbd015. Newrouting-v1fixture has1journalledtemporaryowner/1acknowledgedprivateversion1Mongo write and3exactnativecopies.3explicitproviderreads returncorrectroutefacts; firstDefaultONreadreturns RequestedProviderMongoDBOASIS andMongo, so gatecorrectlyfails. Readonlysameoriginalrecorddiagnosticconfirms. No fixture/session/save replacement.

Rootcause:DataController only used its unboundV2factory insidePreparedStorageExecution admission. Ordinaryreads reachedBootLoaderdefault activation and constructedprovider-boundHolonManager; ResolveProviderType(Default) thenbecameMongo beforeHyperDrive. Fixextends existinglazyacquisitionhelper with explicitordinaryV2opt-in forDataController; allothercallers retainadmission-onlydefault, admittedinvalidDNAgateway remainsstrict, factoryerrorspropagate. Legacyacquisition behavior unchanged. NoCoreproduction/publicAPI/providerconfigchange.

Actualparameterlesscontroller regression provesDefault selects soleenrolledSQLite whileMongo isnodeDefault; explicitMongo stillwins.2newcases pass;13helperguardtests and61controller/HTTPtests pass0skips (includes2newcases). Initial2fixturefailureswere loggingconfiguration beforeacquisition; correctedfixtureusesexistingLogSwitchingProviders=false, failedTRX/logretained. Evidence TestResults/HostedCqlIntegrationCurrent/routing-acquisition{,-guards,-http}.trx and routing-acquisition-initial-fixture-failure.{trx,log}. Followuppublicationrequiresanotherdailycadenceexception; approvedfirstbatchalreadyused. NoextraDevelopmentpushyet. Aftermatchingfixdeployment, rerunreadonlygateonsameacknowledgedcohort; noresave.


### Seventeen-source prepared review — 31 October 2026

The optional seventeen-source candidate contains 68 presets and 19 application settings. The four actual Sei presets pass the shipping planner using shipping source/Mongo capabilities: 1 passed, 0 failed, 0 skipped. Application-scope controls: 7 passed. Earlier candidates are preserved. No hosted variables were applied, and no deployment or hosted acceptance is inferred. Evidence: `TestResults/HostedCqlIntegrationCurrent/sei-prepared-review.trx`; regenerate with the focused `ActualSeiCandidatePlansFourPresetsWithShippingCapabilities` test and retained public review.
TRX SHA-256: `6dd472c7a76f2c8ec3f01d00bc09519a3662816ba584e7e4fe1693d858f7e9db`.


### Live failover acceptance gap — 31 October 2026
The unfinished failover verifier previously accepted correct record content without proving that another provider served it. It now requires response-owned route metadata: requested/initial provider equals the faulted source, serving provider is a different enrolled replica, Auto strategy and success. Five controls pass, including all six directed routes and rejection of missing, same-provider, foreign-route, error and changed-record evidence. Command: `python -m unittest Scripts/Sandbox/test_hyperdrive_failover.py`. No new hosted failover pass, fault, account, save or session is claimed; original cohort is preserved.


### Routing-v1 live failover attempt — 31 October 2026
Added explicit routing-v1 selection, durable fixture reuse and independent-visitor setup to the unfinished gate. Eight controls pass, including expiry preventing visitor creation and retained journals preventing replay. Live execution stopped because the original routing owner session expired; no faults or saves were sent and no gate journal was created. One independent temporary foreign visitor had already been acknowledged during explicit setup; retained under the same state directory, not deleted/recreated. Setup now validates the owner before allocating a visitor. Original routing cohort remains preserved; a separately planned post-deployment cohort is needed. No failover pass claimed.


### Complete seventeen-source planner acceptance — 31 October 2026
The actual retained public-only candidate (17 profiles,68 presets,66,092 bytes) passes the shipping planner as one batch using real source descriptors and Mongo capabilities. All source instances remain inactive. Result:1passed,0failed,0skipped. Test: `ActualSeventeenSourceCandidatePlansAllSixtyEightPresetsWithShippingCapabilities`; explicit `COSMIC_CQL_PUBLIC_REVIEW` selects the retained public review. No native I/O, account, save, host mutation or deployment. TRX: `TestResults/HostedCqlIntegrationCurrent/seventeen-prepared-review.trx`, SHA256 `adff4b0c51d06e490ccb299f02c700eb99c157cc2c2296ab7af605d2ebe9a8cf`. Review SHA256 `6378320a11b99a255a136e274a255045440f04fd27cc04100243e4602510d3e3`. Hosted acceptance remains red.


### Enforced matching-release publication guard — 31 October 2026
Found a release-path gap: the multi-source apply command enforced exact variable scope and concurrency, but the documented matching deployed application prerequisite was manual only. Apply now requires `COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT` as a full reviewed SHA, verifies the checked-out release equals it, and verifies the latest observed WEB4 dev deployment is SUCCESS at that SHA before invoking the sole journalled configuration mutation owner. Missing/mismatched/building/failed observations stop before writes; no fallback. Eleven application/guard controls pass, including proof the mutation owner is never invoked when the guard fails. This does not authorize a release or apply any variable. Existing protected candidates unchanged.

### Approved Default-routing correction publication — 1 November 2026
David explicitly approved: Publish correction now. Scope: production correction51de129d3 plus its existing13guard/61HTTP tests and documentation; no new public-provider expansion or Core pin change. Fetched Development494f988bc is an ancestor of the scoped release450614de6. Original74passing/zero-skipped TRX revalidated and Railway manifest/gitlinks pass. Core Development6bbd015 independently verified remotely. No production/master promotion, settings change or hosting spend.


### Explicit post-correction routing cohort - 1 November 2026
Deployment d8876691-5ce2-4495-9e3d-dedfb1732de0 remains QUEUED at051ac6af7. Explicit routing-v2 uses HostedHyperdriveRoutingV2Current and preserves expired routing-v1. No account/save/fault sent.23protocol checks pass. After exact job SUCCESS: prepare routing-v2 once, run balancing reads, explicitly prepare its independent visitor, run failover. Unknown writes/fault journals stop replay; preserve original intent and restore owned faults.


### Deployed routing correction and live failover - 1 November 2026
Exact Railway jobd8876691-5ce2-4495-9e3d-dedfb1732de0 SUCCESS at051ac6af7841f95d4babb1a3e061feee06b4a6c6/Core6bbd015. Routing-v2record0f952286-685f-485c-96bf-d55f4e33c1d6 created once; all three replicas match. Three explicit and six Default reads return exact record; Default is preserved and selects Neo4j rather than binding Mongo. Existing balancing verifier remains red because all six choose Neo4j; inspect configured/deterministic policy before changing acceptance, no false variation pass. Live failover passed Mongo->SQLite, SQLite->Mongo, Neo4j->Mongo. Each fault produces the offline error with failover off, succeeds via a different enrolled serving provider with failover on, records2refusals, isolates foreign private rows (0), and restores the fault in finally. No gate saves. Browser acceptance not inferred. Failover report SHA256 79919b7712c2867b44b2db6a2fcda0d7ad3187ba85d967d3822631f3d625bc6d. Durable intents/ACKs remain LocalState/COSMIC/HostedHyperdriveRoutingV2Current; no repeat of completed gate.


### Auto policy and request-scoped candidate routing - 1 November 2026
Read-only hosted configuration confirms DefaultStrategy Auto. Shipping SelectAutoProvider chooses maximum weighted score, with latency tie-break; repeated selection does not violate a rotation contract. Preserve the earlier nine-read report and unacknowledged variation gate; do not claim fairness or score optimality. A distinct live read-only gate passed3Default requests, each with one explicit autoLoadBalanceProviders candidate (Mongo,SQLite,Neo4j). Each response-owned InitialProvider/ServingProvider equals its sole eligible source and exact record/owner/version/content is retained. No settings changes, faults or saves. Two controlled tests cover all3eligible sources, wrong-serving rejection and unenrolled rejection. Evidence TestResults/HostedCqlIntegrationCurrent/hyperdrive-candidate-routing-v2-http.json, SHA256 a72cf17587b02103beac6f6b8fe825f69f0315146b295aa71407f0c2f01b74c6. Durable completion ACK prevents replay.


### Seventeen-profile consolidated release preflight - 1 November 2026
Preparing the second normal daily Development batch after verified routing correction. Parent candidate includes17public-read profiles; Core diff from deployed6bbd015 is only the explicit27provider unsigned-read tariff catalogue (current56c869ff8de0a520da8c09ba8ebc6f85bbd40ea4). No shared routing API change. Integration regression:970Core HyperDrive/tariff contracts and101API hosted-admission/controller contracts pass,0failures/0skips;53provider configuration controls pass. Route manifest730actions and manager manifest900methods pass; dependency pins/gitlinks pass. Actual68preset shipping-planner proof retained separately. Existing native profile/cohort evidence is reused; no green native scopes rerun. New public settings remain unapplied until matching deployed revision succeeds. No new cloud hosting, credentials or production/master promotion.
TestResults/HostedCqlIntegrationCurrent/expansion-release-core.trx SHA256 d580e3403eacc9b99b6671ba9e34cdbfca4336fc51eed4cb678c57635ae052a9
TestResults/HostedCqlIntegrationCurrent/expansion-release-api.trx SHA256 d8a98bfec58a032bc7151dcbbe8c21082aff3a9bbb934c608f1bae4e61ccf3a8
