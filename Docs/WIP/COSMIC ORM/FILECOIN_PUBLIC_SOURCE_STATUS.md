# Filecoin public CQL source — qualification

1 November 2026. Agent-owned red breadth scope. Filecoin adapter/native CQL implemented; HTTP admission and hosted proof remain open. Native/CLI/local HTTP verified; hosted proof remains open.

## Native evidence

- Official GLIF endpoint `https://api.node.glif.io/rpc/v1` reports mainnet chain314 (`0x13a`).
- Original Ethereum genesis-block qualification failed HTTP400/RPC-32602: requested block outside available upstream range. Original failure is preserved, not retried.
- Separate explicitly selected native identity contract succeeds: `Filecoin.ChainGetGenesis`, height0 and CID `bafy2bzacecnamqgqmifpluoeldx7zzglxcljo6oja4vrmtj7432rphldpdmm2`.
- Finalized header and three EIP1898 balance reads with the exact selected block hash and `requireCanonical:true` succeeded. All three fixture balances were0. This is a native protocol diagnostic, not a shared-manager/CQL test or independent finality proof.
- `/rpc/v1` finalized semantics are described by Lotus as EC head minus900epochs; do not label this independently verified F3 finality.
- No source catalogue/owner, account, wallet, gas, install or chain write created. Original qualification intents prevent replay.

## Implemented and verified

The immutable Filecoin native genesis-CID policy is implemented in the shared EVM reader. Existing descriptors retain Ethereum genesis-hash identity and the original seven-parameter constructor. No after-failure identity switching. Filecoin source uses chain314, exact native genesis height0/single CID, finalized hash-bound balances and existing access/paging/budget validation.

Controlled suite:300passed/0failed/0skipped, comprising22Filecoin and278affected shared-reader regressions. New Filecoin cases cover wrong CID/height/empty or extra CIDs/extra CID fields, chain/hash/envelope failures, partial/overflow results, precision, paging, cancellation, unknown/inactive keys, unsupported writes/scans and credentials. One real published C# walkthrough test passes3/2/0skips through HolonManager/CQL.

Stable source catalogue now exists under LocalState/COSMIC/HostedFilecoinPublicSourceCurrent/source.json; source-create intent binds original diagnostic. Preserve owner/address IDs. No source writes. Commented FilecoinPublicWalkthrough and --filecoin-public entry point are implemented; actual standalone console build/run completed successfully3/2. Original source-bound native-qualified ACK is retained; do not replay the completed native gate.

## Local HTTP and prepared hosted milestone

Core1admission and API2controls pass/0skips; API producer build passes. Initial API test compile lacked its explicit Filecoin dependency; fixed the project graph and retained api-admission.log. No data ran on that failure. Local native HTTP1test passes3/2, pairedMongo5/4 and foreign-private0/public3, with two SDK-owned private saves and nine query receipts. Original fixture intent/two save ACKs/marker-verified cleanup ACK retained; exact database absent, owned Mongo stopped/port27029absent. Do not replay this cohort. These controlled auth/billing gates do not prove hosted replication or production settlement.

Publisher9pure controls and client1preparation/reuse control pass. Client test commitc291373 is pushed; live website remains eef0769. Protected source/admission/examples candidate contains3settings and4presets, prepared only. No service mutation/deployment. Source-bound native/local qualification ACKs bind this candidate. Coreb386875 task branch pushed/ref-verified; parent gitlink/manifest updated together. Integrated types46; hosted HTTP41/browser5/open36. No Filecoin hosted claim.

## Exact next step

Parentf93399e3 and Coreb386875 task refs are pushed; manifest/gitlinks validate. Merged current Development42da7fe9; only its David actions document changed, executable source identical to verified batch. Publish current verified parent/Core refs with durable intent; preserve merged owner actions. Then observe exact code deploymentSUCCESS before guarded apply of reviewed3settings, one journalled settings rollout, first hosted native/federation/reuse/privacy/replica cohort and sandbox card. Never apply at a different code SHA or replay either completed native/local fixture. Hosted PostgreSQL/Azure Cosmos/Oracle and browser control remain deferred/closed.

## Evidence and resources

- `TestResults/FilecoinPublicSourceCurrent/NativeGenesis/qualification.json`: 2356bytes; SHA256 `cae42b05ffa0c1c25eebe9d52863fa894c20543657217cdbffbcb18baf15357a`. Six bounded RPC responses retained alongside it.
- Original failure: `TestResults/FilecoinPublicSourceCurrent/qualification.json` and raw genesis rejection.
- Durable intents: `LocalState/COSMIC/HostedFilecoinPublicSourceCurrent/{public-qualification,native-genesis-qualification}.intent.json`. Preserve both and the separately created source catalogue.
- Reuse Python3.12, existing .NET10/provider projects and caches. No new environment required. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Primary references: [Filecoin Ethereum API](https://docs.filecoin.io/reference/json-rpc/eth), [native genesis method](https://docs.filecoin.io/reference/json-rpc/chain), [Lotus v1/v2 finality semantics](https://github.com/filecoin-project/lotus/blob/master/CHANGELOG.md).

- `TestResults/FilecoinPublicSourceCurrent/filecoin-identity-controls.trx`: 461252bytes; SHA256 `41247aeb53f9b6a648917025ecbb5d5bc691021449673da2d33a86841940cd01`.

- `TestResults/FilecoinPublicSourceCurrent/filecoin-native.trx`: 2885bytes; SHA256 `9354b22284d7e1d94720a2460ddb3f6df379ba604f2af649a327eb66010e144b`.

- `TestResults/FilecoinPublicSourceCurrent/console-native.log`: 1479bytes; SHA256 `fa7c2204c3e75f2bfa3f7aee790d40fa809a678b853725efc3716bec0a32e74a`.

- `TestResults/FilecoinPublicSourceCurrent/filecoin-core-admission.trx`: 3312bytes; SHA256 `7374c9e6a16a6138be7f48d1b9929c2680f27e78bee52d3807cf2f1ea2f4ec52`.

- `TestResults/FilecoinPublicSourceCurrent/filecoin-api-admission.trx`: 4965bytes; SHA256 `de42262021bfd7b33ecff9a1c1c108dc00fa75be7ed1aaad06e9ccf71857be67`.

- `TestResults/FilecoinPublicSourceCurrent/mongo-http-native.trx`: 3371bytes; SHA256 `0d6339a93b80686dd11ecc72748f2fc92ac3849ad8b52001859b96db58f6a48c`.

- `TestResults/FilecoinPublicSourceCurrent/filecoin-mongo-native-summary.json`: 229bytes; SHA256 `afac311dca2fa600334c7af9c1d9e34b3a734328ca1a8278d9c79ac32ae81c4c`.
