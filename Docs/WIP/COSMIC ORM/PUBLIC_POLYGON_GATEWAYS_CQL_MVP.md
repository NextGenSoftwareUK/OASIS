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

Mongo federation, hosted HTTP and browser acceptance
remain open. Tenderly simulation and QuickNode's general node APIs are
outside this balance demo.

[Polygon's public RPC list](https://docs.polygon.technology/pos/reference/rpc-endpoints#public-rpcs) ·
[Tenderly's Polygon gateway](https://tenderly.co/blog/changelog/tenderly-is-a-polygon-public-rpc-provider/).

## WEB4 admission

Set `CQL_TENDERLY_POLYGON_QUERY_SOURCE_OWNER` or
`CQL_QUICKNODE_POLYGON_QUERY_SOURCE_OWNER` to a canonical teaching owner
GUID. The independent profiles use the three reviewed addresses and
their fixed official endpoints. Each provider requires a separate
read-only admission entry and tariff.

The exact WEB4 producer and test consumer passed seven gateway
admission checks and seven Temporal regression checks at Core
`ddd996a55d41c2cd38711b08effc305ff5fd83a3`. The 25,973-byte TRX
SHA-256 is `8b426570434f0e3af205a13b624206641034841df82ce8e2f13c3b5ec96f70a3`.
The first producer build compiled WEB4 and found a missing import in the
new test. The corrected consumer passed against the same frozen producer
outputs.

After LocalFile, hosted configuration requires reviewed public source
IDs, examples and an independent receipt for each provider. If the
approved WEB4 policy uses encrypted Mongo metadata, the matching
`TenderlyPolygonAccountAddress` and `QuickNodePolygonAccountAddress`
keys must be queryable under that policy before enrollment.
