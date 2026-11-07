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

The reviewed local WEB4 HTTP batch verified native 3/2, paired MongoDB
5/4, and visitor isolation. Both native registry sources supplied
the selected rows. The owned two-record Mongo fixture was removed.
[Local acceptance scope](PROVIDER_CQL_AND_SANDBOX_STATUS.md#reviewed-public-source-http-on-6-november-2026)

## Joint hosted candidate

The scoped candidate dcae58d976d2af04800d1670fc1e4fe3979b5608 and Core
f695b1cd8880b3009b7b14190ab41867fb38ee65 are published on
work/cosmic-registry-object-release-candidate. This batch adds the public
Oracle object HEAD profile, Cloudinary image metadata and Proof of
Humanity registry reads. The shared registration includes only those
reviewed profiles. The existing native and local WEB4 proofs remain
the source acceptance. [Image CI 42336260220](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42336260220)
must pass before promotion and hosted acceptance.

The eight-value protected candidate and twelve presets use prefix
registry-object-public under LocalState/COSMIC/HostedSourceRolloutCurrent.
No hosted settings were applied. Oracle database hosting remains deferred.

Image CI 42336260220 passed for the exact candidate. Published image digest:
`sha256:90636c35d33788eeab3d2993a04ad551aacd4398bc11db91029925af46277692`.
Dependency pins, remote Core and live settings baseline were verified.
Development advanced to dcae58d97. Railway code job
900d0fe7-9dc9-489c-a12b-7df8ef84fea8 is queued; settings remain unapplied.

## Hosted HTTP acceptance

Verified on settings deployment `fb589d55-f737-4622-9160-6e63e0e55099`,
parent `dcae58d976d2af04800d1670fc1e4fe3979b5608`, Core
`f695b1cd8880b3009b7b14190ab41867fb38ee65`. Native selections returned
3/2 exact records. MongoDB federation returned 5/4. First preparation saved
two version-one private records; repeat preparation saved zero and reused two.
SQLite and Neo4j each returned both replicas. An independent visitor saw
three public records and zero private copies on all three storage providers.
The original cohort and save receipts are retained; acceptance must not be replayed.
Browser interaction remains unverified.

Evidence under `TestResults/HostedCqlIntegrationCurrent/`:

| Report suffix | SHA-256 |
| --- | --- |
| `ProofOfHumanityOASIS-native-http.json` | `6b3631e561fa6d0a8ffd77eef470a467f1d1dabbad3fc44ba8bb06070a00964a` |
| `ProofOfHumanityOASIS-client-http-flow.json` | `1aa675ef04a0f8da30137518787ba0259bd70f281b5f6781bb2b2c04069b6d7e` |
| `ProofOfHumanityOASIS-isolation-http.json` | `0dabd6b4c5b23fd66291b05cea074a35e80ef83486787e7e83ae23f5913ad299` |

Website `a88076d` publishes the hosted playground instructions and pinned
local C# example. All 114 client tests passed. Public HTML was HTTP 200,
239108 bytes, identical to the exact commit, SHA-256
`8773ee77638a24988968db32a15d58069424ac2e03604639a03d244198e65666`.
This verifies publication; functional browser interaction remains open.
