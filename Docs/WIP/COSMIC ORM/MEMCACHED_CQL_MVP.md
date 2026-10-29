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

## Native runtime admitted; query proof still pending

No native daemon/package/cache was installed. The official signed Ubuntu package
metadata identified1.6.40-1ubuntu0.3; its296,868byte DEB was downloaded once,
SHA256-verified and extracted without installing a system service. The extracted
binary was compared with the original archive and reports memcached1.6.40.
No native daemon has been started; no cache source/data exists yet.

- Runtime: `/var/tmp/oasis-runtimes/memcached-1.6.40`,791,732bytes including admission.
- Executable SHA256: `a1d63f0865bafd0fb1c64022f90fd9be5a58a095fa4856ef56d50aa20cabfb6e`.
- Archive SHA256: `1e5272002efa8674959ee9796356b89bb1075ccb59f12a429d1a72396b61f15a`.
- Verified installation archive removed after version/admission check.
- Missing libevent-2.1-7t64 2.1.12-stable-10ubuntu0.2 (383KiB installed) and
  liblua5.4-0 5.4.8-1build1 (570KiB installed) were added from Ubuntu; other OS
  libraries reused. No memcached package/global service/autostart installed.
- Setup: `Scripts/TestHosts/prepare-cosmic-memcached-linux.py`; normal rerun verifies
  and reuses the admitted binary. Explicit extracted-state finalization preserves
  and validates the original package rather than overlaying a partial setup.

## Exact next step and ephemeral cache contract

Implement one bounded native runner over the existing Linux environment/runtime,
loopback11239, UDPdisabled,32MiBmemory/one thread. Before writing, persist a canonical
synthetic seed plus an active process-generation intent and prove the owned cache
is initially empty. Verify acknowledged Save/Load for three live records and a
tombstone, real KEY_ENOENT vs transport failure, native3/2/missing queries,
paging/result budgets and unchanged native record bytes/CAS across queries.

Treat each owned daemon lifetime as an explicitly volatile generation. On verified
success, stop the exact owned daemon and record a clean retired generation; a later
explicit demo may warm the same canonical fixture into a new empty owned cache.
This is planned cache initialization, never persistence recovery. Unknown write or
teardown outcomes keep pending state and must not trigger automatic replay/reseed.
Keep original canonical IDs/receipts/baselines for audit, no cache restart durability
claim. Then add Mongo-paired C#/HTTP/browser using the existing Linux Mongo store,
without creating another runtime/environment or per-run database copy.
Native and paired inventory remain red until actual proof.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
