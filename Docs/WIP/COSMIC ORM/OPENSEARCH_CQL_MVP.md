# OpenSearch bounded CQL scope

Current checkpoint: create-only native bootstrap, CQL3/2/0/paging/source
preservation and retained restart pass. Owned hidden-console Ctrl+C and native
node shutdown are verified. Manager/HTTP/browser remain open; counts stay99.
Earlier sections preserve the initial adapter/package checkpoint history.

5 November2026. Provider/unit26/26 passes, zero failures/skips:21new CQL cases
and5existing metadata checks. The native reader fixture builds(0errors/4warnings)
but has NOT run. No native server, index or source records have been created.
Coverage remains99local/60hosted HTTP/28hosted browser,32browser gates open.

## Query contract

The existing provider writes canonical camel-case Holon JSON directly into
`oasis_holons`, keyed by the canonical GUID. CQL uses the shared bounded identity
compiler with at most100explicit authorized IDs and native reads:

```
GET /oasis_holons/_doc/{id}?realtime=true&_source=true
```

Read-only activation requires a configured existing live teaching-record ID,
verified through the same decoder. It never provisions indices or calls normal
storage activation. A single exact index and default routing are the current
contract; aliases, custom routing, wider search/vector queries, historical
records and full ORM are not admitted by this bounded profile.

Each envelope must match the configured _index and requested _id; its _source.id
must match too. Duplicate wrapper/source properties, invalid payloads or missing
_source fail. Only HTTP404 with the matching index/ID, found=false and no _source
means a missing record. Missing-index errors, unauthorized/forbidden/server errors,
HTTP200/found=false and inconsistent404 bodies never become empty success.
Read authority/deletion are checked; children excluded, canonical order/paging
applied after filtering. All errors are OASISResult failures without partial rows.
Responses are bounded1MiB each/4MiB aggregate under one15second deadline.
Default HTTP redirects are disabled. No managed predicate scanning or fallback
into the legacy CRUD reader occurs.

Controlled tests cover3/2/paging with deleted/missing controls, identity/index
mismatch, malformed/duplicate/inconsistent native envelopes, source absence,
authorization/server failures, activation, cancellation/history/inactive states
and transport budgets. The legacy normal activation/CRUD helpers still contain
unchecked responses; these are NOT certified by the new CQL path and remain
full-provider work. No placeholder/shim was added to mask that gap.

## Retained Windows package qualification

Official OpenSearch3.9.0 Windows bundle downloaded once into
LocalState/COSMIC/RecoveryLaptopOpenSearch390. Archive1,050,303,612bytes matches
published SHA5128d6d1420a93de227b20c4bd4de4987279349841e291b936cf87b40a3522bfd0ef8c1753a92bf35bfd82713a609b55682de2e450633ca964d2bc0631c3a64b01e.
The official minimal artifact has no Windows build; no unofficial package used.

The official Java compatibility table supports Java21. The existing qualified
Temurin21.0.13+9 JRE is reused through OPENSEARCH_JAVA_HOME. The bundle's
250,992,996bytes of duplicate JDK were not extracted. All other3423files total
1,126,631,239bytes; archive-derived manifest SHA256
bbc42bc9d2a1a9aa164083270dec1e921aedc9eec2033e2f0b0a31c6199764b8.
Server class major65(Java21). The unmodified official Windows launcher -V -s
returns0 and reports OpenSearch3.9.0/JVM21.0.13. This proves launcher/version
compatibility only, not server/plugin/query readiness. Runtime-version.log/result
are retained. No system/PATH/service changes or second Java installation.

The archive remains temporarily until actual native qualification establishes
which supported Java is required. Do not download another copy or extract a
second runtime by default. If native startup exposes a concrete runtime defect,
fix the configured dependency/launcher contract rather than adding retry/fallback
paths. Remove the verified archive after the chosen runtime is qualified.

## Exact continuation

- NativeCql fixture: explicit COSMIC_OPENSEARCH_CQL_SOURCE metadata contains
  Endpoint(loopback HTTP origin),Actor,LiveIds(3),DeletedId,MissingId.
  Optional COSMIC_OPENSEARCH_CQL_USERNAME/PASSWORD must be supplied together in
  the process environment. The fixture independently checks native index/ID/
  found/source, deleted/missing controls,3/2/0,paging and document hashes before/
  after. No provisioning or saves occur in this fixture.
- Configure one isolated native node/store using this package and the supported
  official launcher/JVM options. Keep all listeners loopback and record paths,
  process ownership, settings and bootstrap intent/ACKs before writes.
- Resolve the Windows owned-process shutdown invariant before service startup:
  OpenSearch's normal stop is console Ctrl+C, not a shutdown REST endpoint.
  Use an owned hidden console and verified Ctrl+C delivery to that console if
  implementing native lifecycle control. Do not invent a nonexistent REST
  shutdown, broadly kill Java, swallow stop failures or leave owned processes.
  Read the shipped launcher/options rather than guessing a parallel startup path.
- Bootstrap one explicitly owned index and4create-only canonical Holon records,
  with per-operation original intent/ACKs and a fifth absent control. No replay
  after dispatched/unknown writes. Then native/retained restart, manager/Mongo/
  HTTP and local browser gates. Publish a WEB4 card only after real acceptance.
- Hosted HTTPS/credential qualification and full storage/ORM remain open.

[Get Document API](https://docs.opensearch.org/latest/api-reference/document-apis/get-documents/),
[Windows installation](https://docs.opensearch.org/latest/install-and-configure/install-opensearch/windows/),
[Java compatibility](https://docs.opensearch.org/latest/install-and-configure/install-opensearch/),
[official artifacts](https://opensearch.org/artifacts/by-version/).

Unit TRX SHA256 7a91dac90bf1d6422aefb140086821f448e41b2d6fb26f83ebfd197d810d4d0d.

## Native acceptance — 5 November 2026

Native create-only bootstrap1/1, initial read1/1 and retained read1/1 pass, each
zero failures/skips. Bootstrap creates exactly one explicitly owned index with
one shard/zero replicas, checks index/shard acknowledgement, then creates four
full canonical camel-case Holon documents through _create. Per-document intents
flush before dispatch; native201/index/ID/result=created/version1/shard-success
ACKs are checked and retained. Three private live records and one deleted control
share the source actor; a fifth control is absent. No create/save replay occurs.
Native CQL independently proves3/2/0,paging and all four document hashes unchanged
before/after each read-only test. Same source/store/IDs reused on restart.

The source uses the full canonical Holon serializer, not a scalar-only replacement.
This still does not certify the legacy provider's unchecked CRUD acknowledgements,
conditional writes or full storage suite. The fixture uses deliberate create-only
bootstrap API semantics rather than claiming unsupported create-only provider CRUD.

## Lifecycle and one retained pre-test failure

Signal-OwnedConsoleProcess.py first detaches only its own inherited console,
attaches to the known live target, verifies that the console contains exactly
target+helper, then sends Ctrl+C. It refuses unrelated members or invalid/self
PIDs before signalling. Three controlled guard tests pass. A real hidden-console
process accepted SIGINT and exited0. The original preflight failed with WinError5
because the helper was still attached to its inherited console; that root cause
was corrected. The original controlled PID7032 then received SIGINT, and the
independent corrected preflight PID17536 exited0. Neither test started a backend.

OpenSearch uses its shipped JvmOptionsParser and the documented startup properties,
with the same Java21 runtime,128MiB initial/512MiB maximum heap and2active CPUs.
The direct JVM has one owned hidden console, with HTTP19200 and transport19300
loopback-only. The node is standalone with security disabled only for this new
owned loopback teaching configuration; hosted TLS/auth/least-privilege remains open.
All package modules/plugins are retained. Index auto-creation is off; the bootstrap
uses one explicit index creation. Source data/config/log/temp paths are in the
single existing state directory; no new environment/system/PATH/service install.

Initial and accepted restart both close through Ctrl+C: native logs explicitly
show stopping/stopped/closing/closed, exit130 (normal SIGINT), listeners closed.
The restart receipt records ctrlCSent=true,nodeClosed=true. All owned listeners
are checked by PID and loopback address before bootstrap/tests. No broad Java
kill, invented shutdown REST route or force-termination path is used.

The first retained attempt stopped BEFORE test dispatch: the1second native health
wait returned HTTP408 while recovery was still warming. The readiness decoder
incorrectly treated that protocol outcome as fatal and shut the node down.
Readiness now parses only bounded408 responses proving timed_out=true, correct
cluster and a valid health state, then continues bounded readiness polling.
Malformed/foreign/oversized408 or auth errors still fail; admission requires
yellow/green and matching native identity/listeners. Four readiness controls pass.
The original failure/log/PID and clean stop remain; the successful continuation
is a separate phase, with no bootstrap or save replay.

## Resource/capacity checkpoint

Qualified native Java21 allowed removal of the exact verified1,050,303,612byte
archive after process stop and checksum/path verification. The duplicate bundled
JDK250,992,996bytes was never extracted. Retain the1,126,631,239byte package and
shared Java while this demo is needed. The node's default disk monitor observed
>90%use of C:, high-watermark/index-create blocking. Existing-index CQL and restart
acceptance pass; new-index/full storage capacity remains a separate gate. No disk
watermark was lowered or disabled. Windows reported44,374,634,496bytes free after
archive cleanup. Data197,104bytes and temp5,921,631bytes at that checkpoint.

Automatic approval review rejected additional recursive cleanup of disposable
OpenSearch/ArcadeDB/Solr unit bin/obj and owned temp output, reporting only
blocked by policy. No cleanup intent was executed and all those paths remain
intact. Existing integration outputs/reports and all source data are retained.
This is an agent/tool cleanup issue, not a new owner/account action.

Exact native evidence:

- `bootstrap.trx`: 2884bytes, SHA256 `0bdd123c18fbbeb5907148d626c0a914b46301aa5191bb9c90dd4e7ed87a2532`.

- `initial.trx`: 2899bytes, SHA256 `0bda28dcd3e600c60a6f045402fb7f63cd4c7552301400f6538010e0f8e6c47a`.

- `retained-88338fbc93f14e088b4ebee86740305a.trx`: 2899bytes, SHA256 `d333c66d76d1e2fd20178b74c2dc3e0363dd7e174ac67b0f921912eb7e7817c6`.

Accepted retained phase: retained-88338fbc93f14e088b4ebee86740305a. Original pre-test failure: retained-db37c75d45574c29a27be84073fea063.
Next: protect original payloads against bootstrap hashes, manager/Mongo/HTTP, then
local browser and WEB4 local card. Hosted/full ORM/provider gates remain open.
