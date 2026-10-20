# Neo4j outbox delivery store

Updated 19 October 2026. Local implementation, not published or deployed. Broader roadmap acceptance remains open.

`NeoHolonReplicationOutbox` implements `IHolonReplicationRecoveryOutbox` over a caller-owned `IDriver` and the existing default database. It neither creates a database nor initializes or repairs enrollment/schema. Activate the authoritative provider and explicitly enroll primary writes before using the worker. Driver configuration, credentials, transaction deadlines and ownership remain with the host.

## Ownership and clock

The earliest unfinished obligation for a target is locked before examining its current state. A held lease, future retry or reconciliation requirement blocks successors for that target. Other targets advance independently. Each claim has a fresh GUID token and increments the attempt count once. Renewal and finish require sequence, operation, target, worker and token to match the current unexpired owner after acquiring its native node lock.

Lease decisions use `datetime.realtime().epochMillis` converted to .NET UTC ticks after lock acquisition, with millisecond resolution. Worker wall clocks do not determine expiration. Renewal never shortens a lease. Retry timestamps must be explicit UTC values later than native server time. Dynamic worker identities, diagnostics and evidence are native query parameters; none become Cypher expressions.

The original frozen payload is SHA256-checked and decoded against operation, source, actor and Neo4j authority before claim. Corruption does not consume an attempt. Claims never rebuild payloads from current primary data. Completion, retry and reconciliation state and recovery evidence/checksum share a transaction fenced by the lease. Inspection excludes business payload and token. Evidence uses the shared exact versioned codec; opaque original native identifiers are retained without diagnostic truncation.

## Outcome and cleanup

The store owns explicit native transactions and does not use managed driver transaction retries. Unknown commit or failed rollback is a typed native reconciliation requirement; the current driver supplies no native transaction identity, so none is invented. All transaction/session cleanup errors remain attached. Known results survive close/dispose errors as warnings. Cancellation is checked before I/O and before commit; transactions are observed and closed, not abandoned in a background task. This does not provide a native pre-submission recovery journal or process/wire-loss certification.

## Verification

128 unit cases pass, including six no-I/O admission cases and five read/rollback/disposal ownership cases. The first two native store cases pass for synchronous/asynchronous primary enrollment: expiry and replacement-token fencing, independent targets, renewal, retry ordering, immutable payloads, recovery evidence, corrupt-payload refusal and competing claimants.

The expanded four-case focused native run passes and exercises a real claim commit followed by lost acknowledgement and both cleanup failures, and actual Neo4j-to-SQLite dispatcher creation, update, soft/hard deletion and exact target receipt replay. The 174-case native regression passed before the reverse SQLite-to-Neo4j cases were added. The expanded six-case focused SQLite/Neo4j/store suite and separate four-case MongoDB/Neo4j suite now pass, zero failures/skips. Final artifact hashes and commands are recorded in VERIFICATION.md.

Reproduction: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicOutboxDelivery`. Reuse the existing pinned local environment; the runner owns its lease and shuts down its service.

## Open gates

Automatic host registration/scheduling, authorized target catalog and source/tenant/actor/encryption context, authenticated reconciliation/resume, pre-submission and process-kill recovery, leadership changes, hosted cluster deployment, bounded payload/queue/retention policy and metrics remain open. All six selected worker directions now have [native evidence](OUTBOX_PROVIDER_DIRECTIONS.md). Explicit discovery/CQL [soft-delete visibility](SOFT_DELETE_READ_CONTRACT.md), broader child/history/migration/placement/delta/CQL acceptance and topology/deployment certification remain open.


Latest readiness checkpoint: [read-only native head inspection](COSMIC_REPLICATION_READINESS.md) preserves reconciliation blocks and corruption without consuming attempts. Current Neo unit129, focused native delivery6 and host/restart2 cases pass, zero failures/skips. The host/source readiness response excludes payload/token/exception text. ONODE factory/configuration/endpoint wiring remains open.
