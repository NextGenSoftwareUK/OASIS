# ThreeFold Grid Proxy public node qualification

Updated2November2026. Red: no native/CQL/hosted/browser acceptance yet.

[Official proxy guide](https://manual.grid.tf/labs/documentation/developers/proxy_readme/proxy/)
publishes gridproxy.grid.tf for mainnet, separate from dev/QA/test networks.
It exposes public indexed TFChain node information, including filtered/paged
collections and /nodes/{node_id}. This is distinct from creating contracts,
renting compute, signing transactions or storing COSMIC holons on the grid.

## Pinned contract

threefoldtech/zos_sdk_go commitf8d1d856db5b13154d7262daa548aa81b38bbfe8:
Swagger JSON and two Go data schemas retained with SHAmanifest under
LocalState/COSMIC/ThreeFoldPublicQualificationCurrent/sdk.

The /nodes collection has page/size/sort_by/sort_order/randomize and geographic,
resource and farm filters. It does not advertise arbitrary node-ID membership.
The point route accepts node_id. NodeDTO uses nodeId/farmId/country/city and
nested capacity values; whitelist projection fields, preserve integer units,
exclude IPs/twin contacts/arbitrary metadata. Do not invent an IN route.

## Exact next gate

Use the same .NET HTTP transport as the intended adapter, with default headers,
no authorization, redirects, retries or alternative hosts. First bounded paged
GET /nodes?page=1&size=3&sort_by=node_id&sort_order=asc&randomize=false; freeze its
three existing distinct IDs under an original intent. Read exactly those three
point records, then a two-ID subset through bounded native point composition.
Require exact selected identities and same stable public projection. No source
scans during later CQL execution. Pin operator-reported mainnet endpoint, without
claiming independent TFChain state proof or node uptime/availability guarantees.

No native endpoint request, environment, node, account, contract, deployment,
funding or installation has been created. Controlled tests precede the native
capture. ClickHouse's existing code deployment remains the immediate priority.

## Original native qualification passed - 2 November 2026

4offlinecontrols/PowerShellsyntax pass. Original.NETHttpClient(defaultheaders)6GETs allHTTP200: bounded3nodediscovery, exact3pointreads, exact2subset. StableNodeID/FarmID/GridVersion/country/city projections matchdiscovery/subset. NodeIDs 1,2,3. No IP/contact/resource-pricepayload projected, no compute/storagecontract/writes/install/account. Operator mainnetproxy provenance only; independentchainstate proof remainsfalse, no uptime/health/reliability claim.

Originalintent/discoveredkeys/report/compressed6rawresponses+SHAmanifest retained ThreeFoldPublicQualificationCurrent; raw TestResults/ThreeFoldPublicSourceCurrent. Acceptedqualification frozen/no replay. NativeCQL/ORM/manager/CLI/HTTP/Mongo/hosted remain unverified; no providercount increase. Next separate read-only GridProxy node metadata source, configured UInt32nodeIDeq/IN using boundednativepointcomposition, preciseidentity/complete-selection/error/cancellation/read-onlytests.

response-1.json:4769bytes SHA256 `5f5fae1ff5e7f6d5cf61abda290152e6aa99ed3457700f8c476d16a911b57361`.

response-2.json:2343bytes SHA256 `20d36868bdecdf7a660d5ff6a269ff678fa4eab11780cd053cfb3398f76b410c`.

response-3.json:1231bytes SHA256 `1405466a9b972e966ec6b72e15fc55cef34824975bceaf5f3b61904728415573`.

response-4.json:1232bytes SHA256 `1383da01164b64965420ae659e9f6da4defe6ba6827d96842bbbdeae463df4a3`.

response-5.json:2343bytes SHA256 `20d36868bdecdf7a660d5ff6a269ff678fa4eab11780cd053cfb3398f76b410c`.

response-6.json:1231bytes SHA256 `1405466a9b972e966ec6b72e15fc55cef34824975bceaf5f3b61904728415573`.

## Read-only adapter controlled milestone

ThreeFoldPublicNodeMetadataSource builds with33/33adaptercontrols passing (no skips). Fixedmainnetroot, canonicalnonzeroUInt32nodeIDs, metadataeq/IN+paging via boundedpointcomposition, strictnativeidentity/UInt32/geographicprojection,256KiB/15s/one-secondpacing, cancellation/nativeerrors/no partialrows and Save/Delete no-networkrejection. IPs/contacts/resources/provisioning excluded, chainstateverifiedfalse. ExistingMinIO/legacyprovider untouched. No nativeCQL/manager/CLI/HTTP/hostedacceptance or countincrease yet.

Reproduced developerwalkthrough failure: sharedRunAsync rejectedThreeFold because a duplicatedprovider-name switch lacked its metadata key. Originalnewtest failed, then passed after replacing switch with adapter.QueryCapabilities.StringKeySetLookup.MetadataKey. Onegenericstrategy now owns admission/keyselection; no extra nativequeryengine or per-providerdemo registry. Controlled3rows pass throughHolonManager/LINQ; nativeoriginal remains next. Acceptedgreenproviders not replayed.

TestResults\ThreeFoldPublicSourceCurrent\adapter.trx SHA256 `a7ca7c4a484273ec7faa26bae3608c5750d10e67232472141b51a1a63de28881`.

TestResults\ThreeFoldPublicSourceCurrent\demo-red.trx SHA256 `124f005b2779584a4c6552f14f3c5e190164a2735cac28cada32c75ef94d16db`.

TestResults\ThreeFoldPublicSourceCurrent\demo-green.trx SHA256 `4c440712f9d27e8edd1c9f9daa6432472a5afbdef686998cec567b41f667f5b9`.

Next: commentedThreeFoldC#walkthrough/configuration and actualcompilednative3/2 over originalnodeIDs1/2/3; then HTTP/Mongo/tariff/presets/hosted/card. ClickHouseactivation78d92d9b remains first live rollout gate.

## Actual compiled manager/LINQ/CLI proof

SharedC#demo fullcachedbuildpassed1m46s. Original --threefold-public run passed3full/2filterednodes throughHolonManager/deferredLINQ/nativepointcomposition over originalIDs1/2/3. No listing/discoveryreplay/provisioning/contracts/nativewrites/Mongosaves. Stableprojectionowner/config and qualification/source/binary-boundintent/ACK/compressedoutput retained HostedThreeFoldPublicSourceCurrent; outputSHA256 `48c283263bde4a3cc1bd8d37b64e81edaf1020b1ec5cad84652dddcc52d56740`. This is64thlocallyintegratedquerytype; hostedfunctional58untilClickHouseaudit. HTTP/Mongo/readtariff/presets/hosted remainnext. Acceptedsource/CLI frozen, do not replay.

## Explicit HTTP admission and original local Mongo proof

Three API tests and one Core tariff test pass, no skips. Explicit read profile registration uses CQL_THREEFOLD_PUBLIC_SOURCE_JSON; compiler and tariff enrollment remain independent of native write permissions. Four native/Mongo examples plan using shipping capabilities. Original local HTTP test passed in 19 seconds: native 3/2, Mongo-paired 5/4, foreign 0 private/3 public, two SDK version-1 saves and query receipts. Canonical UInt32 projection and chain-state-unverified provenance checked. Exact nonce-owned cosmic_cql_threefold_http_demo removed/absence acknowledged; HTTP consumers and owned Mongo stopped. Existing shared data retained. Original cohort/config and source-bound ACK frozen; do not replay. Core task commit 409a4445f5c2c23b83eb68ed90283c4c0824df0d pushed/remote verified; parent manifest advances with gitlink. No hosted publication/certification yet; totals remain 64 local/59 hosted. Next prepare/test hosted four-preset candidate, preserve newer Development/Core before integration, then original hosted acceptance and card.

`TestResults\ThreeFoldPublicSourceCurrent\admission-api.trx` SHA256 `e3d36069e0adab175308e16db482dedd02f27a287e0ff4bd6fa0a12787542ef7`; compressed original retained at `LocalState\COSMIC\HostedThreeFoldPublicSourceCurrent\admission-api.trx.gz`.

`TestResults\ThreeFoldPublicSourceCurrent\admission-core.trx` SHA256 `74074df7447b9440e00a68660e4c22bb7ede3fb96b5fe147d9ae4a818bf7065c`; compressed original retained at `LocalState\COSMIC\HostedThreeFoldPublicSourceCurrent\admission-core.trx.gz`.

`TestResults\ThreeFoldPublicSourceCurrent\mongo-http-native.trx` SHA256 `1919bfd91cd8076b5327ac971c2062ed3b90033a16bc6cc4338b5be6edcb5385`; compressed original retained at `LocalState\COSMIC\HostedThreeFoldPublicSourceCurrent\mongo-http-native.trx.gz`.

## Hosted candidate and audit controls

Protected original hosted candidate prepared without service mutation: exactly three values, free query tariff, native full/subset and Mongo paired full/subset. Ten pure publisher controls pass. Initial source-policy label mismatch was caught and fixed before candidate preparation; failed control output preserved. Independent audit controls pass two tests covering sixteen negative cases, including wrong endpoint/ownership/UInt32/verification claim/payload/privacy/replicas/ACKs. Actual shipping prepared-preset test passes 1/1, four available presets. No new native query/save. Core upstream gardening reward commit c83e255 preserved in task merge 54159614ebdb879c8636eed5fae60be97dbbfff3. Next preserve parent Development and verify combined build before publication/activation.

## Combined release qualification

Parent upstream c0edcb0ed merged as cb8ceed1c, Core merged 54159614ebdb879c8636eed5fae60be97dbbfff3. Only manifest conflict resolved to merged Core; gardening source/tests preserved. Combined API 11/11 and Core tariff 1/1 pass. Initial filename-based garden Core filter matched no reward methods; explicit GardenRewardCommitsOnceAndRepairsAuditAfterRestart runs both cases, 2/2 pass, no skips. Seven Dockerfiles/manifest/gitlinks validate. No accepted native fixture replay. Next code publication then exact job observation before applying the original candidate once.

`TestResults\ThreeFoldPublicSourceCurrent\prepared-presets.trx` SHA256 `bc1407bfd70c79fdfa8e3189e21438b3f80635ed1187f3a6c6930ffccd71255f`; original compressed under `HostedThreeFoldPublicSourceCurrent`.

`TestResults\ThreeFoldPublicSourceCurrent\combined-api.trx` SHA256 `79b86aec36e1101b300e93f1b8904f7ad2b375e1704d9a5110a7825185b0f2d2`; original compressed under `HostedThreeFoldPublicSourceCurrent`.

`TestResults\ThreeFoldPublicSourceCurrent\combined-core.trx` SHA256 `cf28248ac0963aa7aec2129d042e6c98d5c51629097c54694d8bfde792dffcb8`; original compressed under `HostedThreeFoldPublicSourceCurrent`.

`TestResults\ThreeFoldPublicSourceCurrent\merged-garden-reward.trx` SHA256 `106d091e149d7996254d213b0962887fcfb0c63a69554bba7e067ba035a7dfd2`; original compressed under `HostedThreeFoldPublicSourceCurrent`.

## Verified code publication

Parent Development/task c00da49c105c3ba4f9e7fa749ac538925b18d818, Core Development/task 54159614ebdb879c8636eed5fae60be97dbbfff3 pushed and remote verified. Complete live settings still exactly match the original candidate baseline. Durable code-publication intent/ACK in LocalState/COSMIC/HostedThreeFoldReleaseCurrent records exact new job 5d9b665b-984d-4bcd-bebd-e722ff9a6612, initially QUEUED. Settings not applied; native hosted cohort not started. Observe this existing job; do not redeploy/restart on timeout. After exact code SUCCESS, apply original three-value candidate once with shared journals, then activate and audit original cohort. Totals unchanged 64 local/59 hosted.

Exact code job 5d9b665b-984d-4bcd-bebd-e722ff9a6612 reports BUILDING after the explicit Waiting for build slot queue reason. No restart/redeployment or settings application. Follow the same job to terminal code outcome.

## Original settings application and activation

Code job 5d9b665b SUCCESS at c00da49c105c3ba4f9e7fa749ac538925b18d818. Original three settings applied exactly once through shared per-key intent/ACK journals, complete candidate equality and unchanged DNA verified. Exact release was selected only for gated application, then task branch restored without detached commits. One activation a737028a-40d9-489d-a087-53d2f6f955a6 BUILDING at the same code. Durable settings ACK/activation intent/protected CLI submission/job identity under HostedThreeFoldReleaseCurrent. No hosted cohort/native queries/private saves yet. Observe exact activation to terminal outcome, no reapplication/restart on timeout.

Activation a737028a SUCCESS at original c00da49c1. Provider discovery available/federationAvailable with four presets, no error. Original journalled owner created once and reused by the hosted verifier; original hosted cohort running. Preserve all native/save/operation outcomes, no replay/replacement owner.

## Original hosted acceptance and independent audit pass

Original owner reused after ready discovery. Native3/2, Mongo5/4, repeat0new/2existing, foreign0private/3public, exactSQLite/Neo4j2each all pass. Two ordinary API version1 private saves, no pending intents. Independent audit passed without new reads/saves. Original source/config/owners/operations/cohort frozen; do not replay or reapply settings/activation. Hosted60/local66 after historical DuckDB/LocalFile source recovery. No compute/native writes/fullORM/independent-chain/uptime certification. Next publish pinned card/guide/C#links and byteverify assets; browser remains open.

`ThreeFoldOASIS-native-http.json`: 12404 bytes SHA256 `d841cf3b3b3589610fd57eb56893aa2dc28de8139b5c7a140cb0ec89e1f2f00f`; compressed original under `HostedQueryAcceptance-ThreeFoldOASIS`.

`ThreeFoldOASIS-client-http-flow.json`: 33085 bytes SHA256 `baa40f71a302ca2ad6a0eae12b75a6d521ad143bfc9ed9eba7b898040fdaa218`; compressed original under `HostedQueryAcceptance-ThreeFoldOASIS`.

`ThreeFoldOASIS-isolation-http.json`: 4670 bytes SHA256 `f36181095d8309eed418dca2381497244a6e4bd8cba069e96efca35d24a3d2bc`; compressed original under `HostedQueryAcceptance-ThreeFoldOASIS`.

## Sandbox card and public assets verified

Website task/main2170b672851e7b302b3f0f582f7a938667d1ad89 pushed/remote verified. HTML parses with157uniqueIDs; card exposes live four-preset instructions and pinned guide/commented C#links. Initial public GET served old ClickHouse-eraHTML; original mismatch preserved. Second ordinary GET matches new HTML, both unchanged JS already match. No cache bypass/republication/native query/save replay. Compact verified ACK HostedThreeFoldPublicSourceCurrent/website-publication-verified.json; initial mismatch and second observation retained separately. Browser interaction remains unverified. Next recover33missinglocal launchers from retained branch.

`web4-sandbox.html`: 203638 bytes SHA256 `3049d46e0f8a24238acafae491adfb510822cc701c42f6e1bdb541615a84a290`, HTTP200/exactGitblob match.

`cql-demo-preparation.js`: 10241 bytes SHA256 `c1006abf6074616428f93e83e13774ba8a6198f35d8ed097d84323466a8b309a`, HTTP200/exactGitblob match.

`cql-playground.js`: 21871 bytes SHA256 `7cb6210b9adda1e83630fc966926772f8338c43163c24d1abd88c626ad1ad649`, HTTP200/exactGitblob match.
