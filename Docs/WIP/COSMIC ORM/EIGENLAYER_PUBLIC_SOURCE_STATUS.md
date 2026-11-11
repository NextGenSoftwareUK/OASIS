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

The guarded hosted publisher passed three configuration tests. It binds
preparation to the exact local source digest and federation/cleanup receipt,
preserves existing write admission, and checks the expected live release
before applying the reviewed query-only configuration.

Local federation exposed a stall during the finalized-block RPC before
writes. Removing the duplicate 15-second adapter deadline allowed the
reader's own deadline to report the failing request. The fixed-length
JSON build passed and the compiled reader returned three true flags and
one false control again at finalized block 0x191f738. A direct transport
comparison accepted both fixed and chunked requests, so framing is not
established as the root cause. WEB4 federation still requires a green run.

Using a stable ID for each sequential JSON-RPC request, WEB4 admission and
local Mongo federation passed. Native counts were 3/2, federation 5/4,
foreign private 0 and foreign public 3. The harness verified two SDK saves
and nine receipts, dropped its exact fixture database, and the publisher
accepted the source-bound cleanup receipt. Hosted acceptance is open.

## Development hosted HTTP acceptance — 7 November 2026

Original first cohort passed and is sealed on source88b896815/Core944d380,
settings8783d8eb. Native3/2, Mongo5/4, first2private saves, repeat0new/2existing,
foreign-private0/public3 and exact SQLite/Neo4j replicas2/2 passed. Original
owner/foreign credentials and two save ACKs retained; pending intents empty.
No native fixture replay or source writes. Browser acceptance remains open.
[Batch gates and limits](SIX_PUBLIC_READERS_HOSTED_BATCH.md).

`EigenLayerOASIS-client-http-flow.json`: 34922B, SHA256
`b1888fca140d7dbefaf0a99bb0a6fe946393804e7e307ef71e05d6f2489937af`.

`EigenLayerOASIS-isolation-http.json`: 6027B, SHA256
`8630d1ec5c8710a67964c10b355ec37888561472a337784aa48662d803ff0360`.

`EigenLayerOASIS-native-http.json`: 13114B, SHA256
`224111fb4da84e81a99536a1cd7edaca7821ce8f501c0a25ea7c44ccb0512c04`.

11 November UTC: EigenLayer public operator-registration presets rendered 3 full and 2 filtered rows in the live Development WEB4 CQL Playground. The original-owner private paired browser check remains open.
