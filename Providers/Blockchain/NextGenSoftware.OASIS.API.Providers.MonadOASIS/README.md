# MonadOASIS Provider

This project wires the Monad L1 blockchain into the WEB4 OASIS provider matrix by
re-using the shared `Web3CoreOASIS` smart-contract/runtime. Configuration is supplied
through `OASIS_DNA.json` (or the local `DNA.json` helper) and requires:

- `ConnectionString` (RPC endpoint, e.g. `https://testnet-rpc.monad.xyz`)
- `ChainPrivateKey` (hot wallet used for persistence transactions)
- `ContractAddress` (deployed Web3Core storage contract on Monad)

Because Monad is EVM-equivalent, no additional ABI changes are required. Once the
DNA entry is populated the provider can participate in HyperDrive replication,
failover, and load-balancing flows like any other blockchain provider.

