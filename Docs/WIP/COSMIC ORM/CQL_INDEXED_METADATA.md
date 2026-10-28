# Indexed metadata: explicit queryable-root profile

27 October2026. Shared policy/source-admission foundation verified; persistent
metadata projection and native/HTTP enrollment are still incomplete.

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

## Next implementation: persistent schema3 (not enrolled yet)

1. Preserve history-only schema1 and canonical-row schema2. Require an explicit
   metadata policy/new schema3 source/index binding; never mutate the retained
   Solana schema2 index or silently rebuild/migrate it.
2. Bind the selected-root profile hash into the authenticated checkpoint's
   projection profile. Reopening with a different selection must fail.
3. Persist only selected queryable roots as MetaDataJson, with an authenticated
   proof bound to source/profile/record revision. Never persist a decrypted private
   dictionary or the encrypted-envelope root in the plaintext projection.
4. Write metadata/canonical projection/history/checkpoint in the same atomic
   transaction. Validate metadata proofs before any SQL result is acknowledged,
   including empty results and cardinality. Tampering must fail closed.
5. Reuse the existing SQLite JSON AST translation, parameterized paths and native
   visibility/window owners. No post-fetch managed metadata filtering/counting.
6. Enroll exact scalar domains/operators only after their SQL semantics pass:
   initial target is strings, Boolean and exact signed integers, missing/null,
   nested paths, ordinal comparison/order and membership. Decimal/float semantics
   and selected metadata require separate proof before enrollment.
7. Prove close/reopen, policy mismatch, private-key rejection before I/O, tamper,
   actor visibility, source evidence and Count/Any windows in actual SQLite. Then
   add declared-range live source and WEB4/demo acceptance with no recovery claims.

Raw evidence belongs in TestResults. Preserve retained source keys/seed/index;
new owned fixture state must be recorded in the existing resource ledger.
