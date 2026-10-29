# Memcached CQL MVP admission

Updated29 October2026. **Scoped native volatile-cache and Mongo-paired C#/HTTP/browser MVP verified.**

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
open. Native status1 cache-miss behavior is now verified against the actual server.

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

## Native runtime admission

At admission no native daemon/package/cache was available. The official signed Ubuntu package
metadata identified1.6.40-1ubuntu0.3; its296,868byte DEB was downloaded once,
SHA256-verified and extracted without installing a system service. The extracted
binary was compared with the original archive and reports memcached1.6.40.
Native generations are now verified below; no daemon remains running, and retired
cache data is intentionally volatile.

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

## Verified native volatile generations

The actual pinned Memcached daemon runs on Linux loopback11239,UDPdisabled,
32MiBmemory/one thread. Runner verifies the exact process owns the listener and
native stats show an empty cache before any fixture writes; no scans/flush/adoption.
Canonical actor/three live IDs/tombstone ID remain in one persistent seed. Each
owned process has an active intent before writes and becomes a clean retired
generation only after the native proof and exact process shutdown succeed.

Four acknowledged saves and live Load round trips pass. Actual native missing-key
responses return empty query results, whereas unavailable-endpoint unit controls
return errors. CQL returns3/2/missing/tombstone results, applies paging, and rejects
an undersized result budget without partial rows. Independent bounded binary GETs
verify identical raw value hashes and CAS versions for all four record keys plus
the index before/after queries. Native stats show exactly five owned cache items.

The initial generation1passed752ms; an explicit subsequent empty generation using
the same retained canonical identities1passed274ms. Final-source runner verification
also passes. This proves intentional cache warmup/lifecycle, not persistence or
byte equality across process restart. Unknown write/teardown outcomes retain pending
state and refuse automatic replay. Known clean retirement intentionally loses the
volatile cache. No daemon or11239listener remains.

- Fixture: `MemcachedCqlNativeTests.cs` in MemcachedOASIS.IntegrationTests.
- Runner: `Scripts/TestHosts/run-cosmic-memcached-linux.py`.
- Initial evidence: `TestResults/MemcachedCqlCurrent/memcached-cql-initial.trx`.
- Initial SHA256: `FC19ECF876CF09F2B4B2A88608C38AF795A3FB1F1F50882071F9DA38E55E3B67`.
- Current evidence: `TestResults/MemcachedCqlCurrent/memcached-cql-native.trx`.
- Current SHA256: `7416A6D2B91FD1F0238271F9A8628F7ECF1D7B63217160ED436D45EB9CA4943F`.
- Canonical seed/last generation/CAS receipt/PID receipt: `/var/tmp/oasis-memcached-cql`,
  measured1,689bytes after second verification; reusable, not cache persistence.

## Mongo-paired teaching demo

One admitted volatile generation stays active through native checks and the paired
C#/HTTP/browser demo. The existing Linux Mongo runtime/store is reused. Results
preserve provider-labelled copies: five physical rows, then four for the two-ID
subset. Invalid actor/preset reject; an inactive cache adapter returns unavailable
without partial results. Independent native GETs before/after the entire demo
verify identical values/CAS for all four cache records and their index.

- Fixture: MemcachedMongoCqlNativeTests.cs; shared existing owned Linux replica
  admission test linked unchanged into the integration consumer.
- Browser run1passed24s; launcher automated repeat1passed2s; zero failures/skips.
- Current evidence: `TestResults/MemcachedCqlCurrent/memcached-mongo-federation.trx`.
- Paired SHA256: `95C0693D4E275F27C36ED2EE7469311CE9357C36A92907941D4C971025492F23`.
- Browser: `TestResults/MemcachedCqlCurrent/memcached-mongo-browser.jpg`.
- Hub:8single/16plans,26paired/52plans,38paired rejections,3archive rejections pass;
  listeners unchanged by planning tests.
- Owned Mongo database cosmic_cql_mongo_memcached_demo verified/dropped. Native
  cache generation retired only after paired proof and owned service teardown.
- Runner62897 and corrected launcher45553 exit0; no cache/Mongo/HTTP listener
  remains. Browser tab closed; canonical fixture/retirement receipts retained.
- An initial launcher check selected a nonexistent runner filename and stopped
  before service startup; fixed to the existing runner and reran the actual preset.

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Memcached -Federated -ShowPlan
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Memcached -Federated
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Memcached -Federated -VerifyOnly
```

Stop using the page button to verify fixture cleanup and retire the volatile cache.
An explicit later demo initializes a new empty owned generation from the same
canonical identities; this is not persistence recovery. Pending/unknown outcomes
refuse replay. No new runtime/data-store environment was created for the pair.

## Exact next step

Continue the next red provider MVP; inspect Valkey implementation and native
resources without inheriting Redis certification. Preserve this green scope and
explicit volatile lifecycle. Shared-index atomicity, conditional writes, broader
history/children and complete ORM/production acceptance remain open.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
