# Workspace cleanup ledger and repeatable checks

2026-10-15 IPFS native verification: retained ONE official pinned Kubo0.43.1 executable at C:/Users/david/.cache/oasis-runtimes/kubo-0.43.1/ipfs.exe (91489792bytes), installed only after inventory found no reusable Kubo. Removed exact owned download archive kubo_v0.43.1_windows-amd64.zip (42170789bytes) after official SHA256 verification; can redownload from official GitHub release if required, not needed for reruns. Reuse Scripts/TestHosts/Run-ReusableIpfsTests.ps1, one ignored TestResults/IPFSCurrent/repo, and storage-key.dpapi (262bytes, credential—DO NOT DELETE). Preserve repo identity/config and compact initial verified baseline evidence. Successful runs unpin only acknowledged owned fixture CIDs and GC unpinned blocks (latest18), delete exact successful owned-dna.json; failed/uncertain fixtures remain for inspection. No daemon repository reset/recreation. Fixed TRX/logs overwritten, owned offline daemon stopped. Regenerate disposable synthetic fixture content through runner; ciphertext/CIDs change with fresh test IDs/nonces. Final run free90262515712 ->90262659072bytes; installation cost~91MB after archive removal, not tens of GB. No unrelated cleanup.

2026-10-14 Ethereum reusable fixture privacy key: preserve ignored `TestResults/EthereumCurrent/storage-key.dpapi` (262bytes). It is a CurrentUser-DPAPI-protected32byte private storage key, generated once and reused, separate from the public Hardhat development signer. This is a credential, NOT a disposable test report; do not delete it with broad output cleanup. Fixed fixture JSON/log/TRX files remain compact/overwritten, local EVM has no filesystem ledger, owned SQLite DBs disposed/deleted after tests. No new SDK/runtime/install/download for encryption work.

2026-10-14 Solana runtime restored ONCE after absence verified: official Agave4.3.0 archive84,790,505bytes, SHA256c97289a8abb1d0efb497d8b5cb285baabd9b7f8ea6647f5d145c5dc8ff3611e8. Retained only solana-test-validator/solana/solana-keygen in /var/tmp/oasis-runtimes/agave-4.3.0 (~113MiB). Removed exact resolved downloaded archive after successful digest/extraction/version checks; regenerate only from pinned official release if genuinely necessary. Single reusable /var/tmp/oasis-solana ledger+fixture keypair+separate storage key (~148MiB) retained; owned validator stopped and verified absent after actual integration1/1. Do not recreate keys/ledger or download runtime next run. No user data/other runtime removed. Free C110323154944bytes (~102.7GiB). Older denied Mongo log deletion remains unresolved, not bypassed.

2026-10-14 reusable Mongo verification inspection: `TestResults/SubscriptionUsageCurrent/mongo-current.log` was 489,761,165 bytes (~467 MiB). Exact disposable-log removal was denied by execution policy; nothing removed and no alternative deletion route used. Port27029 was not listening after owned suite shutdown; unrelated mongod PID7140 was not stopped. Existing compact TRX reports are overwritten, pinned runtime/replica reused. Latest free C111444733952bytes (~103.8GiB); remaining free-space movement is not attributed to a specific artifact by this check. A bounded runtime-log retention policy remains needed; no claim that cleanup succeeded.

Updated 2026-10-13. Units: GiB = 1,073,741,824 bytes; GB = 1,000,000,000 bytes. File totals, logical VHD length, and recovered drive space are different measurements. Concurrent cleanup means a drive-space increase cannot all be attributed to this chat.

## Rules

- Reuse the existing HyperDrive checkout and compatible installed runtimes. No new clones, worktrees, replacement environments or repeated downloads.
- Commit source before removing a checkout. Preserve detached heads and initialized submodule heads in the existing primary repositories; do not delete their branches.
- Validate exact resolved targets, tracked/uncommitted files, ignored data, links, active processes and credentials before removal. No broad workspace/home deletion.
- Retain credentials, user data, required current evidence and unresolved native transaction ownership journals. Never reset an uncertain ledger or replay an uncertain mutation.
- Build only when needed, with bounded workers; do not restore into caches another cleanup is currently deleting. User last confirmed that cleanup is still running.
- Default to inspection. Explicit removal mode applies only to validated disposable output. A policy-denied deletion is not retried through another tool, shell or script.

## Confirmed removals

| Exact target | Removed material | Size / evidence | Recovery or regeneration |
|---|---|---|---|
| `/nix/var/nix/builds/nix-583-3695671890/build/source/target` | Stale generated Holochain compilation | 3.5 GiB file footprint; original PID absent, no compiler/open files. Linux used72→69GiB; TRIM returned **2.45 GiB measured C: space** | Rebuild the retained Holochain source with existing tooling if required. Source and Nix store were retained |
| `C:/Source/ourworld-main-promotion` | Clean redundant worktree | Earlier file inventory2.44GiB | `refs/archive/cleanup/ourworld-main-promotion` in `C:/Source/Our-World`; existing history retained |
| `C:/Source/OASIS-api-host-resilience-dev` | Clean redundant worktree and generated output | Earlier2.87GiB | Parent and initialized submodule recovery refs in primary OASIS repos |
| `C:/Source/OASIS-api-host-resilience-prod` | Clean redundant worktree | Earlier0.36GiB | Same reference convention; existing branches retained |
| `C:/Source/OASIS-sharing` | Clean redundant worktree and generated output | Earlier1.37GiB | Same reference convention; branch retained |
| `C:/Source/OASIS/.claude/worktrees/practical-austin-2d6d0a` | Clean redundant worktree, no ignored data or initialized submodules | Removal confirmed; byte recovery not separately measured | `refs/archive/cleanup/practical-austin-2d6d0a` in primary OASIS |
| Generated `bin`/`obj` belonging to `.csproj` projects under `C:/Source/OASIS` | Untracked unused .NET output; tracked output/credentials/dependency links excluded | Script aggregate **9.60GiB** removed. Source status clean afterward | Normal build of the required project, using existing environment, not a fresh checkout |
| Stale Git worktree registrations for already-absent `.codex/worktrees` checkouts | Administrative records only | `git worktree prune --expire now`; no live checkout or branch removed | Existing branches/history retained |

The first .NET removal streamed per-directory results, but no durable per-file manifest was retained. Do not invent one retrospectively. Future removals record exact directory intent/completion in the compact journal described below.

## Commit-first preservation

These are local preservation commits, not tested releases or deployment authorization:

- Android reusable runtime: `d324df1`; line-ending-only apparent changes normalized, actual Cargo lock change committed.
- Main OASIS submodules: ONODEManager `789df4c`, STAR `357af79`, WEB6 `aae330d`.
- Main OASIS parent: `6ee2e46eb`; matching dependency pins `71d2c455d`. Existing Python validator passed manifest/seven Dockerfiles/gitlinks checks.
- Our World source and Edge package: `e0224439`. Private/untracked `BuildLogs` were not staged or deleted.
- Current provider work remains on the existing HyperDrive task branch; no new branch created and no premature merge.

## `C:/Source/OASIS/artifacts` inspection

Approximate total2.8GiB, no tracked files and no process referencing this directory at inspection:

| Item | GiB | Current disposition |
|---|---:|---|
| `unity-edge-validation-project` | 2.398 | User authorized unused generated output removal, but deletion was policy-blocked; remains |
| `native-games` | 0.311 | Generated object and duplicate DLL/PDB; duplicate hashes match retained publish copies. Deletion attempt blocked; remains |
| `unity` | 0.046 | Packaged/unpacked generated Edge output; remains |
| `release-promotion` | 0.030 | Generated archives/packages and test reports; remains |
| Remaining logs/reports/protected session/backup directories | Small | User subsequently said Railway backup already exists and unused artifacts may be removed; **whole-directory deletion still policy-blocked** |

No artifact deletion is claimed. Do not bypass the policy through a different mechanism. User may remove this explicitly named directory manually after confirming needed backups/data are elsewhere. Current HyperDrive evidence and native pending journals are outside it.

Known producers to consult before rerunning:

- `Scripts/validate_edge_unity_package.ps1` creates the validation project. Do not blindly recreate its disposable project copy: prefer the existing suitable Unity project, and review necessity before any duplicate allocation.
- `OASIS Omniverse/OGEngineClient/Scripts/publish_and_deploy_star_api.ps1` produces native Edge builds/publish outputs. Do not run merely to regenerate deleted artifacts; deployment is a separate authorized action.
- `Docs/Devs/THREE_GAME_EDGE_RELEASE_ARTIFACT_MAP.md` documents native build/deployment ownership.

## Re-run and monitor

From the **existing** `C:/Source/OASIS-hyperdrive-v2-gaps` checkout:

```powershell
# Read-only current Windows drive measurement.
Get-PSDrive C | Select-Object Used, Free

# Read-only generated .NET output audit; does not restore or build.
& Scripts/TestHosts/Remove-StalePrimaryDotNetOutputs.ps1

# Only after source preservation, target/process/data verification and no overlap:
& Scripts/TestHosts/Remove-StalePrimaryDotNetOutputs.ps1 -Remove

# Inspect recovery references and retained checkouts.
git -C C:/Source/OASIS worktree list
git -C C:/Source/OASIS for-each-ref refs/archive/cleanup
```

Future `-Remove` writes JSONL **before each deletion** (`DeleteIntent`) and after success (`Deleted`) to `TestResults/cleanup-events.jsonl`, with exact directory, bytes and free-space observations. An intent without completion is not proof of deletion. Keep this compact journal, not copies of deleted output. Inspection alone does not create it. No scheduled automation has been created.

Do not rerun `cleanup-stale-holochain-build.sh` expecting further recovery: its one exact target was already removed. It must refuse missing/changed/in-use targets. WSL TRIM only releases unused filesystem blocks; never treat its reported trimmed capacity as actual Windows recovery.

## Still retained / unresolved

Disk verification checkpoint2026-10-13 after current Sui live NFT/token runs: Windows free121517133824bytes (121.52GB decimal). Retained `/var/tmp/oasis-sui/network`973MiB, pinned Sui runtime209MiB; preceding ledger measurement954MiB is historical apparent usage, not exact Windows allocation. Current NFT/token TAP373/420bytes, prepared-ledger TRX4963bytes, startup stderr1994bytes; listed Sui reports small, no per-run environment copies. Windows pagefile remains20480MiB allocated,842MiB current,5409MiB peak. Recursive Temp audit found only VS Code installer251842640bytes above200MiB, last write12:12:50. This evidence does NOT account for the whole-drive multi-GB drop or prove its owner; no unrelated deletion/reinstall attempted. Keep measuring before/after subsequent substantive work and investigate changes rather than claim test footprint explains them.

- Main repositories and the one active HyperDrive checkout, pinned runtimes and current reusable outputs.
- Private `BuildLogs`, DPAPI test identities, live account keys, pending Cosmos/TRON/native deployment journals and necessary current test evidence.
- Conda environments `piper-art`, `slp`, `mfa`: ownership/continued need unresolved. Their33GiB combined installation footprint is not proof of33GiB newly leaked data.
- NEAR runtime/build folders have not been removed; source/dependency ownership needs review before disposal.
- Requested150GB recovery is unfinished. Last observed123.29GiB =132.38GB free before resuming dependency verification. User subsequently confirmed concurrent cleanup finished. The shared global NuGet packages directory was removed; required dependencies may now be restored into that same cache, without another environment or checkout. Reuse remains mandatory globally in `C:/Users/david/.codex/AGENTS.md`.
# 2026-10-13 shared WEB4 dependency reuse

WEB4 verification found pinned AWS DynamoDB3.7.400.15 and S3 3.7.401 package directories absent from the shared NuGet cache. Restored existing project into that same cache once; no new environment or checkout. Free C:109.99GiB before,109.35GiB after restore/build/test. Retained reusable packages and current build outputs; fixed compact WEB4 TRX reused rather than per-run directories. WEB4 build and147 unit tests pass. No deletion performed in this step.

