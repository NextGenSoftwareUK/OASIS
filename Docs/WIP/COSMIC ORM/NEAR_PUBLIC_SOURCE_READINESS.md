# NEAR public account CQL readiness

31 October 2026.20focused controlled tests and1native CQL test pass, zero skips. Native queries return3accounts then a2account subset. Published C# walkthrough/CLI3/2 also pass; Original-cohort HTTP/Mongo federation/isolation now also pass; hosted/browser remain open.

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

## HTTP integration under verification

WEB4 compiler/admission now includes the explicit NEAR query profile, with lazy `CQL_NEAR_PUBLIC_SOURCE_JSON` registration and independent read tariff/permission. Canonical signed storage is unchanged. New focused controls cover17provider catalogues, missing grants/prices, duplicate tariffs, signed-write rejection and four native/Mongo-paired preset plans. Actual native HTTP fixture verification remains pending its build/test gate; no hosted claim yet.

The native fixture reuses Mongo27029, journals two original SDK create-only operations before mutation, checks source3/2 and owner pairs5/4, and rejects another actor's private records. Success-only cleanup requires exact ownership/nonce/count and database absence. HTTP deadline60seconds is based on measured native activation/full/subset34seconds. No retry or replay allowed; unknown outcomes retain original intent/DB.

## HTTP deadline issue and original-cohort recovery

Original native HTTP fixture created and acknowledged two normal SDK create-only Mongo snapshots, then failed near-3 with HTTP504. The canonical NEAR RPC took34seconds for activation/full/subset and exceeded the shipping leaf endpoint's fixed5second query deadline. The failure and original intent/ACKs were preserved; no replay, source substitution in code or deadline relaxation.

Explicitly selected documented free `https://rpc.mainnet.fastnear.com/` after4bounded readiness reads passed in about1second. Only rpcUrl changed; the3accounts and stable mapping owner are unchanged. Published native CQL walkthrough then passed1/0skips in1second and actualCLI3/2 passed. Original slow-endpoint evidence remains historical; current source config224B usesFastNear. No canonical provider DNA/default endpoint changed.

Added an explicit `-NearMongoHttpContinuation` test gate. It reads the original actor, two operation IDs and mapped IDs from the original journal, verifies both original ACKs, privateowner/version1/provenance and exact marker/2records, and performs no additional Save. Shipping HTTP endpoints now return native3/2, owner pairs5/4, foreign-private0 and foreign-public-pair3 with9validated receipts. Continuation passed1/0skips in4seconds; sdkOwnedSaves0/reusedAcknowledgedSaves2. Original failed test was not rerun, reseeded or relabelled green.

After consumers stopped, exact ownership/count checks passed and the ownedDB was dropped; absence asserted. Originalintent444B+2ACKs216B+cleaned70B=730B retained in `LocalState/COSMIC/NearMongoHttpCurrent`; source identity unchanged. Mongo process stopped, no27029listener. Controlled bearer/usage authority is not production login/billing/replication/browser proof.

API admission/preset/discovery2controls and affected Core tariff11controls pass. Lazy `CQL_NEAR_PUBLIC_SOURCE_JSON` enrolls the separate read profile;17supported unsigned read types still need independent permission/price. Signed-write fence unchanged. Next: strict FastNear source publisher, joint configuration and four presets, client preparation test/links, matching hosted rollout and real hosted gates.

- `TestResults/NearPublicSourceCandidateCurrent/near-mongo-http.trx`: 6680 B; SHA256 `dad7731c8bc33258b0491476ddb62bff0df21528a27b28bcb53ad316963d8d8e`.

- `TestResults/NearPublicSourceCandidateCurrent/readiness-fastnear.json`: 1560 B; SHA256 `ffeb0985f1a7b36e80b5ab7ee6408421b7f5713ec652c9b1714a0d5817c3c411`.

- `TestResults/NearPublicSourceCandidateCurrent/console-native-fastnear.trx`: 2830 B; SHA256 `6b192bc80149d0af76096517339effab61b12cd2d43a27a456b6d58834abae2f`.

- `TestResults/NearPublicSourceCandidateCurrent/console-cli-fastnear.log`: 1058 B; SHA256 `79771e26926bb5064586e8be26ca42217923e745dd07cb053454a07219f73912`.

- `TestResults/NearPublicSourceCandidateCurrent/near-mongo-http-continuation.trx`: 3329 B; SHA256 `404f608f38dde4b76b7ecb39953c1cb23c5bb2c9a63124a7cb40856b28b87121`.

- `TestResults/NearPublicSourceCandidateCurrent/near-mongo-continuation-summary.json`: 249 B; SHA256 `0752a22de109aafc616898770f39480baa81f366079be5d7bebe7f73ccce9746`.

- `TestResults/NearPublicSourceCandidateCurrent/api-controls.trx`: 4980 B; SHA256 `56bcacd94e596490597db5b9c1046bb6848c02aec1710a1eba6bef490f47ddc4`.

- `TestResults/NearPublicSourceCandidateCurrent/core-controls.trx`: 19177 B; SHA256 `5527eaba6c91db48a199d1ded8e0572d0ec003ebff8e623447b98fb323f51166`.

## Hosted configuration and browser-link staging

Strict FastNear source publisher3tests pass (including paid/overlap/malformed and encryption cases); seven-source joint composition8tests pass. Four NEAR presets: native3/2 and Mongo pairs5/4. The joint candidate preserves all28new presets and existing DNA/admission/settings; missing NearAccountId encryption query-key permission rejects enrollment without changing system-reserved metadata.

Protected `LocalState/COSMIC/HostedSourceRolloutCurrent/google-ethereum-public-polygon-arbitrum-cosmos-polkadot-near-{before,candidate}-variables.dpapi` prepared,67,094/85,206B,9values/28presets. Not applied. Requires matching application/Core0eb3449, never frozen6bb. Earlier standalone/six-source candidates remain historical, not sequential updates to apply.

Frontend C:/Source/Web4Site work/cosmic-cql commite8bf2d3 pushed: local NEAR guide/CLI/commented-source links and generic private-snapshot preparation coverage.54client tests pass, including two Mongo-only private writes, server-provided IDs, preserved native selection and repeat0writes/2existing. No provider-specific production JS branch. Public main unchanged. No hosted deployment/variable change.

Next: revalidate browser access for the still-open original Solana/SOLID/S3 browser gates; integrate the matching scheduled application/configuration/frontend batch, then actual hosted preparation/reuse/isolation/replica/browser checks for the seven new profiles. Agent tooling failures remain agent work, not new owner chores.

- `TestResults/NearPublicSourceCandidateCurrent/config-controls.log`: 106 B; SHA256 `383a12fba9b5b327054a24ce7e5739569c4f0b6621facd981f2d6a4271063b3c`.

- `TestResults/NearPublicSourceCandidateCurrent/combined-config-controls.log`: 111 B; SHA256 `712d9323cfcaf36a238fc155aafa042f2c9d73847dc609ebb0c64676ab50446d`.

- `TestResults/NearPublicSourceCandidateCurrent/frontend-controls.log`: 5252 B; SHA256 `33849fba293c920c9bf987343a486174f65c0ae13af3a565777ae6b1430d950c`.
