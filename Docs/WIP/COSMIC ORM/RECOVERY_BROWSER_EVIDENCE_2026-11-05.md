# Development browser evidence, 5 November 2026

Live page https://web4.oasisomniverse.one/web4-sandbox.html.
Development selected. One demo session, sandbox-ca8924da, owner
b1f86140-00a9-4017-9b6c-af9eade65c76. Reuse this browser session.

| Provider | Native full/subset | Mongo full/subset | Preparation/reuse | Independent exact-ID privacy |
|---|---|---|---|---|
| SolanaOASIS | 3/2 | 5/4 | 2 saved, then 0 saved/2 prepared | MongoDB, SQLite, Neo4j each 0 |
| GoogleCloudOASIS | 3/2 | 5/4 | 2 saved, then 0 saved/2 prepared | MongoDB, SQLite, Neo4j each 0 |
| EthereumOASIS | 3/2 | 5/4 | 2 saved, then 0 saved/2 prepared | MongoDB, SQLite, Neo4j each 0 |
| PolygonOASIS | 3/2 | 5/4 | 2 saved; reuse pending | Pending |

Solana private IDs are 3066ca99-2a54-885f-afad-8b104f13b653 and
b7f878cb-cd6d-8bf6-b075-e86856ade714. Google private IDs are
320c306b-485e-895b-91e9-b407ceb03e1c and
ddcdee44-dac8-815b-bf8c-d01adef3e65d.
Ethereum private IDs are bc09569b-eb32-8878-8d53-bf70b51fdcb1 and
f519b537-5160-8714-aa9d-6a507fe468f3. Its subset table showed both
Mongo copies at version 1 and both native rows at version 0.
Polygon private IDs are 0629d889-f2dc-8b73-8fd1-a6335941db49 and
3c8d23d1-bad2-89df-82b5-4fc8c078b314, both version 1. Polygon reuse,
privacy and rendered replica comparison remain open.

Solana and Google subset screenshots were emitted and inspected in the
chat. Both show four physical rows with native and Mongo provider labels,
and preparation reuse. Images have no retained standalone path. Privacy
checks used HTTP with the independent retained rehearsal visitor.

The same owner queried all six private IDs through the browser on MongoDB,
SQLite and Neo4j. Each provider returned six rows, all at version 1.
Rendered name, description, version, ID and metadata matched exactly after
excluding the provider column. This verifies the displayed content on both
replicas for all three pairs. These three flows satisfy native and paired
browser execution, setup reuse, independent privacy and rendered replica
content checks. They increase the scoped browser count from five to eight
and reduce the open count from 55 to 52. Ethereum's standalone screenshot
and durable browser credential export remain absent. Existing accepted
HTTP cohorts remain retained.
