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

## WEB4 admission

The protected profile requires the canonical teaching owner setting
`CQL_WORLD_ID_PROTOCOL_QUERY_SOURCE_OWNER`. It uses its own read permission
and tariff. At Core `c1354d87da2add7720f9c40d4e502195c01a3f41`, all 29 combined admission
checks passed with zero skips. The initial 27 passed, and the count
guard exposed two absent invalid-owner cases. The expanded consumer
ran against the unchanged producer. The 52,144-byte final TRX
SHA-256 is
`887bf56bef6798423c9d1383ad47b84796df95a9e1b1bc62d34cf54d392dd29c`.
Protected configuration passed 39 tests and response evidence passed 21.

## Local WEB4 and Mongo federation

At Core `c1354d87da2add7720f9c40d4e502195c01a3f41`, local
WEB4 HTTP returned native 3/2, paired 5/4, foreign-private 0 and
foreign public 3. The combined run passed both cases with zero
skips. The 4,724-byte TRX SHA-256 is
`55846eda17f2707263b62c42502c1e6faac38094b859e70e812136dd441d9e7a`.
The exact synthetic database was removed after verification.
Hosted HTTP and browser acceptance remain open.
