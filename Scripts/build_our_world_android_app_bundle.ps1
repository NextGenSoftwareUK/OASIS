[CmdletBinding()]
param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [string]$ProjectDirectory = 'artifacts\our-world-play-build\project',
    [string]$SigningEnvironmentFile = 'artifacts\our-world-release-secrets\android-signing.secrets.env',
    [string]$OutputPath = 'artifacts\our-world-release\OurWorld-Edge-Android-production-signed.aab',
    [ValidateSet('AppBundle', 'Apk')]
    [string]$PackageFormat = 'AppBundle',
    [string]$ApplicationId = 'com.NextGenWorldLtd.OASISOmniverse',
    [string]$BundleVersion = '1.0.0',
    [int]$VersionCode = 1
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $ProjectDirectory))
$secretsPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $SigningEnvironmentFile))
$bundlePath = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputPath))
$releaseRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts\our-world-release')) +
    [IO.Path]::DirectorySeparatorChar
if (!$bundlePath.StartsWith($releaseRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "The signed app bundle must be written under '$releaseRoot'."
}
foreach ($requiredPath in @($UnityEditor, $secretsPath,
    (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'))) {
    if (!(Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required build input is missing: $requiredPath" }
}

$requiredSecrets = @(
    'OASIS_ANDROID_KEYSTORE_PATH', 'OASIS_ANDROID_KEYSTORE_PASSWORD',
    'OASIS_ANDROID_KEY_ALIAS', 'OASIS_ANDROID_KEY_PASSWORD'
)
$environmentNames = [Collections.Generic.List[string]]::new()
foreach ($line in Get-Content -LiteralPath $secretsPath) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
    $separator = $line.IndexOf('=')
    if ($separator -lt 1) { continue }
    $name = $line.Substring(0, $separator).Trim()
    $value = $line.Substring($separator + 1).Trim()
    if (($value.StartsWith('"') -and $value.EndsWith('"')) -or
        ($value.StartsWith("'") -and $value.EndsWith("'"))) {
        $value = $value.Substring(1, $value.Length - 2)
    }
    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
    $environmentNames.Add($name)
}
foreach ($name in $requiredSecrets) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "Signing environment file is missing $name."
    }
}

$buildEnvironment = [ordered]@{
    OASIS_ANDROID_APPLICATION_ID = $ApplicationId
    OASIS_ANDROID_BUNDLE_VERSION = $BundleVersion
    OASIS_ANDROID_VERSION_CODE = $VersionCode.ToString()
    OASIS_OUR_WORLD_ANDROID_OUTPUT = $bundlePath
}
foreach ($setting in $buildEnvironment.GetEnumerator()) {
    [Environment]::SetEnvironmentVariable($setting.Key, $setting.Value, 'Process')
    $environmentNames.Add($setting.Key)
}

$logPath = Join-Path (Split-Path $bundlePath -Parent) 'our-world-production-aab-build.log'
Remove-Item -LiteralPath $bundlePath, $logPath -Force -ErrorAction SilentlyContinue
try {
    $buildMethod = if ($PackageFormat -eq 'AppBundle') {
        'OASIS.Omniverse.UnityHost.Editor.OurWorldAndroidBuildValidator.BuildSignedAndroidAppBundle'
    } else {
        'OASIS.Omniverse.UnityHost.Editor.OurWorldAndroidBuildValidator.BuildSignedAndroidPlayer'
    }
    $arguments = @(
        '-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot,
        '-buildTarget', 'Android', '-executeMethod',
        $buildMethod,
        '-logFile', $logPath
    )
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
}
finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }
}
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $bundlePath -PathType Leaf)) {
    throw "Signed Android App Bundle build failed. See '$logPath'."
}
if (!(Select-String -LiteralPath $logPath -Pattern 'OASIS_OUR_WORLD_ANDROID_BUILD_PASSED' -Quiet)) {
    throw "Signed Android App Bundle build did not emit its success invariant. See '$logPath'."
}

$hash = (Get-FileHash -LiteralPath $bundlePath -Algorithm SHA256).Hash
Write-Host "Signed Android $PackageFormat created: $bundlePath"
Write-Host "SHA-256: $hash"
