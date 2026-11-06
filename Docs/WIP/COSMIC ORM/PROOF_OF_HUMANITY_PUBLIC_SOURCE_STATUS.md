# Proof of Humanity v2 public registry CQL

The proposed profile exposes exact public contract-address observations
from the official cross-chain registries on Ethereum and Gnosis. Each
selection captures a finalized block on each chain and binds every
`isHuman(address)` call to the corresponding canonical block hash. A
response error on either chain fails the selection. Each row retains
the two reported Boolean values and both block identities.
[Kleros integration guide](https://docs.kleros.io/products/proof-of-humanity/proof-of-humanity-2.0-integration-guide) ·
[Official deployments](https://github.com/Proof-Of-Humanity/proof-of-humanity-v2-contracts)

6 November 2026 direct qualification verified chain identities and
canonical hash-bound ABI Boolean responses for three published contract
addresses. Both chains reported false for all three.

| Chain | Finalized height | Block hash |
| --- | --- | --- |
| Ethereum | `0x1922bd3` | `0xc969124e443c552d40c84a7cfa24b63c4809911cbd62e44cf706c6088ae2a7d0` |
| Gnosis | `0x2edd707` | `0xe02e11eba9efbb5b1e8d909f35c32e552fc73d391a3d85a8a9233bc59a35d80f` |

The leased suite passed three controlled cases and one real CQL 3/2
query on both public registries, with zero skips at Core
`e400afd53cef4997e8bfcabc4b34b3f84a854626`. The TRX SHA-256 is
`163468926aeef8240d046f43c00fc22cc8a4464a732e1a95026a14ed99a21c47`.

The scoped source publishes the reviewed addresses, registry Booleans
and block identities. The existing provider owns its separate identity
operations. The combined opt-in WEB4 admission passed seven new profile
checks and 12 Cloudinary and pgvector regressions at Core
`c2a8b2303264bd159ac55d71b684c712647c6d3b`. The admission TRX SHA-256 is
`a9d49e94de9b6223950850dbeae8df7c1dfdfb6b6d8e31140ab53689e49f3da8`.
The public registry tests also passed again. Live verification remains
open. LocalFile remains first for live rollout.
