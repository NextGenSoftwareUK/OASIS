# Reusable environments and disk inventory

Measured 2026-10-19. This is a backfill in progress, not a claim that all machine usage is accounted for. Every subsequent setup/download must update this inventory AND the chronological WORKSPACE_CLEANUP_LEDGER.md. Do not duplicate an existing suitable runtime. Record exact version/digest, location, measured image/runtime AND data sizes, process/service/port owner, purpose, reuse/start command, retained credentials/evidence and cleanup prerequisites. Never record secret contents.

Windows figures below are logical file bytes from bounded recursive enumeration; WSL `du -sh` figures are allocated approximate binary units. Podman image sizes are reported estimates and layers can be shared. Parent totals INCLUDE their children: do not add both. WSL files also occupy the Windows VHDX: do not count VHDX and Linux contents twice. Windows free disk at the first snapshot:79,090,307,072bytes. No deletion or reclaim is claimed.

## Windows retained resources

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

## Oracle — approved but NOT downloaded yet

User approved ONE reusable Oracle test runtime. Inspected root AND rootless stores: no existing Oracle image/container. Remote manifest inspection only, no layers pulled. Candidate official Lite23.26.0.0 linux/amd64 manifest digest sha256:2b54beb4e3cfd0434918c2e43bc1f88da78ecc5e9c16fc7d8bbeb62ecbb62e11. Actual compressed/expanded/database sizes not measured yet; earlier10–15GB total was an ESTIMATE, not allocation or accounting. Record exact storage location, before/after measured usage, one container/volume and safe stop/reuse commands before running. Never retain a second archive/image/setup copy for repeated tests.

## Cleanup / remaining inventory work

No automatic removal from this document. First resolve absolute targets, process/mount owners, source/uncommitted files, credentials, original ledgers, pending mutation journals and evidence. Disposable acknowledged synthetic fixtures must be reconciled and removed through their existing conditional cleanup owner; unknown outcomes are protected. Compact reports remain. Removing Linux data does not necessarily shrink the Windows VHDX; compaction requires separately authorized WSL shutdown.

Still to backfill: installed .NET/JDK/Node SDKs and Android SDK/AVD ownership, all provider test data directories and bound ports, exact container volume/mount mappings/digests, repository bin/obj outputs, temporary downloads, product-managed caches and VHDX allocated footprint. Existing cleanup ledger remains the historical source for previous downloads/removals; do not reinterpret shared/user installations as task-owned.
