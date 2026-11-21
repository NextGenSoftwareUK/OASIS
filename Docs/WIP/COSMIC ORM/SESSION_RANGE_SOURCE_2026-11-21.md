# Session range registration — 21 November 2026

AddRange and AttachRange share RegisterCore with single-root registration. They perform no native I/O and do not flush; SaveChanges remains the explicit acknowledged write owner. Attach captures the existing root baseline; Add marks a new root.

One call accepts at most 10,000 roots. Enumeration runs under the session operation guard, so reentrant session operations are refused. On an item/enumeration failure, earlier accepted roots remain tracked and are returned in the immutable Result list; this is explicit partial local registration, not an atomic database transaction. Inspect/detach those roots before retrying.

Source published only. The focused strict-provider contract test, compilation and representative native save qualification remain pending. No full framework parity credit or live acceptance. The frozen snapshot demo image remains pinned to Core13bd050 and excludes this unqualified SDK increment.

Cleanup: no local build, runtime, database, container or downloaded artifact; the scoped temporary Git index is removed by the publisher. Existing active demo handoff journals retained.

Qualification uses the existing session-entry workflow with the new range contract plus the three entry compatibility checks, because both now share the refactored registration owner. Actual WEB4 compilation follows in the same bounded CI build lease. No local build or shared Atlas operation.
