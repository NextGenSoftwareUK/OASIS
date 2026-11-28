# GPT COSMIC parity ledger

## 2026-11-27 — O07 execution strategy

- Claim: claims commit0b1d0686126a76df69f117b71ef96ecc80f0c557; session gpt-cosmic; branch work/gpt-o07.
- Core implementation:6613656a264d6d06cdf15af2eb3ff151da31b568. Native acceptance:0/3 pending CI; O07 credit0/1 pending. Reported owner baseline130/164 is not incremented from source presence.
- Contract: retain operation ID, actor, expected revisions, captured payload and physical source across bounded native transaction retries. Only source-bound confirmed-abort evidence permits retry. Unknown outcome uses one bound receipt read; committed or absent receipts never authorize replay or falsely publish a saved acknowledgement.
- Real providers: MongoDB, SQLite and Neo4j. Optional source-bound entry points reuse their existing native transaction owners. Mongo abort failure is reconciliation-required; SQLite busy/locked and Neo4j deadlock get retry certificates only outside commit/rollback uncertainty.
- CI: three real-store journeys, ten feature groups each. Transport-boundary fault injection delegates all database writes and receipt reads to shipping native providers; no mocked datastore.
- No PowerShell: new CI uses Bash/Python and a portable wrapper for the existing adjacent producer-consumer.lock. Win32 exclusive sharing matches the original lease; two local ownership/release tests passed, with temporary fixtures removed. No local .NET build, database, runtime install or shared Atlas/PIPER calls.
- Shared checkout protection: an unrelated pending ACTIONS-FOR-DAVID edit prevented a checkout switch. Parent commits are isolated on work/gpt-o07 using a separate temporary index; existing working-tree changes are preserved. Core's clean checkout uses work/gpt-o07 directly.
- Discovered accounting discrepancy: the current checklist text has168 feature entries,141 with no red cells, while the handoff/roadmap report130/164. Do not silently credit rows or rewrite accepted evidence; normalize the tally against the declared checklist scope separately.
- Remaining: native3 CI, representative failure/cleanup evidence, scoped Development batch, docs-only master integration. Other phases and deferred owner decisions remain intact.

### 2026-11-28 UTC — O07 CI contract correction; backend query ownership

Native run 46100733894 stopped at SQLite compilation: its repository interface omitted the new bound execution method. Added the exact contract to IHolonRepository and made its implementation public. No native acceptance credit; MongoDB/SQLite/Neo4j verification remains 0/3 pending the corrected run. Owned CI Mongo container and service cleanup succeeded. No local build output or downloaded logs retained.

SQL and AI query engines remain backend-owned (`POST /api/data/query-cql/sql` and `/ask`). Future engine improvements belong in server code; do not modify Web4Site cql-text.js or cql-ask.js.

### Native test host dependency correction

O07 run46102113566 reached native test-project compilation and exposed an existing linked-source omission: CqlHostedSources now calls the shipping CqlSandboxSidecar, but the linked HTTP test host did not compile that dependency. Link the actual shipping class alongside its caller; no stub or duplicate implementation. The same host fix is applied to both pending row candidates. Native acceptance remains 0/3 pending corrected CI; no runtime or deployment change.
