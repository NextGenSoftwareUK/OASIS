# HyperDrive and provider working reports

This folder contains this workstream's transient reports, audits, evidence inventories and handoffs. It is not the published product documentation and does not certify completion. Keep new reports here instead of adding them to `Docs/`, `Docs/Devs/` or the top level of `Docs/WIP/`.

## Start here

- [Current release gates and pending user actions](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md)
- [Provider implementation audit](PROVIDER_IMPLEMENTATION_AUDIT_2026-10.md)
- [Provider I/O evidence and gaps](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md)
- [Oracle client priority](ORACLE_CLIENT_PRIORITY.md)
- [Firestore implementation and native evidence](FIRESTORE_RECORD_HISTORY.md)
- [Native fees, rebates and recovery](HYPERDRIVE_V2_NATIVE_FEE_ACCOUNTING.md)
- [Encryption comparison](OASIS_ENCRYPTION_ARCHITECTURE_COMPARISON.md)
- [Environment reuse and cleanup ledger](WORKSPACE_CLEANUP_LEDGER.md)
- [Measured existing environment/runtime inventory](ENVIRONMENT_INVENTORY.md)

Other files hold provider-specific histories, retained-runtime evidence and generated API manifests. Reports contain chronological entries; use their latest scoped evidence and outstanding gates, not older claims or the presence of a test project, to assess readiness. Oracle was promoted to top client priority on2026-10-19. No provider is certified100% across all interfaces, hosted deployment and HyperDrive V2 guarantees.

Organisation2026-10-19: moved34related reports/manifests here from `Docs/WIP/`, preserving every file's SHA256 during the move. Updated explicit references and manifest-verifier paths to the new location. Unrelated release, Our World, subscription and other WIP documents remain outside this folder. Git history retains former locations; no report or required evidence was deleted and no duplicate copies were created.

Reference checks: index links resolve and the registered-provider documentation inventory finds all29entries. The manager-method and WEB4-route scanners successfully read their moved manifests but report existing unreviewed source additions (eight manager methods/five actions); the manifests were not regenerated to hide those review gaps. These inventory failures are not missing-file errors or provider completion proof.
