# Indexed metadata: explicit queryable-root profile

27 October2026. Shared policy/source admission and native SQLite schema3 metadata rows/cardinality
are verified. Declared-range live Solana/SQLite metadata SDK/HTTP/demo acceptance now passes;
production identity/durable settlement and broader parity remain open.

## Invariant

An index may export only metadata roots explicitly selected by a server policy
and already exempted from metadata encryption. A request cannot grant this consent.
A query must not infer access to another root because its CLR type is supported.
This is query-index enrollment, not an actor authorization or decryption grant.

## Implemented

- HolonQueryableMetadataPolicy captures1–32 exact ordinal roots. Each must be in
  HolonManager's existing system encryption-exemption list or the trusted runtime
  HolonMetadataEncryptionPolicy.AdditionalQueryableKeys. It reuses the existing
  list through one internal owner; the list/encryption behavior is unchanged.
- __oasis_enc__ is always forbidden, including when incorrectly added to an
  additional exemption list. Invalid Unicode/control/empty/duplicate roots reject.
- The immutable sorted selection has a deterministic domain-separated SHA256
  profile hash. Construction does not invoke or retain a key resolver.
- CosmicQueryCapabilities.WithMetadataRoots creates a detached narrowing of
  source predicate/ordering enrollment. Null means the adapter's existing
  unrestricted enrollment; an explicit empty collection admits no metadata roots.
  A previously restricted profile cannot be broadened by another call.
- Every source comparison, ordering, metadata presence/null, membership, string,
  field-comparison and nested logical/child predicate checks the exact root.
  Independent projection/aggregate capabilities do not become enrolled by this API.

Tests: [policy and source admission](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicQueryableMetadataPolicyTests.cs).
17focusedcases pass; full Core result recorded in CURRENT_STATUS/cleanup ledger.

## Persistent schema3: implemented and verified

- A constructor taking HolonQueryableMetadataPolicy explicitly selects schema3.
  Schema1/history and schema2/canonical constructors keep their existing signatures
  and proof format. A schema2 source with a metadata policy, or schema3 without
  one, rejects; no retained index is migrated or reconstructed.
- The selected-root hash is part of the authenticated checkpoint projection
  profile. Reopening with another profile fails before returning a snapshot.
- MetaDataJson contains only selected roots, using ordinal key matching. It never
  copies __oasis_enc__, private roots or unselected system roots. Original source
  payload remains encrypted in history. No metadata key resolution/decryption occurs.
- A separate encrypted metadata proof binds the source, profile, logical record
  and VersionId. Metadata/canonical projection/history/checkpoint publish in the
  same SQLite transaction. Integrity validation precedes rows, empty results and
  cardinality; metadata tampering returns no result or source-read dispatch.
- Rows and Count/LongCount/Any reuse the native JSON AST translator, visibility,
  ordering and source-window owners. Schema3 connections use the existing native
  CQL budget/UTF-8 function owner. Missing registration was reproduced by five
  metadata cases and fixed at connection setup; no alternate translator was added.
- Enrolled domains: string, Boolean, Int32, Int64 and nullable value variants;
  equality/range where meaningful, ordering, membership, presence/null and nested
  paths. Decimal/Single/Double, selected metadata columns, graph and selected-stage
  operators remain unsupported. Actor access is independent of metadata enrollment.

Actual SQLite tests:22newcases,98indexed-store cases and full205provider unit tests
pass with0failures/skips. They cover close/reopen, wrong profile, absent policy,
forbidden roots before database creation, private/envelope/unselected-system key
exclusion, missing/null, each enrolled primitive/nullable domain, exact Int64 above
2^53, ordering/membership, zero window, hidden/deleted rows, JSON/proof tampering
and rollback of metadata/history/checkpoint after a projection insert failure.
Source transport in these tests is controlled; live-chain and production claims
must not be inferred. [Proving tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests/SqliteCosmicChainIndexMetadataTests.cs).

## Shared manager and WEB4 preparation: verified

[Readable C# walkthrough](../../../Examples/CosmicCqlDemo/ChainIndexedMetadataWalkthrough.cs)
uses ordinary LINQ over an explicit source/index/actor grant: nested string and
exact Int64 filtering, ordering, evidence-bearing rows, LongCount, Any and a
zero-length source window. It invokes the shared V2 manager and native owners.

Actual SQLite acceptance now includes a real authenticated metadata envelope and
an authorized runtime decryption policy. The returned row decrypts its private
value and remains clean for tracking; hidden rows are excluded. Native scalar
operations do not decrypt business payloads. With the runtime context omitted,
the row is rejected with HOLON_METADATA_DECRYPTION_CONFIG_MISSING and no private
value is published. This replaces an invalid placeholder-envelope fixture;
no decryption guard was relaxed. Full SQLite provider suite207passes.

Seven Core admission cases prove schema3 alias preparation for rows/LongCount/Any,
exact root/case/envelope rejection without I/O, capture before resolver mutation,
positive reservation bounds and rejection of history-only/unknown billing schemas.
Tariff enrollment was still schema2-only; it now explicitly accepts schema2/3.
The schema stays in BindingHash, so schema2 reservation identity cannot authorize
schema3. Full Core4235passes. These controlled preparation tests are not metadata
HTTP/native-transport acceptance.

## Live schema3 SDK, WEB4 and teaching demo: verified

The retained Agave4.3.0 validator was reused. A separately journaled synthetic
metadata fixture was acknowledged once under LocalState/COSMIC/SolanaMetadataCurrent.
It has a new source range3325 and index86d676b6-c2c5-4c8f-98b2-a88e0fb8bb11.
The source record a03306d4-649f-4871-8f27-8e13869b59a1 contains Status.category and
exact Status.amount9007199254740993 plus a privateNote excluded from the query
projection. The canonical Solana owner encrypts its source frame. This gate is not
original-history recovery or a live field-level metadata-key-resolution certificate;
the separately verified SDK test covers that authenticated envelope/runtime context.

The initial filter gate passed1/1 with42HTTP requests (superseded by the50request
selected gate below):19accepted,19replays rejected and
4unsupported before execution. Six metadata cases cover rows, LongCount, Any,
zero/empty source windows and canonical projection over metadata filters. Three
forbidden roots (privateNote, wrong case and __oasis_enc__) reject before reservation
or physical reads. Accepted metadata reads settle their exact measured service
receipts. Source/index/schema/checkpoint/head provenance is retained; privateNote
is absent from public HTTP responses. Identity/outbox/authority remain controlled.

First run submitted exactly one new seed, acknowledged durably before indexing.
The first report is hash-archived. A second bounded run and an interactive run reuse
that acknowledgement with submittedNewSeedfalse/sourceWrites0. The old schema2
seed/index and original missing-history evidence remain untouched. No pending
metadata seed journal remains. Index32768bytes and acknowledgement511bytes are retained.

```powershell
# Run from C:/Source/OASIS-cosmic. Preview starts no service.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -ShowPlan

# Bounded acceptance; preserve completed matching report before reuse.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -VerifyOnly -ArchiveCompletedDemo

# Serve on http://127.0.0.1:5089/ until POST/demo/shutdown.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -ArchiveCompletedDemo
```

The page enables controls after health readiness and shows metadata controls only
for schema3. The additional interactive exact-integer filter/LongCount returned1;
page/profile/HTTP/shutdown lifecycle passed. JavaScript syntax checked with reused
Node24.19.0; browser rendering/click automation remains unverified. Validator18899
and page5089listeners are absent after teardown. Continuous ingestion uses the same
shared acknowledged-checkpoint session.

Completed report archiving requires matching retained seed identity/signature/index/
network and completed native evidence. A first-seed report is archivable only when
its source acknowledgement matches; incomplete/unknown outcomes are preserved.

## Remaining acceptance

Selected metadata SDK/SQLite/live HTTP/demo execution is verified below;
other aggregates/selected stages, production JWT and
durable external settlement, hosted-chain acceptance, incremental source transport
and history/index scaling remain open. Do not promote broad EF/NHibernate parity
from this explicit declared-range profile. Evidence hashes/paths are recorded in
the resource ledger; raw reports stay in TestResults.

Raw evidence belongs in TestResults. Preserve retained source keys/seed/index;
new owned fixture state must be recorded in the existing resource ledger.

## Selected metadata: shared SDK and SQLite verified

Projection capabilities now have an independent immutable root restriction.
Predicate support never grants selected-column support. Indexed metadata selection
requires an explicit root profile during planning and actual-reader re-admission;
wildcard roots cannot authorize it. Canonical fields retain their own domains.

SQLite schema3 explicitly enrolls string/Boolean/Int32/Int64 and nullable variants
for selected metadata. One native projection translator/decoder handles indexed
canonical and metadata cells; a declared index column map binds VersionId/IsDeleted
without borrowing the entity-table schema. Source filters and selected columns
share parameter allocation. Missing/null/wrong-type state is validated natively.
Schema2 remains canonical-only.

Before any DTO constructor runs, the shared indexed owner compares each selected
cell against its independently validated source witness, using exact typed metadata
traversal. Only the selected root is serialized; encrypted/private sibling roots
are not traversed or decrypted. A forged value rejects with CQL_SOURCE_RECORD_MISMATCH.

Eight new Core cases prove root isolation/narrowing, planning-time wildcard
rejection, actual-profile re-admission and proof before DTO construction. Twelve
new actual SQLite cases cover primitive/nullable selection, null/missing/wrong type,
private-root denial, mixed canonical/metadata column order and exact Int64 above2^53.
Full results/hashes are in CURRENT_STATUS and the resource ledger. Live selected metadata HTTP/demo acceptance now passes in the expanded50request
gate described below; the earlier42request report is preserved in the archive.

```csharp
// The host has already published an explicit schema3 Status-root profile.
var selected = query
    .Where(holon => holon.Name == "COSMIC native chain coffee")
    .Select(holon => new
    {
        holon.Id,
        Category = CosmicQueryMetadata.Value<string>(holon, "Status", "category"),
        Amount = CosmicQueryMetadata.Value<long>(holon, "Status", "amount")
    });

// Retain checkpoint/head/source receipts rather than hiding provenance.
var page = await selected.ToCosmicIndexedProjectionPageAsync();
```

## Live selected metadata HTTP and demo acceptance

The expanded schema3 gate passes50HTTP requests:21accepted,21idempotency replays
rejected and8unsupported requests rejected before reservation/execution. It reuses
the same acknowledged fixture with submittedNewSeedfalse/sourceWrites0.

The new selected-metadata cases return canonical Id plus nested category, exact
Int64 amount and a missing nullable Int64. The wire amount is the decimal string
"9007199254740993" and the missing cell is null. An empty selected result retains
its checkpoint/head. Actual source receipts remain aligned with returned rows.
Private/wrong-case/envelope roots and unsupported Decimal selection reject before
reservation or reads. Accepted reads settle their exact captured physical receipts.

RunSelectionAsync in ChainIndexedMetadataWalkthrough.cs runs the same shape through
standard LINQ Select and the shared V2 manager. It verifies the exact DTO values
and source evidence against the retained validator/reopened index. The interactive
SolanaMetadata page now includes Select metadata columns. HTTP page/profile/query/
shutdown acceptance passed with an additional selected request; Node syntax and
example build pass. Browser rendering/click automation remains unverified.

Both bounded and interactive native tests pass1/1. The validator18899and page5089
listeners are absent after teardown; the seed/index/keys remain reusable. Reports:
TestResults/SolanaCurrent/solana-indexed-metadata-native.trx/json and
TestResults/ProtocolCqlCurrent/index-selected-metadata-interactive-lifecycle.json.
Earlier reports are hash-preserved by the runner. Production identity/durable
external settlement, other aggregates/selected stages and broad EF parity remain open.


## Browser acceptance verified — 30 October

Retained Agave4.3.0/genesis and schema3 index/acknowledged seed reused. One live native schedule passed with submittedNewSeed=false/sourceWrites=0. All nine browser buttons completed HTTP200/settled: rows1, selected rows1, Count1, LongCount"1", Anytrue, empty Count0, nested metadata rows1, metadata count"1", selected metadata rows1. Each completed response was captured after query buttons were re-enabled. Projection preserves amount"9007199254740993" and missing=null, with actual Solana receipt, declared coverage/checkpoint/finalized head and explicit SQLite execution provider.

Evidence under TestResults/SolanaCurrent (raw files not committed): browser-query-proof.json SHA2561B26C4B13F2A8D335FE73315E7F4AE3264BD2D83B82B1BC70DA32292F650F15E; browser-selected-metadata.png SHA256772C589BAFDA629FC1B6FB42329B31B362B58C038F70261D712BE8B1E3287408; solana-indexed-metadata-native.trx SHA256A267AEF5E0549B0D5164BEDBA8DCEBA9B9EE881C47602958E73C6EE2761B39DE. Previous completed reports hash-archived by runner.

Command: wrap Run-ReusableSolanaTests.ps1 -IndexedMetadataDemo -ServeIndexedDemo -ArchiveCompletedDemo once in Invoke-WithProviderBuildLease.ps1. Native host5089. POST/demo/shutdown returned202; runner exited0, validator process and18899/5089 listeners absent. Original seed/index/keys/ledger retained. Free bytes66961833984before/66858381312after. No runtime/install/download/restore/new ledger/funding/source write.

This closes browser rendering/click acceptance for this local declared-range demo. It does not close original-history recovery, hosted-chain identity/settlement, full parity or Mongo+Solana federation. Next reconcile the latter's preserved implementation/current proof and catalogue entry.
