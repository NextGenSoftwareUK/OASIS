# Normalized GUID distinct-count qualification

19 November 2026. Three-provider native qualification accepted; API/client and deployment acceptance pending.

Use logical GUID identity for DISTINCT. MongoDB reuses ComparableGuid (case-insensitive text and correct Standard/CSharpLegacy binary byte order); SQLite uses NOCASE only for Guid selected counts while strings retain BINARY; Neo4j lowercases GUID equality values. Provider admission adds Guid and Guid? independently of still-unenrolled date/Decimal/UInt64.

Three new native cases run five checks per store: mixed Guid objects/lowercase/uppercase text; unique canonical IDs; required GUIDs; all-missing/null; invalid value rejection before an empty page. Mongo adds two owned raw metadata updates explicitly verifying Standard and CSharpLegacy bytes/subtypes and their deduplication. Ten owned logical records in Mongo, eight each SQLite/Neo, all under the existing exact-cleanup fixture. No shared Atlas, PIPER or local database writes; native proof and API/client qualification are still pending. No full ORM parity credit.


## Accepted native evidence

Run44637895235/job135961086965 succeeded on exact870c98eb145618867e0e639a467ae276ab9c9596 (runtime source019ab0bc, timingsc504e629). Strict artifact14586015283,2712bytes,SHA256ad9c7426f18ba25e24f86ac251cd85d50761f221a7ae6137777cc5f9a4cf4d5d contains exactly one TRX/three source-generated reports:3passed/0failed/0skipped,5checks/store,26owned logical records cleaned,2Mongo raw binary updates proven. Mongo/Neo CI containers removed. No previous accepted cases selected.

Query times ms (mixedGuid/canonicalGuid/requiredGuid/missingNull/invalidOffPage): Mongo59/10/7/6/8; SQLite24/2/2/1/2; Neo782/382/343/16/437. These are isolated native samples, not production performance. Standard and CSharpLegacy bytes/subtypes were independently verified and count as the same Guid as its text/object forms.

Native acceptance does not prove WEB4 HTTP, stored-plus-virtual GUID grouping, SQL editor or hosted browser acceptance. Those remain next; date/Decimal/UInt64 distinct domains and full ORM parity remain open. No data backfill or DNA change is part of this source.
