<#
.SYNOPSIS
Produces the real MongoDB replica-set TRX evidence required by the Edge release gate.
.DESCRIPTION
Downloads pinned portable MongoDB 7 and mongosh archives, starts three loopback-only
replica-set members, runs the hosted transaction/election suite, then coordinates an
abrupt primary process termination with the dedicated external-termination test.
Every process started by this script is stopped in the finally block.
#>
[CmdletBinding()]
param(
    [string]$WorkingPath = (Join-Path $PSScriptRoot '..\TestResults\HostedMongoRelease'),
    [string]$ArtifactsDirectory = 'artifacts/hosted-mongo-local',
    [int]$FirstPort = 27117
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$work = [IO.Path]::GetFullPath($WorkingPath)
$testResultsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'TestResults')).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (!$work.StartsWith($testResultsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Mongo evidence working data must remain under '$testResultsRoot'."
}

$artifacts = [IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactsDirectory))
$repoPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (!$artifacts.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Mongo evidence artifacts must remain inside '$repoRoot'."
}

$downloads = Join-Path $work 'downloads'
$runtime = Join-Path $work 'runtime'
$run = Join-Path $work ("runs/{0}" -f [Guid]::NewGuid().ToString('N'))
$logs = Join-Path $run 'logs'
$coordination = Join-Path $run 'kill-coordination'
New-Item -ItemType Directory -Force $downloads, $runtime, $logs, $coordination, $artifacts | Out-Null

$archives = [ordered]@{
    mongodb = @('mongodb-7.0.26.zip', 'https://fastdl.mongodb.org/windows/mongodb-windows-x86_64-7.0.26.zip')
    mongosh = @('mongosh-2.6.0.zip', 'https://downloads.mongodb.com/compass/mongosh-2.6.0-win32-x64.zip')
}

function Get-Archive([string]$Name) {
    $target = Join-Path $downloads $archives[$Name][0]
    if (!(Test-Path -LiteralPath $target -PathType Leaf)) {
        Write-Host "Downloading pinned $Name runtime..."
        Invoke-WebRequest -Uri $archives[$Name][1] -OutFile $target -MaximumRedirection 8
    }
    return $target
}

function Expand-Cached([string]$Name) {
    $destination = Join-Path $runtime $Name
    if (!(Test-Path -LiteralPath $destination -PathType Container)) {
        New-Item -ItemType Directory -Force $destination | Out-Null
        Expand-Archive -LiteralPath (Get-Archive $Name) -DestinationPath $destination -Force
    }
    return $destination
}

function Wait-Port([int]$Port, [int]$Seconds = 120) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-NetConnection 127.0.0.1 -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue) { return }
        Start-Sleep -Seconds 1
    }
    throw "MongoDB port $Port did not open within $Seconds seconds."
}

function Invoke-Mongo([int]$Port, [string]$JavaScript) {
    $result = & $script:mongosh "mongodb://127.0.0.1:$Port/?directConnection=true" --quiet --eval $JavaScript
    if ($LASTEXITCODE -ne 0) { throw "mongosh failed on port $Port with exit code $LASTEXITCODE." }
    return (($result | Select-Object -Last 1) -as [string]).Trim()
}

$ports = @($FirstPort; $FirstPort + 1; $FirstPort + 2)
foreach ($port in $ports) {
    if (Test-NetConnection 127.0.0.1 -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue) {
        throw "Loopback port $port is already in use."
    }
}

$mongoRoot = Expand-Cached 'mongodb'
$shellRoot = Expand-Cached 'mongosh'
$mongod = (Get-ChildItem -LiteralPath $mongoRoot -Recurse -Filter 'mongod.exe' -File | Select-Object -First 1).FullName
$script:mongosh = (Get-ChildItem -LiteralPath $shellRoot -Recurse -Filter 'mongosh.exe' -File | Select-Object -First 1).FullName
if (!$mongod -or !$script:mongosh) { throw 'The portable MongoDB archives did not contain mongod.exe and mongosh.exe.' }

$processes = @{}
$testProcess = $null
$failure = $null
try {
    foreach ($port in $ports) {
        $dataPath = Join-Path $run "data-$port"
        New-Item -ItemType Directory -Force $dataPath | Out-Null
        $processes[$port] = Start-Process $mongod -ArgumentList @(
            '--dbpath', $dataPath, '--bind_ip', '127.0.0.1', '--port', "$port",
            '--replSet', 'oasisReleaseRs', '--logpath', (Join-Path $logs "mongodb-$port.log"), '--logappend'
        ) -PassThru -WindowStyle Hidden
        Wait-Port $port
    }

    $members = for ($index = 0; $index -lt $ports.Count; $index++) {
        "{_id:$index,host:'127.0.0.1:$($ports[$index])'}"
    }
    Invoke-Mongo $ports[0] "rs.initiate({_id:'oasisReleaseRs',members:[$($members -join ',')]})" | Out-Null

    $ready = $false
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        try {
            $state = Invoke-Mongo $ports[0] "const s=rs.status().members.map(m=>m.stateStr); print(s.filter(x=>x==='PRIMARY').length===1&&s.filter(x=>x==='SECONDARY').length===2?'ready':'waiting')"
            if ($state -eq 'ready') { $ready = $true; break }
        } catch { }
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'Replica set did not reach one-primary/two-secondary health.' }

    $connection = "mongodb://$((($ports | ForEach-Object { "127.0.0.1:$_" }) -join ','))/?replicaSet=oasisReleaseRs&retryWrites=true&serverSelectionTimeoutMS=2000&connectTimeoutMS=2000"
    $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $connection
    $project = 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests.csproj'

    & dotnet test $project --configuration Release --filter 'Category!=ExternalPrimaryTermination' `
        --logger 'trx;LogFileName=hosted-mongo-sync.trx' --results-directory $artifacts --nologo
    if ($LASTEXITCODE -ne 0) { throw "Hosted Mongo transaction suite failed with exit code $LASTEXITCODE." }

    $env:OASIS_MONGO_PROCESS_KILL_COORDINATION_DIRECTORY = $coordination
    $stdout = Join-Path $logs 'process-kill-test.stdout.log'
    $stderr = Join-Path $logs 'process-kill-test.stderr.log'
    $testProcess = Start-Process dotnet -WorkingDirectory $repoRoot -ArgumentList @(
        'test', $project, '--configuration', 'Release', '--filter', 'Category=ExternalPrimaryTermination',
        '--logger', 'trx;LogFileName=hosted-mongo-process-kill.trx', '--results-directory', $artifacts, '--nologo'
    ) -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -WindowStyle Hidden

    $readyFile = Join-Path $coordination 'ready'
    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    while (!(Test-Path -LiteralPath $readyFile -PathType Leaf)) {
        if ($testProcess.HasExited) { throw "Primary-termination test exited before its checkpoint. See '$stdout' and '$stderr'." }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Timed out waiting for the primary-termination checkpoint.' }
        Start-Sleep -Seconds 1
        $testProcess.Refresh()
    }

    $primaryAddress = Invoke-Mongo $ports[0] 'print(db.hello().primary)'
    $primaryPort = [int]($primaryAddress.Split(':')[-1])
    if (!$processes.ContainsKey($primaryPort)) { throw "Replica set reported unexpected primary '$primaryAddress'." }
    Stop-Process -Id $processes[$primaryPort].Id -Force
    $processes.Remove($primaryPort)

    $elected = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        foreach ($port in @($processes.Keys)) {
            try {
                if ((Invoke-Mongo $port "print(db.hello().isWritablePrimary?'primary':'not-primary')") -eq 'primary') {
                    $elected = $true
                    break
                }
            } catch { }
        }
        if ($elected) { break }
        Start-Sleep -Seconds 1
    }
    if (!$elected) { throw 'The surviving MongoDB members did not elect a writable primary.' }

    New-Item -ItemType File -Path (Join-Path $coordination 'continue') -Force | Out-Null
    $testProcess.WaitForExit()
    if ($testProcess.ExitCode -ne 0) { throw "Primary-termination test failed with exit code $($testProcess.ExitCode). See '$stdout' and '$stderr'." }

    Write-Host "Hosted Mongo release evidence passed: '$artifacts'."
}
catch {
    $failure = $_
}
finally {
    if ($testProcess -and !$testProcess.HasExited) { Stop-Process -Id $testProcess.Id -Force -ErrorAction SilentlyContinue }
    foreach ($process in @($processes.Values)) {
        if (!$process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
    }
    Remove-Item Env:OASIS_MONGO_REPLICA_SET_CONNECTION -ErrorAction SilentlyContinue
    Remove-Item Env:OASIS_MONGO_PROCESS_KILL_COORDINATION_DIRECTORY -ErrorAction SilentlyContinue
}

if ($failure) { throw $failure }
