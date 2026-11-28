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

### 2026-11-28 UTC — O07 native acceptance, 3/3

Run46103037019/job140849566886 passed exact sourcecdefa9eebabe935a3a854aa63974b1f96904131f/Core6613656a264d6d06cdf15af2eb3ff151da31b568. RAM-only artifact15246538702, 3702bytes, SHA2561418943911eb9b9b31ccaa2b644fec8c8e7a2b93c5a2b6a1c2fcd2981e96a63a, contains exactly native3 TRX, three10-group reports and source.json. Three cases passed, zero failed/skipped, actual WEB4 built;5 owned IDs/3 writes/10 timings per store and exact fixture cleanup verified. Mongo removal and service cleanup succeeded. Receipt recovery never replays an uncertain write, even an absent receipt. Fault injection is a client transport boundary; actual writes/receipts use shipping native stores.

O07 promoted; new fully verified rows this session1/1 claimed completed. M01 remains0/3 pending, M09 in progress. Table status cells now142/168; preserve all rows despite historical130/164 owner baseline. Source not yet integrated/deployed. Compact acceptance JSON retained; zip was read in memory and released, never saved. No local env/build/database/log archive, shared Atlas/PIPER/API/DNA/deployment operation.

### Development integration

Verified O07 source was integrated into Development at6f3e5bf71d29a1052a18c1d98f8bf7ae21ff195b, Core6613656a264d6d06cdf15af2eb3ff151da31b568. All19 scoped paths were preserved byte-for-byte, alongside the current HyperDrive/Antelope batch. Root and Core remote Development refs were independently verified after a tool observation timeout; no push was repeated. Manifest/gitlinks matched before push. Runtime/live acceptance is pending and is not inferred from the push. The next ordinary Development batch must respect the30-minute cadence; M01/M09 remain feature-branch candidates. Docs-only evidence is copied to master without promoting runtime source.
