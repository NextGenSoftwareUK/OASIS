[CmdletBinding()]
param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [string]$PackageDirectory = 'artifacts\unity\com.nextgensoftware.oasis.edge',
    [string]$LogDirectory = 'artifacts'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$packageRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $PackageDirectory))
$logRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $LogDirectory))
if (-not $packageRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath (Join-Path $packageRoot 'build-manifest.json') -PathType Leaf)) {
    throw "A generated, manifested Unity Edge package inside '$artifactsRoot' is required."
}
if (-not $logRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "The Unity validation log directory must remain inside '$artifactsRoot'."
}
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor not found: $UnityEditor" }

$linkerConfigPath = Join-Path $packageRoot 'Runtime\link.xml'
if (-not (Test-Path -LiteralPath $linkerConfigPath -PathType Leaf)) {
    throw "Generated Edge package is missing Runtime/link.xml."
}
[xml]$linkerConfig = Get-Content -LiteralPath $linkerConfigPath -Raw
$preservedAssemblies = @($linkerConfig.linker.assembly | ForEach-Object { [string]$_.fullname })
$requiredRuntimeAssemblies = @(
    'NextGenSoftware.OGEngine.Shared',
    'NextGenSoftware.OGEngine.Client.Edge',
    'NextGenSoftware.OASIS.Edge.Runtime',
    'NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS',
    'Microsoft.Data.Sqlite',
    'SQLitePCLRaw.batteries_v2',
    'SQLitePCLRaw.core',
    'SQLitePCLRaw.provider.e_sqlite3',
    'Newtonsoft.Json',
    'System.Text.Json'
)
foreach ($assemblyName in $requiredRuntimeAssemblies) {
    if ($assemblyName -notin $preservedAssemblies) {
        throw "Generated Edge package does not preserve the runtime-reflected assembly '$assemblyName' for IL2CPP."
    }
}

$projectRoot = Join-Path $artifactsRoot 'unity-edge-validation-project'
if (Test-Path -LiteralPath $projectRoot) { Remove-Item -LiteralPath $projectRoot -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Assets') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Packages') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'ProjectSettings') -Force | Out-Null
$sampleSource = Join-Path $packageRoot 'Samples~\QuickStart'
if (-not (Test-Path -LiteralPath $sampleSource -PathType Container)) {
    throw 'The public Quick Start sample is missing from the generated package.'
}
Copy-Item -LiteralPath $sampleSource -Destination (Join-Path $projectRoot 'Assets\OASISEdgeQuickStart') `
    -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Assets\Editor') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'Scripts\UnityValidation\OASISEdgePackageValidator.cs') `
    -Destination (Join-Path $projectRoot 'Assets\Editor\OASISEdgePackageValidator.cs') -Force

$packageReference = 'file:' + $packageRoot.Replace('\', '/')
@{
    dependencies = [ordered]@{ 'com.nextgensoftware.oasis.edge' = $packageReference }
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $projectRoot 'Packages\manifest.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') `
    -Value "m_EditorVersion: 2022.3.62f3`nm_EditorVersionWithRevision: 2022.3.62f3 (e9b2a6e6c3a0)" -Encoding utf8

$logPath = Join-Path $logRoot 'unity-edge-validation.log'
$unityProcess = Start-Process -FilePath $UnityEditor -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', $projectRoot,
    '-executeMethod', 'NextGenSoftware.OASIS.Edge.Unity.Editor.OASISEdgePackageValidator.Validate',
    '-logFile', $logPath
) -WindowStyle Hidden -Wait -PassThru
if ($unityProcess.ExitCode -ne 0) {
    throw "Unity Edge package validation failed with exit code $($unityProcess.ExitCode). See '$logPath'."
}
$errors = Select-String -LiteralPath $logPath -Pattern 'error CS\d+|Assembly .* will not be loaded|Failed to resolve packages' -CaseSensitive:$false
if ($errors) { throw "Unity reported package compilation errors. See '$logPath'." }
if (-not (Select-String -LiteralPath $logPath -Pattern 'OASIS_EDGE_UNITY_PACKAGE_VALIDATION_PASSED' -Quiet)) {
    throw "Unity did not complete the Edge package secure-storage smoke test. See '$logPath'."
}
$androidLogPath = Join-Path $logRoot 'unity-edge-android-validation.log'
$androidProcess = Start-Process -FilePath $UnityEditor -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', $projectRoot, '-buildTarget', 'Android',
    '-executeMethod', 'NextGenSoftware.OASIS.Edge.Unity.Editor.OASISEdgePackageValidator.ValidateAndroidBuild',
    '-logFile', $androidLogPath
) -WindowStyle Hidden -Wait -PassThru
if ($androidProcess.ExitCode -ne 0 -or
    -not (Select-String -LiteralPath $androidLogPath -Pattern 'OASIS_EDGE_ANDROID_BUILD_VALIDATION_PASSED' -Quiet)) {
    throw "Unity Android Edge package validation failed. See '$androidLogPath'."
}
Write-Host "Unity Edge package compiled successfully. Log: $logPath"
