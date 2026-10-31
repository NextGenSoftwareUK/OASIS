# Disk accounting —31 October2026

Read-only measurements after completed-test-output cleanup. GB below is decimal;
Windows Explorer usually displays GiB. Shared/inherited resources are not all
attributable to this task. No historical ownership inferred from file dates.

| Resource | Measured size | Meaning / retention |
|---|---:|---|
|Ubuntu ext4.vhdx|98.47GB file length|Contains Linux rows below; do not add twice. File length is not measured allocated NTFS bytes.|
|Linux filesystem used, df|93.73GB|Includes container storage/home/native fixtures; not additional to VHD.|
|Rootful container storage, du|31.95GB allocated|29 images/30 stopped containers/9 volumes; retained native fixtures and pinned runtime images. Shared ownership.|
|Container volume oasis-oracle-data|7.34GB allocated|Subset of container storage; required original Oracle data, preserve.|
|WSL miniforge3|34.50GB allocated|23.41GB envs/7.44GB lib/3.54GB packages, shared ML provenance uncertain. Preserve environments pending ownership review.|
|WSL /var/tmp|8.66GB allocated|Retained provider source/runtime/ledger fixtures; do not delete broadly.|
|Windows Temp|16.61GB logical|Includes installers,3.46GB Claude files and1.18GB WSL swap; mixed ownership, inspect exact candidates.|
|Windows NuGet packages|4.84GB logical|Shared reusable dependency cache, retain.|
|Windows .cache|2.69GB logical|Shared runtimes/cache, retain pending exact ownership review.|
|OASIS-cosmic total|7.94GB logical|Includes LocalState0.88GB/TestResults1.18GB and remaining binaries; overlapping rows, not additive.|
|Windows pagefile|20433MiB allocated|OS-managed virtual memory;2.17GiB current usage at observation, not a task-owned temporary file.|

Container image tool reports19.91GB images/2.714GB writable layers/8.857GB volumes;
these are tool estimates, not a substitute for du or safe-deletion decisions.
"Reclaimable" containers include required stopped demo fixtures. No prune run.
Container storage changed from an earlier35.76GB observation to31.95GB during
the scan; concurrent changes are unattributed, not claimed as this task's cleanup.

Completed provider test outputs:63 exact bin directories/4.751GB removed and
verified absent, source/cache/obj/native state/evidence preserved. See
[cleanup report](COMPLETED_TEST_OUTPUT_CLEANUP_2026-10-31.md).

Raw regenerable inventory: C:/Source/OASIS-cosmic/TestResults/DiskAccountingCurrent/.
No credentials or file contents included. Read-only measurement processes own
no native listening ports. Recreate by filesystem size scans and podman system df;
never recreate a source database to measure it. Source/environment identities and
versions remain in [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Published CQL catalogue currently has44 distinct providers:3 hosted sandbox,
40 local/native launchers and1 temporary hosted Upstash. This is scoped demo
coverage, not full ORM parity or all providers hosted on the public API.

## Reviewed installer cleanup admission

User-requested finished-artifact cleanup.37 exact Temp directories10294938775B. Microsoft-signed VS installer extractions plus exact m54u4uwi component staging already reviewed in repository helper; no active installer processes. Installed VS/SDKs and ProgramData installer state preserved. Mixed historical ownership/inherited staging, versions below; not attributed to COSMIC downloads. Lifecycle disposable after installer teardown; regenerate extraction through original Microsoft installer, reuse installed runtimes. Owner of cleanup COSMIC/CQL. No credentials in inventory.

| Exact path | Bytes | Version |
|---|---:|---|
| C:\Users\david\AppData\Local\Temp\0gyl5lwl.atc | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\1coniri5.j0n | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\3isdijyq.ggr | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\3m1i5xwh.v1s | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\4yhxix4h.p4a | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\a5jewkji.c1n | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\bt0yzswo.42w | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\ctuspl25.njf | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\dpr2wmna.3rf | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\drjkdxz3.l23 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\dxxlr4lq.mxo | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\ealpqkte.534 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\ep3awptl.hr1 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\fe5zyoef.ruk | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\gw5ltb3v.4w2 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\hd3w2lwt.npo | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\ig1rjlrn.ytv | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\odxptkg3.vig | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\ougjvrmq.gmy | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\p3t21hqn.l53 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\pjwnml3t.mgt | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\ptfzcgr5.dfp | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\pv0flndl.eid | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\rnl0tq3b.ftn | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\s4dkvzrr.5wf | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\ttz2phdk.1ql | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\txnph1gs.jxv | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\u1tduldx.yuj | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\uwpx4uqu.35c | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\w0epxwj0.10y | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\wm3oe5ik.fn0 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\wsbqrgmb.f4u | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\wvxhizrr.oit | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\yhb3kkop.b03 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\z05n52dk.xo3 | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\zzwnqoq1.kup | 148569784 | 4.11.24.2075 |
| C:\Users\david\AppData\Local\Temp\m54u4uwi | 4946426551 | Mixed component versions; inherited provenance |

## Actual cleanup outcome

All37 reviewed VS staging directories removed10294938775B; all absent, zero failures. Cfree66679021568B before/77055348736B after. Additional12 installer logs and signed VSCode staging removed264156555B, all absent/zero failures; free77318524928B (72.01GiB). Total this installer/temp batch10559095330B (10.56GB), plus earlier completed-test bins4751008965B =15310104295B removed. Free-space deltas differ because other processes use the disk concurrently. Raw receipts TestResults/DiskAccountingCurrent/{installer-cleanup-result,extra-temp-cleanup-result}.json. No downloads or installations.

Preserved in-use Claude Temp (~3.46GB;11 running processes), WSL swap.vhdx (~1.18GB; WSL active) and RDP diagnostic traces (~0.92GB;msrdc PID32632 active). Unknown source scripts/attachments/credentials retained. These were not deleted merely because they live in Temp. Installed Visual Studio/VSCode/SDKs, pinned packages, native demo stores and ML environments untouched. Lifecycle of deleted entries removed/regenerable; recreate staging with original installers only if genuinely needed.

