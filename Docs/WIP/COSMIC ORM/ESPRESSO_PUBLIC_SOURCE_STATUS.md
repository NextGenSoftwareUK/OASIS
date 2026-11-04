# Espresso public header source

4 November 2026. The official mainnet query service answered anonymous
JSON GETs at /v1/availability/header/{height} for heights 1, 2 and 3.
All returned the requested height, embedded chain ID 1, and a canonical
32-byte L1 finalized hash.

| Height | Unix timestamp | Reported finalized L1 block |
| --- | --- | --- |
| 1 | 1730830678 | 21123092 |
| 2 | 1730830688 | 21123124 |
| 3 | 1730830697 | 21123124 |

Use the pinned mainnet query-service endpoint and strict height and schema
validation for a bounded read profile. Report the L1 data as returned by the
query service. Independent L1 and Espresso finality verification remains
outside this probe. CQL adapter, local WEB4 federation, and hosted acceptance
remain open. No runtime or output files were needed for the probe.

[Official Espresso query API documentation](https://docs.espressosys.com/network/developer/espresso-api)
