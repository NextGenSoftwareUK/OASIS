<#
.SYNOPSIS
    Install the shared Raze OASIS integration into ODuke3D-RT and build it.
.DESCRIPTION
    ODuke3D-RT is a Raze fork; it uses the shared Raze integration from
    OGames/OShadowWarrior (OGLib/oglib_game.h). This forwards to BUILD_ODUKE3DRT.bat.
#>
param([switch]$BatchMode)
$here = Split-Path -Parent $PSScriptRoot
$cmdArgs = @()
if ($BatchMode) { $cmdArgs += "batch" }
& cmd /c "`"$here\BUILD_ODUKE3DRT.bat`" $cmdArgs"
exit $LASTEXITCODE
