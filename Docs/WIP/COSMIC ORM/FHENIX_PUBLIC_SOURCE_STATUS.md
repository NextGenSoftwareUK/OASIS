# Fhenix public source status

4 November 2026. The existing provider defaults to the Helium testnet and
also documents Nitrogen. Credential-free eth_chainId probes failed DNS for
both hosts on the recovery laptop. Neither produced a usable CQL record.

Fhenix currently documents CoFHE deployments on Ethereum and Arbitrum
mainnet and several EVM testnets. Qualify a bounded public read of an
official CoFHE contract before admitting a CQL profile. Public observations
must stay separate from ciphertext access, permits and confidential writes.
The existing registry contract and provider defaults require separate
migration evidence. No dependency or generated output was needed.

[Fhenix CoFHE overview](https://www.fhenix.io/)

## CoFHE read qualification

The official SDK resolves the Task Manager through CoFHEAddressBook
0xC0F4e00E531a2B086492Ae3DCC1515038307196b using getTm(uint256) with ID 1.
Anonymous RPC calls used selector 0x25adb56b, checked chain IDs and
requested each RPC's finalized block hash with requireCanonical=true.

| Network | Chain ID | Finalized block | Task Manager |
| --- | --- | --- | --- |
| Ethereum | 1 | 0x191f6b9 | 0x52e7a0caf234b266bcfaa6aa933a412fde58432a |
| Sepolia | 11155111 | 0xb8152d | 0xea30c4b8b44078bbf8a6ef5b9f1ec1626c7848d9 |
| Base Sepolia | 84532 | 0x33f12ac | 0xea30c4b8b44078bbf8a6ef5b9f1ec1626c7848d9 |

Arbitrum One returned historical state unavailable at its finalized block.
Use the three qualified chain IDs as distinct keys, even when two networks
report the same Task Manager address. This covers public deployment lookup.
CQL integration and live WEB4 acceptance are pending.

[Official SDK lookup](https://github.com/FhenixProtocol/cofhesdk/blob/master/packages/sdk/core/taskManager.ts) ·
[SDK contract constants](https://github.com/FhenixProtocol/cofhesdk/blob/master/packages/sdk/core/consts.ts)

The compiled guarded reader and read-only CQL source built with zero errors.
The source activated all three configured networks live using the exact local
fixture. It exposes chain ID, Task Manager address and finalized block
provenance. WEB4 registration, native CQL and local/hosted federation remain
open. The guarded publisher configuration passed its three tests.

WEB4 admission passed. The local HTTP test saved two private Mongo fixture
copies, then its first three-network request hit the default 5-second WEB4
deadline. The concurrent chain-read build passed, but the resume still used
that default. The harness now sends the reviewed 30-second demo budget for
Fhenix and keeps the reader's 15-second bound. The exact two acknowledged
copies are retained for validated resume. Local federation and cleanup
remain open until the updated run completes.

The 30-second preset run rebuilt successfully but stopped during the
preceding native CQL read. A direct follow-up isolated an Ethereum
PublicNode finalized-block timeout while both testnet RPCs answered.
Ethereum dRPC reported chain ID 1 and returned the expected Task Manager
at canonical finalized block 0x191f837. The adapter now uses that
qualified Ethereum endpoint. The resumed WEB4 gate still needs to pass.

The qualified endpoint passed local WEB4 federation with the reviewed
request budget. Native counts were 3/2 and federated counts were 5/4.
The foreign actor saw three public rows and zero private rows. The resume
validated the two original SDK saves and recorded nine usage receipts.
The fixture database was removed, the replica stopped, and the guarded
publisher accepted the source-bound evidence. Hosted acceptance is open.

## Development hosted HTTP acceptance — 7 November 2026

Original first cohort passed and is sealed on source88b896815/Core944d380,
settings8783d8eb. Native3/2, Mongo5/4, first2private saves, repeat0new/2existing,
foreign-private0/public3 and exact SQLite/Neo4j replicas2/2 passed. Original
owner/foreign credentials and two save ACKs retained; pending intents empty.
No native fixture replay or source writes. Browser acceptance remains open.
[Batch gates and limits](SIX_PUBLIC_READERS_HOSTED_BATCH.md).

`FhenixOASIS-client-http-flow.json`: 31581B, SHA256
`8c62f9d56d8f0e5524a813cedcb184cdbd14bda7eff5baafd4cb558987145fc0`.

`FhenixOASIS-isolation-http.json`: 5598B, SHA256
`514ee1df3c4bfebf4208c41ad5167fcd589b2013962cf4802d8cdc54a8179cab`.

`FhenixOASIS-native-http.json`: 11968B, SHA256
`2894523b868cb17c92e30ceedddfc91d90c9fc804c81fff99aa85ac5d7a4321c`.

11 November UTC: the Development CQL Playground full public preset rendered three CoFHE Task Manager deployment rows. The two-network subset was selected but not dispatched because a Windows low-battery alert blocked browser input. The existing disposable visitor was reused. Private paired browser acceptance remains open.

11 November UTC: Fhenix public browser acceptance is complete. The Development CQL Playground rendered the earlier three-network preset and the remaining two-network preset. The subset returned FhenixOASIS rows for chain IDs 1 and 11155111. The accepted full preset was not repeated. Original-cohort private paired browser verification remains open.
