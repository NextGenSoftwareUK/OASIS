# EigenLayer public operator source

4 November 2026. A credential-free Ethereum RPC reported chain ID 1 and
finalized block 0x191f679, hash
0x78bb2f9c94964ae2ac942b238b5f86615c96cf1ef94070fe21dc89bbd2c6ccf8.
The three read-only calls to the published DelegationManager proxy
0x39053D51B77DC0d36036Fc1fCc8Cb819df8Ef37A used the isOperator(address)
selector 0x6d70f7ae with that block hash and requireCanonical=true.
Each returned the canonical 32-byte ABI encoding of true.

| Operator catalogue address |
| --- |
| 0x39beca54f450fcc4b31729a85b3e9655ec0081d6 |
| 0xdbed88d83176316fc46797b43adee927dc2ff2f5 |
| 0xdfcb21ac9b99de986d99f4ce5fce2a6542efe3a1 |

Scope the CQL source to exact address equality and bounded membership.
Read all addresses at one RPC-reported finalized canonical block. Expose
registration status and block provenance. The observation covers delegation
registration only. AVS participation, balances, writes and independent
finality verification remain outside this source proof. The CQL adapter,
WEB4 federation and hosted acceptance remain open. No files or runtimes were
needed for the probe.

[Contract deployment](https://github.com/Layr-Labs/eigenlayer-contracts/blob/main/README.md) ·
[DelegationManager contract notes](https://github.com/Layr-Labs/eigenlayer-contracts/blob/main/docs/core/DelegationManager.md)

The compiled guarded reader passed a live smoke at the same canonical block.
All three catalogue addresses returned true. The zero address returned false,
exercising both canonical ABI boolean values. The provider build had zero
errors. The smoke reused the existing binaries and created no output files.

The bounded read-only CQL source built and activated all three configured
operators against Ethereum mainnet. It projects stable public holon IDs,
registration flags and the shared finalized block provenance. WEB4 admission
and end-to-end query and federation tests remain open. Existing build paths
were reused.
