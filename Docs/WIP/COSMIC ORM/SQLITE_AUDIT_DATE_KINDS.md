# SQLite audit timestamp kind fidelity

## Reproduced gap

Audit timestamps must reload with the value and kind acknowledged by the save. EF's existing SQLite DateTime columns retain ticks but return Unspecified kind, while ordinary Avatar/AvatarDetail acknowledgements can still expose the submitted UTC/Local kind. The same read can consequently disagree with its acknowledged commit.

The first native test cohort passes4/fails14 (`TestResults/SQLiteCurrent/audit-date-kind-red.trx`). Two Local holon failures reveal a separate canonical serialization concern: applying UTC conversion to a Local-kind DateTime.MinValue deletion sentinel can turn it into a nondefault timestamp before lifecycle admission. This concern remains a separate open Core audit item; it is not suppressed or treated as a SQLite kind fix.

The timestamp test fixture now uses an actual UTC tombstone, with its actor and inactive state, while varying creation/modification kind. This isolates timestamp round-trip fidelity from the noncanonical sentinel concern. That cohort passes6/fails12 (`audit-date-kind-roundtrip-red.trx`): all Holon routes return a normalized acknowledgement matching their reload, while all Avatar/AvatarDetail cases expose kind loss, either at creation or at the subsequent UTC modification. Both sync and async paths and all three input kinds are exercised.

An earlier test compile attempt incorrectly assumed Avatar implements IHolon; the fixture now uses the actual shared IHolonBase contract. No tests or production mutations ran in that failed compilation.

## Owning-layer repair and verification

Store explicit CreatedDateKind/ModifiedDateKind/DeletedDateKind companions in each relational entity. The DateTime property setter captures the kind, and its getter validates/restores that kind without converting ticks. EF hydrates backing date fields and separate kind columns, so field order cannot erase the companion. All existing repository mappings and audit mutations then share one invariant rather than adding mapping-specific updates.

The direct avatar/detail tombstone patch uses ExecuteUpdate and bypasses CLR setters. Its existing atomic update now writes DeletedDateKind alongside DeletedDate, and its tracked-state refresh includes the same companion. Exact repeated tombstones remain no-write operations. The strengthened native test checks stored and tracked UTC kinds, original values and absence of dirty lifecycle fields.

The additive activation upgrade and formal migration will default old rows to Unspecified, preserving their prior observable behavior. No timezone or precision will be invented for old data. Downgrade must refuse nondefault kind information. Exact ticks remain in the existing date columns, preserving current sentinel/filter/index behavior.

Both Core audit generation and this provider's own tombstone generation already use UTC. This change preserves physical clock ticks and DateTime.Kind; it does not store a timezone identifier or disambiguate Local DST transitions. Portable instant interpretation of arbitrary Local/Unspecified input remains part of the broader audit-date policy.

Provider build succeeds with zero errors. Focused35/35 passes, zero skips (`TestResults/SQLiteCurrent/audit-date-kind-focused.trx`):18 save/reload/update routes,9 legacy/migration/corruption cases and8 existing snapshot checks. Formal migration `20261020080000_AddAuditDateKinds` and its target model/snapshot are aligned.

The first full regression passes1200/fails3 (`TestResults/SQLiteCurrent/audit-date-kind-full.trx`). Two older migration fixtures now contain retained UTC-kind data, correctly rejected by the newer guard before the migration those tests intend to exercise. Their setup now explicitly constructs the prior Unspecified representation while preserving clock ticks, so their own historical guards remain the subject of the test. An expected partial downgrade can also remove newer columns before an older guard rejects; retained-presence assertions now project only the column they verify instead of materializing the latest whole-entity model against that older schema. The shipping downgrade guard is unchanged. Corrected focused verification passes **43/43** (`TestResults/SQLiteCurrent/audit-date-kind-final-focused.trx`). The complete SQLite integration suite passes **1203/1203**, with zero failures or skips, in 2m14s (`TestResults/SQLiteCurrent/audit-date-kind-final-full.trx`). The legacy fixture corrections therefore preserve the previous migration checks while the new guard retains its own positive and rejection coverage.

Canonical audit-date CQL admission remains unsupported until this storage prerequisite and the date representation/query domain are verified. The full COSMIC ORM roadmap remains open.
