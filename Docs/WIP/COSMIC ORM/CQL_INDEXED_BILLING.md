# Indexed physical-read accounting

27 October 2026. Prepared SDK accounting implemented; WEB4 binding/reservation
estimation and native-chain acceptance remain open. This supersedes earlier
checkpoint statements that all prepared indexed execution is unsupported.

## Contract

- The server supplies an exact schema2 Solana/SQLite source/index policy. Network,
  catalogue, coverage start, schema and index identity must all match. No client
  JSON field grants a policy or signing authority. At most eight bindings.
- All five read operations require explicit service tariffs: SQL snapshot,
  source genesis, slot, block and transaction. History/range/bootstrap operations
  are not enrolled in prepared query execution. Maximum physical reads is explicit
  (4–100,000); budget exhaustion rejects before another durable begin or dispatch.
- An inherited actor must match the admitted usage handle. Missing exact policy
  or actor mismatch rejects before calling the registered reader.
- Each actual SQL snapshot/RPC durably begins before I/O. Each completed invocation
  records one receipt with the actual execution/source provider, exact attempt ID,
  operation and a hash of the full immutable binding. Root/chunk transaction reads
  use the sole canonical transport delegate, rather than one aggregate observer fee.
- Receipts use `CQL-indexed-physical-read-service-tariff`, explicit catalogue and
  units=1. These are server service tariffs, not inferred chain gas/native invoices.
  No signing, funding, source writes or native fee quotation is introduced.
- Successful pages require complete acknowledged SQL/head/selected-source capture;
  a reader that omits instrumentation cannot publish a prepared success. Empty
  pages require one SQL plus live genesis/slot/block capture too.
- Lifetime/cancellation is checked before dispatch and again after durable begin.
  Failed begin/receipt persistence returns `CQL_INDEXED_ATTEMPT_UNCERTAIN` with
  original exception evidence. No retry, fabricated completion or partial page.
  A thrown/indeterminate transport leaves its begin unresolved for reconciliation.
- SQLite and the canonical Solana observer preserve billing/budget error codes.
  Existing native query admission and old PreparedStorageExecution constructors
  retain their signatures and behavior. Native indexed execution stays rejected.

## Server composition example

```csharp
// source and admittedHandle are resolved/authorized by the server.
// Prices below are server configuration values, not request DTO fields.
var tariffs = new[]
{
    new CosmicIndexedReadTariff(CosmicIndexedReadOperation.IndexSqlSnapshot,
        serverSqlSnapshotTariffUsd, catalogueVersion),
    new CosmicIndexedReadTariff(CosmicIndexedReadOperation.SourceGenesis,
        serverRpcTariffUsd, catalogueVersion),
    new CosmicIndexedReadTariff(CosmicIndexedReadOperation.SourceSlot,
        serverRpcTariffUsd, catalogueVersion),
    new CosmicIndexedReadTariff(CosmicIndexedReadOperation.SourceBlock,
        serverRpcTariffUsd, catalogueVersion),
    new CosmicIndexedReadTariff(CosmicIndexedReadOperation.SourceTransaction,
        serverRpcTariffUsd, catalogueVersion)
};

var indexedPolicy = new CosmicIndexedReadTariffPolicy(source, 256, tariffs);
var prepared = new PreparedStorageExecution(
    admittedHandle, correlation,
    nativeSigners: new Dictionary<ProviderType, string>(),
    permittedNonNativeProviders: Array.Empty<ProviderType>(),
    queryTariffs: null,
    storageTariffs: null,
    requireStorageTariffs: false,
    indexedReadPolicies: new[] { indexedPolicy });

var result = await prepared.RunAsync(() => query.ToCosmicIndexedPageAsync());
// Inspect OASISResult before using Result; keep its source receipts/checkpoint.
```

A hosted endpoint must also reserve/authorize a bounded estimate before creating
this handle, resolve its source/index/observer server-side and return the indexed
page contract. Those WEB4 pieces are next; this example is SDK composition.

## Evidence and remaining acceptance

14 new Core cases cover complete rows/empty receipts, actor/missing policy,
begin/receipt/transport failures, read budget, omitted instrumentation,
cancellation during begin and incomplete/duplicate/negative/unenrolled tariffs.
333 Core admission/accounting regression checks pass, including127focused checks.
31 actual SQLite tests pass. Full Solana unit suite789pass includes seven wired
actual-SQL/canonical-Solana-decoder cases over controlled RPC/outbox boundaries.
Successful one-row query has1SQL+6RPC receipts; empty has1SQL+3RPC. Source/SQL
persistence faults retain classified uncertainty and do not repeat reads. The
initial fixture query used an unsupported conditional; captured parameter form
was used without expanding the expression compiler. Both Core targets and
SQLite/Solana provider builds pass. Zero skips; no native chain submissions.

These tests exercise usage-ledger calls through controlled outbox stores; they
do not establish production Mongo outbox durability, settlement/authority/JWT
acceptance or native chain coverage. Original Solana full-range recovery remains
red. Scalar/projection/graph indexed execution and full ORM parity remain open.

- `TestResults/ProtocolCqlCurrent/index-billing-regression.trx` SHA256 `51457A980278A249033ED19D7895B2319F446E0469F53994749A80636AAAE5F8`.
- `TestResults/ProtocolCqlCurrent/index-billing-core.trx` SHA256 `2CCFAFD025EEEC7EEDD8C2BF70D945EA507A7DE42449F4181BD03941D7AA0483`.
- `TestResults/ProtocolCqlCurrent/index-billing-sqlite.trx` SHA256 `9EAF197E675FD9C5D9413A7CE204DB38F860B4F9B41BD94E0DD64A9463BF8A9F`.
- `TestResults/ProtocolCqlCurrent/index-billing-solana.trx` SHA256 `31A2D92FAD2771C392DE564151332C7C7FC831390BD904D1B799E48521A3C4CF`.
- `TestResults/ProtocolCqlCurrent/index-billing-wired.trx` SHA256 `B54C1B75B02A32BD1687203A1BCF37C4484D9D7A611368B4246FE43DE8984195`.

[WEB4 indexed preparation](CQL_INDEXED_WEB4_PREPARATION.md) now captures server alias authorization, immutable query/freshness and conservative reservation bound once. HTTP route/usage binding and acceptance remain next; this does not certify native coverage or production deployment.
