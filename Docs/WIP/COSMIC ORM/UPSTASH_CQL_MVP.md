# Upstash CQL MVP admission

Updated29 October2026. **Hosted native/repeat verified; paired demo remains red.**

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
- Repeat SHA256: `C8AB9790BD5CD2A2E9CD2050B2C813DDF314F7D2670C2866297AE388379756D2`.
- Seed SHA256: `B24961E3DB2A04F6F772B3472581BEC281B9CDEF7B13EFE4CEF8F20770480B35`.
- Persistent source directory `LocalState/COSMIC/UpstashCql`: 763bytes,
  plus three hosted records/index entries (native size unmeasured), reusable until expiry.

## Exact next step

Add a Mongo-paired C#/HTTP/browser teaching preset using the same admitted hosted
source. Validate original endpoint/expiry and source hashes before each demo; never
reseed if it expires or disappears. Include provider provenance and unavailable
provider controls, then verify/drop only the owned temporary Mongo fixture. Keep
paired admission red until verified. Native JSON is plaintext; no encryption,
atomic save, hosted restart or full LINQ/ORM parity claim.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
