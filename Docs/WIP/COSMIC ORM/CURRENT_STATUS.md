# Current COSMIC ORM / CQL status

Updated30October2026 UTC. Full programme remains incomplete.

## Immediate phase: user-approved extra Development fix batch

- Existing backend checkout C:/Source/OASIS-cosmic, parent/Core work/cosmic-cql-sandbox-integration.
- Corefdca0ed, scoped parent47f7f2606 plus this checkpoint; broader parentwork/cosmic-cql f89cf0085/Core69bb8a4 preserved/pushed.
- Human approved one extra consolidated Development push today. Only receipt ownership, Mongo root-filter optimization, free write profilev2 and their tests/docs/scripts are included. New PostgreSQL/SQLServer/Oracle changes are excluded.
- Both remote Development refs fetched/inspected: parent89a960111, Core432c613. Normal fast-forward pushes must reject any concurrent drift; never force.
- All7 scoped producers/consumers rebuilt againstfdca0ed without restore.30API controls,8real native billing lifecycle and4native ancestor cases pass/no skips. Native owned fixtures removed/Mongo stopped.
- Railway dev WEB4 PREPARED_STORAGE_ADMISSION_JSON v2 staged with --skip-deploys; oldv1/current expected profile compared first, exact canonical readback verified. No duplicate deployment or unrelated setting change.

Exact next: push Corefdca0ed to Development, verify ls-remote; push this scoped parent/pin+manifest together, verify manifest and exact Railway deployment handle. Observe it to terminal, then run the hosted19-call fixture verifier against that exact SHA/deployment; reconcile failures without replay. Frontend main remains unpublished until real acceptance.

## Hosted verification and unresolved gates

- Current published89a960111/e88fbed5 deployment is SUCCESS. Its Mongo pipeline is40stages/full16levels, but new receipt/profile/root-filter fixes are not yet in that image.
- Receipt collision fix binds physical request IDs to admitted operation. Existing executed unsettled-query receipts remain immutable/unresolved; never replay provider execution to settle them.
- Free profilev2 enrolls canonical save/batch/delete/recovery across Mongo/SQLite/Neo4j, zero price/native fee with real quota/measurement guards. No paid/native-chain admission implied.
- Mongo root-local filters precede ancestor walks; complete final access predicate retained. Hosted timeout resolution still needs actual execution.
- Original save56eaea32 definitively rejected before manager acquisition. Original fixture and reconciliation retained in LocalState/COSMIC/HostedSandboxCqlCurrent/Archive; a new declared fixture can be created, no old-operation replay.
- Hosted verifier requires exact deployment/SHA and persists mutation intents outside TestResults. Six offline protocol tests pass; this is not live acceptance.
- Existing frontend C:/Source/Web4Site work/cosmic-cql c17e5f7 has four request-helper plus11CQL tests passing; main unpublished.

## Broader goal retained on work/cosmic-cql

- MVP breadth/content queries first; LINQ depth, POCO/migrations tooling paused/deferred. Work only on red cells; no green/cursor hardening without a reproduced bug. HyperDriveV2 only; legacy gaps documentation-only.
- PostgreSQL16controls+17native/restart and4paired SDK/HTTP/browser presets2/4/4/2 verified.
- SQLServer17controls+23native/restart and4paired SDK/HTTP/browser presets2/4/4/2 verified.
- Oracle13controls+15read-only native and21saved-holon queries/restart verified; content Mongo-paired/browser/sandbox still red. Four retained source records/ACKs unchanged; owned Oracle stopped.
- Deployed sandbox tenant authorization/admission for these new providers remains red. Hosted Azure explicitly pending/free-only.
- Resume broader task after the approved scoped deployment: Oracle content pairing, then remaining approved10-provider content/sandbox plan; full roadmap remains incomplete.
- [Scoped release](CQL_SANDBOX_RELEASE_2026-10-30.md), [checklist](CQL_PARITY_CHECKLIST.md), [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md). Broader current roadmap/proofs remain on preserved task branch.

## Recovery/resources

[Single cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). All logs/TRX under worktree TestResults; persistent source/keys/ACKs/pending journals outside it. Reuse existing SDK/cache/services; no new worktree/env/install/restore. Unknown root logs untouched.

Build through provider lease with /m:2 /nr:false /p:UseSharedCompilation=false. Push/verify Core before parent gitlink+manifest. Validate HEAD with python Scripts/validate_railway_dependency_manifest.py --require-gitlinks. Do not merge/publish broader task work merely to synchronize branches.
