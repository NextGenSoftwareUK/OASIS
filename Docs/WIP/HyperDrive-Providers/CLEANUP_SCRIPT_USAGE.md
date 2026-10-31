# Cleanup script — 26 October 2026

Use the existing Windows launcher from the repository root:

```powershell
.\Scripts\tools\cleanup-emergency-space.bat -DryRun
.\Scripts\tools\cleanup-emergency-space.bat
```

The batch launcher's original behavior is restored. No machine execution-policy setting was changed. Direct Windows PowerShell script tests were refused by its existing Restricted policy; the tests and read-only end-to-end invocation below ran in the already-installed PowerShell runtime. No mutating cleanup was executed during verification.

Default cleanup now includes discovered MSBuild `bin`/`obj` folders in the main OASIS checkout and the registered old HyperDrive checkout, reviewed Unity validation caches/builds, native-game/package verification output copies, signed VS installer extraction folders, the reviewed VS package-download staging folder, signed VSCode staging and old installer diagnostic logs. Source, package/runtime caches, current native test evidence and credentials are retained.

Active project/output ownership and exact artifact candidates are in [cleanup-emergency-space.targets.psd1](../../../Scripts/tools/cleanup-emergency-space.targets.psd1). The active-chat snapshot protects Core/Common/Contracts/DNA/BootLoader/ONODE, current Mongo/SQLite/Neo4j and AWS/Dynamo projects, consumers and the shared storage layer. Update it when ownership changes; extra exclusions can be passed with `-PreservePath` and `-ActiveWorktreePath`.

The global producer/consumer lease and process checks prevent cleanup during a build/test slot. The script does not stop builds or services to force removal. Close/finish builds before running it again if generated-output cleanup was skipped. Referenced runtime junctions, tracked source, native databases, pending journals, secrets and open files are refused. Unity Library `.db` files are a narrow regenerable-cache exception, not permission to delete provider databases.

Clean merged inactive worktrees are considered automatically, without forced Git removal. Unique commits, ignored/uncommitted files, initialized submodules and protected resources keep a worktree in place with a reason. The old `C:/Source/OASIS-hyperdrive-v2-gaps` checkout currently holds live shared Hardhat, DynamoDB and IPFS resources and a unique commit; it cannot yet be retired wholesale. Its unused build outputs are still cleanup candidates. No automatic merge of unverified code is performed.

Whole Downloads, whole TestResults, WSL disks/container volumes, unrelated ML data and the Windows pagefile are not disposable build outputs. WSL shutdown/compaction and package-cache clearing require explicit options and are not part of the default cleanup. Exact additional artifacts/download files can be selected through the existing parameters; preservation checks still apply.

## Verification

- `Scripts/tools/Test-CleanupEmergencySpace.ps1`: **86 read-only checks pass**, no deletion, restore, build or service start.
- Full Windows cleanup dry-run completed with exit0 and enumerated both checkouts. It retained linked/native/secret resources rather than forcing their removal; no source or data was deleted.
- Subsequent targeted dry-run confirms the Unity validation Library is selected, **2,558,626,787 logical bytes**, including its regenerable shader database.
- Fixed the accidental empty optional exclusion that previously preserved the entire repository. Fixed normal Wallet/Sqlite DLL names being mistaken for private data. Original batch launcher restored after the previous edit broke launching under the host's Windows PowerShell configuration.
- Current script parses successfully; `git diff --check` passes. Deletion execution and system-administration options were deliberately not exercised on this active machine.

Per-target cleanup actions and regeneration details are recorded by the script in [WORKSPACE_CLEANUP_LEDGER.md](WORKSPACE_CLEANUP_LEDGER.md). Shared resources and prior observations remain in [ENVIRONMENT_INVENTORY.md](ENVIRONMENT_INVENTORY.md). Logical candidate sizes are not guaranteed recovered disk allocation; drive deltas include concurrent activity.
