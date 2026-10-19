# Reusable environments and disk inventory

Cosmos native reuse checkpoint2026-10-19: existing root Podman oasis-wasmd/container/image/ledger/signing identity/contract reused without download/install/reset/deploy. Current runner accepts explicit -EvidenceRoot pointing at `C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/HyperDriveCosmos`, measured2,114,979logicalbytes/66files AFTER run, including original unresolved replication journal. Do NOT delete this old-checkout evidence directory or duplicate it under current checkout. Current Cosmos integration bin45,521,347logicalbytes/205files; existing output SDK junction reuses current source's dependency junction into retained old-checkout node_modules, no new SDK copy. Missing test project assets regenerated from local NuGet cache only.2nativepassed57s/outer runner terminal0; owned wasmd/container stopped, original replication hash unchanged and independent pending journals absent. Same exclusive Mongo environment used; later transient27029listener ownership unknown and PID gone on follow-up, no unrelated process killed. Windows free68,015,890,432bytes whole-machine observation, not task allocation/reclaimed attribution. No public gas/spend, no new environment; compact reports retained.

2026-10-19 TRON/Hedera shared-consumer follow-up: existing TRON .NET SDK/project/output reused (no Node bridge claimed for TRON). Missing test assets restored solely from existing local NuGet cache. TRON unit bin127,201,887logicalbytes/214files; Hedera unit bin45,951,931bytes/215files, non-additive with parent totals. Hedera current `C:/Source/OASIS/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/node_modules` junction now targets retained `C:/Source/OASIS-hyperdrive-v2-gaps/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/node_modules`; all three direct dependency versions verified (@hiero-ledger/sdk2.86.2, @hiero-ledger/proto2.31.0, ethers6.16.0). No new SDK/install/download/copy. Protect this old-checkout dependency target from blanket cleanup. Same existing exclusive Mongo environment reused sequentially; TRON480unit cases passed, runner terminal0; Hedera outcome still pending at this checkpoint. Neither blockchain node started, no public funding/spend. Windows free68,323,536,896bytes is whole-machine observation, not task allocation or reclaimed space.

Sui follow-up2026-10-19: current `C:/Source/OASIS/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/node_modules` is now a junction to retained `C:/Source/OASIS-hyperdrive-v2-gaps/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/node_modules`. Verified existing @mysten/sui2.35.0 and @scure/bip392.4.0 against current package.json before linking. No dependency installation/download/copy; old dependency target must remain until reviewed consolidation. Existing missing project assets refreshed from local NuGet cache only. Same exclusive Mongo recovery environment reused; Sui native network was NOT started. During-run Sui unit bin45,523,598logicalbytes/207files, Windows free68,497,014,784bytes whole-machine observation. Both Cosmos and Sui test-output shared DLL hashes match the freshly built shared producer; this is local-binary identity, not clean committed-source provenance or full native certification.

2026-10-19 shared caller-publication consumer verification: reused existing Cosmos SDK dependencies by creating one directory junction at `C:/Source/OASIS/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/node_modules` targeting the existing `C:/Source/OASIS-hyperdrive-v2-gaps/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/node_modules`. All direct dependency versions checked against current package.json; current patch-cosmjs.mjs --check passed without rewriting the retained installation. The old checkout's bridge/source/lock differ from current source: do NOT substitute that old bridge. Its node_modules is now an active shared dependency, not a disposable old-checkout artifact. No npm install/download/copy. Same retained SubscriptionUsageCurrent Mongo data/runtime/27029 used through existing exclusive runner, no new replica environment. During-run logical measurements: SharedCurrent 1,849,460 bytes/5files; Cosmos unit bin 45,589,281 bytes/209files. Windows free68,516,147,200bytes, whole-machine observation rather than attributed usage. Final test/lifecycle outcome must be recorded separately; these measurements do not imply acceptance or disk reclamation.

## Coverage and follow-up checklist

This inventory applies retrospectively to existing downloads and setups as well as future ones. Keep one reusable installation per compatible requirement; an inventory entry is not permission to delete it.

| Resource group | Current coverage | Remaining action |
| --- | --- | --- |
| Oracle, Cosmos DB, wasmd, TRON, Neo4j, SOLID containers | Existing image identities, storage locations and configured bindings recorded below | Complete missing digests/size provenance and lifecycle commands; preserve writable-layer data and secrets |
| Solana, Sui, NEAR, Substrate | Retained runtime/data paths and measured sizes recorded | Reconcile remaining SDK dependencies, process ownership and safe retirement prerequisites |
| DynamoDB Local, Azurite, Firestore, IPFS | Existing runtime/cache paths recorded | Complete data/key ownership and reuse commands where still missing |
| Windows .NET, JDK, Node, NuGet | Shared installation/cache measurements recorded | Distinguish shared dependencies from task-owned disposable outputs |
| Repository bin/obj, artifacts, old checkouts | Partial measurements and active dependency junctions recorded | Finish bounded ownership review; do not delete old checkouts with active linked dependencies |
| Candidate duplicate NEAR Node runtime | Matching version, file count, logical size and executable hash recorded | Review consumers and physical allocation before consolidating to one installation |
| Android and Windows/WSL disk allocation | Partial location checks only | Locate alternate installations and measure actual VHDX allocation without double-counting Linux contents |

Latest read-only Windows free-space observation, 2026-10-19: **68,819,816,448 bytes**. No downloads, installations, service starts or removals were performed for this documentation checkpoint. This is not a complete machine-wide accounting or a claim of reclaimed space. Continue updating the entries below and `WORKSPACE_CLEANUP_LEDGER.md` in place; do not create another inventory copy.

Polkadot2026-10-19 signing audit/native regression: SAME retained contracts-node0.42.0 binarySHA256B5E30AD4BCF37AB0A875F88C690460CF68A0847C03E74045D15BB16513892991/spec/genesis/ledger, existing Windows SDK junction and unit host. Bootstrap reused existing completed transaction, no new top-up/runtime/contract/env. Native4pass/0skip; original pending journals absent only after verified results; owned node250496 stoppedSIGINT/terminal0, ports19944/19945closed, ledger1.5MiB retained. Current PolkadotCurrent913,583logicalbytes/30files; unitbin230,625,702bytes/527files measured after restoring ordinary unit-only build, non-additive with parent totals. Unit197pass/0skip; source reports no storage completion. WSL Rust1.99/cargo1.99 and wasm32-unknown-unknown target already installed; inspected cargo registry lacks ink/cargo-contract and cargo-contract executable absent. No installation/download undertaken pending target decision. Windows free68,839,616,512bytes whole-machine observation, not allocation/reclaim attribution; compact reports/bins/SDK/runtime retained for reuse.

NEAR metadata final2026-10-19: SAME retained runtime/home/signers/SDK/junctions/fixed reports, no installation/download/duplicate environment/contract build/deploy/reset.152units/19native pass/0skip; node207715 stopped/SIGINT terminal0, no process/listeners3030+24567/pending checked SDK original, completion verified-fixtures-absent. Receipt retention temporarily5→50 then restored5after stop; existing ledger247MiB allocated, retained. NearRuntime733,316logicalbytes/17files (two metadata investigation TRX retained, initial boolean-assumption caveat recorded); unitbin45,487,209bytes/209files; nativebin45,439,223bytes/204files. Existing outputs reused/non-additive with parent totals, no broad deletion. Windows free68,843,704,320bytes is whole-machine observation, not allocation/reclaim attribution. Candidate duplicate Node runtimes from prior inventory remain untouched pending reviewed consolidation.

NEAR final checkpoint2026-10-19:148controlled/19native pass/0skip, same owned node160653 stopped/SIGINT terminal0; no near-sandbox process/3030 or24567 listeners, checked SDK pending absent/completion verified-fixtures-absent. Restored retention50→5after stop. Latest launcher loopback3030/24567 supersedes earlier18830/18831 notes below. Existing ledger241MiB allocated; no chain reset or deletion. Fixed NearRuntime708,029logicalbytes/15files (includes retained failed-interface report); unitbin45,482,269bytes/209files; nativebin45,437,439bytes/204files. Parent/child totals non-additive; bins retained for reuse. Windows free68,859,015,168bytes whole-machine, not attributed usage/reclaim. No download/install/new environment/contract rebuild/deploy. Preserve old-checkout dependency junctions and both candidate Node runtime copies until reviewed consolidation; no blanket worktree/cache deletion.

2026-10-19 NEAR interface verification inventory: SAME retained near-sandbox2.6.2 executable/home/genesis/signer and Node22.22.2; cached run-evidence.mjs SHA256B43F06370D1DC5ABFCFE3C343B7F408E91F24B8C8CD13E82E7B6DF0369D35EC0 equals current source before startup. Windows SDK node_modules remains an existing junction into C:/Source/OASIS-hyperdrive-v2-gaps/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/node_modules; Linux SDK/compiler node_modules remains the existing symlink into that checkout's Scripts/TestHosts/near-local/node_modules. These old-checkout paths are ACTIVE dependencies, not blanket disposable worktree artifacts. No new runtime/environment/download/install or contract compilation/deployment. Loopback-only18830/18831 override existing wildcard config bindings. Temporary gc_num_epochs_to_keep5→50 is solely the established verification receipt window; restore5after stopping. Ledger measured232MiB allocated DURING live run, not final size or a claim of space recovered.

Retrospective discovery: C:/Users/david/AppData/Local/OASIS/provider-evidence/near/node/22.22.2 ALSO exists,196,581,359logicalbytes/4753files, equal logical total to C:/Source/oasis-near-node-22.22.2. Both directories report no junction/symlink; both bin/node files (124,679,552bytes) SHA25681925C0995B5C1427B5D538E6A90CA2FDC4DAFFB786B09AF749BEAF7369D4E90. This is a candidate duplicate pinned runtime, NOT a new download from this work. Existing Scripts/run_near_provider_evidence.sh still references the AppData cache-root layout, while the new reuse-only launcher uses the existing Source runtime. Review all consumers, process ownership and hardlink/physical allocation before consolidation/removal; neither has been removed or counted twice as reclaimed space. Keep one canonical runtime when that review is complete, not another installation.

2026-10-19 Azure public-history final94native/40units acceptance reuses SAME Azurite3.37.0 installation, Node, loopbackports10000/10003, fixed AzureCurrent and existing .NET/NuGet/test projects. Final runner0/no matching owned Azurite process/listeners/routingfiles. AzureCurrent502,883logicalbytes/7files (includes two compact failed native reports); unitbin46,972,358bytes/202files, nativebin88,167,091bytes/237files. Do not add nested totals to earlier parent inventories. Windows free70,323,032,064bytes, whole-machine only; no attribution or broad reclaim claim. No install/download/new environment. Removed only exact independently verified disposable638,976byte SQLite and1910byte LocalFile failed fixtures, with hashes/identity checks/report retention in cleanup ledger. Existing bins retained for reuse; in-memory emulator data is not durable restart evidence.

Azure candidate terminal checkpoint2026-10-19: runner exit0,80native pass/0skip; owned Node39464 absent, ports10000/10003closed. AzureCurrent final190,579logicalbytes/5files (supersedes during-run size only). Existing in-memory runtime was stopped by its owner; this is not restart durability proof. Three acknowledged history fixtures exact-ETag-deleted after independent validation/native absence. No new runtime/download/install or broad output removal; retain the same fixed binaries for continued public-integration work.

2026-10-19 Azure history candidate work reused the SAME retained Azurite3.37.0 Node runtime and TestResults/AzureCurrent runner/ports. Unit and native project.assets.json were missing; refreshed only from existing C:/Users/david/.nuget/packages using an explicit offline source. No downloads/install/new environment. Fixed unit bin46,963,046logicalbytes/202files; native bin88,762,519bytes/238files. Reuse these existing project outputs, not new per-run directories. AzureCurrent measured59,952bytes/4files DURING live native testing (not final report size); compact reports/log/lease are evidence, not a deletion target during execution. Windows free70,476,255,232bytes is whole-machine observation, not allocation attribution or reclaimed space. Native runtime lifecycle/outcome is recorded separately after terminal confirmation.

## Retrospective backfill checkpoint — 2026-10-19

The requirement covers **all existing setups and downloads**, not just Oracle. This inventory is being extended in place; it is not yet complete machine-wide accounting. No services were started or resources downloaded/deleted for this checkpoint.

Read-only Windows enumeration measured these additional existing locations (logical bytes, not physical allocation):

| Exact location | Logical bytes / files | Classification and retention |
| --- | ---: | --- |
| C:/Source/android-service-runtime | 34,743,355 / 1,332 | Existing Git source checkout, not an Android SDK/emulator installation. Preserve source and uncommitted work. |
| C:/Users/david/.cache/pkg | 9,520,416 / 8 | Shared package cache; package ownership/version/use not yet reconciled. Do not remove or attribute to this task without review. |
| C:/Source/OASIS/Providers/Cloud | 710,814,303 / 3,248 | Includes source and generated output; not a deletion target. |
| C:/Source/OASIS/Providers/Storage | 904,961,599 / 6,027 | Includes source and generated output; not a deletion target. |
| C:/Source/OASIS/Providers/Network | 602,180,248 / 4,147 | Includes source and generated output; not a deletion target. |

Within those provider parents, existing generated-output totals were measured separately:

| Parent | Nested bin directories: bytes / files | Nested obj directories: bytes / files |
| --- | ---: | ---: |
| Providers/Cloud | 682,690,908 / 2,394 | 26,117,733 / 514 |
| Providers/Storage | 820,695,044 / 3,598 | 75,992,334 / 1,490 |
| Providers/Network | 545,411,703 / 2,596 | 47,920,163 / 1,043 |

Outputs are already included in parent totals and overlap individual project measurements below: **do not add these tables together**. Build artifacts are not independent runtime installations. Reuse current outputs; before cleanup resolve exact project directories, verify no build/test process uses them, and preserve required evidence/dependencies. Regenerate through the existing project and retained dependency cache, not a replacement environment. No blanket parent deletion is authorized by this measurement.

Windows free space: 70,645,809,152 bytes, a whole-machine observation, not reclaimed space or allocation attribution. Outstanding retrospective work includes other provider families, additional source/worktree artifacts, alternate Android installs and actual VHDX allocation. Unknown ownership and sizes remain explicitly unverified.

## Scope: existing resources as well as future setups

The user's requirement explicitly applies retrospectively to every environment, runtime, SDK, download cache and generated build/test directory used by this provider programme, not only Oracle. This file is the central inventory; do not create separate duplicate inventories per run. The cleanup ledger records changes and removals.

Already inventoried below: retained Oracle, Azure Cosmos emulator, TRON, Cosmos/wasmd, Neo4j and SOLID containers; Solana, Sui, NEAR and Substrate runtimes/data; DynamoDB Local, Azurite, IPFS Kubo and Firestore; shared Windows .NET/JDK/Node/NuGet caches; and sampled repository build outputs. These entries have different levels of provenance and completeness: inclusion is not certification that every version, port or byte has been reconciled.

Outstanding backfill must cover all other provider evidence/runtime directories, repository-wide bin/obj and temporary downloads, actual Windows VHDX allocation, and any alternate Android locations. For each resource, record exact path, version/digest where available, measured size and measurement date, owner/purpose, data/credential dependencies, existing reuse command, and safe retirement/regeneration prerequisites. Mark missing evidence as unknown rather than guessing. Never start, reinstall or download a service merely to inventory it. Review possible duplicate copies before removal; preserve the one reusable environment and all unreconciled data.

Measured 2026-10-19. This is a backfill in progress, not a claim that all machine usage is accounted for. Every subsequent setup/download must update this inventory AND the chronological WORKSPACE_CLEANUP_LEDGER.md. Do not duplicate an existing suitable runtime. Record exact version/digest, location, measured image/runtime AND data sizes, process/service/port owner, purpose, reuse/start command, retained credentials/evidence and cleanup prerequisites. Never record secret contents.

Windows figures below are logical file bytes from bounded recursive enumeration; WSL `du -sh` figures are allocated approximate binary units. Podman image sizes are reported estimates and layers can be shared. Parent totals INCLUDE their children: do not add both. WSL files also occupy the Windows VHDX: do not count VHDX and Linux contents twice. Windows free disk at the first snapshot:79,090,307,072bytes. No deletion or reclaim is claimed.

## Windows retained resources

Additional read-only shared SDK backfill2026-10-19: C:/Program Files/dotnet measured4,960,797,829logical file bytes; C:/Program Files/Microsoft/jdk-21.0.12.101-hotspot342,880,907bytes; C:/Program Files/nodejs105,853,313bytes. Existing installed SDKs, reused not newly installed by this batch; retain, do not attribute all machine SDK storage to this task. Enumeration of C:/Users/david/.android/avd returned no files, not evidence that all Android SDK/emulator locations are accounted for. Bound recursive file measurement can omit inaccessible files; VHDX/allocated/shared-layer ownership still needs separate reconciliation. No cleanup or download in this backfill.

| Exact location | Measured logical bytes | Purpose / retention |
| --- | ---: | --- |
| C:/Users/david/.cache/oasis-runtimes | 240321671 | Parent total; shared provider runtimes |
| C:/Users/david/.cache/oasis-runtimes/azurite-3.37.0 | 148831879 | Retain Azure storage emulator; reuse existing runner |
| C:/Users/david/.cache/oasis-runtimes/kubo-0.43.1 | 91489792 | Retain IPFS Kubo; preserve separate repo identity/key |
| C:/Users/david/.cache/firebase/emulators | 154630097 | Retain Firestore official emulator v1.22.0; same existing runtime |
| C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/runtime | 66707703 | Retain DynamoDB Local; reviewed source SHA256 A3CB28740EA3875A7B0E14CFB13CED686F36CE27BC00F13F8A7D72DDEEE6C9B1; runner Scripts/TestHosts/Run-ReusableDynamoHistoryTests.ps1 |
| C:/Source/OASIS-hyperdrive-v2-gaps/TestResults | 899630146 | Parent total includes retained runtimes/data/evidence; not blanket disposable |
| C:/Source/OASIS/TestResults | 231375892 | Fixed current reports, journals, keys, retained data; never bulk delete |
| C:/Source/oasis-near-node-22.22.2 | 196581359 | Retained Node22.22.2 for actual NEAR SDK; reuse |
| C:/Source/oasis-near-build.PW3RzM | 372359216 | Existing build/dependency environment; ownership/use review before cleanup |
| C:/Users/david/.nuget/packages | 7235382876 | Shared dependency cache; preserve/reuse, not per-run restore/download |
| C:/Users/david/.cache/codex-runtimes | 2138120967 | Product-managed runtime cache; NOT assumed created/owned by this task; do not remove without product/use review |
| C:/Source/OASIS/artifacts | 2991724689 | Shared parent total, other sessions/projects own contents |
| C:/Source/OASIS/artifacts/unity-edge-validation-project | 2575104592 | Largest artifact child; active-use/source/owner check required, no deletion authorized by this inventory |
| C:/Source/OASIS/artifacts/native-games | 334305823 | Shared game artifacts, not assumed disposable provider output |

## WSL Ubuntu retained runtime/data

| Exact Linux location | `du -sh` allocated size | Purpose / retention |
| --- | ---: | --- |
| /var/tmp/oasis-runtimes | 565M | Parent total; retain pinned reusable executables |
| /var/tmp/oasis-runtimes/agave-4.3.0 | 113M | Solana validator runtime |
| /var/tmp/oasis-runtimes/near-sandbox-2.6.2 | 72M | NEAR sandbox runtime |
| /var/tmp/oasis-runtimes/substrate-contracts-node-0.42.0 | 172M | Polkadot/Substrate contract runtime |
| /var/tmp/oasis-runtimes/sui-1.81.1 | 209M | Sui runtime |
| /var/tmp/oasis-near-evidence | 670M | Parent total, includes ledger665M, build/dependencies/evidence; retain original outcomes/signers |
| /var/tmp/oasis-solana | 130M | Original ledger/key material; consensus recovery remains open; NEVER reset/delete tower/ledger to get passing tests |
| /var/tmp/oasis-sui | 1.4G | Same retained network and deployment evidence/keys; no reset |
| /var/tmp/oasis-polkadot-evidence | 3.4M | Retained chain/evidence; preserve originals |
| /var/tmp/oasis-cosmos-contract-target | 1.5G | Existing contract build output; candidate ONLY after source/rebuild/process/owner verification |

## WSL root Podman store — existing, not new installations

Store /var/lib/containers/storage measured5.4G. Rootless Podman inventory is separately empty; do not mistake it for absence of root-owned services. All5retained containers were stopped at inspection. `podman system df`: images3.045GB, container writable layers1.988GB, volumes549.6MB. Its label “reclaimable” DOES NOT authorize removing retained environments or data.

| Retained container | Image/version or immutable digest | Reported image size |
| --- | --- | ---: |
| oasis-cosmos | mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview | 1.69GB |
| oasis-tron-tre | docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba | 278MB |
| oasis-wasmd | docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773 (v0.70.3) | 214MB |
| oasis-neo4j | docker.io/library/neo4j@sha256:9f75e8df4325a24f00fdd7a8c0bcce650a58375049b1058e496e8b43d6c36b37 (5.26.12) | 529MB |
| oasis-solid | docker.io/solidproject/community-server@sha256:125727897fde6e2225239a0748bf47d8fa559cac065d5694dc10f6428799e9f2 | 336MB |

Named volume /var/lib/containers/storage/volumes/oasis-solid-data measured360K. Two existing anonymous volumes measured517M and7.6M; exact owner/mount mapping still needs inspection before any cleanup. Neither deleted. Pin Cosmos image digest and backfill port/mount/runtime provenance for all rows before declaring inventory complete.

## Oracle — installed once, retained and stopped

2026-10-19 local conditional-write verification reused same container/volume/secret; activation now provisions additional OASIS_WRITE_RECEIPTS table in the authorized local schema (operation-ID/hash/exact CLOB result). Synthetic committed receipt rows removed by exact operation-ID+hash test cleanup; table retained for reuse, not a new database/runtime. Post-stop `/var/lib/containers/storage/volumes/oasis-oracle-data/_data` measured4,111,988KiB allocated with du -sk. Windows free71,207,342,080bytes; parent totals include this same volume, no additive attribution or reclaim claim. Local schema/source is NOT yet deployed conditional support; exact native evidence/open gates in ORACLE_CLIENT_PRIORITY.md. No downloads/install/new environment/reset.

Final measured state2026-10-19: official Lite image pulled ONCE at the pinned manifest below. Image config ID358b32468eddc329751c28007a535cbc661f1090ab5d8a9f02267cf44ef1e3b9, expanded1787986520bytes. ONE root Podman container oasis-oracle-free, loopback127.0.0.1:11521→1521,2CPUs/3GiBmemory/1GiBshm, bounded5MBlog. ONE named volume /var/lib/containers/storage/volumes/oasis-oracle-data measured4.0G allocated after clean shutdown; writable container layer5.75MB. Same store /var/lib/containers/storage, no separate archive/runtime copy. Podman secret oasis-oracle-pwd retained in root secret store; never publish/delete it independently of database ownership. Windows free72,648,433,664bytes after installation/tests, versus79,089,102,848before (difference6,440,669,184bytes is whole-system observed change, not exact Oracle attribution). Final whole root store du7.1G; shared layers/sparse allocation make parent/child/virtual totals non-additive.

Reusable runner Scripts/TestHosts/Run-ReusableOracleTests.ps1 only accepts existing exact image/container/port and lease, never pulls/creates/resets. Starts SAME stopped container, reads SAME Podman secret in memory, runs cached native tests and stops it only if started here and terminal success; failure preserves originals. Original-journal reconciliation is an explicit -ReconcileOriginal mode and never resubmits saved rows. Manual lifecycle: `wsl -d Ubuntu -u root -- podman start oasis-oracle-free`; `wsl -d Ubuntu -u root -- podman stop --time 30 oasis-oracle-free`. Container currently EXITED after clean stop, volume preserved. Cleanup only after future explicit retirement/credential/evidence/journal review: this retained environment is not disposable per test. No automatic volume/image removal. Authorized test schema was initialized in local FREEPDB1 using SYSTEM; least-privilege application-user/hosted wallet/IAM acceptance remains OPEN. Not an OCI Object Storage environment.

2026-10-19 allocation intent, BEFORE pull: registry authenticated manifest proves7compressed layers totalling882727776bytes (~883MB decimal), linux/amd64 pinned digest below. No registry token retained/logged. Destination is EXISTING WSL Ubuntu root Podman store /var/lib/containers/storage; reuse it, not rootless duplicate. Windows free79,089,102,848bytes; Linux root allocated used74,060,423,168bytes before pull. Expanded image and future data size still unknown. User approved one runtime; proceeding with exactly this digest, no standalone archive. Update this section after pull/initialization before claiming total footprint.

User approved ONE reusable Oracle test runtime. Inspected root AND rootless stores: no existing Oracle image/container. Remote manifest inspection only, no layers pulled. Candidate official Lite23.26.0.0 linux/amd64 manifest digest sha256:2b54beb4e3cfd0434918c2e43bc1f88da78ecc5e9c16fc7d8bbeb62ecbb62e11. Actual compressed/expanded/database sizes not measured yet; earlier10–15GB total was an ESTIMATE, not allocation or accounting. Record exact storage location, before/after measured usage, one container/volume and safe stop/reuse commands before running. Never retain a second archive/image/setup copy for repeated tests.

## Cleanup / remaining inventory work

### GoogleCloud/Firestore retained runtime/output backfill — 2026-10-19

Existing C:/Users/david/.cache/firebase/emulators/cloud-firestore-emulator-v1.22.0.jar measured136,707,194logical bytes, observed SHA2569B6498B7F62714D67F48F59B3818883CD682DBCD46B9F59511DE81C97BB5166C (local observed identity, not a newly downloaded/vendor-signature certificate). Same acknowledged C:/Source/OASIS/TestResults/GoogleCurrent/retained-export measured373bytes/3files, preserved known checkpoint fixture. No second jar/export/runtime/data directory. Completed same retained checkpoint/history/regression runner terminal0, journals0, no18080–83listeners/owned emulator stopped. No downloads/install/reset/deletion of retained export.

Existing fixed paths under C:/Source/OASIS: Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests/bin50,482,596logical bytes/223files; corresponding GoogleCloudOASIS.IntegrationTests/bin91,635,637bytes/258files; TestResults/GoogleCurrent849,207bytes/15files INCLUDING373-byte export. Parent/child totals non-additive. Reuse same outputs/cache; bin regeneration from retained source/dependencies only after owned processes stop. Reports/export/original evidence are not bulk disposable. Windows free70,691,012,608bytes whole-machine, not attributable allocation/reclaim. Runtime import/readback/conditional cleanup tested, not full hosted IAM or every durability scenario.

### DynamoDB retained history/data follow-up — 2026-10-19

Same reviewed runtime at C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/runtime, fixed persistent database directory C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/retained-history-data measured1,282,048logical bytes/1file. These are retained dependencies outside active checkout: do not delete that old checkout's TestResults wholesale. Fixed C:/Source/OASIS/TestResults/DynamoHistoryCurrent measured1,011,207bytes/23files, including one necessary compact byte-identical failed TRX plus exact original reconciliation inventory. No source/runtime clone/archive copy/new installation/download. Seven known synthetic records conditionally removed only after full original readback/digest inventory and independent absence; no database reset/physical disk reclaim. Same owned Java daemon stopped, no18000/18001listeners, journals0. Windows free70,695,403,520bytes whole-machine observation. Keep runtime/database/protected evidence; compact failed evidence is not an alternative environment. Future retirement requires original-outcome/ownership checks and existing rebuild/provisioning runbook.

### ActivityPub fixed build/evidence backfill — 2026-10-19

No installed service/runtime or download: constructor/security tests use bounded in-process loopback TCP listeners disposed after each case; existing live public-read fixtures require no account/token/local server. Existing fixed paths under C:/Source/OASIS measured logical bytes: Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests/bin46,188,514bytes/189files; corresponding ActivityPubOASIS.IntegrationTests/bin45,428,627bytes/187files; TestResults/ActivityPubCurrent302,680bytes/5files. Reuse these same outputs; bins can be rebuilt from retained source/cache after confirming no owned active build/test process. Compact red/current reports are evidence, not bulk disposable cache. No per-run directory, archive, clone, SDK installation or deletion. Windows free70,706,847,744bytes whole-machine observation, not allocated/reclaimed attribution.

### SOLID startup-binding verification — 2026-10-19

Same root-Podman oasis-solid image58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a, configured127.0.0.1:13000, retained oasis-solid-data volume. Volume /var/lib/containers/storage/volumes/oasis-solid-data/_data measured364KiB allocated (du -sk). No account/Pod/client/runtime/image/volume recreation; existing protected synthetic identity reused. Owned resume completed6native cases then stopped; SIGTERM exceeded30seconds and Podman used SIGKILL, state exited137, journals0. Final-source rerun restart command was policy-rejected BEFORE launch; approval pending, do not bypass. No clean-shutdown/durability claim.

Fixed existing output locations measured logical bytes: Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests/bin49,347,324bytes/191files; corresponding SOLIDOASIS.IntegrationTests/bin49,179,848bytes/191files; TestResults/SolidCurrent401,687bytes/12files. All relative to C:/Source/OASIS. The report folder includes protected credentials/original evidence and is NOT disposable. Reuse these same compiled output directories; future bin removal requires no active build/test process and can be regenerated from retained source/dependencies. Do not count these logical DLL copies as separate installed environments or add nested totals twice. Windows free70,747,381,760bytes is whole-machine observation, not attributed usage/reclaim. Test-project reference graphs refreshed solely from C:/Users/david/.nuget/packages as explicit offline source; no installation/download/duplicate environment.

### Existing-resource backfill — 2026-10-19 follow-up

User explicitly requires this inventory for ALL previously downloaded/setup resources, not only Oracle or future installations. Read-only inspection below reused the existing environments; no installation, download, restart, deletion or reclaim occurred. All six root Podman containers were stopped at inspection. Configured bindings below are not evidence of running listeners. Cosmos has existing wildcard bindings: review and restrict before any future start; this inventory did not alter them.

| Existing container | Configured host bindings | Persisted data owner |
| --- | --- | --- |
| oasis-cosmos | 0.0.0.0:1234,8080,8081 | No mounts; writable container layer must be preserved/reviewed, not assumed disposable |
| oasis-wasmd | 127.0.0.1:26657 | No mounts; writable container layer may hold original chain state |
| oasis-tron-tre | 127.0.0.1:50051,50052,9090 | No mounts; preserve original writable layer until reconciliation |
| oasis-neo4j | 127.0.0.1:17687 → 7687 | Root volume db5b0ed7dfe7b115c845cf7efe40afd8d4d8b4b594147bb03ba74a5c34af4d4b → /data; volume 3a3e642a2f6d8224bec840b2bcb4805bb6eb8d7bebb6ead17bc84baa2c5d1909 → /logs |
| oasis-solid | 127.0.0.1:13000 | Root named volume oasis-solid-data → /data |
| oasis-oracle-free | 127.0.0.1:11521 → 1521 | Root named volume oasis-oracle-data → /opt/oracle/oradata |

Named/anonymous volumes above live under `/var/lib/containers/storage/volumes/<name>/_data`. This resolves the previously unknown Neo4j anonymous-volume ownership; neither volume was removed. Image identities/digests remain in the earlier tables. Reuse these exact containers rather than create replacements. Preserve credentials outside this report; never publish full container environment/configuration dumps containing secrets.

Additional measured Windows logical file bytes (parent/child totals elsewhere are non-additive):

| Exact location | Bytes / files | Retention and regeneration |
| --- | ---: | --- |
| C:/Source/OASIS/Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests/bin | 45,434,122 / 198 | Reusable compiled test output; rebuild from project and retained dependencies after confirming no test/build process uses it |
| C:/Source/OASIS/Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests/bin | 51,279,951 / 206 | Reusable compiled test output; same process/source verification before future cleanup |
| C:/Source/OASIS/TestResults/OracleCurrent | 1,537,223 / 24 | Compact current evidence/lease and possible reconciliation journals; NOT blanket disposable or equivalent to bin output |

`C:/Users/david/AppData/Local/Android/Sdk` was absent; `.android/avd` had zero enumerated files. This does NOT establish absence of Android SDKs elsewhere. Windows free at this follow-up: 71,658,201,088 bytes, a whole-machine observation, not provider allocation attribution. Remaining unknown locations/allocated VHDX footprint and repository-wide build-output ownership still need backfill; do not claim complete disk reconciliation.

No automatic removal from this document. First resolve absolute targets, process/mount owners, source/uncommitted files, credentials, original ledgers, pending mutation journals and evidence. Disposable acknowledged synthetic fixtures must be reconciled and removed through their existing conditional cleanup owner; unknown outcomes are protected. Compact reports remain. Removing Linux data does not necessarily shrink the Windows VHDX; compaction requires separately authorized WSL shutdown.

Still to backfill: installed .NET/JDK/Node SDKs and Android SDK/AVD ownership, all provider test data directories and bound ports, exact container volume/mount mappings/digests, repository bin/obj outputs, temporary downloads, product-managed caches and VHDX allocated footprint. Existing cleanup ledger remains the historical source for previous downloads/removals; do not reinterpret shared/user installations as task-owned.
