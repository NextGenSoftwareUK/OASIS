# Chroma CQL MVP — in progress

30 October 2026. Inventory remains red; native/CQL/paired acceptance is pending.

## Verified save ownership

- Holon IDs/provider keys are staged in a runtime-typed JSON document, then
  published to the caller only after the native HTTP write succeeds.
- Failed/unknown writes preserve caller identity and existing provider keys;
  no replay occurs. Derived fields and exact decimal JSON survive serialization.
- HolonManager retains change-tracking acceptance ownership.
- Batch saves expose errors, count acknowledged writes, and honour stop/continue.
- Unsupported history rejects before identity allocation or network I/O.
- Dedicated injected HttpClient remains caller-owned; the original constructor stays.

Bounded leased Windows .NET10 build + tests: **12 passed,0 failed,0 skipped**.
[Tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests/ChromaSaveOwnershipTests.cs).
Evidence: `C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent/save-controls.trx`.
SHA256 `CB9D69E2418F8C43BC6D76A90A92BAFE50D4BD55A6E9EA4F0FA3C25E9E0105AA`.
Provider and test cache-only restores succeeded, no download. Initial no-restore
provider build correctly rejected missing assets; explicit cache restore fixed it.
These are injected-HTTP controls, not native database certification.

## Next acceptance

1. Audit pinned native Chroma collection/record schema and upsert HTTP contract;
   activation must verify the selected collection/schema and clear stale state.
2. Provision one free retained native source (no existing Chroma image/source found);
   verify schema, multiple original records, filtered query and restart without reseeding.
3. Add Mongo-paired C#/HTTP/browser profile, clear comments and teaching examples.

## Remaining gaps

Collection resolution currently creates/caches collections with minimal schema
validation; activation failure may retain stale activation. Generic read helpers
can hide HTTP errors as missing data; list bounds/response-ID ownership and
malformed JSON need admission checks. Shared upserts lack request/response bounds
and runtime serialization on other write paths. Avatar saves still need caller
ownership review. Children, history, conditional writes, replication, migrations,
full EF parity, cloud/auth and vector similarity remain unverified. No native
receipt/CAS guarantee is inferred from an HTTP success.

[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Bounded CQL controls — 30 October 2026

Shared bounded-identity compiler bound specifically to Chroma: trusted complete
catalogue of at most100 GUIDs, captured identity membership/Boolean expressions,
and stable admitted-key pagination. Broader predicates/order/graphs reject before
native execution. No vector-similarity or unbounded scan capability is advertised.
One tenant/database/activated collection UUID `/get` request, explicit ids,
include documents, limit selected+1 and offset0. Querying never creates collections.
Native parallel IDs/documents require equal lengths, bounded counts, canonical
unique selected IDs and matching payload IDs. Missing/deleted rows are omitted;
malformed rows fail the whole page. Existing trusted catalogue grants remain the
read authority (creator mismatch alone does not revoke an explicit catalogue grant).
Linked15second deadline,4MiB response/1MiB document bounds, cancellation and final
page budget are checked before publishing. These are adapter budgets, not native
server limits or guarantees of storage transactionality.

**38 passed,0 failed,0 skipped**, including prior12 save controls,9 compiler/adapter
cases and17 adversarial native-response cases. Initial test assumed catalogue
access meant creator-only; corrected that test against the existing explicit
`AuthoritativeRecords` contract, with no Core/permission behavior change.
Evidence: `C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent/cql-controls.trx`.
SHA256 `91191B86039335EED8696F846FA6EABBA7A23CB05D2F19B266CFC94BAF192567`.
[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests/ChromaCqlTests.cs)
and [response tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests/ChromaNativeQueryTests.cs).
Cache-only restores/bounded provider build pass. Real native/paired acceptance
remains red; no hub or inventory green change.
