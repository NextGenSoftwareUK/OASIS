# ChainLink public feed source qualification

Updated1November2026. Native protocol qualified; CQL/API/hosted integration remains unimplemented. Existing48integrated/44hosted counts do not include ChainLink.

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

## Exact next step

Implement a separate read-only `ChainLinkPublicFeedMetadataSource` in the existing ChainLink provider project. Use shared CQL interfaces/planning and native mapper, with bounded equality/membership on `ChainLinkFeedAddress`. Preserve raw exact answer/decimals/round/time metadata, explicit feed identities and RPC-reported finality. Validate all selected feeds before publication; fail with OASISResult errors, no partial rows or scans. Keep signed storage separate.

Then controlled tests, native manager/C#3/2, API independent read permission/tariff, local Mongo/privacy/prepared presets, exact hosted rollout and original acceptance/card. No ChainLink CQL/hosted claim until those gates pass. Green providers/deep LINQ parity stay frozen.

## Durable resources and evidence

State: `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent` contains original catalogue/native intents, selected catalogue and stable source owner/config. Never regenerate owner or replay original native qualification. Raw responses/compact reports: worktree `TestResults/ChainLinkPublicSourceCurrent`, not Docs. Scripts: [catalogue](../../../Scripts/Sandbox/Fetch-ChainLinkPublicCatalogue.ps1), [native](../../../Scripts/Sandbox/Probe-ChainLinkPublicReadiness.ps1), [offline check](../../../Scripts/Sandbox/Test-ChainLinkPublicProbe.ps1).

- `TestResults/ChainLinkPublicSourceCurrent/mainnet-catalogue.json`: 388274bytes; SHA256 `cf13ccee5ebd84a6427f25e490f77ad88a991cc040c655d55088c404ab1bd0d4`.
- `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/selected-catalogue.json`: 4405bytes; SHA256 `b5d35fd33654bd4bb379812fb0cbdfa55e06141749905c011d4795c8b11cc7bb`.
- `LocalState/COSMIC/HostedChainLinkPublicSourceCurrent/source.json`: 555bytes; SHA256 `2b309aa619a984cac55fb2ea33ede55730e73df85704c44d68e1148521673c5d`.
- `TestResults/ChainLinkPublicSourceCurrent/qualification.json`: 10265bytes; SHA256 `e7e7179a2b8b05d27374e0942eb174131fe490943035471edeacf2fc0ec11d2e`.

Native ABI reference: [Chainlink API](https://docs.chain.link/data-feeds/api-reference). Data is read via proxy AggregatorV3Interface; decimals must be preserved, answeredInRound is deprecated.
