[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Project,
    [Parameter(Mandatory)][string]$Happ,
    [Parameter(Mandatory)][string]$ResultsDirectory,
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
function Assert-CompleteTestReport([string]$Path, [int]$Minimum) {
    [xml]$report = Get-Content -LiteralPath $Path -Raw
    $counters = $report.SelectSingleNode('//*[local-name()="Counters"]')
    if (-not $counters -or [int]$counters.total -lt $Minimum -or
        [int]$counters.total -ne [int]$counters.executed -or [int]$counters.executed -ne [int]$counters.passed) {
        throw "Required report $([IO.Path]::GetFileName($Path)) is incomplete or contains nonpassing tests."
    }
}
if ($env:GITHUB_ACTIONS -ne 'true' -or -not $IsWindows) {
    throw 'This fixture provisions a Windows hosted-runner conductor only; no local installation is allowed.'
}
$resources = Join-Path (Split-Path (Split-Path $Project -Parent) -Parent) 'NextGenSoftware.Holochain.HoloNET.Client/Resources'
$tools = @{
    'hc-0.7.0.exe' = '643ed26e84a7ae72a454bd78b9427b50883fb1306406ba7b5fa4857c61c6b322'
    'holochain-0.7.0.exe' = '5d9bd8cf28c45645158d56198cfec38d04ac4cf620ba89ca37db2f7c46d7b3d2'
    'hc.exe' = '7b00c9515e9de6af0f5ba0f2ae5b37f27090169de5fa42af0184913129873c42'
    'holochain.exe' = '59261119a3ad3e51cd33d996be3110d39a5c4d1cc7c34c173a28efec137bb5b1'
}
foreach ($name in $tools.Keys) {
    $path = Join-Path $resources $name
    if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $tools[$name]) {
        throw "Bundled $name differs from its pinned official Holochain release digest."
    }
}
# The default release path is 0.7.1. Versioned 0.7.0 binaries above are
# retained only for the explicitly selected compatibility lifecycle cases.
$hc = Join-Path $resources 'hc.exe'
$conductor = Join-Path $resources 'holochain.exe'
foreach ($binary in @($hc, $conductor)) {
    $version = (& $binary --version) -join "`n"
    if ($LASTEXITCODE -ne 0 -or $version -notmatch '\b0\.7\.1\b') { throw 'Default release tools must report Holochain/hc 0.7.1.' }
}
$fixture = Join-Path $ResultsDirectory 'holonet-live-fixture'
if (Test-Path -LiteralPath $fixture) { throw 'Conductor fixture already exists; reconcile the prior run rather than reusing state.' }
New-Item -ItemType Directory -Path $fixture | Out-Null
$fixture = (Resolve-Path -LiteralPath $fixture).Path
$Happ = (Resolve-Path -LiteralPath $Happ).Path
$Project = (Resolve-Path -LiteralPath $Project).Path
$legacy = Join-Path (Split-Path (Split-Path $Project -Parent) -Parent) 'HoloNET-Manager/NextGenSoftware.Holochain.HoloNET.Manager/OASIS_hAPP/oasis.happ'
if (-not (Test-Path -LiteralPath $legacy)) { throw 'Pinned legacy hApp rejection fixture is missing; the full release suite must not skip it.' }
$legacyBlob = (& git hash-object $legacy).Trim()
if ($LASTEXITCODE -ne 0 -or $legacyBlob -ne '4069c5da8349f7a866f46fd0bc9a17e4dcef70c0') { throw 'Legacy hApp rejection fixture differs from the reviewed Git blob.' }
$ResultsDirectory = (Resolve-Path -LiteralPath $ResultsDirectory).Path
$saved = @{}
$names = @('HOLONET_LIVE_ADMIN_URI', 'HOLONET_LIVE_HAPP_PATH', 'HOLONET_LIVE_CLONE_HAPP_PATH', 'HOLONET_LIVE_MEMPROOFS_HAPP_PATH', 'HOLONET_LIVE_HC_BIN')
foreach ($name in $names) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
$process = $null
Push-Location $fixture
try {
    foreach ($variant in @('clone', 'memproofs')) {
        $directory = Join-Path $fixture $variant
        & $hc app unpack $Happ --output $directory
        if ($LASTEXITCODE -ne 0) { throw 'Unable to unpack the verified hApp fixture.' }
        $manifest = Join-Path $directory 'happ.yaml'
        $yaml = Get-Content -LiteralPath $manifest -Raw
        $pattern = if ($variant -eq 'clone') { 'clone_limit: 0' } else { 'allow_deferred_memproofs: false' }
        $replacement = if ($variant -eq 'clone') { 'clone_limit: 3' } else { 'allow_deferred_memproofs: true' }
        if ([regex]::Matches($yaml, [regex]::Escape($pattern)).Count -ne 1) { throw "Unexpected $variant fixture manifest contract." }
        $yaml.Replace($pattern, $replacement) | Set-Content -LiteralPath $manifest
        & $hc app pack $directory
        if ($LASTEXITCODE -ne 0) { throw "Unable to pack $variant hApp fixture." }
        $bundle = @(Get-ChildItem -LiteralPath $directory -Filter '*.happ' -File)
        if ($bundle.Count -ne 1) { throw "Expected one $variant hApp bundle." }
        $variable = if ($variant -eq 'clone') { 'HOLONET_LIVE_CLONE_HAPP_PATH' } else { 'HOLONET_LIVE_MEMPROOFS_HAPP_PATH' }
        [Environment]::SetEnvironmentVariable($variable, $bundle[0].FullName)
    }
    # Embedded lair uses the verified conductor binary; no additional installation.
    if (@(Get-NetTCPConnection -LocalPort 65464 -State Listen -ErrorAction SilentlyContinue).Count) {
        throw 'The fixed conductor port is already in use; refusing to test an unowned process.'
    }
    'hosted-disposable-fixture' | & $hc sandbox --piped create --in-process-lair --root $fixture --directories conductor
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create the hosted conductor sandbox.' }
    $start = [Diagnostics.ProcessStartInfo]::new($hc)
    foreach ($arg in @('sandbox', '--piped', '--holochain-path', $conductor, '-f=65464', 'run', '0')) { $start.ArgumentList.Add($arg) }
    $start.WorkingDirectory = $fixture
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.StandardInput.WriteLine('hosted-disposable-fixture')
    $process.StandardInput.Close()
    $ready = $false
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    while ([DateTime]::UtcNow -lt $deadline -and -not $process.HasExited) {
        $socket = [Net.Sockets.TcpClient]::new()
        try { $socket.Connect('127.0.0.1', 65464); $ready = $true; break }
        catch [Net.Sockets.SocketException] { Start-Sleep -Milliseconds 500 }
        finally { $socket.Dispose() }
    }
    if (-not $ready) { throw 'Hosted conductor did not become ready within 60 seconds.' }
    $env:HOLONET_LIVE_ADMIN_URI = 'ws://127.0.0.1:65464'
    $env:HOLONET_LIVE_HAPP_PATH = $Happ
    $env:HOLONET_LIVE_HC_BIN = $hc
    # Complementary partitions: auto-start owns its conductors and must run only after this one exits.
    & dotnet test $Project --configuration $Configuration --filter 'FullyQualifiedName!~LiveConductorAutoStartTests' --logger 'trx;LogFileName=holonet-app-authentication.trx' --results-directory $ResultsDirectory --nologo
    if ($LASTEXITCODE -ne 0) { throw 'HoloNET live release tests failed.' }
    Assert-CompleteTestReport (Join-Path $ResultsDirectory 'holonet-app-authentication.trx') 1188
}
finally {
    if ($process) {
        if (-not $process.HasExited) { $process.Kill($true) }
        $process.WaitForExit()
        $stdout.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $fixture 'conductor.stdout.log')
        $stderr.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $fixture 'conductor.stderr.log')
        $process.Dispose()
    }
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
    Pop-Location
}

# The upstream lifecycle tests intentionally kill conductor processes. Keep them
# isolated from the shared-conductor suite, but require their own release report.
if (@(Get-Process -Name holochain,hc,'holochain-0.7.0','hc-0.7.0' -ErrorAction SilentlyContinue).Count) {
    throw 'A conductor survived shared-suite teardown; auto-start tests must not run against it.'
}
$autoNames = @('HOLONET_LIVE_AUTOSTART','HOLONET_LIVE_EMBEDDED','HOLONET_LIVE_HOLOCHAIN_BIN','HOLONET_LIVE_HC_BIN')
$autoSaved = @{}
foreach ($name in $autoNames) { $autoSaved[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    $clientRoot = Split-Path (Split-Path $Project -Parent) -Parent
    foreach ($version in @('0.7.0','0.7.1')) {
        # Both tests install this same verified hApp: compatibility is tested, not assumed.
        $happDirectory = Join-Path $clientRoot ".holochain-test/happ-$($version.Replace('.',''))"
        New-Item -ItemType Directory -Path $happDirectory -Force | Out-Null
        $happTarget = Join-Path $happDirectory 'oasis.happ'
        if (Test-Path -LiteralPath $happTarget) { throw 'Auto-start hApp fixture already exists; refusing to overwrite unknown state.' }
        Copy-Item -LiteralPath $Happ -Destination $happTarget
        $extracted = Join-Path ([Environment]::GetFolderPath('ApplicationData')) "NextGenSoftware/HoloNET/$version"
        New-Item -ItemType Directory -Path $extracted -Force | Out-Null
        foreach ($binary in @('hc','holochain')) {
            $sourceName = if ($version -eq '0.7.0') { "$binary-0.7.0.exe" } else { "$binary.exe" }
            $target = Join-Path $extracted "$binary.exe"
            if ((Test-Path -LiteralPath $target) -and (Get-FileHash -LiteralPath $target).Hash -ine $tools[$sourceName]) {
                throw 'Existing extracted conductor differs from the pinned release; refusing overwrite.'
            }
            if (-not (Test-Path -LiteralPath $target)) { Copy-Item -LiteralPath (Join-Path $resources $sourceName) -Destination $target }
        }
    }
    $env:HOLONET_LIVE_AUTOSTART = '1'
    $env:HOLONET_LIVE_EMBEDDED = '1'
    $env:HOLONET_LIVE_HOLOCHAIN_BIN = Join-Path $extracted 'holochain.exe'
    $env:HOLONET_LIVE_HC_BIN = Join-Path $extracted 'hc.exe'
    & dotnet test $Project --configuration $Configuration -p:Embedded=true --filter 'FullyQualifiedName~LiveConductorAutoStartTests' --logger 'trx;LogFileName=holonet-auto-start.trx' --results-directory $ResultsDirectory --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Isolated HoloNET auto-start release tests failed.' }
    Assert-CompleteTestReport (Join-Path $ResultsDirectory 'holonet-auto-start.trx') 11
}
finally {
    foreach ($name in $autoNames) { [Environment]::SetEnvironmentVariable($name, $autoSaved[$name]) }
}
