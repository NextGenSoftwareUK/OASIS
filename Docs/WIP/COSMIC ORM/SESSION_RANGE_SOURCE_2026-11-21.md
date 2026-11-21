# Session range registration — 21 November 2026

AddRange and AttachRange share RegisterCore with single-root registration. They perform no native I/O and do not flush; SaveChanges remains the explicit acknowledged write owner. Attach captures the existing root baseline; Add marks a new root.

One call accepts at most 10,000 roots. Enumeration runs under the session operation guard, so reentrant session operations are refused. On an item/enumeration failure, earlier accepted roots remain tracked and are returned in the immutable Result list; this is explicit partial local registration, not an atomic database transaction. Inspect/detach those roots before retrying.

Source published only. The focused strict-provider contract test, compilation and representative native save qualification remain pending. No full framework parity credit or live acceptance. The frozen snapshot demo image remains pinned to Core13bd050 and excludes this unqualified SDK increment.

Cleanup: no local build, runtime, database, container or downloaded artifact; the scoped temporary Git index is removed by the publisher. Existing active demo handoff journals retained.

Qualification uses the existing session-entry workflow with the new range contract plus the three entry compatibility checks, because both now share the refactored registration owner. Actual WEB4 compilation follows in the same bounded CI build lease. No local build or shared Atlas operation.

## Focused contract and WEB4 compile accepted

Run44945637242/job136972218756 on e2161fb6 succeeded: one new range rule/partial-result/reentrancy/no-I/O test plus three entry compatibility tests,4passed/0failed/0skipped. Actual WEB4 compiled0errors5m49.13s. Artifact14723827093 is1808bytes, SHA2569b59cd0d788ed61d7ee977e190932265efea0a1cf765f4b3a63c02ff2dfcd77c; exact entry.trx and all four names/outcomes verified in RAM. Source Core3b69eedb. Earlier source-only status is superseded by this scoped contract/build acceptance; native save acceptance and full framework parity remain open.

Native representative qualification prepared: three cases only, selecting real MongoDB/SQLite/Neo4j, two private roots, absence before SaveChanges, explicit create, AttachRange baseline/no-op, one update and foreign-private read exclusion. Existing fixture owns exact DB/work/container cleanup and writes evidence only after cleanup. Isolated CI Mongo/Neo; no shared Atlas/live operations. Native results pending; frozen timeout image excludes range Core.
