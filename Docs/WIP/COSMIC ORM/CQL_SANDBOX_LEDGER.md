# CQL sandbox workstream ledger

Branch `work/cql-sandbox-providers` (worktree `C:\Source\OASIS2-cql-sandbox`). This workstream covers the live WEB4 CQL sandbox and provider CQL demos. ORM/EF parity rows belong to the COSMIC ORM roadmap session in `C:\Source\OASIS2`.

## Progress

- Done: 14 of 15 priority providers have a live sandbox demo. The live sweep on 24 Nov passed 208/208 presets across 107 providers.
- Done since last update: InfluxDB/Weaviate hosted registrations (Development `59094ffc`, by the roadmap session); confirmed CI green.
- HoloOASIS cold start: the timing log (Development `d0e8cc33`) shows conductor connect 2.2s, then 96.1s for the persisted cell to start running (Holochain network join), then about 1s to install, activate and check the fixture, about 98s in all. That is inside the 480s budget. The earlier 7-minute boot could not be measured after the fact; the log line will show which phase is slow if it happens again.
- Done: browser check of the live CQL Playground (HoloOASIS returns its 3 teaching records). Web4Site 6930aff removes 49 laptop-only pwsh launcher steps from the public page; Web4Site f301621 adds sample records so the built-in MongoDB/SQLite/Neo4j presets return rows for new visitors (verified live). Both are on Web4Site `work/cosmic-cql`; public promotion waits on David.
- First federation sweep: 129 of 208 passed in the first run; the failures were an expired test token and a WEB4 redeploy mid-run. Optimism, Linea and Scroll pass on retry.
- Done 25 Nov: clean full live sweep, 208/208 plain presets and 208/208 federation presets across 107 providers (Development `e88664cd`). IPFS/SOLID/ActivityPub test projects now reference the WebAPI project and build in CI (run 45528232647, `11c7dbdd`). Web4Site 3099bf0 hides 127.0.0.1 local-demo links for public visitors.
- Done 25 Nov, browser check of every sandbox tab on the live page with a fresh demo session. Import Your Data: demo shop created in MongoDB, SQLite and Neo4j; preview, import (5 created) and the first holon read separately from all three stores. Failover: with MongoDB offline for the session, the load failed over and SQLite served the replica. COSMIC ORM: all four steps OK (save graph, load with children, query children, find by metadata). Ask Your Data is the only tab not working; it is blocked on David (free WEB6 model route).
- Cosmetic, not fixed: the failover route JSON shows provider enum numbers (47, 49) rather than names; the route table below it is readable.
- Remaining work in this lane is blocked on David (see Blockers).

## Blockers

- Ask Your Data tab: the code is finished on Web4Site `work/cosmic-cql`, but WEB6 dev has no zero-price model route (`WEB6_FREE_ROUTE_UNAVAILABLE`). David needs to add a Gemini free key, an OpenRouter key or an Ollama endpoint (ACTIONS-FOR-DAVID, "WEB6 Ask your data Development demo entitlement").
- Blockscout sometimes shows unavailable after a restart because a public read takes more than 15s during its one startup activation. Retries cover only 5xx errors by design; no code change, it recovers on a later restart.

- TRON: needs a TronGrid API key from David (`COSMIC_TRON_PUBLIC_READER_KEY`, see ACTIONS-FOR-DAVID.md).
- InfluxDB/Weaviate live demos: need hosted accounts and read-only tokens from David (ACTIONS-FOR-DAVID.md, "Hosted InfluxDB and Weaviate read-only CQL sources").
- HyperDrive session answers (24 Nov): the IPFS catalogue root reset on every boot is fixed on `work/hyperdrive-triad` (`f4bd7089`, new DNA key `IPFSOASIS.CatalogueRootPointerPath`) and deploys with its next batch. The Neo4j->SQLite sequence 14 hold is the designed hold and waits on David's approval (ACTIONS-FOR-DAVID I1).
- Originally handed to the HyperDrive session on 24 Nov: live replication Neo4j->SQLite blocked at sequence 14 (`COSMIC_REPLICA_LINEAGE_CONFLICT`), and the IPFS replication source has no persisted catalogue identity.

## Live sweep 25 Nov 2026

- Dev sandbox catalogue: 107 providers, 103 with presets, 412 presets (206 Rows, 206 Federation). Only BlockscoutOASIS is unavailable.
- Result: 411/412 first pass and 412/412 after one retry. The single miss was ArweaveOASIS `public-records`, which hit its 5-second budget once on a slow public gateway (CQL_QUERY_CANCELLED); three retries took 2.1-2.3 s each.
- The earlier 129-ok / 79-bad sweep came from the sweep script's sandbox token expiring mid-run (every later call was a 401), not from the service. The script now refreshes the token every 4 minutes and after any 401.
- I1 (Neo4j->SQLite sequence 14): David chose to keep SQLite's v18. A one-shot `--cosmic-retire-held` command is on `work/i1-retire-held`. It waits for David's approval of the exact request before its single run on dev.
