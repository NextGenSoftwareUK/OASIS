<#
.SYNOPSIS
Runs disposable provider persistence verification without Docker.
.DESCRIPTION
SQLite always runs against per-test temporary database files. MongoDB and Neo4j run only
when their explicit local integration environment variables are set and the corresponding
loopback port is reachable. No shared or Railway endpoint is inferred or contacted.
#>
[CmdletBinding()]
param([string]$OutputPath=(Join-Path $PSScriptRoot '..\TestResults\GeoHotSpotMatrix\providers'))
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$out=[IO.Path]::GetFullPath($OutputPath); New-Item -ItemType Directory -Force $out|Out-Null
$rows=[Collections.Generic.List[object]]::new()
function Add([string]$provider,[string]$status,[string]$evidence){$rows.Add([ordered]@{provider=$provider;status=$status;evidence=$evidence;timestampUtc=[DateTime]::UtcNow.ToString('O')});Write-Host "[$status] $provider - $evidence"}
function Run([string]$provider,[string]$project){
 $dir=Join-Path $out $provider.ToLowerInvariant();New-Item -ItemType Directory -Force $dir|Out-Null
 & dotnet test $project -c Release --logger "trx;LogFileName=$($provider.ToLowerInvariant())-provider.trx" --results-directory $dir -v:quiet
 if($LASTEXITCODE -ne 0){Add $provider FAIL "Integration suite failed; see $dir";return $false}
 Add $provider PASS "Real provider persistence suite passed; TRX: $dir";return $true
}
Push-Location $root
try{
 $ok=Run SQLLiteDBOASIS 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests.csproj'
 $mongo=$env:MONGODBOASIS_CONNECTIONSTRING
 if([string]::IsNullOrWhiteSpace($mongo)){Add MongoDBOASIS SKIP 'Set MONGODBOASIS_CONNECTIONSTRING and MONGODBOASIS_DBNAME for a disposable loopback database.'}
 elseif($mongo -notmatch '^mongodb(?:\+srv)?://(?:localhost|127\.0\.0\.1)(?::|/)'){Add MongoDBOASIS FAIL 'Only loopback MongoDB is accepted by the local provider runner.';$ok=$false}
 else{$ok=(Run MongoDBOASIS 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests.csproj') -and $ok}
 $neo=$env:NEO4JOASIS_HOST
 if([string]::IsNullOrWhiteSpace($neo)){Add Neo4jOASIS SKIP 'Set NEO4JOASIS_HOST, NEO4JOASIS_USERNAME and NEO4JOASIS_PASSWORD for a disposable loopback database.'}
 elseif($neo -notmatch '^(?:bolt|neo4j)://(?:localhost|127\.0\.0\.1)(?::|/)'){Add Neo4jOASIS FAIL 'Only loopback Neo4j is accepted by the local provider runner.';$ok=$false}
 else{$ok=(Run Neo4jOASIS 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj') -and $ok}
}finally{Pop-Location}
$summary=[ordered]@{generatedAtUtc=[DateTime]::UtcNow.ToString('O');pass=@($rows|? status -eq PASS).Count;fail=@($rows|? status -eq FAIL).Count;skip=@($rows|? status -eq SKIP).Count;results=$rows}
$summary|ConvertTo-Json -Depth 6|Set-Content -Encoding utf8 (Join-Path $out 'local-provider-matrix.json')
@('# Local provider matrix','',"PASS $($summary.pass) · FAIL $($summary.fail) · SKIP $($summary.skip)",'','| Provider | Status | Evidence |','|---|---:|---|') + @($rows|%{"| $($_.provider) | $($_.status) | $($_.evidence -replace '\|','/') |"}) | Set-Content -Encoding utf8 (Join-Path $out 'local-provider-matrix.md')
if($summary.fail){exit 1}
