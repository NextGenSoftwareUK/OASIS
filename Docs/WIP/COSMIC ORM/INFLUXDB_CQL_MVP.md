# InfluxDB CQL MVP — controls verified; native/paired demo pending

Updated 30 October 2026. Inventory remains red. Target is InfluxDB2.x Flux through
the existing pinned InfluxDB.Client5.1.0 SDK; no InfluxDB3 compatibility claim.

## Implemented scope

InfluxDBOASIS implements ICosmicQueryStorageProvider bound to the shared bounded
identity compiler. Trusted complete catalogue at most100GUIDs; identity equality,
membership/Boolean composition and stable paging. Other properties/metadata/order/
child graphs reject before native reads. No managed whole-store filtering fallback.

Native Flux constrains measurement, ID tags and required fields before pivoting,
groups by ID, finds maximum timestamp, then joins heads back to original rows.
This retains tied heads so duplicate validation rejects ambiguity rather than
choosing an arbitrary payload. Range covers provider-generated post-Unix-epoch UTC
points through native maximum time, including future clock skew. Native Flux
execution/POCO mapping/selector compatibility still require backend verification.

QueryAsyncEnumerable streams native DTOs through one validation/materialization
boundary. Missing fields, native/payload identity differences, foreign/duplicate
records and authority mismatches fail without partial rows. Cumulative4MiB decoded
payload budget stops enumeration before another row. Linked15s deadline and caller
cancellation propagate to the SDK. SDK wire-byte bounding remains unverified.

Flux strings use strict UTF8 hex literals, preserving quotes/backslashes/Unicode/
control characters and preventing `${...}` interpolation. The same encoder fixes
existing lookup escaping and previously raw bucket interpolation at their common
boundary. This is Flux literal syntax, not JSON string escaping.

SaveHolon stages ID/copied provider keys/JSON until WritePointAsync acknowledges;
only then publishes caller ID/keys and IsSaved/SavedCount. Failed/incomplete writes
leave caller identity unchanged. Batch stop/continue/error counts are honored.
HolonManager retains tracking-acceptance ownership. Activation now publishes active
state after bucket readiness and clears stale state before attempting activation;
deactivation clears state before client disposal. The unimplemented history flag
explicitly rejects Holon saves/CQL before native IO; other legacy paths remain gaps.

## Proving controls

34passed/0failed/0skipped: metadata4, compiler/admission9, streamingquery10,
saveownership6, Flux boundaries5. Provider/unit builds and final cache-only restores
pass. Tests cover unsupported shape/authority/budgets, stable paging, corruption,
duplicates/tied heads represented as duplicate native rows, incomplete native fields,
terminal error/no partial output, empty/no IO/missing, early decoded budget stop,
active cancellation, failed new/existing identity, staged payload equality,
unsupported history and batch counts. Flux controls preserve UTF8 and reject invalid
UTF16; native query shape retains tied heads. No backend used by these controls.

[Query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.UnitTests/InfluxDBNativeQueryBoundaryTests.cs),
[save tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.UnitTests/InfluxDBSaveOwnershipTests.cs),
[Flux tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.UnitTests/InfluxDbFluxBoundaryTests.cs).
Raw TestResults/InfluxDbCqlCurrent/controls.trx SHA256
`c8371f839278a415de45e2c166484137096f8a5bf5348d7c0be4f41aa1ad2756`.
Stale EnumValue assertion fixed; nonexistent GetProviderVersion test removed rather
than adding a fake production API.

## Exact next step and remaining gaps

No cached native InfluxDB image found. Choose one official free2.x release, journal
runtime/authentication/source before creation. Preserve original org/bucket/native
identity before business writes. Verify actual Flux join/stream mapping/escaping,
native3/filter2/missing0/page1/budget and unchanged JSON/nanosecond timestamps across
real restart. Then Mongo-paired C#/HTTP/browser5/4 and launcher tests. No seed replay,
replacement baseline or recreation after unknown writes.

Append timestamps/tag-set changes do not implement atomic conditional writes,
version-control policy or full history/children/replication/migration guarantees.
Retention settings can expire points; explicitly verify the demo bucket policy.
Broader existing avatar/lookup/delete/history paths remain a separate audit scope.
No hosted-cloud, distributed snapshot or full EF/NHibernate parity claim.
[Official Flux string syntax](https://docs.influxdata.com/flux/v0/data-types/basic/string/).
[Pinned SDK streaming API](https://github.com/influxdata/influxdb-client-csharp/blob/v5.1.0/Client/QueryApi.cs).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
