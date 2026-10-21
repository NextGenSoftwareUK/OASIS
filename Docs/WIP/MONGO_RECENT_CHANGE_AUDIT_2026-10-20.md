# MongoDB recent-change audit — 20 October 2026

## Scope and baseline

User reports normal performance until approximately the last one to three days. Prioritize 17–20 October and deployed revisions, retaining a two-week baseline to identify earlier changes that may have reached dev later.

Git baseline before 7 October: `dc653aa29c503c45ce002b661521ed12501f10d1`. Audited deployed parent: `c4646ccbfd141b79d41aa31fdf19f48f96f9fa30`. The Mongo provider directory has 96 commits in the 6–20 October history window and 39 changed files between that baseline and the audited deployment. This is substantial provider work, not solely changes to Cosmos DB.

Raw history: `Docs/WIP/mongo-history-2026-10-06-to-20.txt`.

## Last 72 hours: actual changes

| Date | Commit | Mongo change | Performance relevance |
|---|---|---|---|
| 19 Oct | `74ee81936` | Correct BSON UUID byte-order handling | Metadata conversion; does not introduce a network wait |
| 19 Oct | `5bf5965ca` | Throw for unsupported metadata BSON instead of silently returning null | Error correctness; no new database call in this change |
| 19 Oct | `bbe88a532` | Reject cyclic metadata instead of silently truncating it | Serialization correctness; no new database call in this change |
| 18 Oct | `eac138c75`, `2b2960549`, `db8e4b458`, `4fe300176`, `465743035`, `8ea35e904` | Preserve previously omitted skills, structured profile, collection, chakra and inventory values | Changes mapping and what subsequent saves retain; may increase persisted data compared with previously lossy saves. No historical document-size snapshot was available to quantify growth. |
| 17 Oct | `ec0d4e37f` | Initialize serialization conventions once before class mapping | Changes initialization ownership; no repeated read wait was introduced in this commit |

There are also many **uncommitted** Mongo changes in the shared workspace (including conditional writes, replication/outbox, graph and precision work). Those are not evidence of code already deployed and must not be swept into a release or blamed for live behavior without checking the deployed revision.

## Deployment timeline

Earlier successful WEB4/WEB5 parent: `4da8ad8173f06e36b7bd47921c5eb59d53e05da7`, created 19 October 18:50 UTC (13:50 America/Cancun).

Later successful parent: `c4646ccbfd141b79d41aa31fdf19f48f96f9fa30`. WEB4 reached successful status at 20 October 04:45 UTC (19 October 23:45 America/Cancun); WEB5 at 04:49 UTC (23:49 America/Cancun).

The Mongo provider directory is identical between these two deployed parent revisions. Relevant AvatarManager/HolonManager/HyperDrive routing files and quest controller files are also unchanged between their corresponding dependency pins. The dependency pins changed for other Core/STAR features, but that is not evidence of a new Mongo read path in the later deployment.

The recorded quest reads already took 5.459/5.485 seconds on 19 October around 20:09 UTC (15:09 America/Cancun), before the later deployment. Therefore the later deployment cannot be the start of the observed delay. Earlier provider changes, database-state changes and shared-database traffic remain relevant.

## Earlier changes relevant to regression

| Date | Commit | Concrete change |
|---|---|---|
| 13 Oct | `fa102f8a5` | Avatar-detail save now first reads the persisted entity to preserve native identity and creation audit. This adds a full-profile read. |
| 14 Oct | `017c3ae06`, `fe69a1382` | Single-root and bulk graph writes moved to native transactions. These affect writes; they are not executed by a plain read-only raw-driver find. |
| 15 Oct | `9ef219c01`, `71838f825` | Single/collection reads moved to graph projection, with snapshot sessions for provider-loaded children, JSON round-trip cloning and, when provider child loading is requested, reading all eligible holons for projection. This is real added graph work and deserves performance correction independently of the wire-read delay. The game quest path requests children through the manager, not provider-side full-collection hydration. |
| 15 Oct | `18a602cc3`, `eefaf4f7a` | Shared native writer adds exact persisted-BSON observation, whole-document conditional replacement, unknown-field preservation, and majority/journal acknowledgement. Adds a full-profile read and repeats the original payload in the write condition. |
| 15 Oct | `70ca63b73` | Metadata matching changed from `$toString` to literal/scalar conversion semantics. Controlled comparison below does not show it causing the multi-second delay. |
| 16 Oct | `5bf6fac86` | Provider activation publishes state only after required repository/index initialization succeeds. This can affect cold activation, not the raw-driver warmed repeated-find test. |

The connection-string-keyed singleton MongoClient cache, pool limit of 30, and MongoDB.Driver package version are unchanged between the Git baseline and audited deployment. No introduced `Task.Delay`/`Thread.Sleep`/explicit throttler was found in the Mongo provider read implementation. This does not establish the absence of waits in every upstream dependency or in Atlas infrastructure.

Hosted domain change-capture source was last changed before this two-week window. The live service logs repeat a domain-capture failure. Increased background/test traffic could still affect the shared Atlas cluster indirectly; that has not been measured or ruled out. Both WEB4 and WEB5 point to the same Atlas database.

## Controlled historical query comparison

Implemented the exact 6 October metadata predicate shape from Git in the existing standalone MongoDB.Driver diagnostic, alongside the current predicate. Both request **full documents**, use the same connection/database, run without HyperDrive/COSMIC, and perform no mutations:

| Predicate | Full BSON bytes | Rows | Elapsed | Server explain execution |
|---|---:|---:|---:|---:|
| 6 October | 419,565 | 32 | 4,588 ms | 1 ms |
| Current | 419,565 | 32 | 4,575 ms | 1 ms |

Returned physical record sets were identical. The new metadata predicate does not explain the 4.5-second delay on the current database. This compares historical query **shapes against current data/infrastructure**, not historical deployment performance or historical data sizes. It must not be represented as a complete old-versus-new provider benchmark.

Evidence: `C:/Source/Our-World/Docs/WIP/quest-mongo-performance-history.json`. Reproduce with `Scripts/Test-QuestMongoPerformance.ps1 -HistoryComparison` after rebuilding the existing diagnostic project from cached dependencies.

## Conclusion and remaining question

Recent provider changes demonstrably add save I/O and graph processing; they cannot be dismissed. Conversely, disabling write concurrency cannot eliminate a delay reproduced by a raw read using the older predicate and no concurrency checks.

The current evidence isolates a roughly 90 KB/s effective full-result delivery path. It does not establish **why that delivery performance became slow recently**. Atlas tier/rolling transfer usage/throttle state and historical data-size/traffic evidence are required to distinguish infrastructure throttling, traffic-induced throttling, and newly larger persisted payloads. The user has been asked for Atlas account metrics because the available database credentials do not provide Atlas management access.

No implementation was rolled back, no concurrency protection was disabled, no database record was changed, and no performance fix was deployed during this audit. Broader Mongo-first remediation and the gameplay end-to-end checks remain outstanding.
