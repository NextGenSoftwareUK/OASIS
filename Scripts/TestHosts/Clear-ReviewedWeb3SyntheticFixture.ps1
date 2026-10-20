[CmdletBinding()]
param([Parameter(Mandatory)][switch]$ReviewedSyntheticFixture,[switch]$CosmosOffline,[switch]$InspectOnly)
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
    if($CosmosOffline){
        # Exact offline signing/pipe fixture only. Never target the retained native Cosmos journal/database.
        $offline=$client.GetDatabase('usage_cosmos_signed_test')
        $offlineNames=@();$offlineCursor=$offline.ListCollectionNames()
        try{while($offlineCursor.MoveNext([Threading.CancellationToken]::None)){$offlineNames+=@($offlineCursor.Current)}}finally{$offlineCursor.Dispose()}
        if(@($offlineNames|Where-Object{$_ -ne 'subscription_usage_outbox_v1'}).Count){throw 'Unknown offline collection; preserve database.'}
        $command=[MongoDB.Driver.BsonDocumentCommand[MongoDB.Bson.BsonDocument]]::new([MongoDB.Bson.BsonDocument]::Parse('{"find":"subscription_usage_outbox_v1","filter":{},"batchSize":2}'))
        $reply=$offline.RunCommand[MongoDB.Bson.BsonDocument]($command,$null,[Threading.CancellationToken]::None)
        if($reply['cursor']['id'].ToInt64() -ne 0){throw 'Offline inventory is incomplete; preserve database.'}
        $batch=$reply['cursor']['firstBatch'].AsBsonArray
        if($batch.Count -ne 1){throw 'Expected exactly one reviewed offline outbox; preserve database.'}
        $record=$batch[0].AsBsonDocument
        if($record -isnot [MongoDB.Bson.BsonDocument]){throw ('Unexpected document type: '+$record.GetType().FullName)}
        Write-Output ('Offline document field names: '+($record.Names -join ','))
        $evidence=$record.ToString()
        $evidenceHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($evidence)))
        Write-Output ('Offline evidence SHA256: '+$evidenceHash)
        Write-Output ('Offline state: '+$record['State'].AsString+'; attempts='+$record['ProviderAttempts'].AsBsonArray.Count+'; policy='+$record['Policy'].ToString())
        if($InspectOnly){return}
        # Reviewed 2026-10-17: offline SDK signing + parent pipe only, never broadcast.
        if($evidenceHash -eq 'ED79BFEEBD6B82CF00A67D495511B50BB54046DE66A0109DCE9FCB08AF3132AD'){
            # Reviewed 2026-10-20: SDK import failed before signing/preparation, from the exact failed240 report.
            if($record['State'].AsString -ne 'executing' -or $record['Endpoint'].AsString -ne 'POST /offline-test' -or
                $record['ProviderAttempts'].AsBsonArray.Count -ne 0 -or $record['Receipts'].AsBsonArray.Count -ne 0 -or
                !$record['Settlement'].IsBsonNull -or $record['NativePreparationBytes'].ToInt64() -ne 0 -or
                $record['NativeFeeExposureUsd'].ToDecimal() -ne 0){throw 'Not the exact reviewed pre-signing import failure; preserve database.'}
            $evidencePath=Join-Path $work 'cosmos-offline-import-failure-ED79BFEEBD6B.json'
        }elseif($evidenceHash -eq '9B8218F990C086A8BB6EC71CA4A7E759D2D3BD85A4F2B3E6B8F011761A19B60E'){
            $attempt=$record['ProviderAttempts'].AsBsonArray[0].AsBsonDocument
            if($record['State'].AsString -ne 'executing' -or $record['Endpoint'].AsString -ne 'POST /offline-test' -or
                $record['Receipts'].AsBsonArray.Count -ne 0 -or !$record['Settlement'].IsBsonNull -or
                $attempt['Provider'].AsString -ne 'CosmosBlockChainOASIS' -or !$attempt['AcknowledgedAtUtc'].IsBsonNull -or
                $attempt['NativePreparation']['ChainId'].AsString -ne 'offline-test-chain'){throw 'Not the reviewed offline-only preparation; preserve database.'}
            $evidencePath=Join-Path $work 'cosmos-offline-reviewed-evidence.json'
        }else{throw 'Offline evidence changed; preserve it for a new review.'}
        if((Test-Path -LiteralPath $evidencePath) -and (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash -ne $evidenceHash){throw 'Different retained review evidence already exists.'}
        [IO.File]::WriteAllText($evidencePath,$evidence,[Text.UTF8Encoding]::new($false))
        if((Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash -ne $evidenceHash){throw 'Offline evidence export verification failed.'}
        $client.DropDatabase('usage_cosmos_signed_test',[Threading.CancellationToken]::None)
        Write-Output 'Removed only reviewed offline usage_cosmos_signed_test; original evidence retained in fixed JSON report. Native Cosmos journal/ledger untouched.'
        return
    }
    $database='oasis_web3_prepared_native'
    $cursor=$client.GetDatabase($database).ListCollectionNames();$names=@()
    try {while($cursor.MoveNext([Threading.CancellationToken]::None)){$names+=@($cursor.Current)}} finally {$cursor.Dispose()}
    if(@($names | Where-Object {$_ -ne 'subscription_usage_outbox_v1'}).Count){throw 'Unknown collection; preserve database.'}
    & {
        $command=[MongoDB.Driver.BsonDocumentCommand[MongoDB.Bson.BsonDocument]]::new([MongoDB.Bson.BsonDocument]::Parse('{"find":"subscription_usage_outbox_v1","filter":{},"projection":{"State":1,"Policy":1,"Endpoint":1,"ProviderAttempts.Provider":1,"ProviderAttempts.ClientRequestId":1,"ProviderAttempts.ProviderRequestId":1,"ProviderAttempts.NativePreparation.ChainId":1,"ProviderAttempts.NativePreparation.Sender":1},"batchSize":1000}'))
        $reply=$client.GetDatabase($database).RunCommand[MongoDB.Bson.BsonDocument]($command,$null,[Threading.CancellationToken]::None)
        if($reply['cursor']['id'].ToInt64() -ne 0){throw 'Inspection exceeds bounded batch; preserve database.'}
        $records=$reply['cursor']['firstBatch'].AsBsonArray
        foreach($record in $records){
            if($record['State'].AsString -ne 'executing' -or
                $record['Endpoint'].AsString -notin @('local native storage fixture','local prepared replication fixture','local prepared selection fixture') -or
                $record['Policy']['PricingCatalogueVersion'].AsString -ne 'local-test' -or
                $record['Policy']['EstimatedCostUsd'].ToDecimal() -ne 0 -or
                $record['Policy']['FixedCostUsd'].ToDecimal() -ne 0){throw 'Unrecognized synthetic policy/state; preserve database.'}
            foreach($attempt in $record['ProviderAttempts'].AsBsonArray){
                if($attempt['Provider'].AsString -notin @('PolygonOASIS','SQLLiteDBOASIS')){throw 'Unrecognized provider; preserve database.'}
                if($attempt.Contains('NativePreparation') -and !$attempt['NativePreparation'].IsBsonNull){
                    if($attempt['NativePreparation']['ChainId'].AsString -ne '31337' -or
                        $attempt['NativePreparation']['Sender'].AsString.ToLowerInvariant() -ne '0xf39fd6e51aad88f6f4ce6ab8827279cfffb92266'){throw 'Not the local synthetic signer/chain; preserve database.'}
                }
            }
        }
        $evidence=$records.ToString()
        $evidencePath=Join-Path $work 'web3-synthetic-reviewed-metadata.json'
        [IO.File]::WriteAllText($evidencePath,$evidence,[Text.UTF8Encoding]::new($false))
        Write-Output ('Reviewed synthetic records: '+$records.Count+'; metadata SHA256: '+(Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash)
    }
    if($InspectOnly){return}
    if($names.Count){$client.DropDatabase($database,[Threading.CancellationToken]::None);Write-Output "Removed reviewed synthetic database: $database"}
    $path=[IO.Path]::GetFullPath((Join-Path $dll 'TestResults/Web3CoreRouting/routing.db'))
    $expected=Join-Path $taskRoot 'Providers\Blockchain\TestProjects\NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests\bin\Debug\net10.0\TestResults\Web3CoreRouting\routing.db'
    if($path -ne $expected){throw 'SQLite fixture path mismatch'}
    if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path;Write-Output "Removed reviewed synthetic SQLite fixture: $path"}
} finally {
    if($owned -and !$owned.HasExited){Stop-Process -Id $owned.Id;$owned.WaitForExit()}
    $lease.Dispose()
}
