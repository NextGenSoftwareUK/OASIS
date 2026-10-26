<#
Remove the piper-ml conda environments from WSL Ubuntu to reclaim disk space.

Removes: piper-art (~11 GB), slp (~9.3 GB), mfa (~1.7 GB).
Optional: -IncludeModels also removes ~/art-ml-models (~2.6 GB of downloaded models).

All of it is regenerable from the piper-ml repository:
  mamba env create -f envs/piper-art_env.yml
  mamba env create -f envs/slp_env.yml
  mamba create -n mfa -c conda-forge montreal-forced-aligner=3.1.3

The space is freed inside Ubuntu; run cleanup-emergency-space.ps1 (admin) afterwards
to compact the WSL disk and return it to Windows.

Usage:
  .\remove-piper-wsl-envs.ps1 -DryRun
  .\remove-piper-wsl-envs.ps1
  .\remove-piper-wsl-envs.ps1 -IncludeModels
#>

param(
    [switch]$DryRun,
    [switch]$IncludeModels,
    [string[]]$Environment = @('piper-art', 'slp', 'mfa'),
    [string]$WslDistribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'

function Log($msg) {
    Write-Host "[remove-piper] $msg" -ForegroundColor Cyan
}

function Invoke-Wsl([string]$command) {
    $output = & wsl.exe -d $WslDistribution -- bash -lc $command 2>&1
    if ($LASTEXITCODE -ne 0) { throw "WSL command failed (exit $LASTEXITCODE): $command`n$($output -join "`n")" }
    return $output
}

$conda = '"$HOME/miniforge3/bin/conda"'
$targets = @($Environment | ForEach-Object { "`$HOME/miniforge3/envs/$_" })
if ($IncludeModels) { $targets += '$HOME/art-ml-models' }

foreach ($target in $targets) {
    $size = Invoke-Wsl "if [ -d `"$target`" ]; then du -sh `"$target`" | cut -f1; else echo absent; fi"
    Log "$target : $size"
}

# A process running from one of these environments would break mid-run.
$envPattern = ($Environment | ForEach-Object { [regex]::Escape("/miniforge3/envs/$_/") }) -join '|'
$busy = @(Invoke-Wsl "ps -eo pid=,args= | grep -E '$envPattern' | grep -v grep || true" | Where-Object { $_ })
if ($busy.Count) { throw "Processes are running from these environments; stop them first:`n$($busy -join "`n")" }

if ($DryRun) {
    foreach ($name in $Environment) { Log "DRY RUN: conda env remove -n $name -y" }
    if ($IncludeModels) { Log 'DRY RUN: rm -rf ~/art-ml-models' }
    return
}

foreach ($name in $Environment) {
    $exists = Invoke-Wsl "[ -d `"`$HOME/miniforge3/envs/$name`" ] && echo yes || echo no"
    if ($exists -ne 'yes') { Log "$name not present; nothing to remove."; continue }
    Log "Removing conda environment $name..."
    [void](Invoke-Wsl "$conda env remove -n $name -y")
}
if ($IncludeModels) {
    Log 'Removing ~/art-ml-models...'
    [void](Invoke-Wsl 'rm -rf -- "$HOME/art-ml-models"')
}
[void](Invoke-Wsl "$conda clean --all --yes")

Log ("Ubuntu disk now: " + (Invoke-Wsl 'df -h / | tail -1'))
Log 'Run cleanup-emergency-space.ps1 as administrator to compact the WSL disk and return the space to Windows.'
