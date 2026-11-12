# Current resource and cleanup ledger

12 November canonical HoloOASIS Development integration used one temporary Git index, removed after creating the exact scoped commit. No local build, dependency download or fixture was created.

12 November combined canonical HoloOASIS run 43178174352 passed both mode cleanup gates and completed successfully on its disposable Windows runner. No local build output, fixture or raw test report was created.

12 November verified hApp artifact 13925305621 in memory. Only the 1,015,314-byte shipping bundle and its manifest were written to tracked paths. No ZIP, extracted temporary file, local report or build directory was retained.

12 November hApp producer run 43176148764 built and tested on a disposable runner. The verified bundle is retained in GitHub Actions for seven days for combined provider verification. No local download, report or build output was created.

12 November provider run 43175584012 and hApp producer 43174295696 used disposable hosted runners. The provider stopped after 19 checks per mode; the hApp producer stopped compiling its test payload. The hApp follow-up and scoped Development integration used temporary Git indexes, and the hApp change used one temporary source file. All were removed. No local build outputs or downloads were created.

12 November provider run 43174783071 stopped at compilation on its disposable Windows runner. No local build output, download, or report was created.

12 November canonical HoloOASIS run 43173783746 reached 18 checks in both modes on a disposable Windows runner. The hApp dependency candidate used a temporary Git index and one temporary source file, both removed. The hApp build and conductor tests run on disposable hosted CI. No local build or test downloads were created.

12 November canonical HoloOASIS run 43172617448 reached 17 checks in both modes before owner export failed. It used the disposable Windows runner and no local build output, download or report.

12 November canonical HoloOASIS run 43171424613 reached 10 checks per mode before the holon-ID result failed. It used disposable Windows CI and created no local build output, download, or report.

12 November canonical HoloOASIS run 43170248569 reported 14 checks per mode before metadata selection failed. Superseded run 43166769837 reached the 20-minute hosted limit. Both used disposable hosted Windows runners. No local build output, download, or report was created.

12 November canonical HoloOASIS run 43165294724 was canceled at the hosted job limit after test execution started. Explicit fixture teardown could not be confirmed before cancellation. The runner was disposable. No local test download, output or report was created. Future runs cap a hung test without collecting a process dump.

12 November Holo Desktop conformance run 43163893639 exercised both modes on disposable Windows CI. Both passed avatar and avatar-detail checks and stopped at graph save. No local build output, download, or report was created.

12 November Holo Desktop conformance run 43162859637 ran both modes on the disposable Windows conductor. It stopped at synchronous avatar ID and asynchronous avatar-detail ID lookups. No local build output, download, or report was created.

12 November Holo Desktop conformance run 43161883055 stopped at avatar save acknowledgement in both modes. It used the disposable Windows runner and conductor fixture. No local build output, download, or report was created.

12 November Holo Desktop runs 43158581111 and 43159610649 used disposable hosted Windows runners. The first stopped before fixture startup. The second passed the real conductor lifecycle and its teardown. No local test downloads, build outputs or reports were created.

11 November Firebase run 43105666326 verified exact cleanup of the concurrent-delete fixture and both lifecycle modes. Native RTDB was empty before teardown. The owned emulator, fixture and reports were removed. No local artifacts were created.

11 November Firebase run 43104762300 verified an empty native RTDB namespace after both storage modes and removed the owned emulator, fixture and reports. No local artifacts were created.

11 November Firebase runs 43102301153 and 43102752702 logged removal of the owned RTDB emulator, fixture and reports. The second run passed the focused native graph and stale-delete test. The lifecycle then stopped at classification of the hidden tombstone. No local build, download or raw report was created.

11 November Firebase run 43101286060 logged removal of the owned RTDB emulator, fixture and reports after the native deletion acknowledgement failure. No Windows build, download or raw report was created.

11 November superseded Dragonfly run 43094237991 finished cancelled. GitHub reports no executed steps for job 130778844756. There is no recorded fixture creation in that run and no local runtime, build or report was created.

11 November Base wallet and decoder qualification ran on a disposable GitHub runner. The public read used no signer and created no chain fixture. No local download, build output or raw test result was created.

11 November cleanup check. Automatic approval review blocked the leased recursive removal command before execution. The exact owned MongoNativeMetadataBuild and ConvexResponseDeadlineBuild directories remain, with 99 generated files each and a combined 10,317,999 bytes. Both are untracked and contained no reparse points at inspection. No files were deleted or created by the attempt. C: had 34,100,469,760 free bytes at the read-only check.

11 November live inventory recheck used in-memory HTTPS responses. No local build, download file, test environment or generated result was retained. C: showed 34,135,076,864 free bytes during the recheck.

11 November BNB NFT run 43092221100 confirmed removal of the native node and owned fixture, including both test keys, chain state and npm dependencies. No local outputs were created.

11 November BNB deployment run 43092941435 removed the BSC binary, data, Hardhat outputs, compiler cache, temporary key and npm dependencies with the disposable fixture. No local build or download was created.

11 November Valkey run 43091045329 verified zero keys, removed its container, volumes, image and fixture. Firebase run 43090501206 stopped and removed the RTDB emulator, its JAR, fixture and checkpoints after graph-save failed. Both ran on disposable GitHub runners. No Windows build or download was created.

11 November Firebase runs 43089219847 and 43087789287 were cancelled while queued after combined run 43090501206 was scheduled on the newer revision. No local artifacts were created.

11 November BNB Development BootLoader run 43086956668 confirmed removal of the BSC node fixture and npm dependencies. No local outputs were created.

11 November BNB BootLoader run 43085641543 confirmed cleanup of the owned BSC node, binary, chain data, temporary DNA, signing material and npm dependencies. The disposable runner retired its build workspace. No Windows build or download was created.

11 November BNB BootLoader run 43084767090 confirmed removal of the BSC node fixture and npm dependencies after its dependency build failed. No Windows build or runtime was created.

11 November BNB run 43084066216 confirmed removal of the official binary, private-chain data, temporary keys, storage checkpoints and npm dependencies from the disposable runner. No Windows build or download was created.

11 November BNB run 43083089351 stopped its owned BSC process after the failed write. The job ran on a disposable GitHub runner. The workflow now prints a cleanup confirmation after checking both the fixture and npm dependencies are absent. No Windows runtime, build or download was created.

11 November AvalancheGo run 43080136029 passed both modes and confirmed removal of the container, volumes, newly pulled image, npm dependencies and fixture directory. No Windows runtime, build or download was created.

11 November AvalancheGo run 43079640797 also removed its disposable resources after the startup failure.

11 November Avalanche storage uses a disposable CI node, contract deployments, npm dependencies and temporary credentials/checkpoints. The runner removes the container, volumes, newly pulled image, dependencies and fixture directory on exit. Cleanup confirmation is pending. No Windows runtime or build was created.

11 November Milvus run 43078055694 independently removed ten owned holon tombstones and verified three empty collections per mode. CI removed both containers, volumes, newly pulled images and fixtures. No local build, runtime or download was created.

11 November Milvus run 43077349886 also removed its disposable resources after the parent-query failure.

11 November Milvus storage uses separate disposable CI containers and temporary configuration/checkpoint directories. The workflow removes the containers, volumes, newly pulled images and fixtures on exit. Cleanup confirmation is pending. No Windows build or download was created.

11 November Memcached run 43075704036 verified removal of ten exact owned holon records and confirmed both daemons, runtime and checkpoints were removed. No local runtime, build or download was created.

11 November Memcached run 43074654661 also removed its disposable resources after the compile failure.

11 November Memcached runs 43073793620 and 43074034963 both confirmed removal of their daemons, runtime and checkpoint files after the managed checks. No Windows build or download was created.

11 November Memcached run 43072655062 removed its extracted runtime, archive and fixture after executable admission failed. The retry installs two runtime libraries on its disposable runner and clears package downloads before starting the daemon. No Windows runtime, build or download was created.

11 November Memcached native storage verification uses two disposable loopback daemons, a checksum-verified runtime and temporary checkpoints on CI. The workflow stops both processes and removes the runtime, archive and fixtures. Cleanup confirmation is pending. No local build or download was created.

11 November RavenDB run 43071621377 independently removed ten owned holon tombstones and verified both databases had zero documents. CI removed the container, volumes, newly pulled image and checkpoints. No local build, runtime or download was created.

11 November RavenDB run 43070985964 also removed its disposable resources after the readiness failure.

11 November RavenDB run 43070212406 confirmed removal of its container, volumes, image and checkpoints after HTTP readiness failed. No local runtime or build was created.

11 November RavenDB storage verification uses a disposable CI container, two databases, and checkpoint files. The runner removes the container, volumes, newly pulled image and fixture directory on exit. Cleanup confirmation is pending. No local build or download was created.

11 November ScyllaDB run 43069185380 independently removed ten owned holon tombstones and verified three empty tables in each keyspace. CI removed the container, volumes, newly pulled image and checkpoints. No local build, runtime or download was created.

11 November ScyllaDB run 43068194121 also removed its disposable resources after the readiness failure.

11 November ScyllaDB storage uses an owned CI container, two keyspaces, and disposable checkpoint files. The workflow removes the container, volumes, newly pulled image and checkpoint directory on exit. Cleanup confirmation is pending. No local build or download was created.

11 November Cassandra run 43066716817 independently removed ten owned holon tombstones and verified five empty tables in each keyspace. CI removed the container, volumes, newly pulled image and checkpoints. No local build, runtime or download was created.

11 November Cassandra run 43066102705 also removed its disposable resources after the compile failure.

11 November Cassandra storage verification uses a disposable CI container, two keyspaces and owned checkpoint files. The runner removes its container, volumes, newly pulled image and checkpoint directory on exit. Run 43065545986 confirmed removal of its Cassandra container, volumes, image and checkpoint directory after the graph-save failure. Candidate cleanup is pending. No local runtime, download or build was created.

11 November PlanetScale MySQL run 43063077214 reproduced the detail-by-ID failure and CI logged image removal.

11 November PlanetScale MySQL runs 43063702938 and 43064133424 confirmed disposal of their containers, volumes, newly pulled images and checkpoint directories. The full run independently removed ten exact owned tombstones and verified all three native tables empty per database. No Windows runtime, build or download was created.

11 November TimescaleDB run 43061942830 passed both storage modes and independently verified the empty tables. CI removed the owned container, volumes, newly pulled image and checkpoints. No Windows runtime, build or download was created.

11 November TRON and Polkadot CI cleanup. TRON job 130571986332 verified empty storage in both contracts and removed the owned TRE container and ledger. Polkadot job 130572555894 removed the pinned Binaryen downloads and tools directory. The WASM, runtimes, test reports and keys stayed on disposable runners. No local artifacts or uploads were created.

11 November Polkadot build CI. Job 130571109668 removed both Binaryen downloads after extraction and removed the owned Binaryen directory on exit. Rust remained confined to the disposable runner. No local files or uploads were created.

11 November TRON native CI. Job 130569680181 removed the owned container and ledger after the gRPC initialization failure. Build, Solidity tooling, bytecode and key material remained on the disposable runner. No local artifacts or uploads were created.

11 November TRE diagnostics CI. Job 130566647551 logged the owned container state and removed its container and ledger after the head-block probe failed. The Solidity compiler, bytecode and outputs remained on the disposable runner. No local artifacts or uploads were created.

11 November TRON fixture CI. Job 130564148374 removed the owned TRE container and ledger after readiness failed. The temporary Solidity compiler and bytecode were confined to the disposable runner. No local outputs or uploads were created.

11 November successful managed NEAR sandbox CI. Job 130558443335 confirmed an empty native contract, stopped the owned sandbox and removed the temporary ledger and reports. The build, SDK dependencies and ephemeral keys were confined to the disposable runner. No local artifacts or uploads were created.

11 November managed NEAR lifecycle CI. Job 130556061050 removed its owned sandbox and test report directory after the native regression failures. The build, compiler, npm dependencies and keys remained on the disposable runner. No local artifacts or uploads were created.

11 November managed NEAR provisioning CI. Job 130554821914 removed its owned sandbox directory after the RPC port mismatch. The npm packages and contract outputs stayed on the disposable runner. No local artifacts or uploads were created.

11 November native blockchain cleanup. Cosmos job 130550484963 independently verified empty contract storage and removed its wasmd container and ledger. Solana job 130546850845 removed its owned validator fixture after both modes passed. The builds, reports, downloads and ephemeral secrets stayed on disposable runners. No local artifacts or uploads were created.

11 November NEAR sandbox avatar cleanup. Job 130551514327 stopped the owned sandbox and removed its temporary ledger directory. The locked npm install, WASM and compiler downloads stayed on the disposable runner. No local artifacts or uploads were created.

11 November Cosmos contract deployment CI. Job 130548908467 removed the owned wasmd container and its ledger after native contract verification. Rust, package downloads, build outputs and the image were confined to the disposable runner. No local artifacts or uploads were created.

11 November Solana CI. Job 130530733371 passed both logical cleanup checks but was canceled while waiting for its validator to stop. The ledger, signer, package and build were confined to the disposable GitHub runner. The job did not confirm explicit fixture-directory removal. No local downloads or outputs were created.

11 November successful Spring storage CI. Job 130543241083 independently verified chain table cleanup, stopped the owned node and wallet, and removed the disposable fixture directory including its keys and TRX. No local artifacts or uploads were created.

11 November Spring diagnostics CI. Job 130542638348 logged owned startup diagnostics, stopped the fixture processes and removed the wallet, keys and chain. No local files or uploads were created.

11 November Spring native storage CI. Job 130541682935 removed the owned fixture directory and its ephemeral keys, wallet and chain after the account provisioning failure. The pinned package was removed after installation on the disposable runner. No local outputs or uploaded artifacts were created.

11 November successful Antelope build. Job 130539157022 removed the CDT download and published an 8,222-byte ABI/WASM archive with one-day retention. Verified the archive in memory and retained only the two tracked deployment files. No local compiler, archive or temporary files were left.

11 November Antelope build CI. Job 130537519606 verified and installed the pinned CDT package on its disposable runner and removed the download. It failed in the WASM linker and did not upload ABI or WASM. No local installation or build outputs were created.

11 November successful Neon compute CI. GitHub job 130527402749 removed the owned compute, pageserver, three safekeepers, storage broker and their network. The disposable runner held the images, database, build and test receipts. No local artifacts or uploaded reports were created.

11 November Neon native compute CI. Job 130523260874 removed the official compute, pageserver, three safekeepers, storage broker and their network after the fixture cleanup assertion failed. The database, build and receipts stayed on the disposable runner. No local outputs were created.

11 November public provider inventory publication. Verified live HTML and registry names in memory, reused existing site checkouts and ran no local builds or package installs. No downloads, test results, temporary files or build outputs were retained.

11 November Convex companion recovery CI. Job 130512352307 passed and stopped the owned Convex engine. The archive, test database, ephemeral keys, CLI install and build ran on a disposable Linux runner. No upload or local output was created.

11 November Supabase pagination CI. GitHub job 130509010214 passed all three listing cases and removed the PostgreSQL container. The temporary PostgREST services, archive, seeded rows, ID manifest and build were confined to the disposable runner. No local artifacts or uploads were created.

11 November Supabase native PostgREST conformance. Corrected GitHub job 130506351774 passed both modes with exact fixture cleanup and removed the PostgreSQL container. The PostgREST processes, database, download, reports and build outputs were scoped to disposable CI. No local artifacts or uploaded reports were created.

11 November Supabase credential isolation CI. Job 130503718549 ran two PostgREST services on a disposable Linux runner, passed the native credential check, and removed the PostgreSQL container. The download, database and build stayed on the runner. No local artifact or report upload was created.

11 November Neo4j Aura adapter CI. GitHub job 130499437747 confirmed the dedicated Neo4j Community container stopped and was removed after both lifecycle cases passed. The disposable Linux runner held the image, database and build outputs. No local artifacts or uploads were created.

11 November Neo4j V2 harness CI. Job 130497281183 built and ran the standalone harness on a disposable Linux runner. GitHub confirmed the Neo4j service container was stopped and removed. No upload, local download, container, database or build output was created.

11 November Neo4j V2 native CI. Job 130494942531 ran the digest-pinned Neo4j Community container and test build on a disposable GitHub Linux runner. No upload, local image, database, download or build output was created.

11 November Linux IPFS separate-process CI. GitHub job 130493008518 confirmed that the owned Kubo 0.43.1 daemon stopped after all three test processes passed. The archive, repository, lock and build outputs ran on a disposable Linux runner. No upload or local output was created.

11 November IPFS separate-process CI. Baseline job 130489021464 and corrected job 130489798827 both stopped the owned Kubo daemon. The lock file, repository, download and build outputs were confined to disposable Windows runners. No uploads or local artifacts were created.

11 November IPFS independent writer CI. Baseline job 130484784884 and corrected job 130486013043 both stopped their owned Kubo process. Downloads, temporary repositories and builds ran on disposable Windows runners. No uploads or local outputs were created.

11 November IPFS concurrent publication CI. Both the baseline and corrected runs stopped their owned Kubo process. GitHub job 130482122676 passed the corrected test. The isolated repositories, downloads and builds ran on disposable runners and no artifact upload was configured. No local artifacts were created.

11 November IPFS restart CI. GitHub job 130477987590 confirmed that both owned Kubo processes stopped. The disposable Windows runner held the archive, encryption key, repository and build outputs. No artifact upload was configured and no local outputs were created.

11 November IPFS native activation CI. The official archive, offline repository and build outputs are scoped to the disposable GitHub runner. GitHub job 130474978651 completed successfully after the daemon shutdown step. No artifact upload was configured. No local runtime, download, fixture or build output was created.

11 November IPFS activation probe. Automatic policy rejected the combined disposable download, daemon, test and teardown command before launch. No probe directory, archive, repository, daemon, build output or test report was created.

11 November SQL Server avatar wallet verification. Every run dropped its disposable database and stopped and removed the dedicated LocalDB instance. The final leased clean left zero binary files and 5,868,047 bytes of restore and intermediate metadata in TestResults/HyperDrive-Providers/SqlServerWalletBuild. Automatic policy rejected recursive removal of that owned output directory. No database files, runtime installation or raw report remain. C: had 42866348032 bytes free.

11 November public site recheck. Published static copy changes through the existing site repositories and verified public responses in memory. No local build, runtime installation, download file or test artifact was created.

11 November MongoDB graph transaction verification. Reused installed MongoDB 8.2.1 and the same leased build directory as the metadata test. The test gracefully stopped its disposable one-member replica set and removed the database and log. An independent check found no owned fixture directory and only the pre-existing MongoDB process. Leased dotnet clean left zero files in bin and 5,159,754 bytes of restore and intermediate metadata in `TestResults/HyperDrive-Providers/MongoNativeMetadataBuild`. C: had 42,921,754,624 bytes free at the shared-machine check.

11 November MongoDB metadata verification. Reused installed MongoDB 8.2.1 and the .NET package cache. The test gracefully stopped its isolated server and removed its temporary database directory and log. An independent process and path check found only the pre-existing service and no owned fixture directory. Leased dotnet clean left zero files in bin. The isolated `TestResults/HyperDrive-Providers/MongoNativeMetadataBuild` tree retains 5,159,754 bytes of restore and intermediate metadata. C: had 42,934,276,096 bytes free at the shared-machine check.

11 November Polygon public wallet verification. Reused the .NET runtime and package cache. No SDK installation, chain fixture or raw test report was created. Leased dotnet clean left zero files in bin. The isolated `TestResults/HyperDrive-Providers/PolygonPublicWalletBuild` tree retains 6,070,952 bytes of restore and intermediate metadata. C: had 42,947,489,792 bytes free at the shared-machine check.

11 November Arbitrum public wallet verification. Reused the .NET runtime and package cache. No SDK installation, chain fixture or raw test report was created. Leased dotnet clean left zero files in bin. The isolated `TestResults/HyperDrive-Providers/ArbitrumPublicWalletBuild` tree retains 6,073,521 bytes of restore and intermediate metadata. C: had 42,965,209,088 bytes free at the shared-machine check.

11 November SQLite wallet encryption verification. The focused test disposed both provider instances and its independent SQLite connection, then deleted its temporary database and sidecars. Leased dotnet clean left zero files in the redirected bin tree. No SDK install or raw report was created. The isolated `TestResults/HyperDrive-Providers/SQLiteWalletEncryptionBuild` tree retains 6,394,352 bytes of restore and intermediate metadata. C: had 42,987,954,176 bytes free at the shared-machine check.

11 November Convex response deadline verification. The real loopback TCP listener and client sockets were disposed at test completion. No hosted deployment, SDK installation or raw test report was created. Leased dotnet clean left zero files in bin. The isolated `TestResults/HyperDrive-Providers/ConvexResponseDeadlineBuild` tree retains 5,158,245 bytes of restore and intermediate metadata. C: had 43,097,292,800 bytes free at the shared-machine check.

11 November Supabase credential isolation verification. Reused the .NET runtime, package cache and one redirected output tree for the full unit run and the five-case metadata rerun. Leased dotnet clean left zero files in bin. No test environment or raw report was created. The isolated `TestResults/HyperDrive-Providers/SupabaseCredentialBuild` tree retains 5,158,051 bytes of restore and intermediate metadata. C: had 43,098,398,720 bytes free at the shared-machine check.

11 November Polkadot public wallet verification. The isolated npm install added 62 packages. Teardown removed all 62, cleaned the isolated npm cache, and left zero files in the redirected bin tree after leased dotnet clean. No chain fixture or raw report was created. Automatic approval review rejected recursive removal of the exact owned `TestResults/HyperDrive-Providers/PolkadotPublicWalletBuild` directory as blocked by policy. The command did not run. The measured remainder was 54,145,626 bytes of build and npm metadata and copied bridge inputs. C: had 43,110,428,672 bytes free at the shared-machine check.

11 November TRON public wallet verification. Reused the existing .NET runtime and package cache. No new SDK environment, chain fixture or raw test report was created. Leased dotnet clean left zero files in the redirected bin tree. The isolated `TestResults/HyperDrive-Providers/TronPublicWalletBuild` tree retains 5,931,676 bytes of restore and intermediate metadata. C: had 46,839,316,480 bytes free at the shared-machine check.

11 November Cosmos Hub wallet verification. Reused the existing Node runtime, locked CosmJS bridge dependencies and .NET package cache. No new SDK environment, chain fixture or raw test report was created. Leased dotnet clean left zero files in the redirected bin tree. The isolated `TestResults/HyperDrive-Providers/CosmosPublicWalletBuild` tree contains 5,863,490 bytes of restore and intermediate metadata. C: had 47,056,670,720 bytes free at the shared-machine check.

11 November NEAR mainnet wallet verification. The isolated npm install added 68 packages. Teardown removed all 68 and cleaned the isolated npm cache. Leased dotnet clean removed all compiled files from the redirected bin tree. No chain fixture or raw report was created. Automatic approval review rejected recursive removal of the exact owned `TestResults/HyperDrive-Providers/NearPublicWalletBuild` directory as blocked by policy, so that command did not run. The measured remainder was 5,981,009 bytes in restore metadata, copied bridge inputs and npm metadata. C: had 47,085,985,792 bytes free at the shared-machine check.

BootLoader missing-DNA regressions at a41551c70f passed 4/4 under the shared lease. Confirmed RegistrationTests/current-artifacts absent after teardown. Compact result retained in RegistrationTests/fixed-result.json.

10 November (America/Cancun) BootLoader error-propagation build at 820bebbecf passed with 0 errors and 2,139 warnings; compile duration 8m17s. Restore and build ran under the shared lease with isolated packages, HTTP cache, scratch and outputs. The result receipt confirms both exit codes zero and cleanup true. Independently confirmed current-artifacts absent. Retained only the compact receipt. Live registration verification remains open.

Registration error build recovery: session 92516 was missing on resume, with no matching live build process and no recoverable exit result. Under the shared provider build lease, removed 3,559,058,057 bytes from the exact owned priority-errors-8e51c7ab891b4cf7a2a946b4af24ac57 artifact/cache directory and confirmed it absent. The compile outcome for 1626bac589 and 3d36eed517 remains unverified.

10 November BootLoader graph build at 97a7c75829: isolated NuGet restore and .NET 10.0.401 build succeeded under the shared lease (0 errors, 2,139 warnings, 9m35s compile). Verified removal of priority-diagnostics-b33e85a055c14feb8f19ebeba824b52e, including packages, HTTP cache, scratch and redirected build outputs. C: measured 55.49 GiB free afterward. No real backend test ran.

10 November isolated BootLoader restore: stopped the owned restore after several minutes without diagnostics or completion. Compilation did not start. The shared lock was released and the task package, HTTP cache and scratch root priority-restore-20514b2d23f04cdbb226df25a93c5a58 was confirmed absent after cleanup.

10 November priority BootLoader compile: reused bundled PowerShell 7.6.9 and the shared provider build lock. The existing restore manifest refers to 232 package versions without package metadata in its configured folders. Build and project clean both stopped on NETSDK1064 for AWSSDK.DynamoDBv2 3.7.400.15. Confirmed zero files under BootLoader bin afterward. No download, restore or raw report created.

10 November urgent disk cleanup: paused provider testing. Leased project cleanup removed 297,278,724 bytes of generated Debug binaries from BigQuery, Dragonfly, KeyDB, Neo4j integration and unit, and Typesense test projects. Reference outputs were excluded. No downloads, environments or raw reports were created for this cleanup. C: measured 40.77 GiB before and 41.09 GiB after on the shared machine.

10 November Garnet fixture guard: Windows skipped the Linux-only case before backend activation. No runtime, database or raw report allocated. Leased project clean reduced private test outputs from 51,555,032 B to 852,532 B with reference cleanup disabled.

10 November EdgeSQLite enumeration: temporary SQLite fixture and sidecars are removed by passing test disposal. No downloads or raw reports. Leased project clean reduced private test outputs from 41,226,988 B to 194,220 B. Reference outputs were excluded.

10 November 2026 SQL Server concurrency verification: reused the existing dedicated oasis_hyperdrive_provider_test LocalDB fixture and integration project output under C:/Source/OASIS2-hyperdrive/Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.IntegrationTests. The leased test finished 1/1, including fixture teardown, and MSSQLLocalDB stopped. No new download, environment or raw report was requested. Automatic approval review rejected the exact owned bin directory removal command as blocked by policy. Direct removal did not run. The project's leased dotnet clean later removed 49,987,208 B of generated output (50,812,083 B before, 824,875 B after), with project-reference cleanup disabled. Remaining files support incremental build and package restore. LocalDB was confirmed stopped before cleanup. C free at check: 27,893,899,264 B (shared machine).

NuGet restore also reuses shared C:/Users/david/AppData/Local/NuGet/v3-cache and C:/Users/david/AppData/Local/Temp/NuGetScratch (paths confirmed by dotnet nuget locals; sizes/individual historical ownership unmeasured). They are shared caches, not owned disposal targets; no broad removal performed.

PocketBase28October outcome:15 regressions observed red, final24/24 maintained unit cases pass; no native pass claimed. Fixed producer bin7,416,440/obj926,802 B and unit bin46,255,098/obj993,421 B reused/created from existing cache; retained reusable, no download/runtime/service/database/raw report. Safe regeneration via documented bounded cached builds, remove only after owned build/test handles stop and outputs no longer needed. C free104,204,275,712 B after checks (shared-machine observation). Native/full-interface gates remain in DOCUMENT_FAMILY_BASELINE.md; unrelated Core/DNA/BootLoader work preserved.

Appwrite28October outcome:13 targeted regressions observed red, final19unit cases pass. No service/download or raw report retained. Fixed producer bin7,414,248/obj925,194 B and unit bin46,246,134/obj983,170 B created/reused from existing cache, retained reusable. Regenerate with bounded cached builds under provider lease; remove only after owned builds/tests stop and if no longer needed. Native/full-interface gates remain open; details in DOCUMENT_FAMILY_BASELINE.md. Matrix/report checks pending this checkpoint; unrelated work remains unstaged.

28October native checkpoint: RTDB JAR4.11.2 actual34,495,935 B/hash verified; help passes on retained Java21. Launch rejected by execution policy; no owner file/19000listener, no retry/bypass. Firebase binding builds with23controls+2skipped, no native pass. S3 CI52/52 and2post-suite-empty cases verified; artifact3,205 B retrieved once, raw TRX4,077 B compressed/decompressed-verified to1,244 B and removed. Compact summary preserves source/artifact/provider-alias identities; see S3_COMPATIBLE_STORAGE_BASELINE.md. Last C free105,215,623,168 B (shared-machine observation). A later bulk shell documentation/rollover command was also rejected before execution; used explicit reviewed documentation patches instead, with no archive changes or cleanup in that command.

Updated 2026-10-27. Owner: HyperDrive provider task. Use [CURRENT_STATUS.md](CURRENT_STATUS.md) and [current inventory](ENVIRONMENT_INVENTORY.md), not thousands of historical entries, to resume.

## Current rules

- Reuse `C:/Source/OASIS`, existing SDK caches, runtimes and fixed test directories. No duplicate setup or per-run environments.
- Before/alongside resource use, record exact identity/path, version/digest, purpose, ownership, size (or unmeasured), process/port, retention and safe recreation here/in the inventory.
- Preserve source, user/other-agent data, credentials, unresolved outcomes and shared dependencies. A stopped service is not disposable.
- Validate resolved targets and ownership before removal; use literal paths. Stop only owned processes and verify teardown.
- New raw evidence belongs in `TestResults/HyperDrive-Providers/`; docs contain summaries, hashes and paths. Keep only current/previous compact reports rather than unbounded run directories.
- Retain at most ten concise milestones here. Append older entries verbatim to the matching archive; do not rewrite/delete history.

## Current lifecycle and cleanup state

| Resource | State / cleanup and recreation |
|---|---|
| Dynamo evidence `TestResults/AWSProviderEvidence` | Reusable; moved JAR hash verified, same named container rebound, no old-root recreation. DB43,008 B, folder~50.9 MB; all five tables empty. Stop, integrity-check/checkpoint/VACUUM when needed; refuse unknown fixture erasure |
| Podman runtimes/volumes | All listed inventory containers observed stopped; keep data, keys and original failure journals. Shared Neo/Mongo/CQL data must not be deleted by this task |
| NuGet/npm/tools and provider outputs | Inherited/reused; sizes partly unmeasured. Rebuild from existing cache/runner; no install/download in this checkpoint |
| SOLID/IPFS/EVM missing inputs | Missing is not permission to overwrite an existing identity. Preserve original store and record recovery/configuration gates |
| Other Node/Mongo processes | Ownership unverified; not stopped or cleaned |
| Archived docs | Retained documentation, moved without duplicating environments; exact pre-trim hashes in Archive/ARCHIVE_INDEX.md |
| Evidence relocation | Only unreferenced raw files may move; exact paths/hashes and retained-reference reasons are in EVIDENCE_LOCATIONS.md |

## Recent milestones

No broad filesystem or cache cleanup was performed here. Current free-space measurements are machine-wide observations, not proof of exclusive reclaim; inspect current disk state before substantial work.

Complete history: [Archive/WORKSPACE_CLEANUP_LEDGER.md](Archive/WORKSPACE_CLEANUP_LEDGER.md). Do not read it by default. Historical provenance/ownership is not retrospectively asserted complete.

Earlier 28October milestones are preserved verbatim in [the dated rollover](Archive/WORKSPACE_CLEANUP_LEDGER_CHECKPOINTS_2026-10-28.md); hashes in the archive index.

- S3 native28October retrieval: CI40754776833/job123050672281 completed success on b5a7906de. Retrieve existing artifact12845074648 once to fixed TestResults/HyperDrive-Providers/S3CompatibleCurrent/incoming; ZIP7,365B GitHub digest48b98487bf3896e12b389ad7934fd233a00d75d5d8344b473c5cf3eeddd08be4. Metadata/reports only; no local MinIO runtime or hosted AWS/QSS. Validate reports, TRX post-cleanup success and DLL/service identities before replacing current summaries; preserve failed/unknown evidence, compress owned TRX after verification.

- Convex28October outcome: official pinned backend binary213,980,664B, total215,216,349B/state628,958B/log605,847B; one NodeToolsSDK install53,750,184B, package/dependency versions and exact paths in ConvexCurrent/resources.json. Reused TypeScript5.5.4 via existing compiler; source companion junction makes no SDK copy. SourceZIP64,331,338B verified/removed, disposable45,554B package verified/removed; fixed bin/obj reused. Native52/52, five stale/missing/create conflicts,105-row paging and24controlled cases pass; all native rows0, owned backend stopped, credentials protected/preserved. Windows checkpoint rename denial at13/26 preserved; exact7fixture revisions/cipher hashes inspected then conditionally deleted,500metadata probes and fresh52checks passed, no retryshim. See CONVEX_STORAGE_BASELINE.md and NativeEvidence/conformance-convex-controls-current.json. Shared npm cache delta unmeasured; no broad cleanup. Final C free96,205,598,720B shared observation, not exclusive task attribution.

- Redis/Upstash28October pre-action: reuse existing Shared.KeyValueStorage and Redis bin/obj, .NET10.0.401 and cached StackExchange.Redis2.8.16; extract exact Redis observation/Lua/glob policy for REST reuse. No install/download/service/DB creation. Fixed paths under Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage{,.UnitTests}/bin, obj and Providers/Storage/NextGenSoftware.OASIS.API.Providers.RedisOASIS/bin, obj; generated size pending. One build lease; retain reusable outputs, disposable only after owned handles terminal, regenerate via cached bounded builds. C free95,846,227,968B shared observation. Owner: HyperDrive provider breadth session.

- Redis/Upstash28October outcome:6shared state-comparison controls and32existing Redis record/admission/version controls pass; cached Shared/Redis/consumer builds0errors. No install/restore/download/service/data/keys created. Exact reused bin/obj measurements and safe regeneration in KEY_VALUE_FAMILY_BASELINE.md; owned build/test processes terminal, outputs retained reusable. C free95,648,800,768B shared observation. Upstash implementation/native checks remain next, no new native proof claimed.

- Upstash REST28October pre-action: reuse existing Upstash provider/unit projects and Shared.KeyValueStorage outputs/cache; no SDK install required for real HTTP API. Fixed bin/obj under Providers/Cloud/NextGenSoftware.OASIS.API.Providers.UpstashOASIS and Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests; sizes pending. No configured UPSTASH environment names or retained protected test directory found at prior inspection. No service/database allocated yet. Native scratch service, if needed, will be one retained resource with protected credentials, explicit expiry and fixture cleanup, recorded before allocation. Owner HyperDrive provider breadth; source/keys preserved.

- Upstash native28October allocation plan: no existing configured environment/retained Upstash resource found. Use one official free72-hour scratch Redis via https://upstash.com/start-redis (official REST docs), no payment/account or local runtime/download. Protected resource root C:/Users/david/AppData/Local/OASIS/ReusableProviderTests/Upstash; response/credentials and persisted32-byte storage key CurrentUserDPAPI only, never source/evidence. Owner HyperDrive provider breadth. Reuse until expiry, never silently allocate replacement. Synthetic fixture records cleaned by shared suite; unknown outcomes retained, service expiry lifecycle recorded in compact receipt. Size/expiry actual pending.

- Upstash28October outcome: real hosted canonical52/52;28controlled cases pass. Native namespace0 before/after both modes; no retries, reset or uncertain outcomes. One official free72-hour scratch resource retained, expiry31October; DPAPI response/credentials and persisted key reuse same exact protected root. No SDK/runtime/download; cached restores only. Native source/PDB identities and exact reused output/resource sizes recorded in TestResults/HyperDrive-Providers/UpstashCurrent/{identities,resources,receipt}.json; original report hashes preserved. Later controlled-only test additions do not relabel native producer proof. Owned test/build handles terminal. Clean resource only after expiry/reconciliation; source/keys/evidence preserved meanwhile.

- Cloudflare KV28October pre-action: reuse existing provider/unit projects and shared encrypted storage outputs; no SDK/runtime/install/download/service/account/namespace allocated. Real REST API only; missing Cloudflare environment names at inspection, native credentials unconfigured. Fixed bin/obj under Providers/Cloud/NextGenSoftware.OASIS.API.Providers.CloudflareOASIS and Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.CloudflareOASIS.UnitTests; actual sizes pending. Existing .NET10.0.401/NuGet caches reused under whole-operation lease, generated outputs retained reusable; exact-output cleanup only after owned handles stop and task retirement, regenerate bounded cached builds. C free95,577,460,736B shared observation. Owner HyperDrive provider breadth.

- Cloudflare checkpoint28October: first controlled async report publication stopped after3checks; original688B report and694B pending bytes moved unchanged to TestResults/ProviderConformanceCurrent/CloudflareOASIS/async/report-publication-interrupted{,.next}.json, hashes in publication-failure-preservation.json. Backend was in-memory and process terminal, no native data/keys existed.200owned metadata create/flush/replace probes passed and exact probe files removed; publication-probe-result.json retained. Original exception details unavailable from quiet logger, cause unestablished; no assertion of antivirus fault. One explicit fresh async verification planned after preserving metadata, no automatic retry/fallback or external operation replay.

- Cloudflare KV28October outcome:52controlled shared checks and25protocol/metadata controls pass;2native cases explicitly gated. No Cloudflare requests/account/namespace, SDK/download or native fixture data. Controlled dictionaries0/disposed. Same existing output/cache paths reused; actual bin/obj measurements and safe regeneration in TestResults/HyperDrive-Providers/CloudflareCurrent/resources.json. Source/PDB identity and compact hashes alongside. Original interrupted publication688/694B preserved plus one previous complete report per mode;200metadata probe files removed after success, cause unestablished, no retry shim. Owned handles terminal; retain outputs for reuse and compact evidence for reconciliation.

### TON lifecycle fixture cleanup, 9 November 2026

Both native TON reports confirm 26/26 checks and logical fixture cleanup. Preserve the sync and async conformance.json reports. The temporary source snapshots total 842,078 bytes. The earlier automatic review blocked the combined removal of the generated fixture state, compiled bindings and Node dependency tree. These files remain on disk. No retained COSMIC fixture or credentials were removed.

EdgeSQLite focused file-backed cohort: all eight checks exited. Fixture disposal removes the per-test oasis-edge database and sidecars. A post-run scan found zero oasis-edge-*.db* files in the Windows temporary directory. Reusable build outputs and package cache were retained. C: free space was 28,119,150,592 bytes at inspection.

10 November ClickHouse fixture guard: no database connected, no raw report. Restored project dependencies and cleaned the private integration project outputs under the build lease; reference outputs and restore metadata were retained.

10 November ClickHouse hard-delete regression: no database or raw report created. Leased project clean reduced private outputs from 50941244 B to 827230 B. Reference outputs were excluded.

10 November ClickHouse avatar readback compile: cleaned the private integration project outputs under the lease. No backend, fixture or raw report allocated.

10 November SQLite native import: in-memory database, no raw report. Leased test project clean reduced outputs from 90921349 B to 928123 B.

10 November SQLite child-role checks: fixture database, sidecars and intents confirmed absent. No raw reports. Leased project clean reduced private outputs from 98204456 B to 917773 B.

10 November SQLite child-role rollback: fixture database, sidecars and intents absent after teardown. No raw report. Leased clean reduced private outputs from 98204456 B to 917773 B.

10 November IPFS fixture guard compile: no backend or raw report. Leased project clean removed generated test outputs while retaining reference outputs and restore metadata.

10 November IPFS activation: verified the pinned 42,170,789 B Kubo archive and removed it after automatic approval review blocked the daemon command. No daemon started or fixture extracted. The activation test project compiled and its generated outputs were cleaned under the build lease.

10 November WEB4 count build: lockfile install and Vite production build completed; bundle verified 234. Generated dependency tree (115007857 B) and verification output (361528 B) remain at oasisweb4.com/node_modules and TestResults/Web4ProviderCountBuild. Automatic approval review blocked the scoped recursive removal command as policy-rejected, before any deletion.

10 November Convex hosted harness compile: private integration project outputs cleaned under the build lease. Reference outputs and restore metadata retained. No hosted fixture, backend or raw report allocated.

10 November Neon harness verification: both hosted cases skipped before backend access. Leased project clean completed; reference outputs and restore metadata retained. No native fixture or raw report allocated.

10 November Supabase duplicate-identity guard: no hosted fixture or raw report allocated. Leased clean completed for the private integration project.

10 November IPFS Development candidate 346cd105a9: the build session is no longer available. Post-run inspection found no IPFS integration test assembly under the project's bin or obj directories. No backend fixture was created for this compile.

10 November Neon console harness: leased dotnet clean completed after the successful compile. No hosted fixture or raw report was created.

10 November Convex console harness: leased dotnet clean completed after the successful compile. No hosted fixture or raw report was created.

10 November Supabase console harness: leased dotnet clean completed after compilation. No hosted fixture or raw report was created.

10 November Supabase console exit-code follow-up: leased build and clean completed. No backend or raw report allocated.

10 November priority BootLoader registration: leased build and dotnet clean completed. No hosted fixture or raw report was created.

10 November BootLoader candidate a70a6d5cb4: leased project clean completed after the build, and the checkout returned to the HyperDrive branch. No hosted fixture or raw report was allocated.

10 November BootLoader activation-error follow-up: leased build and project clean completed. No backend or raw report allocated.

10 November IPFS test cleanup follow-up: leased build and clean completed after adding the pinned SDK project reference. No live fixture or raw report was created.

10 November IPFS teardown candidate: leased build and clean completed. Original Core 59595871d and HyperDrive branch restored. No backend or raw report created.

10 November combined BootLoader candidate 6b07640979: leased build and clean completed, and the worktree returned to the HyperDrive branch. No backend or raw report allocated.

10 November urgent disk cleanup: stopped the owned ONODE build before a compile result, then completed leased dotnet clean for the ONODE WebAPI project. C: free space rose from 18,274,529,280 bytes before cleanup to 20,561,883,136 bytes after cleanup (shared disk measurement). Automatic approval review blocked recursive removal of ignored bin, obj, node_modules and the Web4 count build output before deletion. No source, credentials, retained fixtures or unrelated checkouts were targeted.

Urgent disk cleanup follow-up: leased standard project clean succeeded for AWS unit, Web3Core unit, TRON integration and Polkadot unit and integration tests. Shared C: free space rose from 21,235,875,840 to 29,969,158,144 bytes during the operation. No build, restore or download was started. Recursive deletion of the remaining generated dependency folders remains blocked by automatic approval review.

11 November LocalDB readiness: created, started, queried, stopped and deleted only the owned OASISHyperDriveRegistrationProbe instance. Instance-list absence verified. No download or retained fixture database attachment. The query accessed only master metadata.

11 November activation harness graph at 6eaa80d662 compiled with zero errors and 2,140 warnings. Verified ActivationHarness/current-artifacts absent. Retained the compact current-result.json receipt.

11 November SQL Server activation attempt at a5fcf80fcc: removed sqlserver-artifacts and the owned SqlServerBootLoaderFixture after passive boot failed. Confirmed the fixture directory and OASISHyperDriveRegistrationProbe instance are absent. Compact sqlserver-result.json retained.

11 November corrected SQL Server activation at c277943300 passed. Verified the exact sqlserver-artifacts and SqlServerBootLoaderFixture paths are absent and the owned OASISHyperDriveRegistrationProbe instance is absent. Retained the compact sqlserver-fixed-result.json receipt.

11 November public provider inventory update used temporary Git indexes only. All were removed and the temporary index prefix was checked for remnants. Site source and production responses were checked without installing dependencies, downloading binaries or generating site builds locally.

11 November Development SQL Server qualification at c39eba5cd1: LocalDB 17.0.4025.3 registered, activated and deactivated through the shipping BootLoader. All six SQL Server tables confirmed, fixture and named instance independently confirmed absent. Build 0 errors, 2,222 warnings. Standard leased dotnet clean passed. Automatic approval review rejected recursive removal of the exact owned sqlserver-development-artifacts directory. The remaining 547 files total 49,598,241 bytes. Restored the task worktree and removed the copied probe source.

11 November Neo4j Aura metadata build and guarded test discovery used the existing worktree and one isolated AuraMetadataBuild output path. Final build passed with 0 errors and 1,150 warnings. The guarded native case skipped with no hosted fixture and no database writes. Leased standard dotnet clean completed. Automatic approval review rejected recursive removal of the isolated path. The remaining 123 generated files total 6,839,614 bytes.

11 November Supabase context and payload tests reused one isolated SupabaseContextBuild output path. All 39 tests passed after the payload fix. Leased standard dotnet clean completed. Automatic approval review rejected recursive removal of the isolated path. The remaining 99 generated files total 5,161,785 bytes. No hosted fixture was contacted.

11 November Convex readiness build and tests used one isolated ConvexReadinessBuild output path. 24 tests passed and six native cases skipped without a fixture. Standard leased dotnet clean completed. Automatic approval review rejected recursive removal of the isolated path. The remaining 99 generated files total 5,161,647 bytes.

11 November Neon connection policy tests used one isolated NeonConnectionBuild output path. All 13 tests passed. Leased standard dotnet clean completed. Automatic approval review rejected recursive removal of the isolated path. The remaining 107 generated files total 5,945,891 bytes. No hosted fixture was contacted.

11 November TRON lifecycle admission checks used one isolated TronAdmissionBuild path. Five admission cases passed and the native theory skipped. No endpoint was contacted and no fixture reports were written. Standard leased dotnet clean completed. Automatic approval review rejected recursive removal of the isolated path. The remaining 107 files total 5,935,280 bytes.

11 November EOSIO public balance test passed against Vaulta. The opt-in process setting was removed and leased standard dotnet clean completed. Automatic approval review rejected recursive removal of EosioPublicBalanceBuild. The remaining 98 files total 5,188,590 bytes. No private key, chain write or fixture database was used.

11 November exact EOSIO Development candidate 16d22e842e passed the live Vaulta balance check. The process opt-in was removed, the task branch restored, and leased standard dotnet clean completed. Automatic approval review rejected recursive removal of EosioDevelopmentBalanceBuild. The remaining 98 generated files total 5,188,985 bytes. The temporary integration index was removed.

11 November EOSIO wallet balance tests passed sync and async on public Vaulta. The process opt-in was removed and leased standard dotnet clean completed. Automatic approval review rejected recursive removal of EosioWalletBalanceBuild. The remaining 98 files total 5,188,590 bytes. No signer or chain write was used.

11 November Development EOSIO wallet balance candidate 3f5d781f29 reused EosioDevelopmentBalanceBuild. Both public Vaulta cases passed. Leased standard dotnet clean completed, the process opt-in was removed, and the task branch restored. The isolated path still contains 98 generated files totaling 5,188,985 bytes, unchanged from the previous cleanup. Recursive removal of this path was previously rejected by automatic approval review. The temporary Git index was removed.

11 November Hedera synchronous wallet balance passed against the live testnet mirror. Leased standard dotnet clean completed and the process opt-in was removed. Automatic approval review rejected recursive removal of HederaWalletReadBuild. The remaining 107 files total 5,187,033 bytes. No signer, chain write or fixture database was used.

11 November ActivityPub synchronous status and reply tests reused one ActivityPubSynchronousReadBuild path. Leased standard dotnet clean completed after both configurations. Automatic approval review rejected recursive removal. The remaining 107 generated files total 5,881,738 bytes. The public-read process opt-in was removed.

11 November public provider count recheck used in-memory HTTPS responses and existing checkouts. No local build, install, downloaded file, test environment or generated report was created. C: free space observed at 42,186,285,056 bytes before the recheck.

11 November Polkadot run 43036417847 stopped before funding or deployment and confirmed removal of its owned contracts-node fixture on the disposable CI runner. The next candidate stores its generated node identity inside that same cleanup scope. No Windows build, download or fixture was created.

11 November Polkadot run 43037729937 finalized funding and contract deployment on its disposable chain, then confirmed removal of the owned Substrate fixture after the managed compile failed. No Windows build, download or test environment was created.

11 November Polkadot run 43038744304 confirmed removal of the disposable Substrate fixture after the async failure and sync guard. The pending originals applied only to that discarded private chain. No local fixtures or build outputs were created.

11 November ActivityPub authenticated search uses a disposable CI Compose project with scoped teardown for its containers, volumes, TLS trust, loopback hosts entry and generated fixture files. No Windows image, build or test environment was created. Actual CI teardown remains to be verified.

11 November ActivityPub run 43041663800 failed during fixture account admission. Docker confirmed removal of the owned database and Redis containers and network. The script confirmed removal of the fixture directory and reported no host mapping or certificate cleanup errors. No local environment was created.

11 November ActivityPub run 43042154202 reached authenticated native search and removed all four owned service containers, the Compose network and the fixture directory. No local resources were created.

11 November ActivityPub run 43042831640 removed its owned Mastodon fixture directory after the managed search point-read and deletion acknowledgement failures. The two private statuses belonged only to the discarded CI database. No Windows build, download or fixture was created.

11 November ActivityPub run 43043728770 passed 3/3 and confirmed removal of all four owned containers, the Compose network and the fixture directory. The script exited successfully after its temporary TLS and hosts cleanup. No Windows outputs or test environment were created.

11 November EOSIO shared conformance reuses the Spring CI runner's existing exact fixture cleanup in each independently isolated job. The checkpoint report is written inside the disposable fixture. No Windows runtime, build or database was created. CI teardown remains to be verified.

11 November Polkadot run 43041109458 passed both modes and confirmed removal of the Binaryen and Substrate fixtures. EOSIO run 43046140551 confirmed removal of both independently scoped Antelope fixture directories after the first native failure. No Windows outputs were created.

11 November EOSIO run 43046966627 confirmed removal of both Antelope fixtures after the graph-save failure. No Windows builds, downloads or test files were created.

11 November EOSIO async job 130616393997 confirmed removal of its Antelope fixture after the metadata query failure. The sync job was still active at this check. No local build or test files were created.

11 November EOSIO run 43047808703 also removed the sync fixture after the same metadata failure. Run 43048263198 confirmed both later fixtures removed after the avatar-detail absence failure. No local artifacts were created.

11 November EOSIO run 43048799690 passed 26 checks on each independent ledger and verified all three native tables empty. Both jobs confirmed removal of their Antelope fixture directories, including checkpoint reports and node state. No Windows outputs were created.

11 November SQL Server Linux storage verification uses a disposable CI container, its owned volumes and one scoped report directory. No local image or build was created. Native suite and teardown remain pending.

11 November SQL Server run 43049754942 removed its owned container after both managed storage modes passed and the raw cleanup check failed. No local image or outputs were created. The next run keeps generated cleanup SQL inside the owned report directory.

11 November SQL Server run 43050650521 passed 52/52 and confirmed removal of its container and owned volumes after all six tables were verified empty. The scoped CI report directory was removed by the successful cleanup trap. No Windows artifacts were created.

11 November Hedera deployment candidate uses a disposable CI runner, an isolated npm cache, owned SDK dependencies and the official local-node teardown. No Windows images, SDK installs or node files were created. Actual teardown remains to be verified.

11 November Hedera run 43051793550 stopped before deployment. The official CLI confirmed it stopped the network and cleaned volumes. The retry checks that the fresh runner has no remaining containers. No Windows outputs were created.

11 November Hedera run 43052606982 stopped at mirror gRPC startup and executed the official local-node teardown. No local artifacts or retained testnet writes were created.

11 November Hedera run 43053560805 deployed the storage contract and reached managed writes on a disposable consensus fixture. The official CLI confirmed teardown after the detail-save failure. No Windows artifacts or retained testnet writes were created.

11 November Hedera run 43055014445 confirmed official teardown for both independent fixtures after the detail-save failures. No Windows artifacts or funded testnet writes were created.

11 November Hedera run 43056077700 passed both storage modes. Both jobs verified empty native keys and confirmed removal of their containers, fixture directories, npm caches, and temporary SDK dependencies. No Windows builds or downloads were created.

11 November Sui native deployment uses a disposable CI runner. The workflow extracts only the Sui executable and removes its download, then tears down its owned ledger, runtime, dependencies and reports. No local artifacts were created. CI teardown remains pending.

11 November Sui run 43057684457 passed native deployment and confirmed removal of the owned runtime, ledger, reports and SDK dependencies. The next candidate writes its managed checkpoints inside the same cleanup scope. No Windows outputs were created.

11 November Sui run 43058209983 passed 52/52 and confirmed independently empty native contract keys. CI confirmed removal of the validator runtime, ledger, reports and SDK dependencies. No Windows artifacts were created.

11 November CockroachDB run 43060207358 passed 52/52, verified and removed ten exact owned tombstones, and confirmed empty current tables in both databases. CI confirmed the process stopped and the runtime, databases and checkpoint files were removed. No Windows runtime, build or download was created.

11 November cleanup recheck in the HyperDrive worktree. The earlier WEB4 count build still occupies 115,007,857 bytes in `oasisweb4.com/node_modules` and 361,528 bytes in `TestResults/Web4ProviderCountBuild`. Both are Git-ignored generated directories, with no linked content or matching active Node process found. Automatic review rejected the scoped recursive removal before execution. No deletion occurred. The four named wallet and ActivityPub build folders were absent from this worktree's top-level TestResults directory.

11 November Web3Core Solidity verification. Automatic approval review rejected the isolated dotnet test and cleanup command before execution. The named TestResults/Web3CoreSolidity53554aefa9 directory was confirmed absent. The existing pinned Solidity compiler then verified the shared initialization statements entirely in memory. No downloads, build outputs, or reports were created.

2026-11-12 00:17 UTC routing verification and cleanup recheck. The Google Routes, HERE decoder and shared OSRM checks compiled and ran in memory, with no local downloads, runtimes, assemblies or test reports. Routing CI 43124632388 is running on a disposable GitHub runner. The local Web3Core isolated test directory remains absent. Earlier generated WEB4 paths TestResults/Web4ProviderCountBuild and oasisweb4.com/node_modules remain present after the previous automatic cleanup denial. No deletion was attempted in this recheck. Shared C drive free space was 19498004480 bytes.

11 November Cancun time. TRON native run 43136508779 passed its disposable TRE workflow including BootLoader activation and teardown. The fixture cleanup trap removes the owned container and temporary directory containing the protected DNA, contract identity and test reports. No Windows build or download was created.

11 November Cancun time. Polkadot activation-only CI 43138621689 passed, including teardown of the owned Substrate process and fixture directory containing the generated DNA. The job uses a disposable runner. No Windows build output was created.

11 November Cancun time. C: had 20372594688 bytes free after the cleanup audit. The largest reviewed disposable HyperDrive items were the inactive Garnet tool install (562.9 MiB), WEB4 node_modules (109.7 MiB), isolated EVM npm cache (95.6 MiB), and generated NEAR build dependencies and WEB4 output. The full provider results directory was preserved because it also contains native evidence. Automatic approval review rejected the scoped recursive cleanup command as blocked by policy before it ran. No files were removed by this audit.

11 November Cancun time. Holo Desktop native CI 43156037841 passed the selected real-conductor lifecycle test and process teardown. The hApp, conductor fixture and test output lived on the disposable Windows runner. No local download or build artifacts were created.

12 November Cancun time. Azure Cosmos DB BootLoader verification uses a disposable GitHub-hosted runner and a loopback-only emulator. The job removes its container, volumes and temporary DNA directory on exit. The runner owns the downloaded image, restored packages and build outputs. No local downloads, databases or build outputs were created. Job 131055311882 confirmed removal of the owned fixture directory and emulator container with volumes. The disposable runner held all generated build and download data.

12 November Cancun time. Combined Azure startup job 131061911150 confirmed removal of its emulator container, volumes and fixture directory. The exact-tree comparison used one disposable alternate Git index, removed on completion. Matrix regeneration and comparison created no runtime, dependency cache or test output.

12 November Cancun time. The Linux HoloOASIS startup fixture runs on a disposable hosted runner. It puts the downloaded conductor cache and fixture DNA under one owned temporary directory and removes it on exit. No local builds or downloads were created. CI cleanup evidence is pending.

12 November Cancun time. Linux HoloOASIS job 131078300392 verified conductor listener shutdown and removed the owned download cache and fixture directory. All build output stayed on the disposable hosted runner.

12 November Cancun time. Combined Linux HoloOASIS job 131081195718 confirmed listener shutdown and removal of its temporary conductor cache and fixture. The exact-tree comparison used an alternate Git index in a temporary directory that was removed when the comparison completed. No local build outputs or downloads were created.

12 November Cancun time. The Firestore startup check uses a disposable hosted runner. It confines the downloaded emulator JAR, logs, fixture DNA and TRX to one owned temporary directory. Its exit handler stops the owned Java process and removes the directory. No local build or download was performed. CI cleanup confirmation is pending.

12 November Cancun time. Firestore job 131092744265 confirmed the emulator process stopped and its owned fixture directory was removed. The downloaded JAR, logs, fixture DNA and test result were held on the disposable hosted runner.

12 November Cancun time. Combined Firestore job 131099234527 verified emulator and fixture removal. The Firestore exact-tree comparison removed its temporary Git index. Blockscout evidence reconciliation and WEB4 log inspection created no local result files or runtime downloads.

12 November Cancun time. HoloOASIS Linux job 131108586243 confirmed listener shutdown and removed the owned conductor cache and fixture directory. Windows job 131108586275 completed its conductor process teardown on the disposable runner. The combined integration tree used one temporary Git index that was removed on completion. No local test environments or builds were created.

12 November Cancun time. The OpenSearch document fixture runs on a disposable GitHub runner with a loopback-only node. The exit handler removes the owned container, volumes and test directory. No local image, build, or test output was created. CI cleanup evidence is pending.

12 November Cancun time. OpenSearch shared run 43199861002 confirmed both owned nodes, volumes and temporary fixture directories were removed after the avatar-detail read failure. The focused document job 131117468115 also completed on a disposable runner. No local builds or downloads were created.

12 November Cancun time. OpenSearch run 43200638291 removed both owned native nodes, their volumes and fixture directories after the holon point-read failure. No local artifacts were created.

12 November Cancun time. OpenSearch jobs 131125585727 and 131125585989 independently confirmed exact tombstone cleanup, empty native indices, and removal of both owned nodes, volumes and fixture directories. No local build outputs or downloads were created.

12 November Cancun time. OpenSearch Development jobs 131129447327 and 131129447820 each confirmed removal of the owned node, volumes and fixture directory. The temporary index used to reproduce the CI tree was removed. No local builds or downloads were created.

12 November Cancun time. Elasticsearch shared storage CI uses one loopback-only native node and one temporary checkpoint directory per hosted runner. Its exit handler removes the owned node, volumes and directory. No local image, build output or test files were created. CI cleanup confirmation is pending.

12 November Cancun time. Elasticsearch run 43204069260 removed its two owned nodes, volumes and fixture directories after the username lookup failure. No local outputs or downloads were created.

12 November Cancun time. Elasticsearch jobs 131136774362 and 131136774429 reported fixture cleanup and removed both owned nodes, volumes and checkpoint directories after the final index-name check failed. No local builds, downloads or test outputs were created.

12 November Cancun time. Elasticsearch jobs 131140744580 and 131140744258 confirmed exact fixture cleanup, zero native documents and removal of the owned nodes, volumes and checkpoint directories. No local builds, downloads or test outputs were created.

12 November Cancun time. The temporary Git index used to reproduce the exact Elasticsearch Development qualification tree was removed and its absence verified. No runtime, build or test files were created.

12 November Cancun time. Typesense storage CI uses a disposable hosted runner with a loopback-only node, temporary in-memory data and one result directory. Its exit handler removes the owned node, volumes and result directory. No local image, build or test output was created. CI cleanup evidence is pending.

12 November Cancun time. Typesense job 131147811207 confirmed removal of the owned native node, volumes and result directory after the avatar acknowledgement assertion failed. No local test artifacts were created.

12 November Cancun time. The temporary index for the Elasticsearch tree on the managed HoloOASIS parent was removed and its absence verified. Typesense shared CI uses a separate disposable runner per mode, logs compact checkpoints and removes both owned nodes, volumes and result directories on exit. No local build or runtime was created.

12 November Cancun time. Typesense job 131150478927 confirmed zero documents in all three native collections and removed its node, volumes and result directory. Elasticsearch jobs 131149546100 and 131149546680 each confirmed exact fixture cleanup and removed their nodes, volumes and result directories. No local build, test artifacts or downloads were created.

12 November Cancun time. Typesense shared jobs 131152585292 and 131152585627 removed their owned native nodes, volumes and result directories after the directory-prefix guard failed. No local outputs were created.
