# Civic public source qualification

8 November 2026. Review only; no native query, CQL acceptance or admission.

The existing CivicOASIS adapter uses real Nethereum calls to GatewayToken:
verifyToken, getTokenIdsByOwnerAndNetwork and getToken. Preserve that adapter
while qualifying a bounded public query representation. No personal pass or
wallet catalogue has been selected, scanned or imported.

Primary Gateway Protocol source is pinned to
[440c564b6b5c82a202f15ce064062e0a152d6505](https://github.com/identity-com/gateway-protocol-evm/tree/440c564b6b5c82a202f15ce064062e0a152d6505).
The complete, untruncated tree was inspected in memory: 381762 bytes,
SHA256 afb682f0d2bbb7b6868159d33e57fc9c6ac72227589309806d4fd67c02b5bed5.
No clone, package, runtime, database or local build was created.

The pinned SDK constants identify GatewayToken proxy
0xF65b6396dF6B7e2D8a6270E3AB6c7BB08BAEF22E, matching the existing adapter.
This is a source-address match, not evidence of deployed bytecode or ABI.
The separate IGatewayNetwork interface exposes getNetwork(uint256),
getNetworkId(bytes32) and doesNetworkExist(uint256). Its network structure
uses a bytes32 name and includes authority and configuration fields.
The smart-contract README's newer deployment table is explicitly BNB testnet;
do not assume its GatewayNetwork address or ABI applies to the existing
mainnet GatewayToken proxy.

Next gate: establish one exact deployment/ABI/network identity and a small
reviewed public catalogue before implementing its CQL read contract. A
registry configuration representation must retain its native scope and must
not be described as verification of a person. No source writes, wallet,
credentials, owner decision or hosting change is required for this review.

Compact tree and selected contract/hash receipts are retained under
LocalState/COSMIC/RecoveryLaptopResume20261107/civic-gateway-*.json.
Native, manager, HTTP, replication and browser gates remain open.
