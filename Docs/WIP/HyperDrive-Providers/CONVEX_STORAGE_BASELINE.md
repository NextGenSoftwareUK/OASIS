# Convex native storage baseline

28October2026. Shipping ConvexOASIS passes canonical core-storage-v2:26async+26sync against the official retained native backend, not a fake HTTP server. Hosted cloud/IAM, all relevant interfaces, concurrency/version stress and global HyperDrive behavior remain open.

## Implementation and verification

- Replaced duplicated CRUD/actions with KeyValueStorageProviderBase, persisted private encryption and a thin Functions HTTP query/mutation transport. Bundled Companion/convex/schema.ts and oasis/records.ts now supply actual internal functions, indexed storage and native transactional revision predicates. Default soft holon deletion and existing constructor shapes remain.
- Native admin protocol query succeeded; an anonymous call returned a function error. Keys stay outside source. Credential/token selection rejects archived versions before transport; historical record reads remain independent.
- Native core52 passes with independent native-table emptiness before/after each case. Conditional controls reject duplicate create, stale replace/delete and missing replace/delete. Native105-row Unicode-prefix query spans the100-row page boundary, then deletes only its observed fixture revisions; final table0rows.
-24controlled unit cases pass. HTTP/function errors never become empty collections or successful writes; uncertain mutation acknowledgements require reconciliation. Metadata factory now uses explicit synthetic configuration rather than trying constructors with an invalid literal URL.
- Core/Shared consumed hashes match producers and851/56portable-PDB source documents. Owned provider/consumer source documents and provider DLL equality checked. Local Core includes another owner's working changes: this is local source-matched proof, not an immutable release/full-program certification.
- Existing TypeScript5.5.4 compiler checks the companion against official SDK1.48.0. A45,554B package was verified to contain all seven companion files byte-for-byte at the intended paths; no node_modules/local environment file. Disposable package removed after verification.

## Preserved checkpoint failure

A verification run after the token-admission guard hit Windows AccessDenied while renaming an atomic report checkpoint before parent-query;13checks had passed and seven native fixture records remained. Ordinary attributes/ACLs were observed; no persistent locking process/cause was identified. No retries or fallback were added to the atomic publisher.

Original report/pending bytes remain in ProviderConformanceCurrent/ConvexOASIS/async/report-publication-denied{,.next}.json. Inspection decrypted logical identities, matched exactly seven expected fixture keys to native revisions/cipher hashes, then conditionally deleted only those unchanged records. Native table0confirmed. A500-publication disposable metadata probe passed, followed by a fresh52-check native run. Recovery is fixture maintenance, not part of API deletion certification.

## Environment and commands

Reuse /root/oasis-runtimes/convex: official precompiled-2026-10-22-1447815 binary213,980,664B; whole runtime215,216,349B including state628,958B and protected log605,847B at measurement. ZIP64,331,338B verified against published SHA2567be627d8a8c9175b66a55cf6c4ab63c5f6bde1c412f59fbbe93f64a24a11bd13, then removed. Binary SHA256044eedfe569b1550d5003683aec7fceb4dcee3f82abf00ab9baf1c10d3ca0b4c. No image/dashboard/frontend/clone installed.

One NodeTools SDK installation at C:/Users/david/AppData/Local/OASIS/ReusableProviderTests/NodeTools totals53,750,184B: convex1.48.0, esbuild0.27.0/@esbuild-win32-x640.27.0, prettier3.9.10 and ws8.21.0. Companion node_modules is a junction to that installation, not another copy. Reused compiler: C:/Source/OASIS/oasisweb4.com/node_modules/typescript/lib/tsc.js. Shared npm cache contribution is unmeasured; do not broadly clear it.

Runtime is stopped after verification; loopback13210/13211, owner PID/start-time recorded by Scripts/TestHosts/convex_runtime.py. Preserve /root/oasis-runtimes/convex/credentials/{instance-secret,admin-key} (0600) and CurrentUser-DPAPI storage-key.dpapi under LocalAppData/OASIS/ReusableProviderTests/Convex. No secret values in reports. Restart the same runtime with the helper, never reset its state or replace keys.

Scripts/Providers/Run-ConvexStorageConformance.ps1 holds one producer/consumer build lease, bounded cached builds and source checks. It refuses old reports by default; InterfaceOnly, VerifyPaginationOnly and explicit verified-interface/recovery modes are documented in its parameters. Preserve original receipts before any repeat. Native suite opt-in and protected env variables are set only for the child run and removed afterward.

Evidence: TestResults/HyperDrive-Providers/NativeEvidence/conformance-convexoasis-current.json and conformance-convex-controls-current.json; identities, package verification and exact measured output/SDK paths in TestResults/HyperDrive-Providers/ConvexCurrent. Protected state and failure evidence are retained; generated bin/obj can be rebuilt from cached producers after owned handles stop. Retire runtime/state only after exact ownership, fixture/evidence and credential review.

Protocol references: [official HTTP API](https://docs.convex.dev/http-api/), [internal functions](https://docs.convex.dev/functions/internal-functions), [official pinned backend release](https://github.com/convex/convex-backend/releases/tag/precompiled-2026-10-22-1447815), [running the binary](https://github.com/convex/convex-backend/blob/main/self-hosted/advanced/running_binary_directly.md). No hosted certification follows from the local-engine baseline.
