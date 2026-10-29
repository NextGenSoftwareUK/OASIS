# Upstash CQL MVP admission

Updated29 October2026. **Native hosted and paired demo admission remain red.**

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

## Resource admission and exact next step

A retained official scratch resource has metadata expiry2026-10-31T21:15:49.3541451Z
under C:/Users/david/AppData/Local/OASIS/ReusableProviderTests/Upstash. Only lifecycle
metadata/file names were inspected; credentials have not been decrypted or sent.
No new account/service/paid resource is created.

Before native writes, validate that this resource remains the same free scratch
service and has not expired. Reuse protected credentials only in awaited native
processes, never logs. Persist source intent/IDs before three synthetic saves;
validate bounded native3/2/missing/paging/budget responses and preserved original
values on repeat. Do not scan or remove shared data. Then add a Mongo-paired
C#/HTTP/browser preset. Preserve uncertain outcomes instead of reseeding; keep
inventory red until native and paired proof are complete.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
