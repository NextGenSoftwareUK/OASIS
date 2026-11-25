# CQL sandbox workstream ledger

Branch `work/cql-sandbox-providers` (worktree `C:\Source\OASIS2-cql-sandbox`). This workstream covers the live WEB4 CQL sandbox and provider CQL demos. ORM/EF parity rows belong to the COSMIC ORM roadmap session in `C:\Source\OASIS2`.

## Progress

- Done: 14 of 15 priority providers have a live sandbox demo. The live sweep on 24 Nov passed 208/208 presets across 107 providers.
- Done since last update: InfluxDB/Weaviate hosted registrations (Development `59094ffc`, by the roadmap session); confirmed CI green.
- HoloOASIS cold start: the timing log (Development `d0e8cc33`) shows conductor connect 2.2s, then 96.1s for the persisted cell to start running (Holochain network join), then about 1s to install, activate and check the fixture, about 98s in all. That is inside the 480s budget. The earlier 7-minute boot could not be measured after the fact; the log line will show which phase is slow if it happens again.
- Done: browser check of the live CQL Playground (HoloOASIS returns its 3 teaching records). Web4Site 6930aff removes 49 laptop-only pwsh launcher steps from the public page; Web4Site f301621 adds sample records so the built-in MongoDB/SQLite/Neo4j presets return rows for new visitors (verified live). Both are on Web4Site `work/cosmic-cql`; public promotion waits on David.
- First federation sweep: 129 of 208 passed in the first run; the failures were an expired test token and a WEB4 redeploy mid-run. Optimism, Linea and Scroll pass on retry.
- In progress: full live sweep (rows + federation) after the deploys settle; IPFS/SOLID/ActivityPub test projects build fix.
- Next 3: fix anything the full sweep finds; IPFS test-project build; next unclaimed sandbox item.

## Blockers

- Ask Your Data tab: the code is finished on Web4Site `work/cosmic-cql`, but WEB6 dev has no zero-price model route (`WEB6_FREE_ROUTE_UNAVAILABLE`). David needs to add a Gemini free key, an OpenRouter key or an Ollama endpoint (ACTIONS-FOR-DAVID, "WEB6 Ask your data Development demo entitlement").
- Blockscout sometimes shows unavailable after a restart because a public read takes more than 15s during its one startup activation. Retries cover only 5xx errors by design; no code change, it recovers on a later restart.

- TRON: needs a TronGrid API key from David (`COSMIC_TRON_PUBLIC_READER_KEY`, see ACTIONS-FOR-DAVID.md).
- InfluxDB/Weaviate live demos: need hosted accounts and read-only tokens from David (ACTIONS-FOR-DAVID.md, "Hosted InfluxDB and Weaviate read-only CQL sources").
- HyperDrive session answers (24 Nov): the IPFS catalogue root reset on every boot is fixed on `work/hyperdrive-triad` (`f4bd7089`, new DNA key `IPFSOASIS.CatalogueRootPointerPath`) and deploys with its next batch. The Neo4j->SQLite sequence 14 hold is the designed hold and waits on David's approval (ACTIONS-FOR-DAVID I1).
- Originally handed to the HyperDrive session on 24 Nov: live replication Neo4j->SQLite blocked at sequence 14 (`COSMIC_REPLICA_LINEAGE_CONFLICT`), and the IPFS replication source has no persisted catalogue identity.
