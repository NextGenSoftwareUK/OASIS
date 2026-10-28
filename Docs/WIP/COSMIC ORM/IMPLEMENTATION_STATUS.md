# COSMIC ORM implementation status

[Current status](CURRENT_STATUS.md) is the recovery entry point; [checklist](CQL_PARITY_CHECKLIST.md) owns acceptance.

- Full ORM/CQL programme remains incomplete. Work only on red cells; green scopes/cursors stay frozen absent a reproducible bug.
- Original twelve investor-demo rows are green on SQLite/MongoDB/Neo4j. Q01a-e/Q23/Q23e/Q49a remain green within their declared scopes.
- Active Q01f remains red on all3. Entity stages, entity cardinality/elements/reversal/default overloads and final entity-stage Select are native-verified on all3.
- New combined entity-selected owner retains both sequences and independently re-admits actual capabilities. SQLite, MongoDB and Neo4j enroll native selected Where/order/ThenBy/Skip/Take/Reverse after entity stages; standard deferred sync/async LINQ and per-read closures are verified.
- Verification:2173 CQL shared tests and221 full SQLite provider tests pass; both Core target frameworks build. Native fixtures are process-owned in-memory SQLite, not retained user databases.
- Mongo verification:220 CQL/compiler/admission tests and23 live projection cases (14 earlier +9 combined), exact fixture cleanup and service teardown pass.
- Neo verification:151 CQL controls and23 native cases; companion SQLite runs the same23 cases. Canonical input proof and selected proof are streamed by one statement/read owner; fixture cleanup and container teardown pass.
- Reselection now works in shared explicit SDK and standard LINQ on all3:2178 CQL/4262 full Core and26 native cases/database pass. Intermediate DTO constructors never execute; dropped invalid input columns remain validated.
- Entity-selected Count/LongCount/Any/predicate overloads now pass2182 CQL/4266 full Core and34 native cases/database; independent cardinality enrollment, native result/count proof and no DTO construction verified.
- Next:All/element/numeric terminals and combined graph sources. These remaining integrations are unsupported.
- Eight-provider demo expansion remains ahead of large parity-only rows (POCO without IHolon/migrations tooling). Cosmos native/local browser verified; IPFS/SOLID/ActivityPub have point profiles, not general-query parity.
- Solana schema3 selected-metadata SDK/HTTP and exact values verified; original ledger recovery remains blocked by missing historical root/chunk with no known backup. Do not reset/replay it.
- Production hosted auth/durable settlement/scaling and broad cross-provider snapshots remain open; HyperDrive implementation work is V2 only, legacy gaps documented.
- Source work only in C:/Source/OASIS-cosmic on work/cosmic-cql. Task commits pushed; Development integration stays batched once/twice daily.
- Resources/evidence paths/hashes and safe lifecycle steps: [single ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). Raw output stays in worktree TestResults.

[Previous checkpoint](Archive/IMPLEMENTATION_STATUS_before_entity_selected_sqlite_2026-10-28.md) preserves history without establishing broader completeness.
