# Disk recovery — 2 November 2026

User target:120GB+free. Not achieved. Builds suspended during recovery.
Baseline free:47,322,533,888bytes (47.3GB;44.1GiB).
Latest measured free:60,861,591,552bytes (60.9GB;56.7GiB).
Actual free-space change:13,539,057,664bytes; concurrent activity prevents attributing every byte.

## Removed and verified

| Resource | Logical bytes removed | Verification/recreation |
|---|---:|---|
| Assigned OASIS-cosmic .NET bin/obj | 8,088,888,449 |498reviewed targets; bounded lease; inner Git checks; source/credentials retained. Rebuild with existing SDK/packages; never replay native cohorts. |
| VS installer extraction Temp/m54u4uwi | 4,050,725,559 | Existing reviewed cleanup target; no installer active. Installed VS/SDKs retained; installer can recreate extraction. |
| NuGet HTTP cache Local/NuGet/v3-cache | 3,931,321,037 | No active .NET build; installed global package cache retained. HTTP responses regenerate normally. |
| WSL pip HTTP/wheel cache | Tool reports1118.3MB |369files/697directories removed by pip; installed environments/models retained. |
| WSL APT installer cache |180,846,592bytes before | apt-get clean; installed packages retained. |

Windows logical removals total:16,070,935,045bytes (16.1GB).
WSL cache removal changes ext4 usage, not Windows free space until compaction.
Journal vacuum older than7days freed0bytes; retained recent logs.

## Large retained resources — not disposable by name or stopped status

| Exact resource | Measured size | Reason retained |
|---|---:|---|
| C:/Users/david/AppData/Local/Packages/CanonicalGroupLimited.Ubuntu_79rhkp1fndgsc/LocalState/ext4.vhdx |98,558,803,968bytes allocated before trim | Contains real environments/databases. Never delete or unsafe-sparse it. |
| Ubuntu /home/dellams/miniforge3 |34,503,409,664bytes | Personal ML installation,23.4GBenvs; outside this task. David decision required before retirement. |
| Ubuntu /home/dellams/art-ml-models |2,775,560,192bytes | Personal models, retained. |
| Ubuntu /var/lib/containers/storage |31,959,605,248bytes |31stopped containers/29images/9volumes; includes retained and uncertain original fixture data. Not pruned. |
| Ubuntu /var/tmp |8,661,655,552bytes | Contains chain ledgers, native data and pinned runtimes. Not generic disposable temp. |
| C:/Users/david/.nuget/packages |13,811,939,688bytes | Reusable installed dependencies; retained to avoid downloads. |
| C:/Users/david/AppData/Local/Temp/claude |6,799,290,176bytes | Includes Git scratch repositories/history bundles; redundancy/ownership not established. |
| C:/Users/david/.codex/sessions |6,380,625,646bytes | Chat history, user data. |
| C:/Users/david/.codex/thread_history_1.sqlite |1,784,233,984bytes | Chat history, user data. |
| C:/pagefile.sys |12,884,901,888bytes | Windows memory management; not deleted/resized. |

## WSL compaction and target gap

No WSL distributions ran before this audit. Reused Ubuntu for read-only inventory,
pip/APT cache cleanup and free-block fstrim; terminated that owned audit session.
After cleanup df reports92,328,464,384used bytes; fstrim reports virtual free
blocks, not917GiB returned to Windows. VHD compaction headroom is roughly6GB,
not80GB, before retiring further data. Exact recovery depends on VHD allocation.

Current Windows shell is not elevated. Existing compact-wsl-vhdx.ps1 requires
administrator; not attempted or bypassed. Existing David action updated with
this evidence. Reaching120GB requires additional reviewed resource retirement,
not compaction alone. Personal ML deletion requires David's decision; original
provider databases/identities must not be discarded merely because stopped.

## Evidence and preserved state

Read-only inventory and exact cleanup plans/results: TestResults/DiskInventoryCurrent.
Per-target deletion ledger: [CLEANUP_LEDGER.md](CLEANUP_LEDGER.md).
Task resource history: [COSMIC ledger](../COSMIC%20ORM/ENVIRONMENT_AND_CLEANUP_LEDGER.md).
David decisions/admin step: [single action list](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
Source/Git/uncommitted work, LocalState identities/ACKs, TestResults evidence,
credentials, active Codex runtimes/default Mongo service preserved. Previously
rejected ChainLink bin/dump deletions not retried. Other checkouts unchanged.

## Read-only duplicate archive check

The main repository pack and the scratch `rw.git` pack named
`pack-1db87945a993a68ea077a0fb53de88226f89d03d.pack` are byte-identical:
1,606,261,400 bytes each, SHA256
`b0e54a0ef4821569553d2d8b7939ba60d07f83cdd3513f8a6afb53ebb3d246b0`.
Evidence: `TestResults/DiskInventoryCurrent/duplicate-pack-readonly.json`.
This proves duplicate pack content, not that the scratch repository or its other
refs/bundles can be discarded. Both retained; original checkout read-only.
Windows free at this check: 60,860,346,368 bytes. Personal ML retirement question
sent against the existing David action; no deletion authorized or performed.

## Wider drive inventory — measured, not deletion candidates

A read-only metadata scan visited 1,511,239 files in 57.3 seconds, skipped
280 reparse points and encountered 583 inaccessible entries. Enumeration finished;
these exclusions, hard links and logical-versus-allocated size mean this is not
an exact accounting of every occupied disk sector. No file contents inspected.
Evidence: `TestResults/DiskInventoryCurrent/full-drive-metadata.json`.

| Windows path | Logical GB | Status |
|---|---:|---|
| C:/Program Files (x86)/Diablo IV |172.20| User-installed game; untouched |
| C:/Program Files (x86)/Steam |159.74| User-installed games/client; untouched |
| C:/Program Files (x86)/World of Warcraft |135.29| User-installed game; untouched |
| C:/Program Files (x86)/StarCraft II |27.05| User-installed game; untouched |
| C:/Program Files/Unity |28.64| Installed editors; retained |
| C:/ProgramData/Microsoft/VisualStudio/Packages |7.00| Managed installer cache; retained, no manual purge |
| C:/ProgramData/NVIDIA Corporation/NVIDIA App/UpdateFramework |4.88| Managed application data; retained |

Top-level logical sizes: Program Files (x86)516.17GB; Users179.74GB;
Program Files60.48GB; Source53.01GB; Windows42.70GB; ProgramData17.51GB.
Do not add nested rows to parent totals. These measurements establish occupancy,
not historical growth or which session introduced it. Games/installations are
not agent-owned temporary artifacts; no uninstall or deletion performed.
Windows free at scan completion:60,153,503,744bytes (60.2GB).
No additional bytes reclaimed by this read-only scan.120GBtarget remains open.

## Shared Cosmos build cache — retention confirmed

`/var/tmp/oasis-cosmos-contract-target` is not an unclaimed temp folder.
The Providers archive documents the reusable locked/offline Rust1.99.0 cache
and retained deployed-contract WASM. Current
`Scripts/TestHosts/Run-ReusableCosmosV2Tests.ps1` reads the WASM directly from
`wasm32-unknown-unknown/release/oasis_cosmos_storage.wasm`, and
`build-cosmos-contract.sh` uses this exact target. Removing the whole folder
would remove a live runner input, not just compiler intermediates. Retained;
no WSL startup or shared cache deletion in this review. Original Solana ledger,
Sui runtime, Cosmos chain and database folders likewise remain protected.

## David decision — 2 November 2026

David answered **Keep both** for `/home/dellams/miniforge3` (34.5GB) and
`/home/dellams/art-ml-models` (2.8GB). Both are retained personal resources,
excluded from cleanup. No deletion performed; no renewed retirement request.
The earlier pending-decision text above is historical and resolved by this entry.
Compaction alone still offers only approximately6GB;120GBtarget remains unmet.

## Authorized duplicate removal — rejected, file preserved

On2November David explicitly asked to remove the1.6GB duplicate scratch Git
archive. Automatic approval review rejected the exact bounded removal command
before execution: `blocked by policy`. No preflight/removal ran and no space was
reclaimed. Do not retry through another shell/tool or bypass the rejection.
The retained original and duplicate identities are recorded above. This is an
agent/tool limitation, not a request for David to repair the environment.

Provider environment retirement: approximately32GBcontainer storage plus9GB
WSLvar/tmp includes reusable runtimes and unique fixture/chain/database state.
These are occupancy estimates, not historical growth attribution or current-use
proof for every environment. Most owned disposable resources can be retired after
their acceptance gates finish and original data/identities/evidence are preserved
or explicitly retired. Do not blanket-delete stopped containers/ledgers. Personal
Miniforge/models remain excluded by David's KEEP BOTH decision.

## Current test batch cleanup and end-of-task retirement requirement

David reiterated that task-introduced environments/downloads must be cleaned up
after their work finishes. This includes task-only runtimes/installations once
no remaining acceptance gate or other owner needs them, not just bin/obj. At
handoff reconcile exact paths/ownership with this report and the resource ledger;
remove verified disposable resources, explicitly list retained/transferred ones
and why. Personal ML remains excluded; no blanket database/history deletion.

After Akash17controlled/1native continuation tests, removed305,589,142logical
bytes across59reviewed generated-output targets in the assigned worktree under
the build lease. Secrets were retained; no source/config/ACK/data/runtime removed.
Per-target log SHA256b375a8ad59ed02d2497e51b0e8843eac1b210990c8088c93f71dab0ac5be77cd.
Raw log/result `TestResults/DiskInventoryCurrent/akash-streaming-build-cleanup*`;
per-target ledger CLEANUP_LEDGER.md. Wrapper counters initially reported0due to
scope; original observation preserved, reconciled total comes from59actual
per-target removal records. No extra deletion run to repair telemetry.
Free before65,685,192,704bytes/after65,894,416,384bytes; logical removal and
physical free change differ. Concurrent free-space changes are not attributed.
