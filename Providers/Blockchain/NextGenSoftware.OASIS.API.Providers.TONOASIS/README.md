# Current native-source distinction

TonLocalTvmStorageSource is an explicit local TVM/emulator source with encrypted
canonical storage and bounded CQL. Four admission controls/current builds and a native local TVM test pass
(encrypted reopen, queries3/2/0 and ownership/budget checks). Public-network,
adapted retained-source/Mongo and hosted execution remain unverified. See
[scope and commands](../../../Docs/WIP/COSMIC%20ORM/TON_CQL_MVP.md).

The existing TONOASIS EVM wrapper remains legacy and is not native TON TVM.
Public-header queries use a separate profile. Older text below is historical
and does not establish public-network native storage, NFT/wallet or full parity.

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
