# MongoDB metadata UUID identity preservation

2026-10-19. Scoped serializer fix, not full provider or HyperDrive V2 certification.

The metadata binary decoder used `new Guid(bytes)` for both BSON UUID-standard (subtype4) and UUID-legacy (subtype3). Standard UUIDs use network byte order; .NET's legacy GUID constructor changes the first three components. For example, `00112233-4455-6677-8899-aabbccddeeff` did not retain its identity.

The decoder now uses the installed official MongoDB driver's `BsonBinaryData.ToGuid(GuidRepresentation.Standard)` for subtype4. Subtype3 explicitly retains this provider's existing CSharpLegacy contract. It does **not** infer Python/Java legacy representations from subtype3, which does not identify them. The external metadata value remains a string, as before. Normal Guid writes remain strings; no record migration or bulk rewrite is performed. Generic binary handling is unchanged.

## Evidence

- New actual BSON serializer regression: standard/C#legacy, root/nested. Before fix2failed/2passed; report SHA256 `3933251A82B53A1C5762537A5AE43B8F10102ED0CFFF3837B52BB5A9FEE7CBA8`.
- Rebuilt producer0errors/18warnings. Full unit56/56passed/zero skips474ms; `TestResults/MongoCurrent/mongo-unit-current.trx` SHA256 `E155F89831BBFCB203C4A1D02A8A8CF6EAD5CF8F79F691ECE7279EA106D92878`.
- Same retained Mongo replica and existing native projection runner3/3passed/zero skips1s. Existing synchronous/asynchronous detail insert/update cases now inject actual standard and C#legacy BSON UUID metadata through the official driver, then verify exact identities via independent providers' GUID/username/email sync/async selectors. Native report `TestResults/SubscriptionUsageCurrent/mongo-projection-current.trx` SHA256 `10A13684557FCE93DA587A4D8C190457AF5E30C03E15226ABAD8931407A6DFA3`.
- Railway dependency/gitlink validation and whitespace check pass. Shared Development fetched0ahead/0behind before integration.

Missing project assets regenerated from existing local NuGet cache only; no network download, runtime installation, clone or duplicate environment. Cache restore warned test SDK18.0.1 unavailable locally and resolved already-cached18.7.0; this is recorded rather than called an exact pinned-package run. Existing owned Mongo stopped by runner;27029listener absent afterward. Retained shared Mongo data is not disposable and was not deleted. Scoped test bin/obj total about95MB and remain reusable. Observed free C:82,741,530,624bytes; no claim attributing the entire free-space change to these outputs or of reclaimed space.

Remaining: other BSON representations/types, all provider interfaces/options, broad native/hosted acceptance and actual V2 failover/replication/load balancing remain separate programme gates. Firestore's pending original checkpoint and unverified source are excluded from this batch.
