# World ID v4 public protocol CQL MVP

This read profile covers three World ID v4 deployment components on
World Chain mainnet. It reads the registry's public root, tree depth
and root validity period, validates the verifier's registry wiring
and validates the issuer registry's OPRF registry. Each result set
uses one finalized EIP-1898 canonical block. The provider validates
chain ID 480 and the reviewed genesis. The projection contains no
individual identity, proof or credential data.

| Exact key | Contract |
| --- | --- |
| registry | `0x0000000000ae079eb8a274cd51c0f44a9e4d67d4` |
| verifier | `0x00000000009e00f9fe82cfeebb4556686da094d7` |
| issuer-registry | `0x941239840f4d9668da8be76b568e836b50685d2c` |

[World ID v4 production deployment manifest](https://github.com/worldcoin/world-id-protocol/blob/986e1cd5b149d10d0f7c4f3f6c98c65b55955b66/contracts/deployments/core/production.json) ·
[Alchemy World Chain endpoint](https://www.alchemy.com/rpc/worldchain).

At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, the
native CQL returned exact 3/2 rows with one finalized block per
selection. The wrong-genesis and wrong-verifier topology controls
passed, including complete query failure without partial rows.
Three checks passed with zero skips. The 5,770-byte TRX SHA-256 is
`b08a7abf39d63aa28b80991fb1460579f4d5b0200dda950500255a1f9cae17a4`.
WEB4 admission, local federation and hosted acceptance remain open.
