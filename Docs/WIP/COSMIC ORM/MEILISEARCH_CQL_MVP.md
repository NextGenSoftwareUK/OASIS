# Meilisearch bounded CQL demo scope

Current checkpoint: native CQL/restart, manager/Mongo/HTTP and local browser
acceptance pass. The explicitly local Windows card is published on WEB4.
Hosted provider enrollment and full ORM remain open; the original failed startup
is preserved. Earlier sections below record the sequential evidence milestones.

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

## Read-only standalone startup diagnostic

After the original paired process was observed gone and ports closed, a separate
exclusive startup-diagnostic journal started the same pinned runtime/data/key,
with the same30-second budget and Mongo stopped. It became ready and verified
all four original-batch documents in0.687seconds. No mutation or test dispatch
occurred. PID21260and process I/O observations were recorded; owned termination
completed and port17700closed. This narrows the failed paired startup gate but
does not establish its cause. No timeout extension, reseed or test retry occurred.

The runner's diagnostic mode now captures process identity and I/O observations;
future observations include a post-readiness sample. Diagnostic receipt writes
cannot prevent the finally-owned stop attempt. Three process-stop regressions
still pass. The original paired failure remains unchanged. Next compare the
same read-only startup with the admitted Mongo process running, using separately
named diagnostic evidence; do not relabel a diagnostic as federation acceptance.

## Concurrent diagnostic and explicitly resumed manager acceptance

A separate concurrent-startup-diagnostic with the admitted Mongo process passed
in0.636seconds and verified all four original documents, with no mutation/test
dispatch. PID27028/I/O observations and both owned stop outcomes were retained;
all demo ports closed. Both standalone and concurrent conditions now work, but
the original startup cause remains unknown. This is not reliability certification.

The original pairing test had never dispatched and no Mongo fixture intent/report
existed. Explicit resume validates that reconciliation and both source-bound
read-only diagnostics, hashes all original evidence into a new paired-resumed
intent, and uses the exact existing source/runtime. It does not overwrite the
failed phase or repeat a mutation. Five controlled runner tests pass, including
rejection of changed source, pending Mongo state, prior test dispatch or mutation.

The resumed actual manager/Mongo/HTTP fixture passed1/1, zero skips. Through the
shared expression/manager walkthrough it returned native+Mongo physical5/4rows
with exact provider provenance, excluded an out-of-catalogue Mongo record, and
verified the same presets over a real loopback HTTP host. Forged actor/unknown
presets reject; deactivated native source yields503without rows. Its isolated
Mongo database was absent before three journalled SDK saves; exact owned data
and collection contents were verified before drop, absence confirmed, and the
pending journal removed. This is a local teaching host, not hosted WEB4 auth.

All four Meili documents matched the original acknowledged batch before/after,
with zero reseed calls. The owned processes stopped,17700/27029/5116closed;
the HTTP host stopped in its fixture finally. Original failed startup remains
retained. Report paired-resumed.trx SHA256:
3c2102ad8cac08765c6a5b12471e4953101217152da44b6ee802f481d2072f09.
Compact paired-manager-http-evidence.json and all phase receipts are retained
beside the single native source. Hosted enrollment, browser interaction, broader
privacy/replication and full storage/ORM parity remain open. Existing global
counts await explicit scope reconciliation; this milestone does not relabel
controlled-only providers or the original failed run as complete.

## Local interactive browser acceptance

The explicit -Serve launcher now reuses the same source and admitted environment,
with a unique session evidence ID and normal verified Mongo fixture cleanup.
Local IAB browser interaction passed on http://127.0.0.1:5116/ : clicking full
returned5physical rows (Meili3/Mongo2), then subset4 (2each), with exact provider
provenance in actual JSON. The page clearly names retained authenticated native
Meilisearch and warns that reads do not form a distributed snapshot. Its stop
button acknowledged shutdown, then the runner passed1/1/zero skips, removed the
verified temporary Mongo database and pending/ready markers, verified all four
original Meili documents and stopped owned services.17700/27029/5116closed.

Interactive run1f3e76130bf34a56ab6f24f796045b71 TRX SHA256:
816e4d41955d5ccb2c2779be14ef2bfae58c67ed80e52fbe561a88d1d39988a2.
Compact local-browser-evidence.json is retained with the source. This is a
local browser gate; hosted browser totals remain28verified/32open.
[Launch and presentation guide](../../Devs/MEILISEARCH_CQL_LOCAL_DEMO.md).

## Published WEB4 local catalogue card

Web4Site8d6d32013a08eebd66a44f30d1a63821f200cd94 publishes the Windows-local
Meili/Mongo card.112client tests pass; GitHub Vercel status is success. Public
HTML returns200and exactly matches committed bytes, SHA256:
7798ed838fc838752dafed471f57608e699ee11382adc81474141e50f7a76c71.
The guide and C# walkthrough pinned to parent9824864d860c13cc3f59f7bb60c681842cb0ae08
return200and match their git blobs. The new card's single launcher exists.
The actual published browser card rendered its local-only scope, expanded the
start instructions and showed the exact -Serve command and127.0.0.1:5116link.
No demo session, avatar or sign-in was created to inspect this static card.
API source enrollment, Railway settings and deployment remain unchanged.

Coverage reconciliation: this newly implemented provider was explicitly marked
not CQL-certified in the prior provider inventory and had no prior query reader.
Its native+manager/HTTP+local browser proof adds one distinct local profile to
the preserved96baseline:97=3canonical+57public/source+37other local. Hosted
HTTP60and hosted browser28/32remain unchanged. Full provider conformance is not
increased; do not count RavenDB/Typesense controlled-only work as native proof.
