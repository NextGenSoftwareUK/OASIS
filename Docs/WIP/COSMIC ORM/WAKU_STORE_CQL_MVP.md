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

### WEB4 admission verification

The CQL_WAKU_STORE_QUERY_SOURCE_JSON setting adds a read-only profile in
sandbox mode. WEB4 admits explicit Waku scope with a dedicated read tariff.
The focused registration and tariff suite passed 28/28.
Evidence: TestResults/WakuStoreCqlCurrent/web4-admission.trx, SHA-256 D3BA8A478BCA338B40661E1E92161849DD46B6326106ADA231AA87654891EE52.
Next is native fixture activation through WEB4 and Mongo federation.
