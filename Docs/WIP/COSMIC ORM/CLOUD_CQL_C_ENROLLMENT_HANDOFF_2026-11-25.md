# Cloud CQL session C: enrollment handoff (25 November 2026)

Cloud sessions cannot set Railway variables: the publish scripts need the
Railway CLI and the laptop's DPAPI state. These three sources are merged code
with passing CQL tests. Each one needs the local CQL sandbox session to enrol it
on Railway Development. Nothing here needs David.

| Provider | Variable | RPC (anonymous, no key) | Chain | Read commitment |
| --- | --- | --- | --- | --- |
| RootstockOASIS | `CQL_ROOTSTOCK_PUBLIC_SOURCE_JSON` | `https://public-node.rsk.co/` | `0x1e` (30) | `confirmed-100` |
| StoryProtocolOASIS | `CQL_STORY_PUBLIC_SOURCE_JSON` | `https://mainnet.storyrpc.io/` | `0x5ea` (1514) | `finalized` |
| LensOASIS | `CQL_LENS_PUBLIC_SOURCE_JSON` | `https://rpc.lens.xyz/` | `0xe8` (232) | `finalized` |
| QuestDBOASIS | `CQL_QUESTDB_SYMBOL_SOURCE_JSON` | `https://demo.questdb.io/api/v1/sql/execute` (read-only public demo) | n/a | latest trade row per symbol |
| ENSOffchainOASIS | `CQL_ENSOFFCHAIN_PUBLIC_SOURCE_JSON` | `https://api.web3.bio/` | n/a | live name-service profile |

## Steps (per provider, on the laptop)

1. Write `LocalState/COSMIC/Hosted<Name>PublicSourceCurrent/source.json`, where `<Name>` is `Rootstock`, `Story` or `Lens`:

   ```json
   {"schema":1,"rpcUrl":"<RPC above>","chainId":"<chain above>",
    "addresses":["0x0000000000000000000000000000000000000001",
                 "0x0000000000000000000000000000000000000002",
                 "0x0000000000000000000000000000000000000003"],
    "sourceOwner":"<the public demo source owner used for the other EVM sources>"}
   ```

2. Run `python3 Scripts/Sandbox/Publish-Hosted<Name>PublicConfiguration.py` to prepare the protected candidate (the script makes no change and starts no deployment).
3. Review it, then run `python3 Scripts/Sandbox/Publish-Hosted<Name>PublicConfiguration.py apply`.
4. After the next Railway deployment, `GET /api/data/query-cql/providers` should list the provider as available. Its full and filtered presets should return 3 and 2 records.

The scripts are clones of the BNB publisher. They add the provider to
`queryOnlyProviders`, add a zero-cost read tariff, and add four presets. The
canonical DNA stays unchanged.

## What was verified from the cloud session

- Live RPC probes on 25 November: the chain ID and genesis hash matched for all three.
  - Story and Lens answer `finalized` and EIP-1898 `requireCanonical` balance reads.
  - Rootstock rejects `finalized` (`-32602`) and `requireCanonical` (`-32603`).
    Its profile therefore reads the block 100 confirmations below the head, by hash,
    and checks that block's hash at that height again after the reads. A changed
    hash fails the query; it is never retried or read from `latest`.
- Controlled CQL tests in `EthereumOASIS.CqlTests` (`Rootstock*`, `Story*`, `Lens*`).
- The live dev catalogue showed 106 of 107 sources ready (only Blockscout down, upstream).

## Also merged: CQL startup head-of-line fix

Before this fix, sources registered through BootLoader started one at a time in
alphabetical order. HoloOASIS starts first in that order, and its conductor cold
start takes about 7 minutes, so IPFS, NEAR, Neo4j, Polkadot, Polygon, SOLID,
Solana and TON stayed `CQL_SOURCE_NOT_READY` for those minutes after every deploy.
This was observed live on 25 November (generation 0 on one host instance).

Now only the BootLoader registration step is serialized (`CqlHostedSourceRegistry`).
Holo provisioning and the per-source health checks run in the bounded parallel lane.
`CqlActivationLaneTests` covers this. The Holo cold start itself is still the CQL
sandbox session's item.

## QuestDB and ENSOffchain (merged 25a128c2aa)

Use `Scripts/Sandbox/Publish-HostedQuestDbPublicConfiguration.py` and `Scripts/Sandbox/Publish-HostedEnsOffchainPublicConfiguration.py`, following the same steps as above. Their `source.json` files go in `LocalState/COSMIC/HostedQuestDbPublicSourceCurrent/` and `LocalState/COSMIC/HostedEnsOffchainPublicSourceCurrent/`, with these exact values:

```json
CQL_QUESTDB_SYMBOL_SOURCE_JSON = {"schema":1,"endpoint":"https://demo.questdb.io/api/v1/sql/execute",
  "dataset":"questdb-demo-trades","sourceOwner":"<public demo source owner>",
  "symbols":["BTC-USDT","ETH-USDT","SOL-USDT"]}
CQL_ENSOFFCHAIN_PUBLIC_SOURCE_JSON = {"schema":1,"origin":"https://api.web3.bio/",
  "sourceOwner":"<public demo source owner>","names":["brantly.eth","nick.eth","vitalik.eth"]}
```

Preset metadata keys: `QuestDbSymbol` and `ENSOffchainName`. Both sources were verified live from the cloud session on 25 November. QuestDB returned the three symbols as `SYMBOL` in 0.3 s. web3.bio returned the exact identity, the `ens` platform and a canonical address for all three names.

The QuestDB symbol adapter (`9e67263e`..`35d38833`, 4 November) had never been merged into Development. It is now ported, with an explicit `dataset` descriptor: `fixture` (the default) or `questdb-demo-trades`. SQL is never taken from configuration.
