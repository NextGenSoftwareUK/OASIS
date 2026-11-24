# CQL sandbox workstream ledger

Branch `work/cql-sandbox-providers` (worktree `C:\Source\OASIS2-cql-sandbox`). This workstream covers the live WEB4 CQL sandbox and provider CQL demos. ORM/EF parity rows belong to the COSMIC ORM roadmap session in `C:\Source\OASIS2`.

## Progress

- Done: 14 of 15 priority providers have a live sandbox demo. The live sweep on 24 Nov passed 208/208 presets across 107 providers.
- Done since last update: InfluxDB/Weaviate hosted registrations (Development `59094ffc`, by the roadmap session); confirmed CI green.
- In progress: HoloOASIS cold start. The cell takes about 7 minutes after boot (boot 21:27:37, first read 21:34:46 on 24 Nov). A phase-timing log line has been added to find where the time goes.
- Next 3: fix the HoloOASIS cold-start root cause; HoloOASIS private-data copies; HoloOASIS live HyperDrive writes.

## Blockers

- TRON: needs a TronGrid API key from David (`COSMIC_TRON_PUBLIC_READER_KEY`, see ACTIONS-FOR-DAVID.md).
- InfluxDB/Weaviate live demos: need hosted accounts and read-only tokens from David (ACTIONS-FOR-DAVID.md, "Hosted InfluxDB and Weaviate read-only CQL sources").
- Handed to the HyperDrive session on 24 Nov: live replication Neo4j->SQLite blocked at sequence 14 (`COSMIC_REPLICA_LINEAGE_CONFLICT`), and the IPFS replication source has no persisted catalogue identity.
