# HyperDrive provider work

Transient provider reports live here. Begin with the current status; do not load chronological archives during routine recovery.

## Start here

- [CURRENT_STATUS.md — phase, next action and commands](CURRENT_STATUS.md)
- [PROVIDER_MATRIX.md — per-provider baseline inventory](PROVIDER_MATRIX.md)
- [Current release gates and pending inputs](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md)
- [Current environment inventory](ENVIRONMENT_INVENTORY.md)
- [Current cleanup ledger](WORKSPACE_CLEANUP_LEDGER.md)
- [Current I/O verification scope](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md)
- [Evidence paths/hashes and relocation limits](EVIDENCE_LOCATIONS.md)

Open a provider-specific audit only when working on that provider. The default status/summaries/README recovery set is under 300 lines; the matrix is an on-demand lookup.

Keep CURRENT_STATUS under 100 lines and each current summary under 150. Update status at each milestone; retain at most ten recent entries and roll older entries verbatim into Archive. New raw evidence belongs in TestResults, not docs. Source review/registry metadata and hashes do not constitute full implementation or live certification.

[Archive index](Archive/ARCHIVE_INDEX.md) preserves exact historical hashes, including the old README and superseded handoffs. Read archives only for a specific historical question; they are not resume instructions.
