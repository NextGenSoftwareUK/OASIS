# Axelar public block source

4 November 2026. The anonymous Axelar LCD returned exact mainnet blocks
from https://axelar-rest.publicnode.com/cosmos/base/tendermint/v1beta1/blocks/{height}.
Each returned the requested height and documented chain ID axelar-dojo-1.
Block IDs use base64 on this route and each decoded to exactly 32 bytes.

| Height | Block ID in lowercase hex |
| --- | --- |
| 37269326 | 78e5cb63ccf196198e914242c58a7e840f0e59221130d3e8478c2bf1922cf53a |
| 37269327 | d4b4997b1c2b74b9ff3c2297ab6ad9b9e7e83b641514f25d445dedfeb021c744 |
| 37269328 | ef834975a9fc0d085fffc271386dae492278e20c6d676530c674df39a4c5a011 |

Use explicit height equality and bounded membership. Preserve the chain ID,
height, block ID and source timestamp in each row. This scope covers public
block observations. GMP message settlement and independent consensus
verification require their own evidence. CQL integration, local WEB4
federation and hosted acceptance remain open. No installs or outputs were
needed for the probe.

[Official mainnet chain identity](https://docs.axelar.dev/resources/contract-addresses/mainnet/)
