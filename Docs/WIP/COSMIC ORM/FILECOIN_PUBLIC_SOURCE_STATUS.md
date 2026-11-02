# Filecoin public CQL source — qualification

1 November 2026. Agent-owned red breadth scope. No Filecoin CQL implementation, admission, hosted proof or count increment yet.

## Native evidence

- Official GLIF endpoint `https://api.node.glif.io/rpc/v1` reports mainnet chain314 (`0x13a`).
- Original Ethereum genesis-block qualification failed HTTP400/RPC-32602: requested block outside available upstream range. Original failure is preserved, not retried.
- Separate explicitly selected native identity contract succeeds: `Filecoin.ChainGetGenesis`, height0 and CID `bafy2bzacecnamqgqmifpluoeldx7zzglxcljo6oja4vrmtj7432rphldpdmm2`.
- Finalized header and three EIP1898 balance reads with the exact selected block hash and `requireCanonical:true` succeeded. All three fixture balances were0. This is a native protocol diagnostic, not a shared-manager/CQL test or independent finality proof.
- `/rpc/v1` finalized semantics are described by Lotus as EC head minus900epochs; do not label this independently verified F3 finality.
- No source catalogue/owner, account, wallet, gas, install or chain write created. Original qualification intents prevent replay.

## Exact next implementation step

Implement an immutable, Filecoin-specific native genesis-CID identity descriptor over the existing shared EVM reader. Preserve its original constructor/default genesis-hash contract for every existing profile. Validate chain314, native genesis height0/exact CID set, canonical finalized hash and exact UInt256 balances before rows become visible. Do not replace the shared genesis invariant with chain ID alone or try native identity only after an Ethereum-genesis failure.

Add controlled identity/envelope/budget/failure tests and new real shared-manager/C#3/2 proof before creating a stable source catalogue. Then use the established API/admission/local HTTP/Mongo/privacy and hosted acceptance/card sequence. Read-only FEVM balances are not Filecoin storage deals, CID content queries or native writes. Counts remain45integrated/41hosted/browser5/open36.

## Evidence and resources

- `TestResults/FilecoinPublicSourceCurrent/NativeGenesis/qualification.json`: 2356bytes; SHA256 `cae42b05ffa0c1c25eebe9d52863fa894c20543657217cdbffbcb18baf15357a`. Six bounded RPC responses retained alongside it.
- Original failure: `TestResults/FilecoinPublicSourceCurrent/qualification.json` and raw genesis rejection.
- Durable intents: `LocalState/COSMIC/HostedFilecoinPublicSourceCurrent/{public-qualification,native-genesis-qualification}.intent.json`. Preserve both; no catalogue yet.
- Reuse Python3.12, existing .NET10/provider projects and caches. No new environment required. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Primary references: [Filecoin Ethereum API](https://docs.filecoin.io/reference/json-rpc/eth), [native genesis method](https://docs.filecoin.io/reference/json-rpc/chain), [Lotus v1/v2 finality semantics](https://github.com/filecoin-project/lotus/blob/master/CHANGELOG.md).
