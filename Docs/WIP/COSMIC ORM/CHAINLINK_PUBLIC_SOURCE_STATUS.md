# ChainLink public feed source qualification

Updated1November2026. Native protocol and local CQL/actual C# CLI verified; API admission/local HTTP federation verified; hosted rollout remains pending. Current49integrated types include ChainLink native-only;44hosted proofs do not.

## Verified primary catalogue

The official documentation's [Ethereum network configuration](https://raw.githubusercontent.com/smartcontractkit/documentation/main/src/features/data/chains.ts) identifies [the reference directory](https://reference-data-directory.vercel.app/feeds-mainnet.json). Guarded anonymous GET returned HTTP200/301records/388274bytes. Exact `eth-usd`, `btc-usd`, `link-usd` paths selected; same-name SVR variants excluded. Original full selected entries and catalogue digest retained in LocalState, not inferred from search snippets.

| Feed | Proxy address (canonical lowercase) | Catalogue/native decimals |
|---|---|---|
| ETH/USD | `0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419` |8|
| BTC/USD | `0xf4030086522a5beea4988f8ca5b36dbc97bee88c` |8|
| LINK/USD | `0x2c1d072e956affc0d435cb7ac38ef18d24d9127c` |8|

## Native read contract

- Reused `https://ethereum-rpc.publicnode.com/`, pinned Ethereum chain1/genesis.18guarded HTTP200 calls: identity/genesis/finalized header plus description/decimals/latestRoundData for3feeds then2feeds.
- Every contract call uses EIP1898 `blockHash`/`requireCanonical:true`; descriptions/decimals match catalogue, round fields have exact ABI widths and valid timestamp ordering bounded by block timestamp. Five words retain uint80 round IDs, signed int256 answers, timestamps and deprecated answeredInRound without floating point.
- Finalized block `0xc3af2867f720230178891d37d162f83fd50a6a7f0160d4d72bbbb79c96a823d3` / number `0x191ad93`. Full/subset share this original qualification block; normal queries will resolve their own block.
- Original per-feed ages at block: ETH3372s/BTC372s/LINK276s. No current-price/freshness guarantee or financial recommendation. These are RPC-reported finalized observations, not independently verified consensus.
- Offline parser/transport check passes: UInt256 precision/signed -1, canonical dynamic ABI string, invalid padding/offset/UTF8 rejection, one RPC result/no task-output leak and HTTP fail-closed. Zero native calls in offline test.
- No wallet/node/account/funding/install/source transaction. Legacy provider remains unchanged; six unimplemented throws remain documented, not repaired.

## Local CQL implementation milestone

- Separate ChainLinkPublicFeedMetadataSource in the existing project, using shared CQL interfaces/planning/native mapper; legacy signed storage unchanged. Explicit shared KeyValueStorage project reference, no new package version.
- 43 controlled tests pass/0failures/0skips. Covers membership/point/window, unknown keys/no native I/O, chain/genesis/header identity, RPC envelope/status/duplicate/size failures, description offset/padding/UTF8/name, scale mismatch, round/time widths, cancellation/budget, failure without partial rows and native save rejection.
- Exact int256 maximum/minimum and -1, uint80 rounds above2^53 and deprecated answeredInRound0 remain representable. No float conversion or automatic freshness inference.
- Producer and actual console builds pass. Actual C# CLI/manager/LINQ3full/2filtered feeds pass against retained catalogue. Original cql-console intent/outcome retained; no native source transaction or Mongo save.
- [Setup/C# guide](../../Devs/CHAINLINK_PUBLIC_FEED_CQL_GUIDE.md), [commented walkthrough](../../../Examples/CosmicCqlDemo/ChainLinkPublicWalkthrough.cs).

## Exact next step

Core permission1/API admission2/local HTTP1/publisher10/prepared-planner1/client preparation1 now pass. Native3/2, Mongo5/4, foreign-private0/public3,2SDK version1 saves/9query receipts verified. Exact owned marked database cosmic_cql_chainlink_http_demo dropped after HTTP consumers stopped; Mongo stopped/27029 absent. Original fixture journal retained, no reseed/replay.

Lazy `CQL_CHAINLINK_PUBLIC_SOURCE_JSON` registration, independent unsigned read permission/free tariff and four presets are implemented; no signed write grant/default routing change. Protected baseline/candidate under `LocalState/COSMIC/HostedSourceRolloutCurrent/chainlink-public-*`; exactly3values differ, DNA unchanged, no settings applied yet. Source-bound native/local ACKs are durable. Core task990c1c75afcafdbc0678b6e8131a10b67650f7e3 pushed/ref-verified; parent gitlink/manifest updated together.

Development parent09c66998cb2c204f290d7a85198006e6abd3202f/Core990c1c75afcafdbc0678b6e8131a10b67650f7e3 now fast-forward published/ref-verified; manifest validates. Code job215b4b51-4431-4292-9c12-1063497d5773 BUILDING. Public publication ACK LocalState/COSMIC/HostedChainLinkReleaseCurrent/code-publication.acknowledged.json, observation TestResults/ChainLinkPublicSourceCurrent/code-deployment.json.

Next: observe that exact code deployment. Only after SUCCESS check out the exact code release for guarded settings application (no detached commits), apply reviewed settings once, issue one journalled settings deployment and restore task branch. Only after SUCCESS run original hosted acceptance/card. No ChainLink hosted/browser claim yet. Preserve completed native/local cohorts. Frontend preparation test taskdb58432 is pushed, no card/publication yet.

## Durable resources and evidence

State: `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent` contains original catalogue/native intents, selected catalogue and stable source owner/config. Never regenerate owner or replay original native qualification. Raw responses/compact reports: worktree `TestResults/ChainLinkPublicSourceCurrent`, not Docs. Scripts: [catalogue](../../../Scripts/Sandbox/Fetch-ChainLinkPublicCatalogue.ps1), [native](../../../Scripts/Sandbox/Probe-ChainLinkPublicReadiness.ps1), [offline check](../../../Scripts/Sandbox/Test-ChainLinkPublicProbe.ps1).

- `TestResults/ChainLinkPublicSourceCurrent/mainnet-catalogue.json`: 388274bytes; SHA256 `cf13ccee5ebd84a6427f25e490f77ad88a991cc040c655d55088c404ab1bd0d4`.
- `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/selected-catalogue.json`: 4405bytes; SHA256 `b5d35fd33654bd4bb379812fb0cbdfa55e06141749905c011d4795c8b11cc7bb`.
- `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/source.json`: 555bytes; SHA256 `2b309aa619a984cac55fb2ea33ede55730e73df85704c44d68e1148521673c5d`.
- `TestResults/ChainLinkPublicSourceCurrent/qualification.json`: 10265bytes; SHA256 `e7e7179a2b8b05d27374e0942eb174131fe490943035471edeacf2fc0ec11d2e`.

Native ABI reference: [Chainlink API](https://docs.chain.link/data-feeds/api-reference). Data is read via proxy AggregatorV3Interface; decimals must be preserved, answeredInRound is deprecated.

- `TestResults/ChainLinkPublicSourceCurrent/controls.trx`: 77588bytes; SHA256 `097b4ae7e961b0904e3f362701d1c7c3c42d84163b80eaf94d590788f9c1dc82`.

- `TestResults/ChainLinkPublicSourceCurrent/provider-build.log`: 19775bytes; SHA256 `cd53e3e36a72a8f4b37719a6b65190a81ccf65df3aa8afca16410564f7630b04`.

- `TestResults/ChainLinkPublicSourceCurrent/demo-build.log`: 10857bytes; SHA256 `0ebd9df8d7e21fa9a4f6b4c2b0d2d5a6a463037a5db819119d0bff8e079f18e6`.

- `TestResults/ChainLinkPublicSourceCurrent/cql-console.log`: 1341bytes; SHA256 `779dce6785c7a0e4bb8d984911ae896ba8f41d4a75625c740a76ddcb00520a61`.

- `TestResults/ChainLinkPublicSourceCurrent/core-admission.trx`: 3318bytes; SHA256 `50a05f2208b734540b9919598462d50205b19d311a5a7dc1474c53d7a8cf6e70`.

- `TestResults/ChainLinkPublicSourceCurrent/api-admission.trx`: 4965bytes; SHA256 `7d058bea03c1ca9fc0dd6923b29f5d8b0721d1c2186004c7dcbd6d245f3ced19`.

- `TestResults/ChainLinkPublicSourceCurrent/mongo-http-native.trx`: 3377bytes; SHA256 `c58f32561351fbb09b77a79e032e74a03e6e413b479cbb61775d583982831194`.

- `TestResults/ChainLinkPublicSourceCurrent/prepared-planner.trx`: 3341bytes; SHA256 `1379e9565e6d3fea724074d66e02ad39385f731d6edb10fef6cc0c65b0548512`.

- `TestResults/ChainLinkPublicSourceCurrent/chainlink-mongo-native-summary.json`: 231bytes; SHA256 `e8c9287829e7bf39f858b1378c6e701b2f027ce5b146b26feca3faeaa335ef5f`.

- `TestResults/ChainLinkPublicSourceCurrent/prepared-public-review.json`: 4315bytes; SHA256 `4c6709d283bb8e8f52b815e045ab71bb7e6a77721073af4f49bd1b2ed97b15b9`.

Frontend97/97regression tests pass with the unpublished ChainLink card; guide includes native and paired HTTP examples. No hosted acceptance claimed. `TestResults/ChainLinkPublicSourceCurrent/client-tests.log`: 9350bytes/SHA256 `681e45e40b784ea04c150712d8b360371cdf5d4ba9413b24d8c7ba9b11f5f89b`.
