# Polkadot public header source readiness

31 October 2026. Native source and CQL3/2 verified; C# manager walkthrough, HTTP federation and hosted/browser acceptance remain open.

Python HTTP probes to PublicNode and the existing provider default rpc.polkadot.io returned403 before data. An explicit .NET probe against that same existing default returned200 with the expected Polkadot genesis. The reason for the transport difference is not established; no production fallback/retry/user-agent spoofing was introduced.

Five bounded .NET RPC reads then passed: genesis identity, reported finalized head, three individual headers following parent hashes. Python locally SCALE-encoded each returned header and verified its BLAKE2b-256 hash against the requested identity; parent links and consecutive block numbers match. No block bodies, runtimeWASM, storage scan, transaction, signer or node installation.

This proves header-content identity and the RPC-reported ancestry, not independent GRANDPA finality or state inclusion. It is a planned public native metadata representation, not holons stored on-chain.

Next: implement the explicit Polkadot block-header CQL profile over a retained bounded hash catalogue; use the existing provider/runtime and an existing compatible cryptography dependency after inspecting its references/cache. Add malformed/wrong-network/hash/partial-output tests plus real native3/2 and C# walkthrough, then HTTP+Mongo5/4/isolation, strict hosted presets and client links. No CQL or hosted success is inferred from this probe.

Reproduce without installing anything:

```powershell
pwsh -File Scripts/Sandbox/Test-PolkadotPublicMetadataCandidate.ps1
python Scripts/Sandbox/Probe-PolkadotPublicReadiness.py --validate-dotnet
```

Raw bounded header evidence stays in TestResults, not Docs. The validator is local and makes no network call. Rerunning the network probe overwrites regenerable evidence with a new height; no persisted source identity has been created yet.

Primary contracts: [Polkadot RPC](https://polkadot.js.org/docs/asset-hub-polkadot/rpc/), [native header SCALE field order](https://github.com/paritytech/polkadot-sdk/blob/master/substrate/primitives/runtime/src/generic/header.rs), [Polkadot genesis identity](https://standards.chainagnostic.org/CAIPs/caip-13). The existing provider documents rpc.polkadot.io; HTTPS support was observed directly.

- `TestResults/PolkadotPublicSourceCandidateCurrent/headers-dotnet.json`: 4232 B, SHA256 `ba52ed6505fb7bea43b2a2d4cfb957d9bb5f89d08cccb87122b9654d4c70fa1e`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/readiness-dotnet.json`: 314 B, SHA256 `e0339cbf8e463b05f7d0eb16b2dd62cb53e491056ac570b83f8e2dce7f22235c`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/readiness-official.json`: 271 B, SHA256 `d4f674f9181edc3d9ad3bc0aeff039669983941b155051935fbe9840f4a31427`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/readiness.json`: 283 B, SHA256 `4da1ba959074c5a638134e3bb7f75b68d34f814a35864c50c1f98364bf04c3e7`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/validated-headers.json`: 243 B, SHA256 `b45502db9453c9d216000ee4e2f6eeda689b3488263fddd6795651e89306fd14`.

## Adapter contract under verification

`PolkadotPublicHeaderMetadataSource` is an explicit read representation inside the existing Polkadot provider. Strict schema: schema1/rpcUrl/genesisHash/blockHashes/sourceOwner. One to eight unique canonical lowercase32byte hashes; stable configured mapping owner. Query key PolkadotBlockHash supports point/membership, visibility and paging after complete validation. Source writes are unsupported.

Each selection binds Polkadot genesis, reads and locally hashes the reported finalized header, then verifies each configured selected header's SCALE/BLAKE2b content hash, number not above the reported finalized height and canonical hash at that number. It reads no block bodies, account data, storage scan or runtimeWASM. Canonical/finality assertions remain RPC-reported, not independently verified GRANDPA proofs. Exact block numbers are decimal strings in holon metadata.

Crypto reuses cached BouncyCastle.Cryptography2.7.0, matching the existing BootLoader/Aptos minimum; the initially considered2.6.2 was rejected by NuGet rather than suppressing a downgrade. Controlled vectors independently generated with Python stdlib cover all four SCALE compact-number widths. Source builds;17focused controlled tests and1native test pass, zero skips. Native query returns3configured headers and a real2hash subset. No independent consensus claim.

## Native CQL milestone

New `PolkadotPublicHeaderMetadataTests` verifies all four SCALE compact widths, point/membership, paging/budget, wrong genesis, modified content, noncanonical history, future height, wrong RPC id, duplicates, oversized response, cancellation/inactive/unknown keys, and invalid configuration. A malformed last row fails even with Take(0). The native3/2 test uses retained configured hashes and real rpc.polkadot.io through the guarded HTTP source. No wallet/node/signing/bodies/storage scans.

Persistent `LocalState/COSMIC/HostedPolkadotPublicSourceCurrent/source.json` holds the original fixed3hash catalogue and stable mapping owner; no source reset. Do not regenerate it from newer probe output. The source has no WEB4 hosted enrollment yet. Next: C# HolonManager/LINQ walkthrough and HTTP+Mongo5/4/isolation, then strict hosted configuration and browser links.

- `TestResults/PolkadotPublicSourceCandidateCurrent/provider-controls.trx`: 27614 B; SHA256 `699a394e1122658468f2b3c79db90e19bb466d7f2fa76542d819cceb2a2f981f`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/provider-native.trx`: 2839 B; SHA256 `fd0af2c0ab46e300ce1de3e90c0a9fbe5155211f2d2a94050c2b5b38e07f5108`.
