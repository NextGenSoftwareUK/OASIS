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

## Original-record and exact-history probes (27 October 2026)

The diagnostic now consumes the registered, SHA-verified committed conformance
report directly. It no longer requires disposable raw reports from a different
checkout. It validates exactly two original runs with distinct sync/async modes.
The conditional `IncludeCoreStorageConformance=true` build and source/PDB guard
pass (70 provider/11 consumer sources); one native diagnostic passes, no skips.
This is diagnostic execution, **not passing storage conformance**.

Both original owner-ID reads fail on the same record: root
`5EyE1Sr5NqGkVuRzfLjAnknpx6RgZr2DUeTEbnpneiFviTVGucXXjwf6kbNRBSDS5VMFhV87eRw2vqU3Zg9vcvqr`,
write ID `71687f37-b7b5-411c-b19b-30de9df38c2a`, missing chunk index0/count5,
reference `5jdHMTckQ4WeE5RSr4hHrkMKNxVpQNpfFJbubHaEswPp4kvgNVmuyhNRDSQEMhLhYPSsqu5ojq2FkhcbRdfFZVis`.
Because ID lookup scans shared history, this does not prove either requested owner
is the record behind that commit.

A subsequent, separate validator session queried both signatures directly with
finalized/max-version1 options and history-enabled status reads. Both transactions
and statuses were absent. FirstAvailableBlock1402, MinimumLedgerSlot1401;
replay snapshot3014/root3062. Earlier keyless session observed finality3048->3052
but zero writer signatures. `solana-keygen pubkey` independently confirmed the
retained fixture public address matches the keyless probe. These observations
establish current node unavailability, not when/how loss occurred, original
non-submission, permanent chain loss, or successful recovery. The differing
session observations remain material; do not infer a pruning cause from them.

`Get-SolanaHistoryAvailability.ps1` provides a bounded read-only probe (max8
signatures); it outputs public availability/slots only, with no payload/key
material, retries, signing or submission. Five synthetic controls pass, covering
presence, successful absence, RPC error, wrong identity and malformed status.
Use via `Run-ReusableSolanaTests.ps1 -InspectRecent -HistorySignatures <array>`.

Evidence remains under this worktree's `TestResults/SolanaCurrent/`:

| Evidence | SHA256 |
|---|---|
| solana-core-read-diagnostic.json | 94FBFD6EC40B51AEB4490E536350CF52E78270216859BF422A526D38A50590B3 |
| solana-core-read-current.trx | 758DBEB9FADAA200E8BA8BC645A702E73A5F4FD529AB0B124598CE35A8484CF6 |
| exact-history-read.log | 785B6F09753069CB768EF272DB1A68840DC7061C8DDDDA6525CD4DABB3BA88AC |

All owned validators stopped; retained source ledger/keys/journals preserved.
No application writes, funding, new environment or downloads. An older backup
location has been requested. Recovery stays red; continue the explicit chain
query execution/source-proof design while backup availability is resolved.

A durable SQLite ingestion-store primitive now passes11realDB tests without anySolanasubmissions, node/key changes or sourcehistoryreplay. It atomically binds encryptedhistory/checkpoint and rejects competingwriters/conflicts. This doesnotrecover originals or enroll Solana CQL. [Contract and nextgates](CQL_CHAIN_QUERY_CONTRACT.md#durable-sqlite-ingestion-store-milestone).

Canonical read-only configured-range observer now passes14controlledcases/full779Solanaunits. It bindswriter/network, head/block and retention, retainsrevisions/tombstones, independentlyrereadsreceipt and rejectspartial/pruned/corruptcoverage withzerosubmissions. No nativevalidatorprobe, originalrecordrecovery or CQLexecutionenrollment is claimed. [Observer contract/limits](CQL_CHAIN_QUERY_CONTRACT.md#canonical-solana-range-observer-milestone). Next publisherbridge/nativecoverage andindexedSQLexecution.

Observer→atomicpublisher bridge is implementedthroughsharedCorecontracts:54Core/17wiredrange/13storechecks pass. ActualcanonicalSolana decoder→Coreindependentreceipt/freshhead coordinator→actualSQLitepersist/restart is controlledRPC-proven, zerosubmissions. Nativecoverage/sourceRecovery/CQLindexedexecution remainred. [Bridge contract/evidence](CQL_CHAIN_QUERY_CONTRACT.md#shared-observer-to-publisher-bridge-milestone).

## Native declared-range rejection verified

The actual canonical range observer was executed read-only against the retained
validator/genesis F22uw23o1HuahQiQVfd7Lfi6Q51fv4KNoYAvDnDDpEvj.
FirstAvailableBlock1402 does not cover declared start0. The observer correctly
returns an error with no range, records or checkpoint. Probe executes four
allowlisted source RPC reads, zero submissions, no index publication or recovery.
One native diagnostic test passes; **native full-range/index/CQL acceptance stays
red**. A successful rejection is not a complete index/bootstrap demonstration.

The existing runner's identity/replay/runtime ownership checks were reused; keys
were read only into memory and cleared, existing uncertain journals preserved.
Validator is stopped; no reset, funding, new ledger/runtime or application write.
The original missing records remain unrecovered (user has no known backup).

Command (preserve existing diagnostic before another run):

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-solana-index-readonly -Operation {
    & Scripts/TestHosts/Run-ReusableSolanaTests.ps1 -ChainIndexReadOnly
}
```

Next implement indexed SQL/query execution with its controlled-source proving
tests. Native full-history bootstrap cannot be certified from this retained node.
An explicitly different declared range/catalogue, if later authorized/configured,
must not be presented as original-record recovery or complete historical coverage.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/SolanaCurrent/solana-chain-index-readonly.json | A4F68F3ADAA21DFA421B83C7CCDB7297075168455793F30A874B4A1503A2D2BA |
| TestResults/SolanaCurrent/solana-chain-index-readonly.trx | 9ADC4711A6152B53B4B09611F6684A849F392272B86384CC41DF2E875292F381 |

Explicit indexedcanonicalSQLrow primitive is controlled-source proven (30SQLite/56Corechecks), not nativeSolanaexecution; source/executionidentities remainseparate. Fullrange0acceptance remainsblocked atretainedblock1402, no sourcewrite/recovery attempted. [Indexedrowcontract](CQL_INDEXED_SQL_ROWS.md).


## Live declared-range SDK demo (new range, not original recovery)

One native acceptance schedule now passes against the retained Agave4.3.0 local
validator/genesis above. New declared coverage starts at finalized slot3073;
a separately journaled canonical synthetic holon was submitted once. Its original
acknowledgement, identities and encrypted SQLite index persist under
LocalState/COSMIC/SolanaCqlCurrent. No new runtime, validator ledger, funding,
reset or replay of an old uncertain submission was used.

The shared ingestion coordinator independently decodes source commits, validates
fresh head/older finalized anchors and atomically publishes schema2 SQLite rows.
After reopening the index, the shared manager/LINQ pipeline returns the exact
source revision/commit and a successful empty page. The test executes
Examples/CosmicCqlDemo/ChainIndexedWalkthrough.cs verbatim; it prints source versus
execution provider, declared range, checkpoint, head and source receipts.
Subsequent verification reused the acknowledged seed with zero source writes.

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-solana-cql -Operation {
    & Scripts/TestHosts/Run-ReusableSolanaTests.ps1 -IndexedQueryDemo
}
```

Preserve existing solana-indexed-native.trx/json in TestResults/SolanaCurrent
before rerunning. Never delete or replace seed.pending.json to make a run pass;
reconcile an unknown outcome first. A pending seed blocks another submission.
The runner refuses an existing report and validates source/binary/runtime/ledger
identity before startup. The original journaled seed remains outside TestResults
so cleanup cannot trigger another submission.

Evidence: TestResults/SolanaCurrent/solana-indexed-native.trx/json (one pass,
zero skips); original demo-code failure is retained as solana-indexed-native-first.trx.
LocalState dataset is retained/reusable, not disposable failure debris. Validator
is stopped after verification. This proves local-validator indexed SDK execution
for the declared range only. Original historical recovery/range0, live-source HTTP,
production authentication/durable billing, full common demo and parity remain red.

Native walkthrough final TRX SHA256: 44C3556C46FB7203FB0F2010F266DAF0732B60E5DBAF0904DB266173A216C7DC; summary JSON SHA256: 80BD7F435BB5B814878DCC42405B52E9898AE817792A4277847F0A2705547571. See the single environment ledger for durable dataset hashes/sizes and verified shutdown.
