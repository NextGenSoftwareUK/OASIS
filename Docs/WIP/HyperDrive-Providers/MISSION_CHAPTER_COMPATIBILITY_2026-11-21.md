# Mission/chapter compatibility release gate

Status: open. New-data native/HTTP acceptance is recorded in MISSION_GRAPH_COMPOSITION_2026-11-21.json. It does not establish compatibility with existing records. No shared database was queried or changed for this assessment.

## Confirmed source difference

The previous ONODE d51bdd8 Mission declares Chapters as a CustomOASISProperty but does not declare it as a child role. Candidate ONODE 3d4c0e0 declares Chapters as a role and removes the scalar metadata annotation. Core StoredRecordJson excludes holon navigation collections from scalar stored values. Previous populated read qualification failed converting a stored string to IList<IChapter>.

A manifest that only declares Children does not satisfy the new Mission contract, which declares Children and Chapters. Missing historical chapter edges cannot safely be inferred from names, nearby records or an empty collection. New-data proof cannot justify discarding historical chapter metadata.

## Evidence required before deployment

1. A bounded, read-only inventory of the affected environment, with approval/coordination before any shared-cluster operation. Record only counts and sanitized identifiers; retain credentials in the secret store.
2. Classify Mission records by manifest presence and role names, Chapters metadata presence/encoding, and actual chapter identity/parent/revision evidence. Detect duplicate or conflicting ownership rather than choosing a record.
3. For records requiring conversion, prepare an explicit migration that preserves every acknowledged scalar value, chapter identity, parent link, native revision and unrelated role. Use the existing concurrency/storage-binding contract; fail on ambiguous/missing ownership. Do not introduce runtime parsing fallbacks or silently replace a legacy role manifest.
4. Verify a representative legacy record using the real owned backend before any environment migration, then verify bounded readback in each affected environment. Track migration operations and rollback/reconciliation evidence separately from new-data acceptance.

Until those checks are complete, root5d050dd6 remains an isolated qualification candidate, not an approved Development or production rollout. Browser verification may proceed against its owned new-data fixture without granting this release gate.
