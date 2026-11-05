# OpenSearch bounded CQL scope

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
