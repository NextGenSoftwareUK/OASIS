# Polkadot public header source readiness

31 October 2026. Native source and CQL3/2 verified; C# walkthrough/CLI and local HTTP/Mongo federation/isolation also pass; hosted/browser acceptance remain open.

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

## Published C# walkthrough

Existing console now accepts `--polkadot-public LocalState/COSMIC/HostedPolkadotPublicSourceCurrent/source.json`. The spaced/commented `PolkadotPublicWalkthrough` calls the same NativeKeySetWalkthrough→CosmicLinq→HolonManager planner/executor as other source demos. The native test compiles that published source verbatim and passed1/0skips; the actual CLI also returned3/2. Source catalogue/owner unchanged, no DB/transactions or source mutation. Controlled17tests pass after linking published sources. [Maintained guide](../../Devs/POLKADOT_PUBLIC_METADATA_CQL_GUIDE.md).

HTTP/Mongo admission/federation verification is in progress; no hosted success inferred from the C# console.

## HTTP federation milestone

`CqlPolkadotMongoNativeTests` passed1/0skips through shipping leaf/federation endpoints on loopback Kestrel: native3/2, private owner pairs5/4, foreign-private0 and foreign-public-pair3. Nine validated query receipts; every returned ID, provenance, visibility and snapshot owner is checked. Two normal SDK create-only saves use server-mapped IDs and original persisted operations; source catalogue unchanged. Controlled bearer/admission is not production signup/billing or hosted replication proof.

Exact owner/nonce/2record checks passed before dropping owned `cosmic_cql_polkadot_http_demo`; absence asserted. Kestrel stopped and Mongo process exited; no listener27029. Original fixture intent631B,2ACKs216B, cleaned70B (917B total) preserved at `LocalState/COSMIC/PolkadotMongoHttpCurrent`; no replay/reseed.

API admission/discovery/preset controls2pass; affected Core tariff controls10pass. Sixteen supported unsigned read profiles still require independent permission/price; signed write admission fence unchanged. WEB4 now explicitly references the Polkadot assembly at composition root and lazily registers `CQL_POLKADOT_PUBLIC_SOURCE_JSON`. Initial compile caught that missing reference and a new test's mistyped Azure enum; both fixed at their source. No hosted configuration applied.

Next: strict Polkadot publisher/joint candidate and four presets, generic browser snapshot tests/local links, then matching hosted deployment and real preparation/isolation/replica/browser gates. [Source guide](../../Devs/POLKADOT_PUBLIC_METADATA_CQL_GUIDE.md).

- `TestResults/PolkadotPublicSourceCandidateCurrent/console-controls.trx`: 27614 B; SHA256 `11bf1f98e99c7724d115d826a46bda8d64187a6f44089dec917826b9383d71b2`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/console-native.trx`: 2839 B; SHA256 `ca4ca3d565f18899a771889817bbad28d5eb46f8a2aad3379945531995a89efd`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/console-cli.log`: 1416 B; SHA256 `49714f9dcf105ed8225a6f76d83a966a27613733e12094509e7515142004fe9c`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/api-controls.trx`: 5007 B; SHA256 `8d584dd31d0f079e483cdfb9f11836bc61688d0ccabefef5b6b4704d6f0c8bce`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/core-controls.trx`: 17586 B; SHA256 `8623c25025d00d160b44fa1b6d5c505f25076dba09d73e3209f26f6278d06bc2`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/polkadot-mongo-http.trx`: 3371 B; SHA256 `95806817eca7ba726287c10a1b94c0668f762e899207fda38e4b30de523f978f`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/polkadot-mongo-native-summary.json`: 229 B; SHA256 `7a3550ec06d49f6bef110ad8b587c19c8b2a01850daf59559366f527b3450dfd`.

## Hosted configuration and browser-link staging

Strict Polkadot configuration controls3pass (including malformed/paid/overlap and encryption cases); joint composition7tests pass. Four presets: native3/2 and Mongo pairs5/4. The six-source composition preserves all24new presets and existing DNA/admission/settings. Only the specific query key needs an explicit encryption-policy grant; no global reserved list changed.

Protected `LocalState/COSMIC/HostedSourceRolloutCurrent/google-ethereum-public-polygon-arbitrum-cosmos-polkadot-{before,candidate}-variables.dpapi` prepared,67,094/83,270B,8values/24presets. Not applied. Requires matching application/Core79d7859, never frozen6bb. Earlier individual/five-source candidates are historical, not successive changes to apply.

Frontend `C:/Source/Web4Site`, work/cosmic-cql commit5066b03 pushed: local guide/CLI/commented-source links plus generic Polkadot snapshot preparation coverage.53client controls pass: two Mongo-only private writes, server-derived IDs, original source predicate preserved, repeat0writes/2existing. No provider-specific production JS branch needed. Public main unchanged; hosted preparation/replica/browser acceptance still open.

Next: matching scheduled application/configuration/frontend rollout, then actual hosted availability, visitor preparation/reuse/isolation, replicas and browser verification. NEAR breadth can proceed independently while deployment timing remains pending.

- `TestResults/PolkadotPublicSourceCandidateCurrent/config-controls.log`: 106 B; SHA256 `383a12fba9b5b327054a24ce7e5739569c4f0b6621facd981f2d6a4271063b3c`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/combined-config-controls.log`: 110 B; SHA256 `6b01cf532002921c2dd88971821b5865cafe78472020d0909e7df38db83acb08`.

- `TestResults/PolkadotPublicSourceCandidateCurrent/frontend-controls.log`: 5148 B; SHA256 `77aa017bc6ee48f2f4f3fd1c06cf46433fc9c39f1fac2ae444aec9d4864602f6`.
