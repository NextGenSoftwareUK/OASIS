# Chain query admission contract

Status: implementation design; not an enrolled capability.
27 October 2026. Solana first; IPFS/protocol adapters may reuse the source model.

## Invariant

A query result identifies both the authoritative source and the system that
executed the query. Selecting Solana never authorizes an undisclosed database
copy, a full-history download followed by managed filtering, or partial results.

Existing shared contracts select `ProviderType` and predicate capabilities.
They do not yet represent an index identity, a source checkpoint or an explicit
freshness requirement. `CosmicQueryProviderRows` reports provider and rows only.
Those contracts therefore cannot honestly admit indexed chain queries today.

## Two explicit modes

| Mode | Execution | Admission |
|---|---|---|
| Native | Source RPC/program executes the admitted query | Exact supported operators; no hidden scan/filter substitution |
| Indexed | Named persistent index executes against an authenticated source projection | Caller explicitly selects the authorized index and freshness requirement before planning |

An indexed plan carries logical source provider, source network/genesis,
execution provider, immutable index identity and schema version. The server
resolves credentials/endpoints and grants access; caller strings grant nothing.
No automatic switch between modes, networks, indexes or source replicas.

For current Solana encrypted memo storage, `getProgramAccounts` does not query
the memo records. GUID lookup currently scans writer history. Neither is a native
metadata/range query implementation. A new chain program/storage migration is a
separate explicit decision; this demo must not silently introduce one.

## Indexed read ownership

1. Server captures one authorized immutable source/index binding and read policy.
2. Planner admits the complete query against that binding before storage I/O.
3. Index read uses one stable checkpoint/snapshot for rows, ordering, paging,
   scalar/projection results and their source receipts. Unsupported graph/stage
   operations reject before execution. Empty results also require a checkpoint.
4. Shared execution validates the returned binding, checkpoint, freshness and
   complete page before hydration/tracking/publication. Index errors or failed
   source validation return real errors, never a native fallback or stale success.

Each result publishes source network, writer/catalogue scope, finalized source
checkpoint (slot plus independently bound block identity), index checkpoint and
observation time. Each selected record binds logical ID/version, source commit
signature, canonical hash and deletion state. A block/transaction reference is
an observation, not a cryptographic inclusion proof; label the actual assurance.

Freshness is explicit: a minimum required finalized source checkpoint and an
optional maximum lag against an independently observed finalized source head.
An old timestamp alone cannot establish completeness. Missing head observation,
unknown watermark, checkpoint regression or wrong network rejects a freshness
requirement. No cross-provider snapshot is implied.

## Index ingestion and security

- Initial bootstrap must prove the configured source range/catalogue is complete.
  A successful empty RPC page with unavailable older history is insufficient.
- Decode through the canonical source owner: validate writer, signatures, chunk
  chain, ciphertext authentication and logical identity/version before indexing.
- Advance the durable checkpoint atomically with all effects of a source batch;
  replay is idempotent by original source identity, never resubmits chain writes.
- Preserve tombstones and version history; conflicting revisions, missing chunks,
  finality regression and unavailable history stop progress explicitly.
- Source authorization and mandatory actor visibility apply inside the index
  query. A queryable plaintext index is a separate protected data store; reserved
  metadata policy does not authorize arbitrary decrypted-field indexing.
- Charge/report actual physical source/index attempts; never label an index host
  as a native Solana RPC query. Keep signing authority out of query execution.

## Implementation order and proving gates

1. Add immutable source selection/checkpoint/receipt contracts with one shared
   validation owner; preserve them through C# composition and WEB4 binding.
2. Extend row/scalar/projection planning and execution together. Native existing
   profiles keep their current semantics; indexed requests cannot enter them.
3. Implement an explicitly configured durable index with transactional checkpoint
   advancement and canonical Solana ingestion. Reuse a suitable retained runtime;
   do not create a second environment or reset the failed source fixture.
4. Test wrong network/index/actor, stale/regressing/incomplete checkpoints, empty
   pages, deletion, source corruption, unsupported operators and cancellation.
   Assert zero submissions and no DTO construction/publication after failure.
5. Native acceptance must independently compare source and index receipts, prove
   a restart preserves checkpoint/data, and run the same readable C#/HTTP shapes.
   Retained-record recovery stays a separate red gate until original records load.

Large ORM parity remains deferred. This contract does not turn any demo or parity
cell green. [Source failure evidence](CQL_SOLANA_DEMO_ADMISSION.md) and
[demo acceptance](CQL_DEMO_EXPANSION_ROADMAP.md) retain the remaining gates.

## Checkpoint foundation implemented (27 October 2026)

Core `d3350c1` implements immutable `CosmicChainQuerySource`, position,
network-bound finalized head, durable coverage checkpoint and explicit read
requirement descriptors, plus `CosmicChainQueryCheckpointAdmission.Admit`.
The validator returns `OASISResult`, requires independent head evidence, binds
all source/index/catalogue/schema identities, checks complete configured
coverage, same-slot block identity, monotonic acknowledgements, minimum slot
and maximum lag. Admission does not acknowledge a loaded record or grant access.

Twenty focused controls pass, zero skips; both Core targets and the consumer
build. Evidence `TestResults/CoreCurrent/chain-checkpoint-foundation.trx`, SHA256
`038E7A31EFAEC82549243CB1E6BA75635CF240CD53200FACD914141004BF7E67`.
Tests: `CosmicChainQueryCheckpointTests.cs` in the parent Core.UnitTests project.

This is a callable validation foundation, **not adapter enrollment**. Its caller
must obtain a live independent head and verify finalized ancestry; descriptor
strings are not an inclusion proof. Observation-time/lifetime enforcement,
record receipts, composition/WEB4 propagation, execution integration, persistent
ingestion and native acceptance remain unimplemented. No Solana demo/parity
cell changes. Next: bind record receipts and observation lifetime at the shared
validation owner before connecting source selection to query planning.
