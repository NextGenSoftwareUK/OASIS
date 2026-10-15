[CmdletBinding()]
param([Parameter(Mandatory)][switch]$ReviewedSyntheticFixture)
$ErrorActionPreference='Stop'
if(!$ReviewedSyntheticFixture){throw 'Review the failed synthetic fixture and retain compact failure evidence before cleanup.'}
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/SubscriptionUsageCurrent'
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),'OpenOrCreate','ReadWrite','None')
$owned=$null
try {
    foreach($port in 18545,18546,27029){if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw "Owned fixture port $port is occupied; refuse cleanup."}}
    $runtime='C:\Program Files\MongoDB\Server\8.3\bin\mongod.exe'
    if((& $runtime --version | Select-Object -First 1) -ne 'db version v8.3.11'){throw 'Inspect retained Mongo version before cleanup.'}
    $owned=Start-Process $runtime -ArgumentList @('--dbpath',(Join-Path $work 'mongo-data'),'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $work 'mongo-current.log'),'--logappend') -WindowStyle Hidden -PassThru
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    do {
        $listener=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
        if($listener){if(@($listener | Where-Object OwningProcess -ne $owned.Id).Count){throw 'Listener ownership mismatch'};break}
        if($owned.HasExited -or [DateTime]::UtcNow -gt $deadline){throw 'Retained Mongo startup failed'}
        Start-Sleep -Milliseconds 100
    }while($true)
    $dll=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/bin/Debug/net10.0'
    $framework=Get-ChildItem -LiteralPath 'C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App' -Directory | Where-Object Name -Like '10.0.*' | Sort-Object { [version]$_.Name } | Select-Object -Last 1
    if(!$framework){throw 'Existing .NET10 ASP.NET runtime required; no install attempted.'}
    Add-Type -Path (Join-Path $framework.FullName 'Microsoft.Extensions.Logging.Abstractions.dll')
    foreach($assembly in 'MongoDB.Bson.dll','MongoDB.Driver.dll'){Add-Type -Path (Join-Path $dll $assembly)}
    $client=[MongoDB.Driver.MongoClient]::new('mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=5000')
    $database='oasis_web3_prepared_native'
    $cursor=$client.GetDatabase($database).ListCollectionNames();$names=@()
    try {while($cursor.MoveNext([Threading.CancellationToken]::None)){$names+=@($cursor.Current)}} finally {$cursor.Dispose()}
    if(@($names | Where-Object {$_ -ne 'subscription_usage_outbox_v1'}).Count){throw 'Unknown collection; preserve database.'}
    if($names.Count){$client.DropDatabase($database,[Threading.CancellationToken]::None);Write-Output "Removed reviewed synthetic database: $database"}
    $path=[IO.Path]::GetFullPath((Join-Path $dll 'TestResults/Web3CoreRouting/routing.db'))
    $expected=Join-Path $taskRoot 'Providers\Blockchain\TestProjects\NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests\bin\Debug\net10.0\TestResults\Web3CoreRouting\routing.db'
    if($path -ne $expected){throw 'SQLite fixture path mismatch'}
    if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path;Write-Output "Removed reviewed synthetic SQLite fixture: $path"}
} finally {
    if($owned -and !$owned.HasExited){Stop-Process -Id $owned.Id;$owned.WaitForExit()}
    $lease.Dispose()
}
