param([switch]$Apply)
$ErrorActionPreference = 'Stop'
trap { Write-Error ('Staging isolation failed: ' + $_.Exception.GetType().Name + '. No secrets are included in this diagnostic.') -ErrorAction Continue; exit 1 }
$appDatabase = 'oasis_staging'
$ledgerDatabase = 'oasis_subscription_accounting_staging'
$marker = '_staging_isolation'

# Reuse already-built MongoDB driver assemblies; no installation, download or build.
$driverDirectory = Join-Path $PSScriptRoot '../ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin/Debug/net10.0'
$loggingAssembly = 'C:/Program Files/dotnet/shared/Microsoft.AspNetCore.App/10.0.12/Microsoft.Extensions.Logging.Abstractions.dll'
if (-not (Test-Path -LiteralPath $loggingAssembly)) { throw 'Required installed .NET logging assembly is absent' }
[void][Reflection.Assembly]::LoadFrom($loggingAssembly)
foreach ($file in @('MongoDB.Bson.dll','DnsClient.dll','SharpCompress.dll','Snappier.dll','ZstdSharp.dll','MongoDB.Driver.dll')) {
    $path = [IO.Path]::GetFullPath((Join-Path $driverDirectory $file))
    if (-not (Test-Path -LiteralPath $path)) { throw "Required reusable driver assembly is absent: $file" }
    [void][Reflection.Assembly]::LoadFrom($path)
}
function Read-ServiceVariables($service, $environment) {
    $raw = railway variable list -s $service -e $environment --json
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read effective Railway configuration' }
    return $raw | ConvertFrom-Json
}
function Set-StagingVariable($service, $key, $value) {
    # Secret-bearing values go through stdin, never argv, files or logs. Do not deploy.
    $value | railway variable set $key --stdin --skip-deploys -s $service -e staging | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Staging variable update failed for $key; reconcile before continuing" }
}

$snapshot = railway status --json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $snapshot.id -ne '712f50f6-3b7b-46d4-b229-44b49922f16b') { throw 'Wrong Railway project' }
$stage = $snapshot.environments.edges.node | Where-Object name -eq staging
$production = $snapshot.environments.edges.node | Where-Object name -eq production
$services = @($stage.serviceInstances.edges.node | Where-Object serviceName -Match '^WEB(4|5|6|7|8|9|10) ')
if ($services.Count -ne 7) { throw 'Expected exactly seven staging API services' }

$plans = @()
$productionBaseline = @{}
foreach ($service in $services) {
    $stageValues = Read-ServiceVariables $service.serviceName staging
    $prodValues = Read-ServiceVariables $service.serviceName production
    $prodDna = $prodValues.OASIS_DNA_JSON | ConvertFrom-Json
    $stageDna = $stageValues.OASIS_DNA_JSON | ConvertFrom-Json
    if (-not $stageDna.OASIS.StorageProviders.MongoDBOASIS.ConnectionString) { throw 'Missing staging Mongo configuration' }
    if ($prodDna.OASIS.StorageProviders.MongoDBOASIS.DBName -in @($appDatabase,$ledgerDatabase)) { throw 'Target overlaps production database' }
    if ($prodValues.SUBSCRIPTION_MONGODB_DATABASE -in @($appDatabase,$ledgerDatabase)) { throw 'Target overlaps production accounting database' }
    $productionBaseline[$service.serviceName] = $prodValues.OASIS_DNA_JSON
    $stageDna.OASIS.StorageProviders.MongoDBOASIS.DBName = $appDatabase
    $plans += [pscustomobject]@{service=$service.serviceName;dna=$stageDna;values=$stageValues}
}

$web4 = $plans | Where-Object service -eq 'WEB4 OASIS API'
$connection = $web4.dna.OASIS.StorageProviders.MongoDBOASIS.ConnectionString
if (@($plans | Where-Object { $_.dna.OASIS.StorageProviders.MongoDBOASIS.ConnectionString -cne $connection }).Count) { throw 'Staging services do not all use the verified existing Mongo connection' }
$settings = [MongoDB.Driver.MongoClientSettings]::FromConnectionString($connection)
$settings.ServerSelectionTimeout = [TimeSpan]::FromSeconds(15)
$settings.ConnectTimeout = [TimeSpan]::FromSeconds(10)
$client = [MongoDB.Driver.MongoClient]::new($settings)
$cancel = [Threading.CancellationToken]::None

foreach ($name in @($appDatabase,$ledgerDatabase)) {
    if ($name -notmatch '^oasis(_subscription_accounting)?_staging$') { throw 'Invalid staging target' }
    $database = $client.GetDatabase($name, $null)
    $cursor = $database.ListCollectionNames([MongoDB.Driver.ListCollectionNamesOptions]::new(), $cancel)
    $collections = @()
    try { while ($cursor.MoveNext($cancel)) { $collections += $cursor.Current } } finally { $cursor.Dispose() }
    if (@($collections | Where-Object { $_ -ne $marker }).Count) { throw 'Target is not an empty owned staging namespace; nothing will be erased' }
    if ($Apply -and $marker -notin $collections) {
        $database.CreateCollection($marker, [MongoDB.Driver.CreateCollectionOptions]::new(), $cancel)
    }
    $command = [MongoDB.Driver.BsonDocumentCommand[MongoDB.Bson.BsonDocument]]::new([MongoDB.Bson.BsonDocument]::Parse('{"dbStats":1}'))
    $stats = $database.RunCommand[MongoDB.Bson.BsonDocument]($command, $null, $cancel)
    if ($stats['objects'].ToInt64() -ne 0) { throw 'Staging namespace contains records; refusing empty-installation setup' }
    "$name verified empty; apply=$Apply"
}
if ($Apply) {
    foreach ($plan in $plans) {
        Set-StagingVariable $plan.service 'OASIS_DNA_JSON' ($plan.dna | ConvertTo-Json -Depth 100 -Compress)
        if ($plan.service -eq 'WEB4 OASIS API') {
            # The accounting owner resolves these explicit settings independently of app DBName.
            Set-StagingVariable $plan.service 'SUBSCRIPTION_MONGODB_DATABASE' $ledgerDatabase
            Set-StagingVariable $plan.service 'SUBSCRIPTION_LEDGER_INITIAL_STATE' 'empty-new-installation'
            if ($plan.values.SUBSCRIPTION_MONGODB_CONNECTION_STRING) {
                Set-StagingVariable $plan.service 'SUBSCRIPTION_MONGODB_CONNECTION_STRING' $connection
            }
        }
        $readback = Read-ServiceVariables $plan.service staging
        if (($readback.OASIS_DNA_JSON | ConvertFrom-Json).OASIS.StorageProviders.MongoDBOASIS.DBName -ne $appDatabase) { throw 'Staging app database readback mismatch' }
        if ($plan.service -eq 'WEB4 OASIS API' -and $readback.SUBSCRIPTION_MONGODB_DATABASE -ne $ledgerDatabase) { throw 'Staging accounting database readback mismatch' }
        $prodReadback = Read-ServiceVariables $plan.service production
        if ($prodReadback.OASIS_DNA_JSON -cne $productionBaseline[$plan.service]) { throw 'Production DNA changed concurrently; investigate without overwriting it' }
        "$($plan.service): staging database configuration verified; production DNA unchanged; no deployment triggered"
    }
}
