[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Project,
    [Parameter(Mandatory)][string]$Happ,
    [Parameter(Mandatory)][string]$ResultsDirectory,
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true' -or -not $IsWindows) {
    throw 'This fixture provisions a Windows hosted-runner conductor only; no local installation is allowed.'
}
$resources = Join-Path (Split-Path (Split-Path $Project -Parent) -Parent) 'NextGenSoftware.Holochain.HoloNET.Client/Resources'
$tools = @{
    'hc.exe' = '643ed26e84a7ae72a454bd78b9427b50883fb1306406ba7b5fa4857c61c6b322'
    'holochain.exe' = '5d9bd8cf28c45645158d56198cfec38d04ac4cf620ba89ca37db2f7c46d7b3d2'
}
foreach ($name in $tools.Keys) {
    $path = Join-Path $resources $name
    if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $tools[$name]) {
        throw "Bundled $name differs from the official Holochain 0.7.0 release digest."
    }
}
$hc = Join-Path $resources 'hc.exe'
$conductor = Join-Path $resources 'holochain.exe'
$fixture = Join-Path $ResultsDirectory 'holonet-live-fixture'
if (Test-Path -LiteralPath $fixture) { throw 'Conductor fixture already exists; reconcile the prior run rather than reusing state.' }
New-Item -ItemType Directory -Path $fixture | Out-Null
$fixture = (Resolve-Path -LiteralPath $fixture).Path
$Happ = (Resolve-Path -LiteralPath $Happ).Path
$Project = (Resolve-Path -LiteralPath $Project).Path
$ResultsDirectory = (Resolve-Path -LiteralPath $ResultsDirectory).Path
$saved = @{}
$names = @('HOLONET_LIVE_ADMIN_URI', 'HOLONET_LIVE_HAPP_PATH', 'HOLONET_LIVE_CLONE_HAPP_PATH', 'HOLONET_LIVE_MEMPROOFS_HAPP_PATH')
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
    & dotnet test $Project --configuration $Configuration --logger 'trx;LogFileName=holonet-app-authentication.trx' --results-directory $ResultsDirectory --nologo
    if ($LASTEXITCODE -ne 0) { throw 'HoloNET live release tests failed.' }
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
