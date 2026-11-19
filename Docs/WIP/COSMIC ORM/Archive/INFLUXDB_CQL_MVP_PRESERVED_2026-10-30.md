> Superseded by ../INFLUXDB_CQL_MVP.md for current integration; retained historical Flux/native point proof and limits.

# InfluxDB CQL MVP — controls/native/restart/paired/browser verified

Updated 30 October 2026. Green means the bounded identity demo scope only. Target is InfluxDB2.x Flux through
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
execution/POCO mapping/selector compatibility pass against the retained server.

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

Continue red providers, next Qdrant capability/native-source audit. Preserve this
scope; do not deepen green rows unless a reproducible bug is found.

## Native and paired acceptance

Official free OSS2.7.12 runs authenticated HTTPS/private CA on loopback18086.
Original orgd17ca32a3773df54/bucket11032ffc911af144/infinite retention preserved.
Six original points: three normal, one future head, two tied-head controls. Three
logical admitted records are returned; ambiguous control ID is outside demo presets
and explicitly rejects when queried. Future Espresso proves future-clock visibility.
UTF8/`${...}` native echo, latest join/POCO mapping, native3/filter2/missing0/page1/
budget and unchanged JSON/nanosecond timestamps across actual restart all pass.

Launch `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider InfluxDB -Federated`
from the task worktree; add `-VerifyOnly` for bounded checks. Browser5144. The C#
example `Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs` uses HolonManager's
actual executor and explicit per-provider read catalogues. HTTP/browser all returns
InfluxDB3+Mongo2=5physical result rows; subset2+2=4. Source points remain six.
Copies retain provider provenance. Forged actor/unknown preset400 and inactive
provider503/no partial rows pass.

Bounded and interactive runners each passed five phases: native-first,
native-restart, mongo-init, mongo-federation and native-postpair. Browser all5/subset4
verified. Hub8single/16plans,53paired/106plans,92pairedrejections,3archive pass,
listeners unchanged. Exact temporary cosmic_cql_mongo_influxdb_demo verified/dropped,
pending/ready cleared; own native/Mongo/demo services stopped, nativeexit0/noOOM/noError.
Original source /var/tmp/oasis-influxdb-cql33,764,955B logical retained with privateCA,
TLSkey/password/token/nativebolt/sqlite/engine/actor3IDs/controlID/receipts/baseline.
Raw interactive evidence in TestResults/InfluxDbCqlCurrent (not committed):
- native-first.trx SHA256 `58e4e235ef811b2d18ee6ca64dc8f489c7139208b600b8cc9f9a5ef11d777ff8`
- native-restart.trx SHA256 `9c6f44dfec8e5e15ba710dfe9675e925e4a853718d60ef0a79f655454a8f39b6`
- mongo-init.trx SHA256 `7f0cb5ead01a47b0110880f5d7fb74e90ed38290603ff1805b629d08849ece9d`
- mongo-federation.trx SHA256 `c9f320955e68a3dcd1ff3633a9b0a6862190bccd207769af6216dd53e57e4daf`
- native-postpair.trx SHA256 `40a9160008672309414771eec600ad5b272f538cc33cf478e6c0ba0268fcfedf`
Browser screenshot browser.jpg158,093B, runner57238terminal0. Native source and
credentials remain outside TestResults; only raw logs/TRX/images are regenerable.

Append timestamps/tag-set changes do not implement atomic conditional writes,
version-control policy or full history/children/replication/migration guarantees.
Retention settings can expire points; explicitly verify the demo bucket policy.
Broader existing avatar/lookup/delete/history paths remain a separate audit scope.
No hosted-cloud, distributed snapshot or full EF/NHibernate parity claim.
[Official Flux string syntax](https://docs.influxdata.com/flux/v0/data-types/basic/string/).
[Pinned SDK streaming API](https://github.com/influxdata/influxdb-client-csharp/blob/v5.1.0/Client/QueryApi.cs).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Initial setup used container-default SIGTERM; native2.7.12 registers SIGINT instead,
so it exited2 abruptly after acknowledged setup, before any business writes. Corrected
runner sends SIGINT and waits for nativeexit0 without escalation. This Podman lacks
stop--signal (that attempt failed without stopping); explicit signal is sent through
Podman's kill operation. Original org/bucket/setup receipts were read back, no setup
replay/container/source replacement. Native signal source: [v2.7.12 handler](https://github.com/influxdata/influxdb/blob/v2.7.12/kit/signals/context.go).
