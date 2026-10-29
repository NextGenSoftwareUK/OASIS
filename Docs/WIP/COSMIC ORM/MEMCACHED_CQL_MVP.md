# Memcached CQL MVP admission

Updated29 October2026. **Native cache and Mongo-paired demo remain red.**

## Implemented scope

Explicit authoritative UUID catalogue (at most100 IDs) compiles to bounded native
GETs on the same holon:{GUID-D} keys as Save/Load. Unsupported business predicates,
ordering/children reject; no managed predicate or cache index scan substitutes.
Missing/deleted records are removed before paging; errors return no partial page.
Aggregate decoded payload4MiB is checked after allocation. The pinned SDK does not
receive a cancellation token for GetAsync; checks occur before/after each awaited
read, with no detached work or prompt in-flight cancellation claim.

The shared read helper previously used GetValueAsync, which maps failed reads to
the same default as cache misses. It now inspects native GetAsync result: only
binary KEY_ENOENT status1 with no exception is missing; transport/server failures
throw into OASISResult errors. Load and index reads share that helper. SetAsync now
requires true acknowledgement instead of ignoring a false result; successful holon
save sets IsSaved/SavedCount only after existing record/index operations succeed.

Cached JSON is plaintext. Memcached is volatile; no persistence/restart durability
is claimed. Existing shared index read-modify-write is not atomic across clients;
conditional writes, broader history/child semantics and production routing remain
open. Native miss/status1 behavior still needs actual server proof.

## Verified controls

Fifteen pass, zero failed/skipped,14s: nine CQL admission/capture/cancellation/alias
controls, five current provider metadata checks, and one real unavailable-endpoint
read/write check. Unavailable server cannot yield an empty successful CQL page or
saved record. The stale metadata test called a removed version method; it now
checks actual provider category without a shim.

- Tests: MemcachedCqlTests.cs and MemcachedOASISProviderTests.cs in MemcachedOASIS.UnitTests.
- Evidence: `TestResults/MemcachedCqlCurrent/memcached-cql-controls.trx`.
- SHA256: `E7020E460FD96AE236B6106F0535EAF071728E20E0A0DF4A21C30D948D3331BE`.
- EnyimMemcachedCore2.5.1 pinned package restored once (previously absent), cached
  at C:/Users/david/.nuget/packages/enyimmemcachedcore/2.5.1, measured883538bytes.
- Used package inventory: `TestResults/MemcachedCqlCurrent/packages-resource-inventory.json`;
  other transitive provenance is explicitly unverified. Existing runtime cache reused.

## Exact next step

Admit a bounded loopback-only native Memcached service in the reusable Linux
provider environment; inspect OS/runtime caches before installation. Verify actual
cache misses separately from transport failures, acknowledged saves/load, native
three/two/missing queries, paging/budget/tombstones and no query mutation. Add one
Mongo-paired C#/HTTP/browser preset using the retained Linux Mongo environment.
Define the cache's intentional ephemeral lifecycle explicitly: do not imply that
records survive process restart or silently recreate an uncertain source. Keep
native and paired inventory red until actual proof.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
