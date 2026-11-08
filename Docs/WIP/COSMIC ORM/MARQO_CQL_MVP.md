# Marqo bounded CQL MVP

## Current scope — legacy unstructured API only

8 November2026. Source38f811b4aa36434439ac653615fc6045b49c8c81 passed all26
controlled cases, zero skipped, in
[run42500640392](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42500640392),
job128792408892. This comprises21CQL/wire cases and5metadata/interface cases.
Artifact13624624962/4530bytes SHA256
`9706a1f958549b61bdaff0c10a50d4e5f7567042205841935c173b28303ea15a`;
TRX SHA256`68502302edac260ad8954b2d7b0c0412f3601d2c8ad28c428e6ebf5816a00417`.
Artifact counts/classes and all outcomes inspected in memory; compact receipt
LocalState/COSMIC/RecoveryLaptopResume20261107/marqo-query-qualified.json.
No native, current-commerce, manager/HTTP, hosted/browser or HyperDrive pass.

The [pinned upstream README](https://github.com/marqo-ai/marqo/blob/37a728385a25c4572a8f47b5327e6a7c946d94a9/README.md)
explicitly declares the open-source project deprecated and directs users to the
current commerce product. The API routes reviewed below belong to that old
unstructured document contract, which the existing provider already uses. They
do not establish compatibility with the new commerce index/document service.
README1427bytes SHA256778d5390fcff79090ef1d0d83a9dd9fa5f02c8e2ba142cd4724fce1757d4fd54;
compact lifecycle receipt marqo-lifecycle-primary.json retained. No historical
runtime/model/image was downloaded, no current service/key requested, and no
live admission or profile-count increase. Current-service contract/source
qualification takes precedence over an optional historical-runtime demo.


Initial implementation sourcea7a01b905 and first qualification42500341110
are historical; the verified result and lifecycle boundary above supersede them. No native Marqo, manager,
HTTP/Mongo, live WEB4 or browser proof yet; no coverage-count increment.
Priority-provider live gates and existing owner deferrals retain precedence.

## Source and contract

The existing Marqo provider stores canonical default Pascal-case Holon JSON
inside a string `doc` field, with `_id` as the native canonical GUID. Preserve
that format. Query code never calls the legacy best-effort GetDoc helper,
which currently turns all failed HTTP reads into null. There is one explicit
strict CQL path; no legacy retry or parser guessing.

The actual [document GET contract](https://marqo.pages.dev/latest/reference/api/documents/get-one-document/)
is `/indexes/{index_name}/documents/{document_id}`. Reviewed upstream source
is pinned to `37a728385a25c4572a8f47b5327e6a7c946d94a9`:
[API routes](https://github.com/marqo-ai/marqo/blob/37a728385a25c4572a8f47b5327e6a7c946d94a9/components/marqo/src/marqo/tensor_search/api.py),
[native retrieval](https://github.com/marqo-ai/marqo/blob/37a728385a25c4572a8f47b5327e6a7c946d94a9/components/marqo/src/marqo/tensor_search/tensor_search.py),
[typed errors](https://github.com/marqo-ai/marqo/blob/37a728385a25c4572a8f47b5327e6a7c946d94a9/components/marqo/src/marqo/api/exceptions.py),
[index settings](https://github.com/marqo-ai/marqo/blob/37a728385a25c4572a8f47b5327e6a7c946d94a9/components/marqo/src/marqo/tensor_search/models/index_settings.py).
Read-only activation checks the existing `oasis-holons` settings endpoint,
requiring unstructured layout and a nonempty model. It never creates an index.
Only HTTP404 with the native document_not_found code is absence; index_not_found,
authentication, throttling and native errors reject the query without partial rows.

## Implemented bounded scope

- Shared bounded identity compiler, trusted catalogue intersection before GETs,
  exact native and canonical IDs, duplicate root-property rejection, deletion
  exclusion, read authority and paging after exclusions.
- Root materialization removes Pascal-case Children before deserialization;
  stored embedded graphs remain intact. Wider graph/content/history reject.
- 100-key shared compiler bound,15-second aggregate deadline,1MiB per native
  response,4MiB aggregate wire budget and bounded complete result pages.
- Default HTTP client disables redirects; explicitly injected clients remain
  caller-owned and must establish their own transport policy and credentials.
- Query activation failures clear readiness and return typed OASISResult errors.
  HTTP failure status is preserved in the exception for operator diagnostics.

26 controls passed:21 new CQL/wire cases and5 provider metadata/interface
checks. They cover3/2/0/paging, child projection, malformed/foreign/
duplicate envelopes, index-vs-document absence, HTTP401/403/429/500, per-record
and aggregate budgets, failed readiness, no-read cancellation/inactivity,
unsupported shapes/history and frozen selections intersected with read grants.
A controlled HttpMessageHandler is not a native Marqo service.

## Queue and remaining gates

An initial review of four self-hosted implementation candidates observed
primary GitHub repository stars were Marqo5024,Urbit3629,Scuttlebutt1708 and
Holochain1398. This limited interest proxy is not a universal provider adoption ranking; the
later primary lifecycle finding overrides eligibility based on stars alone. Receipt953bytes SHA256
0a61548810e698372a388fa4fcf4f66790422f9738341634a6088ffb4fca2287 at
LocalState/COSMIC/RecoveryLaptopResume20261107/eligible-native-provider-interest-20261108.json.
More popular already-accepted or access-deferred scopes are not replayed/bypassed.

Next qualify the current commerce service's actual read contract and an approved
teaching index/reader before implementing its distinct native-record mapping or
claiming current-provider coverage. Do not silently relabel old unstructured
Holon envelopes as commerce records. Historical native acceptance would require
an explicitly labelled version/source and verified official runtime, never a
custom JSON server, another search database or tensor-search quality claims
from ID reads. No native runtime has been qualified or started in this scope.
Preserve priority queues and owner deferrals; no paid setup is assumed.
Legacy generic writes, empty-list error handling and full provider interfaces
remain unverified; this implementation does not certify them or HyperDrive/EF parity.

First controlled run42500341110 failed before executing tests: the production
provider compiled, but the existing generated metadata test called nonexistent
GetProviderVersion (CS1061,MarqoOASISProviderTests.cs:45). Preserve the failed run
and compact receipt marqo-first-controlled-build-failure.json. Replace that stale
assertion with the actual read-only query interface/authority contract; do not
add a fake version method or compatibility shim. Nullable exception assertions
also now establish the expected type before accessing status. No native call.

Research cleanup complete: the five exact hash-verified upstream cache files
(128523bytes) were removed after contract/lifecycle extraction. Original source
commit/URLs/hashes and compact marqo-primary-source-cleanup.json remain retained.
No native evidence, credentials, original cohort or unrelated file was removed.
