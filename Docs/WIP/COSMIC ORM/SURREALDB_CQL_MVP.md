# SurrealDB CQL MVP — controls only

Updated 30 October 2026. Native and paired demo acceptance remain red.

## Implemented scope

The existing shipping provider builds against pinned SurrealDb.Net1.0.0.
A provider-specific CQL adapter now binds the shared bounded identity compiler to
SurrealDB authority. It admits GUID equality/membership, Boolean combinations and
paging over a complete server-authorized catalogue of at most100 identities.
Each selected key uses native SDK Select with typed oasis_holons RecordId; no
full-table managed filter or query-string interpolation. Arbitrary content,
sorting, graphs and broader LINQ semantics are rejected before network access.

Read admission validates decoded ID/read authority, skips deleted records,
excludes children, and applies paging after selected reads. Complete decoded
JSON budget4MiB and result limits fail without partial rows. SDK buffering occurs
before decoded-byte accounting; this is not a wire-allocation bound. Cancellation
flows through authentication, namespace selection and record reads. Independent
reads have no distributed/database snapshot guarantee. Plaintext envelope scope.

Activation now marks the provider active only after configured sign-in/use;
failed reactivation clears stale state, deactivation clears it. A failed client
setup disposes its owned SDK client and propagates the actual error.

## Verified controls

15 unit tests pass (5existing provider controls +10CQL/lifecycle tests):
`Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.UnitTests/SurrealDbCqlTests.cs`.
They cover exact authority, immutable captured catalogue, unsupported shapes,
pre-cancellation, inactive state, invalid budgets, missing plan and lifecycle
state errors. They do not prove native round trips or successful authentication.

Evidence `TestResults/SurrealDbCqlCurrent/controls.trx`,15passed/0failed/0skipped,
SHA256 `be5eb0c7513924f6d42a61d69dfaf9733b8e1bba90a2b21b7c4fcc77be92e49c`.
Provider/test builds pass. Existing package-pruning warnings remain unrelated.
An initial build failed because the nonrecursive test restore did not create the
provider assets; explicitly restoring that provider resolved it, no bypass.

## Exact next step / remaining gaps

Inspect WSL/container caches for a reusable native SurrealDB runtime, then pin an
official compatible server if absent. Record exact identities/resources before
setup. Verify original several-record writes, native3/filter2/page1, preserved
payload hashes and actual process restart before Mongo-paired C#/HTTP/browser5/4
and hub enrollment. Do not mark the provider green from controls alone.

SaveHolon currently mutates caller IDs/keys before acknowledgement, ignores returned
upsert acknowledgement and lacks saved-count flags; batch continuation, graph,
history, metadata querying and cross-client CAS are not certified. Fix the save
ownership invariant before using it to seed an accepted native demo. Avatar writes
have similar unverified ownership. No HyperDrive replication proof from this work.

SDK reference: [official .NET repository](https://github.com/surrealdb/surrealdb.net)
and [pinned package](https://www.nuget.org/packages/SurrealDb.Net/1.0.0).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
