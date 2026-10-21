# SQLite CQL native resource bounds

The CQL ordering/paging implementation now owns native query work and materialization limits. A page size alone cannot bound blocking-sort work or the payload loaded by a query.

The new read owner installs a connection-local SQLite progress handler while holding the EF concurrency scope. It interrupts at cancellation, thirty seconds or fifty million native VM instructions, checking every thousand instructions. A temporary native `SQLITE_LIMIT_LENGTH` of at most64MiB also prevents oversized strings/BLOBs/rows from being materialized. Disposal removes the callback and restores the exact previous limit before an owned connection closes. Existing stricter limits are preserved.

Native rows are streamed rather than collected by EF before validation. The reader bounds row count and cumulative UTF-8 physical string-column payload plus fixed scalar overhead to64MiB, before hydrating each holon. Rejection returns no partially loaded result. Empty pages still avoid opening the database.

Original end-to-end payload baseline genuinely failed: all nine8MiB saves succeeded, but the old query returned no `CQL_QUERY_LIMIT` error (`TestResults/SQLiteCurrent/cql-payload-red.trx`). SQLite's existing fixture disposed that original test database. The corrected focused gate passes **5/5**, zero skips (`cql-resource-current.trx`): the same oversized query rejects without partial rows, a following query on the same provider succeeds, real native SQL is interrupted for work/deadline/cancellation/record size, and the original native limit plus an unrestricted following command are verified after disposal.

The producer compiled during the concurrent AWS acceptance run and again in the owned resource window; Core1853304C was held unchanged. The later complete SQLite suite passes **1,145/1,145**, zero skips (`TestResults/SQLiteCurrent/cql-window-full-current.trx`), after the separate avatar presence/migration fix. SQLiteAC4C384E exactly matches its complete-test consumer copy. Final immutable-source artifact matching/publication remains required. No owned disposable database remains after execution; free disk is approximately60.4GiB.

The concurrent native karma fixture also exposed a separate SQLite nullable AvatarType projection defect before any karma mutation. That original fixture remains preserved by its owning session. Its root fix and acceptance are separate from this query resource gate.

Final pinned acceptance uses clean Corefe492b2: complete Core1,606, Shared925, Mongo197 controls/3 native and SQLite1,145 pass, zero skips;17 declared consumer copies match. [Final reports and hashes](CQL_ORDERING_PAGING.md#final-immutable-source-acceptance). The earlier producer hashes above retain their historical scope. The separate SQLite Akashic collection persistence gap remains open.
