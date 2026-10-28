> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

# HTTP configuration ownership — working audit

## Request protection policy preservation — 26 October 2026

The request-context factory now retains configured metadata and preparation
protection policies. Dropping them silently disabled inherited protection on
scoped writes. Two genuine regressions exposed that loss; the corrected factory
and barrier-controlled concurrent override case pass within full Core3,497/3,497,
zero skips/failures, with both targets built. Policies belong to request-owned
managers; later parent updates or another request's assignments do not replace
their captured configuration. [Contract and evidence](WRITE_PREPARATION.md#request-owned-protection-configuration).

Hosted authentication, actor-specific protection policy enrollment and recovery
endpoint admission remain separate requirements. Retaining a configured policy
does not supply one for an unconfigured host.

## Earlier routing ownership audit

26 October 2026. The request-owned implementation is active and its focused
controls pass. Final portable/full-unit/native certification is in progress. The completed
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
The existing test class is partial. Original baseline: **6 failed /0 passed
/0 skipped**, captured in completed process output and committed source at
`f8d055dfc`. External cleanup later removed the original raw TRX/log along with
other TestResults outputs. Do not describe those original files as retained.
The six schedules now pass, including response settings remaining tied to their
own request. Before the final preferred-binding correction, full Core3,365 and
normal WEB4231 also passed; the new complete-policy correction needs final gates.
The earlier verified milestone `cc67cce73` remains separately identified.

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

The draft is now implemented in Core `Configuration/ProviderRoutingPolicy.cs`,
`ProviderManager.RequestPolicy.cs` and `HolonManager-RequestContext.cs`.
The shipping single/batch write paths resolve/capture routing without temporary
global mutation, then execute with a request-owned manager. Existing constructor
signatures remain available; the new configured-affinity path does not add
permanent provider event subscriptions. Response metadata binds the same snapshot.

Review exposed a second genuine regression: converting the policy to overrides
dropped its preferred provider, allowing a later global default to replace the
captured write authority. The original failure and compressed TRX are retained
under `../Evidence/Verification/preferred-binding-red.*`. The correction preserves captured
preferred identity, including an explicitly missing default, while an explicit
operation or original manager affinity still wins. Twenty-six focused Core
controls pass, including actual scoped reads and no-provider-execution checks.

The portable build also caught Core's existing compatibility `Enum` shadowing
`System.Enum`; qualification is fixed at the new policy owner, without changing
the legacy shim. Final portable/full-unit/native gates are still required before
merging this follow-up into Development. A changed test expectation or ignored
routing option is not a fix.

Keep legacy code unchanged; document its equivalent global mutation/restoration
gap. Remaining verification must cover ordered lists, provider selection,
invalid/default options, actual request-policy application, and simultaneous
cross-provider requests as well as the six initial flag cases.

This migration covers the canonical V2 graph-save actions and their delegating
aliases. Non-migrated V2 controllers still using temporary singleton configuration
can affect global defaults; their migration and cross-route tests remain open.
The snapshot covers the three feature flags, ordered provider lists and preferred
provider, not a deep immutable copy of every DNA/strategy/security setting.
Pinned conditional writes never gain permission to fail over to another authority.

### Latest verification (2026-10-26)

Final Core netstandard2.1/net10.0 builds pass. Full Core suite: 3,368 passed, zero failures/skips. Shipping WEB4 build passes; normal WEB4 suite: 231 passed, zero failures/skips (external Stripe sandbox/operator Mongo soak excluded). Compressed original reports are retained in ../Evidence/Verification/configuration-final-core.trx.gz and configuration-final-web4.trx.gz. The isolated HTTP fixture now explicitly declares its owned SQLite provider affinity; a missing captured global default must not implicitly provide authority. Native three-provider verification remains pending; this is not full roadmap completion.

## Verified request-routing checkpoint — 26 October 2026

Canonical V2 WEB4 single/batch graph writes now use immutable request-owned
routing contexts. Global publication is atomic; request completion cannot restore
an older global policy. Captured preferred identity includes a missing default.
Performance and load-balancer history remain shared; construction performs no
provider activation or extra persistent event subscriptions.

Core netstandard2.1/net10.0 and shipping WEB4 builds pass. Full suites: 3,368 Core
and 231 WEB4 tests, no failures/skips. Fresh three-provider native acceptance:
138 SDK /138 HTTP walkthrough queries,36 shipping REST requests,378 settled
operations/403 attempts. Source freeze held and all seven binary pairs matched.
Required compressed reports and safe source/binary summaries are retained in
`../Evidence/Verification/configuration-*`. Core implementation is published as `2cb7f88`.

This closes this routing checkpoint, not the roadmap. Non-migrated routes,
cross-route concurrency, deep DNA/security/strategy ownership, gRPC/GraphQL
identity, recipient/tenant queries, federation and remaining ORM work stay open.
See [request policy ownership](CQL_HTTP_CONFIGURATION_OWNERSHIP.md).
