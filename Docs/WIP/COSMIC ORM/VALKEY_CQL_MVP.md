# Valkey CQL MVP — pending native admission

Updated 29 October 2026. Breadth-first scope; not demo-certified yet.

The existing encrypted Redis-protocol transport now has an explicit authority
specialization point. Valkey binds its own authority and uses the same executor,
canonical save keys and authenticated encryption. Mutable provider identity alone
cannot grant capabilities to another Redis-compatible facade.

The bounded profile selects authorized IDs (equality/membership/Boolean algebra,
maximum 100 catalogue IDs), excludes missing/deleted records and applies paging.
It does not implement arbitrary field filters, child loading or global snapshots.

## Verified controls

- Redis: 8 passed, 0 failed/skipped; `TestResults/ValKeyCqlCurrent/Redis-controls.trx`; SHA256 `45b19fa12595250cacf30b1945337ca7954bb4e38b50dd54ad5eb5f28873d427`.
- ValKey: 8 passed, 0 failed/skipped; `TestResults/ValKeyCqlCurrent/ValKey-controls.trx`; SHA256 `afba3b626505935fc6bf5e3f56cab628fffd2b111147e12ea67458800242851a`.

Controls cover immutable captured selections, authority separation, unsupported
predicates/order/children/catalogue bounds, inactive provider and pre-cancellation.
Builds and tests used the worktree build lease and bounded workers. Valkey restore
reused the existing NuGet cache; no native service/download was introduced.

## Next acceptance

Run against an independently identified native Valkey server; verify encrypted
source records, several-record filtered selection, missing/deleted/paging/budget
behavior and restart preservation. Then add the Mongo paired C#/HTTP/browser demo
and launcher preset. Keep the inventory red until those proofs exist.
