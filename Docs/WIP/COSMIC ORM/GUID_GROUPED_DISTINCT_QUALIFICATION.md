# Normalized GUID distinct-count qualification

19 November 2026. Source prepared; not yet qualified or deployed.

Use logical GUID identity for DISTINCT. MongoDB reuses ComparableGuid (case-insensitive text and correct Standard/CSharpLegacy binary byte order); SQLite uses NOCASE only for Guid selected counts while strings retain BINARY; Neo4j lowercases GUID equality values. Provider admission adds Guid and Guid? independently of still-unenrolled date/Decimal/UInt64.

Three new native cases run five checks per store: mixed Guid objects/lowercase/uppercase text; unique canonical IDs; required GUIDs; all-missing/null; invalid value rejection before an empty page. Mongo adds two owned raw metadata updates explicitly verifying Standard and CSharpLegacy bytes/subtypes and their deduplication. Ten owned logical records in Mongo, eight each SQLite/Neo, all under the existing exact-cleanup fixture. No shared Atlas, PIPER or local database writes; native proof and API/client qualification are still pending. No full ORM parity credit.
