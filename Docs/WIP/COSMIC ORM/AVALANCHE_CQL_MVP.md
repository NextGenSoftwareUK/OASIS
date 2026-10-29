# Avalanche CQL MVP

Updated 29 October 2026. **Local native/C#/HTTP/browser MVP verified.**

## Root causes fixed

The shipped Web3Core Avalanche class inherited CQL, but its authority was excluded
by the shared compiler. Two new controls reproduced that rejection before admission
was added. Native acceptance then reproduced a second shipping defect: the
constructor never supplied the chain identity required by authenticated encryption,
so its first save failed before any record write.

The constructor now supplies default C-chain identity43114, with an explicit chain
overload for configured networks. Its original three-string signature is retained
for compiled callers; both constructors use one initialization path. BootLoader
now passes the existing DNA ChainId through its actual factory. Positive decimal
and hexadecimal values are parsed explicitly, including unsigned high-bit hex
`0xa86a`; missing/invalid/nonpositive identities reject before native access.
No legacy implementation or alternate storage path was changed.

## Native failure preservation and continuation

The original deployed contract and three planned IDs were retained after failure.
An explicit, Avalanche-only continuation verified native runtime code against the
pinned compiler output, the original owner, and zero holon/avatar/detail counters.
It flushed a durable admission record before the first record writes. No transaction
was replayed, no replacement IDs generated and no second contract deployed.

Ordinary runners still reject pending setup. Continuation refuses nonempty state,
wrong owner/code, completed source or an existing continuation admission. It is
specific to the reviewed empty pre-write failure, not general unknown-outcome recovery.

## Verified scope

- Controls: **37/37**; Avalanche16 including constructor/factory configuration,
  plus changed-compiler Polygon7/Arbitrum7/Base7. BootLoader producer builds.
- Native: **1/1**; three encrypted records filtered to two. Full runner restart
  reuses the original source without further writes; query reads preserve nonce.
- Paired C#/HTTP: **1/1, zero skips**, 1m19s including interactive verification.
  Five/four physical rows retain Avalanche/Mongo provenance. Forged actor/unknown
  preset reject; inactive source returns503 without partial rows.
- Actual browser clicks verified both presets. Hub discovery:8single/16plans,
  16paired/32plans,18paired rejections,3archive rejections; listeners unchanged.
- Exact owned `cosmic_cql_mongo_avalanche_demo` validated/dropped; pending/ready
  markers removed. Owned EVM flushed/stopped, Mongo/host stopped, ports18547/27029/5105 closed.

Supported profile: up to100 catalogue-intersected native ID equality/membership,
AND/OR/constants and paging, using one observed block number. Unsupported metadata,
ordering and child expansion reject before I/O. This tests the concrete provider
on **local Ganache chain31338**; public Avalanche consensus/finality, full parity
and universal HyperDrive behavior remain outside this proof.

## Evidence

| Path under TestResults | SHA256 |
|---|---|
| `AvalancheCqlCurrent/avalanche-cql-before.trx` | `2DA879C138989F8E857D715D1C07520CE424B1970542ADC859E1DEBB2D5AF2F4` |
| `AvalancheCqlCurrent/avalanche-family-controls.trx` | `C98E973BA76D1F5D44AB0813A567297FAE8F7E6D1169DBF8D6EC8BD8113D8038` |
| `AvalancheCqlCurrent/avalanche-cql-native.initial-chain-identity-failure.trx` | `19DCF4E13161D65F968E48A7E01817F612C74746E4B3F4C33EA8A779767EAB29` |
| `EthereumCqlCurrent/avalanche-cql-native.trx` | `2FDB86721C3AA1A9ABB68CD8DDA2685AB6D1897B663ED5994C709FC0E13DA738` |
| `EthereumCqlCurrent/avalanche-mongo-federation.trx` | `F863CD40B566E388D1C795033EC86307983573EBA3B952C19FA3E72C0A3312F7` |
| `AvalancheCqlCurrent/avalanche-mongo-browser.jpg` | `F9D628C309A363B1280ABF481D7872D626EC1DC578619C5DA38678F0BBD078B0` |

Persistent state: `LocalState/COSMIC/EvmCql/Avalanche/demo-seed.json`229bytes,
SHA256 `594C37E395F4ADAED9325262B52E0904C76178189BCD7B855061D6B7E4E3F4E8`;
`continued-empty-original.json`599bytes records the original empty-state admission.
Retain these, the shared ledger/runtime and original protected key; no new runtime
or ledger was installed. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

```powershell
# Interactive host: http://127.0.0.1:5105/.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Avalanche -Federated

# Bounded native/SDK/HTTP verification and cleanup.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Avalanche -Federated -VerifyOnly
```

Next: registered Azure Blob Storage admission/native/paired MVP review; hosted Azure
Cosmos stays pending by user instruction. [Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md).
