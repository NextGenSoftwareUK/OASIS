#Requires -Version 7.0
[CmdletBinding()]
param([switch]$ArchiveOriginalV1Reports)
$ErrorActionPreference='Stop'
$pgTaskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $pgTaskRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'PostgreSQL current canonical storage breadth' -Operation {
    & (Join-Path $pgTaskRoot 'Scripts/TestHosts/Run-ReusablePostgreSqlTests.ps1') -ConformanceOnly -ArchiveOriginalV1Reports:$ArchiveOriginalV1Reports
}
