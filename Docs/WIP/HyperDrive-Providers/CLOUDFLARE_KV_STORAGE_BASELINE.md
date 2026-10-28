# Cloudflare Workers KV storage baseline

Updated28October2026. Scope: real REST implementation and shared-policy controlled verification. Hosted acceptance is open.

## Implementation

- Replaced duplicated plaintext avatar/detail/holon/search/export/history logic with `KeyValueStorageProviderBase` and persisted private storage. The existing accountId/namespaceId/apiToken constructor remains; `OASIS_CLOUDFLARE_STORAGE_KEY` or an explicit resolver must supply a separately persisted32-byte key.
- Native leaf implements actual KV value GET/PUT/DELETE and cursor-based key enumeration, using per-request Bearer authentication. Default owned HTTP handler disables redirects; injected clients stay caller-owned unless explicitly transferred. No service/namespace provisioning or retries.
- Validates HTTP/envelope acknowledgements, rejects contradictory or duplicate fields, preserves later-page errors, follows empty pages with continuation cursors, and rejects cursor cycles/out-of-scope keys. HTTP5xx status survives in read exceptions; uncertain mutations use the existing reconciliation-required marker.
- Read-only legacy-prefix admission precedes direct access; legacy records are refused, never rewritten. Default holon deletion stays soft. Namespace admission is not coordination with concurrent legacy writers or a snapshot.
- No conditional backend interface is advertised: KV supplies unconditional writes. No fabricated ETags, compare-and-swap, local read cache or wait/retry loop was added.

## Evidence and limits

- The SAME core-storage-v2 passes26async+26sync checks through controlled HTTP and the shipping provider; dictionaries empty afterward.25additional transport/metadata controls pass. These are not native service tests.
- Two opt-in native cases compile and skip without configuration. They use the same suite, preserve checkpoints and test actual visibility without artificial delays. No Cloudflare request, account, namespace, runtime, SDK or download was created.
- Aggregate: `TestResults/HyperDrive-Providers/CloudflareCurrent/conformance-controlled-current.json`, SHA256`9cf5b1d9580db76ea77a53a3de73e192d01a6eff974b9f1300e5dc70f2839107`. Source/PDB identities, resource measurements and receipt are alongside it. Proof uses a local working Core cohort; immutable release/full-interface/global-routing certification remains open.
- First controlled async verification stopped at checkpoint replacement after3checks. Original688B report and694B pending bytes preserved verbatim under `TestResults/ProviderConformanceCurrent/CloudflareOASIS/async/report-publication-interrupted{,.next}.json`; original exception details were not captured by quiet logging, cause unestablished.200owned publication probes passed and probe files were removed. Explicit fresh async verification passed, then final source cohort52/52 passed after transport-status classification changed. Previous complete reports retained once. No native data, automatic retry or external operation replay was involved.

## Native service gates

Cloudflare documents [eventual consistency, including negative lookups](https://developers.cloudflare.com/kv/concepts/how-kv-works/) and [one write per second to the same key](https://developers.cloudflare.com/kv/platform/limits/). Immediate read-your-writes, globally current credentials, uniqueness under concurrency, immutable version reservations and multi-key transactions cannot be inferred. Controlled instantaneous visibility does not overcome these native limits. A stronger authority/Durable Objects design would be separate work; this adapter does not silently substitute another store.

Native credentials were absent from the process and three standard Windows Wrangler profile locations inspected. Configure a dedicated owned test namespace with scoped KV read/write credentials outside chat:

- `OASIS_CLOUDFLARE_STORAGE_CONFORMANCE=1`
- `OASIS_CLOUDFLARE_CONFORMANCE_ACCOUNT`, `OASIS_CLOUDFLARE_CONFORMANCE_NAMESPACE`, `OASIS_CLOUDFLARE_CONFORMANCE_TOKEN`
- `OASIS_CLOUDFLARE_CONFORMANCE_STORAGE_KEY`: separately retained canonical32-byte Base64 key.

Run `CloudflareStorageConformanceTests` in the existing Cloudflare unit project after bounded cache-only Core/Shared/producer/consumer builds under one build lease. Verify source/PDB identities before/after. Native evidence goes to fixed `TestResults/ProviderConformanceCurrent/CloudflareOASIS-native/{async,sync}`; never remove an incomplete report or replay an uncertain write to obtain a pass. Rate-limit/visibility failures must be recorded, not hidden by weakening the suite.

Protocol sources: [REST value write](https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/update/), [REST listing and optional cursor](https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/keys/methods/list/). Workers binding `list_complete` is a different response shape; the REST adapter follows `result_info.cursor` and never terminates merely because a page is empty.

## Resources and next step

Existing fixed provider/unit bin/obj and .NET10.0.401/NuGet caches reused; exact Windows paths, measured sizes, lifecycle and cleanup/recreation steps in `CloudflareCurrent/resources.json`. No shared cache cleanup or duplicated environment. Preserve compact original failure metadata; controlled fixtures were only in-memory.

Cloudflare native/core-contract gates stay open. Continue breadth with the existing Tigris S3 adapter and shared S3 base; do not begin routing or Cloudflare Durable Objects depth here.
