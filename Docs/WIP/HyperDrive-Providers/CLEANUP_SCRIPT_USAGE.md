# Cleanup script — 18 November 2026

Use the existing Windows launcher from the repository root:

```powershell
.\Scripts\tools\cleanup-emergency-space.bat -DryRun
.\Scripts\tools\cleanup-emergency-space.bat
```

The launcher respects the configured execution policy instead of forcing Bypass. The common build-lease implementation uses APIs available in Windows PowerShell5.1 and PowerShell7, allowing both shells to use the same lock. The launcher does not change execution-policy settings. The historical October checks below are not acceptance of the November guard changes; current qualification belongs in the cleanup ledger.

Default cleanup now includes discovered MSBuild `bin`/`obj` folders in the main OASIS checkout and the registered old HyperDrive checkout, reviewed Unity validation caches/builds, native-game/package verification output copies, signed VS installer extraction folders, the reviewed VS package-download staging folder, signed VSCode staging and old installer diagnostic logs. Source, package/runtime caches, current native test evidence and credentials are retained.

Active project/output ownership and exact artifact candidates are in [cleanup-emergency-space.targets.psd1](../../../Scripts/tools/cleanup-emergency-space.targets.psd1). The active-chat snapshot protects Core/Common/Contracts/DNA/BootLoader/ONODE, current Mongo/SQLite/Neo4j and AWS/Dynamo projects, consumers and the shared storage layer. Update it when ownership changes; extra exclusions can be passed with `-PreservePath` and `-ActiveWorktreePath`.

The global producer/consumer lease and process checks prevent cleanup during a build/test slot. The script does not stop builds or services to force removal. Close/finish builds before running it again if generated-output cleanup was skipped. Referenced runtime junctions, tracked source, native databases, pending journals, secrets and open files are refused. Unity Library `.db` files are a narrow regenerable-cache exception, not permission to delete provider databases.

Retirement requires exact reviewed inactive paths through `-WorktreePath`. Clean/merged history alone does not establish that another agent has finished using a checkout. Registered active worktrees, unique commits, uncommitted files, initialized submodules and protected resources remain guarded; Git removal is never forced. Check the current ledger for live resources rather than assuming that a historical checkout is disposable. No automatic merge of unverified code is performed.

Whole Downloads, whole TestResults, WSL disks/container volumes, unrelated ML data and the Windows pagefile are not disposable build outputs. WSL shutdown/compaction and package-cache clearing require explicit options and are not part of the default cleanup. Exact additional artifacts/download files can be selected through the existing parameters; preservation checks still apply.

## Verification

18 November2026: eight focused cases in `Scripts/tools/Test-CleanupBuildLease.ps1` passed, including Windows PowerShell5.1/PowerShell7 mutual exclusion/release, active-producer preservation and explicit worktree selection. `Scripts/tools/Test-CleanupCliBusyExit.ps1` passed against the real default entry point in dry-run: busy lease, exit1, removed0 bytes, unchanged ledger and no default WSL cleanup/compaction. `Scripts/TestHosts/Test-ProviderBuildLease.ps1` passed in the actual repository (busy1/free0/exception-release true); its generated logs/report were removed. No build, package restore or backend was invoked. Initial focused checks exposed Windows PowerShell's AllSigned policy and a callback-scope/counter bug; both were corrected before acceptance. Exact cleanup amounts/preservation are in the ledger.

The following October checks are retained historical evidence; their entire suite was not repeated:

- `Scripts/tools/Test-CleanupEmergencySpace.ps1`: **86 read-only checks pass**, no deletion, restore, build or service start.
- Full Windows cleanup dry-run completed with exit0 and enumerated both checkouts. It retained linked/native/secret resources rather than forcing their removal; no source or data was deleted.
- Subsequent targeted dry-run confirms the Unity validation Library is selected, **2,558,626,787 logical bytes**, including its regenerable shader database.
- Fixed the accidental empty optional exclusion that previously preserved the entire repository. Fixed normal Wallet/Sqlite DLL names being mistaken for private data. Original batch launcher restored after the previous edit broke launching under the host's Windows PowerShell configuration.
- Current script parses successfully; `git diff --check` passes. Deletion execution and system-administration options were deliberately not exercised on this active machine.

Per-target cleanup actions and regeneration details are recorded by the script in [WORKSPACE_CLEANUP_LEDGER.md](WORKSPACE_CLEANUP_LEDGER.md). Shared resources and prior observations remain in [ENVIRONMENT_INVENTORY.md](ENVIRONMENT_INVENTORY.md). Logical candidate sizes are not guaranteed recovered disk allocation; drive deltas include concurrent activity.
