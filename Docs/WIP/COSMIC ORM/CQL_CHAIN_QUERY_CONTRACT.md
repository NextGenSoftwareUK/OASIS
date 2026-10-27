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

## Record receipt and observation lifetime milestone

Core `ca32960` adds a mandatory head observation timestamp, positive explicit
maximum observation age and caller-supplied validation clock. Future observations
and expired observations reject, including when an earlier admitted checkpoint
is reused later. Equal instants with different UTC offsets behave identically.

Immutable record identity/receipt descriptors bind logical ID, positive version,
VersionId, deletion state, canonical SHA256, source commit reference and position.
`CosmicChainQueryRecordAdmission` revalidates the complete checkpoint/lifetime,
then compares the indexed receipt with the canonical source owner's separately
obtained receipt. Matching payloads cannot hide another commit or position.
Source references outside coverage or conflicting finalized anchors reject.

Final cohort:38 tests pass, no skips; both Core targets and consumer build.
Evidence `TestResults/CoreCurrent/chain-record-final.trx`, SHA256
`7D81272363F31CDC536EE94151D549F6D4D2343E49A620CCEF9CF2DCC84FDDEA`.
Tests: `CosmicChainQueryCheckpointTests` and `CosmicChainQueryRecordReceiptTests`.

This remains shared validation, not an enrolled adapter/query execution path.
The trusted source owner still must authenticate/decrypt/decode the original
record, compute its canonical hash, observe the actual network/head and verify
ancestry. Passing the same unverified index claim twice does not supply that
independent evidence. Snapshot-wide row/scalar/projection publication, query
binding, durable ingestion and native acceptance are the next integration gates.

## Explicit source planning milestone

Core `2f441eb` adds `WithIndexedSource(requirement)` to the immutable query.
It requires one explicitly named logical source and freezes the requirement.
Predicate/order/window/graph/access composition preserves it. Row, scalar and
projection targets carry the server-authorized index profile; planning rejects
mode or binding substitution. Native capability admission and the shared native
attempt owner reject indexed requests before provider I/O. No implicit native
execution, fallback or storage adapter is introduced.

Six new planning/composition controls pass;44 focused foundation tests pass.
Final affected regression cohort:1,949 passes, zero skips. Both Core targets and
consumer build. Evidence `TestResults/CoreCurrent/indexed-source-final.trx`,
SHA256 `313625967BCAF960A17C3C83849464612A9E2BC9E80B94AA8D24672D0DA9A3E7`.
No WEB4 request binding, indexed snapshot execution or ingestion is enrolled.

User reports no known Solana backup and requests other demo work. Preserve the
failed source, defer recovery and continue with the IPFS admission audit; these
foundations remain available when chain/index execution resumes.

## Durable SQLite ingestion store milestone

`SqliteCosmicChainIndexStore` is now a real SQLite persistence primitive inside
the existing SQLite provider. It is not an ICosmicQueryStorageProvider adapter,
a complete index, source observer or actor authorization grant.

Trusted ingestion supplies a complete checkpoint/independent head, expected prior
position, source receipt, independently decoded receipt and canonical JSON bytes.
Shared admission checks source/network/catalogue/schema/coverage/finality/freshness;
the publisher additionally checks the canonical SHA256 and decoded logical
ID/version/versionID/deletion state before opening a publication transaction.
Maximum1000entries,4MiB per payload/16MiB per batch; bounded reads reject truncation.

One immediate SQLite transaction owns expected-checkpoint comparison, immutable
revision insertion and checkpoint publication. Two independent clients cannot both
advance the same prior position. Exact current-batch replay acknowledges no new
write. Conflicting revisions/checkpoints and newly discovered commits inside an
already acknowledged complete range reject. Existing revisions may be reobserved
only with exact matching source receipt/payload. Subsequent versions advance byone;
initial coverage/bootstrap correctness remains the trusted source owner's duty.

Canonical payloads and checkpoint/batch/count envelope use the existing AES256GCM
codec with source/index/receipt-specific authenticated contexts. Source receipts
and index identity columns remain stored for future SQL projection/query work;
payload history is encrypted. This is not arbitrary plaintext field indexing.
History count and indexed identity columns are checked against authenticated
state/receipts; missing/modified data fails without partial snapshots. A loaded
stored receipt is explicitly not returned as an independently decoded source proof.

Commit exceptions are marked CQL_INDEX_PUBLICATION_UNCERTAIN; reconcile the exact
checkpoint/history before replay. Pre-commit errors return no successful publication.
No source transaction is submitted or retried by this store.

11 real SQLite tests pass: restart/encrypted persistence and exact replay;
forced second-insert failure rolls back earlier row and checkpoint; independent
writer race permits one winner; tombstone history/immutable conflict; altered
payload/late covered commit; deleted history/modified checkpoint/identity columns;
wrong network/key; disposed owner. Exact owned DB files are removed after all
nonpooled connections stop. No chain/node/key/environment is created.

Next: canonical source observer and complete-range bootstrap evidence, durable
index projections with actor-visible native SQL query execution, then row/scalar/
projection receipts through shared CQL and WEB4. Original Solana source recovery
remains separate/red. No parity/demo cell is promoted from this storage primitive.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/ProtocolCqlCurrent/chain-index-store.trx (11pass/0skip) | 7ED43F3368469080D7AAB004D427EB9AC3C4CC857E22EC82FFD59D6BC1EFD824 |

## Canonical Solana range observer milestone

`SolanaRepository.ChainIndexObservation.cs` adds a read-only observer for the
explicit writer/network/holon catalogue. It reuses the existing canonical memo
transaction/chunk/authentication decoder and GetHolon projection. It is separate
from current-record collection selection and retains every observed holon revision,
including tombstones. No source transaction is submitted or index published here.

Source catalogue hash binds the configured writer, network/genesis and this holon
profile. Before scanning, observe genesis, first available block, finalized slot
and native blockhash; reject network/catalogue mismatch or retention newer than
coverage start. Scan finalized writer signatures through that fixed head, with
strict descending slots, distinct signatures, exact transaction slot/writer and
canonical record kinds. Stop only at an older boundary or successful empty page,
then recheck retention. Failed transactions and non-storage activity are explicitly
excluded. Missing/malformed owned records fail the whole observation.

Bounds:100pages of100signatures;1000holon records/16MiB canonical bytes. Existing
chunk decoder retains its own bounds. Repeated revisions/versionIDs and version
advancement against slot order reject. Version0 or missing committed versionID
cannot be silently invented for old data. Empty ranges still carry a captured
head/block and complete configured checkpoint; a successful empty page alone is
not a coverage proof.

`ObserveFinalizedHolonAsync` independently re-reads one original source signature,
validates network/finalized block/slot and produces its canonical receipt/JSON.
Stored index receipts must not be substituted for that independent read. Receipts
are trusted-RPC observations, not cryptographic inclusion proofs. Cancellation
stops awaited work/publication; current Solnet history/header APIs lack a transport
cancellation argument, so an already dispatched read can finish in the background.
No retry or replacement endpoint is introduced.

14 controlled cases pass: valid revisions+tombstone+independent reread, empty,
initial/mid-scan pruning, wrong network, missing transaction, wrong slot,
out-of-order/duplicate signatures, duplicate logical version, version0/missing
versionID, pre-cancel and wrong catalogue. All assert zero submissions. Full
Solana unit suite779pass/0skip. No validator/service/key/ledger was started or
changed, and no native acceptance is claimed.

Next connect the canonical observer/independent receipt to atomic SQLite publisher,
prove native complete-range/bootstrap on a declared source, then implement indexed
SQL projections/actor-visible row/scalar/projection execution and WEB4 receipts.
Original unavailable records remain separate/red; no parity cell is promoted.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/ProtocolCqlCurrent/solana-range-regression.trx (779pass/0skip,14new) | CC9F881354731DA890D9A3A0ADEA600A80C72487B284425CFADE5E3C80434D69 |

## Shared observer-to-publisher bridge milestone

Core1756287 introduces ICosmicChainQuerySourceObserver and
ICosmicChainQueryIndexPublisher plus CosmicChainQueryIngestion.PublishAsync.
These are provider-independent ingestion contracts, not query adapters or access
grants. SolanaRepository explicitly implements the observer using its canonical
range/exact-record reads and a new fresh finalized-head read. SQLite explicitly
implements the publisher by delegating to its sole atomic publication owner.
There is no production dependency between the two providers.

The shared coordinator captures a bounded complete range, validates payload hashes,
independently re-reads each original source commit, refreshes finalized head after
those reads, and admits checkpoint/freshness/receipts before dispatching exactly
one publication. It checks cancellation before that dispatch. Cancellation after
dispatch cannot erase a successful acknowledgement; null/exception/cancellation
from a dispatched publisher is explicitly publication-uncertain and must be
reconciled, never retried automatically. Failed/missing/altered/stale source evidence
causes zero publisher invocations. Empty ranges still need fresh source evidence.

Source errors retain their OASISResult evidence. Core builds net10.0 and
netstandard2.1. SQLite interface boundary rejects missing/null entry batches as
real errors before DB creation. Payload/receipt/checkpoint encryption and atomic
compare-and-publish remain with the existing SQLite owner.

54 Core checks pass (16 new coordinator cases plus existing chain contracts),
17 Solana range/wired checks pass (14 observer cases and3 integrated cases), and
13 SQLite-store checks pass. The integrated test runs actual Solana memo decoding,
shared coordinator and actual SQLite persistence with controlled RPC responses;
restart preserves two revisions including tombstone. Pruned/missing sources leave
no index DB; all assert zero submissions. These are controlled-source tests, not
native Solana acceptance. Test-only SQLite project reference adds no production
provider coupling or new environment/download.

Next: native declared-range/bootstrap evidence and indexed SQL projection/query
execution with actor access/source receipts, then CQL/WEB4 row/scalar/projection
integration. Original Solana records remain unavailable/unrecovered; full demo,
indexed execution and parity cells remain red.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/ProtocolCqlCurrent/chain-ingestion-core.trx | 6EED350AC3D635CC74135A2B48AAB4D80ACD19874AB5165F93A4E29E41B36143 |
| TestResults/ProtocolCqlCurrent/chain-ingestion-wired.trx | 2D1E2127DB2E06B21727BA22C2D34EEC1493C291F35926AB7619D76777C1FBD4 |
| TestResults/ProtocolCqlCurrent/chain-index-store.trx | 74D71CDDDFDA9381D8F3E658C7749651F7CCDC025B668A7FFB775C53747A778B |

Native original-range probe now verifies fail-closed behavior: retained FirstAvailableBlock1402 cannot cover start0; observer returns no records/checkpoint and performs no submissions/publication.1native diagnosticpass is notCQL/bootstrap acceptance. [Exact evidence](CQL_SOLANA_DEMO_ADMISSION.md#native-declared-range-rejection-verified). Next indexedSQL/query execution and controlled-source gates while originalfull-history coverage remainsred.

Indexed canonical SQL row execution primitive now passes30realSQLite/56Corechecks. Explicit schema2 plaintextcanonicalprojection/encryptedfullpayload; atomiccheckpoint/projection integrity, nativeSQLactor/tombstone/filter/order/page and independentrecord/head evidence. Two beforefailures caughttombstonesoccupyingwindows, fixedatSQLselection. Genericmanager/LINQ/tracking/scalar/projection/metadata/WEB4 integration remainred. [Contract, policy, evidence andnext](CQL_INDEXED_SQL_ROWS.md).


## Finalized anchor identity correction (27 October)

Reproduced bug: a minimum slot below the index/head with a wrong block identity
returned HTTP 200 with rows. Numeric freshness and same-slot comparisons were
insufficient. Shared checkpoint admission now requires independent finalized
block identities for the checkpoint, requested minimum and previous accepted
position, even when those slots are below the current head or the page is empty.
Missing evidence returns CQL_SOURCE_EVIDENCE_MISSING; a different observed block
returns CQL_SOURCE_FINALITY_CONFLICT. No rows/publication survive either result.

ICosmicChainQuerySourceObserver.ObserveHeadAsync now takes IReadOnlyList<long>
requiredSlots. Shared RequiredSlots captures at most three distinct slots from
minimum/checkpoint/previous. The source receives slots, not expected block hashes.
Solana binds the actual genesis, reads its finalized head and separately reads
older anchors through finalized GetBlock. A same-head anchor reuses that exact
head observation. Each additional RPC uses the existing SourceBlock tariff and
attempt owner; conservative reservation limits remain enforced.

CosmicChainQueryFinalizedHead retains a captured, bounded FinalizedPositions
collection (at most three, unique slots, no future anchors). Its original
four-argument constructor remains valid for head-only evidence; it cannot prove
older positions. Custom observers must obtain and return those observations.
SQLite queries and the shared ingestion coordinator request anchors; the SDK
executor and atomic publisher enforce the same shared admission contract.
This is trusted source observation, not a cryptographic ancestry/inclusion proof.
