# Indexed metadata: explicit queryable-root profile

27 October2026. Shared policy/source admission and native SQLite schema3 metadata rows/cardinality
are verified. Live-chain metadata dataset and HTTP/demo acceptance remain incomplete.

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

## Exact next acceptance

1. Add a readable SDK/schema3 binding example, then verify it through the shared
   V2 manager and WEB4 alias preparation, including reservation and rejection cases.
2. Enroll a separately acknowledged metadata-bearing synthetic chain dataset and
   index binding; preserve the existing seed, schema2 index and original failure.
   A new fixture is not recovery of missing original history.
3. Verify live SQL/source/HTTP rows and cardinality plus the teaching demo. Selected
   metadata columns and other aggregates need independent capability/evidence work.

Raw evidence belongs in TestResults. Preserve retained source keys/seed/index;
new owned fixture state must be recorded in the existing resource ledger.
