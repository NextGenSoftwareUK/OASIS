<#
Emergency Windows Cleanup Script
Developer/Gaming workstation cleanup utility.

Features:
- Literal-path cleanup; reusable package/runtime caches are retained by default
- Dry-run support
- Accurate reclaimed-space reporting
- Clears temp/cache/dev junk
- Explicit optional package-cache removal (never during active builds)
- Clears Cursor/VSCode/JetBrains caches
- Clears shader caches
- Clears Steam/Battle.net caches
- Optional aggressive cleanup
- Optional hibernation disable
- Optional recycle bin purge
- Exact reviewed Downloads/report/build selections (never whole Downloads/TestResults)
- Optional WSL compaction; never implicit shutdown

Usage:
  powershell -File .\cleanup-emergency-space.ps1 -DryRun

Examples:
  .\cleanup-emergency-space.ps1
  .\cleanup-emergency-space.ps1 -DryRun
  .\cleanup-emergency-space.ps1 -Aggressive
  .\cleanup-emergency-space.ps1 -DisableHibernate
  .\cleanup-emergency-space.ps1 -ClearRecycleBin
  .\cleanup-emergency-space.ps1 -ClearSteamCache -ClearBattleNetCache
  .\cleanup-emergency-space.ps1 -ClearBuildOutputs -BuildRoot C:\Source\OASIS\SomeProject\bin
  .\cleanup-emergency-space.ps1 -ReviewedArtifact C:\Source\OASIS\artifacts\completed-validation
  .\cleanup-emergency-space.ps1 -WorktreePath C:\Source\reviewed-unused-worktree -ActiveWorktreePath C:\Source\OASIS

No cleanup policy bypass: an execution-policy denial must be reported, not retried
through another shell. This script cannot certify hosted test outcomes or merge
unverified work. Review worktree dependencies and reports before selecting them.
#>

param(
    [switch]$DryRun,
    [switch]$Aggressive,
    [switch]$DisableHibernate,
    [switch]$ClearDownloads,
    [switch]$ClearRecycleBin,
    [switch]$KeepNuGet,
    [switch]$KeepCursor,
    [switch]$KeepJetBrains,
    [switch]$KeepWSLVhdx,
    [switch]$KeepOASISTemporaryArtifacts,
    [switch]$ClearSteamCache,
    [switch]$ClearBattleNetCache,
    [switch]$ClearPackageCaches,
    [switch]$ClearWindowsUpdateCache,
    [switch]$CompactWSL,
    [switch]$ClearBuildOutputs = $true,
    [string[]]$BuildRoot,
    [string[]]$ReviewedTestOutput,
    [string[]]$ReviewedArtifact,
    [string[]]$DownloadFile,
    [string[]]$WorktreePath,
    [string[]]$ActiveWorktreePath,
    [string[]]$PreservePath,
    [ValidateRange(1,3650)][int]$MinimumAgeDays = 7,
    [switch]$LibraryOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$cleanupLedger = Join-Path $repoRoot 'Docs\WIP\HyperDrive-Providers\WORKSPACE_CLEANUP_LEDGER.md'
$cleanupFailureCount = 0
$cleanupRemovedBytes = 0L
$cleanupCutoff = [DateTime]::UtcNow.AddDays(-$MinimumAgeDays)
$cleanupTargets = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'cleanup-emergency-space.targets.psd1')
function Convert-CleanupPath([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { throw 'An empty cleanup path is not a repository selection.' }
    if (![IO.Path]::IsPathRooted($value)) { $value = Join-Path $repoRoot $value }
    return [IO.Path]::GetFullPath($value).TrimEnd('\','/')
}
$cleanupPreserve = @($cleanupTargets.Preserve | ForEach-Object { Convert-CleanupPath $_ }) + @($PreservePath | Where-Object { ![string]::IsNullOrWhiteSpace($_) } | ForEach-Object { Convert-CleanupPath $_ })
$cleanupActiveProjects = @($cleanupTargets.ActiveProjects | ForEach-Object { Convert-CleanupPath $_ })
$ActiveWorktreePath = @($ActiveWorktreePath) + @($cleanupTargets.ActiveWorktrees | ForEach-Object { Convert-CleanupPath $_ })

function Test-CleanupProtectedFileName([string]$name) {
    return $name -match '(?i)(\.pending\.json(?:\.next)?$|\.(db|sqlite|sqlite3)(?:-wal|-shm)?$|\.vhdx?$|\.(key|pem|pfx|p12|jks)$|^\.env(?:\.|$)|^wallet(?:\.|$)|^credentials?(?:\.|$)|private.?key\.(json|txt)$|^keystore(?:\.|$)|WiredTiger|^\.git$)'
}

function Test-CleanupUnityCacheDatabase([string]$path) {
    $cacheRoot = (Convert-CleanupPath 'artifacts/unity-edge-validation-project/Library') + '\'
    $full = Convert-CleanupPath $path
    return $full.StartsWith($cacheRoot,[StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $full) -match '(?i)\.db(?:-wal|-shm)?$'
}

function Test-CleanupOverlap([string]$left, [string]$right) {
    $a = (Convert-CleanupPath $left) + '\'; $b = (Convert-CleanupPath $right) + '\'
    return $a.StartsWith($b,[StringComparison]::OrdinalIgnoreCase) -or $b.StartsWith($a,[StringComparison]::OrdinalIgnoreCase)
}

function Get-CleanupPreservationReason([string]$path) {
    foreach ($protected in $cleanupPreserve) {
        if (Test-CleanupOverlap $path $protected) { return "Retained runtime/data/evidence: $protected" }
    }
    foreach ($project in $cleanupActiveProjects) {
        foreach ($output in @((Join-Path $project 'bin'),(Join-Path $project 'obj'))) {
            if (Test-CleanupOverlap $path $output) { return "Active chat output: $output" }
        }
    }
    return $null
}

function Log($msg) {
    Write-Host "[cleanup] $msg" -ForegroundColor Cyan
}

function Get-CleanupInventory([string]$path) {
    $full = [IO.Path]::GetFullPath($path).TrimEnd('\')
    $preserved = Get-CleanupPreservationReason $full
    if ($preserved) { throw $preserved }
    $forbidden = @([IO.Path]::GetPathRoot($full).TrimEnd('\'), $repoRoot,
        $env:USERPROFILE, $env:LOCALAPPDATA, $env:APPDATA, $env:WINDIR, $env:TEMP,
        (Join-Path $env:USERPROFILE 'Downloads'))
    if ($full -in $forbidden -or $full -match '[*?]' -or $full -match '(?i)(^|\\)\.git(\\|$)') {
        throw "Refusing broad or ambiguous cleanup target: $full"
    }
    # Resolve every ancestor, not just the leaf. Never traverse a junction into a shared environment.
    $ancestor = $full
    while ($ancestor) {
        $item = Get-Item -LiteralPath $ancestor -Force -ErrorAction Stop
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse point preserved: $ancestor" }
        $ancestor = Split-Path -Parent $ancestor
    }
    $stack = [Collections.Generic.Stack[string]]::new()
    $stack.Push($full)
    $files = [Collections.Generic.List[object]]::new()
    while ($stack.Count) {
        $item = Get-Item -LiteralPath $stack.Pop() -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Shared link preserved: $($item.FullName)" }
        if ($item.PSIsContainer) {
            foreach ($child in Get-ChildItem -LiteralPath $item.FullName -Force) { $stack.Push($child.FullName) }
        } else {
            if ((Test-CleanupProtectedFileName $item.Name) -and !(Test-CleanupUnityCacheDatabase $item.FullName)) {
                throw "Native data, secret or unresolved evidence preserved: $($item.FullName)"
            }
            $files.Add($item)
        }
    }
    [pscustomobject]@{ Path=$full; Files=$files.ToArray(); Bytes=[long](($files | Measure-Object Length -Sum).Sum) }
}

function Write-CleanupLedger([string]$message) {
    if (!$DryRun) {
        Add-Content -LiteralPath $cleanupLedger -Encoding UTF8 -Value ("`n{0} cleanup script PID{1}: {2}" -f [DateTime]::UtcNow.ToString('o'), $PID, $message)
    }
}

function Remove-Safely([string]$path, [string]$reason='Regenerable cache; recreate through the original application') {
    if (!(Test-Path -LiteralPath $path)) { return }
    try {
        $inventory = Get-CleanupInventory $path
        if ($DryRun) { Log "DRY RUN: $($inventory.Path) [$($inventory.Bytes) bytes] -- $reason"; return }
        # Do not report success after a partial/locked deletion. Check files before dispatch.
        foreach ($file in $inventory.Files) {
            $handle = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
            $handle.Dispose()
        }
        Write-CleanupLedger "PLANNED exact target=$($inventory.Path); bytes=$($inventory.Bytes); inherited/version unmeasured; reason=$reason; no observed open-file dependency; permanent removal, regenerate via original tool."
        Remove-Item -LiteralPath $inventory.Path -Force -Recurse -ErrorAction Stop
        if (Test-Path -LiteralPath $inventory.Path) { throw 'Target still exists after cleanup.' }
        $script:cleanupRemovedBytes += $inventory.Bytes
        Write-CleanupLedger "REMOVED verified absent: $($inventory.Path); logical bytes=$($inventory.Bytes)."
        Log "Removed: $($inventory.Path) [$($inventory.Bytes) bytes]"
    } catch {
        $script:cleanupFailureCount++
        Write-CleanupLedger "PRESERVED or PARTIAL/FAILED target=$path; $($_.Exception.Message). Inspect before retry; no success claimed."
        Write-Warning "PRESERVED/FAILED: $path -- $($_.Exception.Message)"
    }
}

function Empty-Directory($path) {

    if (!(Test-Path -LiteralPath $path)) {
        return
    }

    if ($DryRun) {
        Log "DRY RUN: Empty $path"
        return
    }

    try {
        foreach ($child in Get-ChildItem -LiteralPath $path -Force) { Remove-Safely $child.FullName }
    }
    catch { $script:cleanupFailureCount++; Write-Warning "FAILED enumerating $path -- $($_.Exception.Message)" }
}

function Remove-OASISPathSafely([string]$path, [string]$root) {
    $boundary = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
    $target = [IO.Path]::GetFullPath($path)
    if (!$target.StartsWith($boundary, [StringComparison]::OrdinalIgnoreCase)) { throw "Outside intended repository: $target" }
    if (!(Test-Path -LiteralPath $target)) { return }
    # Ask the innermost Git checkout, so tracked files inside submodules are protected too.
    $parent = Split-Path -Parent $target
    $gitRoot = & git -C $parent rev-parse --show-toplevel
    if ($LASTEXITCODE -ne 0) { throw "Cannot establish source ownership: $target" }
    $gitBoundary = (Convert-CleanupPath $gitRoot) + '\'
    if (!$target.StartsWith($gitBoundary,[StringComparison]::OrdinalIgnoreCase)) { throw 'Git boundary mismatch.' }
    $relative = $target.Substring($gitBoundary.Length).Replace('\','/')
    $tracked = @(& git -C $gitRoot ls-files -- $relative)
    if ($LASTEXITCODE -ne 0 -or $tracked.Count) { throw "Tracked source/submodule preserved: $target" }
    Remove-Safely $target 'Untracked generated output; regenerate using the existing source, SDK and original build command'
}

function Test-CleanupProcess([string]$names) {
    return @((Get-Process -ErrorAction Stop) | Where-Object { $_.ProcessName -match $names }).Count -gt 0
}

function Remove-InstallerStaging([string]$tempRoot) {
    if (!(Test-Path -LiteralPath $tempRoot)) { return }
    if (Test-CleanupProcess '^(setup|msiexec|vs_installer|vs_installershell|CodeSetup.*)$') { Log 'Installer running; staging preserved.'; return }
    foreach ($folder in Get-ChildItem -LiteralPath $tempRoot -Directory -Force) {
        if ($folder.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        $installer = Join-Path $folder.FullName 'vs_installer.exe'
        if (Test-Path -LiteralPath $installer) {
            $signature = Get-AuthenticodeSignature -LiteralPath $installer
            if ($signature.Status -eq 'Valid' -and $signature.SignerCertificate.Subject -match 'Microsoft Corporation') {
                Remove-Safely $folder.FullName 'VS installer extraction only; installed Visual Studio/SDKs are NOT removed; installer regenerates staging'
            }
        }
    }
    # Reviewed package-download staging from the disk audit, not a wildcard Temp sweep.
    Remove-Safely (Join-Path $tempRoot 'm54u4uwi') 'Reviewed disposable VS component download/extraction staging; original installer can regenerate it'
    $codeStaging = Join-Path $tempRoot 'vscode-stable-user-x64'
    if (Test-Path -LiteralPath $codeStaging) {
        $installers = @(Get-ChildItem -LiteralPath $codeStaging -File -Filter 'CodeSetup-*.exe')
        if ($installers.Count -gt 0 -and @($installers | Where-Object {
            $signature=Get-AuthenticodeSignature -LiteralPath $_.FullName
            $signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation'
        }).Count -eq 0) {
            Remove-Safely $codeStaging 'Signed VSCode installer download staging; installed editor is preserved'
        }
    }
    foreach ($file in Get-ChildItem -LiteralPath $tempRoot -File -Filter 'dd_*.log' | Where-Object LastWriteTimeUtc -lt $cleanupCutoff) {
        Remove-Safely $file.FullName 'Old completed VS installer diagnostic log; not required test evidence'
    }
    Log "Review-only diagnostics: $(Join-Path $tempRoot 'DiagOutputDir') (do not infer disposable from age)."
}

function Find-GeneratedOutput([string]$root) {
    $stack = [Collections.Generic.Stack[string]]::new(); $stack.Push($root)
    while ($stack.Count) {
        foreach ($folder in Get-ChildItem -LiteralPath $stack.Pop() -Directory -Force) {
            if ($folder.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            if ($folder.Name -in @('.git','node_modules','TestResults','artifacts','.claude','.cache','.venv','venv')) { continue }
            if ($folder.Name -in @('bin','obj')) {
                # Do not mistake a source/tool bin directory for MSBuild output.
                $projectParent = Split-Path -Parent $folder.FullName
                if (@(Get-ChildItem -LiteralPath $projectParent -File -Filter '*.*proj').Count) { $folder.FullName }
            } else { $stack.Push($folder.FullName) }
        }
    }
}

function Remove-GeneratedOutputs {
    if (Test-CleanupProcess '^(dotnet|testhost.*|MSBuild|VBCSCompiler|Unity.*|cargo|rustc)$') { Log 'Build/test/Unity process active: generated outputs preserved.'; return }
    $buildLease = $null
    try {
        $lock = Join-Path $repoRoot 'TestResults\ProviderBuildCurrent\producer-consumer.lock'
        if (Test-Path -LiteralPath $lock) { $buildLease = [IO.File]::Open($lock,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
        if (Test-CleanupProcess '^(dotnet|testhost.*|MSBuild|VBCSCompiler|Unity.*|cargo|rustc)$') { throw 'Build started; outputs preserved.' }
        if (!$KeepOASISTemporaryArtifacts) {
            foreach ($relative in $cleanupTargets.ArtifactOutputs) {
                Remove-OASISPathSafely (Join-Path $repoRoot $relative) $repoRoot
            }
        }
        foreach ($relative in $ReviewedArtifact) {
            $target = [IO.Path]::GetFullPath($relative)
            if (!$target.StartsWith((Join-Path $repoRoot 'artifacts\'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Reviewed artifacts must be inside this repository artifacts directory.' }
            Remove-OASISPathSafely $target $repoRoot
        }
        foreach ($target in $ReviewedTestOutput) {
            $full = [IO.Path]::GetFullPath($target)
            if (!$full.StartsWith((Join-Path $repoRoot 'TestResults\'),[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($full) -notin @('.trx','.log','.txt','.html')) { throw 'Select exact reviewed report files, never entire TestResults environments.' }
            Remove-OASISPathSafely $full $repoRoot
        }
        if ($ClearBuildOutputs) {
            $repositories = @($repoRoot) + @($cleanupTargets.AdditionalBuildRepositories | ForEach-Object { Convert-CleanupPath $_ })
            foreach ($repository in $repositories) {
                if (!(Test-Path -LiteralPath $repository)) { continue }
                if ($repository -ne $repoRoot) {
                    $registered = @(& git -C $repoRoot worktree list --porcelain | Where-Object { $_ -like 'worktree *' } | ForEach-Object { Convert-CleanupPath $_.Substring(9) })
                    if ($LASTEXITCODE -ne 0 -or $repository -notin $registered) { throw 'Old build repository is not a registered worktree.' }
                }
                $outputs = @(Find-GeneratedOutput $repository)
                foreach ($output in $outputs) {
                    $reason = Get-CleanupPreservationReason $output
                    if ($reason) { Log "PRESERVE: $output -- $reason"; continue }
                    Remove-OASISPathSafely $output $repository
                }
            }
            foreach ($output in $BuildRoot) {
                $full = Convert-CleanupPath $output
                if ((Split-Path -Leaf $full) -notin @('bin','obj')) { throw 'BuildRoot must identify one exact bin or obj directory.' }
                Remove-OASISPathSafely $full $repoRoot
            }
        }
    } catch { $script:cleanupFailureCount++; Write-Warning "Generated cleanup stopped: $($_.Exception.Message)" }
    finally { if ($buildLease) { $buildLease.Dispose() } }
}

function Remove-MergedWorktrees {
    $registeredPaths = @(& git -C $repoRoot worktree list --porcelain | Where-Object { $_ -like 'worktree *' } | ForEach-Object { Convert-CleanupPath $_.Substring(9) })
    if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate registered worktrees.' }
    foreach ($path in @($registeredPaths + $WorktreePath | Sort-Object -Unique)) {
        try {
        $target = [IO.Path]::GetFullPath($path).TrimEnd('\')
        if (!(Test-Path -LiteralPath $target)) { continue }
        if ($target -eq $repoRoot -or $target -in $ActiveWorktreePath) { Log "Active/primary worktree preserved: $target"; continue }
        $reason = Get-CleanupPreservationReason $target
        if ($reason) { Log "WORKTREE RETAINED: $target -- $reason"; continue }
        $registered = @(& git -C $repoRoot worktree list --porcelain | Where-Object { $_ -like 'worktree *' } | ForEach-Object { [IO.Path]::GetFullPath($_.Substring(9)).TrimEnd('\') })
        if ($LASTEXITCODE -ne 0 -or $target -notin $registered) { throw "Not a registered worktree of this repository: $target" }
        $dirty = @(& git -C $target status --porcelain --ignored)
        if ($LASTEXITCODE -ne 0 -or $dirty.Count) { throw "Uncommitted/ignored files preserved; review and commit explicitly first: $target" }
        & git -C $repoRoot merge-base --is-ancestor (& git -C $target rev-parse HEAD) origin/Development
        if ($LASTEXITCODE -ne 0) { throw "Unique work preserved; verify and merge into Development before retirement: $target" }
        # Get-CleanupInventory intentionally refuses .git and native evidence. For a
        # worktree Git owns the metadata; check all other entries independently.
        foreach ($entry in Get-ChildItem -LiteralPath $target -Force | Where-Object Name -ne '.git') { [void](Get-CleanupInventory $entry.FullName) }
        $submodules = @(& git -C $target submodule status)
        if ($LASTEXITCODE -ne 0 -or @($submodules | Where-Object { $_ -notmatch '^-' }).Count) { throw 'Initialized submodules preserved; no forced worktree removal.' }
        if ($DryRun) { Log "DRY RUN: git worktree remove $target (clean and merged; branch retained)"; continue }
        Write-CleanupLedger "PLANNED reviewed unused clean/merged worktree=$target; no unique commits or ignored data; size unmeasured; regenerate with git worktree add at retained branch. Active chats must be excluded by caller."
        & git -C $repoRoot worktree remove $target
        if ($LASTEXITCODE -ne 0 -or (Test-Path -LiteralPath $target)) { throw "Worktree removal incomplete: $target" }
        Write-CleanupLedger "REMOVED worktree verified absent=$target; original branch/commit retained."
        } catch {
            $script:cleanupFailureCount++
            Write-Warning "WORKTREE RETAINED: $path -- $($_.Exception.Message)"
        }
    }
}

if ($LibraryOnly) { return }

function Get-DriveStats {

    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"

    return @{
        SizeGB = [math]::Round(($disk.Size / 1GB), 2)
        FreeGB = [math]::Round(($disk.FreeSpace / 1GB), 2)
        UsedGB = [math]::Round((($disk.Size - $disk.FreeSpace) / 1GB), 2)
        FreeBytes = $disk.FreeSpace
    }
}

# =========================================================
# BEFORE STATS
# =========================================================

$before = Get-DriveStats

Write-Host ""
Log "Starting cleanup..."
Write-Host ""

Log "C: BEFORE => Used: $($before.UsedGB) GB | Free: $($before.FreeGB) GB"
Log 'Build-output cleanup defaults ON for both registered OASIS checkouts. Active-output exclusions: cleanup-emergency-space.targets.psd1.'
Log 'No package/runtime/WSL data removal by default. Use -ClearBuildOutputs:$false to omit generated build outputs.'
Write-Host ""

# =========================================================
# TEMP FILES
# =========================================================

Log "Cleaning temp folders..."

@($env:TEMP, (Join-Path $env:LOCALAPPDATA 'Temp')) | Sort-Object -Unique | ForEach-Object { Remove-InstallerStaging $_ }
Log 'Runtime downloads, clipboard attachments, SQLite temporary files and WSL swap disks are preserved; Temp is not emptied wholesale.'

# =========================================================
# OASIS GENERATED BUILD / RELEASE OUTPUTS
# =========================================================

Remove-GeneratedOutputs
Remove-MergedWorktrees

# =========================================================
# WINDOWS UPDATE CACHE
# =========================================================

if ($ClearWindowsUpdateCache) {
Log "Cleaning Windows Update cache (explicit option)..."

$updateWasRunning = (Get-Service wuauserv).Status -eq 'Running'
$bitsWasRunning = (Get-Service bits).Status -eq 'Running'
try {
    if (!$DryRun) { Stop-Service wuauserv -Force; Stop-Service bits -Force }
    Empty-Directory "C:\Windows\SoftwareDistribution\Download"
} finally {
    if (!$DryRun) {
        if ($updateWasRunning) { Start-Service wuauserv }
        if ($bitsWasRunning) { Start-Service bits }
    }
}
}

# =========================================================
# DELIVERY OPTIMIZATION CACHE
# =========================================================

Log "Cleaning Delivery Optimization cache..."

Empty-Directory "C:\ProgramData\Microsoft\Windows\DeliveryOptimization\Cache"

# =========================================================
# SHADER CACHES
# =========================================================

Log "Cleaning shader caches..."

$shaderPaths = @(
    "$env:LOCALAPPDATA\NVIDIA\DXCache",
    "$env:LOCALAPPDATA\NVIDIA\GLCache",
    "$env:LOCALAPPDATA\D3DSCache"
)

foreach ($p in $shaderPaths) {
    Empty-Directory $p
}

# =========================================================
# NUGET
# =========================================================

if ($ClearPackageCaches -and !$KeepNuGet) {
    if (Test-CleanupProcess '^(dotnet|testhost.*|MSBuild|VBCSCompiler)$') { throw 'Active builds require the shared NuGet cache; stop cleanup, not the builds.' }

    Log "Cleaning NuGet caches..."

    if (Get-Command dotnet -ErrorAction SilentlyContinue) {

        if ($DryRun) {
            Log "DRY RUN: dotnet nuget locals all --clear"
        }
        else {
            dotnet nuget locals all --clear
        }
    }

    Remove-Safely "$env:USERPROFILE\.nuget\packages"
}

# =========================================================
# NODE PACKAGE MANAGER CACHES
# =========================================================

if ($ClearPackageCaches) {
if (Test-CleanupProcess '^(dotnet|testhost.*|MSBuild|VBCSCompiler|node|npm|pnpm|yarn)$') { throw 'Active processes require shared dependency caches; no package removal.' }
Log "Cleaning npm/pnpm/yarn caches (explicit option; will require dependency restore later)..."

if ($DryRun) {

    Log "DRY RUN: npm cache clean --force"
    Log "DRY RUN: pnpm store prune"
    Log "DRY RUN: yarn cache clean"
}
else {

    npm cache clean --force

    if (Get-Command pnpm -ErrorAction SilentlyContinue) {
        pnpm store prune
    }

    if (Get-Command yarn -ErrorAction SilentlyContinue) {
        yarn cache clean
    }
}
}

# =========================================================
# CURSOR / VSCODE
# =========================================================

if (!$KeepCursor -and !(Test-CleanupProcess '^(Code|Cursor)$')) {

    Log "Cleaning Cursor/VSCode caches..."

    $cursorPaths = @(
        "$env:APPDATA\Cursor\Cache",
        "$env:APPDATA\Cursor\GPUCache",
        "$env:APPDATA\Cursor\CachedData",
        "$env:APPDATA\Cursor\logs",
        "$env:APPDATA\Code\Cache",
        "$env:APPDATA\Code\GPUCache",
        "$env:APPDATA\Code\CachedData",
        "$env:APPDATA\Code\logs"
    )

    foreach ($p in $cursorPaths) {
        Empty-Directory $p
    }
}

# =========================================================
# JETBRAINS
# =========================================================

if (!$KeepJetBrains -and !(Test-CleanupProcess '^(rider64|idea64|webstorm64|pycharm64)$')) {

    Log "Cleaning JetBrains caches..."

    $jbPaths = @(
        "$env:LOCALAPPDATA\JetBrains\TransientCache"
    )

    foreach ($p in $jbPaths) {
        Empty-Directory $p
    }
}

# =========================================================
# STEAM CACHE CLEANUP
# =========================================================

if ($ClearSteamCache) {

    $steamRunning = Get-Process steam -ErrorAction SilentlyContinue

    if ($steamRunning) {

        Log "Steam is currently running. Skipping Steam cache cleanup."
    }
    else {

        Log "Cleaning Steam caches..."

        $steamRoot = "${env:ProgramFiles(x86)}\Steam"

        $steamPaths = @(
            "$steamRoot\steamapps\shadercache",
            "$steamRoot\appcache\httpcache"
        )

        foreach ($p in $steamPaths) {
            Empty-Directory $p
        }
    }
}

# =========================================================
# BATTLE.NET CACHE CLEANUP
# =========================================================

if ($ClearBattleNetCache) {

    $battleNetRunning = Get-Process "Battle.net" -ErrorAction SilentlyContinue
    $agentRunning = Get-Process Agent -ErrorAction SilentlyContinue

    if ($battleNetRunning -or $agentRunning) {

        Log "Battle.net is currently running. Skipping Battle.net cache cleanup."
    }
    else {

        Log "Cleaning Battle.net caches..."

        $battleNetPaths = @(
            "$env:PROGRAMDATA\Battle.net\Cache",
            "$env:PROGRAMDATA\Blizzard Entertainment\Battle.net\Cache"
        )

        foreach ($p in $battleNetPaths) {
            Empty-Directory $p
        }
    }
}

# =========================================================
# WINDOWS CRASH DUMPS
# =========================================================

Log "Cleaning crash dumps..."

$dumpPaths = @(
    "C:\Windows\Minidump",
    "C:\Windows\MEMORY.DMP"
)

foreach ($p in $dumpPaths) {
    Remove-Safely $p
}

# =========================================================
# RECYCLE BIN
# =========================================================

if ($ClearRecycleBin) {

    Log "Emptying Recycle Bin..."

    if ($DryRun) {
        Log "DRY RUN: Clear-RecycleBin -Force"
    }
    else {
        Clear-RecycleBin -Force
    }
}

# =========================================================
# DOWNLOADS
# =========================================================

if ($ClearDownloads) {

    Log "Clearing Downloads folder..."

    foreach ($file in $DownloadFile) {
        $full = [IO.Path]::GetFullPath($file)
        $downloadRoot = (Join-Path $env:USERPROFILE 'Downloads').TrimEnd('\') + '\'
        if (!$full.StartsWith($downloadRoot,[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $full -PathType Leaf)) { throw 'Choose exact reviewed disposable files inside Downloads.' }
        Remove-Safely $full 'Explicitly selected obsolete download; source/runtime must already be retained elsewhere'
    }
    if (!$DownloadFile) { Log 'Specify -DownloadFile with exact obsolete installers/archives; no blanket Downloads deletion.' }
}

# =========================================================
# HIBERNATION
# =========================================================

if ($DisableHibernate) {

    Log "Disabling hibernation..."

    if ($DryRun) {
        Log "DRY RUN: powercfg /hibernate off"
    }
    else {
        powercfg /hibernate off
    }
}

# =========================================================
# DISM CLEANUP
# =========================================================

if ($Aggressive) {

    Log "Running DISM component cleanup..."

    if ($DryRun) {
        Log "DRY RUN: DISM /Online /Cleanup-Image /StartComponentCleanup"
    }
    else {
        DISM /Online /Cleanup-Image /StartComponentCleanup
    }
}


# =========================================================
# WSL VIRTUAL DISK COMPACTION
# =========================================================

if ($CompactWSL -and !$KeepWSLVhdx) {

    Log "Compacting WSL virtual disks..."

    $wslCompactionScript = Join-Path $PSScriptRoot "compact-wsl-vhdx.ps1"
    try {
        & $wslCompactionScript -DryRun:$DryRun -ErrorAction Stop
    }
    catch {
        throw "WSL virtual disk compaction failed: $($_.Exception.Message)"
    }
}

# =========================================================
# FINAL STATS
# =========================================================

Write-Host ""

$after = Get-DriveStats

Log "Cleanup finished: $cleanupFailureCount preserved/failed targets; $cleanupRemovedBytes logical bytes verified removed."
Write-Host ""

Log "C: AFTER => Used: $($after.UsedGB) GB | Free: $($after.FreeGB) GB"

$freed = [math]::Round((($after.FreeBytes - $before.FreeBytes) / 1GB), 2)

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "DRIVE FREE-SPACE CHANGE: $freed GiB (includes concurrent activity)" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
