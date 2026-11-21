# Mapped DateTime keys: native acceptance — 21 November 2026

MongoDB, SQLite and Neo4j passed the three provider cases in [run 44890869474](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44890869474). The strict in-memory artifact inspection verified all twelve assertions per provider, zero failures and zero skipped cases.

The fixture checks native UTC timestamps one tick apart, the null group, distinct keys, identity reconciliation before final paging, rejection of another owner's request, an unchanged native source, no materialization of native-only rows, and exact cleanup of fixture records. Each provider performs five scoped checks. No archive was saved locally, and no shared Atlas or PIPER records were used.

Artifact 14702812086: 3,029 bytes; SHA-256 2e452d267c07c5195465b5293ce1344c91ecaba99d1f792bed88abdaa0f7d3c8.

This accepts the native fixture scope. Hosted acceptance, public website publication and full EF/NHibernate parity remain open.
