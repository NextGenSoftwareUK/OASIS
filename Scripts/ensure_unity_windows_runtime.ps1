$ErrorActionPreference = 'Stop'
if (!$IsWindows -or ![Environment]::Is64BitProcess) {
    throw 'Unity Windows runtime provisioning requires 64-bit PowerShell on Windows.'
}
# Unity 2022.3 OpenRL.dll imports these VC++ 2010 DLLs. Editor caches do not
# provision machine-wide prerequisites on a fresh GitHub-hosted runner.
$runtimeFiles = @('MSVCP100.dll', 'MSVCR100.dll') | ForEach-Object {
    Join-Path ([Environment]::GetFolderPath('System')) $_
}
$missing = @($runtimeFiles | Where-Object { !(Test-Path -LiteralPath $_ -PathType Leaf) })
if ($missing.Count -gt 0) {
    $installerPath = Join-Path ([IO.Path]::GetTempPath()) 'oasis-unity-vc2010-x64.exe'
    if (!(Test-Path -LiteralPath $installerPath -PathType Leaf)) {
        Invoke-WebRequest -Uri 'https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe' -OutFile $installerPath
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $installerPath
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        throw 'The Unity VC++ prerequisite installer does not have a valid Microsoft signature.'
    }
    $installer = Start-Process -FilePath $installerPath -ArgumentList '/q', '/norestart' -WindowStyle Hidden -PassThru
    if (!$installer.WaitForExit(120000)) {
        $installer.Kill($true)
        $installer.WaitForExit()
        throw 'Unity VC++ prerequisite installation exceeded two minutes.'
    }
    if ($installer.ExitCode -notin @(0, 3010)) {
        throw "Unity VC++ prerequisite installation failed: $($installer.ExitCode)."
    }
    # This fixed-path installer is owned by this script and its process has stopped.
    Remove-Item -LiteralPath $installerPath -Force
}
foreach ($runtimeFile in $runtimeFiles) {
    if (!(Test-Path -LiteralPath $runtimeFile -PathType Leaf)) {
        throw "Unity's required Windows runtime is absent: $runtimeFile"
    }
    Write-Host "Unity prerequisite verified: $runtimeFile ($((Get-Item -LiteralPath $runtimeFile).VersionInfo.FileVersion))"
}
