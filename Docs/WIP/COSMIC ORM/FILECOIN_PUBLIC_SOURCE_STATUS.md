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

## Hosted acceptance complete — 1 November 2026

Code d1fe285f and settings082c1f0d succeeded at parentfcc4f735d/Coreb386875. Native3/2, Mongo-paired5/4, repeat0new/2existing, foreign-private0/public3 and exact SQLite/Neo4j replicas2/2 pass. Two normal private saves at version1; no pending intents. All native rows retain finalized/chain314 metadata and one block hash per selection. Immutable completion ACK under LocalState/COSMIC/HostedQueryAcceptance-FilecoinOASIS freezes the original cohort; never replay/resave it. Counts46integrated/42hosted/browser5/open37. Earlier pending-hosting statements are superseded.

Copy scope: private Mongo demo records retain source identity/name/description and mapping/provenance metadata; native balances/block fields remain native. This proves scoped query federation and replication of those demo records, not full source-state migration. Full payload mapping is tracked in AFTER_DEMO_BACKLOG.md.

## Exact next step

Publish the Filecoin sandbox card with pinned guide/C# links, run client publication checks and verify exact public HTML/scripts. Browser interaction remains open. Then implement the already qualified Telos descriptor/native C# scope (TELOS_PUBLIC_SOURCE_STATUS.md), keeping completed Filecoin/native/local cohorts frozen. No Filecoin configuration reapply/redeploy. Hosting deferrals unchanged.

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


Prepared-preset shipping gate1November2026: actual public projection of protected candidate profiles/examples passes shipping CQL compiler and exact MongoDBOASIS.CqlCapabilities for all4presets (2leaf/2federation),1test/0skips. Sources remain inactive; no native request/DB/save. `TestResults/FilecoinPublicSourceCurrent/filecoin-prepared-planner.trx`: 3338bytes; SHA256 `323d3f92b0414a77df804518e7eea97e86326e5a00eeee681f6a27fc42b1a2da`. Original code jobd1fe285f is still BUILDING at fcc4f735d; no settings applied/redeployment. Public guide now includes the tested HTTP request/federation route.

Code application milestone supersedes earlier pending-code/settings statements. Protected state remains LocalState/COSMIC/HostedSourceRolloutCurrent/filecoin-public-*; releaseACKs HostedFilecoinReleaseCurrent. No visitor acceptance cohort created yet.


Hosted immutable evidence:
- `TestResults/HostedCqlIntegrationCurrent/FilecoinOASIS-native-http.json`: 13754bytes; SHA256 `33cf1ed8a4022a3977e8e8db5e9969da238566ea09189c9630b7fd10c294b7a9`.
- `TestResults/HostedCqlIntegrationCurrent/FilecoinOASIS-client-http-flow.json`: 36934bytes; SHA256 `17720236fb18e73d15ec5b343d0a6b4a90f5f2eefedbbb8d187d17a07a6807c8`.
- `TestResults/HostedCqlIntegrationCurrent/FilecoinOASIS-isolation-http.json`: 5212bytes; SHA256 `e336263f393a69489e9c2ccefced858d88fff2bf21943273deee77a7083d4d01`.
