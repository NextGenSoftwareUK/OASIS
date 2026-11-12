# Hedera / Hashgraph public account CQL status

Updated 1 November 2026. Native CQL/shared-manager/CLI complete; API/local HTTP complete; hosted/browser gates open. Provider type is the existing `HashgraphOASIS`. Counts:34implemented query types and31scoped hosted HTTP providers.

## Qualified contract

- Anonymous official Mainnet mirror endpoint `https://mainnet.mirrornode.hedera.com/api/v1/`; no SDK bridge, signing key, paid service or account creation.
- Three explicitly selected accounts: `0.0.2`, `0.0.3`, `0.0.98`. Proposed native predicate key `HederaAccountId`, equality/bounded membership over the configured catalogue.
- Canonical numeric shard.realm.number identities; exact nonnegative Int64 tinybar balances exposed as decimal strings. Preserve the native seconds/nine-digit-nanoseconds balance timestamp as a string.
- Each request is `accounts/<id>?limit=1`; do not follow transaction-history pagination or expose the incidental transaction payload as a query result.
- Require response account identity and explicit `deleted: false`; reject malformed balances/timestamps and unexpected transaction counts.
- Consistency is **LatestIndexed**. Returned balance timestamps can be much older than the request and differ across accounts. No freshness SLA, shared snapshot or caller-selected historical view is promised.
- Account responses have no independent network ID. Mainnet binding is the explicitly configured official endpoint, also recognized by the existing provider. Do not invent a network/ledger proof from the nodes response. Future configuration must reject endpoints for another network instead of merely labelling them Mainnet.
- Mirror data is API-reported indexing, not independent state/consensus/signature verification or proof of an avatar's account ownership. No source writes.

Primary references: [official Mainnet Swagger](https://mainnet.mirrornode.hedera.com/api/v1/docs/), [Hedera's public mirror announcement](https://hedera.com/blog/now-available-public-mainnet-mirror-node-managed-by-hedera/), [SDK versus mirror reads](https://hedera.com/blog/querying-data-on-hedera-sdk-vs-mirror-node-rest-api/). Existing provider endpoint policy: `HashgraphOASIS.ResolveMirrorEndpoint`.

## Evidence

`Scripts/Sandbox/Probe-HederaPublicReadiness.py`: three bounded anonymous GETs, three account observations, zero source writes. Redirects and duplicate JSON are rejected. `test_hedera_public_readiness.py`: eight boundary tests pass, no skips or network dependencies.

- Raw `TestResults/HederaPublicSourceCandidateCurrent/readiness.json`,1423bytes, SHA256 `d999e38c182d413a7682c11a761b7ae802b5933e63c34385d6cdb24f94e94efc`.
- Durable `LocalState/COSMIC/HostedHederaPublicSourceCurrent/{source,catalogue}.json`,1688bytes. Stable sourceOwner created once; preserve it and all three identities. Candidate schema is not consumed by shipping code yet.
- Earlier memory-only network/nodes probe has no ledger ID; it is not used as a network certificate.

## Next gates

1. Complete: explicit read-only mirror source, strict Mainnet endpoint, exact identity/balance/timestamp validation, bounds/cancellation and complete-page failure. Unknown config/signing/history options/unsupported scans reject.
2. Complete:21controlled+1real HolonManager3/2 tests pass/no skips; readable actual C#/CLI3/2 passes.
3. Complete:2API+1Core+1native HTTP+1actual4preset planner tests pass/no skips;3pure publisher+1focused client controls pass.
4. Guarded hosted rollout, visitor3/2+5/4, repeat reuse, foreign-private0 and exact replica checks; retain original save intents/ACKs.
5. Sandbox/local-demo links and separate browser verification. No provider-count promotion before the appropriate acceptance gate.

## Native CQL milestone

HederaPublicAccountMetadataSource uses shared capabilities/planner/manager and explicit read-only key-value backend. Official Mainnet endpoint bound; account identity/deleted state, exact nonnegative Int64 tinybars and seconds/nanoseconds timestamp checked before output. Incidental transaction history bounded/not mapped or followed. No freshness/shared-snapshot or consensus proof.

- TestResults/HederaPublicSourceCandidateCurrent/hedera-source.trx:22passed/0skips; SHA2562ce3a468e9b611dc24be8c917b431405e39c5dbc152baee332cbfa5d15528067.
- ActualCLI exit0,3/2; TestResults/HederaPublicSourceCandidateCurrent/demo-run.log SHA25684be881a19c4173fad629c94d37446cad57b1c156eb6adc4ebbf246fab6243b5.
- [Developer guide](../../Devs/HEDERA_PUBLIC_ACCOUNT_CQL_GUIDE.md). No API admission/hosted grant yet. Existing SDK bridge is not used for these reads.

## API and local HTTP complete

Native3/2, two private Mongo creates, paired5/4, foreign-private0/public3 and nine query receipts pass. Auth/billing remain controlled fixture infrastructure; no production/hosted replication claim. Exact cosmic_cql_hedera_http_demo marker9d45a9b3-513e-4ca6-a9c7-7d0ade42922c verified then DB dropped; owned Mongo stopped/27029 absent. Durable HederaMongoHttpCurrent original intents/save ACKs/cleanup retained; never replay.

- TestResults/HederaPublicSourceCandidateCurrent/hedera-admission.trx:2passed, SHA25605523d979287c2f819f0dc1f57d0c0aac0b20f220a6a2b528b1e7507c026da1b.
- hedera-tariff.trx:1passed, SHA25696b12c7d1b36ac9992aae138e5a0b79ddb1e69efb4dfd9412e4615c95a53b719.
- hedera-mongo-http.trx:1passed, SHA2566c8bc48c5b575d0359d9a532db40c68e563d1d14001e7236545cbf4c326cdd42.
- hedera-prepared.trx:1passed, SHA256e8c97d1e93096796089d168592a4edd571659ac14bd660784d380c2dd47541d6.
- Separate deployed Node reachability3account reads200, no balance-number interpretation/CQL proof: hosted-egress.json SHA256727c1442365ef9daa515fd04d498cad54b58fbdcfd952b728e17fb357d06a747.

Protected3setting candidate prepared against verified exact applied Aptos variables; actual4preset shipping planner passes. No Hedera setting/deployment/visitor mutation. Core89b5ffa task pushed/ref verified; keep later work out of current Aptos1cfb893c8 release. After Aptos acceptance/publication, publish this reviewed application batch, require exact SUCCESS, then guarded settings/one rollout and hosted visitor gates.

Development publication1November: parent9d9b28872565d54ca058238e8e89ce6e8b454bf2/Core89b5ffad5c589e6607f26892f46a4ee718335fe2, both remote refs verified after fresh fetch/ancestry checks. Committed dependency manifest/gitlinks and5SDK closures pass. Exact applicatione5e9ef07-6e15-4ed2-99a7-779ad413cd5d BUILDING; candidate still prepared only, no hosted visitor. Monitor same job/no restart on observation timeout. Guidecbba242f7 adds complete first-checkout JSON; card task-only.

Rollout update1November: exact appe5e9ef07 SUCCESS at9d9b28872/Core89b5ffa. All3Hedera values individually acknowledged and whole protected candidate observed exactly. One journalled settings deployment9dfe2fbd-45f9-4706-a3d4-85ca0f5c2a3d submitted. No hosted visitor/save cohort yet. Observe same job; no timeout replay/restart.

## Live scheduling defect and correction

First hosted native selection returned504/CQL_QUERY_CANCELLED before any private preparation/save artifact. Original owner cohort retained; token expires2026-11-01T12:22:42Z. No hosted pass/count granted. Endpoint query deadline remains5seconds.

Deployed read-only timing: serial account calls294/522/2872ms,total3688ms. This observation is below the deadline but shows additive latency and limited margin; the actual acceptance request timed out. Independent latest-indexed account reads now overlap with maximum3inflight, preserve input order and await all reads before decoding/publishing or disposing the gate. No retry, endpoint fallback, partial success or deadline increase.

Two deterministic gate-based regressions prove overlap/bound/order and cancellation of inflight/queued reads. All24affected source tests pass/no skips, including real shared-manager3/2 and complete-page failure cases. Native HTTP/Mongo seed fixture is already green/cleaned and must not be replayed.

- TestResults/HederaPublicSourceCandidateCurrent/hedera-scheduling.trx SHA2565b327c03f3df79b3f63b4b742a06c5c7dab8f30970d4d95ae930581e9503c9ba.
- hosted-read-timing.json SHA2564f677824b9aca958f812203e88931f0869949c33c4d53c04a0225dd796a92812.

Deploy correction, then resume original owner cohort if still valid; preserve it if expired and explicitly plan a separate cohort. Sui new source file remains uncommitted/unverified and excluded from this fix.

Correction published1November: exact parent4fb66c68705be82d37bb97d9764b97f165661fca Development ref verified, Core89b5ffa unchanged/manifest valid. App122310fc-11ec-45cf-a631-2ebd471ce66a INITIALIZING; same existing Hedera variables/sourceOwner/catalogue. No settings redeploy required. Original cohort/no private writes retained; resume only after exact app SUCCESS, never restart job for timeout.

## Hosted HTTP complete — 1 November 2026

## Current verified milestone — 1 November 2026

**32 providers have scoped hosted HTTP evidence; 5 have browser evidence and 27 browser gates remain open.** Hedera now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. The original owner cohort was resumed after the scheduling correction; no duplicate seed saves were required.

Application `4fb66c68705be82d37bb97d9764b97f165661fca`, Core `89b5ffad5c589e6607f26892f46a4ee718335fe2`, deployment `122310fc-11ec-45cf-a631-2ebd471ce66a` succeeded. Existing settings deployment `9dfe2fbd-45f9-4706-a3d4-85ca0f5c2a3d` was retained. Website main `07a36e7` is byte-identical to the served HTML (SHA256 `742262270af5294b0e16a3cb254a8152b545d668e81d7037314f64f024644367`); this proves publication, not browser interaction.

Sui brings implemented query types to **35**, with **30 native tests and an actual shared-manager CLI 3/2 pass**. Sui API admission, local HTTP/Mongo integration, hosting and browser verification remain open. Azure/Oracle hosting stays explicitly deferred. Full ORM/parity/HyperDrive certification remains incomplete. Dated older milestones below retain their original scope and are superseded by this current summary.

Evidence is retained under `TestResults/HostedCqlIntegrationCurrent/`; durable original cohort intents, save acknowledgements and completion acknowledgement are under `LocalState/COSMIC/HostedQueryAcceptance-HashgraphOASIS/`. Initial 504 occurred before private saves. The resumed cohort completed successfully; do not replay its writes.

| Evidence | SHA256 |
|---|---|
| HashgraphOASIS-native-http.json | fa142f0856cb8722e13668929e21a2d47336c0e2de28ff5a6a0d72f5c7406007 |
| HashgraphOASIS-client-http-flow.json | dde55b33ab2c11ccbbdefeeb38cff575610aad1a6471796e5d34f7d90ea2aeb9 |
| HashgraphOASIS-isolation-http.json | 2c10605fa0a9bf5461e51e1587b6f4271233a1f66ee8e9e24470369f8bbe2eba |

Scheduling is bounded to three concurrent reads **per native query**, with the existing five-second budget. Per-account index timestamps can differ; no freshness, shared snapshot, independent consensus proof or source writes are claimed.

Development 85df5709 (9eb5284b73): guarded .NET startup activation hit the 15-second response-header deadline at 15,007 ms without upstream status. An exact-container, read-only Node probe matched all three configured account identities with HTTP 200 headers at 353 ms, 690 ms and 9,611 ms. This confirms reachability and variable latency for the sample. The guarded .NET path and recovery remain open under the existing Wizard action. No settings, files, installs or artifacts were created by the probe.

Development fbc0e08e (1f1021ec6a): Hedera moved from unavailable during startup to native and federated catalogue readiness. Both reviewed public native presets passed the row checks (3 full and 2 subset) using the retained demo visitor. Paired browser verification and startup reliability across deployments remain open. No new avatar, query report, local build, or download was created.

On Development fbc0e08e the two reviewed federation presets returned three and two public Hedera rows with exact provider and copy identity checks and strict subset containment. The existing visitor had no prepared Mongo copies. The private paired browser flow remains open. No report file or local build output was created.

On fbc0e08e, the browser displayed 3/2 public Hedera rows under the disposable demo visitor. The original private cohort directory is absent on this recovery checkout. Recover the original identity and its completion acknowledgement from the originating machine before verifying the private paired presets. Do not replay the two accepted writes.

At 00:06 UTC on 11 November the recovery laptop reached the configured public mirror probe endpoint and validated all three teaching accounts. This is laptop-to-mirror evidence. Development startup recovery is still open.

The feature image build 42925770019 passed for b5a01a8030. The Development-based image run 42926699339 reached Build and push after checkout and provider SDK publication checks passed. Its input is 2b828fd377. The standalone classifier passed three cases and the two WEB4 test cases remain unrun. Probe e754304684 passed eight existing tests and returned three live accounts with --no-report, without writing a report. Development integration and live activation remain open.

Railway a64672cb deployed 2b828fd377 after exact CI 42926699339 passed. Hedera activation again timed out awaiting response headers at 15,009 ms without an HTTP status. The catalogue settled at 104/106 ready, with Hedera and Nostr unavailable. Preserve the public browser 3/2 result on fbc0e08e. Recovery still requires successful activation on the current release and private paired verification with the original cohort.

11 November UTC guarded .NET comparison: a 9,728-byte temporary probe loaded NativeConnectionGuard.CreateGuardedHttpHandler from the shipped /app Core assembly over Railway SSH. Three concurrent mirror reads returned HTTP 200 with response headers at 335, 617 and 3,551 ms. Standard SocketsHttpHandler controls also returned HTTP 200 at 16, 531 and 925 ms. This establishes post-startup guarded HTTP reachability; it does not identify the cause of the earlier activation timeout or recover registry readiness. No deadlines/settings changed.

11 November UTC full shipped-provider activation probe: an isolated .NET process in the current Development container loaded HederaPublicAccountMetadataSource from the deployed provider assembly and invoked FromJson with the existing CQL_HEDERA_PUBLIC_SOURCE_JSON environment value without printing it. ActivateQuerySourceAsync completed in 2,727 ms with IsError=False, Result=True and IsProviderActivated=True under the existing 30-second caller budget and 15-second HTTP timeout. This exercises all configured account reads, identity validation and mapping. The process disposed its source. The running WEB4 registry was not changed; its controlled recovery remains open.

WEB4 diagnostic regression gate complete: CI 42938257820/job 130253566184 succeeded on fda32cf5d3 with all 13 selected cases passing, including the seven native diagnostic cases and both Hedera stage cases previously unrun. This replaces the earlier unrun-test checkpoint. Current-release registry recovery and live query acceptance remain open.


11 November UTC: Railway Development WEB4 deployment 345d9ec9-7810-4251-9cbf-f2c6e697a738 succeeded on 9b8a30a65c. Its startup log independently reproduced the HashgraphOASIS response_headers activation timeout at 15,008 ms with no upstream HTTP status. The historical public 3/2 browser proof remains recorded. Current-release registry recovery and live query verification remain open.


11 November UTC: Development fd40b1ad12 reproduced the Hedera startup response_headers timeout in 15,017 ms without an HTTP status. Authenticated discovery returned CQL_SOURCE_UNAVAILABLE. Earlier browser acceptance is preserved. Current release recovery remains open.


11 November UTC: the new c39eba5cd1 WEB4 instance advertised Hedera native and federated readiness. Both native public presets passed authenticated live HTTP with exact row IDs and counts 3/2. Prior startup failures remain recorded. Private paired browser and restart reliability remain open.


11 November UTC: on Development c39eba5cd1, Hedera's two MongoDB-paired public presets returned 3 full and 2 filtered rows through the live WEB4 federation endpoint. The selections matched the native preset keys and all returned copies were from Hedera. The independent visitor saw no MongoDB copies. Original private paired browser verification and restart reliability remain open.

11 November UTC: standalone Hedera Railway deployment `09567e4c-41ea-4418-a32e-8c1150572d47` at `a7522f4286` is REMOVED. Build logs reached Docker image export and no deployment logs were returned. Live WEB4 still serves `3f5d781f29`. Building successor `fcf2a522e6` includes the Hedera commit by Git ancestry, so verify Hedera and ActivityPub on that successor after cutover.

11 November UTC: verified Railway Development deployment `5e1e3ab0-81f9-4fea-83b8-12e851c999ed` at `fcf2a522e6`. ActivityPubOASIS and HashgraphOASIS both returned three full and two subset rows through their live native public CQL presets. Discovery also reported federation ready. Prior paired and browser evidence retains its original scope.

11 November UTC: Development b1d78063c7 integrates HyperDrive Hedera native storage conformance. CI 43056077700 passed 52/52 on independent Hiero consensus ledgers using compression before encryption, with both contracts confirmed empty afterward. Hosted signed storage and the original-owner paired CQL browser check remain open.

11 November UTC: live WEB4 ea614f3fc6 reports Hedera activation timed out awaiting response headers after 15,008 ms with no HTTP status. Nostr's relay returned HTTP 503 during WebSocket upgrade (NotAWebSocket). Discovery remains 104/106. The protected Development Wizard session is absent on this laptop. Keep the existing operator recovery action open.

11 November UTC: Railway WEB4 cut over to e5e3ca694c, deployment a1f0917e-b72a-4b5a-8732-0b6547f41687. Authenticated discovery warmed from 92/106 to 105/106 native and federation ready. Hedera and TON recovered readiness on this revision; Nostr remains CQL_SOURCE_UNAVAILABLE. Supabase, TRON and CouchDB remain absent. Prior functional cohorts were left intact.

11 November UTC: Railway WEB4 moved to ea54de967e. Authenticated discovery reported 104/106 native and federated ready. Hedera and Nostr returned CQL_SOURCE_UNAVAILABLE. Supabase, TRON and CouchDB remain absent. Development 73f8b98967 added verified RavenDB storage (52/52 and four write-ownership checks, CI 43071621377). Prepared Supabase integration c9e1e98548 preserves all nine qualified Supabase blobs on that head. Image run 43071690459 still builds the preceding security-patched candidate.

11 November UTC: Railway WEB4 cut over to d93d984b7b, deployment d04ed4c4-2d76-4ea1-b261-393da541fe3b. Authenticated discovery returned 105/106 native and federation ready. Hedera recovered readiness. Nostr remains CQL_SOURCE_UNAVAILABLE. Supabase, TRON and CouchDB remain absent. No accepted query cohort was replayed.

11 November UTC: Railway WEB4 cut over to Sui storage revision 31db62c741 (deployment 1974ed95-8e85-4c76-80a0-3ec9f9b4fea8). Authenticated discovery returned 104/106 native and federated ready. Hedera and Nostr reported CQL_SOURCE_UNAVAILABLE. Supabase, TRON and CouchDB remain absent. CockroachDB revision 2dd52cf6c0 is building. Exact Supabase image CI 43072535002 remains active.

11 November UTC: Railway deployed WEB4 cadf5bf85b (c08e176d). Authenticated discovery warmed from 18/106 to 68/106 to 104/106 ready for native and federated CQL. Hedera timed out awaiting headers after 15,007 ms; Nostr received WebSocket HTTP 503 after 129 ms. Supabase, TRON and CouchDB remain absent. Prior functional receipts retain their scope.

11 November UTC: after the AvalancheGo WEB4 rollout restored Cosmos and Hedera readiness, the authenticated visitor ran both native public HTTP presets for each provider. Both returned three full and two filtered rows, with exact preset selections and strict subset containment. Prior private cohorts were preserved.

12 November UTC: live Development 922321a8aa9d reports Hedera unavailable after activation timed out awaiting response headers (15,008 ms, no HTTP status). The rollout did not change Hedera reader or CQL files. Existing owner recovery action applies; no recovery, settings change or query replay was performed.

Development WEB4 5251dc6df still reports Hedera unavailable in CQL discovery. A single read of the existing public 0.0.2 mirror account returned HTTP 200 with response headers in 184 ms from the live WEB4 container. The local Windows probe also returned 200. This establishes reachability at observation time. The protected operator session is still required to rerun WEB4 activation.
