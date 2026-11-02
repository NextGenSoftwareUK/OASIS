# Filecoin public CQL source — qualification

1 November 2026. Agent-owned red breadth scope. Filecoin adapter/native CQL implemented; HTTP admission and hosted proof remain open. Counts unchanged pending standalone CLI milestone.

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

Stable source catalogue now exists under LocalState/COSMIC/HostedFilecoinPublicSourceCurrent/source.json; source-create intent binds original diagnostic. Preserve owner/address IDs. No source writes. Commented FilecoinPublicWalkthrough and --filecoin-public entry point are implemented; actual standalone console build/run is currently live in exec session93291. Do not launch a duplicate build or rerun the original native test to resolve this wait.

## Exact next step

Poll existing exec session93291. It owns the build lease and will run the actual CLI once its console build succeeds. Inspect TestResults/FilecoinPublicSourceCurrent/console-build.log and console-native.log; do not restart solely on an observation timeout. Then write source-bound native qualification ACK after validating the CLI3/2 output, record hashes, and implement API/admission/local HTTP/Mongo/privacy before preparing any hosted rollout. The public guide is Docs/Devs/FILECOIN_PUBLIC_ACCOUNT_CQL_GUIDE.md. Source reads are not storage-deal/CID queries or native writes. Counts remain45integrated/41hosted/browser5/open36 until next milestone.

## Evidence and resources

- `TestResults/FilecoinPublicSourceCurrent/NativeGenesis/qualification.json`: 2356bytes; SHA256 `cae42b05ffa0c1c25eebe9d52863fa894c20543657217cdbffbcb18baf15357a`. Six bounded RPC responses retained alongside it.
- Original failure: `TestResults/FilecoinPublicSourceCurrent/qualification.json` and raw genesis rejection.
- Durable intents: `LocalState/COSMIC/HostedFilecoinPublicSourceCurrent/{public-qualification,native-genesis-qualification}.intent.json`. Preserve both and the separately created source catalogue.
- Reuse Python3.12, existing .NET10/provider projects and caches. No new environment required. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Primary references: [Filecoin Ethereum API](https://docs.filecoin.io/reference/json-rpc/eth), [native genesis method](https://docs.filecoin.io/reference/json-rpc/chain), [Lotus v1/v2 finality semantics](https://github.com/filecoin-project/lotus/blob/master/CHANGELOG.md).

- `TestResults/FilecoinPublicSourceCurrent/filecoin-identity-controls.trx`: 461252bytes; SHA256 `41247aeb53f9b6a648917025ecbb5d5bc691021449673da2d33a86841940cd01`.

- `TestResults/FilecoinPublicSourceCurrent/filecoin-native.trx`: 2885bytes; SHA256 `9354b22284d7e1d94720a2460ddb3f6df379ba604f2af649a327eb66010e144b`.
