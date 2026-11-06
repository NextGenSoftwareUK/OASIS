# Pinata public IPFS block CQL MVP

The exact-key profile uses the official Pinata public gateway with
the IPFS raw block representation. It matches each block's bytes
against the requested CID using the existing vendored IPFS library.
The bounded read projects CID, codec, byte length and digest into
an owner-scoped public holon. The block bytes are read in memory and
remain on IPFS for subsequent retrieval.

The catalogue contains the root published in Pinata's gateway
documentation and two linked blocks from that verified root.

| CID | Raw bytes |
| --- | ---: |
| `QmP4eZj61tDxCmtb6aqSwQsrxRQAN2gwxTEXkTed9TujHh` | 344 |
| `QmeBt6f2Dem8QnZuZ8PDRT1iuWpphmza3gP17en2ZoReYR` | 262,158 |
| `Qmb733sded7TRm8U8bM4FHif89jUVKd8yQiurKCKgCLyQk` | 262,158 |

At Core `cec62ced64ddb5b913f595e4ffc901e95f4c86dd`, the actual
gateway returned exact CQL 3/2. Every row's digest, size and codec
matched. Three transport controls rejected the wrong digest, an
oversized block and the wrong media type. Four checks passed with
zero skips. The 7,479-byte TRX SHA-256 is
`43cedefc1ca18665f0491f9fddd815e7ab1609c23dfa45914690f7bb7f500a0b`.

The first test project compile found two outdated legacy test
assertions. The focused consumer passed against the same compiled
provider after the assertions were aligned with its current enum
metadata. WEB4 admission and federation remain open. Protected
Pinata pin management and canonical storage acceptance are separate.

[Pinata gateway guidance](https://knowledge.pinata.cloud/en/articles/6297294-public-gateways-vs-dedicated-gateways) ·
[Trustless gateway specification](https://specs.ipfs.tech/http-gateways/trustless-gateway/).
