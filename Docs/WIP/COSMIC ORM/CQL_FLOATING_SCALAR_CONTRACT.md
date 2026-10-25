# CQL floating scalar implementation contract

24 October 2026. Core's finite bit-key codec/group capture passes the full
2,507-test suite and both configured targets, netstandard2.1/net10.0. SQLite
Double SDK metadata passes the full1,485-case native suite; Mongo/Neo Double
and all Single query enrollment remain open. Remaining native adapters, transport and
live examples remain open; the Decimal acceptance scope is unchanged.

## One finite value-key owner

`CosmicFloatingScalarResults` encodes finite binary64 values as16 uppercase hex
digits with numeric ordinal ordering. Negative values invert their bits;
nonnegative values flip the sign bit. Both zero encodings have one identity.
NaN/infinity cannot become keys. Decode rejects malformed, noncanonical,
nonfinite and negative-zero packets with `CQL_DATA_INVALID`.

Single uses the exact widened binary32 value in this same key space. Its decoder
requires exact binary32 representability; it cannot round a binary64-only packet,
overflow or underflow it. A stored representation must be normalized to its
declared finite type before producing a key. This codec alone does not admit a
storage type, authorize a query or prescribe client-side execution.

Six new Core facts exercise8,192 finite random bit patterns, extrema/subnormals,
ordinal ordering, zero identity, malformed/nonfinite keys and invalid Single
packets. Evidence: TestResults/CosmicCurrent/floating-codec-full-current.trx;
build/test log: TestResults/CosmicCqlDemo/floating-foundation-full.log.

## Storage prerequisite: Single must remain numeric and acknowledgement-safe

Mongo's metadata serializer handles Double but previously sent boxed Single
through complex JSON text. Six finite root/nested/array controls reproduce that
loss; three nonfinite controls already reject. BSON has no Single primitive.

The replacement must preserve BOTH binary32 round-trip identity and Core's
canonical JSON snapshot value used for save acknowledgements. Blindly widening
Single0.1 to Double changes its serialized numeric spelling from0.1 to
0.10000000149011612, violating the second invariant. The new acknowledgement
control reproduces this separately:9pass/1red. The repaired producer stores the
Single's canonical round-trip numeric spelling as BSON Double, preserving both
binary32 round-trip and snapshot value identity. Full493 Mongo units now pass;
the full231-case native gate including3 real storage round-trips also passes.
Both zero signs have one numeric identity;
the storage test normalizes zero before comparing binary32 bits.

Historical Single values already stored as strings stay strings. Do not infer or
silently migrate their type. Explicit migration intent and native evidence are
required. No legacy Hyperdrive code changes are part of this work.

Actual42737 terminal0:493 full Mongo units and231 full native CQL cases pass,
zero skips; source snapshots remain unchanged. The three new native storage
cases verify fractional/subnormal/maximum values through actual save/load,
numeric BSON and canonical snapshot comparison. No floating query is dispatched.
Evidence: SubscriptionUsageCurrent/floating-storage-full-current.trx and
mongo-cql-native-current.trx; CosmicCurrent/floating-foundation-identities.json;
CosmicCqlDemo/floating-storage-native-final.log. Native fixtures cleaned and
owned service stopped; no new environment/download/restore was needed.

## Remaining enrollment gates

### SQLite Double SDK slice verified

The prepared adapter interprets JSON numeric tokens as finite IEEE binary64,
including the rounding inherent in an explicitly Double-valued query. This does
not change integer or Decimal query semantics. Nonzero underflow cannot become a
matching zero; overflow and foreign JSON types remain outside the value domain.

`cosmic_cql_double_key` runs inside SQLite's predicate/order/reduction pipeline,
using the original token and the shared Core16-character key. The same connection
read-budget owner registers/unregisters it and checks cancellation, deadline and
the4,096-character scalar-token limit before publication, even for excluded rows.
Projection, extrema and grouping decode through Core's finite-key owner. There
is no hydrated-row filtering or REAL-based substitute path.

Core now captures Double/nullable Double group keys without granting a provider
capability. The first gates pass2,507 full Core tests and26 focused native Double
cases. Actual40902 terminal0 also passes fullSQLite1,485 regression,0fail/skip;
Mongo/Neo Double, all Single query paths and floating transport/live demos remain
open. Source snapshots are unchanged; native/producer pairs match Core57A3770B,
Shared6D182110 andSQLite02B743C3. Evidence:
CosmicCurrent/double-sqlite-core-current.trx, SQLiteCurrent/double-focused-current.trx,
SQLiteCurrent/double-full-current.trx and double-identities-current.json;
CosmicCqlDemo/double-sqlite-full.log. Owned fixed DB/WAL/SHM are absent after
verified teardown. No installation or restore was required.

- Define and verify the finite persisted value domain on all three stores,
  including binary32 normalization, native comparisons/order and typed decoding.
- Cover null/membership/field comparisons, extrema/grouping, wrong domains,
  boundaries and native budget/cancellation ownership.
- Expose certified types additively through WEB4 with exact round-trip strings;
  preserve enum identities and the existing v1 surface.
- Run matched provider/native/HTTP tests and update annotated demos/docs. Do not
  infer completion or enable capabilities from this foundation checkpoint.
