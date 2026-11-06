# Push Protocol public channel CQL MVP

The profile reads exact Ethereum channel addresses from the production
Get Channel route used by the official Push SDK. It validates the
returned address and exposes the reviewed public name, channel
address, chain and verification status. Channel settings, subscriber
data, message content and media stay outside the projection.

The reviewed channels are Push Protocol
`0xb88460bb2696cab9d66013a05dff29a28330689d`, ShapeShift
`0x90a48d5cf7343b08da12e067680b4c6dbfe551be` and ETH Tracker
`0xdbc5936e4daae94f415c39d284f6a69c4d553f2f`.

At Core `06a4363e1c26e616eddda34c2959acadab8dbba8`, the official
endpoint returned the exact 3/2 CQL rows. The controlled checks
rejected mismatched identity, invalid verification state and a
key outside the reviewed catalogue. All four passed with zero skips.
The 7,664-byte TRX SHA-256 is
`857babb7896c5f685e2d43a70495da5a5a34633d964fd82129d717928983cc11`.

The explicit WEB4 read profile requires the protected setting `CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER`.
At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, all 23 admission checks
passed with zero skips. The 41,744-byte admission TRX SHA-256 is
`b51d80c1903740dddf00ad219da9a9deea541cae29630dab236f0b87e069b2b2`.
The protected publisher passed 39 settings and 21 evidence tests.
Local federation and hosted acceptance remain open.
[Official SDK route](https://github.com/pushchain/push-notifications-sdk/blob/bdcdb9948c8696fc691d2cd38624d75979677f58/packages/restapi/src/lib/channels/getChannel.ts) ·
[Push Core verification documentation](https://comms.push.org/docs/notifications/push-smart-contracts/core-smart-contract/).
