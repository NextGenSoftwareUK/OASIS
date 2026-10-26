# HTTP configuration ownership — working audit

26 October 2026. Six genuine HTTP regressions reproduce the issue; the production
fix remains pending. The completed
canonical REST/native/usage milestone is recorded in
[storage accounting](CQL_STORAGE_WRITE_ACCOUNTING.md).

`OASISControllerBase.ConfigureOASISEngineInternal` changes the singleton
`ProviderManager.Instance` feature flags and provider lists even when
`SetGlobally=false`. `ResetOASISSettings` later restores its earlier values.
An await between those operations allows another request to see temporary
settings. Older request cleanup can also overwrite a later explicit global
update. All six deterministic schedules now fail against unchanged production
code: temporary scopes leak true flags, and older cleanup restores false over
an explicit global update. Both HTTP requests reach their controlled conditional
conflict, so these are configuration failures, not provider/host startup failures.

The six-case HTTP test covers replication, failover and load
balancing, with two deterministic completion schedules per feature. Provider
entry/release barriers hold two real MVC requests open. One schedule checks that
temporary settings never become global; the other checks that older temporary
cleanup cannot erase a subsequent `SetGlobally=true` update. Conditional storage
returns a controlled conflict, so no database or provider environment is needed.

Source: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/CosmicDataControllerConcurrencyTests.cs`.
The existing test class is partial. Evidence:
`TestResults/CoreCurrent/configuration-concurrency-red.trx` and
`TestResults/CosmicCqlDemo/configuration-concurrency-red.log`: **6 failed /0 passed
/0 skipped**. These active regressions intentionally remain red on the task
branch until the implementation is fixed; do not merge this unfinished follow-up
into Development. The earlier verified milestone `cc67cce73` remains separately
identified.

Cleanup had removed transitive provider outputs. The initial CS0006/NETSDK1005
build failures and cached-only restore failures were prerequisites, not functional
baseline evidence. The dependency graph now builds using original selected
frameworks, with only exact8.0.31 reference packages restored for ONET's net8.0
target. No SDK/runtime was installed. See the shared task resource ledger and
`TestResults/CosmicCqlDemo/configuration-restored-project-resources.json`.

The intended V2 invariant is request-owned routing configuration, with explicit
global changes published by one policy owner. Temporary request cleanup must
never restore shared snapshots. A candidate design is a Core-owned immutable
runtime-policy snapshot and request registry bound to existing providers and
performance state; this needs source/ABI/admission review before implementation.
Do not activate a provider while capturing policy or bypass prepared admission.
Do not serialize all HTTP requests around a global lock to conceal shared state.

Implementation review must keep the actual settings effective, rather than only
making the six global-state assertions pass. Bind the selected policy to the
manager's real registry and nested ownership/write operations, preserve pinned
conditional authority, share performance/selection state where required, and
report the request's effective detailed settings. Global publication must be
atomic and independent of older request cleanup. Inheritance/default behavior,
immutable list capture and provider activation ordering need dedicated controls.

### Selected implementation direction

Use an explicit request manager/registry, not an ambient global replacement or
temporary mutation of singleton properties. Core captures the effective policy
once, applies immutable overrides and creates a registry pointing at already
registered provider instances. Provider capture must not activate/deactivate,
register replacements or allocate another performance monitor. Share the original
load-balancer selection state so request isolation cannot reset round-robin or
weighted fairness on every request.

The request manager must preserve the original manager's configured provider
unless an explicit request provider wins. Construct the scoped manager and search
manager without adding persistent event handlers to shared providers: the existing
OASISManager constructor subscribes when a provider argument is supplied and has
no teardown. A new per-request factory must not multiply that existing leak.

Publish `SetGlobally=true` changes atomically through the runtime-policy owner.
Default/list inheritance in V2 comes from that effective runtime policy, rather
than the legacy StorageProviders section. The V2 write configuration path should
validate/describe intent without temporary singleton updates; its cleanup must
have no global snapshot to restore. Keep legacy behavior as documented only.

Detailed response settings also need a captured policy. Their existing getters
read the singleton during JSON serialization, after controller cleanup, so simply
changing execution would still report another request's settings. Preserve the
existing response surface while binding V2 responses to their immutable policy.

An excluded draft, `Drafts/ProviderRoutingPolicy.cs.pending`, defines
bounded immutable override/snapshot data only. It is not active or certified.
Next: implement capture/global publication and scoped registry/manager creation,
add Core isolation/default/list/fairness/no-activation controls, bind the shipping
single/batch routes and their responses, then run the six HTTP regressions plus
full unit/native gates. A changed test expectation or ignored routing option is
not a fix.

Keep legacy code unchanged; document its equivalent global mutation/restoration
gap. Remaining verification must cover ordered lists, provider selection,
invalid/default options, actual request-policy application, and simultaneous
cross-provider requests as well as the six initial flag cases.
