# Meilisearch bounded CQL demo scope

5 November 2026. Provider/shared build and 16 controlled tests pass, zero
failed/skipped: eleven new HTTP/CQL cases and five existing metadata checks.
No native runtime, manager/LINQ, HTTP/Mongo, hosted or browser proof yet.
Coverage counts remain unchanged.

The existing provider stores camel-case canonical Holon JSON in oasis_holons,
with id as its primary key. CQL now uses the shared bounded identity compiler
and exact GET /indexes/oasis_holons/documents/{canonical-guid} reads, after
intersecting server-owned authority. It does not invoke search scans or the
legacy implicit index-creation path. Read-only activation verifies both uid
and primaryKey through the [official index API](https://www.meilisearch.com/docs/reference/api/indexes/get-index).
Default HTTP transport refuses redirects; an injected client remains configured
and owned by its caller. Bearer credentials use the existing provider contract.

The adapter bounds each streamed document to1MiB and all accepted native bodies
to4MiB, with one15-second query deadline. Native ID and decoded read authority
must match; duplicate document properties reject. Deleted records are excluded
before canonical-key paging. Unsupported predicates/history reject explicitly.
No partial rows are published after any failure. Only HTTP404 with the native
code document_not_found is absence. Missing-index, auth/authorization, server
errors and malformed error envelopes remain query failures.

Controlled tests use an HttpMessageHandler, not a native server. They verify
3/2 selection and paging with deleted/missing controls, activation without writes,
index/primary-key mismatch rejection, wrong/duplicate IDs, oversized second
records and unchanged error semantics after a valid first row. They prove no
search or mutation route is requested by these CQL paths.

Next qualify one reusable real Meilisearch service, canonical retained records,
missing/deleted controls and source preservation; follow with manager/LINQ and
HTTP/Mongo pairing, privacy and replicas. Only then prepare WEB4 source/tariff/
preset enrollment. Wider content/search, writes/conditional concurrency, graphs,
history, replication, migrations and full ORM parity remain separate gates.

## Recovery-laptop native runtime preparation

The official community Windows v1.56.1 asset is cached once under
LocalState/COSMIC/RecoveryLaptopMeili1561. Source release:
https://github.com/meilisearch/meilisearch/releases/tag/v1.56.1 .
Published size353,019,904bytes and SHA256
ac6d2506cafe862c93a6add533e125a19852339ec21303413120bde84aad3a48
match the downloaded executable. --version reports meilisearch1.56.1. This is
the community asset, not the enterprise executable. The release repository
separately identifies MIT and enterprise BUSL code; no enterprise activation or
binding license prompt was accepted. No service, native index or documents yet.

MeilisearchCqlNativeTests uses COSMIC_MEILISEARCH_CQL_SOURCE metadata plus
COSMIC_MEILISEARCH_CQL_KEY in the process environment. It reads one existing
loopback source, validates3live/1deleted/1missing control, tests3/2/0selection
and paging, and requires identical document hashes before/after. Missing
control must be HTTP404with document_not_found, not a missing index. All native
observation bodies are bounded, and no test write or provisioning occurs.
A364-byte source.json was created once with fixed actor/five IDs and port17700.
Its records do not exist yet. Native bootstrap, actual test execution, manager/
LINQ and Mongo/HTTP remain open. Runtime identity alone is not CQL acceptance.

The leased native fixture build passed with zero errors. It has not run against
a server; no native coverage count or source-mutation claim follows.

## Actual Windows native acceptance — 5 November 2026

The pinned community runtime started on127.0.0.1:17700 with telemetry off and
indexing bounded to256MiB/two threads. One protected master key is retained with
Windows DPAPI; no secret was written to arguments or source config. One index
creation and one four-document batch were journalled before dispatch, and their
actual asynchronous task identities were polled to succeeded before ACKs.
No uncertain mutation was retried. Three live records, one deleted control and
one absent identity remain in the fixed source.

Native CQL test initial1/1passes, zero skips: exact3/2/0selections, paging,
independent deleted/missing controls and identical native document hashes before/
after reads. A retained restart passed1/1with zero skips and zero seed calls.
All four native documents matched the reconstructed original request body,
whose SHA256matches the immutable pre-dispatch intent, before/after the restart
test. Original batch SHA256:
05124a9bec7a59351e916b0a61720db9abd82eccfbbefb2c577c6a305cda0e81.

| Evidence | SHA256 |
|---|---|
| initial.trx | 9da7b261afbccb546c178eb2406c9b95b4a25e272a1b11017cf8db21343583d2 |
| retained.trx | 984daf539505d995894ead35259123aada4ac44745721bdabed950a33e1710d7 |

Reports: TestResults/MeilisearchCqlNativeCurrent. Compact provenance, source,
original intents/task ACKs and native-evidence.json remain under
LocalState/COSMIC/RecoveryLaptopMeili1561. Runner:
Scripts/TestHosts/run-cosmic-meili-windows.py. Its initialization refuses existing
data/intent; existing initial/retained run journals prevent accidental replay.
Only the owned Windows process was terminated after each quiescent test; the
port is closed. Retained database logical file bytes442,438. This verifies this
completed-write restart, not generic uncheckpointed crash durability or cluster
availability. Manager/LINQ walkthrough, Mongo/HTTP pairing, privacy/replicas,
WEB4 registration and browser acceptance remain open; global counts unchanged.

## Manager/Mongo/HTTP preparation and original startup failure

The real manager/federation/HTTP fixture and linked shared walkthrough/API host
compile with zero errors. Run-CosmicMeilisearchMongoDemo.ps1 reuses the admitted
Mongo8.2runtime/replica and retained Meili source. It refuses occupied ports,
existing run evidence and unresolved Mongo journals. The fixture scopes one
absent-before Mongo database, journals three SDK-owned copies before saves,
checks manager5/4provenance plus HTTP presets/forged-input rejection/source
failure propagation, then verifies exact contents/ownership before cleanup.
Meili activation uses only the read-only collection/index path; no reseed.

The first actual paired attempt is FAILED, not accepted. Mongo reached primary
but Meili did not become ready within30seconds. Its paired-server.log is empty.
The subsequent15-second stop wait also expired and obscured the original error.
No paired TRX or Mongo fixture intent exists: test dispatch was not reached.
Read-only reconciliation observed no Meili process and closed17700/27029/5116
ports; the unrelated older Mongo process was preserved. Startup cause remains
unresolved; memory/antivirus or source corruption must not be inferred without
proof. No restart/reseed or longer-timeout workaround was attempted.

Original state/logs and paired-failure-reconciled-observed.json are retained.
The runner now records PID immediately within its cleanup scope, saves the
operation failure and explicit dispatch-attempt state, records unknown stop
outcomes and groups operation/stop failures so neither is lost. Three controlled
process-boundary regression tests pass without starting a service. These fix
failure evidence, not the unresolved native startup. Manager/Mongo/HTTP remains
open; original native/restart acceptance and global counts are unchanged.
