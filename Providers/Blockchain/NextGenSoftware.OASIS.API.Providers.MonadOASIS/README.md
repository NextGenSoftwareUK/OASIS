# MonadOASIS Provider

The provider uses the shared Web3Core storage contract and runtime. The original
three-string constructor defaults to mainnet RPC `https://rpc.monad.xyz` and chain143.
For another network, pass its matching endpoint and explicit positive chain ID:

```csharp
// Local demonstration only; use your deployed contract and configured wallet.
var provider = new MonadOASIS(localRpc, walletKey, storageContract, chainId: 31338);
```

The helper DNA currently contains a testnet endpoint; it does not enroll a central
BootLoader case or supply an explicit network ID to a constructor. Wire configuration
through an actual factory before claiming automatic DNA activation. Never assume an
endpoint change alone changes the encryption chain identity.

Bounded CQL authority/chain/lifecycle controls pass; native/restart, paired demos and
broader HyperDrive acceptance remain pending. See
[the scoped work report](../../../Docs/WIP/COSMIC%20ORM/MONAD_CQL_MVP.md).
[Official mainnet identity](https://github.com/monad-developers).
