# HTTP configuration ownership — working audit

26 October 2026. Production fix and baseline execution are pending. The completed
canonical REST/native/usage milestone is recorded in
[storage accounting](CQL_STORAGE_WRITE_ACCOUNTING.md).

`OASISControllerBase.ConfigureOASISEngineInternal` changes the singleton
`ProviderManager.Instance` feature flags and provider lists even when
`SetGlobally=false`. `ResetOASISSettings` later restores its earlier values.
An await between those operations allows another request to see temporary
settings. Older request cleanup can also overwrite a later explicit global
update. The source pattern establishes a concurrency risk; it is not yet an
executed regression result.

The prepared six-case HTTP test draft covers replication, failover and load
balancing, with two deterministic completion schedules per feature. Provider
entry/release barriers hold two real MVC requests open. One schedule checks that
temporary settings never become global; the other checks that older temporary
cleanup cannot erase a subsequent `SetGlobally=true` update. Conditional storage
returns a controlled conflict, so no database or provider environment is needed.

Draft: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/CosmicDataControllerConcurrencyTests.cs.pending`.
It is excluded from compilation until source/output reservation permits a
baseline. Activation also requires making the existing test class partial.
No pass/fail result is claimed for this draft.

The intended V2 invariant is request-owned routing configuration, with explicit
global changes published by one policy owner. Temporary request cleanup must
never restore shared snapshots. A candidate design is a Core-owned immutable
runtime-policy snapshot and request registry bound to existing providers and
performance state; this needs source/ABI/admission review before implementation.
Do not activate a provider while capturing policy or bypass prepared admission.
Do not serialize all HTTP requests around a global lock to conceal shared state.

Keep legacy code unchanged; document its equivalent global mutation/restoration
gap. Remaining verification must cover ordered lists, provider selection,
invalid/default options, actual request-policy application, and simultaneous
cross-provider requests as well as the six initial flag cases.
