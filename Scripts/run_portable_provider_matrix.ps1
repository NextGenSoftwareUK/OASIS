<#
.SYNOPSIS
Runs the SQLite, MongoDB and Neo4j provider integration suites against disposable local runtimes.
.DESCRIPTION
Downloads pinned official archives into TestResults, starts MongoDB as a loopback-only
single-node replica set and Neo4j as a loopback-only process, executes the provider matrix,
then stops every process in a finally block. Nothing is installed as a Windows service.
#>
[CmdletBinding()]
param(
    [string]$WorkingPath = (Join-Path $PSScriptRoot '..\TestResults\GeoHotSpotMatrix\portable-providers'),
    [int]$MongoPort = 27028,
    [int]$Neo4jBoltPort = 7688,
    [int]$Neo4jHttpPort = 7475
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$work=[IO.Path]::GetFullPath($WorkingPath)
$downloads=Join-Path $work 'downloads'
$runtime=Join-Path $work 'runtime'
$run=Join-Path $work ("runs/{0}" -f [Guid]::NewGuid().ToString('N'))
$logs=Join-Path $run 'logs'
New-Item -ItemType Directory -Force $downloads,$runtime,$logs|Out-Null

$archives=[ordered]@{
    mongodb=@('mongodb.zip','https://fastdl.mongodb.org/windows/mongodb-windows-x86_64-8.3.8.zip')
    mongosh=@('mongosh.zip','https://downloads.mongodb.com/compass/mongosh-2.6.0-win32-x64.zip')
    neo4j=@('neo4j.zip','https://dist.neo4j.org/neo4j-community-2026.08.1-windows.zip')
    jdk21=@('jdk21.zip','https://api.adoptium.net/v3/binary/version/jdk-21.0.12.1+1/windows/x64/jdk/hotspot/normal/eclipse')
}
function Get-Archive([string]$key){
    $target=Join-Path $downloads $archives[$key][0]
    if(!(Test-Path $target)){Write-Host "Downloading pinned $key runtime...";Invoke-WebRequest -Uri $archives[$key][1] -OutFile $target -MaximumRedirection 8}
    $target
}
function Expand-Cached([string]$key,[string]$destination){
    if(!(Test-Path $destination)){New-Item -ItemType Directory -Force $destination|Out-Null;Expand-Archive -LiteralPath (Get-Archive $key) -DestinationPath $destination -Force}
}
function Wait-Port([int]$port,[int]$seconds=120){
    $deadline=[DateTime]::UtcNow.AddSeconds($seconds)
    while([DateTime]::UtcNow -lt $deadline){if(Test-NetConnection 127.0.0.1 -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue){return};Start-Sleep 1}
    throw "Loopback port $port did not open within $seconds seconds."
}
foreach($port in @($MongoPort,$Neo4jBoltPort,$Neo4jHttpPort)){if(Test-NetConnection 127.0.0.1 -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue){throw "Loopback port $port is already in use."}}

$mongoRuntime=Join-Path $runtime 'mongodb';$shellRuntime=Join-Path $runtime 'mongosh';$jdkRuntime=Join-Path $runtime 'jdk21'
Expand-Cached mongodb $mongoRuntime;Expand-Cached mongosh $shellRuntime;Expand-Cached jdk21 $jdkRuntime
$neoRuntime=Join-Path $run 'neo4j';New-Item -ItemType Directory -Force $neoRuntime|Out-Null;Expand-Archive -LiteralPath (Get-Archive neo4j) -DestinationPath $neoRuntime -Force
$mongod=(Get-ChildItem $mongoRuntime -Recurse -Filter mongod.exe|Select-Object -First 1).FullName
$mongosh=(Get-ChildItem $shellRuntime -Recurse -Filter mongosh.exe|Select-Object -First 1).FullName
$jdk=(Get-ChildItem $jdkRuntime -Directory|Select-Object -First 1).FullName
$neo=(Get-ChildItem $neoRuntime -Directory|Select-Object -First 1).FullName
if(!$mongod -or !$mongosh -or !$jdk -or !$neo){throw 'A portable runtime archive did not contain its expected executable/root directory.'}

$started=[Collections.Generic.List[int]]::new();$failure=$null;$exitCode=0
try{
    $mongoData=Join-Path $run 'mongo-data';New-Item -ItemType Directory -Force $mongoData|Out-Null
    $mongo=Start-Process $mongod -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port',"$MongoPort",'--replSet','oasisTestRs','--logpath',(Join-Path $logs 'mongodb.log'),'--logappend') -PassThru -WindowStyle Hidden
    $started.Add($mongo.Id);Wait-Port $MongoPort
    $initScript='rs.initiate({_id:"oasisTestRs",members:[{_id:0,host:"127.0.0.1:' + $MongoPort + '"}]})'
    & $mongosh "mongodb://127.0.0.1:$MongoPort/?directConnection=true" --quiet --eval $initScript
    if($LASTEXITCODE -ne 0){throw 'MongoDB replica-set initiation failed.'}
    $primary=$false
    for($i=0;$i-lt 60;$i++){& $mongosh "mongodb://127.0.0.1:$MongoPort/?directConnection=true" --quiet --eval 'quit(db.hello().isWritablePrimary ? 0 : 1)' *> $null;if($LASTEXITCODE -eq 0){$primary=$true;break};Start-Sleep 1}
    if(!$primary){throw 'MongoDB replica set did not elect a writable primary.'}

    $env:JAVA_HOME=$jdk;$env:NEO4J_HOME=$neo
    Add-Content (Join-Path $neo 'conf/neo4j.conf') "`nserver.default_listen_address=127.0.0.1`nserver.bolt.listen_address=127.0.0.1:$Neo4jBoltPort`nserver.bolt.advertised_address=127.0.0.1:$Neo4jBoltPort`nserver.http.listen_address=127.0.0.1:$Neo4jHttpPort`nserver.http.advertised_address=127.0.0.1:$Neo4jHttpPort"
    $password='OasisPortableTest123!'
    & (Join-Path $neo 'bin/neo4j-admin.ps1') dbms set-initial-password $password
    if($LASTEXITCODE -ne 0){throw 'Neo4j initial-password setup failed.'}
    $neoProcess=Start-Process (Join-Path $neo 'bin/neo4j.bat') -ArgumentList 'console' -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logs 'neo4j-stdout.log') -RedirectStandardError (Join-Path $logs 'neo4j-stderr.log') -Environment @{JAVA_HOME=$jdk;NEO4J_HOME=$neo}
    $started.Add($neoProcess.Id);Wait-Port $Neo4jBoltPort

    $env:MONGODBOASIS_CONNECTIONSTRING="mongodb://127.0.0.1:$MongoPort/?replicaSet=oasisTestRs"
    $env:MONGODBOASIS_DBNAME='oasis_geohotspot_portable_it'
    $env:NEO4JOASIS_HOST="bolt://127.0.0.1:$Neo4jBoltPort"
    $env:NEO4JOASIS_USERNAME='neo4j';$env:NEO4JOASIS_PASSWORD=$password
    & (Join-Path $PSScriptRoot 'run_local_provider_matrix.ps1') -OutputPath (Join-Path $work 'results')
    if($LASTEXITCODE -ne 0){throw "Local provider matrix exited with $LASTEXITCODE."}
}
catch{$failure=$_;$exitCode=1}
finally{
    $owned=@($started)
    $owned+=@(Get-CimInstance Win32_Process|Where-Object {$_.CommandLine -and $_.CommandLine.Contains($run,[StringComparison]::OrdinalIgnoreCase)}|Select-Object -Expand ProcessId)
    foreach($id in ($owned|Sort-Object -Unique)){Stop-Process -Id $id -Force -ErrorAction SilentlyContinue}
    Start-Sleep 2
    foreach($port in @($MongoPort,$Neo4jBoltPort,$Neo4jHttpPort)){if(Test-NetConnection 127.0.0.1 -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue){Write-Host "[FAIL] Portable provider port $port remained open." -ForegroundColor Red;$exitCode=1}}
}
if($failure){Write-Host "[FAIL] $($failure.Exception.Message)`n$($failure.ScriptStackTrace)" -ForegroundColor Red}
if($exitCode -eq 0){Write-Host "[PASS] Portable provider matrix completed and all loopback processes stopped." -ForegroundColor Green}
exit $exitCode
