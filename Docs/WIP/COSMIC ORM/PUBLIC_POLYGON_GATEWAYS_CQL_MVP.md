# Tenderly and QuickNode Polygon CQL MVP

The two query adapters use their respective official anonymous Polygon
PoS RPC origins. Each independently verifies Polygon mainnet chain ID
`0x89` and the genesis hash, then reads account balances at a finalized
block with EIP-1898 canonical block-hash selectors. The reviewed teaching
addresses are the zero address, the conventional burn address and
Polygon's native-token system address. The adapters expose exact
address-set reads with separate provider identities and metadata keys.
The query clients hold no API key, wallet or signer.

At Core `c682a69c766160f723c391a44c20f9a95d455afa`, the live
Tenderly and QuickNode endpoints each returned the requested 3/2 CQL
rows. The test checked the exact key sets, public owner, finalized
commitment, and a common block hash per read. Both wrong-chain controls
failed at the first RPC. Four passed with zero skips. The 7,960-byte
TRX SHA-256 is
`cee431fd17a01bf6fc6d61ef5d5df46d11ed5d016ebcdb378967c4842e91f088`.

WEB4 admission, Mongo federation, hosted HTTP and browser acceptance
remain open. Tenderly simulation and QuickNode's general node APIs are
outside this balance demo.

[Polygon's public RPC list](https://docs.polygon.technology/pos/reference/rpc-endpoints#public-rpcs) ·
[Tenderly's Polygon gateway](https://tenderly.co/blog/changelog/tenderly-is-a-polygon-public-rpc-provider/).
