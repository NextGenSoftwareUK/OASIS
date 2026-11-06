# Livepeer public orchestrator CQL MVP

The read profile calls `isActiveTranscoder(address)` on Livepeer's
Arbitrum One BondingManager at
`0x35bcf3c30594191d53231e4ff333e8a770453e40`. Activation verifies the
Arbitrum chain and genesis, then reads one reviewed teaching probe.
Every set query reads one finalized header and binds all contract
responses to the same block hash using EIP-1898 `requireCanonical`.
Only the three reviewed public orchestrator addresses are admitted.

The first native preflight discovered these addresses in the published
BondingManager pool and confirmed all three reported active at finalized
block `0x1f1b1540` with hash
`0x2ce320fc143da5e51a000e7052ee56dc126b16d0147790ca642eb642fd4b8c5f`.

- `0x525419ff5707190389bfb5c87c375d710f5fcb0e`
- `0xac2e50c8f7ac0f82923a7df9d9903f6ec4741919`
- `0x4f4758f7167b18e1f5b3c1a7575e3eb584894dbc`

At Core `ddd996a55d41c2cd38711b08effc305ff5fd83a3`, the native
CQL checks passed exact 3/2, including one finalized block hash per
set. Controlled wrong-chain and malformed second-record checks also
passed. A malformed response publishes zero partial rows. Three passed
with zero skips. The 5,847-byte TRX SHA-256 is
`d5463133d5d3dea6d638adf3a387a3c865488b27e9f1c1ca68b7e75e442f6e1d`.

WEB4 admission passed. Local federation and hosted acceptance remain open. This
source covers public orchestrator activity metadata. Studio and compute
operations have separate contracts and acceptance.

[Livepeer protocol](https://github.com/livepeer/protocol) ·
[Pinned BondingManager ABI](https://github.com/livepeer/protocol/blob/962107f601e2037a62201e337efa476c4fa5a680/deployments/arbitrumMainnet/BondingManagerTarget.json) ·
[Arbitrum public endpoint](https://docs.arbitrum.io/chain-info).

## WEB4 admission

Set `CQL_LIVEPEER_ORCHESTRATOR_QUERY_SOURCE_OWNER` to the reviewed canonical teaching owner GUID.
The provider admits the fixed teaching catalogue through an independent
read-only permission and tariff. A reviewed settings descriptor
contains the single `Actor` field. The protected settings and HTTP
verifier derive the three exact logical IDs from that owner and the
native catalogue.

The WEB4 producer and tests passed seven Discord/Livepeer admission
checks and seven Polygon gateway regression checks at Core
`cec62ced64ddb5b913f595e4ffc901e95f4c86dd`. The 26,040-byte TRX
SHA-256 is `a4b9c0d872d508be606b816d04b1afa601e91865f3e42eee910245bf955b4346`.
The settings and query evidence suites passed 39 and 21 checks.
Development activation follows LocalFile.
