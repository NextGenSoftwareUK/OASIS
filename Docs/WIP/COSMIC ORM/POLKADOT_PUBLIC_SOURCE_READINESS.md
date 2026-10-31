# Polkadot public header source readiness

31 October 2026. Native source feasibility verified; no CQL adapter, HTTP federation or hosted/browser acceptance yet.

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
