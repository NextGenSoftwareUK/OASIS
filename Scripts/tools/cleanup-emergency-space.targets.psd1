@{
    ActiveWorktrees = @('C:/Source/OASIS', 'C:/Source/OASIS-cosmic', 'C:/Source/Our-World', 'C:/Source/OASIS2-hyperdrive')
    # Build and test outputs are always regenerable, so nothing here preserves bin/obj
    # or TestResults. Only non-regenerable secret backups and the pinned DynamoDB evidence are kept.
    # Paths below are relative to the main repository unless explicitly absolute.
    Preserve = @(
        'artifacts/railway-secure-backup'
        'artifacts/desktop-protected-sessions'
        # Hash-pinned reusable DynamoDB runtime and retained history; not regenerable.
        'TestResults/AWSProviderEvidence'
    )
    # Warn when any preserved folder grows beyond this size.
    PreservedSizeLimitMB = 200
    # Preserved TestResults evidence is kept only while small: files larger than this are removed.
    EvidenceKeepFileMB = 25
    # Whole-directory test outputs, emptied on every run.
    TestOutputs = @(
        'TestResults'
    )
    ArtifactOutputs = @(
        'artifacts/global-release-validation'
        'artifacts/global-release-optional-version-check'
        'artifacts/unity-edge-validation-project/Library'
        'artifacts/unity-edge-validation-project/Temp'
        'artifacts/unity-edge-validation-project/Logs'
        'artifacts/unity-edge-validation-project/obj'
        'artifacts/unity-edge-validation-project/Build'
        'artifacts/unity-edge-validation-project/Builds'
        'artifacts/native-games/native'
        'artifacts/unity/com.nextgensoftware.oasis.edge'
        'artifacts/unity/com.nextgensoftware.oasis.edge.tgz'
        'artifacts/release-promotion/package-verification'
    )
    # Other registered checkouts whose bin/obj and TestResults are also reclaimed.
    AdditionalBuildRepositories = @('C:/Source/OASIS-cosmic', 'C:/Source/OASIS2-hyperdrive')
    # Registered worktrees whose generated outputs (TestResults, artifact outputs, bulk
    # LocalState runtimes) are also reclaimed, in addition to the main repository.
    # Worktree *removal* is never attempted for these (see ActiveWorktrees).
    # Relative to each repository.
    LocalStateRoots = @('LocalState')
    # Downloaded runtimes/envs: inside every folder directly under a LocalStateRoot, any
    # subfolder larger than this is brute-force removed (extracted runtimes, node_modules,
    # JDKs, databases, browser sources). Small top-level files and small folders (evidence,
    # ACK/intent JSON, identities, logs) are kept. Agents can re-download what they need.
    LocalStateKeepSubfolderMB = 25
    # Folder names removed anywhere inside a LocalStateRoot regardless of size.
    LocalStateDisposableNames = @('node_modules', 'jdk', 'jre', 'Library', 'Temp', '.cache', '.venv', 'venv', 'target')
    # Exact absolute disposable folders (task download/extraction staging).
    AbsoluteDisposable = @(
        '%LOCALAPPDATA%/NuGet/v3-cache'
        '%LOCALAPPDATA%/Temp/m54u4uwi'
        '%USERPROFILE%/.cache/codex-runtimes'
        '%LOCALAPPDATA%/Unity/cache'
        '%APPDATA%/Code/CachedExtensionVSIXs'
        '%APPDATA%/Code/Service Worker/CacheStorage'
    )
    # Removed only when no dotnet/MSBuild/node process is running (restore regenerates them).
    # Agent leftovers their approval policy blocks them from deleting (recursive removal).
    # Repo-relative; handled by Remove-GeneratedOutputs for every build repository.
    # Agent ledgers scanned for items the agents reported they could not remove.
    AgentLedgers = @(
        @{ Repository = 'C:/Source/OASIS2'; Ledger = 'C:/Source/OASIS2/Docs/WIP/COSMIC ORM/ENVIRONMENT_AND_CLEANUP_LEDGER.md' }
        @{ Repository = 'C:/Source/OASIS2'; Ledger = 'C:/Source/OASIS2/Docs/WIP/COSMIC ORM/RECOVERY_LAPTOP_CLEANUP_LEDGER.md' }
        @{ Repository = 'C:/Source/OASIS2-hyperdrive'; Ledger = 'C:/Source/OASIS2-hyperdrive/Docs/WIP/HyperDrive-Providers/WORKSPACE_CLEANUP_LEDGER.md' }
    )
    AgentLeftovers = @(
        'oasisweb4.com/node_modules'
        'TestResults/GarnetProviderTools'
        'TestResults/NearAvatarContractBuild'
        'TestResults/Web4ProviderCountBuild'
        'TestResults/HyperDrive-Providers/NearPublicWalletBuild'
    )
    AbsoluteDisposableWhenIdle = @(
        '%USERPROFILE%/.nuget/packages'
    )
    # Brute-force sweep: every top-level folder in %TEMP% older than MinimumAgeDays goes,
    # except these names (active chat scratch / diagnostics for review).
    TempSweepFolders = $true
    TempKeepNames = @('claude', 'DiagOutputDir')
    # Temp subfolders matching these name patterns (agent/installer scratch) older than
    # MinimumAgeDays are removed from %TEMP%.
    TempScratchPatterns = @('dotnet-sdk-advertising-temp', 'bc1sz23v*', 'NuGetScratch*', 'chrome_BITS_*', 'playwright*', 'puppeteer*', 'pip-*', 'npm-*', 'node-compile-cache', 'vs-*', 'dd_*')
    # Run `podman system prune -a -f` (stopped containers + unused images; NOT volumes) in WSL.
    PruneWslContainers = $true
}
