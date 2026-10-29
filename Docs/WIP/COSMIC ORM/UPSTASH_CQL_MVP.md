# Upstash CQL MVP admission

Updated29 October2026. **Scoped hosted native/repeat and Mongo-paired C#/HTTP/browser MVP verified.**

## Implemented scope

An explicit authoritative catalogue of at most100 UUIDs compiles to one native
MGET against the exact holon:{GUID-D} keys used by the existing save/load methods.
No keyspace scan or managed business predicate is substituted. Missing/deleted
records are removed before bounded-page offset/limit. Response positions/count,
record identity and authority are validated; malformed/error responses publish no
partial page. Aggregate decoded payload budget4MiB is checked after allocation,
not a pre-allocation bound. Cancellation is propagated into HTTP send/body read.
Providers are observed separately; no distributed snapshot claim.

The existing shared REST helper now disposes requests/responses and rejects Redis
command errors even when HTTP status is200. Activation requires PONG and publishes
lifecycle state; failed activation/deactivation clear it. Save/load and CQL share
the existing JSON codec and transport; no alternate storage path is introduced.
Stored JSON in this provider is plaintext: a separate retained storage-key file is
not evidence that this provider encrypts records.

## Verified controls

Seventeen pass, zero failed/skipped,244ms: nine CQL controls, five current provider
metadata tests and three loopback activation outcomes (PONG, command error, null).
Existing stale tests referenced a removed version method and compared an enum
wrapper directly; these now test the current category/value contract.

- Tests: UpstashCqlTests.cs and UpstashOASISUnitTests.cs in UpstashOASIS.UnitTests.
- Evidence: `TestResults/UpstashCqlCurrent/upstash-cql-controls.trx`.
- SHA256: `9A32920872FCE08A1098F61D4ED0BCE8FFF08DC373C44804C0CD65F5BA705017`.

## Hosted native verification

The retained official free scratch resource was reused without provisioning. The
runner checks expiry2026-10-31T21:15:49.3541451Z before credential use and restricts
the protected identity to the original HTTPS Upstash service. Credentials are
unprotected only in-process, passed to the awaited test via environment, restored
and excluded from logs. No account, purchase or infrastructure installation occurred.

Three acknowledged synthetic saves and load round trips pass. Bounded native MGET
returns3/2/missing rows; paging and result-budget rejection pass with no partial
results. Native GET value hashes and membership of the three owned IDs in the
existing holons:all index survive fresh clients/processes unchanged. No keyspace
scan, shared-index replacement or unrelated-key deletion occurs. This is fresh
client repeat verification, not a hosted service restart or durability/failover test.

SaveHolonAsync now reports IsSaved=true/SavedCount=1 after its existing native
record/index operations succeed. Native assertions prove those acknowledgements;
the multi-command save is not certified atomic and wider concurrency remains open.
The old integration file had an incorrect Holon/Avatar namespace import, now fixed.

- Test: `UpstashCqlNativeTests.cs` in UpstashOASIS.IntegrationTests.
- Runner: `Scripts/TestHosts/Run-CosmicUpstashCqlNative.ps1`.
- Initial result1passed2s/no skips; `TestResults/UpstashCqlCurrent/upstash-cql-initial.trx`.
- Initial SHA256: `EF1B978C4A1EA0E28FC6489811C59E07B94088582B859F566616C0B444C842BE`.
- Repeat result1passed1s/no skips; `TestResults/UpstashCqlCurrent/upstash-cql-native.trx`.
- Latest native/repeat preflight SHA256: `4AB29E843712870030AF63AE7CAFC15D330B92D15EA21CF47B55F1C991A6F19B`.
- Seed SHA256: `B24961E3DB2A04F6F772B3472581BEC281B9CDEF7B13EFE4CEF8F20770480B35`.
- Persistent source directory `LocalState/COSMIC/UpstashCql`: 763bytes,
  plus three hosted records/index entries (native size unmeasured), reusable until expiry.

## Hosted Mongo-paired teaching demo

Shared C#, HTTP and browser queries return five physical rows (hosted Upstash3 +
local Mongo2), then four for the two-ID subset. Provider copies remain labelled.
Forged actor/unknown preset reject, and an inactive Upstash target returns unavailable
without partial rows. Only the owned temporary Mongo database is removed afterward;
the original hosted source/credentials remain unchanged.

- Fixture: `UpstashMongoCqlNativeTests.cs` in UpstashOASIS.IntegrationTests.
- Result: one passed24s, zero failed/skipped.
- Evidence: `TestResults/UpstashCqlCurrent/upstash-mongo-federation.trx`.
- SHA256: `75B64517C4928A4E37358A406018AC1FB7386828BD8ED2CB80859309468187E2`.
- Browser: `TestResults/UpstashCqlCurrent/upstash-mongo-browser.jpg`.
- Runner26758 exit0; owned Mongo/HTTP listeners absent; ownership journal removed;
  browser tab closed. Protected credential environment restored.
- Hub: 8 single/16plans,24 paired/48plans,34 paired rejections and3 archive
  rejections pass; listeners unchanged.

```powershell
# Inspect without services/credentials.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Upstash -Federated -ShowPlan

# SDK/HTTP checks, followed by loopback5115 browser demo.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Upstash -Federated

# Automatic verification and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Upstash -Federated -VerifyOnly
```

Stop using the page button to finish temporary Mongo cleanup. **This retained free
scratch expires2026-10-31T21:15:49.3541451Z**; the runner refuses expired admission.
Missing/expired original source requires explicit reconciliation, never automatic
reseed or allocation. A permanently available hosted demo needs separately admitted
free infrastructure. Canonical JSON remains plaintext and multi-command saves are
not certified atomic; arbitrary predicates/full ORM parity remain open.

## Exact next step

Continue missing provider MVPs from the inventory. Preserve this scope and source
until expiry; do not harden green rows absent a reproduced bug. Broader correctness,
replication, migration and parity acceptance remain unchanged.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
