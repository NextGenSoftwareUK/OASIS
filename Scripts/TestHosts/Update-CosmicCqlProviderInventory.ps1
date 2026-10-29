#Requires -Version 7.5
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$enumPath = 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/ProviderType.cs'
$registerPath = 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.Register.cs'
$names = @(Get-Content (Join-Path $root $enumPath) | ForEach-Object {
    if ($_ -match '^\s*([A-Za-z][A-Za-z0-9_]*)\s*,?\s*(?://.*)?$' -and $Matches[1] -notin @('None','All','Default')) { $Matches[1] }
})
if (!$names.Count -or @($names | Sort-Object -Unique).Count -ne $names.Count) { throw 'Provider identity inventory is empty or ambiguous.' }
$registrations = @{}
$line = 0
Get-Content (Join-Path $root $registerPath) | ForEach-Object {
    $line++
    if ($_ -match '^\s*case ProviderType\.([A-Za-z0-9_]+):') { $registrations[$Matches[1]] = $line }
}
$proofs = @{
    MongoDBOASIS='CQL_PARITY_CHECKLIST.md'; SQLLiteDBOASIS='CQL_PARITY_CHECKLIST.md'; Neo4jOASIS='CQL_PARITY_CHECKLIST.md'
    OracleDBOASIS='CQL_ORACLE_DEMO_ADMISSION.md'; AzureCosmosDBOASIS='CQL_HOSTED_AZURE_FREE_ADMISSION.md'
    AWSOASIS='AWS_CQL_MVP.md'; EthereumOASIS='ETHEREUM_CQL_MVP.md'; PolygonOASIS='POLYGON_CQL_MVP.md'
    ArbitrumOASIS='ARBITRUM_CQL_MVP.md'; GoogleCloudOASIS='FIRESTORE_CQL_MVP.md'
    BaseOASIS='BASE_CQL_MVP.md'
    AvalancheOASIS='AVALANCHE_CQL_MVP.md'
    CosmosBlockChainOASIS='COSMOS_BLOCKCHAIN_CQL_MVP.md'; PolkadotOASIS='POLKADOT_CQL_MVP.md'; NEAROASIS='NEAR_CQL_MVP.md'
    SolanaOASIS='CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md'; IPFSOASIS='CQL_PROTOCOL_MULTI_RECORD.md'
    SOLIDOASIS='CQL_PROTOCOL_MULTI_RECORD.md'; ActivityPubOASIS='CQL_PROTOCOL_MULTI_RECORD.md'
}
$projects = @( & rg --files (Join-Path $root 'Providers') -g '*.csproj' | Where-Object { $_ -notmatch '(?i)TestProjects|Tests|TestHarness|TestSupport|[\\/]Shared[\\/]|[\\/]Template[\\/]' })
if ($LASTEXITCODE) { throw 'Source project inventory failed.' }
$byName = @{}
foreach ($project in $projects) {
    $name = [IO.Path]::GetFileNameWithoutExtension($project).Replace('NextGenSoftware.OASIS.API.Providers.','')
    if (!$byName.ContainsKey($name)) { $byName[$name] = @() }
    $byName[$name] += [IO.Path]::GetRelativePath($root,$project).Replace('\','/')
}
$report = [Collections.Generic.List[string]]::new()
$report.Add('# All-provider CQL MVP inventory')
$report.Add('')
$report.Add("Generated $([DateTime]::Now.ToString('yyyy-MM-dd')) by Scripts/TestHosts/Update-CosmicCqlProviderInventory.ps1. $($names.Count) concrete enum identities; selectors None/All/Default excluded.")
$enumHash = (Get-FileHash (Join-Path $root $enumPath) -Algorithm SHA256).Hash
$registerHash = (Get-FileHash (Join-Path $root $registerPath) -Algorithm SHA256).Hash
$incoming = (& git -C $root rev-parse --short origin/Development) -join ''
if ($LASTEXITCODE) { throw 'Fetched Development identity unavailable.' }
& git -C $root merge-base --is-ancestor origin/Development HEAD
if ($LASTEXITCODE -notin 0,1) { throw 'Could not determine incoming integration state.' }
$integrated = $LASTEXITCODE -eq 0
$coreCommit = (& git -C (Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core') rev-parse HEAD) -join ''
if ($LASTEXITCODE) { throw 'Core checkout identity unavailable.' }
$report.Add("Source SHA256: ProviderType $enumHash; central registration $registerHash. Fetched Development: $incoming; ancestor of task HEAD: $integrated. Core checkout: $coreCommit. These facts describe the current task checkout, not fresh native acceptance for every row.")
$report.Add('')
$report.Add('This is a source inventory, not certification. Green means only the linked scoped MVP, often local/emulated. Red means MVP proof is missing; a project or registration alone proves no storage/query capability. Registration observations cover active central switch cases only, not direct construction or every alternative registry. Inheritance, aliases, factory mode and source-only projects still require review before implementation.')
$report.Add('')
$report.Add('Hosted Azure remains pending by user instruction. Legacy HyperDrive changes are documentation-only. Do not re-harden green rows without a reproduced bug. Scope: one native bounded multi-record query plus Mongo-paired C#/HTTP/browser proof, with explicit limits; non-storage providers require a capability/gap assessment.')
$report.Add('')
$report.Add('## Next queue')
$report.Add('')
if (!$integrated) {
    $report.Add("Integrate fetched Development $incoming and reconcile Core/gitlink/deployment pins before the next provider change. Do not deploy the entire task branch while migration gates remain open.")
} else {
    $report.Add("Fetched Development $incoming is integrated. See [integration verification](DEVELOPMENT_SYNC_2026-10-29.md). Outgoing Development deployment remains a separate scoped batch.")
}
$report.Add('1. AzureStorageOASIS next: active central registration uses CloudProviderFactory.CreateAzureStorage and native Blob containers. Review CQL admission/native emulator resources, then prove bounded multi-record selection and a Mongo-paired preset. Hosted Azure Cosmos remains pending by user instruction; this is separate Blob Storage scope.')
$report.Add('2. Review the remaining registered storage providers first, then source-only/unregistered identities. Optimism has a Web3Core class but its observed central case is commented out: registration is a separate gap, not inherited readiness.')
$report.Add('')
$report.Add('| Provider identity | Scoped MVP | Source project evidence | Central registration / next gate |')
$report.Add('|---|---|---|---|')
foreach ($name in $names) {
    if ($proofs.ContainsKey($name)) {
        $proof = $proofs[$name]
        if (!(Test-Path (Join-Path $root "Docs/WIP/COSMIC ORM/$proof"))) { throw "Missing proof guide: $proof" }
        $status = "✅ [Scoped proof]($proof)"
        if ($name -eq 'AzureCosmosDBOASIS') { $status += '; emulator only, hosted pending' }
    } elseif ($name -eq 'AzureStorageOASIS') {
        $status = '❌ [Adapter controls only; native/paired pending](AZURE_BLOB_CQL_MVP.md)'
    } else { $status = '❌ Not verified' }
    $source = if ($byName.ContainsKey($name)) { ($byName[$name] | ForEach-Object { "[project](<../../../$_>)" }) -join ', ' } else { 'No exact project-name match; alias/source review required' }
    $registration = if ($registrations.ContainsKey($name)) { "[Active case](<../../../${registerPath}#L$($registrations[$name])>); verify factory/mode" } else { 'No active central case found; review registration/direct construction' }
    if ($name -eq 'AzureStorageOASIS') { $registration += '; CQL/native pairing review required' }
    $report.Add("| $name | $status | $source | $registration |")
}
$extras = @($byName.Keys | Where-Object { $_ -notin $names } | Sort-Object)
$report.Add('')
$report.Add('## Additional source-project names requiring alias/base/helper classification')
$report.Add('')
$report.Add('These are not counted as extra concrete provider identities until their role is verified. They remain in scope for assessment, including wrappers, shared implementations and spelling aliases.')
$report.Add('')
foreach ($name in $extras) { $report.Add("- ${name}: " + (($byName[$name] | ForEach-Object { "[project](<../../../$_>)" }) -join ', ')) }
$report.Add('')
$report.Add('[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md).')
[IO.File]::WriteAllLines((Join-Path $root 'Docs/WIP/COSMIC ORM/CQL_ALL_PROVIDER_MVP_INVENTORY.md'),$report,[Text.UTF8Encoding]::new($false))
Write-Output "Inventoried $($names.Count) identities, $($proofs.Count) identities with scoped proof links, $($registrations.Count) active central cases and $($extras.Count) additional source-project names."
