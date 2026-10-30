# LanceDB CQL MVP — protocol audit, not verified

30October2026. Inventory stays red; no native source/account/environment created.

Existing provider explicitly targets LanceDB Cloud REST, not embedded OSS.
It sends JSON row envelopes to create/merge_insert and parses query responses as
JSON arrays. The [official remote client source](https://github.com/lancedb/lancedb/blob/4d7a64712f7fbe39bffaf110f3eb790943bd7874/rust/lancedb/src/remote/table.rs)
uses Arrow IPC upload/response contracts. Current provider wire behavior therefore
needs protocol correction and proving tests before any native claim. Do not create
a custom JSON server and label it native LanceDB, or silently switch Cloud to OSS.
The [Enterprise guide](https://docs.lancedb.com/enterprise/quickstart) distinguishes
remote service endpoints from OSS storage-path access.

Additional observed root gaps:
- EnsureTableAsync ignores the HTTP response entirely and writes a `__schema__`
  sentinel; activation can report success after a failed native request.
- Failed holon writes can mutate caller IDs; batch errors/stop semantics need review.
- Native query errors become empty lists; all-documents uses placeholder-vector
  similarity/topK1000, not a complete document scan.
- Interpolated identity/metadata predicates are unescaped; capability admission,
  response identity/Arrow schema checks and byte/time bounds are absent.
- Runtime holon serialization, acknowledged save ownership, explicit table schemas,
  history/children/concurrency/replication/migration remain unverified.

Next: choose and explicitly specify supported remote versus embedded transport;
correct the shipped Cloud wire contract with Arrow and schema/acknowledgement tests.
Native remote acceptance requires an admitted free endpoint/account. No LANCE-named
environment variable found (names only); broader configuration discovery pending.
Do not claim credentials are absent everywhere or allocate a paid service.

[Current status](CURRENT_STATUS.md) / [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
