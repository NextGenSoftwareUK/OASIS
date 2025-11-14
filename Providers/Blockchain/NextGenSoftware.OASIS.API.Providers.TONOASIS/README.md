# TONOASIS Provider

`TONOASIS` brings the Open Network (TON) into OASIS via the new TON EVM runtime.
By pointing the shared `Web3CoreOASIS` contract suite at a TON EVM RPC endpoint we
gain full CRUD/NFT/wallet parity without re-implementing a bespoke TON pipeline.

Populate `DNA.json` (or `OASIS_DNA`) with:

- `ConnectionString`: TON EVM RPC endpoint (testnet or mainnet)
- `ChainId`: network identifier exposed by the EVM gateway
- `ChainPrivateKey`: hot wallet used for writes
- `ContractAddress`: deployed `Web3CoreOASIS.sol` instance on TON

Once configured, enable `TONOASIS` in HyperDrive replication/failover settings to
include TON alongside the rest of the blockchain quorum.

