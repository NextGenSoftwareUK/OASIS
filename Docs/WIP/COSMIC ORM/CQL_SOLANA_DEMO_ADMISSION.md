# Solana CQL demo admission

Updated 27 October 2026 after merging published Development. Chain is the next
demo priority; full COSMIC/EF parity and remaining cloud work remain open.

## Current evidence

- No Solana CQL row/scalar/projection enrollment was found in the current provider.
- Published provider baseline remains **4/52**, with original fixtures preserved.
  A read-only diagnostic located an unavailable finalized referenced memo chunk.
  This does not establish its precise cause or authorize replay/reset.
- Existing finalized transaction/signing/fee proofs are separate from canonical
  storage/query acceptance. Do not combine them into a full-chain readiness claim.
- Evidence owners: [provider current status](../HyperDrive-Providers/CURRENT_STATUS.md),
  [release gates](../HyperDrive-Providers/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md),
  [submission evidence](../HyperDrive-Providers/SOLANA_SUBMISSION_CONCURRENCY.md).

## Stored representation and query boundary

Provider source root:
`Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SolanaOASIS/`.

`Infrastructure/Repositories/SolanaRepository.MemoRecords.cs` stores encrypted
records in memo transactions, sometimes as a commit referring to a linked chunk
chain. Decoding checks write identity, index/count, length/hash, writer and repeated
references. A missing referenced transaction rejects the whole record.

`SolanaRepository.Collections.cs` reads finalized signatures for the configured
writer, then authenticates/decodes memo records and selects current versions. The
explicit bound is 100 pages of at most100 signatures; exhaustion rejects rather
than truncates. A broken historical record can prevent a complete collection.

`SolanaOasis.IdentityLookup.cs` loads that authenticated collection and applies the
identity selector with ambiguity detection. This is not an RPC-side metadata or
GUID predicate. Calling it from a CQL adapter would not turn the underlying history
scan into a native database query. Memo records are not program-owned queryable
accounts merely because Solana also exposes account-query RPCs.

## Open gates, in order

| Gate | Required result |
|---|---|
| Original record recovery | Read-only identification of the commit/missing reference and actual RPC/retention outcome; preserve original journals, ledger and keys |
| Canonical storage | Original source records load completely with independent finalized source proof; no skip, reconstructed fake success or replay of uncertain writes |
| Execution mode | Explicitly design/admit native supported lookups or a persistent indexed mode with source signatures, canonical hashes, finality and watermark/lag |
| Index integrity, if used | Same logical source identity/revision; actor visibility, deletion/version history, reorg/retention handling and stale-index policy; no implicit failure fallback |
| Scope and accounting | Server-authorized provider/index scope and real physical attempts; an index host must not masquerade as a native Solana query |
| Demo | Same small C# and API query shapes, accurate local/devnet label, source provenance, unsupported-operator rejection and owned-fixture cleanup |

A broader indexed execution mode must be an explicit shared contract before
enrollment, not a Solana-named SQLite adapter or an automatic Mongo substitute.
Do not add a new chain program, wallet/runtime, funding transaction or replacement
ledger just to avoid the retained-record failure.

Exact next step: locate the existing read-only diagnostic and retained runtime
configuration through the provider inventory, validate ownership/availability,
then inspect the original reference without application writes. No Solana service,
key, account, ledger or transaction was started/changed by this source audit.

## Missing-chunk diagnostic milestone (27 October 2026)

The memo decoder now raises a typed failure with the finalized root signature,
missing chunk signature, zero-based chunk index/count and write ID. These are
transaction references, never keys or payload. Exact and collection reads still
fail closed; tests prove that neither read resubmits writes. The existing native
read-only diagnostic serializes this context when available.

Provider, unit and integration projects build using cached dependencies.
765 unit tests pass, zero failures/skips (including 32 focused wire/frame tests).
Evidence: `TestResults/SolanaCurrent/solana-diagnostic-unit.trx`.
SHA256: `387F7FB53E782C060A3DF64ECE9CD991B80047E77BFA09211AC66C74CD3BEE59`.
No native chain probe was run: original conformance prerequisites are absent from
this worktree. This does not repair the missing historical record or enroll CQL.
Next: identify the original missing reference through a bounded read-only probe
using retained resources, preserving all journals and source transactions.
