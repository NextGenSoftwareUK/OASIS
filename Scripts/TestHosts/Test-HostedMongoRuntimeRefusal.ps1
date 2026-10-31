#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runner = Join-Path $root 'Scripts/run_hosted_mongo_release_evidence.ps1'
$names = @('OASIS_MONGO_REPLICA_SET_CONNECTION', 'OASIS_MONGO_PROCESS_KILL_COORDINATION_DIRECTORY')
$prior = @{}
foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$probeDirectory = Join-Path $root 'TestResults/HostedMongoRuntimeRefusal'
if (Test-Path -LiteralPath $probeDirectory) { throw 'Refusal probe path already exists; refusing to alter it.' }
$missingRuntime = Join-Path $probeDirectory 'missing.exe'
try {
    foreach ($present in @($false, $true)) {
        foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $(if ($present) { "prior-$name" } else { $null }), 'Process') }
        $expected = @{}
        foreach ($name in $names) { $expected[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
        foreach ($missingMongo in @($true, $false)) {
            $arguments = @{ WorkingPath = $probeDirectory; MongoShellExecutable = $missingRuntime }
            if ($missingMongo) { $arguments.MongoExecutable = $missingRuntime }
            $refused = $false
            try { & $runner @arguments }
            catch {
                $message = if ($missingMongo) { 'Select an existing MongoDB runtime' } else { 'Pass -MongoShellExecutable' }
                if (!$_.Exception.Message.Contains($message)) { throw }
                $refused = $true
            }
            if (!$refused) { throw 'Missing runtime was not refused.' }
            if (Test-Path -LiteralPath $probeDirectory) { throw 'Refusal created test data or downloads.' }
            foreach ($name in $names) {
                if ([Environment]::GetEnvironmentVariable($name, 'Process') -cne $expected[$name]) { throw "Refusal altered $name" }
            }
            Write-Output "PASS: missingMongo=$missingMongo, existingVariables=$present; no files or environment mutation"
        }
    }
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $prior[$name], 'Process') }
}
