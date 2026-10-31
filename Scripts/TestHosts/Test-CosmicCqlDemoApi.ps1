#Requires -Version 7.0
param([switch]$NoBuild)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(!$NoBuild){ & (Join-Path $PSScriptRoot 'Run-CosmicCqlDemo.ps1'); if($LASTEXITCODE){throw 'Console demo/build failed.'} }
$work=Join-Path $taskRoot 'TestResults/CosmicCqlDemo'
$dll=Join-Path $taskRoot 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC CQL HTTP demo verification' -Operation {
    if(Get-NetTCPConnection -State Listen -LocalPort 5087 -ErrorAction SilentlyContinue){throw 'Demo port is occupied; no unrelated process will be stopped.'}
    foreach($name in @('demo.db','demo.db-wal','demo.db-shm','demo-manifest.json')){
        if(Test-Path -LiteralPath (Join-Path $work $name)){throw 'Existing fixture requires review before another demo.'}
    }
    $process=Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @(('"'+$dll+'"'),'--serve') `
        -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'api-host.out.log') -RedirectStandardError (Join-Path $work 'api-host.err.log')
    $checks=[Collections.Generic.List[string]]::new()
    try {
        $deadline=[DateTime]::UtcNow.AddSeconds(30);$ready=$false
        do {
            $process.Refresh();if($process.HasExited){throw 'Owned API host exited before readiness; preserve its logs/fixture.'}
            try {$health=Invoke-RestMethod -Uri 'http://127.0.0.1:5087/health' -TimeoutSec 1;$ready=$health.status -eq 'ready'}
            catch {if([DateTime]::UtcNow -ge $deadline){throw};Start-Sleep -Milliseconds 250}
        }while(!$ready -and [DateTime]::UtcNow -lt $deadline)
        if(!$ready){throw 'Owned API host readiness deadline expired.'}
        $schema=Invoke-RestMethod -Uri 'http://127.0.0.1:5087/swagger/v1/swagger.json' -TimeoutSec 10
        if(!$schema.paths.'/api/cql/coffees'.post){throw 'OpenAPI query operation is missing.'}
        if($schema.components.schemas.CoffeeQueryRequest.example.category -ne 'coffee' -or
            $schema.components.schemas.CoffeeQueryRequest.example.provider -ne 'SQLLiteDBOASIS'){throw 'Swagger needs a working prefilled demo request.'}
        $ui=Invoke-WebRequest -Uri 'http://127.0.0.1:5087/swagger/index.html' -TimeoutSec 10
        if($ui.Content -notmatch 'SwaggerUIBundle'){throw 'Swagger UI did not render its local bootstrap.'}
        $checks.Add('OpenAPI and Swagger UI served')
        function Query([hashtable]$request){Invoke-WebRequest -Uri 'http://127.0.0.1:5087/api/cql/coffees' -Method Post -ContentType 'application/json' -Body ($request|ConvertTo-Json -Depth 5) -SkipHttpErrorCheck -TimeoutSec 10}
        $response=Query @{category='coffee';minimumRating=8;orderBy='Price';descending=$false;take=2}
        $body=$response.Content|ConvertFrom-Json
        if($response.StatusCode -ne 200 -or $body.isError -or ($body.result.rows.name -join ',') -ne 'Espresso,Flat White'){throw 'Typed native price query returned unexpected results.'}
        if($body.result.provider -ne 'SQLLiteDBOASIS'){throw 'Provider provenance was lost.'}
        $checks.Add('Typed filters and native ordering preserve provenance')
        $response=Query @{categories=@('coffee','tea');city='London';minimumRating=7;scope='AllEligible';orderBy='Rating';descending=$true}
        $body=$response.Content|ConvertFrom-Json
        if($response.StatusCode -ne 200 -or $body.isError -or ($body.result.rows.name -join ',') -ne 'Espresso,Flat White,Green Tea'){throw 'Nested metadata/membership scope query failed.'}
        $checks.Add('Nested metadata, membership and AllEligible scope')
        $response=Query @{category='coffee';inStockOnly=$false;scope='Named';provider='SQLLiteDBOASIS';orderBy='Price';descending=$false;skip=1;take=2}
        $body=$response.Content|ConvertFrom-Json
        if($response.StatusCode -ne 200 -or $body.isError -or ($body.result.rows.name -join ',') -ne 'Flat White,Latte'){throw 'Named-scope page failed.'}
        $checks.Add('Named scope and native Skip/Take')
        $response=Query @{skip=-1};$body=$response.Content|ConvertFrom-Json
        if($response.StatusCode -ne 400 -or !$body.isError -or $body.errorCode -ne 'CQL_DEMO_REQUEST_INVALID' -or $null -ne $body.result){throw 'Invalid bounds did not reject without results.'}
        $checks.Add('Invalid bounds reject without partial results')
        $response=Query @{scope='Named';provider='Neo4jOASIS'};$body=$response.Content|ConvertFrom-Json
        if($response.StatusCode -ne 400 -or !$body.isError -or $null -ne $body.result){throw 'Unavailable provider scope was not rejected.'}
        $checks.Add('Unavailable providers reject without implicit activation')
    }
    finally {
        $process.Refresh()
        if(!$process.HasExited){
            try {Invoke-RestMethod -Uri 'http://127.0.0.1:5087/api/cql/demo/shutdown' -Method Post -TimeoutSec 5 | Out-Null}
            catch {Write-Warning ('Owned host graceful shutdown request failed: '+$_.Exception.Message)}
            if(!$process.WaitForExit(10000)){
                $process.Kill($true);$process.WaitForExit();throw 'Owned host required forced stop; preserve its fixture for inspection.'
            }
        }
        $exitCode=$process.ExitCode;$process.Dispose()
        if($exitCode -ne 0){throw 'Owned host failed; preserve its logs and any fixture.'}
    }
    if(Get-NetTCPConnection -State Listen -LocalPort 5087 -ErrorAction SilentlyContinue){throw 'Demo listener remained after shutdown.'}
    foreach($name in @('demo.db','demo.db-wal','demo.db-shm','demo-manifest.json')){
        if(Test-Path -LiteralPath (Join-Path $work $name)){throw 'Successful owned fixture teardown is incomplete.'}
    }
    $proof=Get-Content -LiteralPath (Join-Path $work 'demo-artifacts.json') -Raw | ConvertFrom-Json
    foreach($identity in $proof.assemblies){
        if((Get-FileHash -LiteralPath $identity.producer).Hash -ne $identity.sha256 -or (Get-FileHash -LiteralPath $identity.consumer).Hash -ne $identity.sha256){throw 'API host artifact identity changed during verification.'}
    }
    $proof.reportSha256=(Get-FileHash -LiteralPath (Join-Path $work 'api-demo-result.json')).Hash
    $proof | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'api-demo-artifacts.json') -Encoding utf8
    @{status='passed';checks=$checks.ToArray();hostStopped=$true;fixtureRemoved=$true} | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $work 'api-smoke.json') -Encoding utf8
    Write-Output ('CQL API/Swagger: '+$checks.Count+' checks passed; owned host stopped and fixture removed.')
}
