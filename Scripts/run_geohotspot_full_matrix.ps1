<#
.SYNOPSIS
Runs the automated GeoHotSpot/GeoNFT/quest verification matrix and publishes evidence.
.DESCRIPTION
Local build/unit/Unity stages always run. Live stages run when their explicit inputs are
provided. Missing live credentials are reported as SKIPPED rather than silently passed.
ProviderProfilesPath is a JSON array of objects with name, web4BaseUrl, web5BaseUrl and
credentialPath; each profile runs the same destructive-but-self-resetting Our World test.
#>
[CmdletBinding()]
param(
    [string]$OurWorldPath = 'C:\Source\Our-World',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\TestResults\GeoHotSpotMatrix'),
    [string]$ProviderProfilesPath,
    [string]$ConcurrencyFixturePath,
    [switch]$SkipUnity,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$output = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force -Path $output | Out-Null
$results = [Collections.Generic.List[object]]::new()

function Add-Result([string]$area,[string]$name,[string]$status,[double]$seconds,[string]$evidence) {
    $results.Add([ordered]@{ area=$area; name=$name; status=$status; durationSeconds=[Math]::Round($seconds,3); evidence=$evidence; timestampUtc=[DateTime]::UtcNow.ToString('O') })
    $colour = if ($status -eq 'PASS') {'Green'} elseif ($status -eq 'SKIP') {'Yellow'} else {'Red'}
    Write-Host "[$status] $area :: $name" -ForegroundColor $colour
}
function Invoke-Case([string]$area,[string]$name,[scriptblock]$body) {
    $watch=[Diagnostics.Stopwatch]::StartNew()
    try { $global:LASTEXITCODE=0; & $body; if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "Process exited with $LASTEXITCODE" }; Add-Result $area $name PASS $watch.Elapsed.TotalSeconds 'Completed without error.' }
    catch { Add-Result $area $name FAIL $watch.Elapsed.TotalSeconds $_.Exception.Message }
}
function Add-Skip([string]$area,[string]$name,[string]$why) { Add-Result $area $name SKIP 0 $why }

Push-Location $repoRoot
try {
    if (!$SkipBuild) {
        Invoke-Case Build 'WEB5 Release build' { dotnet build 'STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj' --no-restore -c Release -p:UseSharedCompilation=false -v:q }
    } else { Add-Skip Build 'WEB5 Release build' 'Disabled by -SkipBuild.' }

    Invoke-Case API 'GeoHotSpot focused contract suite' {
        dotnet test 'STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI.UnitTests/NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.csproj' --no-restore -c Release --filter 'FullyQualifiedName~GeoHotSpot' -p:UseSharedCompilation=false --logger 'trx;LogFileName=web5-geohotspot.trx' --results-directory $output -v:q
    }
    Invoke-Case Policy 'GeoNFT/GeoHotSpot spawn combination matrix' {
        dotnet run --project 'Tests/GeoNFTCollectionRules/GeoNFTCollectionRules.csproj' -c Release | Tee-Object -FilePath (Join-Path $output 'spawn-policy.log')
    }
    Invoke-Case Providers 'Disposable local provider persistence' {
        & (Join-Path $PSScriptRoot 'run_local_provider_matrix.ps1') -OutputPath (Join-Path $output 'providers')
    }

    if ($SkipUnity) { Add-Skip Unity 'Our World EditMode and PlayMode verification' 'Disabled by -SkipUnity.' }
    elseif (!(Test-Path -LiteralPath $UnityPath)) { Add-Skip Unity 'Our World EditMode and PlayMode verification' "Unity executable not found: $UnityPath" }
    else {
        Invoke-Case Unity 'Our World EditMode and PlayMode verification' {
            $unityXml=Join-Path $output 'unity-results.xml'; $unityLog=Join-Path $output 'unity.log'
            $openEditor = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*$OurWorldPath*" } | Select-Object -First 1
            if ($openEditor) {
                $request=Join-Path $OurWorldPath 'Library/GeoHotSpotMatrix.request'; $result=Join-Path $OurWorldPath 'BuildLogs/geohotspot-matrix-unity.json'
                Remove-Item -LiteralPath $result -Force -ErrorAction SilentlyContinue
                New-Item -ItemType File -Force -Path $request | Out-Null
                $deadline=[DateTime]::UtcNow.AddMinutes(5)
                while (!(Test-Path $result) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 2 }
                if (!(Test-Path $result)) { throw 'The open Unity editor did not finish the requested matrix tests within five minutes.' }
                Copy-Item $result (Join-Path $output 'unity-open-editor.json') -Force
                $unityResult=Get-Content -Raw $result|ConvertFrom-Json
                if ([int]$unityResult.failCount -gt 0) { throw "$($unityResult.failCount) Unity tests failed: $($unityResult.message)" }
            } else {
                Remove-Item -LiteralPath $unityXml -Force -ErrorAction SilentlyContinue
                $unityProcess = [Diagnostics.Process]::new()
                $unityProcess.StartInfo = [Diagnostics.ProcessStartInfo]::new($UnityPath)
                $unityProcess.StartInfo.UseShellExecute = $false
                $unityProcess.StartInfo.CreateNoWindow = $true
                foreach ($argument in @('-batchmode','-nographics','-projectPath',$OurWorldPath,'-runTests','-testPlatform','EditMode','-testFilter','QuestEffectsRuntimeTests','-testResults',$unityXml,'-logFile',$unityLog)) {
                    $null = $unityProcess.StartInfo.ArgumentList.Add($argument)
                }
                if (!$unityProcess.Start()) { throw 'Unity test process could not be started.' }
                $unityProcess.WaitForExit()
                if (!(Test-Path $unityXml)) { throw 'Unity did not produce a test result file.' }
                [xml]$xml=Get-Content -Raw $unityXml
                $failed=[int]$xml.'test-run'.failed
                if ($failed -gt 0) { throw "$failed Unity tests failed. See $unityXml" }
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($ProviderProfilesPath)) {
        Add-Skip Providers 'Configured provider profiles' 'Supply -ProviderProfilesPath to run live MongoDB, SQLite, Neo4j and other provider profiles.'
    } else {
        $profiles=@(Get-Content -Raw $ProviderProfilesPath | ConvertFrom-Json)
        foreach ($profile in $profiles) {
            Invoke-Case Providers "Live contract: $($profile.name)" {
                & (Join-Path $PSScriptRoot 'test_our_world_tree_collection.ps1') -Apply -Web4BaseUrl $profile.web4BaseUrl -Web5BaseUrl $profile.web5BaseUrl -Provider $profile.name -CredentialPath $profile.credentialPath
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($ConcurrencyFixturePath)) {
        Invoke-Case Concurrency 'Disposable two-avatar and two-replica HTTP contract' { & (Join-Path $PSScriptRoot 'run_local_geohotspot_resilience.ps1') -Mode Concurrency }
        Invoke-Case Recovery 'Disposable process interruption and durable replay contract' { & (Join-Path $PSScriptRoot 'run_local_geohotspot_resilience.ps1') -Mode RestartReplay }
    } else {
        Invoke-Case Concurrency 'Two avatars and multiple replicas' { & (Join-Path $PSScriptRoot 'test_geohotspot_live_resilience.ps1') -FixturePath $ConcurrencyFixturePath -Mode Concurrency }
        Invoke-Case Recovery 'Restart and replay pending transaction' { & (Join-Path $PSScriptRoot 'test_geohotspot_live_resilience.ps1') -FixturePath $ConcurrencyFixturePath -Mode RestartReplay }
    }
}
finally { Pop-Location }

$summary=[ordered]@{
    generatedAtUtc=[DateTime]::UtcNow.ToString('O'); pass=@($results|Where-Object status -eq PASS).Count
    fail=@($results|Where-Object status -eq FAIL).Count; skip=@($results|Where-Object status -eq SKIP).Count; results=$results
}
$jsonPath=Join-Path $output 'geohotspot-matrix.json'
$summary | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 $jsonPath
$rows=$results|ForEach-Object { "| $($_.area) | $($_.name) | $($_.status) | $($_.durationSeconds) | $($_.evidence -replace '\|','/') |" }
@("# GeoHotSpot automated matrix",'',"Generated: $($summary.generatedAtUtc)",'',"**PASS $($summary.pass) · FAIL $($summary.fail) · SKIP $($summary.skip)**",'','| Area | Case | Result | Seconds | Evidence |','|---|---|---:|---:|---|') + $rows | Set-Content -Encoding UTF8 (Join-Path $output 'geohotspot-matrix.md')
$htmlRows=$results|ForEach-Object { "<tr class='$($_.status.ToLower())'><td>$([Net.WebUtility]::HtmlEncode($_.area))</td><td>$([Net.WebUtility]::HtmlEncode($_.name))</td><td>$($_.status)</td><td>$($_.durationSeconds)</td><td>$([Net.WebUtility]::HtmlEncode($_.evidence))</td></tr>" }
@("<!doctype html><meta charset='utf-8'><title>GeoHotSpot matrix</title><style>body{font:15px Segoe UI;margin:32px;background:#071923;color:#def}table{border-collapse:collapse;width:100%}th,td{border:1px solid #26788c;padding:8px}.pass{background:#123d31}.fail{background:#5a2028}.skip{background:#574916}</style>","<h1>GeoHotSpot automated matrix</h1><p>PASS $($summary.pass) · FAIL $($summary.fail) · SKIP $($summary.skip)</p><table><tr><th>Area</th><th>Case</th><th>Result</th><th>Seconds</th><th>Evidence</th></tr>") + $htmlRows + '</table>' | Set-Content -Encoding UTF8 (Join-Path $output 'geohotspot-matrix.html')
Write-Host "Reports: $output"
if ($summary.fail -gt 0) { exit 1 }
