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
