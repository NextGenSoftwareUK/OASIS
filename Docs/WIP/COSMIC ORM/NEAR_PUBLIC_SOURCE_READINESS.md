# NEAR public account CQL readiness

31 October 2026.20focused controlled tests and1native CQL test pass, zero skips. Native queries return3accounts then a2account subset. Published C# walkthrough/CLI3/2 also pass; HTTP/Mongo federation, hosted and browser acceptance remain open.

Four bounded read-only .NET RPC readiness calls passed at the existing `https://rpc.mainnet.near.org/`: synced mainnet status and view_account for near/wrap.near/aurora. The first account selected `finality:final`; subsequent accounts were pinned to its exact block_id, with matching returned block_hash/height. No wallet/signing, transaction, contract execution, runtime/body download or node install.

Implemented `NearPublicAccountMetadataSource` in the existing NEAROASIS provider, separate from signed storage. Strict schema: schema1/rpcUrl/networkId/accounts/sourceOwner.1–8unique canonical lowercase account IDs, mainnet only, credential-free root HTTPS. `NearAccountId` supports point/membership over the retained catalogue; unsupported filters/scans/sorts fail. All records validate before visibility/paging, including Take(0); cancellation/error never returns partial output.

Each query checks RPC-reported mainnet/synced status, resolves one final account state, then pins all other accounts to that exact hash and height. UInt128 amount/locked stay canonical decimal strings; UInt64 height/storage usage parsed exactly and published as decimal strings. Base58 block/code hashes must decode to32bytes. Replies capped256KiB/depth16 and duplicate fields rejected. Chain identity, account attribution and finality are RPC assertions, not independent genesis/consensus/state proofs. These public account projections are not stored COSMIC holons on-chain.

Metadata: NearAccountId, NearNetworkId, NearBlockHash, NearBlockHeight, NearFinality, NearAmountYocto, NearLockedYocto, NearStorageUsageBytes and NearCodeHash. Shared NativeRecordHolonMapper supplies stable IDs; public active roots/no children. No source mutation supported.

Controlled tests cover maxUInt128, maxUInt64/storage and heights beyond JS integer precision; malformed hash/amount, wrong/syncing chain, mismatched hash/height, RPC identity, duplicate/oversize/error, paging/budget, inactive/cancel/unknown keys, invalid config and partial-last-record errors even on an empty page.

Native test runs the real guarded HTTP source: activation plus full/subset reads passed in34seconds. Record this measured latency when setting C# demo deadlines and HTTP-client budgets; do not add retries or silently substitute a source. Retained `LocalState/COSMIC/HostedNearPublicSourceCurrent/source.json`220B has3public account IDs and stable mapping owner (not a credential); preserve it across cleanup. No new dependency/runtime installed or restored.

Next: spaced C# walkthrough through HolonManager/LINQ, actual CLI, shipping HTTP+Mongo5/4/isolation with original-operation journalling and exact-owned cleanup, then strict hosted configuration/presets and client links. No hosted availability is inferred.

Primary contracts: [NEAR query request/response](https://github.com/near/nearcore/blob/master/chain/jsonrpc-primitives/src/types/query.rs), [FastNear view_account reference](https://docs.fastnear.com/rpc/account/view-account). Source probe: `Scripts/Sandbox/Test-NearPublicMetadataCandidate.ps1`; CQL proving tests: `NearPublicAccountMetadataTests` in the existing NEAROASIS UnitTests project. Raw evidence only TestResults.

- `TestResults/NearPublicSourceCandidateCurrent/readiness.json`: 1557 B; SHA256 `b90285f537e503cd955433228346a09d26b54fc106416ca0df074404dad73459`.

- `TestResults/NearPublicSourceCandidateCurrent/provider-controls.trx`: 31401 B; SHA256 `bec5c59cce13130fb831b947ca2f7c7378167041b5da226ff3a96c3b7410c929`.

- `TestResults/NearPublicSourceCandidateCurrent/provider-native.trx`: 2830 B; SHA256 `47eb309ce839ef20f926b14801a9db7c2a79cbf01adeb23c05fe44ba0a85a5fc`.

## Published C# walkthrough milestone

`NearPublicWalkthrough` is spaced/commented and uses the existing NativeKeySetWalkthrough→CosmicLinq→HolonManager pipeline. Console builds;20focused controls pass after the published source is linked into the existing provider test project. The native test runs that exact published source and passed1/0skips in34seconds; actual `--near-public` CLI also returned3/2. Both use a cancellation deadline of60seconds based on measured34seconds, with no retries or source substitution. No source/dependency/runtime/identity change, transaction, Mongo record or fixture created.

Command: `dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj --no-build --no-restore -- --near-public LocalState/COSMIC/HostedNearPublicSourceCurrent/source.json`.

[Maintained source guide](../../Devs/NEAR_PUBLIC_METADATA_CQL_GUIDE.md). Next: shipping HTTP+Mongo5/4/isolation and exact-owned fixture cleanup, then hosted presets/config/client links and actual hosted verification. The source endpoint's measured latency must inform the HTTP test budget; no silent fallback.

- `TestResults/NearPublicSourceCandidateCurrent/console-controls.trx`: 31401 B; SHA256 `ce85c5df384bc52f9f6610a7e99a1e13d4b54367b6c527638752009b1eea6b53`.

- `TestResults/NearPublicSourceCandidateCurrent/console-native.trx`: 2830 B; SHA256 `cc334e98ef5addb40def1be1944f1d2edd5f7fbcb76398f3c38f0af0fc4ba92e`.

- `TestResults/NearPublicSourceCandidateCurrent/console-cli.log`: 1058 B; SHA256 `32857632bb3ba16db5cb96234849426870a490c3956174a5b3db6b1c03bc8d44`.
