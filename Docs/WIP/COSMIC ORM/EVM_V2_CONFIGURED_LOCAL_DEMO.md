# Configured local EVM V2 CQL reader

Source/consumer verified2November2026; seven controlled factory/configuration
cases pass. No new configured native/hosted execution is claimed.

Run-CosmicEthereumCqlNative.ps1 now uses the actual shipping V2 factories for
Ethereum/Polygon/Arbitrum. No legacy ABI retry, node startup, key creation,
contract deployment, seed migration or data writes/deletes are performed.
It requires an explicit compatible existing source. The legacy Ethereum seed
is incompatible and is never selected implicitly. Existing Arbitrum V2 native
3/2 and paired5/4 evidence remains valid at its original scope; it is not a
fresh execution of this configuration-driven consumer.

## Source configuration

Create a metadata-only JSON file from an existing admitted source. No secrets:

```json
{
  "Provider": "Ethereum",
  "RpcEndpoint": "http://127.0.0.1:18547",
  "ChainId": 31338,
  "Contract": "0x1111111111111111111111111111111111111111",
  "Actor": "11111111-1111-1111-1111-111111111111",
  "Ids": [
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002",
    "00000000-0000-0000-0000-000000000003"
  ],
  "MongoDatabase": "your_existing_demo_database",
  "MongoIds": ["00000000-0000-0000-0000-000000000001"]
}
```

The contract/actor/IDs/database above are illustrative, not deployed fixtures.
Replace them with the exact existing compatible contract and trusted policy IDs.
Do not use browser-supplied IDs as authorization. RPC must be HTTP loopback and
chain identity positive. Only these three providers are admitted. The native
reader uses a public local-test signer identity; no production key is accepted.
Configure the original private-storage decryption key through the process
OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY environment, never arguments/docs/chat.

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 `
  -Provider Ethereum `
  -ConfigurationPath LocalState/COSMIC/EvmV2ConfiguredCurrent/Ethereum.json
```

For the existing Mongo-paired/browser preset add `-Federated -Serve`. Mongo must
already run on loopback27029/oasisRecoveryRs; the named database must exist.
MongoIds must select real authorized copies of the chain IDs. The reader checks
both full/subset row counts. No synthetic Mongo records are created or removed.
Local UI ports: Ethereum5097, Polygon5098, Arbitrum5099. Stop preserves records.
The page describes configured catalogue counts, not guaranteed live row counts;
missing/deleted records remain absent. Shutdown stops the reader and preserves
externally managed services. Three in-process page-render regressions pass for
one/two/three Mongo copies, including a subset with zero Mongo copies. Evidence:
LocalState/COSMIC/LauncherRecoveryCurrent/preserved-page-regression-evidence.json.

The original result is preserved under TestResults/EvmV2ConfiguredCurrent/<Provider>;
existing reports reject another dispatch until explicit reconciliation. Zero
skips/exactly one native pass required. Controlled tests only instantiate shipping
factories and reject malformed/implicit configuration before activation. Native
consumer and HTTP host compile; configured native/Mongo/browser gates remain open.

All37published launcher paths now exist; this file audit does not prove services,
configuration or native/hosted readiness. Profile totals remain96local/60hosted.
