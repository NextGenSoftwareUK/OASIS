<#
.SYNOPSIS
  Prints the OASIS banner (coloured header + slogan). Used by BUILD ODOOM.bat, BUILD_OQUAKE.bat, BUILD EVERYTHING.bat.
.PARAMETER Title
  Main title line (e.g. "O A S I S   O D O O M").
.PARAMETER Subtitle
  Optional line under the title (e.g. "By NextGen World Ltd").
.PARAMETER Version
  Optional version string; if set, title is shown as "Title  vVersion". Can also come from env var VERSION_DISPLAY.
.PARAMETER Success
  If set, use green theme and Message as the single line (for completion banner).
.PARAMETER Message
  When -Success is set, this message is shown (e.g. "Run RUN ODOOM.bat or RUN OQUAKE.bat to launch.").
.PARAMETER Message2
  Optional second line when -Success is set.
#>
param(
    [Parameter(Mandatory=$false)][string]$Title,
    [string]$Subtitle = "By NextGen World Ltd",
    [string]$Version,
    [switch]$Success,
    [string]$Message,
    [string]$Message2
)

$w = 60
function Center($s) { $p = [math]::Max(0, [int](($w - $s.Length) / 2)); "  " + (" " * $p) + $s }

$slogan = "Enabling full interoperable games across the OASIS Omniverse!"

if ($Success) {
    Write-Host ""
    Write-Host ("  " + ("=" * $w)) -ForegroundColor DarkGreen
    Write-Host (Center $Message) -ForegroundColor Green
    if ($Message2) { Write-Host (Center $Message2) -ForegroundColor DarkGray }
    Write-Host ("  " + ("=" * $w)) -ForegroundColor DarkGreen
    Write-Host ""
} else {
    $ver = if ($Version) { $Version } else { $env:VERSION_DISPLAY }
    $displayTitle = if ($ver) { $Title + "  v" + $ver } else { $Title }
    Write-Host ""
    Write-Host ("  " + ("=" * $w)) -ForegroundColor DarkCyan
    Write-Host (Center $displayTitle) -ForegroundColor Cyan
    if ($Subtitle) { Write-Host (Center $Subtitle) -ForegroundColor DarkGray }
    Write-Host ("  " + ("=" * $w)) -ForegroundColor DarkCyan
    Write-Host (Center $slogan) -ForegroundColor DarkMagenta
    Write-Host ""
}
