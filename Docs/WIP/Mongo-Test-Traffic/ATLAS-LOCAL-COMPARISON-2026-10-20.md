# Atlas versus local MongoDB: identical matching data

Used the same native MongoDB.Driver 3.12 diagnostic process, predicates and complete BSON results. Read 32 matching quest Holon documents (419,565 bytes) and one avatar-detail document (56,639 bytes) from dev Atlas, copied only those matching records in memory into isolated local MongoDB on 127.0.0.1:27017, then alternated three Atlas/local reads per dataset. Every read's sorted BSON SHA-256 matched its initial snapshot. No OASIS, COSMIC ORM or HyperDrive runtime participates in this benchmark.

| Dataset | Atlas three reads (ms) | Local three reads (ms) |
| --- | --- | --- |
| Full quest records | 4583.74, 4610.45, 4574.90 | 15.40, 4.53, 2.36 |
| Full avatar detail | 664.82, 715.40, 711.80 | 18.42, 1.69, 1.38 |

Atlas ID-only projections took 73.56 ms for quest records and 53.41 ms for avatar detail. Atlas explain execution times were 1 ms and 0 ms respectively. Local projections took 1.34/1.02 ms. The first local full reads include warm-up effects; medians are 4.53 ms for quests and 1.69 ms for avatar detail.

## Conclusion and limits

The same result bytes and query logic are fast locally. The multi-second cost is in the hosted database/network path, rather than intrinsic computation required by these documents or the native driver. This comparison does not distinguish Atlas quota throttling, shared host resource contention, TLS/network behaviour or route-specific throughput. It does not prove that an Atlas upgrade will solve it. Atlas quota confirmation is still needed.

This is a native database read benchmark, not a full local WEB4/WEB5 end-to-end or write benchmark. Local includes only matching records, with ordinary source index keys recreated; it is not a full cluster clone, and unique/TTL settings were not copied. Atlas examined 612 quest records, local examined 32; Atlas still reported only 1 ms query execution. Atlas version 8.0.34, local 8.3.11. Both accessed from the same machine, with the expected transport/topology differences (remote Atlas versus loopback local).

Owned local database `quest_perf_atlas_compare` was dropped in finally. No raw account/quest payload was exported to evidence files; only sizes, hashes, timings and counts. Zero Atlas writes. Compact evidence: [atlas-local-identical-data.json](atlas-local-identical-data.json).

## Atlas checks and support request

In Atlas, select project OASIS, then Clusters → OASIS → Metrics. Inspect Network (Bytes In/Out) and Opcounters across one week. These charts do not themselves establish the quota-accounted rolling total; do not sum all replica-member traffic and treat it as proof of usage.

Official documentation lists Free/M0 limits of 10 GB incoming and 10 GB outgoing over a rolling seven-day period and 100 read/write operations per second. Exceeding the limits can throttle network transfers and introduce connection cooldown/queuing. Free monitoring exposes only Connections, Logical Size, Network and Opscounter; the storage percentage is not a transfer-quota gauge.

Use Atlas's bottom-right in-app support chat (Basic support available), and paste:

> Please check whether our OASIS Free/M0 cluster in AWS us-east-1 is currently being throttled. On October 20, 2026 around 08:42 UTC, native MongoDB.Driver 3.12 full quest reads returned 419,565 BSON bytes in 4.57–4.61 seconds, while explain reported 1 ms and an ID-only result took 74 ms. Identical BSON data and predicates on local MongoDB returned in 2–15 ms. Avatar-detail reads (56,639 bytes) took 665–715 ms on Atlas and 1–18 ms locally. Could you confirm the actual quota-accounted incoming/outgoing totals over the rolling seven days, whether data-transfer or operations-per-second throttling is active, when it began, and when usage is expected to fall below the limit? Do the Network charts include replication/internal traffic that is excluded from this quota? If no throttling is active, please investigate shared-tier resource contention or unusually low result-transfer throughput. What aggregate source/application traffic information is available for attributing the usage?

Sources checked October 20, 2026:
- https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/
- https://www.mongodb.com/docs/atlas/support/
