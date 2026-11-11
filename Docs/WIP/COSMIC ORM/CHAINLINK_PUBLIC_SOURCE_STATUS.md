# ChainLink public feed source status

Updated1November2026. Scoped hosted HTTP acceptance complete; browser interaction remains unverified. This read-only source is not full ChainLink storage/ORM certification.

## Verified contract

- Official documentation [Ethereum configuration](https://raw.githubusercontent.com/smartcontractkit/documentation/main/src/features/data/chains.ts) identifies [the reference directory](https://reference-data-directory.vercel.app/feeds-mainnet.json). Guarded GET HTTP200/301records/388274B. Exact eth-usd/btc-usd/link-usd proxy entries selected, excluding same-name SVR variants; original selected catalogue/digest retained.
- Proxy addresses: ETH `0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419`; BTC `0xf4030086522a5beea4988f8ca5b36dbc97bee88c`; LINK `0x2c1d072e956affc0d435cb7ac38ef18d24d9127c`; catalogue/native scales8.
- Original protocol18guarded HTTP200 reads: Ethereum chain1/genesis, finalized header, hash-bound descriptions/decimals/rounds for3feeds then2. Offline ABI/transport check passes with zero native requests. No wallet/account/node/install/funding/source transaction.
- Separate read-only CQL source43controlled tests pass; actual C# CLI/manager/LINQ3/2 pass. Exact signed int256 maximum/minimum/-1, uint80 rounds above2^53, timestamps and deprecated answeredInRound0 preserved without floats. Invalid ABI/identity/status/size/cancellation/budget fail without partial rows; native saves rejected. Legacy6unimplemented throws unchanged/documented only.
- Core permission1/API admission2/localHTTP1/publisher10/prepared-planner1/client preparation1 pass. Local native3/2, Mongo5/4, privacy0/3,2SDK version1 saves/9query receipts verified. Exact marked local database dropped, HTTP/Mongo stopped; original journal retained.

## Hosted release and acceptance

- Development parent `09c66998cb2c204f290d7a85198006e6abd3202f` / Core `990c1c75afcafdbc0678b6e8131a10b67650f7e3` pushed/ref-verified; manifest validates. Code job `215b4b51-4431-4292-9c12-1063497d5773` SUCCESS.
- Exactly3settings applied once: source, independent unsigned-read permission/free tariff,4presets. Whole candidate/per-key digests match, unrelated DNA unchanged. Single settings job `3a00f63f-c286-45bc-b0ee-baeccc7e9a02` SUCCESS; no reapply/redeploy.
- Original hosted cohort: native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3, matching SQLite/Neo4j replicas2/2 each. Two normal private saves/version1, no pending preparation intents. Immutable completion ACK freezes cohort; no replay/resave/new visitor for this completed gate.
- Website main/task `80633dc5e49975eb97769ea1205552f38b848e6e` published/ref-verified;97client tests pass. Public HTML169120B/SHA256 `60736fa4c027c6acf6f7ab80d1f4cc4c588d18416b907ca2740729defae767a6` and both unchanged JS assets match exact git blobs (HTTP200). HTTP/asset proof does not establish browser interaction.
- [Setup/C#/HTTP guide](../../Devs/CHAINLINK_PUBLIC_FEED_CQL_GUIDE.md) pinned3ec0dbac5; [commented walkthrough](../../../Examples/CosmicCqlDemo/ChainLinkPublicWalkthrough.cs) pinned09c66998c. Four native/Mongo presets available in discovery/CQL tab.

## Limits and remaining work

Every query resolves its own RPC-reported finalized Ethereum block and hash-binds selected calls with EIP1898 requireCanonical. This is not independent consensus, a current-price guarantee or a cross-query/provider snapshot. Original qualification block0xc3af2867f720230178891d37d162f83fd50a6a7f0160d4d72bbbb79c96a823d3/0x191ad93 had per-feed update ages ETH3372s/BTC372s/LINK276s. Preserve raw update times; answeredInRound is deprecated, not a freshness proof. Generic browser Mongo copies retain identity/name/description/provenance, not all native feed metadata; full payload mapping remains red. Native signing/writes/full ORM and browser interaction remain separate gaps.

Next: preserve this green scope and implement the qualified BlueSky public AppView source described in BLUESKY_PUBLIC_SOURCE_STATUS.md. Work red scopes only; do not resume paused deep terminals or harden green providers absent a reproduced bug.

## Durable resources

Stable catalogue/native ACKs: LocalState/COSMIC/HostedChainLinkPublicSourceCurrent. Cleaned local fixture journal: ChainLinkMongoHttpCurrent. Protected baseline/candidate/key ACKs/redeploy intent/outcome: HostedSourceRolloutCurrent/chainlink-public-*. Public release/job ACKs: HostedChainLinkReleaseCurrent. Frozen protected visitors/save ACKs/completion ACK: HostedQueryAcceptance-ChainLinkOASIS. All paths are under worktree LocalState/COSMIC. Preserve owners/ACKs through reviewed expiry; never print credentials or regenerate/replay cohorts.

Raw reports remain in worktree TestResults/, not Docs. Generated unit binaries50.6MB remain recorded after automatic cleanup review rejected deletion; no bypass attempted.

## Evidence hashes

- `TestResults/ChainLinkPublicSourceCurrent/mainnet-catalogue.json`: 388274bytes; SHA256 `cf13ccee5ebd84a6427f25e490f77ad88a991cc040c655d55088c404ab1bd0d4`.
- `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/selected-catalogue.json`: 4405bytes; SHA256 `b5d35fd33654bd4bb379812fb0cbdfa55e06141749905c011d4795c8b11cc7bb`.
- `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/source.json`: 555bytes; SHA256 `2b309aa619a984cac55fb2ea33ede55730e73df85704c44d68e1148521673c5d`.
- `TestResults/ChainLinkPublicSourceCurrent/qualification.json`: 10265bytes; SHA256 `e7e7179a2b8b05d27374e0942eb174131fe490943035471edeacf2fc0ec11d2e`.
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

- `TestResults/HostedCqlIntegrationCurrent/ChainLinkOASIS-native-http.json`: 15692bytes; SHA256 `2751acdd9d92ad1fb77a56743866ebc4b7fb8932c5c6cd0fb3378708cdf17f09`.

- `TestResults/HostedCqlIntegrationCurrent/ChainLinkOASIS-client-http-flow.json`: 40657bytes; SHA256 `a0ab4a795975ab7399389a6cf1d62a0b0523bc177c0385a6b342372dd9a386b9`.

- `TestResults/HostedCqlIntegrationCurrent/ChainLinkOASIS-isolation-http.json`: 6301bytes; SHA256 `3a85c4f2478385ccd3fbdad94019c055e7d7df3bee6255f3e6733839a4c07f0f`.

Native ABI reference: [Chainlink AggregatorV3 API](https://docs.chain.link/data-feeds/api-reference).

11 November UTC: Chainlink public feed observation presets returned three full and two filtered rows in the live Development CQL Playground. Original-owner MongoDB paired browser verification remains open. No preparation or record saves.

11 November UTC: Chainlink's prior public browser acceptance is retained. The configured PublicNode origin pruned Ethereum genesis, causing startup rejection. The dRPC replacement passed exact genesis and all three configured feed checks from Development WEB4. Settings deployment 44149acb is queued. Live activation and browser verification on dRPC remain pending.
