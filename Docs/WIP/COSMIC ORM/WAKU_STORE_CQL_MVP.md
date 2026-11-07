# Waku Store v3 CQL MVP

The read profile uses the official Store v3 exact hash endpoint on
an isolated node. An explicit reviewed catalogue admits up to four
keys. It verifies the returned hash, reviewed pubsub and content
topics, and the protocol SHA-256 over the topics, payload, metadata
and big-endian timestamp. The public holon includes hash, topics,
timestamp and payload size. Payload and metadata bytes stay out of
the projection.

The pinned v0.39.2 Linux fixture runs two real peer-to-peer nodes
with loopback listeners and one SQLite archive. One peer publishes
three synthetic messages and the other stores them. Native REST and
CQL both returned exact 3/2/0. The modified-payload control also
passed, for a total of two focused CQL checks with zero skips.

The initial REST qualification returned HTTP 400 because the fixture
combined exact hashes and topic filters. The pinned API permits hash
selection independently of content filters. The corrected native
request validates the returned topics against the reviewed pair.

The source currently uses only the fixture's loopback address and
topic. WEB4 admission, a reviewed hosted Store and hosted federation
remain open. General relay publication and retention are separate
provider scopes.

[Original Linux run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42289148713) ·
[Passing native REST run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42289322076) ·
[Passing CQL run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42289754507) ·
[Pinned native hash implementation](https://github.com/logos-messaging/logos-delivery/blob/fd07b72c370a6b39c69870c087cc0d928d485327/logos_delivery/waku/waku_core/message/digest.nim).

### WEB4 configuration contract

The reviewed teaching descriptor accepts only a canonical sourceOwner and
three distinct lowercase Waku messageHashes. The source retains its fixed
loopback endpoint. Focused descriptor and forged-payload tests passed (2/2).
Evidence: TestResults/WakuStoreCqlCurrent/descriptor.trx, SHA-256 D926E7A290AA059A29F503E61E83CA7EE928473A9C90C11C2A1B3BF73009C51A.
WEB4 registration, tariff and Mongo federation proof remain open.
