# CQL v1 milestone 1 — provider execution admission

Completed 22 October 2026. Scope: shared hosted (.NET 10) CQL execution admission for the existing MongoDB, SQLite and Neo4j query adapters; no provider-specific query implementation changed.

## Root cause and repair

CosmicQueryExecutor previously called ExecuteCosmicQueryAsync directly. Even inside a server-created PreparedStorageExecution scope, a caller could query an unpermitted provider without entering durable physical-attempt accounting. Capability admission alone is not execution permission.

The executor now observes the inherited policy, preflights actual IOASISStorageProvider/query identity agreement for every selected target, and invokes native reads through the existing PreparedStorageExecution.ExecuteAsync owner. That owner enforces its provider allowlist, signing requirements and durable attempt recording before I/O. The query does not create admission, select new providers, retry, fail over or settle usage itself.

The scope and identities are rechecked immediately before native execution, including after asynchronous attempt persistence. Expired/detached scopes fail closed; cancellation while accounting waits prevents the read. Rejected attempts retain their original exception and return no result. Native error codes/causes and existing result-budget/provenance checks survive.

Trusted local calls without an inherited policy retain their existing behavior. The netstandard2.1 portable build has no hosted subscription subsystem, as defined by the existing project profile; this milestone does not add a portable-host authorization system. Production endpoints must require their server-owned admission and visibility policy rather than treating absent admission as authorization.

## Verification

- Original executor: 11 new regression cases fail (retained admission-red.trx).
- Final source: full Core suite **1,651 passed, 0 failed, 0 skipped**, including **16 new admission cases**. Both net10.0 and netstandard2.1 builds pass; existing warnings remain.
- New cases cover awaited accounting before reads for Mongo/SQLite/Neo identities, denied providers for each, exhausted attempts with exact cause preservation, mismatched identities, all-target identity preflight, expired scopes before and during persistence, pure-interface bypass rejection, cancellation during persistence, native-preparation downgrade rejection, native error preservation and identity changes during persistence.
- Core producer and test consumer SHA256 match: D794AEC6844028F5E62BB4BD373424A52C61F18401D1011A8AFE4CBB7BA58DE7.
- Fixed evidence: TestResults/CosmicCqlV1Admission/admission-core-full.trx, admission-red.trx, milestone-evidence.json and compact logs. No native database/service or test dataset was created.

These are controlled tests of the common production call boundary, not real MongoDB/SQLite/Neo4j or authenticated HTTP acceptance. Existing provider native reports are separate historical evidence.

## Resume point

Stop after this milestone at the user's request to conserve tokens. Next is milestone 2 in [CQL_V1_PRODUCTION_PLAN.md](CQL_V1_PRODUCTION_PLAN.md): authenticated bounded WEB4 requests, server-owned eligible providers and actor/visibility restrictions before native reads, plus measurement/accounting and negative authorization tests. Milestone 3 certifies the complete v1 subset against the three actual providers and HTTP boundary. CQL v1 is not production-certified yet.

The initial test command returned zero without executing tests because test packages had been cleaned externally; that result was rejected. Pinned Core/test dependencies were restored into the same shared cache, with no new environment/SDK. See [dependency inventory](CQL_V1_ADMISSION_DEPENDENCIES.md) and [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
