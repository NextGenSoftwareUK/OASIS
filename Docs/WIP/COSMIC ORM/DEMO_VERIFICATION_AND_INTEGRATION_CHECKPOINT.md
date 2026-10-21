# Demo verification and Development integration checkpoint — 21 October 2026

## Verified demo batch

All three C# demo sources, project file, README and local formatting configuration now use readable layout and developer-oriented guidance. The C# walkthrough labels seven examples; the HTTP host explains scope resolution, filter composition, ordering/paging, native capability planning, execution and result provenance. Host ownership/teardown is documented in Program.cs.

The existing runner rebuilt the SQLite producer and sample with bounded workers and serialized leases. Console checks and six real HTTP/Swagger checks passed; the host exited and its owned synthetic database/sidecars/manifest were removed. Producer/consumer identity checks passed for Core, Shared and SQLite. Formatting, build-lease contract and Railway dependency manifest/gitlink checks passed. Compact evidence is retained under TestResults/CosmicCqlDemo/readability-verification.log and its existing JSON reports.

## Development integration prerequisite

Read-only fetch at this checkpoint found origin/Development at 6425451c6f053f9d71f9ca041a5110f8e733567b and the task branch baseline at dac04278746edfb82e8848de90e05205e3c11f10. They diverge by 9 Development commits and 230 task commits, spanning 776 changed paths. A merge-tree preview is conflict-free, but that is not release validation.

The demo depends on CQL APIs absent from Development's pinned Core 2775936cd0e9fbc16550f260c349b7a02505f554 and SQLite CQL implementation absent from that branch. Copying only the demo would therefore produce a non-buildable integration. Advancing the entire shared branch would also include extensive unrelated provider checkpoints and a Core pin change to c4a89b193aff419d1e5cf0f9b979ff56f2f80f4e. The current demo test does not validate that full deployment cohort.

The repository instruction says: “Incomplete or unverified changes must not be swept into a deployment merely to synchronize branches.” Therefore this checkpoint does not claim or perform a full Development deployment. The user has authorized integration; no additional authorization is needed. Remaining technical work is to prepare a dependency-complete integration batch, validate its affected deployment projects and existing provider gates, verify dependency publication, then perform one normal Development push. Preserve the existing Development changes and unrelated dirty worktree throughout; do not force-push or create another environment just for integration.

Only this task's explicitly owned paths belong in the demo commit. The many unrelated tracked/untracked edits in the shared checkout, including provider-revision-migration-hang.json, remain with their existing owners.


## Authorized Development integration — 21 October 2026

The user reiterated authorization to merge the committed task branch into Development. The earlier paragraph records a pre-integration checkpoint, not a continuing approval requirement. Proceed with the clean committed-tree merge, preserving both Development history and all unrelated dirty checkout files. Use Git merge-tree/commit-tree so no shared checkout switch, stash, new worktree or environment is needed. Validate the actual merged manifest, seven Dockerfiles and gitlinks before one normal non-force Development push. Record exact parents, merged commit and remote readback in TestResults/CosmicCqlDemo/development-integration.json.

The console and six HTTP/Swagger checks passed for the demo batch. This integration carries previously committed provider/ORM checkpoints and their documented evidence; it does not claim the full provider programme, remaining roadmap or deployed Railway services are now certified complete. No production/master release is authorized by this Development merge.
