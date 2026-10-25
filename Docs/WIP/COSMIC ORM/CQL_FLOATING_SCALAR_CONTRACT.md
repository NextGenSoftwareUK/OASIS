# CQL floating scalar implementation contract

24 October 2026. Foundation only: Core's finite bit-key codec passes the full
2,506-test suite and both configured targets, netstandard2.1/net10.0. No provider
currently advertises Double/Single CQL metadata. Native adapters, transport and
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

- Define and verify the finite persisted value domain on all three stores,
  including binary32 normalization, native comparisons/order and typed decoding.
- Cover null/membership/field comparisons, extrema/grouping, wrong domains,
  boundaries and native budget/cancellation ownership.
- Expose certified types additively through WEB4 with exact round-trip strings;
  preserve enum identities and the existing v1 surface.
- Run matched provider/native/HTTP tests and update annotated demos/docs. Do not
  infer completion or enable capabilities from this foundation checkpoint.
