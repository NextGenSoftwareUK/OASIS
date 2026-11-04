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
