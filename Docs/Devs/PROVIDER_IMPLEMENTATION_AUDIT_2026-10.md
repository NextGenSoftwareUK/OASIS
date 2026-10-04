# Provider Implementation Audit — October 2026

_Audit date: 2026-10-03 · Scope: all 222 working providers in `Providers/` (see `Docs/Provider-Summary.md`)_

This audit checks whether every provider is a real implementation on its vendor's API, SDK or client — no stubs, fake success, placeholders or "simplified" shortcuts. It combines a static scan of all 13,880 `override` methods with a manual review of every flagged provider.

## Method

| Check | How |
|---|---|
| Builds | `dotnet build` of every provider project (231 projects, including test harnesses) |
| Fake success | `override` methods that set a successful `Result` with no I/O and no delegation (e.g. `SaveHolon` returning the input, `Delete*` returning `true`, `LoadAvatar(Guid)` returning `new Avatar { Id = id }`) |
| Not supported | `override` methods that only return a "not supported" error |
| Partial implementation | Code comments containing *simplified*, *for now*, *placeholder*, *in production*, *real implementation would*, *mock* |
| Dangerous patterns | Key derivation, hard-coded amounts, fake hashes/selectors, hard-coded data |
| Interface fit | Providers declaring `IOASISBlockchainStorageProvider` / `IOASISNETProvider` but stubbing every method in them |

Static signals were then verified by reading the code. False positives (e.g. SQLite and Mongo methods delegating to repositories) were discarded.

## Headline results

| Result | Count |
|---|---|
| Providers that build | 222 / 222 (one test harness fails: `PinataOASIS.TestHarness`, `IHolon.ProviderKey` no longer exists) |
| Providers with no stub signals | 83 |
| Methods that fake success | **531** across **41** providers |
| Methods that return "not supported" | 696 |
| Partial-implementation comments | 301 across 41 providers |
| Providers declaring interfaces they stub entirely | 23 |
| Map providers whose `IOASISMapProvider` surface is fake | 8 of 10 (every draw/pan/zoom method `return true`) |

### Correction to earlier claims

Earlier commits in this work stream described the 12 Network data providers, the AI/Blockchain oracle providers, and Dapr/Temporal/IntelOpenVINO as "fully implemented". They are not: they still fake success on writes and Guid lookups (e.g. `AnkrOASIS.SaveHolonAsync` returns the holon unchanged, `DeleteAvatarAsync` returns `true`). They are included in the findings below.

## Findings by severity

### P0 — Security and funds (fix first)

These can leak keys, move the wrong amount, or send funds to wrong addresses.

| Provider | Location | Problem |
|---|---|---|
| BaseOASIS | `BaseOASIS.cs:85` | Wallet address derived as `"0x" + privateKey.Substring(0, 40)` — exposes half the private key as the public address and produces a wrong address |
| AptosOASIS | `AptosOASIS.LockBalance.cs:561, 605, 612` | `publicKeyHex = privateKeyHex` — the private key is used and published as the public key |
| AptosOASIS | `AptosOASIS.LockBalance.cs:99, 186`, `SendTransToken.cs:472, 561` | Lock/unlock/mint/burn hard-code amount `"1"` instead of the requested amount |
| AptosOASIS | key derivation | "Simplified" BIP39 word list and hash-based seed derivation instead of BIP39/PBKDF2 + Ed25519 |
| Ethereum, Arbitrum, Web3Core (and every EVM chain inheriting it), Hashgraph, NEAR, EOSIO, Aztec, Holo | `*Token*.cs` | Mint/burn/lock/unlock use `amount = 1m` instead of the request |
| BNBChainOASIS | `BNBChainOASIS_Legacy.Bridge.cs:190` | ABI function selector computed with `string.GetHashCode()` instead of Keccak-256 — every encoded call is wrong |
| CardanoOASIS | address helpers | "Simplified bech32" encoding — invalid or wrong addresses |
| BlockStackOASIS | `GetTransNFT.cs:151` | Stacks address built by string substitution (`"SP" + address.Substring(1)`) instead of c32check |
| BlockStackOASIS | `BlockStackClient.cs:54` | Returns hard-coded users `"user1", "user2", "user3"` |
| RadixOASIS | 5 call sites | Transaction-intent nonce from `System.Random` — use `RandomNumberGenerator` |

### P0 — Chain write paths that cannot produce a valid transaction

A follow-up review of every chain provider's transaction code found that most non-EVM providers either POST unsigned transactions (which every network rejects), sign with the wrong scheme, or hand-roll encoding without the chain's SDK. EVM chains on Nethereum are not affected.

| Provider | Finding | Fix with |
|---|---|---|
| AptosOASIS | Most submissions unsigned; the one signer uses ECDSA/SHA-256 (Aptos needs Ed25519); public key = SHA-256(private key); address = SHA-256 of the key's hex text (Aptos uses SHA3-256(pubkey‖0x00)); random private key with an unrelated seed phrase; `gas_unit_price = "1"` (below the network minimum); mints from `0x1` | BIP39 + SLIP-0010 `m/44'/637'/0'/0'/0'` (Solnet.Wallet), Ed25519 (Chaos.NaCl), REST `encode_submission` + signed submit |
| BitcoinOASIS | 10 transaction POSTs, no signing, no Bitcoin library | NBitcoin |
| ElrondOASIS (MultiversX) | 16 transaction POSTs, no signing | Ed25519 + MultiversX transaction JSON spec |
| AlgorandOASIS, HashgraphOASIS | POST transactions without signing | Algorand4, Hashgraph SDKs |
| StellarOASIS, XRPLOASIS, StarknetOASIS, ZcashOASIS, MidenOASIS | No chain SDK | stellar-dotnet-sdk, Xrpl; Starknet/Zcash/Miden need their RPC + signing specs |
| CardanoOASIS, CosmosBlockChainOASIS, PolkadotOASIS, NEAROASIS | Hand-rolled signing/encoding ("simplified bech32", Nethereum signer for Cosmos) | CardanoSharp.Wallet, Substrate.NET.API, Cosmos protobuf + secp256k1, NEAR Borsh + Ed25519 |
| TONOASIS | Runs on the EVM base; TON is not EVM | TON SDK (TonClient / Net.Ton.Sdk) |

None of these can be verified without funded testnet accounts. Each rewrite is compile-verified here; network verification needs testnet keys in CI secrets (recommendation 6).

### P1 — Fake success (531 methods, 41 providers)

A method that reports success without doing anything is worse than an error: callers (HyperDrive replication, failover, the WebAPI) believe data was saved or deleted. Every one of these must either do the real operation or return an explicit `OASISResult` error.

| Group | Providers | Typical fakes |
|---|---|---|
| Network data APIs | Ankr, Blockscout, Covalent, Dune Analytics, Goldsky, Moralis Streams, Nansen, QuickNode, Reservoir, SubQuery, Tenderly, Zapper | `SaveHolon`/`SaveAvatar` return input; `Delete*` return `true`; `LoadAvatar(Guid)`/`LoadHolon(Guid)` fabricate objects |
| Cross-chain bridges | Axelar, Chainflip, Connext, deBridge, Hyperlane, LayerZero V2, Stargate, Synapse, Wormhole | Same, plus every bridge/token method "not supported" — the bridge functionality itself is missing |
| ZK / proof-of-personhood identity | Holonym, Polygon ID, Proof of Humanity, Reclaim Protocol, Self Protocol, zkPass, Civic | Same; no proof verification implemented |
| Edge storage | Vercel KV, Netlify Blobs, Fly.io, Deno Deploy, Tigris, Nile, Fastly | Storage providers that do not store anything |
| Orchestration / AI runtime | Dapr, Temporal, Intel OpenVINO, NATS JetStream | Same |
| Other | Fhenix, Solana (3 methods) | Same |

The 154-line bridge, identity and edge-storage providers are generated templates: an `HttpClient` is created but never used.

### P2 — Wrong platform or fake interface surface

| Provider(s) | Problem | Recommendation |
|---|---|---|
| TONOASIS | Built on `Web3CoreOASISBaseProvider` as "TON EVM". TON runs the TON Virtual Machine, not EVM, so this cannot talk to TON | Reimplement on the TON HTTP API (toncenter / TonAPI) with TON wallet contracts |
| GoogleMaps, HERE Maps, MapLibre, Niantic Lightship, Mapbox, GO Map, WRLD3D | Every `IOASISMapProvider` method (`DrawRouteOnMap`, `ZoomMapIn`, `PlaceHolonOnMap`, …) is `return true`. These are exposed by `/api/map/*` and the gRPC map service, which therefore report success for nothing | **Design decision needed** — see "Map provider redesign" below |
| 23 providers (bridges, identity, edge storage, Dapr, Temporal, OpenVINO, NATS) | Declare `IOASISBlockchainStorageProvider` and/or `IOASISNETProvider` and stub every method in them | Remove interfaces that do not apply; implement the ones that do (bridges *should* implement the bridge operations) |
| XRPL, Optimism, Fantom | 58 / 41 / 28 methods "not supported" despite declaring storage and blockchain interfaces | Implement on the chain (Optimism and Fantom can inherit Web3Core like the other EVM chains) |
| Avalanche, Aztec, Bitcoin | Search, metadata and parent queries "return all holons" or an empty list because there is no on-chain index | Add an indexer (The Graph / Goldsky subgraph or explorer API) for queries |

### P3 — Partial implementations flagged by their own comments

301 comments admit a shortcut. Heaviest: Zcash 27, Cosmos 21, Aptos 21, EOSIO 19, TRON 17, Aztec 17, Starknet 15, Polkadot 15, Ethereum 14, Avalanche 14, Cardano 10, Arbitrum 10. Typical: NFT metadata not read from the contract, `FromWalletAddress = string.Empty`, "return a single avatar as an example", key derivation without the chain SDK. Each needs the real SDK call in place of the shortcut.

### P4 — Quality

- `PinataOASIS.TestHarness` does not compile (`IHolon.ProviderKey` removed from Core).
- `HoloOASIS` has 13 TODOs and 22 methods that construct a result without setting it.
- `AzureCosmosDBOASIS` has 21 "TODO HB" notes about non-standard error handling.
- `OrionProtocolOASIS` and the read-only Network providers have no avatar concept; the `IOASISStorageProvider` contract forces ~60 "not supported" methods on them. Recommendation below.
- No provider has integration tests that run against the real API in CI, which is how fakes went unnoticed.

## Recommendations

1. **Make fake success impossible.** Add a CI check (the scan used for this audit, `Scripts/audit_providers.pl`) that fails on any `override` returning success without I/O, and on the dangerous patterns above. This stops regressions while the backlog is worked down.
2. **Split the storage contract.** Introduce a read-only capability (`IOASISReadOnlyDataProvider` or a `ProviderCapabilities.ReadOnly` flag) so analytics, oracle and DEX providers don't have to implement 60 write methods. HyperDrive should never route writes to them.
3. **Bridges should actually bridge.** Implement `IOASISBridge` (deposit, withdraw, status) for Axelar, Chainflip, Connext, deBridge, Hyperlane, LayerZero V2, Stargate, Synapse and Wormhole on their public quote/route/status APIs, with signing delegated to the existing chain providers via KeyManager.
4. **Use real SDKs for crypto.** Use NBitcoin (BIP39/BIP32), the Chaos.NaCl or BouncyCastle Ed25519, Nethereum ABI encoding and Keccak, a bech32 library and c32check — never hand-rolled "simplified" versions.
5. **Map provider redesign.** Server-side providers cannot draw on a client's screen. Recommended: map providers own a map *session state* (camera, layers, markers, routes) and compute routes, geocoding and tiles through the vendor's API. They return that state to the client to render, instead of `bool`. This changes `IOASISMapProvider` in the private Core submodule and needs sign-off.
6. **Real-API integration tests.** One smoke test per provider against a sandbox or testnet, run nightly with keys from CI secrets.
7. **Report counts from code.** Generate `Provider-Summary.md` counts from the build, not by hand.

## Remediation plan

| Phase | Work | Providers |
|---|---|---|
| 1 | P0 security and funds fixes | Base, Aptos, BNB, Cardano, BlockStack, Radix, plus the hard-coded amount sites |
| 2 | Remove every fake success (real operation or explicit error) and every non-applicable interface | 41 providers |
| 3 | Implement real APIs for the template providers | 9 bridges, 7 identity, 7 edge storage, Dapr, Temporal, OpenVINO, NATS, Fhenix |
| 4 | Replace "simplified" code with SDK calls | 41 providers with partial-implementation comments |
| 5 | Wrong-platform rewrites | TON; XRPL, Optimism, Fantom completeness |
| 6 | Map provider redesign (after sign-off) | 8 map providers |
| 7 | CI audit gate and real-API smoke tests | all |

Progress is tracked in the change history of `Docs/Provider-Summary.md`.

## Per-provider scan results

Sorted worst first. *Fake* = methods faking success; *Not supported* = methods that only return an error; *Shortcuts* = partial-implementation comments; *SDK pkgs* = third-party package references.

| Folder | Provider | Lines | Overrides | Fake | Not supported | Shortcuts | SDK pkgs |
|---|---|---|---|---|---|---|---|
| Other | IntelOpenVINOOASIS | 175 | 68 | 20 | 6 | 0 | 0 |
| Other | TemporalOASIS | 176 | 68 | 20 | 6 | 0 | 1 |
| Blockchain | XRPLOASIS | 646 | 68 | 0 | 58 | 3 | 0 |
| Other | OrionProtocolOASIS | 252 | 68 | 0 | 58 | 0 | 0 |
| Identity | ProofOfHumanityOASIS | 237 | 66 | 15 | 11 | 0 | 0 |
| Blockchain | OptimismOASIS | 3631 | 68 | 0 | 41 | 7 | 4 |
| Blockchain | ZcashOASIS | 3063 | 68 | 0 | 0 | 27 | 0 |
| Network | ReclaimProtocolOASIS | 253 | 66 | 13 | 14 | 0 | 0 |
| Network | AnkrOASIS | 224 | 68 | 15 | 6 | 0 | 0 |
| Network | BlockscoutOASIS | 253 | 68 | 15 | 6 | 0 | 0 |
| Blockchain | AxelarOASIS | 240 | 66 | 13 | 11 | 0 | 0 |
| Blockchain | ChainflipOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Blockchain | StargateOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Blockchain | SynapseOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Blockchain | WormholeOASIS | 281 | 66 | 13 | 11 | 0 | 0 |
| Blockchain | deBridgeOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Identity | HolonymOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Identity | PolygonIDOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Identity | SelfProtocolOASIS | 269 | 66 | 13 | 11 | 0 | 0 |
| Identity | zkPassOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Network | CovalentOASIS | 244 | 68 | 15 | 5 | 0 | 0 |
| Network | GoldskyOASIS | 241 | 68 | 15 | 5 | 0 | 0 |
| Network | NansenOASIS | 214 | 68 | 15 | 5 | 0 | 0 |
| Network | QuickNodeOASIS | 227 | 68 | 15 | 5 | 0 | 0 |
| Network | ZapperOASIS | 235 | 68 | 15 | 5 | 0 | 0 |
| Storage | DenoDeployOASIS | 245 | 66 | 13 | 11 | 0 | 0 |
| Storage | FlyIOOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Storage | NetlifyBlobsOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Storage | VercelKVOASIS | 154 | 66 | 13 | 11 | 0 | 0 |
| Blockchain | ConnextOASIS | 206 | 66 | 13 | 10 | 0 | 0 |
| Blockchain | HyperlaneOASIS | 206 | 66 | 13 | 10 | 0 | 0 |
| Blockchain | LayerZeroV2OASIS | 206 | 66 | 13 | 10 | 0 | 0 |
| Network | CivicOASIS | 281 | 66 | 11 | 15 | 0 | 0 |
| Network | DuneAnalyticsOASIS | 255 | 68 | 14 | 6 | 0 | 0 |
| Other | DaprOASIS | 199 | 68 | 14 | 6 | 0 | 1 |
| Network | ReservoirOASIS | 245 | 68 | 14 | 5 | 0 | 0 |
| Network | TenderlyOASIS | 266 | 68 | 14 | 5 | 0 | 0 |
| Network | SubqueryOASIS | 221 | 68 | 14 | 4 | 0 | 0 |
| Blockchain | AptosOASIS | 4785 | 68 | 0 | 0 | 21 | 1 |
| Blockchain | CosmosBlockChainOASIS | 4374 | 68 | 0 | 0 | 21 | 1 |
| Network | MoralisStreamsOASIS | 260 | 68 | 12 | 5 | 0 | 0 |
| Blockchain | EOSIOOASIS | 9371 | 93 | 0 | 0 | 19 | 7 |
| Blockchain | FhenixOASIS | 333 | 66 | 7 | 17 | 0 | 0 |
| Storage | FastlyOASIS | 335 | 66 | 7 | 17 | 0 | 0 |
| Network | NATSJetStreamOASIS | 300 | 66 | 9 | 10 | 0 | 1 |
| Storage | NileOASIS | 353 | 66 | 9 | 10 | 0 | 1 |
| Storage | TigrisOASIS | 354 | 66 | 9 | 10 | 0 | 1 |
| Blockchain | FantomOASIS | 3430 | 68 | 0 | 28 | 4 | 4 |
| Blockchain | AztecOASIS | 3628 | 68 | 0 | 0 | 17 | 1 |
| Blockchain | TRONOASIS | 4456 | 68 | 0 | 0 | 17 | 2 |
| Blockchain | PolkadotOASIS | 3743 | 68 | 0 | 0 | 15 | 0 |
| Blockchain | StarknetOASIS | 3395 | 68 | 0 | 0 | 15 | 0 |
| Blockchain | AvalancheOASIS | 4027 | 68 | 0 | 0 | 14 | 4 |
| Blockchain | EthereumOASIS | 5142 | 68 | 0 | 0 | 14 | 4 |
| Blockchain | SOLANAOASIS | 5762 | 74 | 3 | 0 | 7 | 7 |
| Identity | ENSOffchainOASIS | 416 | 68 | 0 | 23 | 0 | 0 |
| Blockchain | ArbitrumOASIS | 4614 | 68 | 0 | 0 | 10 | 4 |
| Blockchain | CardanoOASIS | 4384 | 68 | 0 | 0 | 10 | 1 |
| Maps | DecentralandOASIS | 426 | 68 | 0 | 19 | 0 | 0 |
| Maps | TheSandboxOASIS | 430 | 68 | 0 | 19 | 0 | 0 |
| Blockchain | MidenOASIS | 2949 | 68 | 0 | 0 | 9 | 0 |
| Blockchain | SuiOASIS | 3659 | 68 | 0 | 0 | 9 | 3 |
| Blockchain | BNBChainOASIS | 4323 | 68 | 0 | 0 | 8 | 4 |
| Blockchain | BlockStackOASIS | 4706 | 68 | 0 | 0 | 7 | 0 |
| Blockchain | GelatoNetworkOASIS | 226 | 68 | 0 | 12 | 0 | 0 |
| Network | HoloOASIS | 5083 | 68 | 0 | 0 | 6 | 0 |
| Maps | ReadyPlayerMeOASIS | 553 | 68 | 0 | 11 | 0 | 0 |
| AI | RitualOASIS | 489 | 68 | 0 | 10 | 0 | 0 |
| Blockchain | BaseOASIS | 3901 | 68 | 0 | 0 | 5 | 4 |
| AI | BittensorOASIS | 426 | 68 | 0 | 9 | 0 | 0 |
| Blockchain | Web3CoreOASIS | 4657 | 68 | 0 | 3 | 3 | 4 |
| Blockchain | ChainlinkFunctionsOASIS | 248 | 68 | 0 | 8 | 0 | 0 |
| Blockchain | EspressoSystemsOASIS | 228 | 68 | 0 | 8 | 0 | 0 |
| Blockchain | HashgraphOASIS | 4246 | 68 | 0 | 0 | 4 | 1 |
| Blockchain | NEAROASIS | 4864 | 68 | 0 | 0 | 4 | 0 |
| Blockchain | TelosOASIS | 3848 | 74 | 0 | 0 | 4 | 0 |
| Identity | PrivyServerWalletsOASIS | 641 | 68 | 0 | 8 | 0 | 0 |
| Blockchain | RadixOASIS | 4456 | 68 | 0 | 0 | 3 | 2 |
| Other | PLANOASIS | 2256 | 68 | 0 | 0 | 0 | 0 |
| AI | GaladrielOASIS | 452 | 68 | 0 | 5 | 0 | 0 |
| Blockchain | BitcoinOASIS | 4296 | 68 | 0 | 0 | 2 | 0 |
| Blockchain | EigenLayerOASIS | 267 | 68 | 0 | 4 | 0 | 0 |
| Blockchain | ElrondOASIS | 3612 | 68 | 0 | 0 | 2 | 0 |
| Blockchain | OpenZeppelinDefenderOASIS | 386 | 68 | 0 | 4 | 0 | 0 |
| Network | IPFSOASIS | 1100 | 68 | 0 | 0 | 2 | 5 |
| Storage | QdrantOASIS | 401 | 68 | 0 | 0 | 2 | 0 |
| Blockchain | ChainLinkOASIS | 2333 | 68 | 0 | 0 | 1 | 2 |
| Cloud | AWSOASIS | 1875 | 68 | 0 | 0 | 1 | 0 |
| Network | SOLIDOASIS | 1023 | 68 | 0 | 0 | 1 | 0 |
| Network | ThreeFoldOASIS | 3463 | 68 | 0 | 0 | 1 | 0 |
| Storage | LanceDBOASIS | 917 | 68 | 0 | 0 | 1 | 0 |
| Storage | MilvusOASIS | 928 | 68 | 0 | 0 | 1 | 0 |
| Storage | MongoOASIS | 7595 | 74 | 0 | 0 | 1 | 4 |
| Storage | PineconeOASIS | 899 | 68 | 0 | 0 | 1 | 0 |
| Storage | SQLLiteDBOASIS | 12123 | 74 | 0 | 0 | 1 | 4 |
| Storage | ZillizOASIS | 927 | 68 | 0 | 0 | 1 | 0 |
| Blockchain | AbstractOASIS | 32 | 0 | 0 | 0 | 0 | 4 |
| Blockchain | AlgorandOASIS | 594 | 68 | 0 | 0 | 0 | 0 |
| Blockchain | BasechainOASIS | 593 | 68 | 0 | 0 | 0 | 0 |
| Blockchain | BerachainOASIS | 32 | 0 | 0 | 0 | 0 | 4 |
| Blockchain | CeramicOASIS | 685 | 68 | 0 | 0 | 0 | 0 |
| Blockchain | FilecoinOASIS | 659 | 68 | 0 | 0 | 0 | 0 |
| Blockchain | LineaOASIS | 32 | 0 | 0 | 0 | 0 | 4 |
| Blockchain | MonadOASIS | 32 | 0 | 0 | 0 | 0 | 4 |
| Blockchain | MoralisOASIS | 3041 | 68 | 0 | 0 | 0 | 2 |
| Blockchain | PolygonOASIS | 21 | 0 | 0 | 0 | 0 | 0 |
| Blockchain | RootstockOASIS | 20 | 0 | 0 | 0 | 0 | 0 |
| Blockchain | ScrollOASIS | 32 | 0 | 0 | 0 | 0 | 4 |
| Blockchain | SeiOASIS | 32 | 0 | 0 | 0 | 0 | 4 |
| Blockchain | StellarOASIS | 230 | 68 | 0 | 0 | 0 | 0 |
| Blockchain | TONOASIS | 19 | 0 | 0 | 0 | 0 | 0 |
| Blockchain | ZkSyncOASIS | 32 | 0 | 0 | 0 | 0 | 4 |
| Cloud | AppwriteOASIS | 475 | 68 | 0 | 0 | 0 | 0 |
| Cloud | AzureCosmosDBOASIS | 1892 | 61 | 0 | 0 | 0 | 1 |
| Cloud | AzureStorageOASIS | 352 | 68 | 0 | 0 | 0 | 1 |
| Cloud | CloudflareD1OASIS | 363 | 68 | 0 | 0 | 0 | 0 |
| Cloud | CloudflareOASIS | 536 | 68 | 0 | 0 | 0 | 0 |
| Cloud | ConvexOASIS | 343 | 68 | 0 | 0 | 0 | 0 |
| Cloud | DynamoDBOASIS | 435 | 68 | 0 | 0 | 0 | 1 |
| Cloud | FaunaOASIS | 381 | 68 | 0 | 0 | 0 | 0 |
| Cloud | FirebaseOASIS | 538 | 68 | 0 | 0 | 0 | 0 |
| Cloud | GoogleCloudOASIS | 3168 | 68 | 0 | 0 | 0 | 4 |
| Cloud | NeonOASIS | 319 | 68 | 0 | 0 | 0 | 1 |
| Cloud | PocketBaseOASIS | 451 | 68 | 0 | 0 | 0 | 0 |
| Cloud | SupabaseOASIS | 504 | 68 | 0 | 0 | 0 | 0 |
| Cloud | TursoOASIS | 456 | 68 | 0 | 0 | 0 | 0 |
| Cloud | UpstashOASIS | 348 | 68 | 0 | 0 | 0 | 0 |
| Maps | GOMapOASIS | 1539 | 3 | 0 | 0 | 0 | 0 |
| Maps | GoogleMapsOASIS | 78 | 0 | 0 | 0 | 0 | 0 |
| Maps | HEREMapsOASIS | 78 | 0 | 0 | 0 | 0 | 0 |
| Maps | MapLibreOASIS | 78 | 0 | 0 | 0 | 0 | 0 |
| Maps | MapboxOASIS | 1615 | 1 | 0 | 0 | 0 | 0 |
| Maps | NianticLightshipOASIS | 105 | 0 | 0 | 0 | 0 | 0 |
| Maps | WRLD3DOASIS | 331 | 0 | 0 | 0 | 0 | 0 |
| Network | ActivityPubOASIS | 1722 | 68 | 0 | 0 | 0 | 0 |
| Network | AkashOASIS | 229 | 68 | 0 | 0 | 0 | 0 |
| Network | AlchemyOASIS | 230 | 68 | 0 | 0 | 0 | 0 |
| Network | CelestiaOASIS | 241 | 68 | 0 | 0 | 0 | 0 |
| Network | ENSOASIS | 244 | 68 | 0 | 0 | 0 | 0 |
| Network | EclipseOASIS | 223 | 68 | 0 | 0 | 0 | 0 |
| Network | GitcoinPassportOASIS | 261 | 68 | 0 | 0 | 0 | 0 |
| Network | InfuraOASIS | 266 | 68 | 0 | 0 | 0 | 0 |
| Network | LayerZeroOASIS | 241 | 68 | 0 | 0 | 0 | 0 |
| Network | LivepeerOASIS | 252 | 68 | 0 | 0 | 0 | 0 |
| Network | PinataOASIS | 1905 | 68 | 0 | 0 | 0 | 0 |
| Network | PolybaseOASIS | 313 | 68 | 0 | 0 | 0 | 0 |
| Network | PrivyOASIS | 297 | 68 | 0 | 0 | 0 | 0 |
| Network | PushProtocolOASIS | 205 | 68 | 0 | 0 | 0 | 0 |
| Network | SafeOASIS | 228 | 68 | 0 | 0 | 0 | 0 |
| Network | ScuttlebuttOASIS | 842 | 68 | 0 | 0 | 0 | 0 |
| Network | SuiZkLoginOASIS | 238 | 68 | 0 | 0 | 0 | 0 |
| Network | TablelandOASIS | 216 | 68 | 0 | 0 | 0 | 0 |
| Network | TelegramOASIS | 1732 | 68 | 0 | 0 | 0 | 2 |
| Network | WakuOASIS | 222 | 68 | 0 | 0 | 0 | 0 |
| Network | ZkSyncSSOOASIS | 245 | 68 | 0 | 0 | 0 | 0 |
| Other | GUNOASIS | 508 | 68 | 0 | 0 | 0 | 0 |
| Other | ONION-Protocol | 372 | 68 | 0 | 0 | 0 | 0 |
| Other | OrbitDBOASIS | 460 | 68 | 0 | 0 | 0 | 0 |
| Other | SEEDSOASIS | 1532 | 4 | 0 | 0 | 0 | 0 |
| Other | UrbitOASIS | 1066 | 68 | 0 | 0 | 0 | 0 |
| Social | BlueSkyOASIS | 850 | 68 | 0 | 0 | 0 | 0 |
| Social | DiscordOASIS | 540 | 68 | 0 | 0 | 0 | 0 |
| Social | FarcasterOASIS | 615 | 68 | 0 | 0 | 0 | 0 |
| Social | LensOASIS | 982 | 68 | 0 | 0 | 0 | 0 |
| Social | LensV2OASIS | 267 | 68 | 0 | 0 | 0 | 0 |
| Social | LitProtocolOASIS | 445 | 68 | 0 | 0 | 0 | 0 |
| Social | LoomOASIS | 952 | 68 | 0 | 0 | 0 | 0 |
| Social | MatrixOASIS | 813 | 68 | 0 | 0 | 0 | 0 |
| Social | NostrOASIS | 764 | 68 | 0 | 0 | 0 | 0 |
| Social | StoryProtocolOASIS | 417 | 68 | 0 | 0 | 0 | 0 |
| Social | TheGraphOASIS | 409 | 68 | 0 | 0 | 0 | 0 |
| Social | WorldIDOASIS | 391 | 68 | 0 | 0 | 0 | 0 |
| Storage | AlgoliaOASIS | 629 | 68 | 0 | 0 | 0 | 0 |
| Storage | ArangoDBOASIS | 631 | 68 | 0 | 0 | 0 | 0 |
| Storage | ArcadeDBOASIS | 914 | 68 | 0 | 0 | 0 | 0 |
| Storage | ArweaveOASIS | 1773 | 68 | 0 | 0 | 0 | 0 |
| Storage | BigQueryOASIS | 887 | 68 | 0 | 0 | 0 | 1 |
| Storage | CassandraOASIS | 375 | 68 | 0 | 0 | 0 | 1 |
| Storage | ChromaOASIS | 929 | 68 | 0 | 0 | 0 | 0 |
| Storage | ClickHouseOASIS | 722 | 68 | 0 | 0 | 0 | 1 |
| Storage | CloudinaryOASIS | 718 | 68 | 0 | 0 | 0 | 0 |
| Storage | CockroachDBOASIS | 349 | 68 | 0 | 0 | 0 | 1 |
| Storage | CouchDBOASIS | 935 | 68 | 0 | 0 | 0 | 0 |
| Storage | CouchbaseOASIS | 360 | 68 | 0 | 0 | 0 | 1 |
| Storage | DatabricksOASIS | 904 | 68 | 0 | 0 | 0 | 0 |
| Storage | DragonflyOASIS | 903 | 68 | 0 | 0 | 0 | 1 |
| Storage | DruidOASIS | 920 | 68 | 0 | 0 | 0 | 0 |
| Storage | DuckDBOASIS | 910 | 68 | 0 | 0 | 0 | 1 |
| Storage | ElasticsearchOASIS | 399 | 68 | 0 | 0 | 0 | 0 |
| Storage | GarnetOASIS | 903 | 68 | 0 | 0 | 0 | 1 |
| Storage | InfluxDBOASIS | 344 | 68 | 0 | 0 | 0 | 1 |
| Storage | KeyDBOASIS | 596 | 68 | 0 | 0 | 0 | 1 |
| Storage | LitestreamOASIS | 623 | 68 | 0 | 0 | 0 | 1 |
| Storage | LocalFileOASIS | 2267 | 76 | 0 | 0 | 0 | 0 |
| Storage | MarqoOASIS | 895 | 68 | 0 | 0 | 0 | 0 |
| Storage | MeilisearchOASIS | 657 | 68 | 0 | 0 | 0 | 0 |
| Storage | MemcachedOASIS | 620 | 68 | 0 | 0 | 0 | 1 |
| Storage | MinIOOASIS | 722 | 68 | 0 | 0 | 0 | 1 |
| Storage | MotherDuckOASIS | 912 | 68 | 0 | 0 | 0 | 1 |
| Storage | Neo4jOASIS | 1850 | 68 | 0 | 0 | 0 | 2 |
| Storage | Neo4jOASIS.Aura | 2889 | 68 | 0 | 0 | 0 | 2 |
| Storage | OpenSearchOASIS | 684 | 68 | 0 | 0 | 0 | 0 |
| Storage | OracleDBOASIS | 765 | 68 | 0 | 0 | 0 | 1 |
| Storage | PgVectorOASIS | 923 | 68 | 0 | 0 | 0 | 1 |
| Storage | PinotOASIS | 902 | 68 | 0 | 0 | 0 | 0 |
| Storage | PlanetScaleOASIS | 462 | 68 | 0 | 0 | 0 | 1 |
| Storage | PostgreSQLOASIS | 688 | 68 | 0 | 0 | 0 | 1 |
| Storage | PouchDBOASIS | 698 | 68 | 0 | 0 | 0 | 0 |
| Storage | QuestDBOASIS | 644 | 68 | 0 | 0 | 0 | 1 |
| Storage | RavenDBOASIS | 398 | 68 | 0 | 0 | 0 | 1 |
| Storage | RedisOASIS | 671 | 68 | 0 | 0 | 0 | 1 |
| Storage | RedshiftOASIS | 910 | 68 | 0 | 0 | 0 | 1 |
| Storage | SQLServerDBOASIS | 829 | 68 | 0 | 0 | 0 | 1 |
| Storage | ScyllaDBOASIS | 722 | 68 | 0 | 0 | 0 | 1 |
| Storage | SnowflakeOASIS | 910 | 68 | 0 | 0 | 0 | 1 |
| Storage | SolrOASIS | 634 | 68 | 0 | 0 | 0 | 0 |
| Storage | SurrealDBOASIS | 384 | 68 | 0 | 0 | 0 | 1 |
| Storage | TimescaleDBOASIS | 383 | 68 | 0 | 0 | 0 | 1 |
| Storage | TypesenseOASIS | 682 | 68 | 0 | 0 | 0 | 0 |
| Storage | ValKeyOASIS | 903 | 68 | 0 | 0 | 0 | 1 |
| Storage | WeaviateOASIS | 416 | 68 | 0 | 0 | 0 | 0 |
| Storage | XataOASIS | 870 | 68 | 0 | 0 | 0 | 0 |
