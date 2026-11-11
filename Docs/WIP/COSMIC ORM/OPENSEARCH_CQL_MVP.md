# OpenSearch bounded CQL scope

Current local UI address (5 November correction): `http://127.0.0.1:5138/`.
Original cohort retained; new-address browser/provenance and consumer1/1 pass.
[Address correction and exact evidence](LOCAL_DEMO_ADDRESS_CORRECTION.md) owns
publication status; earlier UI addresses below are historical evidence.

Current checkpoint: create-only native bootstrap, CQL3/2/0/paging/source
preservation and retained restart pass. Owned hidden-console Ctrl+C and native
node shutdown are verified. Manager/Mongo/HTTP and local browser acceptance
also pass. Local profile100 and WEB4 local card5073f06 are published and
browser-verified. Hosted and
full-provider/storage/ORM gates remain open.
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

## Integrated local acceptance — 5 November2026

Actual manager/LINQ and Kestrel HTTP paired1/1 and interactive1/1 pass, zero skips.
Full5physical rows(OpenSearch3/Mongo2), subset4(2each), provider provenance and
exclusion of the third unrelated Mongo row pass. Forged actor/unknown preset
return400; inactive source returns503 without partial rows. The page accurately
labels native OpenSearch without claiming account authentication.

Browser tab9 independently displayed full5/subset4 actual JSON and
MovingProviderReads, then used Stop demo. Interactive runf12e841b51434eef876bd81f4a4ba5fe
passes with outer wrapper exit0. Native _source payload bytes exactly match all
four original bootstrap SHA256s, each document remains version1, and the missing
control stays absent. Before/after hashes match; no seed/native save replay.
The exact3record temporary Mongo database was verified/dropped and confirmed
absent. Pending journal and ready marker removed; all19200/19300/27029/5119closed.

The first paired QUERY test passed, but the wrapper initially returned1 because
its new graceful Mongo stop incorrectly expected exit0. Native Mongo logs prove
WiredTiger closed, mongod shutdown complete and exit12; the helper proves Ctrl+C
was sent to its exact owned console. MongoDB8.2's Windows CtrlHandler calls
exitCleanly(ExitCode::kill), whose value is12. The wrapper now requires that
contract plus the native storage/shutdown log events, with no force kill. Original
wrapper failure/logs remain; a separate paired-mongo-stop.reconciled.json records
the proven terminal state without replaying tests/saves. The later interactive
run validates the corrected complete path, including exit12 and receipts.
[Windows signal implementation](https://raw.githubusercontent.com/mongodb/mongo/v8.2/src/mongo/util/signal_handlers.cpp),
[exit code definition](https://raw.githubusercontent.com/mongodb/mongo/v8.2/src/mongo/util/exit_code.h).

Five readiness/source controls plus3console guard controls pass. The original
canonical source version/hash guard is new and verified against the real API.
No extra download/environment or screenshot files. Local100=3canonical+57public/
source+40otherlocal, including31historical recoveries. Hosted60HTTP/28browser/open32
stay unchanged. [Local guide](../../Devs/OPENSEARCH_CQL_LOCAL_DEMO.md). WEB4 card next.

- `paired.trx`: 8176bytes, SHA256 `f1eca2a7a3b0f7516b0a964b19effe8dc3d0ca2b2e322593aa68c1464d4c2176`.

- `interactive-f12e841b51434eef876bd81f4a4ba5fe.trx`: 12352bytes, SHA256 `f31b5eb02c8cc7c41254a23a965eae8363d19e9cdfa1953129aa248af927ebe8`.

## Published WEB4 local card

Web4Site5073f060ccdb99b0fd7e84a5bd0edb5205eaa5d4 publishes card43,
OpenSearch + MongoDB / Windows local. It clearly distinguishes the retained
loopback teaching source from hosted WEB4 enrollment and links the actual
launcher Run-CosmicOpenSearchMongoDemo.ps1 -Serve at localhost5119. Run one
native demo at a time; the Mongo environment/port are shared with other demos.
New launcher audit1/1present; old accepted launcher environments not rerun.

All112 client tests pass, zero skips. GitHub Vercel status success. Public sandbox
HTML returns200 and matches the exact committed Git blob:213,331bytes/SHA256
1ea977334a0b7824a839fcbf938e3d00a5adb6b97c751fd2d689f37c53e91dee.
Guide and C#links pin parentcb3c147052ea6fc7a1c24c33d7f19e2a60224278; both raw
sources return200 and match Git bytes. Guide SHA256
beaaf411436047afb1201559d5e3ca64558855f82132bd5786e5f74734c9695c;
walkthrough SHA25611a55d640cd5b6e02e00c60e550d600d1c823cc5c97a308cb8922f9be7916867.
Signed-out live browser tab10 verified the local card and expanded exact command.
No hosted avatar/session, API/settings/source cohort or credentials were changed
by publication. Counts100local/60hostedHTTP/28hostedbrowser(open32), not100fully
implemented providers. Full ORM/HyperDrive/provider and hosted gates stay open.

## WEB4 hosted enrollment preparation

The opt-in read profile requires CQL_OPENSEARCH_QUERY_ORIGIN,
CQL_OPENSEARCH_QUERY_PROBE_ID, CQL_OPENSEARCH_QUERY_USERNAME and
CQL_OPENSEARCH_QUERY_PASSWORD. Use an existing owned HTTPS origin, a live
teaching probe and a reader restricted to oasis_holons. The shared
transport admits only an exact canonical GUID document GET at that origin.

OpenSearchHostedCqlNative accepts COSMIC_OPENSEARCH_HOSTED_CQL_SOURCE with
Endpoint, Actor, three LiveIds, one DeletedId and one MissingId. It uses
the same guarded HTTP transport as WEB4 and checks public root ownership,
native absence, full and subset reads, paging and unchanged document hashes.
Keep credentials in the process environment. The hosted tests and deployment
remain pending. The retained Windows proof is preserved.
The shared WEB4 admission run passed ten focused checks with zero skips at
Core `0205f68`, covering both providers and all native and Mongo presets.
TRX SHA-256 `e61277ee1449a4cb2156be6a9954608f2f7543be7d697b0235315e12b0cd9edb`.
The native HTTPS fixture compiled with 11 warnings and zero errors.
Execution needs an owned source and restricted read credentials.

11 November UTC: Development OpenSearch CI passed all four generic storage cases. The remaining three tests stopped at missing fixture prerequisites: owned Mongo replica admission, persistent OpenSearch restart state, and the Mongo federation connection. The next CI change should reuse the existing restart and federation tests on isolated state, preserving the generic storage coverage. Hosted CQL remains open. [OpenSearch job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43028699870/job/130549848053)

11 November UTC: published OpenSearch diagnostic `77285ecd91` against Development `73e0d4bb08`. CI reuses the existing secured OpenSearch 3.9.0 provisioner with the runner account as the receipt owner, then runs the existing native test before and after restarting the same container. The diagnostic uses an isolated runner and a private CA. Federation and hosted CQL remain open.

11 November UTC: secured OpenSearch 3.9.0 CI passed one native CQL test before restart and one after restart, zero skips. The tests verified the same cluster identity and original document snapshot over authenticated HTTPS with the fixture CA. CI removed its container. Mongo federation and hosted WEB4 remain open. [OpenSearch restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43035774220)

11 November UTC: OpenSearch federation diagnostic `b4ff3d1e2c` extends the passing secured restart workflow with a disposable Mongo replica set. It runs existing replica admission, SDK/HTTP federation, and the post-federation native source recheck. Hosted HTTPS teaching-source acceptance remains open.

11 November UTC: OpenSearch passed all five isolated CI phases with zero skips, including Mongo replica admission, native SDK/HTTP federation, and a final source recheck. Hosted HTTPS CQL remains open. [OpenSearch federation CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037028447)

11 November UTC: Development `2df4de082f` contains the verified OpenSearch restart/federation and SurrealDB restart workflows and fixture scripts. Both are manually dispatched. SurrealDB generic storage and federation remain open, as does hosted HTTPS CQL for both providers. [OpenSearch CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037028447) · [SurrealDB restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43038591878)
